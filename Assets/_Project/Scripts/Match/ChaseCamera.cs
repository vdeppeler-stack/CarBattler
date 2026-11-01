using UnityEngine;
using UnityEngine.InputSystem;

namespace IronSyndicate
{
    /// <summary>Chase camera for one seat; reads only the followed vehicle's state. R / Start+Down toggles rear view.</summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Vehicle target;
        public Vehicle spectate;
        public float distance = 10f;
        public float height = 4.2f;
        public float lookAhead = 4f;
        public float followSharpness = 6f;

        bool _rearView;

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.rKey.wasPressedThisFrame) || (pad != null && pad.startButton.isPressed && pad.dpad.down.wasPressedThisFrame))
                _rearView = !_rearView;
        }

        void LateUpdate()
        {
            var t = target != null && target.IsAlive ? target : spectate != null ? spectate : target;
            if (t == null) return;

            var fwd = Vector3.ProjectOnPlane(t.transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            if (_rearView) fwd = -fwd;
            var desired = t.transform.position - fwd * distance + Vector3.up * height;
            var focus = t.transform.position + fwd * lookAhead + Vector3.up * 1.2f;

            // Pull in when geometry blocks the view.
            var dir = desired - focus;
            if (Physics.SphereCast(focus, 0.3f, dir.normalized, out var hit, dir.magnitude, Targeting.LosMask, QueryTriggerInteraction.Ignore))
                desired = focus + dir.normalized * Mathf.Max(1.5f, hit.distance - 0.2f);

            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), k * 1.5f);
        }
    }
}
