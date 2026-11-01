using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Thin ice over a drop: slippery while driven on. Loaded for <see cref="loadSeconds"/> it starts
    /// flashing (warning), then breaks and drops whatever is on it; it refreezes after a while.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class IcePlate : MonoBehaviour
    {
        public float loadSeconds = 1f;
        public float warningSeconds = 1.5f;
        public float refreezeSeconds = 8f;

        enum State { Solid, Warning, Broken }

        State _state;
        float _load, _until, _lastContact;
        Collider _col;
        Renderer _r;
        Color _base;

        void Awake()
        {
            _col = GetComponent<Collider>();
            _r = GetComponent<Renderer>();
            if (_r != null) _base = _r.sharedMaterial.color;
        }

        // Vehicles ride on raycast suspension, so the body never touches the plate: detect load by footprint.
        void FixedUpdate()
        {
            if (_state == State.Broken) return;
            var b = _col.bounds;
            foreach (var v in Vehicle.All)
            {
                if (!v.IsAlive) continue;
                var p = v.transform.position;
                if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
                if (p.y < b.min.y - 0.5f || p.y > b.max.y + 2.5f) continue;
                v.Motor.LoseGrip(0.2f);
                _lastContact = Time.time;
            }
        }

        void Update()
        {
            switch (_state)
            {
                case State.Solid:
                    _load = Time.time - _lastContact < 0.2f ? _load + Time.deltaTime : Mathf.Max(0f, _load - Time.deltaTime);
                    if (_load >= loadSeconds)
                    {
                        _state = State.Warning;
                        _until = Time.time + warningSeconds;
                        TestLog.Write("hazard", name, "ice-warning", "");
                    }
                    break;
                case State.Warning:
                    if (_r != null) _r.material.color = Mathf.Repeat(Time.time * 6f, 1f) < 0.5f ? Color.red : _base;
                    if (Time.time >= _until)
                    {
                        _state = State.Broken;
                        _until = Time.time + refreezeSeconds;
                        _col.enabled = false;
                        if (_r != null) _r.enabled = false;
                        Fx.Burst(transform.position, 5f, new Color(0.8f, 0.95f, 1f), 0.5f);
                        TestLog.Write("hazard", name, "ice-break", "");
                    }
                    break;
                case State.Broken:
                    if (Time.time >= _until)
                    {
                        _state = State.Solid;
                        _load = 0f;
                        _col.enabled = true;
                        if (_r != null) { _r.enabled = true; _r.material.color = _base; }
                    }
                    break;
            }
        }
    }
}
