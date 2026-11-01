using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Build switches for unattended runs:
    ///   -balancing [-seeds 11,22,33] [-limit N] [-timescale 6]   546 duels on Stage 1
    ///   -perf [-perfminutes 10] [-warmup 60]                      laptop reference measurement
    /// Results land next to the executable in "Balancing" and "Perf".
    /// </summary>
    static class CommandLineModes
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            string Arg(string name, string fallback)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (args.Contains("-balancing"))
            {
                var seeds = Arg("-seeds", "11,22,33").Split(',').Select(int.Parse).ToArray();
                BalancingRunner.Start(seeds, int.Parse(Arg("-limit", "0")), float.Parse(Arg("-timescale", "6"), System.Globalization.CultureInfo.InvariantCulture),
                    Path.Combine(root, "Balancing", DateTime.Now.ToString("yyyyMMdd_HHmmss")));
            }
            else if (args.Contains("-perf"))
            {
                PerfProbe.Start(float.Parse(Arg("-warmup", "60"), System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(Arg("-perfminutes", "10"), System.Globalization.CultureInfo.InvariantCulture),
                    Path.Combine(root, "Perf"));
            }
        }
    }
}
