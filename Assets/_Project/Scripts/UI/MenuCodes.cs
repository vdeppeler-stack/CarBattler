using System.Collections.Generic;

namespace IronSyndicate
{
    public enum MenuKey { Up, Down, Left, Right, L1, R1, Triangle }

    /// <summary>
    /// Unlock codes, recognised only inside the menu that owns them. Own sequences (not taken from the GDD).
    /// Keyboard: arrows, Q = L1, E = R1, T = Triangle.
    /// </summary>
    public sealed class MenuCodes
    {
        public struct Code
        {
            public string Id;
            public MenuKey[] Keys;
        }

        public static readonly Code[] VehicleSelect =
        {
            new Code { Id = "jester_van", Keys = new[] { MenuKey.Left, MenuKey.Left, MenuKey.Right, MenuKey.Right, MenuKey.L1, MenuKey.R1 } },
            new Code { Id = "leviathan_apc", Keys = new[] { MenuKey.Down, MenuKey.Up, MenuKey.Down, MenuKey.Up, MenuKey.R1, MenuKey.R1 } },
        };

        readonly Code[] _codes;
        readonly List<MenuKey> _buffer = new List<MenuKey>();
        float _last;
        public float maxGap = 1.0f;

        public MenuCodes(Code[] codes) => _codes = codes;

        /// <summary>Returns the id of a completed code or null. Wrong, shortened or reordered input never matches.</summary>
        public string Push(MenuKey key, float time)
        {
            if (time - _last > maxGap) _buffer.Clear();
            _last = time;
            _buffer.Add(key);
            foreach (var c in _codes)
            {
                if (_buffer.Count < c.Keys.Length) continue;
                bool match = true;
                for (int i = 0; i < c.Keys.Length; i++)
                    if (_buffer[_buffer.Count - c.Keys.Length + i] != c.Keys[i]) { match = false; break; }
                if (!match) continue;
                _buffer.Clear();
                return c.Id;
            }
            if (_buffer.Count > 12) _buffer.RemoveAt(0);
            return null;
        }
    }
}
