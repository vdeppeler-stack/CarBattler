using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Interceptor V8: homing net; the target loses grip and cannot ignite nitro for a while.</summary>
    public class SpikeNet : SpecialAbility
    {
        public override bool TryActivate(out string reason)
        {
            var m = V.Muzzle;
            var spec = new ProjectileSpec
            {
                Behaviour = WeaponBehaviour.Rocket, Speed = 55f, Direct = 12f * Power, Homing = 90f, Lifetime = 1.4f,
                SnareSeconds = 3f, Color = new Color(0.2f, 0.4f, 1f), Scale = 0.9f, Cause = V.Definition.specialId, Owner = V, Impulse = 1f,
                Target = FindAhead(35f, 70f)
            };
            Projectile.Launch(spec, m.position, m.forward * spec.Speed + Vector3.Project(V.Body.linearVelocity, m.forward));
            reason = null;
            return true;
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-los"; return false; }
            if (!InFront(V, target, 30f, 45f)) { reason = "not-alongside-yet"; return false; }
            if (target.Status.IsSnared) { reason = "already-snared"; return false; }
            reason = "close-pursuit";
            return true;
        }
    }
}
