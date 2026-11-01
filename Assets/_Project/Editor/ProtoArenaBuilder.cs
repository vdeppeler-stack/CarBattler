using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static IronSyndicate.EditorTools.SceneBuilderUtil;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// First work order: one flat driving surface with walls, a few ramps and covers, start points,
    /// pickups and a Tesla reactor - enough to play a round with one vehicle and bots to the result screen.
    /// </summary>
    public static class ProtoArenaBuilder
    {
        public const string SceneName = "Proto_Arena";

        [MenuItem("Iron Syndicate/Build/Proto Arena Scene")]
        public static void Build()
        {
            var db = GameDatabase.Instance != null ? GameDatabase.Instance : DataBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var env = new GameObject("Environment").transform;
            Lighting(env);
            var ground = Mat("Ground", SceneBuilderUtil.Ground);
            var wall = Mat("Wall", SceneBuilderUtil.Wall);
            var cover = Mat("Cover", SceneBuilderUtil.Cover);
            var ramp = Mat("Ramp", SceneBuilderUtil.RampColor);
            var hazard = Mat("Hazard", SceneBuilderUtil.Hazard);

            const float half = 70f;
            Block(env, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f, 1f, half * 2f), ground);
            Block(env, "Wall_N", new Vector3(0f, 3f, half + 1f), new Vector3(half * 2f + 4f, 6f, 2f), wall);
            Block(env, "Wall_S", new Vector3(0f, 3f, -half - 1f), new Vector3(half * 2f + 4f, 6f, 2f), wall);
            Block(env, "Wall_E", new Vector3(half + 1f, 3f, 0f), new Vector3(2f, 6f, half * 2f), wall);
            Block(env, "Wall_W", new Vector3(-half - 1f, 3f, 0f), new Vector3(2f, 6f, half * 2f), wall);

            // Cover blocks in a loose ring, plus two low walls to shoot over with lobbed weapons.
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f + 22.5f;
                var p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 38f;
                Block(env, "Cover_" + i, new Vector3(p.x, 2f, p.z), new Vector3(6f, 4f, 3f), cover, a);
            }
            Block(env, "LowWall_E", new Vector3(22f, 0.6f, 0f), new Vector3(2f, 1.2f, 16f), hazard);
            Block(env, "LowWall_W", new Vector3(-22f, 0.6f, 0f), new Vector3(2f, 1.2f, 16f), hazard);

            // Raised deck with two ramps.
            Block(env, "Deck", new Vector3(0f, 2f, 52f), new Vector3(30f, 4f, 14f), cover);
            Ramp(env, "Ramp_Deck_W", new Vector3(-22f, 0f, 52f), 8f, 14f, 4f, 90f, ramp);
            Ramp(env, "Ramp_Deck_E", new Vector3(22f, 0f, 52f), 8f, 14f, 4f, -90f, ramp);
            // Jump ramps.
            Ramp(env, "Jump_S", new Vector3(0f, 0f, -45f), 8f, 10f, 2.2f, 0f, ramp);
            Ramp(env, "Jump_SE", new Vector3(45f, 0f, -20f), 8f, 10f, 2.2f, -60f, ramp);

            // Tesla reactor in the middle.
            var reactor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reactor.name = "TeslaReactor";
            reactor.transform.SetParent(env, false);
            reactor.transform.localPosition = new Vector3(0f, 3f, 0f);
            reactor.transform.localScale = new Vector3(4f, 3f, 4f);
            reactor.GetComponent<Renderer>().sharedMaterial = Mat("Reactor", new Color(0.3f, 0.75f, 1f));
            reactor.AddComponent<TeslaReactor>().radius = 22f;

            var spawns = new GameObject("SpawnPoints").transform;
            for (int i = 0; i < 6; i++)
            {
                var p = Quaternion.Euler(0f, i * 60f + 10f, 0f) * Vector3.forward * 55f;
                p.y = 0.2f;
                if (p.z > 40f) p = new Vector3(p.x, 0.2f, 34f + i);
                Spawn(spawns, i, p, Vector3.zero);
            }

            var pickups = new GameObject("Pickups").transform;
            int n = 0;
            foreach (var w in db.weapons)
            {
                if (w.environmentOnly && w.behaviour != WeaponBehaviour.TeslaArc) continue;
                var p = Quaternion.Euler(0f, n * (360f / db.weapons.Count), 0f) * Vector3.forward * 26f;
                Pickup(pickups, PickupKind.Weapon, new Vector3(p.x, 0f, p.z), w);
                n++;
            }
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(60f, 0f, 60f));
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(-60f, 0f, -60f));
            Pickup(pickups, PickupKind.CoreOvercharge, new Vector3(0f, 4f, 54f));
            Pickup(pickups, PickupKind.NitroCell, new Vector3(-60f, 0f, 60f));
            Pickup(pickups, PickupKind.NitroCell, new Vector3(60f, 0f, -60f));

            MatchRig(null, 1);
            SaveScene(scene, SceneName);
        }
    }
}
