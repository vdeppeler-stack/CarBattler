using UnityEngine;

namespace IronSyndicate
{
    public enum DamageType { MachineGun, Missile, Explosion, Ram, Burn, Special, Hazard, Fall, Tesla }

    public struct DamageEvent
    {
        public Vehicle Source;
        public Vehicle Target;
        public float Time;
        public DamageType Type;
        public float Amount;
        public int HitId;
        public Vector3 Point;
        public string Cause;

        static int _nextHitId = 1;

        /// <summary>One id per damaging occurrence; an explosion shares it across every collider it overlaps.</summary>
        public static int NewHitId() => _nextHitId++;

        public static DamageEvent Create(Vehicle source, Vehicle target, DamageType type, float amount, Vector3 point, int hitId = 0, string cause = null)
        {
            return new DamageEvent
            {
                Source = source,
                Target = target,
                Time = UnityEngine.Time.time,
                Type = type,
                Amount = amount,
                HitId = hitId != 0 ? hitId : NewHitId(),
                Point = point,
                Cause = cause ?? type.ToString()
            };
        }
    }

    public interface IDamageable
    {
        void ReceiveDamage(in DamageEvent e);
    }
}
