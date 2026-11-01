using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Chrome Igniter: rear-facing exhaust flame; ignites and burns pursuers, leaves a fire trail.</summary>
    public class ExhaustInferno : SpecialAbility
    {
        const float Duration = 2.5f;
        const float Range = 12f;
        const float Cone = 35f;
        const float TickDamage = 3f;

        float _until, _nextTick, _nextTrail;

        public override bool IsActive => Time.time < _until;

        public override bool TryActivate(out string reason)
        {
            _until = Time.time + Duration;
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (!IsActive) return;
            var origin = V.RearMuzzle.position;
            var back = V.RearMuzzle.forward;
            Fx.Tracer(origin, origin + back * Range, new Color(1f, 0.45f, 0.05f), 1.2f, Time.fixedDeltaTime * 1.5f);
            if (Time.time >= _nextTrail)
            {
                _nextTrail = Time.time + 0.4f;
                BurnZone.Spawn(origin + back * 2f + Vector3.down * 0.8f, 1.8f, 3f, V);
            }
            if (Time.time < _nextTick) return;
            _nextTick = Time.time + 0.1f;
            foreach (var other in Vehicle.All)
            {
                if (other == V || !other.IsAlive) continue;
                var to = other.transform.position - origin;
                if (to.magnitude > Range || Vector3.Angle(back, to) > Cone) continue;
                if (!Targeting.HasLineOfSight(origin, Targeting.AimPoint(other))) continue;
                if (!other.Status.IsBurning) ReportHit(other);
                other.Status.Ignite(V);
                Damage(other, TickDamage * Power);
            }
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!Behind(V, target, Cone, Range)) { reason = "nobody-behind"; return false; }
            reason = "pursuer-in-flame";
            return true;
        }
    }
}
