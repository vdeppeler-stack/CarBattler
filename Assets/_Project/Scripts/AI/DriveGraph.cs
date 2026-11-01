using System;
using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    public enum EdgeKind { Road, Jump, Drop, Teleport }

    [Serializable]
    public class DriveEdge
    {
        public int from, to;
        public EdgeKind kind;
        [Tooltip("Jump edges: speed needed at the ramp lip.")]
        public float minSpeed;
        [Tooltip("Edge unusable once this object is destroyed (bridge span).")]
        public Destructible requiresIntact;
        [Tooltip("Edge usable only after this object is destroyed (depot door).")]
        public Destructible requiresDestroyed;

        public bool Usable =>
            (requiresIntact == null || !requiresIntact.IsDestroyed) &&
            (requiresDestroyed == null || requiresDestroyed.IsDestroyed);
    }

    /// <summary>
    /// Hand-authored drive graph per arena: nodes on drivable surfaces, directed edges typed as road, jump,
    /// drop or teleporter. Destroyed objects close their edges; bots notice via <see cref="Destructible.Version"/>
    /// and replan.
    /// </summary>
    public class DriveGraph : MonoBehaviour
    {
        public static DriveGraph Instance { get; private set; }

        public List<string> names = new List<string>();
        public List<Vector3> nodes = new List<Vector3>();
        public List<DriveEdge> edges = new List<DriveEdge>();

        List<DriveEdge>[] _out;

        void OnEnable() { Instance = this; Build(); }
        void OnDisable() { if (Instance == this) Instance = null; }

        void Build()
        {
            _out = new List<DriveEdge>[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) _out[i] = new List<DriveEdge>();
            foreach (var e in edges) _out[e.from].Add(e);
        }

        public string NameOf(int i) => i >= 0 && i < names.Count ? names[i] : i.ToString();

        /// <summary>Nearest node, height weighted so a vehicle in the trench does not pick the bridge above it.</summary>
        public int Nearest(Vector3 p)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                var d = nodes[i] - p;
                float score = new Vector2(d.x, d.z).sqrMagnitude + d.y * d.y * 16f;
                if (score < bestD) { bestD = score; best = i; }
            }
            return best;
        }

        static float Cost(DriveEdge e, Vector3 a, Vector3 b)
        {
            switch (e.kind)
            {
                case EdgeKind.Teleport: return 8f;
                case EdgeKind.Jump: return Vector3.Distance(a, b) * 1.3f;
                default: return Vector3.Distance(a, b);
            }
        }

        // A* over usable edges. Small graphs (< 100 nodes): a list-based open set is enough.
        readonly List<int> _open = new List<int>();

        public bool FindPath(int start, int goal, List<DriveEdge> path)
        {
            if (_out == null || _out.Length != nodes.Count) Build();
            path.Clear();
            if (start < 0 || goal < 0) return false;
            if (start == goal) return true;
            int n = nodes.Count;
            var g = new float[n];
            var came = new DriveEdge[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) g[i] = float.MaxValue;
            g[start] = 0f;
            _open.Clear();
            _open.Add(start);
            while (_open.Count > 0)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < _open.Count; i++)
                {
                    int c = _open[i];
                    float f = g[c] + Vector3.Distance(nodes[c], nodes[goal]);
                    if (f < bf) { bf = f; bi = i; }
                }
                int cur = _open[bi];
                _open.RemoveAt(bi);
                if (cur == goal) break;
                if (closed[cur]) continue;
                closed[cur] = true;
                foreach (var e in _out[cur])
                {
                    if (!e.Usable || closed[e.to]) continue;
                    float ng = g[cur] + Cost(e, nodes[cur], nodes[e.to]);
                    if (ng >= g[e.to]) continue;
                    g[e.to] = ng;
                    came[e.to] = e;
                    _open.Add(e.to);
                }
            }
            if (came[goal] == null) return false;
            for (int at = goal; at != start; at = came[at].from) path.Add(came[at]);
            path.Reverse();
            return true;
        }

        void OnDrawGizmos()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(nodes[i] + Vector3.up * 0.5f, 0.8f);
            }
            foreach (var e in edges)
            {
                if (e.from >= nodes.Count || e.to >= nodes.Count) continue;
                switch (e.kind)
                {
                    case EdgeKind.Jump: Gizmos.color = Color.yellow; break;
                    case EdgeKind.Drop: Gizmos.color = new Color(1f, 0.5f, 0f); break;
                    case EdgeKind.Teleport: Gizmos.color = Color.magenta; break;
                    default: Gizmos.color = e.Usable ? Color.green : Color.red; break;
                }
                var a = nodes[e.from] + Vector3.up * 0.6f;
                var b = nodes[e.to] + Vector3.up * 0.6f;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(b, b + Quaternion.LookRotation((a - b).normalized + Vector3.up * 0.001f) * Quaternion.Euler(0f, 20f, 0f) * Vector3.forward * 2f);
            }
        }
    }
}
