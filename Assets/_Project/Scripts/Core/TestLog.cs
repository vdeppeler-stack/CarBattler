using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Line-based protocol for lab runs and balancing: one CSV line per event.
    /// Editor: &lt;project&gt;/Logs/IronSyndicate, builds: persistentDataPath/Logs.
    /// </summary>
    public static class TestLog
    {
        static StreamWriter _writer;
        static string _path;
        public static string RunId { get; private set; } = "";
        public static bool EchoToConsole = true;

        public static string Path => _path;

        public static void BeginRun(string label, int seed)
        {
            EndRun();
            string dir = Application.isEditor
                ? System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "IronSyndicate")
                : System.IO.Path.Combine(Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(dir);
            RunId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{label}_{seed}";
            _path = System.IO.Path.Combine(dir, RunId + ".csv");
            _writer = new StreamWriter(_path, false, new UTF8Encoding(false)) { AutoFlush = true };
            _writer.WriteLine("time;category;actor;event;detail");
            Write("run", "-", "begin", $"build={Application.version};unity={Application.unityVersion};seed={seed};device={SystemInfo.deviceModel};cpu={SystemInfo.processorType};gpu={SystemInfo.graphicsDeviceName}");
        }

        public static void EndRun()
        {
            if (_writer == null) return;
            Write("run", "-", "end", "");
            _writer.Dispose();
            _writer = null;
        }

        public static void Write(string category, string actor, string evt, string detail)
        {
            string line = $"{Time.time:F2};{category};{actor};{evt};{detail}";
            _writer?.WriteLine(line);
            if (EchoToConsole && category != "bot") Debug.Log("[IS] " + line);
        }

        /// <summary>Blocked actions are logged with their reason so lab runs can explain silent no-ops.</summary>
        public static void Blocked(Vehicle v, string action, string reason) => Write("blocked", v != null ? v.name : "-", action, reason);
    }
}
