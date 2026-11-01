using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Per-frame breakdown from ProfilerRecorder (works in the editor and in players): frame time, scripts,
    /// physics, rendering, GC allocation and collections, draw calls. Finds what makes the game stutter
    /// (spikes) and what costs throughput (averages). Start with <see cref="Begin"/>, read with <see cref="Report"/>.
    /// </summary>
    public class FrameStats : MonoBehaviour
    {
        struct Rec
        {
            public string label;
            public ProfilerRecorder r;
            public bool time; // nanoseconds -> ms
        }

        static readonly (ProfilerCategory cat, string marker, string label, bool time)[] Markers =
        {
            (ProfilerCategory.Internal, "PlayerLoop", "PlayerLoop", true),
            (ProfilerCategory.Internal, "EditorLoop", "EditorLoop", true),
            (ProfilerCategory.Scripts, "Update.ScriptRunBehaviourUpdate", "Scripts Update", true),
            (ProfilerCategory.Scripts, "FixedUpdate.ScriptRunBehaviourFixedUpdate", "Scripts FixedUpdate", true),
            (ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate", "Scripts LateUpdate", true),
            (ProfilerCategory.Physics, "FixedUpdate.PhysicsFixedUpdate", "Physics", true),
            (ProfilerCategory.Internal, "FixedUpdate.PhysicsFixedUpdate", "Physics (int)", true),
            (ProfilerCategory.Render, "PostLateUpdate.FinishFrameRendering", "Rendering", true),
            (ProfilerCategory.Internal, "PreLateUpdate.DirectorUpdateAnimationBegin", "Animation", true),
            (ProfilerCategory.Gui, "PostLateUpdate.PlayerUpdateCanvases", "UI Canvas", true),
            (ProfilerCategory.Gui, "PostLateUpdate.UpdateRectTransform", "RectTransform", true),
            (ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread", "Wait GPU/Present", true),
            (ProfilerCategory.Render, "PostLateUpdate.UpdateAllRenderers", "UpdateAllRenderers", true),
            (ProfilerCategory.Internal, "PreUpdate.NewInputUpdate", "Input System", true),
            (ProfilerCategory.Internal, "EarlyUpdate.UpdatePreloading", "Preloading", true),
            (ProfilerCategory.Internal, "PostLateUpdate.PresentAfterDraw", "PresentAfterDraw", true),
            (ProfilerCategory.Internal, "Initialization.PlayerUpdateTime", "UpdateTime", true),
            (ProfilerCategory.Physics, "Physics.Simulate", "Physics.Simulate", true),
            (ProfilerCategory.Physics, "Physics.Processing", "Physics.Processing", true),
            (ProfilerCategory.Physics, "Physics.ProcessReports", "Physics.Reports", true),
            (ProfilerCategory.Physics, "Physics.FetchResults", "Physics.Fetch", true),
            (ProfilerCategory.Physics, "Physics.UpdateBodies", "Physics.UpdateBodies", true),
            (ProfilerCategory.Physics, "Physics.SyncColliderTransform", "Physics.SyncColliders", true),
            (ProfilerCategory.Physics, "Physics.SyncRigidbodyTransform", "Physics.SyncBodies", true),
            (ProfilerCategory.Physics, "Physics.Interpolation", "Physics.Interpolation", true),
            (ProfilerCategory.Physics, "Physics.ProcessingCCD", "Physics.CCD", true),
            (ProfilerCategory.Internal, "FixedUpdate.NewInputFixedUpdate", "Input Fixed", true),
            (ProfilerCategory.Internal, "PreUpdate.PhysicsUpdate", "PreUpdate.Physics", true),
            (ProfilerCategory.Scripts, "BehaviourUpdate", "BehaviourUpdate", true),
            (ProfilerCategory.Scripts, "FixedBehaviourUpdate", "FixedBehaviourUpdate", true),
            (ProfilerCategory.Internal, "GC.Collect", "GC.Collect", true),
            (ProfilerCategory.Memory, "GC Allocated In Frame", "GC alloc (KB)", false),
            (ProfilerCategory.Render, "Batches Count", "Batches", false),
            (ProfilerCategory.Render, "SetPass Calls Count", "SetPass", false),
            (ProfilerCategory.Render, "Shadow Casters Count", "Shadow casters", false),
            (ProfilerCategory.Render, "Triangles Count", "Triangles", false),
        };

        readonly List<Rec> _recs = new List<Rec>();
        readonly List<float[]> _frames = new List<float[]>(20000);
        float _until;
        int _skip;

        public static FrameStats Instance { get; private set; }
        public bool Done { get; private set; }

        public static FrameStats Begin(float seconds)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("[FrameStats]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<FrameStats>();
            Instance._until = Time.unscaledTime + seconds;
            return Instance;
        }

        void OnEnable()
        {
            foreach (var m in Markers)
                _recs.Add(new Rec { label = m.label, time = m.time, r = ProfilerRecorder.StartNew(m.cat, m.marker, 1) });
            _skip = 3;
        }

        void OnDisable()
        {
            foreach (var r in _recs) r.r.Dispose();
            _recs.Clear();
        }

        void Update()
        {
            if (Done) return;
            if (_skip-- > 0) return;
            var row = new float[_recs.Count + 1];
            row[0] = Time.unscaledDeltaTime * 1000f;
            for (int i = 0; i < _recs.Count; i++)
            {
                var r = _recs[i];
                long v = r.r.Valid ? r.r.LastValue : 0;
                row[i + 1] = r.time ? v / 1e6f : (r.label.StartsWith("GC alloc") ? v / 1024f : v);
            }
            _frames.Add(row);
            if (Time.unscaledTime >= _until) Done = true;
        }

        public string Report(string path = null)
        {
            if (_frames.Count == 0) return "no frames";
            var sb = new StringBuilder();
            var ft = _frames.Select(f => f[0]).OrderBy(x => x).ToList();
            float P(List<float> s, float q) => s[Mathf.Clamp(Mathf.CeilToInt(q * s.Count) - 1, 0, s.Count - 1)];
            float median = P(ft, 0.5f);
            sb.AppendLine($"{_frames.Count} frames · frame ms avg {ft.Average():F2} p50 {median:F2} p95 {P(ft, 0.95f):F2} p99 {P(ft, 0.99f):F2} max {ft.Last():F2} · FPS avg {1000f / ft.Average():F0}");
            sb.AppendLine();
            sb.AppendLine("marker                 avg      p95      max   (valid)");
            for (int i = 0; i < _recs.Count; i++)
            {
                var col = _frames.Select(f => f[i + 1]).OrderBy(x => x).ToList();
                sb.AppendLine($"{_recs[i].label,-20} {col.Average(),8:F2} {P(col, 0.95f),8:F2} {col.Last(),8:F2}   {_recs[i].r.Valid}");
            }
            int gcFrames = _frames.Count(f => f[_recs.FindIndex(r => r.label == "GC.Collect") + 1] > 0f);
            sb.AppendLine($"frames with GC.Collect: {gcFrames}");
            sb.AppendLine();
            sb.AppendLine($"spikes (> 2x median {median:F2} ms):");
            int n = 0;
            for (int f = 0; f < _frames.Count && n < 12; f++)
            {
                if (_frames[f][0] < median * 2f) continue;
                n++;
                var parts = new List<string>();
                for (int i = 0; i < _recs.Count; i++)
                    if (_recs[i].time && _frames[f][i + 1] > 0.3f) parts.Add($"{_recs[i].label} {_frames[f][i + 1]:F1}");
                parts.Add($"GC {_frames[f][_recs.FindIndex(r => r.label.StartsWith("GC alloc")) + 1]:F0}KB");
                sb.AppendLine($"  #{f}: {_frames[f][0]:F1} ms · " + string.Join(" · ", parts));
            }
            int spikes = _frames.Count(x => x[0] >= median * 2f);
            sb.AppendLine($"spike frames total: {spikes}");
            if (path != null) File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return sb.ToString();
        }
    }
}
