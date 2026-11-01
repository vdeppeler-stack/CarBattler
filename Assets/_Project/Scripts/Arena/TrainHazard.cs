using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Freight train crossing a road on a timetable: warning lamps for <see cref="warningSeconds"/>, then a
    /// fast pass from A to B. Contact deals heavy hazard damage once per pass and throws the vehicle aside.
    /// The train can be destroyed; the wreck stops where it is and never hits again.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrainHazard : MonoBehaviour, IDamageable
    {
        public Transform from, to;
        public Renderer[] lamps = new Renderer[0];
        public float interval = 14f;
        public float warningSeconds = 3f;
        public float speed = 32f;
        public float damage = 220f;
        public float maxHp = 300f;

        public bool IsDestroyed => _state == State.Destroyed;
        public float Hp { get; private set; }

        enum State { Waiting, Warning, Passing, Destroyed }

        Rigidbody _rb;
        State _state;
        float _until;
        int _passId;
        readonly System.Collections.Generic.HashSet<Vehicle> _hitThisPass = new System.Collections.Generic.HashSet<Vehicle>();

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            Hp = maxHp;
            _rb.position = from.position;
            _until = Time.time + interval * 0.5f;
            SetLamps(false);
            SetVisible(false);
        }

        /// <summary>Between passes the train is gone (it runs in from a tunnel beyond the module).</summary>
        void SetVisible(bool on)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = on;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = on;
        }

        void SetLamps(bool on)
        {
            foreach (var l in lamps) if (l != null) l.material.color = on ? Color.red : new Color(0.2f, 0.05f, 0.05f);
        }

        void FixedUpdate()
        {
            if (_rb == null) return;
            switch (_state)
            {
                case State.Destroyed:
                    return;
                case State.Waiting:
                    if (Time.time < _until) return;
                    _state = State.Warning;
                    _until = Time.time + warningSeconds;
                    TestLog.Write("hazard", name, "train-warning", "");
                    break;
                case State.Warning:
                    SetLamps(Mathf.Repeat(Time.time * 3f, 1f) < 0.5f);
                    if (Time.time < _until) return;
                    _state = State.Passing;
                    _passId++;
                    _hitThisPass.Clear();
                    SetVisible(true);
                    break;
                case State.Passing:
                    var next = Vector3.MoveTowards(_rb.position, to.position, speed * Time.fixedDeltaTime);
                    _rb.MovePosition(next);
                    if ((next - to.position).sqrMagnitude < 0.01f)
                    {
                        _state = State.Waiting;
                        _until = Time.time + interval;
                        _rb.position = from.position;
                        transform.position = from.position;
                        SetLamps(false);
                        SetVisible(false);
                    }
                    break;
            }
        }

        public void ReceiveDamage(in DamageEvent e)
        {
            if (_state == State.Destroyed || _state == State.Waiting) return;
            Hp -= e.Amount;
            if (Hp > 0f) return;
            _state = State.Destroyed;
            SetLamps(false);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.material.color = new Color(0.12f, 0.1f, 0.1f);
            Fx.Burst(transform.position, 6f, new Color(1f, 0.5f, 0.1f), 0.6f);
            TestLog.Write("hazard", name, "train-destroyed", $"by={(e.Source != null ? e.Source.name : "-")}");
        }

        void OnCollisionEnter(Collision collision) => Hit(collision);
        void OnCollisionStay(Collision collision) => Hit(collision);

        void Hit(Collision collision)
        {
            if (_state != State.Passing) return;
            var v = Vehicle.Of(collision.rigidbody);
            if (v == null || !v.IsAlive || !_hitThisPass.Add(v)) return;
            v.ReceiveDamage(DamageEvent.Create(null, v, DamageType.Hazard, damage, collision.GetContact(0).point, 0, "train"));
            var dir = (to.position - from.position).normalized;
            v.Body.AddForce((dir * 18f + Vector3.up * 6f), ForceMode.VelocityChange);
            TestLog.Write("hazard", v.name, "train-hit", $"pass={_passId}");
        }
    }
}
