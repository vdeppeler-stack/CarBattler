using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Goliath Wheel: straight charge; everything in the path is damaged once and shoved aside.</summary>
    public class GyroCrush : SpecialAbility
    {
        const float Duration = 1.4f;
        const float HitDamage = 28f;
        static readonly Collider[] Hits = new Collider[16];

        float _until;
        readonly HashSet<Vehicle> _hit = new HashSet<Vehicle>();

        public override bool IsActive => Time.time < _until;

        public override bool TryActivate(out string reason)
        {
            if (!V.Motor.Grounded) { reason = "airborne"; return false; }
            _until = Time.time + Duration;
            _hit.Clear();
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (!IsActive) return;
            var fwd = V.transform.forward;
            float target = V.Definition.topSpeed * 1.25f;
            var vel = V.Body.linearVelocity;
            float along = Vector3.Dot(vel, fwd);
            if (along < target) V.Body.AddForce(fwd * (target - along) * 0.25f, ForceMode.VelocityChange);

            var center = V.transform.position + fwd * (V.Definition.bodySize.z * 0.6f) + Vector3.up;
            int n = Physics.OverlapBoxNonAlloc(center, new Vector3(V.Definition.bodySize.x * 0.8f, 1.2f, 1.5f), Hits, V.transform.rotation, LayerMask.GetMask("Vehicle"));
            for (int i = 0; i < n; i++)
            {
                var other = Vehicle.Of(Hits[i].attachedRigidbody);
                if (other == null || other == V || !other.IsAlive || !_hit.Add(other)) continue;
                Damage(other, HitDamage * Power);
                var side = Vector3.Dot(other.transform.position - V.transform.position, V.transform.right) >= 0f ? V.transform.right : -V.transform.right;
                other.Body.AddForce((side * 12f + Vector3.up * 4f), ForceMode.VelocityChange);
                ReportHit(other);
            }
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-los"; return false; }
            if (!InFront(V, target, 12f, 22f)) { reason = "target-not-in-charge-lane"; return false; }
            reason = $"charge-lane d={distance:F0}";
            return true;
        }
    }
}
