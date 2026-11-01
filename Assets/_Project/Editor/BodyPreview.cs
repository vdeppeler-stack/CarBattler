using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Renders body prefabs into a contact sheet (3/4 front view, +Z marked by a yellow bar, collision
    /// footprint in blue) so orientation and scale can be checked without entering Play Mode.
    /// Renderers register with the GPU-resident drawer at the end of a frame, so the capture runs a few
    /// editor ticks after the objects are placed. Poll <see cref="Done"/>.
    /// </summary>
    public static class BodyPreview
    {
        public static bool Done { get; private set; } = true;

        static readonly List<GameObject> Temp = new List<GameObject>();
        static List<VehicleDefinition> _defs;
        static string _out;
        static int _cell, _wait;
        static readonly Vector3 Origin = new Vector3(0f, 2000f, 0f);
        const float Spacing = 40f;

        public static string Render(string outPath, int cell = 320, string onlyId = null)
        {
            Cleanup();
            _defs = GameDatabase.Instance.vehicles.FindAll(d => d != null && d.bodyPrefab != null && (onlyId == null || d.id == onlyId));
            if (_defs.Count == 0) return "no bodies";
            _out = outPath;
            _cell = cell;
            for (int i = 0; i < _defs.Count; i++)
            {
                var def = _defs[i];
                var at = Origin + Vector3.right * Spacing * i;
                Temp.Add((GameObject)Object.Instantiate(def.bodyPrefab, at, Quaternion.identity));
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.transform.position = at + new Vector3(0f, 0.05f, def.bodySize.z * 0.75f + 0.5f);
                arrow.transform.localScale = new Vector3(0.3f, 0.1f, def.bodySize.z * 0.5f);
                arrow.GetComponent<Renderer>().sharedMaterial = VehicleFactory.Mat(Color.yellow);
                Temp.Add(arrow);
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.position = at + new Vector3(0f, 0.01f, 0f);
                box.transform.localScale = new Vector3(def.bodySize.x, 0.02f, def.bodySize.z);
                box.GetComponent<Renderer>().sharedMaterial = VehicleFactory.Mat(new Color(0.1f, 0.6f, 1f));
                Temp.Add(box);
            }
            var lightGo = new GameObject("PreviewLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            Temp.Add(lightGo);
            Done = false;
            _wait = 3;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return string.Join(", ", _defs.ConvertAll(d => d.id));
        }

        static void Tick()
        {
            if (--_wait > 0) return;
            EditorApplication.update -= Tick;
            try { Capture(); }
            finally { Cleanup(); Done = true; }
        }

        static void Capture()
        {
            // Two cells per vehicle: 3/4 front view and top view (front = up).
            int cols = Mathf.Min(4, _defs.Count * 2), rows = (_defs.Count * 2 + cols - 1) / cols;
            var sheet = new Texture2D(cols * _cell, rows * _cell, TextureFormat.RGB24, false);
            var rt = new RenderTexture(_cell, _cell, 24);
            var camGo = new GameObject("PreviewCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f);
            cam.targetTexture = rt;
            cam.farClipPlane = 200f;
            var tex = new Texture2D(_cell, _cell, TextureFormat.RGB24, false);
            for (int i = 0; i < _defs.Count; i++)
            {
                var def = _defs[i];
                var at = Origin + Vector3.right * Spacing * i;
                float d = Mathf.Max(def.bodySize.z, def.bodySize.x) * 1.6f + 3f;
                for (int view = 0; view < 2; view++)
                {
                    if (view == 0)
                    {
                        cam.transform.position = at + new Vector3(-d * 0.6f, d * 0.45f, d * 0.75f);
                        cam.transform.LookAt(at + Vector3.up * def.bodySize.y * 0.5f);
                    }
                    else
                    {
                        cam.transform.position = at + Vector3.up * d * 1.4f + Vector3.forward * 0.5f;
                        cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                    }
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, _cell, _cell), 0, 0);
                    tex.Apply();
                    int k = i * 2 + view;
                    int cx = k % cols, cy = rows - 1 - k / cols;
                    sheet.SetPixels(cx * _cell, cy * _cell, _cell, _cell, tex.GetPixels());
                }
            }
            RenderTexture.active = null;
            sheet.Apply();
            File.WriteAllBytes(_out, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            rt.Release();
        }

        static void Cleanup()
        {
            foreach (var go in Temp)
                if (go != null) Object.DestroyImmediate(go);
            Temp.Clear();
        }
    }
}
