using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    public static class MenuSceneBuilder
    {
        [MenuItem("Iron Syndicate/Build/Vehicle Select Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            new GameObject("VehicleSelect").AddComponent<VehicleSelectMenu>();
            SceneBuilderUtil.SaveScene(scene, "VehicleSelect");

            // Vehicle select is the entry point of a build.
            var scenes = EditorBuildSettings.scenes.ToList();
            var entry = scenes.First(s => s.path.EndsWith("/VehicleSelect.unity"));
            scenes.Remove(entry);
            scenes.Insert(0, entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
