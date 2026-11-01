using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Minimal placeholder effects: pooled unlit spheres and tracer lines that fade out.</summary>
    public class Fx : MonoBehaviour
    {
        static Fx _instance;
        static Material _unlit;
        readonly List<(Transform t, Renderer r, float start, float life, float from, float to)> _bursts = new List<(Transform, Renderer, float, float, float, float)>();
        readonly Stack<GameObject> _pool = new Stack<GameObject>();
        readonly List<(LineRenderer lr, float until)> _lines = new List<(LineRenderer, float)>();
        readonly Stack<LineRenderer> _linePool = new Stack<LineRenderer>();

        static Fx Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Fx]");
                    _instance = go.AddComponent<Fx>();
                }
                return _instance;
            }
        }

        public static Material Unlit
        {
            get
            {
                if (_unlit == null) _unlit = new Material(BaseMaterials.Unlit);
                return _unlit;
            }
        }

        public static Material UnlitColor(Color c)
        {
            var m = new Material(Unlit);
            m.SetColor("_BaseColor", c);
            m.color = c;
            return m;
        }

        public static void Burst(Vector3 pos, float radius, Color color, float life = 0.35f)
        {
            Instance.SpawnBurst(pos, radius, color, life);
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color, float width = 0.08f, float life = 0.05f)
        {
            Instance.SpawnTracer(from, to, color, width, life);
        }

        void SpawnBurst(Vector3 pos, float radius, Color color, float life)
        {
            GameObject go;
            if (_pool.Count > 0) { go = _pool.Pop(); go.SetActive(true); }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform);
                go.GetComponent<Renderer>().sharedMaterial = new Material(Unlit);
            }
            go.transform.position = pos;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial.SetColor("_BaseColor", color);
            _bursts.Add((go.transform, r, Time.time, life, radius * 0.4f, radius * 2f));
        }

        void SpawnTracer(Vector3 from, Vector3 to, Color color, float width, float life)
        {
            LineRenderer lr;
            if (_linePool.Count > 0) { lr = _linePool.Pop(); lr.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("Tracer");
                go.transform.SetParent(transform);
                lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = new Material(Unlit);
                lr.positionCount = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            lr.sharedMaterial.SetColor("_BaseColor", color);
            lr.startWidth = lr.endWidth = width;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            _lines.Add((lr, Time.time + life));
        }

        void Update()
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var b = _bursts[i];
                float k = (Time.time - b.start) / b.life;
                if (k >= 1f)
                {
                    b.t.gameObject.SetActive(false);
                    _pool.Push(b.t.gameObject);
                    _bursts.RemoveAt(i);
                    continue;
                }
                b.t.localScale = Vector3.one * Mathf.Lerp(b.from, b.to, k);
            }
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (Time.time < _lines[i].until) continue;
                _lines[i].lr.gameObject.SetActive(false);
                _linePool.Push(_lines[i].lr);
                _lines.RemoveAt(i);
            }
        }
    }
}
