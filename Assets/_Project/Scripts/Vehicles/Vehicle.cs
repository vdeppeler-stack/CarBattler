using System;
using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    [Serializable]
    public class VehicleStats
    {
        public float damageDealt;
        public float damageTaken;
        public float selfDamage;
        public float healed;
        public int specialAttempts;
        public int specialHits;
        public float stationaryTime;
        public readonly Dictionary<string, float> damageBySource = new Dictionary<string, float>();
    }

    /// <summary>
    /// Root of a combat vehicle. Owns HP and the command loop; delegates driving, weapons and status
    /// to sibling components. Every vehicle (player, bot, lab spawn) is built by <see cref="VehicleFactory"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Vehicle : MonoBehaviour, IDamageable
    {
        public static readonly List<Vehicle> All = new List<Vehicle>();

        public const float RamThreshold = 7f;
        public const float RamDamagePerMs = 1.6f;
        public const float FallKillY = -25f;

        public VehicleDefinition Definition { get; private set; }
        public int Seat { get; private set; }
        public bool IsHuman { get; private set; }
        public Color TeamColor { get; private set; }
        public Rigidbody Body { get; private set; }
        public VehicleMotor Motor { get; private set; }
        public VehicleStatus Status { get; private set; }
        public VehicleArsenal Arsenal { get; private set; }
        public ISeatInput Input { get; set; }
        public Transform Muzzle { get; set; }
        public Transform RearMuzzle { get; set; }
        public Transform Visual { get; set; }

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float HpFraction => MaxHp > 0 ? Hp / MaxHp : 0f;
        public bool IsAlive { get; private set; } = true;
        public Vehicle LastAttacker { get; private set; }
        public VehicleCommand LastCommand { get; private set; }
        public readonly VehicleStats Stats = new VehicleStats();

        public event Action<Vehicle, DamageEvent> Damaged;
        public event Action<Vehicle, Vehicle> Died;

        readonly int[] _recentHits = new int[32];
        int _hitCursor;

        public void Init(VehicleDefinition def, int seat, bool human, Color teamColor)
        {
            Definition = def;
            Seat = seat;
            IsHuman = human;
            TeamColor = teamColor;
            Body = GetComponent<Rigidbody>();
            Motor = GetComponent<VehicleMotor>();
            Status = GetComponent<VehicleStatus>();
            Arsenal = GetComponent<VehicleArsenal>();
            MaxHp = def.maxHp;
            Hp = MaxHp;
            Motor.Init(this);
            Status.Init(this);
            Arsenal.Init(this);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void FixedUpdate()
        {
            if (Definition == null) return;
            if (!IsAlive)
            {
                // Wrecks that left the arena stop simulating.
                if (transform.position.y < FallKillY * 2f) { Body.isKinematic = true; return; }
                Motor.Step(VehicleCommand.Idle, Time.fixedDeltaTime);
                return;
            }

            var cmd = Input != null ? Input.ReadCommand(Time.time) : VehicleCommand.Idle;
            if (MatchManager.Instance != null && !MatchManager.Instance.AllowsControl) cmd = VehicleCommand.Idle;
            if (Status.IsFrozen)
            {
                if (cmd.WeaponFire || cmd.Special || cmd.Maneuver != Maneuver.None) TestLog.Blocked(this, "command", "stasis");
                cmd = VehicleCommand.Idle;
            }
            LastCommand = cmd;

            float dt = Time.fixedDeltaTime;
            // Stasis keeps gravity and momentum: only the suspension acts, no drive, brake or grip.
            if (Status.IsFrozen) Motor.StepFrozen();
            else Motor.Step(cmd, dt);
            Arsenal.Step(cmd, dt);
            Status.Step(dt);

            if (cmd.Throttle > 0.5f && Body.linearVelocity.magnitude < 1f) Stats.stationaryTime += dt;
            if (transform.position.y < FallKillY) Kill(DamageType.Fall, "abyss");
        }

        public void ReceiveDamage(in DamageEvent e)
        {
            if (!IsAlive || e.Amount <= 0f) return;
            for (int i = 0; i < _recentHits.Length; i++)
                if (_recentHits[i] == e.HitId) return;
            _recentHits[_hitCursor] = e.HitId;
            _hitCursor = (_hitCursor + 1) % _recentHits.Length;

            if (Status.IsInvulnerable)
            {
                TestLog.Blocked(this, "damage:" + e.Cause, Status.IsShielded ? "aegis" : "phased");
                return;
            }

            float amount = Mathf.Min(e.Amount, Hp);
            Hp -= amount;
            Stats.damageTaken += amount;
            if (e.Source == this) Stats.selfDamage += amount;
            else if (e.Source != null)
            {
                e.Source.Stats.damageDealt += amount;
                e.Source.Stats.damageBySource.TryGetValue(e.Cause, out var sum);
                e.Source.Stats.damageBySource[e.Cause] = sum + amount;
                LastAttacker = e.Source;
            }
            if (e.Type == DamageType.MachineGun) Status.OnMachineGunHit();
            Damaged?.Invoke(this, e);

            if (Hp <= 0f) Die(e.Source != this ? e.Source : LastAttacker, e.Cause);
        }

        public void Heal(float amount, string cause)
        {
            if (!IsAlive || amount <= 0f) return;
            float before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            Stats.healed += Hp - before;
            TestLog.Write("heal", name, cause, $"{before:F0}->{Hp:F0}");
        }

        public void Kill(DamageType type, string cause)
        {
            if (!IsAlive) return;
            Hp = 0f;
            Die(LastAttacker, cause);
        }

        void Die(Vehicle killer, string cause)
        {
            if (!IsAlive) return;
            IsAlive = false;
            Hp = 0f;
            Status.ClearAll();
            Arsenal.OnDeath();
            GrabLink.ReleaseAll(this);
            TestLog.Write("match", name, "eliminated", $"by={(killer != null ? killer.name : "-")};cause={cause};pos={transform.position:F0}");
            Fx.Burst(transform.position, 4f, new Color(1f, 0.5f, 0.1f), 0.6f);
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials) m.color *= 0.25f;
            Died?.Invoke(this, killer);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive) return;
            var other = collision.rigidbody != null ? collision.rigidbody.GetComponent<Vehicle>() : null;
            if (other == null || !other.IsAlive) return;

            if (Status.IsBurning) other.Status.Ignite(this);

            float closing = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
            if (closing < RamThreshold) return;
            float share = Body.mass / (Body.mass + other.Body.mass) * 2f;
            float damage = (closing - RamThreshold) * RamDamagePerMs * share * Arsenal.RamMultiplier;
            Arsenal.OnRam(other, closing);
            other.ReceiveDamage(DamageEvent.Create(this, other, DamageType.Ram, damage, collision.GetContact(0).point, 0, "ram"));
        }
    }
}
