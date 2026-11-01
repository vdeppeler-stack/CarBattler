using System.Collections.Generic;

namespace IronSyndicate
{
    /// <summary>
    /// Hand-over between vehicle select, lab and match. Every entry point (menu, lab, balancing runner)
    /// fills this and loads the same match scene.
    /// </summary>
    public static class MatchSetup
    {
        /// <summary>False until a menu or the lab filled the setup; the scene's own defaults apply until then.</summary>
        public static bool Configured;
        public static string PlayerVehicleId = "goliath_wheel";
        public static bool HumanPlaying = true;
        public static readonly List<string> BotVehicleIds = new List<string>();
        public static int BotCount = 4;
        public static int Seed = 1234;
        public static bool RandomizeSeedOnStart = true;
        public static string Label = "match";
        /// <summary>Fixed spawn indices (lab); empty = seeded shuffle over free spawns.</summary>
        public static readonly List<int> SpawnOrder = new List<int>();
        /// <summary>Lab/developer build: every vehicle selectable without unlocking.</summary>
        public static bool RosterUnlocked = true;

        /// <summary>Selectable arenas: scene name and display name.</summary>
        public static readonly (string scene, string title)[] Arenas =
        {
            ("Tectonic_Wreck", "Tectonic Wreck"),
            ("Proto_Arena", "Proto-Arena"),
        };
        public static int ArenaIndex;
    }
}
