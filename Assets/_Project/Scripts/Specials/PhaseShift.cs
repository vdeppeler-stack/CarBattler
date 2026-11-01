using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Phase GT: 1.5 s intangible to vehicles and projectiles; re-materialising releases a shock.</summary>
    public class PhaseShift : SpecialAbility
    {
        const float Duration = 1.5f;
        const float ShockRadius = 6f;
        const float ShockDamage = 22f;

        float _until;
        bool _phased;
        readonly List<(Collider a, Collider b)> _ignored = new List<(Collider, Collider)>();

        public override bool IsActive => _phased;

        public override bool TryActivate(out string reason)
        {
            _until = Time.time + Duration;
            _phased = true;
            V.Status.Phase(Duration);
            var mine = V.GetComponentsInChildren<Collider>();
            foreach (var other in Vehicle.All)
            {
                if (other == V) continue;
                foreach (var oc in other.GetComponentsInChildren<Collider>())
                foreach (var mc in mine)
                {
                    Physics.IgnoreCollision(mc, oc, true);
                    _ignored.Add((mc, oc));
                }
            }
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (!_phased) return;
            bool blink = Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
            V.Visual.gameObject.SetActive(blink);
            if (Time.time < _until) return;
            End(true);
        }

        void End(bool shock)
        {
            _phased = false;
            V.Visual.gameObject.SetActive(true);
            foreach (var (a, b) in _ignored)
                if (a != null && b != null) Physics.IgnoreCollision(a, b, false);
            _ignored.Clear();
            if (!shock) return;
            foreach (var other in Vehicle.All)
                if (other != V && other.IsAlive && Vector3.Distance(other.transform.position, V.transform.position) < ShockRadius) ReportHit(other);
            Explosion.Detonate(new ExplosionSpec
            {
                Position = V.transform.position + Vector3.up, Radius = ShockRadius, Damage = ShockDamage * Power, Impulse = 6f,
                Type = DamageType.Special, Cause = V.Definition.specialId, Source = V, Exclude = V
            });
        }

        public override void OnOwnerDeath()
        {
            if (_phased) End(false);
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            foreach (var p in Projectile.Active)
            {
                if (p.Spec.Owner == V || p.Spec.Target != V) continue;
                if (Vector3.Distance(p.transform.position, V.transform.position) < 15f) { reason = "dodge-incoming"; return true; }
            }
            if (distance < ShockRadius * 0.8f) { reason = "shock-in-contact"; return true; }
            reason = "no-threat";
            return false;
        }
    }
}
