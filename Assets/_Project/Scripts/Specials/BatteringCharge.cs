using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Mammoth 4x4: ram shield for 4 s; the next impact deals heavy speed-scaled damage and tips the target.</summary>
    public class BatteringCharge : SpecialAbility
    {
        const float Duration = 4f;

        float _until;
        GameObject _plow;

        public override bool IsActive => Time.time < _until;
        public override float RamMultiplier => IsActive ? 1f + 2.5f * Power : 1f;

        public override bool TryActivate(out string reason)
        {
            _until = Time.time + Duration;
            if (_plow == null)
            {
                _plow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(_plow.GetComponent<Collider>());
                _plow.transform.SetParent(V.Visual, false);
                var s = V.Definition.bodySize;
                _plow.transform.localPosition = new Vector3(0f, s.y * 0.6f + V.Definition.wheelRadius, s.z * 0.5f + 0.3f);
                _plow.transform.localScale = new Vector3(s.x * 1.15f, s.y * 0.8f, 0.3f);
                _plow.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(new Color(1f, 0.55f, 0f));
            }
            _plow.SetActive(true);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (_plow != null && !IsActive && _plow.activeSelf) _plow.SetActive(false);
        }

        public override void OnRam(Vehicle other, float closing)
        {
            if (!IsActive) return;
            var away = (other.transform.position - V.transform.position).normalized;
            other.Body.AddForceAtPosition((away * 6f + Vector3.up * 7f) * other.Body.mass, other.transform.position + Vector3.up * 1.5f, ForceMode.Impulse);
            ReportHit(other);
            _until = Time.time + 0.05f;
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!InFront(V, target, 15f, 25f)) { reason = "no-ram-line"; return false; }
            if (V.Motor.ForwardSpeed < 8f) { reason = "build-speed-first"; return false; }
            reason = "ram-line";
            return true;
        }
    }
}
