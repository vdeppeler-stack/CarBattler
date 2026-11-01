using UnityEngine;

namespace IronSyndicate
{
    public static class Targeting
    {
        static int _losMask = -1;

        /// <summary>Static geometry only: vehicles, projectiles and pickups never block sight.</summary>
        public static int LosMask
        {
            get
            {
                if (_losMask == -1) _losMask = ~LayerMask.GetMask("Vehicle", "Projectile", "Pickup", "Ignore Raycast");
                return _losMask;
            }
        }

        public static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            return !Physics.Linecast(from, to, LosMask, QueryTriggerInteraction.Ignore);
        }

        public static Vector3 AimPoint(Vehicle v) => v.transform.position + Vector3.up * 0.6f;

        /// <summary>Best living, uncloaked enemy inside the cone, weighted by angle and distance.</summary>
        public static Vehicle Find(Vehicle owner, Vector3 origin, Vector3 dir, float coneDeg, float range, bool needLos)
        {
            Vehicle best = null;
            float bestScore = float.MaxValue;
            foreach (var v in Vehicle.All)
            {
                if (v == owner || !v.IsAlive || v.Status.IsCloaked) continue;
                var to = AimPoint(v) - origin;
                float d = to.magnitude;
                if (d > range) continue;
                if (v.Status.IsConcealed && d > VehicleStatus.ConcealRange) continue;
                float angle = Vector3.Angle(dir, to);
                if (angle > coneDeg) continue;
                if (needLos && !HasLineOfSight(origin, AimPoint(v))) continue;
                float score = d * (1f + angle / 20f);
                if (score < bestScore) { bestScore = score; best = v; }
            }
            return best;
        }
    }
}
