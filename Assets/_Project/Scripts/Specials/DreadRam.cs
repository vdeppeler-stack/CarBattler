using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Dread Jester phase 1: heavy fairground hauler with permanently raised ram damage. Activation is a
    /// 3 s ram run (more speed, much more ram damage). On death the burning carousel top breaks loose and
    /// continues the fight as phase 2 (<see cref="HellfireCarousel"/>); only its death ends the boss fight.
    /// </summary>
    public class DreadRam : SpecialAbility
    {
        public const string PhaseTwoId = "hellfire_carousel";
        const float RunSeconds = 3f;

        float _runUntil;

        public override bool IsActive => Time.time < _runUntil;
        public override float RamMultiplier => IsActive ? 4f : 2.5f;

        public override bool TryActivate(out string reason)
        {
            _runUntil = Time.time + RunSeconds;
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            V.Motor.SpeedMultiplier = IsActive ? 1.4f : 1f;
            if (IsActive && V.Motor.Grounded)
                V.Body.AddForce(V.transform.forward * 6f, ForceMode.Acceleration);
        }

        public override void OnRam(Vehicle other, float closing)
        {
            var away = (other.transform.position - V.transform.position).normalized;
            other.Body.AddForce((away * 5f + Vector3.up * 3f) * (IsActive ? 1.6f : 1f), ForceMode.VelocityChange);
            ReportHit(other);
        }

        public override void OnOwnerDeath()
        {
            V.Motor.SpeedMultiplier = 1f;
            var mm = MatchManager.Instance;
            var def = GameDatabase.Instance != null ? GameDatabase.Instance.Vehicle(PhaseTwoId) : null;
            if (mm == null || def == null) return;
            var pos = V.transform.position + Vector3.up * 2.5f;
            var phase2 = mm.SpawnExtra(def, pos, Quaternion.Euler(0f, V.transform.eulerAngles.y, 0f), false, V.name + " Phase 2");
            Fx.Burst(pos, 8f, new Color(1f, 0.4f, 0.1f), 0.8f);
            BurnZone.Spawn(V.transform.position, 7f, 8f, phase2);
            TestLog.Write("boss", V.name, "phase-2", phase2 != null ? phase2.name : "-");
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            // Long ram lines: target ahead, far enough to build speed, straight path.
            if (!InFront(V, target, 12f, 60f)) { reason = "no-ram-line"; return false; }
            if (distance < 15f) { reason = "too-close-for-run"; return false; }
            reason = "long-ram-line";
            return true;
        }
    }
}
