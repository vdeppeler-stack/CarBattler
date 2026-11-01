using UnityEngine;
using UnityEngine.InputSystem;

namespace IronSyndicate
{
    /// <summary>
    /// Keyboard/mouse (laptop test layout) plus an optional gamepad for one seat. Edges are latched in Update
    /// and consumed exactly once by the next physics step. Only fresh direction presses feed the combo buffer.
    /// </summary>
    public class PlayerSeatInput : MonoBehaviour, ISeatInput
    {
        public bool useKeyboard = true;
        [System.NonSerialized] public Gamepad gamepad;

        readonly ComboBuffer _combo = new ComboBuffer();
        bool _weaponEdge, _specialEdge;
        int _cycleEdge;
        Maneuver _maneuver;

        public ComboBuffer Combo => _combo;

        Gamepad Pad => gamepad ?? (useKeyboard ? Gamepad.current : null);

        void Update()
        {
            var kb = useKeyboard ? Keyboard.current : null;
            var mouse = useKeyboard ? Mouse.current : null;
            var pad = Pad;
            float t = Time.time;

            bool startHeld = pad != null && pad.startButton.isPressed;
            bool primaryHeld = (mouse != null && mouse.leftButton.isPressed) || (pad != null && pad.rightTrigger.isPressed);

            if (!startHeld)
            {
                if (Pressed(kb?.wKey, kb?.upArrowKey, pad?.dpad.up, pad?.leftStick.up)) Push(ComboDir.Up, t, primaryHeld);
                if (Pressed(kb?.sKey, kb?.downArrowKey, pad?.dpad.down, pad?.leftStick.down)) Push(ComboDir.Down, t, primaryHeld);
                if (Pressed(kb?.aKey, kb?.leftArrowKey, pad?.dpad.left, pad?.leftStick.left)) Push(ComboDir.Left, t, primaryHeld);
                if (Pressed(kb?.dKey, kb?.rightArrowKey, pad?.dpad.right, pad?.leftStick.right)) Push(ComboDir.Right, t, primaryHeld);
            }
            else _combo.Clear();

            var timedOut = _combo.Tick(t);
            if (timedOut != Maneuver.None) _maneuver = timedOut;

            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (pad != null && pad.leftTrigger.wasPressedThisFrame)) _weaponEdge = true;
            if ((kb != null && kb.fKey.wasPressedThisFrame) || (pad != null && pad.rightStickButton.wasPressedThisFrame)) _specialEdge = true;
            if ((kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) _cycleEdge = -1;
            if ((kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) _cycleEdge = 1;
        }

        void Push(ComboDir dir, float t, bool primaryHeld)
        {
            var m = _combo.Push(dir, t, primaryHeld);
            if (m != Maneuver.None) _maneuver = m;
        }

        static bool Pressed(params UnityEngine.InputSystem.Controls.ButtonControl[] controls)
        {
            foreach (var c in controls)
                if (c != null && c.wasPressedThisFrame) return true;
            return false;
        }

        static bool Held(params UnityEngine.InputSystem.Controls.ButtonControl[] controls)
        {
            foreach (var c in controls)
                if (c != null && c.isPressed) return true;
            return false;
        }

        public VehicleCommand ReadCommand(float time)
        {
            var kb = useKeyboard ? Keyboard.current : null;
            var mouse = useKeyboard ? Mouse.current : null;
            var pad = Pad;

            float steer = 0f;
            if (Held(kb?.dKey, kb?.rightArrowKey)) steer += 1f;
            if (Held(kb?.aKey, kb?.leftArrowKey)) steer -= 1f;
            if (pad != null)
            {
                steer += pad.leftStick.x.ReadValue();
                steer += pad.dpad.x.ReadValue();
            }

            var cmd = new VehicleCommand
            {
                Steer = Mathf.Clamp(steer, -1f, 1f),
                Throttle = Held(kb?.wKey, kb?.upArrowKey, pad?.buttonWest, pad?.dpad.up) ? 1f : 0f,
                Brake = Held(kb?.sKey, kb?.downArrowKey, pad?.buttonEast, pad?.dpad.down) ? 1f : 0f,
                Drift = Held(kb?.spaceKey, pad?.buttonSouth),
                Boost = Held(kb?.leftShiftKey, kb?.rightShiftKey, pad?.buttonNorth),
                PrimaryFire = (mouse != null && mouse.leftButton.isPressed) || (pad != null && pad.rightTrigger.isPressed),
                WeaponFire = _weaponEdge,
                Special = _specialEdge,
                WeaponCycle = _cycleEdge,
                Maneuver = _maneuver
            };
            if (pad != null && pad.startButton.isPressed) cmd.Throttle = cmd.Brake = 0f;

            _weaponEdge = _specialEdge = false;
            _cycleEdge = 0;
            _maneuver = Maneuver.None;
            return cmd;
        }
    }
}
