using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Base for vehicle specials. Charges live in <see cref="VehicleArsenal"/>; a special only consumes one
    /// when TryActivate succeeds. BotShouldUse encodes the profile's "use it / hold it" rule for the lab.
    /// </summary>
    public abstract class SpecialAbility : MonoBehaviour
    {
        protected Vehicle V;

        public virtual float RamMultiplier => 1f;
        public virtual bool IsActive => false;
        protected float Power => V.Definition.specialPower;

        public virtual void Init(Vehicle v) => V = v;

        public abstract bool TryActivate(out string reason);

        public abstract bool BotShouldUse(Vehicle target, float distance, bool lineOfSight, out string reason);

        public virtual void Step(in VehicleCommand c, float dt) { }
        public virtual void OnRam(Vehicle other, float closing) { }
        public virtual void OnOwnerDeath() { }

        protected void ReportHit(Vehicle target)
        {
            V.Stats.specialHits++;
            TestLog.Write("special", V.name, V.Definition.specialId + "-hit", target != null ? target.name : "-");
        }

        protected void Damage(Vehicle target, float amount, int hitId = 0)
        {
            target.ReceiveDamage(DamageEvent.Create(V, target, DamageType.Special, amount, target.transform.position, hitId, V.Definition.specialId));
        }

        protected Vehicle FindAhead(float coneDeg, float range) =>
            Targeting.Find(V, V.Muzzle.position, V.Muzzle.forward, coneDeg, range, true);

        protected static bool InFront(Vehicle self, Vehicle target, float coneDeg, float range)
        {
            if (target == null) return false;
            var to = target.transform.position - self.transform.position;
            return to.magnitude <= range && Vector3.Angle(self.transform.forward, to) <= coneDeg;
        }

        protected static bool Behind(Vehicle self, Vehicle target, float coneDeg, float range)
        {
            if (target == null) return false;
            var to = target.transform.position - self.transform.position;
            return to.magnitude <= range && Vector3.Angle(-self.transform.forward, to) <= coneDeg;
        }
    }
}
