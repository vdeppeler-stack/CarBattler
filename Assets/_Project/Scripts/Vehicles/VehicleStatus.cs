using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Burn, stasis, shield, cloak, phase and snare timers for one vehicle.</summary>
    public class VehicleStatus : MonoBehaviour
    {
        public const float BurnDuration = 4f;
        public const float BurnTickInterval = 0.5f;
        public const float BurnTickDamage = 3f;
        public const float ShieldDuration = 3f;
        public const float CloakDuration = 6f;

        public float stasisDuration = 3f;
        public int iceStrength = 3;

        Vehicle _v;
        float _burnLeft, _nextBurnTick;
        Vehicle _burnSource;
        float _frozenLeft;
        int _iceLeft;
        float _shieldLeft, _cloakLeft, _phaseLeft, _snareLeft;

        public bool IsBurning => _burnLeft > 0f;
        public bool IsFrozen => _frozenLeft > 0f;
        public bool IsShielded => _shieldLeft > 0f;
        public bool IsCloaked => _cloakLeft > 0f;
        public bool IsPhased => _phaseLeft > 0f;
        public bool IsSnared => _snareLeft > 0f;
        public bool IsInvulnerable => IsShielded || IsPhased;
        public float ShieldLeft => _shieldLeft;
        public int IceLeft => _iceLeft;

        public void Init(Vehicle v) => _v = v;

        public void Ignite(Vehicle source, float duration = BurnDuration)
        {
            if (!_v.IsAlive) return;
            if (IsInvulnerable) { TestLog.Blocked(_v, "ignite", "invulnerable"); return; }
            bool fresh = !IsBurning;
            _burnLeft = duration;
            _burnSource = source;
            if (fresh)
            {
                _nextBurnTick = Time.time + BurnTickInterval;
                TestLog.Write("status", _v.name, "burn-start", source != null ? source.name : "-");
            }
        }

        public void Extinguish(string reason)
        {
            if (!IsBurning) return;
            _burnLeft = 0f;
            TestLog.Write("status", _v.name, "burn-end", reason);
        }

        public void Freeze(Vehicle source)
        {
            if (!_v.IsAlive) return;
            if (IsInvulnerable) { TestLog.Blocked(_v, "freeze", "invulnerable"); return; }
            _frozenLeft = stasisDuration;
            _iceLeft = iceStrength;
            TestLog.Write("status", _v.name, "stasis-start", source != null ? source.name : "-");
        }

        public void OnMachineGunHit()
        {
            if (!IsFrozen) return;
            if (--_iceLeft <= 0) ReleaseStasis("ice-broken");
        }

        void ReleaseStasis(string reason)
        {
            if (!IsFrozen) return;
            _frozenLeft = 0f;
            TestLog.Write("status", _v.name, "stasis-end", reason);
        }

        public void Shield(float duration = ShieldDuration)
        {
            _shieldLeft = duration;
            Extinguish("aegis");
            TestLog.Write("status", _v.name, "shield-start", duration.ToString("F1"));
        }

        public void Cloak(float duration = CloakDuration)
        {
            _cloakLeft = duration;
            TestLog.Write("status", _v.name, "cloak-start", duration.ToString("F1"));
        }

        public void Phase(float duration) => _phaseLeft = duration;

        /// <summary>Reeds and similar cover: hidden from lock-on and bot sight beyond <see cref="ConcealRange"/>.</summary>
        public const float ConcealRange = 12f;
        float _concealedUntil;
        public bool IsConcealed => Time.time < _concealedUntil;
        public void Conceal(float seconds) => _concealedUntil = Mathf.Max(_concealedUntil, Time.time + seconds);
        public void Snare(float duration) => _snareLeft = Mathf.Max(_snareLeft, duration);

        public void Step(float dt)
        {
            if (IsBurning)
            {
                _burnLeft -= dt;
                if (Time.time >= _nextBurnTick)
                {
                    _nextBurnTick += BurnTickInterval;
                    _v.ReceiveDamage(DamageEvent.Create(_burnSource, _v, DamageType.Burn, BurnTickDamage, transform.position, 0, "burn"));
                }
                if (_burnLeft <= 0f) TestLog.Write("status", _v.name, "burn-end", "expired");
            }
            if (IsFrozen)
            {
                _frozenLeft -= dt;
                if (_frozenLeft <= 0f) { _frozenLeft = 0.0001f; ReleaseStasis("expired"); }
            }
            if (IsShielded) { _shieldLeft -= dt; if (_shieldLeft <= 0f) TestLog.Write("status", _v.name, "shield-end", ""); }
            if (IsCloaked) { _cloakLeft -= dt; if (_cloakLeft <= 0f) TestLog.Write("status", _v.name, "cloak-end", ""); }
            if (IsPhased) _phaseLeft -= dt;
            if (IsSnared) _snareLeft -= dt;
        }

        public void ClearAll()
        {
            _burnLeft = _frozenLeft = _shieldLeft = _cloakLeft = _phaseLeft = _snareLeft = 0f;
        }
    }
}
