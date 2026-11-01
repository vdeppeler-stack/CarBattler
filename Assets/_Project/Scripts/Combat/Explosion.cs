using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public struct ExplosionSpec
    {
        public Vector3 Position;
        public float Radius;
        public float Damage;
        public float Impulse;
        public bool Ignites;
        public bool Freezes;
        public DamageType Type;
        public string Cause;
        public Vehicle Source;
        /// <summary>Vehicle hit directly; it already took the direct damage and gets no splash.</summary>
        public Vehicle Exclude;
    }

    public static class Explosion
    {
        static readonly Collider[] Hits = new Collider[64];
        static readonly HashSet<IDamageable> Seen = new HashSet<IDamageable>();

        /// <summary>Damages every vehicle or destructible in range exactly once, with linear falloff to 50% at the edge.</summary>
        public static void Detonate(in ExplosionSpec spec)
        {
            int hitId = DamageEvent.NewHitId();
            int count = Physics.OverlapSphereNonAlloc(spec.Position, spec.Radius, Hits, ~0, QueryTriggerInteraction.Ignore);
            Seen.Clear();
            for (int i = 0; i < count; i++)
            {
                var col = Hits[i];
                var damageable = col.attachedRigidbody != null
                    ? col.attachedRigidbody.GetComponent<IDamageable>()
                    : col.GetComponentInParent<IDamageable>();
                if (damageable == null || !Seen.Add(damageable)) continue;

                var closest = col.ClosestPoint(spec.Position);
                float t = Mathf.Clamp01(Vector3.Distance(closest, spec.Position) / Mathf.Max(0.01f, spec.Radius));
                float amount = spec.Damage * Mathf.Lerp(1f, 0.5f, t);

                if (damageable is Vehicle v)
                {
                    if (v == spec.Exclude || !v.IsAlive) continue;
                    v.ReceiveDamage(DamageEvent.Create(spec.Source, v, spec.Type, amount, closest, hitId, spec.Cause));
                    if (spec.Impulse > 0f && v.Body != null)
                        v.Body.AddExplosionForce(spec.Impulse * v.Body.mass, spec.Position, spec.Radius * 1.5f, 0.6f, ForceMode.Impulse);
                    if (spec.Ignites) v.Status.Ignite(spec.Source);
                    if (spec.Freezes) v.Status.Freeze(spec.Source);
                }
                else
                {
                    damageable.ReceiveDamage(DamageEvent.Create(spec.Source, null, spec.Type, amount, closest, hitId, spec.Cause));
                }
            }
            Fx.Burst(spec.Position, spec.Radius, spec.Ignites ? new Color(1f, 0.45f, 0.1f) : spec.Freezes ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.75f, 0.3f));
        }
    }
}
