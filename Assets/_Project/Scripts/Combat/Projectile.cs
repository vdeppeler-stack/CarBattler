using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public struct ProjectileSpec
    {
        public WeaponBehaviour Behaviour;
        public float Speed, Direct, Splash, Radius, Homing, Gravity, Fuse, TriggerRadius, Lifetime, Impulse, Scale, SnareSeconds;
        public bool Ignites, Freezes, PassThroughWalls, RequiresLock;
        public Color Color;
        public string Cause;
        public Vehicle Owner;
        public Vehicle Target;

        public static ProjectileSpec From(WeaponDefinition w, Vehicle owner)
        {
            return new ProjectileSpec
            {
                Behaviour = w.behaviour,
                Speed = w.speed,
                Direct = w.directDamage,
                Splash = w.splashDamage,
                Radius = w.splashRadius,
                Homing = w.homingDegPerSec,
                Gravity = w.gravity,
                Fuse = w.fuseTime,
                TriggerRadius = w.triggerRadius,
                Lifetime = w.Lifetime,
                Impulse = 4f + w.splashRadius,
                Ignites = w.ignites,
                Freezes = w.freezes,
                RequiresLock = w.requiresLock,
                Color = w.color,
                Scale = w.visualScale,
                Cause = w.id,
                Owner = owner
            };
        }
    }

    /// <summary>Pooled projectile, moved by sphere casts each physics step (no Rigidbody).</summary>
    public class Projectile : MonoBehaviour
    {
        static readonly Stack<Projectile> Pool = new Stack<Projectile>();
        public static readonly List<Projectile> Active = new List<Projectile>();
        static readonly RaycastHit[] Hits = new RaycastHit[16];
        static readonly Collider[] Near = new Collider[16];
        static int _mask = -1;

        [System.NonSerialized] public ProjectileSpec Spec;
        Vector3 _velocity;
        float _age;
        bool _settled;
        bool _live;
        Renderer _renderer;

        public bool Settled => _settled;
        public Vector3 Velocity => _velocity;

        static int Mask
        {
            get
            {
                if (_mask == -1) _mask = ~LayerMask.GetMask("Projectile", "Pickup", "Ignore Raycast");
                return _mask;
            }
        }

        public static Projectile Launch(in ProjectileSpec spec, Vector3 position, Vector3 velocity)
        {
            Projectile p;
            if (Pool.Count > 0) p = Pool.Pop();
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Projectile";
                go.layer = LayerMask.NameToLayer("Projectile");
                Destroy(go.GetComponent<Collider>());
                p = go.AddComponent<Projectile>();
                p._renderer = go.GetComponent<Renderer>();
                p._renderer.sharedMaterial = new Material(Fx.Unlit);
            }
            p.gameObject.SetActive(true);
            p.Spec = spec;
            p.transform.position = position;
            p.transform.localScale = Vector3.one * Mathf.Max(0.15f, spec.Scale);
            p._renderer.sharedMaterial.SetColor("_BaseColor", spec.Color);
            p._velocity = velocity;
            p._age = 0f;
            p._settled = false;
            p._live = true;
            Active.Add(p);
            return p;
        }

        /// <summary>Remote charges (C4, lab detonators) call this on the owner's second press.</summary>
        public void Detonate() => Explode(transform.position, null);

        void FixedUpdate()
        {
            if (!_live) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;

            if (_settled)
            {
                if (Spec.Behaviour == WeaponBehaviour.Mine && _age > 0.6f) CheckProximity();
                if (_age > 90f) Despawn();
                return;
            }

            if (Spec.Homing > 0f && Spec.Target != null && Spec.Target.IsAlive && !(Spec.RequiresLock && Spec.Target.Status.IsCloaked))
            {
                var to = Spec.Target.transform.position + Vector3.up * 0.6f - transform.position;
                var desired = to.normalized * _velocity.magnitude;
                _velocity = Vector3.RotateTowards(_velocity, desired, Spec.Homing * Mathf.Deg2Rad * dt, 0f);
            }
            _velocity += Vector3.down * Spec.Gravity * dt;

            var step = _velocity * dt;
            float dist = step.magnitude;
            if (dist > 0.0001f && TryHit(transform.position, step / dist, dist, out var hit))
            {
                var vehicle = hit.rigidbody != null ? hit.rigidbody.GetComponent<Vehicle>() : null;
                if (vehicle != null && vehicle.IsAlive)
                {
                    if (Spec.Behaviour == WeaponBehaviour.Mine || Spec.Behaviour == WeaponBehaviour.Remote) Settle(hit.point + hit.normal * 0.2f);
                    else Explode(hit.point, vehicle);
                    return;
                }
                if (Spec.PassThroughWalls) { transform.position += step; return; }
                switch (Spec.Behaviour)
                {
                    case WeaponBehaviour.Bounce:
                        _velocity = Vector3.Reflect(_velocity, hit.normal);
                        transform.position = hit.point + hit.normal * 0.3f;
                        return;
                    case WeaponBehaviour.Mine:
                    case WeaponBehaviour.Remote:
                        Settle(hit.point + hit.normal * 0.2f);
                        return;
                    default:
                        Explode(hit.point, null);
                        return;
                }
            }
            transform.position += step;

            if (Spec.Behaviour == WeaponBehaviour.Bounce && Spec.Fuse > 0f && _age >= Spec.Fuse) { Explode(transform.position, null); return; }
            if (_age > Spec.Lifetime)
            {
                if (Spec.Behaviour == WeaponBehaviour.Lob) Explode(transform.position, null);
                else Despawn();
            }
        }

        bool TryHit(Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            int n = Physics.SphereCastNonAlloc(origin, 0.25f, dir, Hits, dist, Mask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (h.distance <= 0f && h.point == Vector3.zero) continue;
                var rb = h.rigidbody;
                if (rb != null && Spec.Owner != null && rb.gameObject == Spec.Owner.gameObject) continue;
                if (rb != null && rb.TryGetComponent<Vehicle>(out var v) && v.IsAlive && v.Status.IsPhased) continue;
                if (h.distance < bestDist) { bestDist = h.distance; best = h; found = true; }
            }
            return found;
        }

        void Settle(Vector3 at)
        {
            _settled = true;
            _velocity = Vector3.zero;
            transform.position = at;
        }

        void CheckProximity()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, Spec.TriggerRadius, Near, LayerMask.GetMask("Vehicle"), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var v = Near[i].attachedRigidbody != null ? Near[i].attachedRigidbody.GetComponent<Vehicle>() : null;
                if (v == null || !v.IsAlive || v == Spec.Owner) continue;
                Explode(transform.position, null);
                return;
            }
        }

        void Explode(Vector3 point, Vehicle direct)
        {
            if (!_live) return;
            if (direct != null && Spec.Direct > 0f)
            {
                direct.ReceiveDamage(DamageEvent.Create(Spec.Owner, direct, DamageType.Missile, Spec.Direct, point, 0, Spec.Cause));
                if (Spec.Ignites) direct.Status.Ignite(Spec.Owner);
                if (Spec.Freezes) direct.Status.Freeze(Spec.Owner);
                if (Spec.SnareSeconds > 0f) direct.Status.Snare(Spec.SnareSeconds);
                if (Spec.Owner != null && Spec.Cause == Spec.Owner.Definition.specialId) Spec.Owner.Stats.specialHits++;
                if (direct.Body != null) direct.Body.AddForce(_velocity.normalized * Spec.Impulse * 0.5f, ForceMode.VelocityChange);
            }
            if (Spec.Radius > 0f)
            {
                Explosion.Detonate(new ExplosionSpec
                {
                    Position = point,
                    Radius = Spec.Radius,
                    Damage = Spec.Splash,
                    Impulse = Spec.Impulse,
                    Ignites = Spec.Ignites,
                    Freezes = Spec.Freezes,
                    Type = DamageType.Explosion,
                    Cause = Spec.Cause,
                    Source = Spec.Owner,
                    Exclude = direct
                });
            }
            else Fx.Burst(point, 1f, Spec.Color, 0.2f);

            if (Spec.Behaviour == WeaponBehaviour.Lob && Spec.Ignites) BurnZone.Spawn(point, Spec.Radius, 4f, Spec.Owner);
            Despawn();
        }

        public void Despawn()
        {
            if (!_live) return;
            _live = false;
            Active.Remove(this);
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        public static void ResetPools()
        {
            Pool.Clear();
            Active.Clear();
        }

        /// <summary>Round restart clears every live projectile.</summary>
        public static void DespawnAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] != null) Active[i].Despawn();
        }
    }
}
