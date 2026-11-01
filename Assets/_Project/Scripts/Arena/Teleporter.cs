using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>One-way pad: a vehicle entering the trigger leaves at <see cref="exit"/> facing its forward, speed kept.</summary>
    [RequireComponent(typeof(Collider))]
    public class Teleporter : MonoBehaviour
    {
        public Transform exit;
        public float cooldown = 2f;
        public float minExitSpeed = 6f;

        // Shared by all pads: arriving on another pad must not bounce the vehicle straight back.
        static readonly Dictionary<Vehicle, float> _readyAt = new Dictionary<Vehicle, float>();
        public static void ResetStatics() => _readyAt.Clear();

        public int Uses { get; private set; }

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        void OnTriggerEnter(Collider other)
        {
            if (exit == null) return;
            var v = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<Vehicle>() : null;
            if (v == null || !v.IsAlive) return;
            if (_readyAt.TryGetValue(v, out var t) && Time.time < t) return;
            _readyAt[v] = Time.time + cooldown;
            Uses++;

            float speed = Mathf.Max(minExitSpeed, v.Body.linearVelocity.magnitude);
            var rot = Quaternion.Euler(0f, exit.eulerAngles.y, 0f);
            v.Body.position = exit.position;
            v.Body.rotation = rot;
            v.transform.SetPositionAndRotation(exit.position, rot);
            v.Body.linearVelocity = rot * Vector3.forward * speed;
            v.Body.angularVelocity = Vector3.zero;
            Fx.Burst(transform.position + Vector3.up, 3f, new Color(0.7f, 0.4f, 1f), 0.4f);
            Fx.Burst(exit.position + Vector3.up, 3f, new Color(0.7f, 0.4f, 1f), 0.4f);
            TestLog.Write("arena", v.name, "teleport", name);
        }
    }
}
