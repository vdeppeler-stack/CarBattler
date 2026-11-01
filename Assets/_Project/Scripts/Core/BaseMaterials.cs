using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Template materials for runtime-created colours. Loaded from Resources so their shaders are part of
    /// every build; Shader.Find alone returns null in a player when no scene material uses the shader.
    /// The assets are created by the data builder (Iron Syndicate/Build/Data).
    /// </summary>
    public static class BaseMaterials
    {
        public const string LitPath = "IronSyndicate/Materials/LitBase";
        public const string UnlitPath = "IronSyndicate/Materials/UnlitBase";

        static Material _lit, _unlit;

        public static Material Lit => _lit != null ? _lit : _lit = Load(LitPath, "Universal Render Pipeline/Lit");
        public static Material Unlit => _unlit != null ? _unlit : _unlit = Load(UnlitPath, "Universal Render Pipeline/Unlit");

        static Material Load(string path, string shaderName)
        {
            var m = Resources.Load<Material>(path);
            if (m != null) return m;
            Debug.LogWarning($"[IS] Base material {path} missing, falling back to Shader.Find");
            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            return new Material(shader);
        }
    }
}
