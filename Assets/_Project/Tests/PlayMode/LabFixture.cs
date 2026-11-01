using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IronSyndicate.Tests
{
    /// <summary>Scripted seat for tests: returns whatever command is set.</summary>
    public sealed class ScriptedSeat : ISeatInput
    {
        public VehicleCommand Cmd;
        public VehicleCommand ReadCommand(float time) => Cmd;
    }

    /// <summary>
    /// Test protocol per handbook: case id, build, device, starting situation, action, expected, actual,
    /// passed/failed, error id. One CSV per day under Logs/IronSyndicate.
    /// </summary>
    public static class TestProtocol
    {
        public static string Path
        {
            get
            {
                string dir = System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "IronSyndicate");
                Directory.CreateDirectory(dir);
                return System.IO.Path.Combine(dir, $"Pruefprotokoll_{DateTime.Now:yyyyMMdd}.csv");
            }
        }

        public static void Record(string caseId, string start, string action, string expected, string actual, bool passed)
        {
            bool header = !File.Exists(Path);
            using (var w = new StreamWriter(Path, true, new UTF8Encoding(false)))
            {
                if (header) w.WriteLine("zeit;fall;build;geraet;ausgangslage;handlung;soll;ist;ergebnis;fehler_id");
                w.WriteLine($"{DateTime.Now:HH:mm:ss};{caseId};{Application.version}/{Application.unityVersion};{SystemInfo.deviceModel} {SystemInfo.processorType};" +
                            $"{start};{action};{expected};{actual};{(passed ? "bestanden" : "fehlgeschlagen")};{(passed ? "" : "F-" + caseId)}");
            }
        }

        /// <summary>Records the case and asserts it.</summary>
        public static void Check(string caseId, string start, string action, string expected, string actual, bool passed)
        {
            Record(caseId, start, action, expected, actual, passed);
            Assert.IsTrue(passed, $"{caseId}: expected {expected}, got {actual}");
        }
    }

    /// <summary>
    /// Builds a bare test ground per test and spawns vehicles through the normal factory (same prefabs and
    /// damage rules as a match). Spawned vehicles are idle unless given a <see cref="ScriptedSeat"/>.
    /// </summary>
    public abstract class LabFixture
    {
        protected readonly List<GameObject> Created = new List<GameObject>();
        int _seat;

        [UnitySetUp]
        public IEnumerator SetUpGround()
        {
            Time.timeScale = 1f;
            MatchSetup.Configured = true;
            MatchSetup.HumanPlaying = false;
            MatchSetup.BotCount = 0;
            MatchSetup.RandomizeSeedOnStart = false;
            MatchSetup.Seed = 4242;
            MatchSetup.SpawnOrder.Clear();
            MatchSetup.Label = "playmode";
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TestGround";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(400f, 1f, 400f);
            Created.Add(ground);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDownAll()
        {
            Projectile.DespawnAll();
            foreach (var v in Vehicle.All.ToArray())
                if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            foreach (var go in Created)
                if (go != null) UnityEngine.Object.Destroy(go);
            Created.Clear();
            var mm = MatchManager.Instance;
            if (mm != null) UnityEngine.Object.Destroy(mm.gameObject);
            foreach (var d in UnityEngine.Object.FindObjectsByType<Destructible>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(d.gameObject);
            GrabLink.Clear();
            Teleporter.ResetStatics();
            yield return null;
        }

        protected Vehicle Spawn(string id, Vector3 pos, float yaw = 0f, ScriptedSeat seat = null)
        {
            var def = GameDatabase.Instance.Vehicle(id);
            Assert.IsNotNull(def, id);
            var v = VehicleFactory.Spawn(def, pos, Quaternion.Euler(0f, yaw, 0f), _seat++, false, Color.white, 1);
            var brain = v.GetComponent<BotBrain>();
            if (brain != null) UnityEngine.Object.Destroy(brain);
            v.Input = seat;
            return v;
        }

        protected GameObject Box(string name, Vector3 pos, Vector3 size, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = size;
            Created.Add(go);
            return go;
        }

        protected static IEnumerator Seconds(float s)
        {
            float end = Time.time + s;
            while (Time.time < end) yield return new WaitForFixedUpdate();
        }

        protected MatchManager Match(bool lab)
        {
            var go = new GameObject("Match");
            Created.Add(go);
            var mm = go.AddComponent<MatchManager>();
            mm.countdownSeconds = 0f;
            mm.labMode = lab;
            return mm;
        }
    }
}
