using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Vortex F1: short burst of speed leaving an energy wake that damages vehicles crossing it.</summary>
    public class SlipstreamLance : SpecialAbility
    {
        const float DashTime = 0.7f;
        const float WakeLife = 3f;
        const float WakeWidth = 1.6f;
        const float HitDamage = 20f;

        float _dashUntil, _nextPoint;
        readonly List<(Vector3 p, float until)> _wake = new List<(Vector3, float)>();
        readonly HashSet<Vehicle> _hit = new HashSet<Vehicle>();
        LineRenderer _line;

        public override bool IsActive => Time.time < _dashUntil || _wake.Count > 0;

        public override bool TryActivate(out string reason)
        {
            if (!V.Motor.Grounded) { reason = "airborne"; return false; }
            _dashUntil = Time.time + DashTime;
            _hit.Clear();
            _wake.Clear();
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (Time.time < _dashUntil)
            {
                var fwd = V.transform.forward;
                float target = V.Definition.topSpeed * 1.6f;
                float along = Vector3.Dot(V.Body.linearVelocity, fwd);
                if (along < target) V.Body.AddForce(fwd * (target - along) * 0.35f, ForceMode.VelocityChange);
                if (Time.time >= _nextPoint)
                {
                    _nextPoint = Time.time + 0.05f;
                    _wake.Add((V.RearMuzzle.position, Time.time + WakeLife));
                }
            }
            for (int i = _wake.Count - 1; i >= 0; i--)
                if (Time.time > _wake[i].until) _wake.RemoveAt(i);

            DrawWake();
            if (_wake.Count < 2) return;
            foreach (var other in Vehicle.All)
            {
                if (other == V || !other.IsAlive || _hit.Contains(other)) continue;
                var p = other.transform.position;
                for (int i = 1; i < _wake.Count; i++)
                {
                    var a = _wake[i - 1].p;
                    var b = _wake[i].p;
                    var closest = a + Vector3.Project(p - a, b - a);
                    if ((closest - a).sqrMagnitude > (b - a).sqrMagnitude || Vector3.Dot(closest - a, b - a) < 0f) continue;
                    if (Vector3.Distance(closest, p) > WakeWidth + other.Definition.bodySize.x * 0.5f) continue;
                    _hit.Add(other);
                    Damage(other, HitDamage * Power);
                    ReportHit(other);
                    break;
                }
            }
        }

        void DrawWake()
        {
            if (_line == null)
            {
                _line = new GameObject("SlipstreamWake").AddComponent<LineRenderer>();
                _line.sharedMaterial = Fx.UnlitColor(new Color(0.3f, 1f, 1f));
                _line.startWidth = _line.endWidth = WakeWidth;
            }
            _line.positionCount = _wake.Count;
            for (int i = 0; i < _wake.Count; i++) _line.SetPosition(i, _wake[i].p);
        }

        void OnDestroy()
        {
            if (_line != null) Destroy(_line.gameObject);
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (V.HpFraction < 0.3f && distance < 25f) { reason = "escape"; return true; }
            if (!InFront(V, target, 35f, 30f)) { reason = "no-crossing-target"; return false; }
            reason = "cut-across-target";
            return true;
        }
    }
}
