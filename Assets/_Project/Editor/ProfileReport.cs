using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Summarises the frames currently held by the Profiler window: main-thread time per marker (self time,
    /// averaged), GC allocations per frame, and the markers behind the slowest frames. Used to find what makes
    /// the game stutter without reading the Profiler UI by hand.
    /// </summary>
    public static class ProfileReport
    {
        public static void Begin()
        {
            ProfilerDriver.ClearAllFrames();
            ProfilerDriver.enabled = true;
            UnityEngine.Profiling.Profiler.enabled = true;
        }

        public static string Write(string path, int topMarkers = 25, int spikeFrames = 6)
        {
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            if (last < 0 || last <= first) return "no frames";
            var selfTotal = new Dictionary<string, double>();
            var calls = new Dictionary<string, long>();
            var frames = new List<(int index, float ms, float gcKb, List<(string name, float ms)> top)>();

            for (int f = first; f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnSelfTime, false))
                {
                    if (view == null || !view.valid) continue;
                    var all = new List<int>();
                    Collect(view, view.GetRootItemID(), all);
                    float gc = 0f;
                    var perFrame = new List<(string, float)>();
                    foreach (int id in all)
                    {
                        string name = view.GetItemName(id);
                        float self = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                        gc += view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);
                        selfTotal[name] = (selfTotal.TryGetValue(name, out var s) ? s : 0) + self;
                        calls[name] = (calls.TryGetValue(name, out var c) ? c : 0) + (long)view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);
                        perFrame.Add((name, self));
                    }
                    frames.Add((f, view.frameTimeMs, gc / 1024f, perFrame.OrderByDescending(p => p.Item2).Take(8).ToList()));
                }
            }
            if (frames.Count == 0) return "no valid frames";

            var sorted = frames.Select(x => x.ms).OrderBy(x => x).ToList();
            float P(float q) => sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)];
            var sb = new StringBuilder();
            sb.AppendLine($"frames {frames.Count}: avg {frames.Average(x => x.ms):F2} ms, p50 {P(0.5f):F2}, p95 {P(0.95f):F2}, p99 {P(0.99f):F2}, max {sorted.Last():F2}");
            sb.AppendLine($"GC alloc per frame: avg {frames.Average(x => x.gcKb):F1} KB, max {frames.Max(x => x.gcKb):F1} KB, frames with GC.Collect: see markers");
            sb.AppendLine();
            sb.AppendLine("Top markers by self time (avg ms/frame, calls/frame):");
            foreach (var kv in selfTotal.OrderByDescending(k => k.Value).Take(topMarkers))
                sb.AppendLine($"  {kv.Value / frames.Count,7:F3}  {calls[kv.Key] / (float)frames.Count,8:F1}  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine("Slowest frames:");
            foreach (var fr in frames.OrderByDescending(x => x.ms).Take(spikeFrames))
            {
                sb.AppendLine($"  frame {fr.index}: {fr.ms:F2} ms, GC {fr.gcKb:F1} KB");
                foreach (var t in fr.top) sb.AppendLine($"      {t.ms,7:F3}  {t.name}");
            }
            File.WriteAllText(path, sb.ToString());
            return sb.ToString();
        }

        static void Collect(HierarchyFrameDataView view, int id, List<int> outIds)
        {
            var children = new List<int>();
            view.GetItemChildren(id, children);
            foreach (var c in children)
            {
                outIds.Add(c);
                Collect(view, c, outIds);
            }
        }
    }
}
