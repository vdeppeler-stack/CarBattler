using System;
using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Arena object with hit points: bridge spans, reactor pillars, letters, depot doors. When destroyed its
    /// child pieces fall as debris, hidden pickups are revealed and <see cref="Version"/> advances so the
    /// drive graph replans. Optional cause filter (e.g. only C4 breaks the highway span).
    /// </summary>
    public class Destructible : MonoBehaviour, IDamageable
    {
        public static readonly List<Destructible> All = new List<Destructible>();

        /// <summary>Increments on every destruction; bots compare it to detect blocked routes.</summary>
        public static int Version { get; private set; }

        public string label = "";
        public float maxHp = 100f;
        [Tooltip("Empty = every cause. Otherwise only these damage causes (weapon ids) hurt it.")]
        public string[] onlyCauses = new string[0];
        public bool rammable = true;
        public PickupSpawner[] revealPickups = new PickupSpawner[0];
        public float debrisSeconds = 10f;
        [Tooltip("Debris: pieces fall and vanish. Keep: pieces stay (a listener such as ToppleOnDestroy moves them).")]
        public bool keepPieces;
        [Tooltip("Opened by the destruction (tunnel plug behind a statue).")]
        public GameObject[] disableOnDestroy = new GameObject[0];
        public GameObject[] enableOnDestroy = new GameObject[0];

        public float Hp { get; private set; }
        public bool IsDestroyed { get; private set; }
        public event Action<Destructible> Destroyed;

        float _nextImmuneLog;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Awake() => Hp = maxHp;

        public static void ResetStatics() => Version = 0;

        public void ReceiveDamage(in DamageEvent e)
        {
            if (IsDestroyed) return;
            if (onlyCauses.Length > 0 && Array.IndexOf(onlyCauses, e.Cause) < 0)
            {
                if (Time.time >= _nextImmuneLog)
                {
                    _nextImmuneLog = Time.time + 2f;
                    TestLog.Write("arena", name, "immune", e.Cause);
                }
                return;
            }
            Hp -= e.Amount;
            if (Hp <= 0f) Break(e.Source, e.Cause);
        }

        void OnCollisionEnter(Collision collision) => OnPieceCollision(collision);

        public void OnPieceCollision(Collision collision)
        {
            if (!rammable || IsDestroyed || onlyCauses.Length > 0) return;
            var v = Vehicle.Of(collision.rigidbody);
            if (v == null) return;
            float closing = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
            if (closing < Vehicle.RamThreshold) return;
            float amount = (closing - Vehicle.RamThreshold) * Vehicle.RamDamagePerMs * v.Body.mass / 1500f;
            ReceiveDamage(DamageEvent.Create(v, null, DamageType.Ram, amount, collision.GetContact(0).point, 0, "ram"));
        }

        /// <summary>Lab and scripted events break objects directly.</summary>
        public void Break(Vehicle by, string cause)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            Hp = 0f;
            Version++;
            foreach (var p in revealPickups)
                if (p != null) p.Reveal();
            foreach (var go in disableOnDestroy)
                if (go != null) go.SetActive(false);
            foreach (var go in enableOnDestroy)
                if (go != null) go.SetActive(true);

            if (keepPieces)
            {
                TestLog.Write("arena", name, "destroyed", $"by={(by != null ? by.name : "-")};cause={cause}");
                Destroyed?.Invoke(this);
                return;
            }

            var pieces = new List<Transform>();
            foreach (Transform c in transform) pieces.Add(c);
            if (pieces.Count == 0) pieces.Add(transform);
            foreach (var piece in pieces)
            {
                piece.gameObject.isStatic = false;
                if (piece == transform)
                {
                    gameObject.SetActive(false);
                    continue;
                }
                piece.SetParent(null, true);
                var relay = piece.GetComponent<DestructiblePiece>();
                if (relay != null) Destroy(relay);
                var rb = piece.gameObject.AddComponent<Rigidbody>();
                var size = piece.lossyScale;
                rb.mass = Mathf.Clamp(size.x * size.y * size.z * 200f, 50f, 20000f);
                rb.AddForce(UnityEngine.Random.insideUnitSphere * 2f + Vector3.down, ForceMode.VelocityChange);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 1.5f, ForceMode.VelocityChange);
                Destroy(piece.gameObject, debrisSeconds);
            }
            TestLog.Write("arena", name, "destroyed", $"by={(by != null ? by.name : "-")};cause={cause}");
            Destroyed?.Invoke(this);
        }
    }
}
