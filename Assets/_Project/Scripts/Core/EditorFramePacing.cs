using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// Editor Play Mode only: the Game view ignores the quality VSync setting, so frame times swing with
    /// whatever else the editor draws (Scene view, Inspector, Hierarchy) and motion looks jerky. Capping at
    /// the monitor refresh rate gives even frame pacing. Players use VSync from the quality settings.
    /// </summary>
    static class EditorFramePacing
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            if (!Application.isEditor) return;
            int hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = hz >= 50 ? hz : 60;
        }
    }
}
