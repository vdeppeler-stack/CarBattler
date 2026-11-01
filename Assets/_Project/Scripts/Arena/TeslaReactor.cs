using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Arena reactor used by the Tesla Arc pickup: the shooter must be inside the radius; the discharge
    /// hits every vehicle inside, shooter included.
    /// </summary>
    public class TeslaReactor : MonoBehaviour
    {
        public static readonly List<TeslaReactor> All = new List<TeslaReactor>();

        public float radius = 22f;
        public float damage = 45f;
        public float cooldown = 4f;

        float _readyAt;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static bool TryDischarge(Vehicle shooter)
        {
            foreach (var r in All)
            {
                if (Vector3.Distance(r.transform.position, shooter.transform.position) > r.radius) continue;
                if (Time.time < r._readyAt) return false;
                r.Discharge(shooter);
                return true;
            }
            return false;
        }

        void Discharge(Vehicle shooter)
        {
            _readyAt = Time.time + cooldown;
            int hitId = DamageEvent.NewHitId();
            var top = transform.position + Vector3.up * 6f;
            foreach (var v in Vehicle.All.ToArray())
            {
                if (!v.IsAlive || Vector3.Distance(transform.position, v.transform.position) > radius) continue;
                v.ReceiveDamage(DamageEvent.Create(shooter, v, DamageType.Tesla, damage, v.transform.position, hitId, "tesla_arc"));
                v.Body.AddForce(Vector3.up * 6f, ForceMode.VelocityChange);
                Fx.Tracer(top, v.transform.position, new Color(0.6f, 0.8f, 1f), 0.25f, 0.25f);
            }
            Fx.Burst(top, 3f, new Color(0.6f, 0.8f, 1f), 0.4f);
            TestLog.Write("arena", name, "tesla-discharge", shooter.name);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
