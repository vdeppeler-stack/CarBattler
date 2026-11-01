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
                def.bodyPrefab = BuildPrefab(def, model, BuildMaterial(def.id));
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
            // Materials are built here from the converted textures, not taken from the FBX.
            if (imp.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }

        /// <summary>
        /// URP Lit from the Blender output: base colour (sRGB), normal map, metallic/smoothness (R/A, linear)
        /// and occlusion (linear). Wrong importer types here produce the speckled, plastic look.
        /// </summary>
        static Material BuildMaterial(string id)
        {
            string tex = $"{ArtDir}/Textures/{id}";
            var baseMap = Texture($"{tex}_BaseColor.png", TextureImporterType.Default, true, 2048);
            var normal = Texture($"{tex}_Normal.png", TextureImporterType.NormalMap, false, 2048);
            var ms = Texture($"{tex}_MS.png", TextureImporterType.Default, false, 1024);
            var ao = Texture($"{tex}_AO.png", TextureImporterType.Default, false, 1024);

            Directory.CreateDirectory($"{ArtDir}/Materials");
            string path = $"{ArtDir}/Materials/{id}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", baseMap);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.SetTexture("_MetallicGlossMap", ms);
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_Smoothness", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetTexture("_OcclusionMap", ao);
            m.SetFloat("_OcclusionStrength", 1f);
            SetKeyword(m, "_NORMALMAP", normal != null);
            SetKeyword(m, "_METALLICSPECGLOSSMAP", ms != null);
            SetKeyword(m, "_OCCLUSIONMAP", ao != null);
            if (ms == null) { m.SetFloat("_Metallic", 0.3f); m.SetFloat("_Smoothness", 0.35f); }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetKeyword(Material m, string kw, bool on)
        {
            if (on) m.EnableKeyword(kw);
            else m.DisableKeyword(kw);
        }

        static Texture2D Texture(string path, TextureImporterType type, bool srgb, int maxSize)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            if (imp.textureType != type || imp.sRGBTexture != srgb || imp.maxTextureSize != maxSize)
            {
                imp.textureType = type;
                imp.sRGBTexture = srgb;
                imp.maxTextureSize = maxSize;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static GameObject BuildPrefab(VehicleDefinition def, GameObject model, Material material)
        {
            var root = new GameObject("Body_" + def.id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(root.transform, false);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }

            // Align the long horizontal axis with Z (except side-wheel styles, which are wider than long).
            var b = Bounds(inst);
            bool wideStyle = def.bodyStyle == BodyStyle.Wheel;
            float yaw = (b.size.x > b.size.z) != wideStyle ? 90f : 0f;
            if (Yaw.TryGetValue(def.id, out var extra)) yaw += extra;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            b = Bounds(inst);
            var target = def.bodySize * Overhang;
            // Match the length; wide tops (canopies, plows) may overhang the collision box up to 1.7x its width.
            float s = Mathf.Min(target.z / Mathf.Max(0.01f, b.size.z), def.bodySize.x * 1.7f / Mathf.Max(0.01f, b.size.x));
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
