using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Keeps Play Mode advancing while the editor window is unfocused (MCP-driven lab runs), and offers
    /// <see cref="RunFrames"/> to fast-forward a fixed number of frames. Frames are stepped from the editor
    /// update loop, never from inside the player loop.
    /// Toggle: Iron Syndicate/Lab/Tick Play Mode In Background.
    /// </summary>
    [InitializeOnLoad]
    public static class BackgroundPlayLoop
    {
        const string Pref = "IronSyndicate.BackgroundPlayLoop";
        const string MenuPath = "Iron Syndicate/Lab/Tick Play Mode In Background";
        const int MaxStepsPerTick = 40;

        static double _last;
        static bool _autoPaused;
        static int _pendingFrames;

        static BackgroundPlayLoop()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += _ => { _pendingFrames = 0; _autoPaused = false; };
        }

        public static int PendingFrames => _pendingFrames;

        /// <summary>Fast-forward: steps this many frames as quickly as the editor allows.</summary>
        public static void RunFrames(int frames) => _pendingFrames += Mathf.Max(0, frames);

        static bool Enabled
        {
            get => EditorPrefs.GetBool(Pref, true);
            set => EditorPrefs.SetBool(Pref, value);
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - _last;
            _last = now;
            if (!EditorApplication.isPlaying) return;

            if (_pendingFrames > 0)
            {
                if (!EditorApplication.isPaused) { EditorApplication.isPaused = true; _autoPaused = true; }
                int n = Mathf.Min(_pendingFrames, MaxStepsPerTick);
                for (int i = 0; i < n; i++) EditorApplication.Step();
                _pendingFrames -= n;
                return;
            }

            bool active = InternalEditorUtility.isApplicationActive;
            if (active || !Enabled)
            {
                if (_autoPaused) { EditorApplication.isPaused = false; _autoPaused = false; }
                return;
            }
            if (EditorApplication.isPaused && !_autoPaused) return;

            EditorApplication.isPaused = true;
            _autoPaused = true;
            int steps = Mathf.Clamp(Mathf.RoundToInt((float)(elapsed / Time.fixedDeltaTime)), 1, 5);
            for (int i = 0; i < steps; i++) EditorApplication.Step();
        }

        [MenuItem(MenuPath)]
        static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
