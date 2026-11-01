using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Leviathan APC: plants stabilisers (immobile), charges, then fires a long-range rail beam.</summary>
    public class SiegeLock : SpecialAbility
    {
        const float ChargeTime = 1.2f;
        const float LockTime = 2.2f;
        const float Range = 130f;
        const float HitDamage = 45f;

        float _start;
        bool _active, _fired;

        public override bool IsActive => _active;

        public override bool TryActivate(out string reason)
        {
            if (!V.Motor.Grounded) { reason = "airborne"; return false; }
            _active = true;
            _fired = false;
            _start = Time.time;
            V.Motor.SpeedMultiplier = 0f;
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (!_active) return;
            var vel = V.Body.linearVelocity;
            V.Body.linearVelocity = new Vector3(vel.x * 0.8f, vel.y, vel.z * 0.8f);
            float t = Time.time - _start;
            var m = V.Muzzle;
            if (!_fired)
            {
                Fx.Tracer(m.position, m.position + m.forward * 4f * (t / ChargeTime), new Color(0.9f, 0.2f, 1f), 0.3f, Time.fixedDeltaTime * 1.5f);
                if (t >= ChargeTime) Fire(m);
            }
            if (t >= LockTime) End();
        }

        void Fire(Transform m)
        {
            _fired = true;
            var target = FindAhead(15f, Range);
            var dir = target != null ? (Targeting.AimPoint(target) - m.position).normalized : m.forward;
            var end = m.position + dir * Range;
            if (Physics.Raycast(m.position, dir, out var hit, Range, ~LayerMask.GetMask("Projectile", "Pickup", "Ignore Raycast"), QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var v = hit.rigidbody != null ? hit.rigidbody.GetComponent<Vehicle>() : null;
                if (v != null && v != V && v.IsAlive)
                {
                    Damage(v, HitDamage * Power);
                    v.Body.AddForce(dir * 8f, ForceMode.VelocityChange);
                    ReportHit(v);
                }
            }
            Fx.Tracer(m.position, end, new Color(0.9f, 0.3f, 1f), 0.6f, 0.25f);
        }

        void End()
        {
            _active = false;
            V.Motor.SpeedMultiplier = 1f;
        }

        public override void OnOwnerDeath() => End();

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-los"; return false; }
            if (distance < 25f) { reason = "too-close-for-siege"; return false; }
            if (!InFront(V, target, 15f, Range)) { reason = "not-aligned"; return false; }
            reason = $"long-range d={distance:F0}";
            return true;
        }
    }
}
