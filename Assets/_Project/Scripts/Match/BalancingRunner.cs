using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSyndicate
{
    /// <summary>
    /// Balancing run (handbook 10): every pair of the 14 playable vehicles on Stage 1, both start sides,
    /// three fixed seeds = 546 duels, same pickup rules. Rounds without a result after
    /// <see cref="maxRoundSeconds"/> are decided by remaining HP share (equal = draw).
    /// Writes one CSV row per duel and a Markdown summary with win rates and the pair matrix.
    /// Start: build with "-balancing [-seeds 11,22,33] [-timescale 6] [-limit N]", or from the editor menu.
    /// </summary>
    public class BalancingRunner : MonoBehaviour
    {
        public struct Duel { public string a, b; public int side, seed; }

        public string arenaScene = "Tectonic_Wreck";
        public int[] spawnPair = { 0, 1 };
        public float maxRoundSeconds = 180f;
        public float timeScale = 6f;
        public string outDir;

        readonly List<Duel> _duels = new List<Duel>();
        readonly List<string> _rows = new List<string>();
        int _index = -1;
        bool _waiting;
        float _startedRealtime;

        public static BalancingRunner Instance { get; private set; }
        public int Done => Mathf.Max(0, _index);
        public int Total => _duels.Count;
        public bool Finished { get; private set; }

        public static BalancingRunner Start(int[] seeds, int limit, float timeScale, string outDir)
        {
            var go = new GameObject("[BalancingRunner]");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<BalancingRunner>();
            r.timeScale = timeScale;
            r.outDir = outDir;
            var ids = GameDatabase.Instance.Playable().Select(v => v.id).ToList();
            for (int i = 0; i < ids.Count; i++)
                for (int j = i + 1; j < ids.Count; j++)
                    foreach (int seed in seeds)
                        for (int side = 0; side < 2; side++)
                            r._duels.Add(new Duel { a = ids[i], b = ids[j], side = side, seed = seed });
            if (limit > 0 && limit < r._duels.Count) r._duels.RemoveRange(limit, r._duels.Count - limit);
            Instance = r;
            return r;
        }

        void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        void Begin()
        {
            Directory.CreateDirectory(outDir);
            _startedRealtime = Time.realtimeSinceStartup;
            Debug.Log($"[IS] Balancing: {_duels.Count} duels on {arenaScene}, out={outDir}");
            Next();
        }

        void Next()
        {
            _index++;
            if (_index >= _duels.Count) { Finish(); return; }
            var d = _duels[_index];
            MatchSetup.Configured = true;
            MatchSetup.HumanPlaying = false;
            MatchSetup.BotCount = 2;
            MatchSetup.BotVehicleIds.Clear();
            MatchSetup.BotVehicleIds.Add(d.a);
            MatchSetup.BotVehicleIds.Add(d.b);
            MatchSetup.SpawnOrder.Clear();
            MatchSetup.SpawnOrder.Add(spawnPair[d.side]);
            MatchSetup.SpawnOrder.Add(spawnPair[1 - d.side]);
            MatchSetup.Seed = d.seed;
            MatchSetup.RandomizeSeedOnStart = false;
            MatchSetup.Label = $"duel{_index:000}";
            Projectile.DespawnAll();
            SceneManager.LoadScene(arenaScene);
            _waiting = true;
        }

        void Update()
        {
            if (_index < 0) { Begin(); return; }
            if (Finished || !_waiting) return;
            Time.timeScale = timeScale;
            var mm = MatchManager.Instance;
            if (mm == null || mm.Participants.Count < 2) return;
            bool timeout = mm.State == MatchManager.Phase.Running && mm.RoundTime >= maxRoundSeconds;
            if (mm.State != MatchManager.Phase.Ended && !timeout) return;
            Record(mm, timeout);
            _waiting = false;
            Next();
        }

        void Record(MatchManager mm, bool timeout)
        {
            var d = _duels[_index];
            var va = mm.Participants.First(p => p.Definition.id == d.a);
            var vb = mm.Participants.Last(p => p.Definition.id == d.b);
            string winner;
            if (!timeout) winner = mm.Draw ? "draw" : mm.Winner.Definition.id;
            else
            {
                float fa = va.HpFraction, fb = vb.HpFraction;
                winner = Mathf.Abs(fa - fb) < 0.02f ? "draw" : (fa > fb ? d.a : d.b);
            }
            _rows.Add(string.Join(";", _index, d.a, d.b, d.side, d.seed, winner, timeout ? "timeout" : "ko",
                mm.RoundTime.ToString("F1"), va.Hp.ToString("F0"), vb.Hp.ToString("F0"),
                va.Stats.damageDealt.ToString("F0"), vb.Stats.damageDealt.ToString("F0")));
            if (_index % 10 == 0 || _index == _duels.Count - 1) Flush();
            Debug.Log($"[IS] Duel {_index + 1}/{_duels.Count}: {d.a} vs {d.b} side {d.side} seed {d.seed} -> {winner} ({(timeout ? "timeout" : "ko")}, {mm.RoundTime:F0}s)");
        }

        void Flush()
        {
            var sb = new StringBuilder("index;a;b;side;seed;winner;end;seconds;hp_a;hp_b;dealt_a;dealt_b\n");
            foreach (var r in _rows) sb.AppendLine(r);
            File.WriteAllText(Path.Combine(outDir, "duelle.csv"), sb.ToString(), new UTF8Encoding(false));
        }

        void Finish()
        {
            Finished = true;
            Time.timeScale = 1f;
            Flush();
            File.WriteAllText(Path.Combine(outDir, "zusammenfassung.md"), Summary(), new UTF8Encoding(false));
            Debug.Log($"[IS] Balancing finished: {_rows.Count} duels in {(Time.realtimeSinceStartup - _startedRealtime) / 60f:F1} min");
            if (!Application.isEditor) Application.Quit();
        }

        string Summary()
        {
            var db = GameDatabase.Instance;
            var ids = db.Playable().Select(v => v.id).ToList();
            var wins = ids.ToDictionary(i => i, _ => 0);
            var losses = ids.ToDictionary(i => i, _ => 0);
            var draws = ids.ToDictionary(i => i, _ => 0);
            var pair = new Dictionary<(string, string), int>();
            int timeouts = 0;
            foreach (var row in _rows)
            {
                var c = row.Split(';');
                string a = c[1], b = c[2], w = c[5];
                if (c[6] == "timeout") timeouts++;
                if (w == "draw") { draws[a]++; draws[b]++; continue; }
                string l = w == a ? b : a;
                wins[w]++; losses[l]++;
                pair[(w, l)] = pair.TryGetValue((w, l), out var n) ? n + 1 : 1;
            }
            var sb = new StringBuilder();
            sb.AppendLine("# Balancing Stage 1 (Tectonic Wreck) – Duelle");
            sb.AppendLine();
            sb.AppendLine($"Duelle: {_rows.Count} · Entscheidung nach Zeit ({maxRoundSeconds:F0} s, HP-Anteil): {timeouts} · Build {Application.version} · Unity {Application.unityVersion} · {SystemInfo.deviceModel}");
            sb.AppendLine();
            sb.AppendLine("| Fahrzeug | Siege | Niederlagen | Unentschieden | Siegquote |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var id in ids.OrderByDescending(i => wins[i] / (float)Mathf.Max(1, wins[i] + losses[i] + draws[i])))
            {
                int games = wins[id] + losses[id] + draws[id];
                sb.AppendLine($"| {db.Vehicle(id).displayName} | {wins[id]} | {losses[id]} | {draws[id]} | {(games > 0 ? wins[id] * 100f / games : 0f):F0} % |");
            }
            sb.AppendLine();
            sb.AppendLine("## Paarungen (Siege Zeile gegen Spalte)");
            sb.AppendLine();
            sb.Append("| |");
            foreach (var id in ids) sb.Append($" {Short(id)} |");
            sb.AppendLine();
            sb.Append("|---|");
            foreach (var _ in ids) sb.Append("---|");
            sb.AppendLine();
            foreach (var r in ids)
            {
                sb.Append($"| {Short(r)} |");
                foreach (var c in ids) sb.Append(r == c ? " – |" : $" {(pair.TryGetValue((r, c), out var n) ? n : 0)} |");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("Auffällig sind Quoten unter 30 % oder über 70 %; Ursachen im Duell-CSV und in den Rundenprotokollen nachsehen.");
            return sb.ToString();
        }

        static string Short(string id) => id.Length <= 6 ? id : id.Substring(0, 6);
    }
}
