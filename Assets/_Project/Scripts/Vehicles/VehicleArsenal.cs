using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public class WeaponSlot
    {
        public WeaponDefinition Def;
        public int Ammo;
    }

    /// <summary>Machine guns, pickup weapons, Overdrive energy with maneuvers, and the vehicle special.</summary>
    public class VehicleArsenal : MonoBehaviour
    {
        public const float EnergyMax = 100f;
        public const float EnergyRegenPerSec = 8f;
        public const float EnergyColorThreshold = 50f;
        public const float CostLow = 15f, CostMid = 30f, CostMidHigh = 45f, CostVeryHigh = 75f;
        public const float GunInterval = 0.09f;
        public const float GunDamage = 2f;
        public const float GunRange = 75f;
        public const float GunAssistDeg = 7f;
        public const float ScrapHealPerCharge = 0.05f;

        Vehicle _v;
        float _nextGun, _nextWeapon, _rechargeTimer;
        bool _gunAlt;
        Projectile _placedRemote;
        static int _gunMask = -1;

        public readonly List<WeaponSlot> Slots = new List<WeaponSlot>();
        public int SelectedIndex { get; private set; }
        public float Energy { get; set; } = EnergyMax;
        public SpecialAbility Special { get; private set; }
        public int SpecialCharges { get; set; }
        public float RamMultiplier => Special != null ? Special.RamMultiplier : 1f;
        public WeaponSlot Selected => Slots.Count > 0 ? Slots[SelectedIndex] : null;
        public bool HasPlacedRemote => _placedRemote != null && _placedRemote.isActiveAndEnabled;
        public bool SpecialReady => Special != null && SpecialCharges > 0;

        public int TotalAmmo
        {
            get
            {
                int sum = 0;
                foreach (var s in Slots) sum += s.Ammo;
                return sum;
            }
        }

        static int GunMask
        {
            get
            {
                if (_gunMask == -1) _gunMask = ~LayerMask.GetMask("Projectile", "Pickup", "Ignore Raycast");
                return _gunMask;
            }
        }

        public void Init(Vehicle v)
        {
            _v = v;
            Slots.Clear();
            var db = GameDatabase.Instance;
            if (db != null)
                foreach (var w in db.weapons)
                    if (w != null) Slots.Add(new WeaponSlot { Def = w, Ammo = 0 });
            Energy = EnergyMax;
            Special = GetComponent<SpecialAbility>();
            if (Special != null) Special.Init(v);
            SpecialCharges = v.Definition.specialCharges;
        }

        public void Step(in VehicleCommand c, float dt)
        {
            Energy = Mathf.Min(EnergyMax, Energy + EnergyRegenPerSec * dt);
            if (Special != null && SpecialCharges < _v.Definition.specialCharges)
            {
                _rechargeTimer += dt;
                if (_rechargeTimer >= _v.Definition.specialRecharge) { _rechargeTimer = 0f; SpecialCharges++; }
            }

            if (c.WeaponCycle != 0) Cycle(c.WeaponCycle);
            if (c.PrimaryFire) FireGuns();
            if (c.WeaponFire) FireSelected(true);
            if (c.Special) TrySpecial();
            if (c.Maneuver != Maneuver.None) TryManeuver(c.Maneuver);
            if (Special != null) Special.Step(c, dt);
        }

        public float SpecialRechargeProgress => _v.Definition.specialRecharge > 0f ? _rechargeTimer / _v.Definition.specialRecharge : 1f;

        // ---------------------------------------------------------------- weapons

        public bool AddAmmo(WeaponDefinition def, int amount)
        {
            var slot = Slots.Find(s => s.Def == def);
            if (slot == null) return false;
            bool wasEmpty = Selected == null || Selected.Ammo == 0;
            slot.Ammo += amount;
            if (wasEmpty) SelectedIndex = Slots.IndexOf(slot);
            return true;
        }

        public void Cycle(int dir)
        {
            for (int i = 1; i <= Slots.Count; i++)
            {
                int idx = ((SelectedIndex + dir * i) % Slots.Count + Slots.Count) % Slots.Count;
                if (Slots[idx].Ammo > 0) { SelectedIndex = idx; return; }
            }
        }

        public bool FireSelected(bool forward)
        {
            if (forward && HasPlacedRemote)
            {
                _placedRemote.Detonate();
                _placedRemote = null;
                TestLog.Write("weapon", _v.name, "remote-detonate", "");
                return true;
            }
            var slot = Selected;
            if (slot == null || slot.Ammo <= 0) Cycle(1);
            slot = Selected;
            if (slot == null || slot.Ammo <= 0) { TestLog.Blocked(_v, "weapon", "no-ammo"); return false; }
            if (Time.time < _nextWeapon) { TestLog.Blocked(_v, "weapon", "cooldown"); return false; }

            if (slot.Def.behaviour == WeaponBehaviour.TeslaArc)
            {
                if (!TeslaReactor.TryDischarge(_v)) { TestLog.Blocked(_v, slot.Def.id, "no-reactor-in-range"); return false; }
            }
            else
            {
                var p = LaunchWeapon(slot.Def, forward);
                if (slot.Def.behaviour == WeaponBehaviour.Remote) _placedRemote = p;
            }
            slot.Ammo--;
            _nextWeapon = Time.time + slot.Def.cooldown;
            TestLog.Write("weapon", _v.name, slot.Def.id, forward ? "forward" : "rear");
            if (slot.Ammo == 0) Cycle(1);
            return true;
        }

        Projectile LaunchWeapon(WeaponDefinition def, bool forward)
        {
            var spec = ProjectileSpec.From(def, _v);
            bool dropped = def.behaviour == WeaponBehaviour.Mine || def.behaviour == WeaponBehaviour.Remote;
            var muzzle = forward && !dropped ? _v.Muzzle : _v.RearMuzzle;
            var dir = muzzle.forward;
            if (def.homingDegPerSec > 0f) spec.Target = Targeting.Find(_v, muzzle.position, dir, def.lockConeDeg, def.range, true);
            return Projectile.Launch(spec, muzzle.position, LaunchVelocity(def.behaviour, dir, def.speed));
        }

        Vector3 LaunchVelocity(WeaponBehaviour b, Vector3 dir, float speed)
        {
            var inherit = _v.Body.linearVelocity;
            switch (b)
            {
                case WeaponBehaviour.Lob:
                    return dir * speed * 0.75f + Vector3.up * speed * 0.45f + inherit;
                case WeaponBehaviour.Mine:
                case WeaponBehaviour.Remote:
                    return dir * 3f + inherit * 0.3f;
                default:
                    return dir * speed + Vector3.Project(inherit, dir);
            }
        }

        void FireGuns()
        {
            if (Time.time < _nextGun) return;
            _nextGun = Time.time + GunInterval;
            _gunAlt = !_gunAlt;
            var m = _v.Muzzle;
            var origin = m.position + m.right * (_gunAlt ? 0.5f : -0.5f);
            var dir = m.forward;
            var assist = Targeting.Find(_v, origin, dir, GunAssistDeg, GunRange, true);
            if (assist != null) dir = (Targeting.AimPoint(assist) - origin).normalized;

            var end = origin + dir * GunRange;
            if (Physics.Raycast(origin, dir, out var hit, GunRange, GunMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var target = hit.rigidbody != null ? hit.rigidbody.GetComponent<IDamageable>() : hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !ReferenceEquals(target, _v))
                    target.ReceiveDamage(DamageEvent.Create(_v, target as Vehicle, DamageType.MachineGun, GunDamage, hit.point, 0, "guns"));
            }
            Fx.Tracer(origin, end, new Color(1f, 0.9f, 0.4f));
        }

        // ---------------------------------------------------------------- special

        public bool TrySpecial()
        {
            if (Special == null) { TestLog.Blocked(_v, "special", "none"); return false; }
            if (SpecialCharges <= 0) { TestLog.Blocked(_v, "special", "no-charge"); return false; }
            _v.Stats.specialAttempts++;
            if (!Special.TryActivate(out var reason))
            {
                _v.Stats.specialAttempts--;
                TestLog.Blocked(_v, "special", reason);
                return false;
            }
            SpecialCharges--;
            TestLog.Write("special", _v.name, _v.Definition.specialId, "activated");
            return true;
        }

        public void OnRam(Vehicle other, float closing)
        {
            if (Special != null) Special.OnRam(other, closing);
        }

        public void OnDeath()
        {
            _placedRemote = null;
            if (Special != null) Special.OnOwnerDeath();
        }

        // ---------------------------------------------------------------- overdrive maneuvers

        public static float CostOf(Maneuver m)
        {
            switch (m)
            {
                case Maneuver.ThrusterHop: return CostLow;
                case Maneuver.AegisBarrier:
                case Maneuver.QuickMine:
                case Maneuver.ThermiteBurst:
                case Maneuver.RearThermite: return CostMid;
                case Maneuver.CryoPulse:
                case Maneuver.RearCryoPulse: return CostMidHigh;
                case Maneuver.PhantomCloak: return CostVeryHigh;
                case Maneuver.CerberusBarrage: return EnergyMax;
                default: return 0f;
            }
        }

        public bool TryManeuver(Maneuver m)
        {
            float cost = CostOf(m);
            if (Energy + 0.001f < cost) { TestLog.Blocked(_v, m.ToString(), $"energy {Energy:F0}<{cost:F0}"); return false; }

            bool ok;
            string reason = null;
            switch (m)
            {
                case Maneuver.ThrusterHop:
                    ok = _v.Motor.GroundedWheels > 0;
                    if (ok) _v.Motor.Hop(9f); else reason = "airborne";
                    break;
                case Maneuver.AegisBarrier:
                    _v.Status.Shield();
                    ok = true;
                    break;
                case Maneuver.PhantomCloak:
                    _v.Status.Cloak();
                    ok = true;
                    break;
                case Maneuver.QuickMine:
                    Projectile.Launch(ManeuverSpecs.Mine(_v), _v.RearMuzzle.position, _v.RearMuzzle.forward * 3f);
                    ok = true;
                    break;
                case Maneuver.ThermiteBurst:
                    LaunchLob(ManeuverSpecs.Thermite(_v), _v.Muzzle);
                    ok = true;
                    break;
                case Maneuver.RearThermite:
                    LaunchLob(ManeuverSpecs.Thermite(_v), _v.RearMuzzle);
                    ok = true;
                    break;
                case Maneuver.CryoPulse:
                    LaunchCryo(_v.Muzzle);
                    ok = true;
                    break;
                case Maneuver.RearCryoPulse:
                    LaunchCryo(_v.RearMuzzle);
                    ok = true;
                    break;
                case Maneuver.RearSalvo:
                    ok = FireSelected(false);
                    if (!ok) reason = "no-weapon";
                    break;
                case Maneuver.CerberusBarrage:
                    LaunchCerberus();
                    ok = true;
                    break;
                case Maneuver.ScrapToRepair:
                    ok = ScrapToRepair();
                    if (!ok) reason = "no-weapons-to-scrap";
                    break;
                default:
                    ok = false;
                    reason = "unknown";
                    break;
            }

            if (!ok)
            {
                if (reason != null) TestLog.Blocked(_v, m.ToString(), reason);
                return false;
            }
            Energy -= cost;
            TestLog.Write("maneuver", _v.name, m.ToString(), $"cost={cost:F0};energy={Energy:F0}");
            return true;
        }

        void LaunchLob(ProjectileSpec spec, Transform muzzle)
        {
            var vel = muzzle.forward * spec.Speed * 0.75f + Vector3.up * spec.Speed * 0.45f + _v.Body.linearVelocity;
            Projectile.Launch(spec, muzzle.position, vel);
        }

        void LaunchCryo(Transform muzzle)
        {
            var spec = ManeuverSpecs.Cryo(_v);
            spec.Target = Targeting.Find(_v, muzzle.position, muzzle.forward, 40f, 90f, true);
            Projectile.Launch(spec, muzzle.position, muzzle.forward * spec.Speed + Vector3.Project(_v.Body.linearVelocity, muzzle.forward));
        }

        void LaunchCerberus()
        {
            var seeker = GameDatabase.Instance.Weapon("seeker_dart");
            var m = _v.Muzzle;
            var target = Targeting.Find(_v, m.position, m.forward, 60f, 120f, true);
            for (int i = 0; i < 3; i++)
            {
                var spec = ProjectileSpec.From(seeker, _v);
                spec.Target = target;
                spec.Cause = "cerberus_seeker";
                var dir = Quaternion.AngleAxis((i - 1) * 12f, Vector3.up) * m.forward;
                Projectile.Launch(spec, m.position + m.right * (i - 1) * 0.6f, dir * spec.Speed);
            }
            var cryo = ManeuverSpecs.Cryo(_v);
            cryo.Target = target;
            cryo.Cause = "cerberus_cryo";
            Projectile.Launch(cryo, m.position + Vector3.up * 0.5f, m.forward * cryo.Speed);
        }

        bool ScrapToRepair()
        {
            int total = TotalAmmo;
            if (total <= 0) return false;
            foreach (var s in Slots) s.Ammo = 0;
            _v.Heal(_v.MaxHp * ScrapHealPerCharge * total, $"scrap-to-repair x{total}");
            return true;
        }
    }

    public static class ManeuverSpecs
    {
        public static ProjectileSpec Cryo(Vehicle owner) => new ProjectileSpec
        {
            Behaviour = WeaponBehaviour.Rocket, Speed = 45f, Direct = 8f, Homing = 140f, Lifetime = 2.5f,
            Freezes = true, Color = new Color(0.55f, 0.9f, 1f), Scale = 0.7f, Cause = "cryo_pulse", Owner = owner, Impulse = 1f
        };

        public static ProjectileSpec Thermite(Vehicle owner) => new ProjectileSpec
        {
            Behaviour = WeaponBehaviour.Lob, Speed = 28f, Direct = 10f, Splash = 8f, Radius = 3f, Gravity = 20f, Lifetime = 4f,
            Ignites = true, Color = new Color(1f, 0.4f, 0.05f), Scale = 0.6f, Cause = "thermite_burst", Owner = owner, Impulse = 2f
        };

        public static ProjectileSpec Mine(Vehicle owner) => new ProjectileSpec
        {
            Behaviour = WeaponBehaviour.Mine, Speed = 3f, Splash = 30f, Radius = 4f, TriggerRadius = 3f, Gravity = 20f, Lifetime = 90f,
            Color = new Color(0.9f, 0.1f, 0.1f), Scale = 0.5f, Cause = "quick_mine", Owner = owner, Impulse = 6f
        };
    }
}
