using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Nocturne: lantern tethers the nearest visible enemy in range and drains HP into Nocturne.</summary>
    public class WraithLantern : SpecialAbility, GrabLink.IHolder
    {
        const float Range = 18f;
        const float Duration = 4f;
        const float TickInterval = 0.5f;
        const float Drain = 5f;

        Vehicle _target;
        float _until, _nextTick;

        public Vehicle Holder => V;
        public Vehicle Held => _target;
        public override bool IsActive => _target != null;

        public override bool TryActivate(out string reason)
        {
            var t = Targeting.Find(V, V.transform.position + Vector3.up, V.transform.forward, 180f, Range, true);
            if (t == null) { reason = "no-enemy-in-radius"; return false; }
            _target = t;
            _until = Time.time + Duration;
            _nextTick = Time.time + TickInterval;
            GrabLink.Register(this);
            ReportHit(t);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (_target == null) return;
            bool broken = Vector3.Distance(_target.transform.position, V.transform.position) > Range * 1.4f || _target.Status.IsCloaked;
            if (Time.time > _until || broken) { Release(broken ? "range" : "timeout"); return; }
            Fx.Tracer(V.transform.position + Vector3.up * 2.5f, _target.transform.position + Vector3.up, new Color(0.55f, 1f, 0.6f), 0.15f, Time.fixedDeltaTime * 1.5f);
            if (Time.time >= _nextTick)
            {
                _nextTick += TickInterval;
                float amount = Drain * Power;
                float before = _target.Hp;
                Damage(_target, amount);
                V.Heal(before - _target.Hp, "wraith-lantern");
            }
        }

        public void Release(string reason)
        {
            if (_target == null) return;
            _target = null;
            GrabLink.Unregister(this);
            TestLog.Write("special", V.name, "lantern-release", reason);
        }

        public override void OnOwnerDeath() => Release("death");

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (distance > Range * 0.85f) { reason = $"radius-empty d={distance:F0}"; return false; }
            if (!los) { reason = "no-los"; return false; }
            reason = "enemy-in-radius";
            return true;
        }
    }
}
