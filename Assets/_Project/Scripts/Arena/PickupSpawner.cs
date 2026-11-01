using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public enum PickupKind { Weapon, NanoPatch, CoreOvercharge, NitroCell }

    /// <summary>Hands out its content once per spawn even with simultaneous contacts, then respawns.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PickupSpawner : MonoBehaviour
    {
        public const float DefaultRespawn = 20f;
        public const float NanoPatchFraction = 0.25f;

        public static readonly List<PickupSpawner> All = new List<PickupSpawner>();

        public PickupKind kind = PickupKind.Weapon;
        public WeaponDefinition weapon;
        public int amountOverride;
        public float respawnSeconds = DefaultRespawn;
        [Tooltip("Hidden until revealed (destroyed letter, pillar, depot door).")]
        public bool startHidden;
        [Tooltip("Never respawns once taken.")]
        public bool oneShot;

        Transform _visual;
        float _respawnAt;

        public bool Available { get; private set; } = true;
        public bool Heals => kind == PickupKind.NanoPatch || kind == PickupKind.CoreOvercharge;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("Pickup");
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.6f;
            BuildVisual();
            if (startHidden)
            {
                Available = false;
                _respawnAt = float.PositiveInfinity;
                _visual.gameObject.SetActive(false);
            }
        }

        public void Reveal()
        {
            if (Available) return;
            Available = true;
            _visual.gameObject.SetActive(true);
            TestLog.Write("arena", name, "revealed", "");
        }

        void BuildVisual()
        {
            var go = GameObject.CreatePrimitive(kind == PickupKind.Weapon ? PrimitiveType.Cube : PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1f;
            go.transform.localScale = Vector3.one * (kind == PickupKind.CoreOvercharge ? 1.3f : 0.9f);
            Color c;
            switch (kind)
            {
                case PickupKind.NanoPatch: c = new Color(0.2f, 1f, 0.3f); break;
                case PickupKind.CoreOvercharge: c = new Color(1f, 1f, 1f); break;
                case PickupKind.NitroCell: c = new Color(0.2f, 0.6f, 1f); break;
                default: c = weapon != null ? weapon.color : Color.magenta; break;
            }
            go.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(c);
            Fx.NoShadows(go.GetComponent<Renderer>());
            _visual = go.transform;
        }

        void Update()
        {
            if (!Available && Time.time >= _respawnAt)
            {
                Available = true;
                _visual.gameObject.SetActive(true);
            }
            if (Available) _visual.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other) => TryGive(other);
        void OnTriggerStay(Collider other) => TryGive(other);

        void TryGive(Collider other)
        {
            if (!Available) return;
            var v = Vehicle.Of(other.attachedRigidbody);
            if (v == null || !v.IsAlive) return;

            // Claim first, then grant: a second contact in the same step finds it unavailable.
            Available = false;
            _respawnAt = oneShot ? float.PositiveInfinity : Time.time + respawnSeconds;
            _visual.gameObject.SetActive(false);

            switch (kind)
            {
                case PickupKind.NanoPatch: v.Heal(v.MaxHp * NanoPatchFraction, "nano-patch"); break;
                case PickupKind.CoreOvercharge: v.Heal(v.MaxHp, "core-overcharge"); break;
                case PickupKind.NitroCell: v.Motor.Nitro = VehicleMotor.NitroMax; break;
                case PickupKind.Weapon:
                    if (weapon != null) v.Arsenal.AddAmmo(weapon, amountOverride > 0 ? amountOverride : weapon.chargesPerPickup);
                    break;
            }
            TestLog.Write("pickup", v.name, kind == PickupKind.Weapon && weapon != null ? weapon.id : kind.ToString(), name);
        }
    }
}
