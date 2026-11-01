using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Iron Jaw: the shovel grabs a vehicle in front and pushes it. Damage ticks rise while the target is
    /// pressed against geometry. Released on timeout or on either vehicle's death.
    /// </summary>
    public class WallPress : SpecialAbility, GrabLink.IHolder
    {
        const float Duration = 2.5f;
        const float TickInterval = 0.25f;
        const float BaseTick = 3f;
        const float PinnedTick = 11f;

        Vehicle _held;
        float _until, _nextTick;

        public Vehicle Holder => V;
        public Vehicle Held => _held;
        public override bool IsActive => _held != null;

        public override bool TryActivate(out string reason)
        {
            var target = FindAhead(30f, 6f + V.Definition.bodySize.z * 0.5f);
            if (target == null) { reason = "nothing-in-shovel"; return false; }
            if (GrabLink.IsHeld(target)) { reason = "target-already-held"; return false; }
            _held = target;
            _until = Time.time + Duration;
            _nextTick = Time.time + TickInterval;
            GrabLink.Register(this);
            ReportHit(target);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (_held == null) return;
            if (Time.time > _until) { Release("timeout"); return; }

            var fwd = V.transform.forward;
            var hold = V.transform.position + fwd * (V.Definition.bodySize.z * 0.5f + _held.Definition.bodySize.z * 0.5f + 0.6f) + Vector3.up * 0.8f;
            var delta = hold - _held.transform.position;
            _held.Body.linearVelocity = V.Body.linearVelocity + delta * 8f;
            V.Body.AddForce(fwd * V.Definition.acceleration * 0.6f, ForceMode.Acceleration);

            if (Time.time >= _nextTick)
            {
                _nextTick += TickInterval;
                bool pinned = Physics.Raycast(_held.transform.position + Vector3.up, fwd, _held.Definition.bodySize.z * 0.5f + 1.2f, Targeting.LosMask, QueryTriggerInteraction.Ignore);
                Damage(_held, (pinned ? PinnedTick : BaseTick) * Power);
            }
        }

        public void Release(string reason)
        {
            if (_held == null) return;
            var held = _held;
            _held = null;
            GrabLink.Unregister(this);
            if (held.IsAlive && reason != "death") held.Body.AddForce(V.transform.forward * 10f + Vector3.up * 5f, ForceMode.VelocityChange);
            TestLog.Write("special", V.name, "wall-press-release", reason);
        }

        public override void OnOwnerDeath() => Release("death");

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (!InFront(V, target, 25f, 6f + V.Definition.bodySize.z * 0.5f)) { reason = distance < 15f ? "align-shovel" : "too-far"; return false; }
            reason = "target-in-shovel";
            return true;
        }
    }
}
