using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Rustfang: harpoon hooks a target ahead and tows it; wall impacts while towed hurt extra.</summary>
    public class ChainHarpoon : SpecialAbility, GrabLink.IHolder
    {
        const float Range = 30f;
        const float Duration = 3f;
        const float TowDistance = 9f;
        const float TickDamage = 3f;
        const float WallDamage = 14f;

        Vehicle _held;
        float _until, _nextTick, _wallCooldown;

        public Vehicle Holder => V;
        public Vehicle Held => _held;
        public override bool IsActive => _held != null;

        public override bool TryActivate(out string reason)
        {
            var t = FindAhead(20f, Range);
            if (t == null) { reason = "no-target-in-line"; return false; }
            if (GrabLink.IsHeld(t)) { reason = "target-already-held"; return false; }
            _held = t;
            _until = Time.time + Duration;
            _nextTick = Time.time + 0.5f;
            GrabLink.Register(this);
            ReportHit(t);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (_held == null) return;
            if (Time.time > _until) { Release("timeout"); return; }

            var anchor = V.RearMuzzle.position + V.RearMuzzle.forward * TowDistance;
            var delta = anchor - _held.transform.position;
            if (delta.magnitude > 2f)
            {
                var pull = delta.normalized * Mathf.Min(delta.magnitude * 3f, 30f);
                _held.Body.AddForce((pull - _held.Body.linearVelocity) * 0.15f, ForceMode.VelocityChange);
            }
            Fx.Tracer(V.RearMuzzle.position, _held.transform.position + Vector3.up, new Color(0.5f, 0.5f, 0.5f), 0.12f, Time.fixedDeltaTime * 1.5f);

            if (Time.time >= _nextTick)
            {
                _nextTick += 0.5f;
                Damage(_held, TickDamage * Power);
            }
            if (Time.time >= _wallCooldown)
            {
                var vel = _held.Body.linearVelocity;
                if (vel.magnitude > 6f && Physics.Raycast(_held.transform.position + Vector3.up, vel.normalized, _held.Definition.bodySize.z * 0.5f + 1f, Targeting.LosMask, QueryTriggerInteraction.Ignore))
                {
                    _wallCooldown = Time.time + 0.6f;
                    Damage(_held, WallDamage * Power);
                    TestLog.Write("special", V.name, "harpoon-wall-hit", _held.name);
                }
            }
        }

        public void Release(string reason)
        {
            if (_held == null) return;
            _held = null;
            GrabLink.Unregister(this);
            TestLog.Write("special", V.name, "harpoon-release", reason);
        }

        public override void OnOwnerDeath() => Release("death");

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!los) { reason = "no-los"; return false; }
            if (!InFront(V, target, 18f, Range)) { reason = "not-in-line"; return false; }
            reason = "hook-target";
            return true;
        }
    }
}
