using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Bastion MK-IV: marks a zone ahead; after 1.5 s six shells land in it.</summary>
    public class MortarGrid : SpecialAbility
    {
        const float Delay = 1.5f;
        const int Shells = 6;
        const float ZoneRadius = 6f;
        const float ShellDamage = 14f;
        const float MinRange = 25f, MaxRange = 90f;

        readonly List<(Vector3 pos, float at)> _pending = new List<(Vector3, float)>();
        GameObject _marker;

        public override bool IsActive => _pending.Count > 0;

        public override bool TryActivate(out string reason)
        {
            if (_pending.Count > 0) { reason = "barrage-in-flight"; return false; }
            var target = FindAhead(60f, MaxRange);
            Vector3 center;
            if (target != null && Vector3.Distance(target.transform.position, V.transform.position) >= MinRange)
                center = target.transform.position + target.Body.linearVelocity * Delay * 0.8f;
            else
                center = V.transform.position + V.transform.forward * 40f;
            if (Physics.Raycast(center + Vector3.up * 30f, Vector3.down, out var hit, 80f, Targeting.LosMask, QueryTriggerInteraction.Ignore))
                center = hit.point;

            for (int i = 0; i < Shells; i++)
            {
                var off = Random.insideUnitCircle * ZoneRadius;
                _pending.Add((center + new Vector3(off.x, 0f, off.y), Time.time + Delay + i * 0.1f));
            }
            _marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(_marker.GetComponent<Collider>());
            _marker.transform.position = center + Vector3.up * 0.05f;
            _marker.transform.localScale = new Vector3(ZoneRadius * 2f, 0.03f, ZoneRadius * 2f);
            _marker.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(new Color(1f, 0.15f, 0.1f));
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < _pending[i].at) continue;
                Explosion.Detonate(new ExplosionSpec
                {
                    Position = _pending[i].pos, Radius = 3.5f, Damage = ShellDamage * Power, Impulse = 5f,
                    Type = DamageType.Special, Cause = V.Definition.specialId, Source = V
                });
                _pending.RemoveAt(i);
            }
            if (_pending.Count == 0 && _marker != null) Destroy(_marker);
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-tracking"; return false; }
            if (distance < MinRange) { reason = "too-close-reposition"; return false; }
            if (distance > MaxRange) { reason = "too-far"; return false; }
            if (!InFront(V, target, 60f, MaxRange)) { reason = "not-ahead"; return false; }
            reason = $"mid-range d={distance:F0}";
            return true;
        }
    }
}
