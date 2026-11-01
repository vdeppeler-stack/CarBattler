using System.Collections.Generic;

namespace IronSyndicate
{
    public enum ComboDir { Up, Down, Left, Right }

    public sealed class ComboDefinition
    {
        public readonly Maneuver Maneuver;
        public readonly ComboDir[] Sequence;
        public readonly bool RequiresPrimaryHeld;

        public ComboDefinition(Maneuver maneuver, bool requiresPrimaryHeld, params ComboDir[] sequence)
        {
            Maneuver = maneuver;
            Sequence = sequence;
            RequiresPrimaryHeld = requiresPrimaryHeld;
        }
    }

    /// <summary>
    /// Turns fresh direction presses into maneuvers. Longer sequences win over their prefix:
    /// a complete prefix is held as pending and only fires on timeout or when the next press
    /// rules out every longer sequence.
    /// </summary>
    public sealed class ComboBuffer
    {
        public const float DefaultMaxGap = 0.30f;

        static readonly ComboDir U = ComboDir.Up, D = ComboDir.Down, L = ComboDir.Left, R = ComboDir.Right;

        public static readonly ComboDefinition[] Default =
        {
            new ComboDefinition(Maneuver.ThrusterHop, false, D, D, U),
            new ComboDefinition(Maneuver.AegisBarrier, false, L, D, R),
            new ComboDefinition(Maneuver.QuickMine, false, R, D, L),
            new ComboDefinition(Maneuver.ThermiteBurst, false, D, L, U),
            new ComboDefinition(Maneuver.CryoPulse, false, D, R, U),
            new ComboDefinition(Maneuver.PhantomCloak, false, U, L, D, R),
            new ComboDefinition(Maneuver.RearSalvo, false, U, D, D),
            new ComboDefinition(Maneuver.RearCryoPulse, false, U, D, D, D, R, U),
            new ComboDefinition(Maneuver.RearThermite, false, U, D, D, D, L, U),
            new ComboDefinition(Maneuver.CerberusBarrage, true, R, R, L, L),
            new ComboDefinition(Maneuver.ScrapToRepair, false, L, U, R, D, L, U, R, D),
        };

        readonly ComboDefinition[] _combos;
        readonly float _maxGap;
        readonly List<ComboDir> _buffer = new List<ComboDir>(8);
        float _lastPress = float.NegativeInfinity;
        Maneuver _pending;
        bool _primaryHeld;

        public ComboBuffer(ComboDefinition[] combos = null, float maxGap = DefaultMaxGap)
        {
            _combos = combos ?? Default;
            _maxGap = maxGap;
        }

        public int Count => _buffer.Count;

        public void Clear()
        {
            _buffer.Clear();
            _pending = Maneuver.None;
        }

        /// <summary>Call every frame. Fires a pending prefix once its follow-up window has passed.</summary>
        public Maneuver Tick(float time)
        {
            if (_buffer.Count == 0 || time - _lastPress <= _maxGap) return Maneuver.None;
            var fired = _pending;
            Clear();
            return fired;
        }

        public Maneuver Push(ComboDir dir, float time, bool primaryHeld)
        {
            var timedOut = Tick(time);
            _primaryHeld = primaryHeld;
            _lastPress = time;
            _buffer.Add(dir);

            if (!IsPrefixOfAny(_buffer))
            {
                var fired = _pending != Maneuver.None ? _pending : timedOut;
                _pending = Maneuver.None;
                if (fired != Maneuver.None)
                {
                    _buffer.Clear();
                    _buffer.Add(dir);
                }
                while (_buffer.Count > 0 && !IsPrefixOfAny(_buffer)) _buffer.RemoveAt(0);
                if (fired != Maneuver.None) return fired;
            }
            else if (timedOut != Maneuver.None)
            {
                return timedOut;
            }

            var exact = FindExact(_buffer);
            if (exact == Maneuver.None) return Maneuver.None;
            if (HasLonger(_buffer))
            {
                _pending = exact;
                return Maneuver.None;
            }
            Clear();
            return exact;
        }

        bool Allowed(ComboDefinition c) => !c.RequiresPrimaryHeld || _primaryHeld;

        bool IsPrefixOfAny(List<ComboDir> buf)
        {
            foreach (var c in _combos)
                if (Allowed(c) && StartsWith(c.Sequence, buf)) return true;
            return false;
        }

        bool HasLonger(List<ComboDir> buf)
        {
            foreach (var c in _combos)
                if (Allowed(c) && c.Sequence.Length > buf.Count && StartsWith(c.Sequence, buf)) return true;
            return false;
        }

        Maneuver FindExact(List<ComboDir> buf)
        {
            foreach (var c in _combos)
                if (Allowed(c) && c.Sequence.Length == buf.Count && StartsWith(c.Sequence, buf)) return c.Maneuver;
            return Maneuver.None;
        }

        static bool StartsWith(ComboDir[] seq, List<ComboDir> buf)
        {
            if (buf.Count > seq.Length) return false;
            for (int i = 0; i < buf.Count; i++)
                if (seq[i] != buf[i]) return false;
            return true;
        }
    }
}
