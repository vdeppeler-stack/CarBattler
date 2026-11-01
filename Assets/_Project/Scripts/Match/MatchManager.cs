using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSyndicate
{
    /// <summary>
    /// Round flow: seeded spawn selection, countdown, one life per participant, last vehicle wins,
    /// simultaneous elimination of all remaining vehicles is a draw. Restart reloads the scene, which
    /// resets inventory, energy, statuses, projectiles, destroyed geometry and the seed.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public enum Phase { Countdown, Running, Ended }

        public static MatchManager Instance { get; private set; }

        public float countdownSeconds = 3f;
        [Header("Defaults when the scene is started directly")]
        public string defaultPlayerVehicle = "goliath_wheel";
        public int defaultBotCount = 4;
        public bool defaultHumanPlaying = true;
        [Tooltip("Test lab: no round end, participants may be added and removed freely.")]
        public bool labMode;
        public Color[] teamColors =
        {
            new Color(0.1f, 0.6f, 1f), new Color(1f, 0.25f, 0.2f), new Color(0.2f, 0.9f, 0.3f),
            new Color(1f, 0.8f, 0.1f), new Color(0.8f, 0.3f, 1f), new Color(1f, 0.5f, 0.1f),
            new Color(0.3f, 1f, 1f), new Color(1f, 0.4f, 0.7f), new Color(0.6f, 0.6f, 0.6f), new Color(0.5f, 0.3f, 0.1f)
        };

        public Phase State { get; private set; } = Phase.Countdown;
        public float CountdownLeft { get; private set; }
        public float RoundTime { get; private set; }
        public int Seed { get; private set; }
        public readonly List<Vehicle> Participants = new List<Vehicle>();
        public Vehicle Human { get; private set; }
        public Vehicle Winner { get; private set; }
        public bool Draw { get; private set; }
        public bool AllowsControl => State == Phase.Running;

        public event Action<MatchManager> Ended;
        /// <summary>Any participant eliminated (victim, killer or null).</summary>
        public event Action<Vehicle, Vehicle> Eliminated;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            TestLog.EndRun();
        }

        void Start()
        {
            if (!MatchSetup.Configured)
            {
                MatchSetup.Configured = true;
                MatchSetup.PlayerVehicleId = defaultPlayerVehicle;
                MatchSetup.BotCount = defaultBotCount;
                MatchSetup.HumanPlaying = defaultHumanPlaying;
            }
            if (MatchSetup.RandomizeSeedOnStart) MatchSetup.Seed = Environment.TickCount & 0x7fffffff;
            MatchSetup.RandomizeSeedOnStart = false;
            Seed = MatchSetup.Seed;
            UnityEngine.Random.InitState(Seed);
            GrabLink.Clear();
            TestLog.BeginRun(MatchSetup.Label + "_" + SceneManager.GetActiveScene().name, Seed);
            SpawnParticipants();
            CountdownLeft = countdownSeconds;
            FindAnyObjectByType<MatchHud>()?.Bind(this);
        }

        void SpawnParticipants()
        {
            var db = GameDatabase.Instance;
            var rng = new System.Random(Seed);
            var spawns = SpawnPoint.All.Where(s => !s.blocked).OrderBy(s => s.index).ToList();
            if (MatchSetup.SpawnOrder.Count > 0)
                spawns = MatchSetup.SpawnOrder.Select(i => spawns.FirstOrDefault(s => s.index == i)).Where(s => s != null).ToList();
            else
                spawns = spawns.OrderBy(_ => rng.Next()).ToList();

            var lineup = new List<(VehicleDefinition def, bool human)>();
            if (MatchSetup.HumanPlaying) lineup.Add((db.Vehicle(MatchSetup.PlayerVehicleId) ?? db.Playable().First(), true));
            var pool = db.Playable().ToList();
            for (int i = 0; i < MatchSetup.BotCount; i++)
            {
                VehicleDefinition def = i < MatchSetup.BotVehicleIds.Count ? db.Vehicle(MatchSetup.BotVehicleIds[i]) : null;
                if (def == null) def = pool[rng.Next(pool.Count)];
                lineup.Add((def, false));
            }

            for (int i = 0; i < lineup.Count && i < spawns.Count; i++)
            {
                var sp = spawns[i];
                var v = VehicleFactory.Spawn(lineup[i].def, sp.transform.position + Vector3.up * 0.3f, sp.transform.rotation, i,
                    lineup[i].human, teamColors[i % teamColors.Length], rng.Next());
                v.Died += OnVehicleDied;
                Participants.Add(v);
                if (lineup[i].human) Human = v;
                TestLog.Write("match", v.name, "spawn", $"vehicle={lineup[i].def.id};spawn={sp.index};human={lineup[i].human}");
            }
            if (lineup.Count > spawns.Count) Debug.LogWarning($"[IS] Only {spawns.Count} free spawns for {lineup.Count} participants.");

            var cam = FindAnyObjectByType<ChaseCamera>();
            if (cam != null) cam.target = Human != null ? Human : Participants.FirstOrDefault();
        }

        /// <summary>
        /// Adds a participant mid-round (lab spawns, boss phase 2). Called from inside a death handler it is
        /// counted in the same step, so a boss phase change never ends the round.
        /// </summary>
        public Vehicle SpawnExtra(VehicleDefinition def, Vector3 position, Quaternion rotation, bool human, string nameOverride = null)
        {
            int seat = Participants.Count;
            var v = VehicleFactory.Spawn(def, position, rotation, seat, human, teamColors[seat % teamColors.Length], Seed + seat * 7919);
            if (!string.IsNullOrEmpty(nameOverride)) v.name = $"{nameOverride} [{seat}]";
            v.Died += OnVehicleDied;
            Participants.Add(v);
            if (human) Human = v;
            TestLog.Write("match", v.name, "spawn-extra", $"vehicle={def.id};pos={position:F0};human={human}");
            return v;
        }

        /// <summary>Lab: removes a participant without a kill.</summary>
        public void Despawn(Vehicle v)
        {
            if (v == null) return;
            Participants.Remove(v);
            if (Human == v) Human = null;
            GrabLink.ReleaseAll(v);
            Destroy(v.gameObject);
        }

        void OnVehicleDied(Vehicle v, Vehicle killer)
        {
            Eliminated?.Invoke(v, killer);
            if (Human != null && v == Human)
            {
                var cam = FindAnyObjectByType<ChaseCamera>();
                if (cam != null && killer != null) cam.spectate = killer;
            }
        }

        void Update()
        {
            switch (State)
            {
                case Phase.Countdown:
                    CountdownLeft -= Time.deltaTime;
                    if (CountdownLeft <= 0f)
                    {
                        State = Phase.Running;
                        TestLog.Write("match", "-", "start", $"participants={Participants.Count}");
                    }
                    break;
                case Phase.Running:
                    RoundTime += Time.deltaTime;
                    // Evaluated after the physics step, so eliminations in the same step count together.
                    if (labMode) break;
                    int alive = Participants.Count(p => p != null && p.IsAlive);
                    if (alive <= 1 && Participants.Count > 1) End(alive == 1 ? Participants.First(p => p != null && p.IsAlive) : null);
                    break;
            }
        }

        void End(Vehicle winner)
        {
            State = Phase.Ended;
            Winner = winner;
            Draw = winner == null;
            TestLog.Write("match", winner != null ? winner.name : "-", Draw ? "draw" : "winner", $"duration={RoundTime:F1}");
            foreach (var p in Participants)
            {
                var s = p.Stats;
                string bySource = string.Join(",", s.damageBySource.Select(kv => $"{kv.Key}:{kv.Value:F0}"));
                TestLog.Write("result", p.name, p.Definition.id,
                    $"alive={p.IsAlive};hp={p.Hp:F0};dealt={s.damageDealt:F0};taken={s.damageTaken:F0};self={s.selfDamage:F0};healed={s.healed:F0};" +
                    $"specialTries={s.specialAttempts};specialHits={s.specialHits};stationary={s.stationaryTime:F1};sources={bySource}");
            }
            Ended?.Invoke(this);
        }

        /// <summary>Same lineup and seed again.</summary>
        public void Restart()
        {
            Projectile.DespawnAll();
            MatchSetup.RandomizeSeedOnStart = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : SceneManager.GetActiveScene().path);
        }

        /// <summary>Back to vehicle select if it is in the build, else cycle the player's vehicle.</summary>
        public void ChangeVehicle()
        {
            Projectile.DespawnAll();
            if (Application.CanStreamedLevelBeLoaded("VehicleSelect"))
            {
                SceneManager.LoadScene("VehicleSelect");
                return;
            }
            var list = GameDatabase.Instance.Playable().ToList();
            int idx = list.FindIndex(v => v.id == MatchSetup.PlayerVehicleId);
            MatchSetup.PlayerVehicleId = list[(idx + 1) % list.Count].id;
            MatchSetup.RandomizeSeedOnStart = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
