using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Jester Van: fan of five lightly homing fireworks rockets.</summary>
    public class FireworksFinale : SpecialAbility
    {
        static readonly Color[] Colors =
        {
            new Color(1f, 0.2f, 0.6f), new Color(1f, 0.9f, 0.2f), new Color(0.3f, 1f, 0.4f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.5f, 0.1f)
        };

        public override bool TryActivate(out string reason)
        {
            var m = V.Muzzle;
            var target = FindAhead(45f, 70f);
            for (int i = 0; i < 5; i++)
            {
                var dir = Quaternion.AngleAxis((i - 2) * 12f, Vector3.up) * Quaternion.AngleAxis(-8f, m.right) * m.forward;
                var spec = new ProjectileSpec
                {
                    Behaviour = WeaponBehaviour.Rocket, Speed = 42f, Direct = 8f * Power, Splash = 7f * Power, Radius = 3.5f,
                    Homing = 45f, Lifetime = 2.2f, Color = Colors[i], Scale = 0.45f, Cause = V.Definition.specialId, Owner = V,
                    Impulse = 3f, Target = target
                };
                Projectile.Launch(spec, m.position + m.right * (i - 2) * 0.4f, dir * spec.Speed);
            }
            reason = null;
            return true;
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-los"; return false; }
            if (distance < 12f) { reason = "too-close-realign"; return false; }
            if (!InFront(V, target, 30f, 60f)) { reason = "no-arc"; return false; }
            reason = "mid-range-arc";
            return true;
        }
    }
}
