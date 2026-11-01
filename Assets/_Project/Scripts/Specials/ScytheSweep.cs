using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Bonechaser: side blades for 3 s; damages vehicles on either flank while passing.</summary>
    public class ScytheSweep : SpecialAbility
    {
        const float Duration = 3f;
        const float Reach = 3f;
        const float HitDamage = 9f;
        const float RehitDelay = 0.5f;
        static readonly Collider[] Hits = new Collider[16];

        float _until;
        readonly Dictionary<Vehicle, float> _lastHit = new Dictionary<Vehicle, float>();
        GameObject _blades;

        public override bool IsActive => Time.time < _until;

        public override bool TryActivate(out string reason)
        {
            _until = Time.time + Duration;
            _lastHit.Clear();
            if (_blades == null)
            {
                _blades = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(_blades.GetComponent<Collider>());
                _blades.transform.SetParent(V.Visual, false);
                _blades.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                _blades.transform.localScale = new Vector3(Reach * 2f + V.Definition.bodySize.x, 0.08f, 0.8f);
                _blades.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(new Color(0.85f, 0.85f, 0.9f));
            }
            _blades.SetActive(true);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (_blades != null && _blades.activeSelf != IsActive) _blades.SetActive(IsActive);
            if (!IsActive) return;
            var half = new Vector3(Reach + V.Definition.bodySize.x * 0.5f, 1f, 2f);
            int n = Physics.OverlapBoxNonAlloc(V.transform.position + Vector3.up, half, Hits, V.transform.rotation, LayerMask.GetMask("Vehicle"));
            for (int i = 0; i < n; i++)
            {
                var other = Vehicle.Of(Hits[i].attachedRigidbody);
                if (other == null || other == V || !other.IsAlive) continue;
                if (_lastHit.TryGetValue(other, out var last) && Time.time - last < RehitDelay) continue;
                if (!_lastHit.ContainsKey(other)) ReportHit(other);
                _lastHit[other] = Time.time;
                Damage(other, HitDamage * Power);
            }
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (target == null || distance > 9f) { reason = "nobody-alongside"; return false; }
            var local = V.transform.InverseTransformPoint(target.transform.position);
            if (Mathf.Abs(local.z) > 4f) { reason = "not-on-flank"; return false; }
            reason = "target-on-flank";
            return true;
        }
    }
}
