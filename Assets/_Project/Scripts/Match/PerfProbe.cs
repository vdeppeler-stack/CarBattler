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
    /// Laptop reference measurement (handbook 11): Stage 1 at 1280x720 with active bots, 60 s warm-up, then
    /// 10 minutes of frame times. Rounds restart automatically; the highway span is blown at minute 3 and the
    /// reactor fires regularly so explosions, Tesla and bridge collapse are inside the window.
    /// Writes frame times (CSV) and a summary with average, 95th/99th percentile and frames over 33.3 ms.
    /// Start: build with "-perf [-perfminutes 10] [-warmup 60]".
    /// </summary>
    public class PerfProbe : MonoBehaviour
    {
        public string arenaScene = "Tectonic_Wreck";
        public float warmupSeconds = 60f;
        public float measureSeconds = 600f;
        public int bots = 4;
        public string outDir;

        readonly List<float> _frames = new List<float>(40000);
        float _t0;
        bool _measuring, _bridgeDone;
        float _nextTesla;
        int _rounds;

        FrameStats _stats;
        public static bool NoVSync;

        public static void Start(float warmup, float minutes, string outDir)
        {
            var go = new GameObject("[PerfProbe]");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<PerfProbe>();
            p.warmupSeconds = warmup;
            p.measureSeconds = minutes * 60f;
            p.outDir = outDir;
        }

        void Awake()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            if (NoVSync) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; }
            Application.runInBackground = true;
            LoadRound();
            _t0 = Time.realtimeSinceStartup;
        }

        void LoadRound()
        {
            MatchSetup.Configured = true;
            // Four bots plus a bot in the human seat so the camera follows real play.
            MatchSetup.HumanPlaying = false;
            MatchSetup.BotCount = bots + 1;
            MatchSetup.BotVehicleIds.Clear();
            MatchSetup.SpawnOrder.Clear();
            MatchSetup.RandomizeSeedOnStart = true;
            MatchSetup.Label = "perf";
            Projectile.DespawnAll();
            SceneManager.LoadScene(arenaScene);
            _rounds++;
        }

        void Update()
        {
            float elapsed = Time.realtimeSinceStartup - _t0;
            var mm = MatchManager.Instance;
            if (mm != null && mm.State == MatchManager.Phase.Ended) { LoadRound(); return; }

            if (!_measuring && elapsed >= warmupSeconds)
            {
                _measuring = true;
                _stats = FrameStats.Begin(measureSeconds + 5f);
                _frames.Clear();
                _bridgeDone = false;
                Debug.Log("[IS] Perf: measuring");
            }
            if (!_measuring) return;
            _frames.Add(Time.unscaledDeltaTime * 1000f);
            float m = elapsed - warmupSeconds;

            if (!_bridgeDone && m > 180f)
            {
                var span = Destructible.All.FirstOrDefault(d => d.name == "Highway_Span");
                if (span != null) span.Break(null, "perf-script");
                _bridgeDone = true;
            }
            if (Time.time >= _nextTesla && mm != null && mm.Participants.Count > 0)
            {
                _nextTesla = Time.time + 20f;
                var shooter = mm.Participants.FirstOrDefault(p => p != null && p.IsAlive);
                if (shooter != null) TeslaReactor.TryDischarge(shooter);
            }
            if (m >= measureSeconds) Finish();
        }

        void Finish()
        {
            Directory.CreateDirectory(outDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sorted = _frames.OrderBy(f => f).ToList();
            float P(float q) => sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)];
            float avg = _frames.Average();
            int over = _frames.Count(f => f > 33.3f);
            var sb = new StringBuilder();
            sb.AppendLine("# Laptopmessung Stage 1");
            sb.AppendLine();
            sb.AppendLine($"- Gerät: {SystemInfo.deviceModel} · CPU {SystemInfo.processorType} ({SystemInfo.processorCount} Threads) · GPU {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB) · RAM {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"- Betrieb: {(SystemInfo.batteryStatus == BatteryStatus.Discharging ? "Akku" : "Netz/unklar")} · Auflösung {Screen.width}x{Screen.height} · Build {Application.version} · Unity {Application.unityVersion}");
            sb.AppendLine($"- Aufwärmen {warmupSeconds:F0} s, Messung {measureSeconds / 60f:0.#} min, {_frames.Count} Frames, {_rounds} Runden, Brückeneinsturz {(_bridgeDone ? "ja" : "nein")}");
            sb.AppendLine();
            sb.AppendLine("| Wert | ms | FPS |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| Mittel | {avg:F1} | {1000f / avg:F0} |");
            sb.AppendLine($"| 95. Perzentil | {P(0.95f):F1} | {1000f / P(0.95f):F0} |");
            sb.AppendLine($"| 99. Perzentil | {P(0.99f):F1} | {1000f / P(0.99f):F0} |");
            sb.AppendLine($"| Maximum | {sorted[sorted.Count - 1]:F1} | |");
            sb.AppendLine();
            sb.AppendLine($"Frames über 33,3 ms: {over} ({over * 100f / _frames.Count:F1} %). Ziel: 95. Perzentil ≤ 33,3 ms → {(P(0.95f) <= 33.3f ? "erreicht" : "nicht erreicht")}.");
            File.WriteAllText(Path.Combine(outDir, $"perf_{stamp}.md"), sb.ToString(), new UTF8Encoding(false));
            if (_stats != null) _stats.Report(Path.Combine(outDir, $"perf_{stamp}_breakdown.txt"));
            File.WriteAllText(Path.Combine(outDir, $"perf_{stamp}_frames.csv"), "ms\n" + string.Join("\n", _frames.Select(f => f.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))), new UTF8Encoding(false));
            Debug.Log("[IS] Perf finished: " + Path.Combine(outDir, $"perf_{stamp}.md"));
            if (!Application.isEditor) Application.Quit();
            Destroy(gameObject);
        }
    }
}
