using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Turns generated vehicle models (ComfyUI TRELLIS.2 → Blender → FBX under Art/Vehicles) into body
    /// prefabs: facing +Z, origin on the ground, scaled to the definition's footprint, no colliders.
    /// Assigns them to the VehicleDefinitions. Vehicles without a model keep their primitive placeholder.
    /// </summary>
    public static class VehicleBodyImporter
    {
        const string ArtDir = "Assets/_Project/Art/Vehicles";
        const string PrefabDir = "Assets/_Project/Prefabs/Bodies";

        /// <summary>Yaw correction per model (degrees) after the automatic long-axis alignment, checked visually.</summary>
        static readonly Dictionary<string, float> Yaw = new Dictionary<string, float>
        {
        };

        /// <summary>Visual footprint relative to the collision box (wheels and armour stick out a little).</summary>
        const float Overhang = 1.15f;

        [MenuItem("Iron Syndicate/Build/Vehicle Bodies (generated models)")]
        public static void BuildAll()
        {
            var db = GameDatabase.Instance;
            Directory.CreateDirectory(PrefabDir);
            int built = 0;
            foreach (var def in db.vehicles)
            {
                if (def == null) continue;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtDir}/{def.id}.fbx");
                if (model == null) continue;
                ConfigureImporter($"{ArtDir}/{def.id}.fbx");
                def.bodyPrefab = BuildPrefab(def, model);
                EditorUtility.SetDirty(def);
                built++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[IS] Vehicle bodies built: {built}");
        }

        static void ConfigureImporter(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            bool dirty = false;
            if (imp.importAnimation) { imp.importAnimation = false; dirty = true; }
            if (imp.addCollider) { imp.addCollider = false; dirty = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                dirty = true;
            }
            if (imp.materialLocation != ModelImporterMaterialLocation.InPrefab)
            {
                imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }

        static GameObject BuildPrefab(VehicleDefinition def, GameObject model)
        {
            var root = new GameObject("Body_" + def.id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(root.transform, false);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            // Align the long horizontal axis with Z (except side-wheel styles, which are wider than long).
            var b = Bounds(inst);
            bool wideStyle = def.bodyStyle == BodyStyle.Wheel;
            float yaw = (b.size.x > b.size.z) != wideStyle ? 90f : 0f;
            if (Yaw.TryGetValue(def.id, out var extra)) yaw += extra;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            b = Bounds(inst);
            var target = def.bodySize * Overhang;
            float s = Mathf.Min(target.x / Mathf.Max(0.01f, b.size.x), target.z / Mathf.Max(0.01f, b.size.z));
            // Side-wheel colossus: giant wheels may overhang the collision box sideways.
            if (wideStyle) s = (def.bodySize.x + 1.6f) / Mathf.Max(0.01f, b.size.x);
            inst.transform.localScale = Vector3.one * s;

            b = Bounds(inst);
            inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

            foreach (var r in inst.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            string path = $"{PrefabDir}/{def.id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
