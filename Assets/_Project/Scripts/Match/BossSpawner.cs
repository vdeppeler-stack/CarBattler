using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Starts the boss fight exactly once when the elimination count reaches <see cref="killsRequired"/>.
    /// Counts real eliminations of non-boss participants; the lab can add simulated ones.
    /// </summary>
    public class BossSpawner : MonoBehaviour
    {
        public string bossId = "dread_jester";
        public int killsRequired = 8;
        public Transform pad;

        public int Kills { get; private set; }
        public bool Spawned { get; private set; }
        public Vehicle Boss { get; private set; }

        MatchManager _mm;

        void Start()
        {
            _mm = MatchManager.Instance;
            if (_mm != null) _mm.Eliminated += OnEliminated;
        }

        void OnDestroy()
        {
            if (_mm != null) _mm.Eliminated -= OnEliminated;
        }

        void OnEliminated(Vehicle v, Vehicle killer)
        {
            if (v.Definition.boss) return;
            Register("elimination:" + v.name);
        }

        /// <summary>Lab: a simulated "last opponent" elimination.</summary>
        public void SimulateKill() => Register("simulated");

        void Register(string what)
        {
            Kills++;
            TestLog.Write("boss", name, "kill-count", $"{Kills}/{killsRequired};{what}");
            if (Spawned || Kills < killsRequired || _mm == null) return;
            var def = GameDatabase.Instance.Vehicle(bossId);
            if (def == null) return;
            Spawned = true;
            var p = pad != null ? pad : transform;
            Boss = _mm.SpawnExtra(def, p.position + Vector3.up * 0.5f, p.rotation, false);
            TestLog.Write("boss", Boss.name, "boss-fight-start", $"after={Kills}");
        }
    }
}
