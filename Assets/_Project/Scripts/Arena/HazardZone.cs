using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public enum HazardKind { Lava, Pool, Reeds }

    /// <summary>
    /// Trigger volume with a per-vehicle effect while inside:
    /// Lava burns and damages, Pool slows and extinguishes, Reeds conceal from lock-on and bot sight.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        public HazardKind kind;
        public float damagePerSecond = 25f;
        public float speedFactor = 0.55f;

        readonly HashSet<Vehicle> _inside = new HashSet<Vehicle>();
        readonly List<Vehicle> _left = new List<Vehicle>();

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        static Vehicle VehicleOf(Collider c) => Vehicle.Of(c.attachedRigidbody);

        void OnTriggerEnter(Collider other)
        {
            var v = VehicleOf(other);
            if (v == null || !v.IsAlive || !_inside.Add(v)) return;
            TestLog.Write("hazard", v.name, "enter", kind.ToString());
            if (kind == HazardKind.Pool) v.Status.Extinguish("pool");
        }

        void OnTriggerExit(Collider other)
        {
            var v = VehicleOf(other);
            if (v == null || !_inside.Remove(v)) return;
            if (kind == HazardKind.Pool) v.Motor.SpeedMultiplier = 1f;
            TestLog.Write("hazard", v.name, "exit", kind.ToString());
        }

        void FixedUpdate()
        {
            _left.Clear();
            foreach (var v in _inside)
            {
                if (v == null || !v.IsAlive) { _left.Add(v); continue; }
                switch (kind)
                {
                    case HazardKind.Lava:
                        v.ReceiveDamage(DamageEvent.Create(null, v, DamageType.Hazard, damagePerSecond * Time.fixedDeltaTime, v.transform.position, 0, "lava"));
                        if (!v.Status.IsBurning) v.Status.Ignite(null);
                        break;
                    case HazardKind.Pool:
                        v.Motor.SpeedMultiplier = speedFactor;
                        if (v.Status.IsBurning) v.Status.Extinguish("pool");
                        break;
                    case HazardKind.Reeds:
                        v.Status.Conceal(0.25f);
                        break;
                }
            }
            foreach (var v in _left) _inside.Remove(v);
        }
    }
}
