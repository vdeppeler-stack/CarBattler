using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSyndicate
{
    /// <summary>
    /// Clears static registries on play-mode entry (works with domain reload disabled) and whenever a scene
    /// unloads, so pooled objects destroyed with the scene are never reused.
    /// </summary>
    static class RuntimeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnPlayModeEnter()
        {
            ClearScenes();
            MatchSetup.Configured = false;
            MatchSetup.RandomizeSeedOnStart = true;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene _) => ClearScenes();

        static void ClearScenes()
        {
            Vehicle.All.Clear();
            SpawnPoint.All.Clear();
            PickupSpawner.All.Clear();
            TeslaReactor.All.Clear();
            Destructible.All.Clear();
            Destructible.ResetStatics();
            Teleporter.ResetStatics();
            GrabLink.Clear();
            Projectile.ResetPools();
        }
    }
}
