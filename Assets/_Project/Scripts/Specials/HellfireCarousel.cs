using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Dread Jester phase 2: the burning carousel top as a slow, rotating gun turret. Fires a ring salvo
    /// plus an aimed volley on a fixed rhythm without input; activation is a double incendiary ring.
    /// </summary>
    public class HellfireCarousel : SpecialAbility
    {
        const float SalvoInterval = 2.6f;
        const float SpinDegPerSec = 45f;

        float _nextSalvo;
        int _salvo;

        public override void Init(Vehicle v)
        {
            base.Init(v);
            _nextSalvo = Time.time + 1.5f;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            V.Visual.Rotate(0f, SpinDegPerSec * dt, 0f, Space.Self);
            if (Time.time < _nextSalvo) return;
            _nextSalvo = Time.time + SalvoInterval;
            _salvo++;
            Ring(8, _salvo * 22.5f, false);
            var target = Targeting.Find(V, V.transform.position + Vector3.up * 2f, V.transform.forward, 180f, 70f, true);
            if (target != null) Aimed(target);
        }

        void Ring(int count, float offsetDeg, bool incendiary)
        {
            var origin = V.transform.position + Vector3.up * (V.Definition.bodySize.y + 1.2f);
            for (int i = 0; i < count; i++)
            {
                var dir = Quaternion.Euler(-12f, offsetDeg + i * 360f / count + V.Visual.eulerAngles.y, 0f) * Vector3.forward;
                var spec = new ProjectileSpec
                {
                    Behaviour = WeaponBehaviour.Rocket, Speed = 28f, Direct = 6f * Power, Splash = 6f * Power, Radius = 3f,
                    Gravity = 6f, Lifetime = 2.5f, Color = incendiary ? new Color(1f, 0.35f, 0.05f) : new Color(1f, 0.75f, 0.2f),
                    Scale = 0.5f, Cause = V.Definition.specialId, Owner = V, Impulse = 2f, Ignites = incendiary
                };
                Projectile.Launch(spec, origin + dir * 2f, dir * spec.Speed);
            }
        }

        void Aimed(Vehicle target)
        {
            var origin = V.transform.position + Vector3.up * (V.Definition.bodySize.y + 1.5f);
            var to = (Targeting.AimPoint(target) - origin).normalized;
            for (int i = -1; i <= 1; i++)
            {
                var dir = Quaternion.AngleAxis(i * 6f, Vector3.up) * to;
                var spec = new ProjectileSpec
                {
                    Behaviour = WeaponBehaviour.Rocket, Speed = 40f, Direct = 10f * Power, Splash = 6f * Power, Radius = 3f,
                    Homing = 35f, Lifetime = 2.5f, Color = new Color(1f, 0.2f, 0.4f), Scale = 0.55f,
                    Cause = V.Definition.specialId, Owner = V, Impulse = 2f, Target = target
                };
                Projectile.Launch(spec, origin + dir * 2.5f, dir * spec.Speed);
            }
        }

        public override bool TryActivate(out string reason)
        {
            Ring(16, 0f, true);
            Ring(16, 11.25f, true);
            reason = null;
            return true;
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (target == null || distance > 30f) { reason = "nobody-close"; return false; }
            reason = "target-in-ring";
            return true;
        }
    }
}
