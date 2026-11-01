using System;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Raycast suspension on a single Rigidbody: drive along the contact surface, limited side grip,
    /// no ground steering while airborne, brake-then-reverse, drift for tight 180s, separate nitro tank.
    /// </summary>
    public class VehicleMotor : MonoBehaviour
    {
        public const float NitroMax = 100f;
        public const float NitroBurnPerSec = 30f;
        public const float NitroRegenPerSec = 3f;
        public const float BrakeDecel = 28f;
        public const float ReverseSpeedFactor = 0.45f;

        public Transform[] wheelPoints = new Transform[0];
        public Transform[] wheelVisuals = new Transform[0];
        public float suspensionLength = 0.55f;

        Vehicle _v;
        Rigidbody _rb;
        VehicleDefinition _d;
        float _springK, _damperC, _wheelRadius;
        int _groundMask;
        float _flippedTime;
        float _wheelSpin;
        float _extraGripLossUntil;

        public float Nitro { get; set; } = NitroMax;
        public bool Boosting { get; private set; }
        public int GroundedWheels { get; private set; }
        public bool Grounded => GroundedWheels >= 2;
        public float ForwardSpeed { get; private set; }
        public float Speed => _rb != null ? _rb.linearVelocity.magnitude : 0f;
        public float SpeedMultiplier { get; set; } = 1f;
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        public event Action NitroIgnited;

        public void Init(Vehicle v)
        {
            _v = v;
            _rb = v.Body;
            _d = v.Definition;
            _wheelRadius = _d.wheelRadius;
            _rb.mass = _d.mass;
            _rb.linearDamping = 0.05f;
            _rb.angularDamping = 1.5f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.centerOfMass = new Vector3(0f, -_d.bodySize.y * 0.35f, 0.1f);
            int wheels = Mathf.Max(1, wheelPoints.Length);
            // Rest compression ~40%, damping at half critical.
            _springK = _d.mass * 9.81f / wheels / 0.4f;
            _damperC = 2f * Mathf.Sqrt(_springK * _d.mass / wheels) * 0.5f;
            _groundMask = ~LayerMask.GetMask("Vehicle", "Projectile", "Pickup", "Ignore Raycast");
        }

        public void LoseGrip(float seconds) => _extraGripLossUntil = Time.time + seconds;

        public void Hop(float upSpeed)
        {
            _rb.AddForce(transform.up * upSpeed, ForceMode.VelocityChange);
        }

        public void Step(VehicleCommand c, float dt)
        {
            Suspension();
            if (!_v.IsAlive) { Boosting = false; return; }

            var vel = _rb.linearVelocity;
            var up = Grounded ? GroundNormal : Vector3.up;
            var fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            var right = Vector3.Cross(up, fwd);
            ForwardSpeed = Vector3.Dot(vel, fwd);

            // Nitro: separate tank, slow regeneration, refilled by Nitro-Cells.
            bool wantBoost = c.Boost && Nitro > 0f && !_v.Status.IsSnared && c.Brake < 0.1f;
            if (wantBoost && !Boosting)
            {
                NitroIgnited?.Invoke();
                _v.Status.Extinguish("nitro");
            }
            Boosting = wantBoost;
            Nitro = Boosting ? Mathf.Max(0f, Nitro - NitroBurnPerSec * dt) : Mathf.Min(NitroMax, Nitro + NitroRegenPerSec * dt);

            float top = _d.topSpeed * SpeedMultiplier * (Boosting ? 1.35f : 1f);
            float accel = _d.acceleration * (Boosting ? 1.9f : 1f);

            if (Grounded)
            {
                if (c.Throttle > 0.01f && ForwardSpeed < top)
                    _rb.AddForce(fwd * accel * c.Throttle, ForceMode.Acceleration);
                else if (Boosting && ForwardSpeed < top)
                    _rb.AddForce(fwd * accel, ForceMode.Acceleration);

                if (c.Brake > 0.01f)
                {
                    if (ForwardSpeed > 1.0f)
                        _rb.AddForce(-fwd * BrakeDecel * c.Brake, ForceMode.Acceleration);
                    else if (ForwardSpeed > -_d.topSpeed * ReverseSpeedFactor)
                        _rb.AddForce(-fwd * _d.acceleration * 0.7f * c.Brake, ForceMode.Acceleration);
                }
                if (c.Throttle < 0.01f && c.Brake < 0.01f && !Boosting)
                {
                    _rb.AddForce(-fwd * ForwardSpeed * 0.35f, ForceMode.Acceleration);
                    // Static friction: a parked vehicle must not creep from suspension tilt.
                    var planar = Vector3.ProjectOnPlane(vel, up);
                    if (planar.magnitude < 2.5f) _rb.AddForce(-planar * 6f, ForceMode.Acceleration);
                }

                if (ForwardSpeed > top + 0.5f)
                    _rb.AddForce(-fwd * (ForwardSpeed - top) * 2f, ForceMode.Acceleration);

                // Side grip: drift and snare loosen it.
                float lateral = Vector3.Dot(vel, right);
                float grip = _d.cornerGrip * (c.Drift ? 0.22f : 1f) * (_v.Status.IsSnared || Time.time < _extraGripLossUntil ? 0.3f : 1f);
                _rb.AddForce(-right * lateral * Mathf.Clamp01(9f * grip * dt), ForceMode.VelocityChange);

                // Steering (yaw rate), reversed when rolling backwards. Drift allows the tight 180.
                float speedFactor = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 5f);
                if (c.Drift) speedFactor = Mathf.Max(speedFactor, 0.6f);
                float dir = ForwardSpeed < -0.5f ? -1f : 1f;
                float highSpeedDamp = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / Mathf.Max(1f, _d.topSpeed)));
                float targetYaw = c.Steer * _d.turnRate * Mathf.Deg2Rad * speedFactor * dir * highSpeedDamp * (c.Drift ? 1.9f : 1f);
                var av = _rb.angularVelocity;
                float currentYaw = Vector3.Dot(av, up);
                _rb.angularVelocity = av + up * (targetYaw - currentYaw) * Mathf.Clamp01(10f * dt);

                _rb.AddForce(-up * Mathf.Min(vel.magnitude, 40f) * 0.25f, ForceMode.Acceleration);
                _flippedTime = 0f;
            }
            else
            {
                // Mild air stabilisation so jumps land wheels-down; no ground steering.
                var axis = Vector3.Cross(transform.up, Vector3.up);
                _rb.AddTorque(axis * 4f, ForceMode.Acceleration);
                if (Vector3.Dot(transform.up, Vector3.up) < 0.3f && _rb.linearVelocity.magnitude < 3f)
                {
                    _flippedTime += dt;
                    if (_flippedTime > 1.5f) SelfRight();
                }
                else _flippedTime = 0f;
            }

            AnimateWheels(dt);
        }

        /// <summary>Stasis: springs only; the vehicle keeps sliding or rolling with what it had.</summary>
        public void StepFrozen()
        {
            Suspension();
            Boosting = false;
            ForwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        }

        void Suspension()
        {
            int grounded = 0;
            var normalSum = Vector3.zero;
            float reach = suspensionLength + _wheelRadius;
            for (int i = 0; i < wheelPoints.Length; i++)
            {
                var p = wheelPoints[i];
                bool hitGround = Physics.Raycast(p.position, -transform.up, out var hit, reach, _groundMask, QueryTriggerInteraction.Ignore);
                float dist = hitGround ? hit.distance - _wheelRadius : suspensionLength;
                // Local offset under the interpolated body: a world position set here would lag the
                // interpolated parent and make the wheels jitter.
                if (i < wheelVisuals.Length && wheelVisuals[i] != null)
                    wheelVisuals[i].localPosition = Vector3.down * Mathf.Clamp(dist, 0f, suspensionLength);
                if (!hitGround) continue;

                grounded++;
                normalSum += hit.normal;
                float compression = Mathf.Clamp01((suspensionLength - dist) / suspensionLength);
                float pointVel = Vector3.Dot(_rb.GetPointVelocity(p.position), transform.up);
                float force = compression * _springK - pointVel * _damperC;
                if (force > 0f) _rb.AddForceAtPosition(transform.up * force, p.position);
            }
            GroundedWheels = grounded;
            GroundNormal = grounded > 0 ? normalSum.normalized : Vector3.up;
        }

        void AnimateWheels(float dt)
        {
            _wheelSpin += ForwardSpeed * dt / Mathf.Max(0.1f, _wheelRadius) * Mathf.Rad2Deg;
            for (int i = 0; i < wheelVisuals.Length; i++)
            {
                if (wheelVisuals[i] == null) continue;
                wheelVisuals[i].localRotation = Quaternion.Euler(_wheelSpin, 0f, 90f);
            }
        }

        void SelfRight()
        {
            _flippedTime = 0f;
            transform.SetPositionAndRotation(transform.position + Vector3.up * 1.5f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            TestLog.Write("vehicle", _v.name, "self-right", transform.position.ToString("F1"));
        }
    }
}
