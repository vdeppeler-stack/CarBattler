using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>Shared helpers for generated placeholder scenes: persistent materials, blocks, ramps, spawns, pickups.</summary>
    public static class SceneBuilderUtil
    {
        public const string MaterialDir = "Assets/_Project/Materials";
        public const string SceneDir = "Assets/_Project/Scenes";

        public static readonly Color Ground = new Color(0.32f, 0.33f, 0.35f);
        public static readonly Color Wall = new Color(0.45f, 0.45f, 0.48f);
        public static readonly Color Cover = new Color(0.55f, 0.5f, 0.42f);
        public static readonly Color RampColor = new Color(0.6f, 0.55f, 0.3f);
        public static readonly Color Hazard = new Color(1f, 0.75f, 0.05f);
        public static readonly Color Danger = new Color(0.9f, 0.15f, 0.1f);

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Material Mat(string name, Color c)
        {
            if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;
            Directory.CreateDirectory(MaterialDir);
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            Cache[name] = m;
            return m;
        }

        public static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>Ramp rising along local +Z from bottom edge at pos to the given height.</summary>
        public static GameObject Ramp(Transform parent, string name, Vector3 bottomCenter, float width, float length, float height, float yaw, Material mat)
        {
            float slope = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            float hyp = Mathf.Sqrt(length * length + height * height);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var center = bottomCenter + rot * new Vector3(0f, height * 0.5f, length * 0.5f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center - rot * (Vector3.up * 0.25f);
            go.transform.localRotation = rot * Quaternion.Euler(-slope, 0f, 0f);
            go.transform.localScale = new Vector3(width, 0.5f, hyp);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        public static SpawnPoint Spawn(Transform parent, int index, Vector3 pos, Vector3 lookAt)
        {
            var go = new GameObject("Spawn_" + index);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var dir = lookAt - pos;
            dir.y = 0f;
            go.transform.localRotation = Quaternion.LookRotation(dir.normalized);
            var sp = go.AddComponent<SpawnPoint>();
            sp.index = index;
            return sp;
        }

        public static PickupSpawner Pickup(Transform parent, PickupKind kind, Vector3 pos, WeaponDefinition weapon = null)
        {
            var go = new GameObject(kind == PickupKind.Weapon && weapon != null ? "Pickup_" + weapon.id : "Pickup_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<SphereCollider>();
            var p = go.AddComponent<PickupSpawner>();
            p.kind = kind;
            p.weapon = weapon;
            return p;
        }

        public static void Lighting(Transform parent)
        {
            var sun = new GameObject("Sun");
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.6f;
            l.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        }

        public static MatchManager MatchRig(Transform parent, int defaultBots)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(parent, false);
            camGo.transform.position = new Vector3(0f, 20f, -30f);
            var cam = camGo.AddComponent<Camera>();
            cam.farClipPlane = 600f;
            cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ChaseCamera>();

            var mm = new GameObject("Match").AddComponent<MatchManager>();
            mm.transform.SetParent(parent, false);
            mm.defaultBotCount = defaultBots;
            var hud = new GameObject("HUD").AddComponent<MatchHud>();
            hud.transform.SetParent(parent, false);
            return mm;
        }

        public static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            Directory.CreateDirectory(SceneDir);
            string path = $"{SceneDir}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            AddToBuild(path);
            Debug.Log("[IS] Scene saved: " + path);
        }

        public static void AddToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
