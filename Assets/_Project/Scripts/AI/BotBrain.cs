using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Bot seat: staggered perception, retreat/re-engage by HP thresholds, direct steering with obstacle
    /// feelers and stuck recovery. Every special decision (use or hold) is logged with its reason.
    /// A hand-authored drive graph replaces direct steering once the arena graph exists.
    /// </summary>
    public class BotBrain : MonoBehaviour, ISeatInput
    {
        public float perceptionInterval = 0.2f;
        public float retreatBelow = 0.30f;
        public float reengageAbove = 0.55f;
        public float memorySeconds = 2f;
        public float stuckSeconds = 2f;

        Vehicle _v;
        System.Random _rng;
        float _nextPerceive;
        Vehicle _target;
        bool _targetVisible;
        Vector3 _lastKnown;
        float _lastSeen = -99f;
        bool _retreating;
        Vector3 _goal;
        PickupSpawner _goalPickup;
        float _stuckTimer, _unstickUntil, _unstickSteer;
        int _stuckCount;
        float _lastSteer;
        float _nextSpecialLog;
        string _lastSpecialReason;
        Vector3 _wanderGoal;
        float _wanderUntil;
        float _detourUntil, _detourSteer, _movingTime;
        readonly System.Collections.Generic.Dictionary<PickupSpawner, float> _blockedGoals = new System.Collections.Generic.Dictionary<PickupSpawner, float>();

        public string State { get; private set; } = "idle";
        public Vehicle Target => _target;

        public void Init(Vehicle v, int seed)
        {
            _v = v;
            _rng = new System.Random(seed);
            _nextPerceive = Time.time + (float)_rng.NextDouble() * perceptionInterval;
        }

        float Rand => (float)_rng.NextDouble();

        public VehicleCommand ReadCommand(float time)
        {
            if (_v == null) return VehicleCommand.Idle;
            if (time >= _nextPerceive)
            {
                _nextPerceive = time + perceptionInterval;
                Perceive(time);
                Decide(time);
            }

            var cmd = Drive(time);
            if (_target != null && _targetVisible && !_retreating) Fight(ref cmd, time);
            else if (_target != null && _targetVisible) FightWhileFleeing(ref cmd);
            SelfCare(ref cmd);
            return cmd;
        }

        // ------------------------------------------------------------------ perception / decision

        void Perceive(float time)
        {
            var eye = _v.transform.position + Vector3.up * 1.2f;
            Vehicle best = null;
            float bestDist = float.MaxValue;
            foreach (var other in Vehicle.All)
            {
                if (other == _v || !other.IsAlive) continue;
                bool alreadyTracked = other == _target && time - _lastSeen < memorySeconds;
                if (other.Status.IsCloaked && !alreadyTracked) continue;
                float d = Vector3.Distance(eye, other.transform.position);
                if (d > 140f) continue;
                if (!Targeting.HasLineOfSight(eye, Targeting.AimPoint(other))) continue;
                if (other.Status.IsCloaked) continue;
                float score = d * (other == _target ? 0.7f : 1f) * (other.Status.IsFrozen ? 0.6f : 1f);
                if (score < bestDist) { bestDist = score; best = other; }
            }

            if (best != null)
            {
                _target = best;
                _targetVisible = true;
                _lastKnown = best.transform.position;
                _lastSeen = time;
            }
            else
            {
                _targetVisible = false;
                if (_target != null && (time - _lastSeen > memorySeconds || !_target.IsAlive)) _target = null;
            }
        }

        void Decide(float time)
        {
            float hp = _v.HpFraction;
            if (!_retreating && hp < retreatBelow) { _retreating = true; TestLog.Write("bot", _v.name, "retreat", $"hp={hp:P0}"); }
            else if (_retreating && hp >= reengageAbove) { _retreating = false; TestLog.Write("bot", _v.name, "reengage", $"hp={hp:P0}"); }

            _goalPickup = null;
            if (_retreating)
            {
                _goalPickup = NearestPickup(p => p.Heals, 120f);
                if (_goalPickup != null) { _goal = _goalPickup.transform.position; State = "retreat-heal"; return; }
                if (_target != null)
                {
                    var away = (_v.transform.position - _lastKnown).normalized;
                    _goal = _v.transform.position + away * 30f;
                    State = "retreat-flee";
                    return;
                }
            }

            if (_target != null)
            {
                var lead = _targetVisible ? _target.Body.linearVelocity * 0.4f : Vector3.zero;
                _goal = (_targetVisible ? _target.transform.position : _lastKnown) + lead;
                State = _targetVisible ? "attack" : "search";
                // Grab weapons first when empty-handed and one is close.
                if (_v.Arsenal.TotalAmmo == 0)
                {
                    var p = NearestPickup(x => x.kind == PickupKind.Weapon, 25f);
                    if (p != null) { _goalPickup = p; _goal = p.transform.position; State = "arm-up"; }
                }
                return;
            }

            var pickup = NearestPickup(x => x.kind == PickupKind.Weapon || (x.Heals && _v.HpFraction < 0.9f), 200f);
            if (pickup != null) { _goalPickup = pickup; _goal = pickup.transform.position; State = "collect"; return; }

            if (time > _wanderUntil)
            {
                var spawns = SpawnPoint.All;
                _wanderGoal = spawns.Count > 0 ? spawns[_rng.Next(spawns.Count)].transform.position : Vector3.zero;
                _wanderUntil = time + 8f;
            }
            _goal = _wanderGoal;
            State = "wander";
        }

        PickupSpawner NearestPickup(System.Predicate<PickupSpawner> filter, float maxDist)
        {
            PickupSpawner best = null;
            float bestD = maxDist;
            foreach (var p in PickupSpawner.All)
            {
                if (!p.Available || !filter(p)) continue;
                if (_blockedGoals.TryGetValue(p, out var until) && Time.time < until) continue;
                float d = Vector3.Distance(p.transform.position, _v.transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        // ------------------------------------------------------------------ driving

        VehicleCommand Drive(float time)
        {
            var cmd = new VehicleCommand();
            var t = _v.transform;
            float speed = _v.Motor.Speed;

            if (time < _unstickUntil)
            {
                cmd.Brake = 1f;
                cmd.Steer = _unstickSteer;
                return cmd;
            }
            if (time < _detourUntil)
            {
                // After backing off, swing out to the side before re-aiming at the goal.
                cmd.Throttle = 0.8f;
                cmd.Steer = _detourSteer;
                return cmd;
            }

            var local = t.InverseTransformPoint(_goal);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float dist = local.magnitude;
            float absAngle = Mathf.Abs(angle);

            float steer = Mathf.Clamp(angle / 30f, -1f, 1f);
            float throttle = 1f;
            if (absAngle > 110f && dist < 14f)
            {
                // Target behind and close: back up while turning.
                cmd.Brake = 1f;
                throttle = 0f;
                steer = -Mathf.Sign(angle);
            }
            else if (absAngle > 45f && dist < 30f)
            {
                // Tighten the turn instead of orbiting the target: ease off, drift when fast.
                throttle = Mathf.Lerp(0.6f, 0.25f, Mathf.InverseLerp(45f, 120f, absAngle));
                if (speed > 12f) { cmd.Drift = true; cmd.Brake = 0.4f; }
            }
            else if (absAngle > 70f && speed > 14f) cmd.Drift = true;

            // Feelers: three rays, length grows with speed.
            float reach = 6f + speed * 0.6f;
            var origin = t.position + Vector3.up * 0.8f;
            bool hitC = Physics.Raycast(origin, t.forward, out var hc, reach, Targeting.LosMask, QueryTriggerInteraction.Ignore);
            bool hitL = Physics.Raycast(origin, Quaternion.AngleAxis(-30f, Vector3.up) * t.forward, out var hl, reach * 0.8f, Targeting.LosMask, QueryTriggerInteraction.Ignore);
            bool hitR = Physics.Raycast(origin, Quaternion.AngleAxis(30f, Vector3.up) * t.forward, out var hr, reach * 0.8f, Targeting.LosMask, QueryTriggerInteraction.Ignore);
            bool steepC = hitC && Vector3.Angle(hc.normal, Vector3.up) > 50f;
            if (steepC)
            {
                float l = hitL ? hl.distance : reach;
                float r = hitR ? hr.distance : reach;
                steer = Mathf.Clamp(steer + (r >= l ? 1.2f : -1.2f), -1f, 1f);
                if (hc.distance < 4f && speed > 6f) { throttle = 0.2f; cmd.Brake = 0.5f; }
            }
            else
            {
                if (hitL && Vector3.Angle(hl.normal, Vector3.up) > 50f) steer += 0.5f * (1f - hl.distance / reach);
                if (hitR && Vector3.Angle(hr.normal, Vector3.up) > 50f) steer -= 0.5f * (1f - hr.distance / reach);
            }

            cmd.Steer = Mathf.Clamp(steer, -1f, 1f);
            cmd.Throttle = throttle;
            if (_retreating && _target != null && Vector3.Distance(_lastKnown, t.position) < 20f && _v.Motor.Nitro > 30f) cmd.Boost = true;
            _lastSteer = cmd.Steer;

            // Stuck: progress below threshold despite throttle for stuckSeconds -> reverse and re-steer.
            if (cmd.Throttle > 0.5f && speed < 1.5f && MatchManager.Instance != null && MatchManager.Instance.AllowsControl)
            {
                _stuckTimer += Time.fixedDeltaTime;
                if (_stuckTimer > stuckSeconds)
                {
                    _stuckTimer = 0f;
                    _stuckCount++;
                    _unstickUntil = time + 1.0f + 0.3f * Mathf.Min(_stuckCount, 3);
                    _unstickSteer = -Mathf.Sign(_lastSteer == 0f ? (Rand - 0.5f) : _lastSteer);
                    _detourUntil = _unstickUntil + 1.2f;
                    _detourSteer = -_unstickSteer * 0.8f;
                    _wanderUntil = 0f;
                    // Repeated failure on the same goal: avoid it for a while.
                    if (_stuckCount >= 2 && _goalPickup != null)
                    {
                        _blockedGoals[_goalPickup] = time + 12f;
                        TestLog.Write("bot", _v.name, "goal-blocked", _goalPickup.name);
                        _goalPickup = null;
                    }
                    TestLog.Write("bot", _v.name, "stuck", $"pos={t.position:F1};state={State};count={_stuckCount}");
                }
            }
            else
            {
                _stuckTimer = 0f;
                _movingTime = speed > 5f ? _movingTime + Time.fixedDeltaTime : 0f;
                if (_movingTime > 4f) _stuckCount = 0;
            }
            return cmd;
        }

        // ------------------------------------------------------------------ combat

        void Fight(ref VehicleCommand cmd, float time)
        {
            var t = _v.transform;
            var to = Targeting.AimPoint(_target) - _v.Muzzle.position;
            float dist = to.magnitude;
            float angle = Vector3.Angle(t.forward, to);
            bool behind = Vector3.Angle(-t.forward, to) < 35f;

            cmd.PrimaryFire = angle < 10f && dist < VehicleArsenal.GunRange;

            ChooseWeapon(ref cmd, dist, angle, behind);
            ConsiderSpecial(ref cmd, dist, time);

            var a = _v.Arsenal;
            if (cmd.Maneuver == Maneuver.None)
            {
                if (_target.Status.IsFrozen && angle < 15f) cmd.PrimaryFire = true;
                else if (a.Energy >= VehicleArsenal.EnergyMax && angle < 25f && dist < 60f && Rand < 0.3f) cmd.Maneuver = Maneuver.CerberusBarrage;
                else if (a.Energy >= 80f && angle < 25f && dist < 50f && Rand < 0.25f) cmd.Maneuver = Maneuver.CryoPulse;
                else if (behind && dist < 25f && a.Energy >= 60f && Rand < 0.1f) cmd.Maneuver = Maneuver.QuickMine;
            }
        }

        void FightWhileFleeing(ref VehicleCommand cmd)
        {
            var to = _target.transform.position - _v.transform.position;
            bool behind = Vector3.Angle(-_v.transform.forward, to) < 35f;
            if (behind && to.magnitude < 30f)
            {
                var sel = _v.Arsenal.Selected;
                if (sel != null && sel.Ammo > 0 && (sel.Def.behaviour == WeaponBehaviour.Mine || sel.Def.behaviour == WeaponBehaviour.Rocket))
                    cmd.Maneuver = sel.Def.behaviour == WeaponBehaviour.Mine ? Maneuver.None : Maneuver.RearSalvo;
                if (sel != null && sel.Ammo > 0 && sel.Def.behaviour == WeaponBehaviour.Mine) cmd.WeaponFire = true;
            }
        }

        void ChooseWeapon(ref VehicleCommand cmd, float dist, float angle, bool behind)
        {
            var a = _v.Arsenal;
            if (a.HasPlacedRemote)
            {
                // Detonate only when someone is next to the charge.
                foreach (var p in Projectile.Active)
                {
                    if (p.Spec.Owner != _v || p.Spec.Behaviour != WeaponBehaviour.Remote) continue;
                    bool selfSafe = Vector3.Distance(p.transform.position, _v.transform.position) > p.Spec.Radius + 1f;
                    if (selfSafe && Vector3.Distance(p.transform.position, _target.transform.position) < p.Spec.Radius * 0.8f) cmd.WeaponFire = true;
                }
                return;
            }

            int bestIdx = -1;
            float bestScore = 0f;
            for (int i = 0; i < a.Slots.Count; i++)
            {
                var s = a.Slots[i];
                if (s.Ammo <= 0) continue;
                float score = WeaponScore(s.Def, dist, angle, behind);
                if (score > bestScore) { bestScore = score; bestIdx = i; }
            }
            if (bestIdx < 0) return;
            if (bestIdx != a.SelectedIndex) { cmd.WeaponCycle = bestIdx > a.SelectedIndex ? 1 : -1; return; }
            cmd.WeaponFire = true;
        }

        float WeaponScore(WeaponDefinition w, float dist, float angle, bool behind)
        {
            // Never fire splash weapons into our own blast radius.
            if (w.splashRadius > 0f && dist < w.splashRadius + 2f && w.behaviour != WeaponBehaviour.Mine) return 0f;
            switch (w.behaviour)
            {
                case WeaponBehaviour.Rocket:
                    if (w.homingDegPerSec <= 0f) return angle < 6f && dist < 45f ? 3f : 0f;
                    return angle < w.lockConeDeg && dist < w.range * 0.8f ? 2f + w.homingDegPerSec / 100f : 0f;
                case WeaponBehaviour.Bounce: return angle < 10f && dist < 40f ? 1.5f : 0f;
                case WeaponBehaviour.Lob: return angle < 15f && dist > 10f && dist < 40f ? 1.8f : 0f;
                case WeaponBehaviour.Mine: return behind && dist < 25f ? 2f : 0f;
                case WeaponBehaviour.Remote: return behind && dist < 12f ? 1.5f : 0f;
                case WeaponBehaviour.TeslaArc:
                    foreach (var r in TeslaReactor.All)
                    {
                        bool selfIn = Vector3.Distance(r.transform.position, _v.transform.position) <= r.radius;
                        bool targetIn = Vector3.Distance(r.transform.position, _target.transform.position) <= r.radius;
                        if (selfIn && targetIn && _v.HpFraction > 0.5f) return 2.5f;
                    }
                    return 0f;
            }
            return 0f;
        }

        void ConsiderSpecial(ref VehicleCommand cmd, float dist, float time)
        {
            var a = _v.Arsenal;
            if (!a.SpecialReady) return;
            bool use = a.Special.BotShouldUse(_target, dist, _targetVisible, out var reason);
            if (use)
            {
                cmd.Special = true;
                TestLog.Write("bot-special", _v.name, "use", reason);
            }
            else if (time >= _nextSpecialLog || reason != _lastSpecialReason)
            {
                _nextSpecialLog = time + 3f;
                _lastSpecialReason = reason;
                TestLog.Write("bot-special", _v.name, "hold", reason);
            }
        }

        void SelfCare(ref VehicleCommand cmd)
        {
            var a = _v.Arsenal;
            var s = _v.Status;
            if (s.IsBurning && cmd.Maneuver == Maneuver.None)
            {
                // Weigh nitro (free, needs space) against Aegis (energy) to put out fire.
                if (_v.Motor.Nitro > 20f && !_retreating) cmd.Boost = true;
                else if (a.Energy >= VehicleArsenal.CostMid) cmd.Maneuver = Maneuver.AegisBarrier;
                else if (_v.Motor.Nitro > 5f) cmd.Boost = true;
            }
            if (_v.HpFraction < 0.35f && a.TotalAmmo >= 3 && cmd.Maneuver == Maneuver.None) cmd.Maneuver = Maneuver.ScrapToRepair;
        }
    }
}
