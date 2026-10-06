using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Typed, allocation-free view over the Controls input asset: the "Rover" map (driving and tools) and the "UI" map
    /// (<see cref="Menu"/>). Systems poll it from Update; nothing else in the game touches InputSystem devices directly,
    /// so rebinding and gamepad support live here.
    /// </summary>
    public sealed class InputReader : IDisposable
    {
        public const string RoverMapName = "Rover";

        /// <summary>Binding group of keyboard and mouse bindings in the Controls asset.</summary>
        public const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>Binding group of gamepad bindings in the Controls asset.</summary>
        public const string GamepadGroup = "Gamepad";

        private readonly InputActionMap _roverMap;
        private readonly InputAction _drive;
        private readonly InputAction _lookDelta;
        private readonly InputAction _lookRate;
        private readonly InputAction _ping;
        private readonly InputAction _excavate;
        private readonly InputAction _tether;
        private readonly InputAction _winch;
        private readonly Action<InputAction.CallbackContext> _onPerformed;

        /// <param name="actions">The Controls asset. Missing maps/actions throw immediately (wiring bug).</param>
        public InputReader(InputActionAsset actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            _roverMap = actions.FindActionMap(RoverMapName, throwIfNotFound: true);
            _drive = _roverMap.FindAction("Drive", throwIfNotFound: true);
            _lookDelta = _roverMap.FindAction("LookDelta", throwIfNotFound: true);
            _lookRate = _roverMap.FindAction("LookRate", throwIfNotFound: true);
            _ping = _roverMap.FindAction("Ping", throwIfNotFound: true);
            _excavate = _roverMap.FindAction("Excavate", throwIfNotFound: true);
            _tether = _roverMap.FindAction("Tether", throwIfNotFound: true);
            _winch = _roverMap.FindAction("Winch", throwIfNotFound: true);
            Menu = new MenuInput(actions.FindActionMap(MenuInput.MapName, throwIfNotFound: true));

            _onPerformed = OnPerformed;
            Track(_roverMap, true);
            Track(Menu.Map, true);
        }

        /// <summary>x = steer, y = throttle, each -1..1.</summary>
        public Vector2 Drive => Vector2.ClampMagnitude(_drive.ReadValue<Vector2>(), 1f);

        /// <summary>Mouse movement this frame in pixels. Do not multiply by deltaTime.</summary>
        public Vector2 LookDelta => _lookDelta.ReadValue<Vector2>();

        /// <summary>Gamepad look stick, -1..1. Multiply by deltaTime and a rate.</summary>
        public Vector2 LookRate => _lookRate.ReadValue<Vector2>();

        public bool PingPressed => _ping.WasPressedThisFrame();

        public bool ExcavateHeld => _excavate.IsPressed();

        public bool TetherHeld => _tether.IsPressed();

        public bool TetherPressed => _tether.WasPressedThisFrame();

        public bool TetherReleased => _tether.WasReleasedThisFrame();

        /// <summary>Reel in (+) / out (-), -1..1 per frame (scroll notch or held d-pad).</summary>
        public float Winch => _winch.ReadValue<float>();

        /// <summary>The "UI" map: pause, back, menu focus and cursor recapture. Enabled and disabled by the UI.</summary>
        public MenuInput Menu { get; }

        /// <summary>The device behind the last actuated action of either map (keyboard and mouse until then).</summary>
        public InputDeviceKind ActiveDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        /// <summary>True while the rover controls are live (false while the game is paused).</summary>
        public bool Enabled => _roverMap.enabled;

        /// <summary>Turns the rover controls on (the bootstrap does this once; the pause menu after resuming).</summary>
        public void Enable()
        {
            _roverMap.Enable();
        }

        /// <summary>Turns the rover controls off; the UI map is unaffected.</summary>
        public void Disable()
        {
            _roverMap.Disable();
        }

        /// <summary>
        /// Short name of the physical control bound to <paramref name="action"/> for <paramref name="device"/>, e.g.
        /// "E", "RMB", "A", "LT", "Scroll", "D-Pad" (an axis or a d-pad direction is named by its control, so a
        /// scroll wheel reads "Scroll", not "Scroll/Y"). Follows rebinding overrides. Empty when nothing is bound for
        /// that device. Allocates a string: call it when the label is needed and cache it, never per frame.
        /// </summary>
        public string GetBindingLabel(RoverAction action, InputDeviceKind device)
        {
            InputBinding mask = InputBinding.MaskByGroup(
                device == InputDeviceKind.Gamepad ? GamepadGroup : KeyboardMouseGroup);
            foreach (InputBinding binding in Resolve(action).bindings)
            {
                if (binding.isComposite || !mask.Matches(binding))
                {
                    continue;
                }

                return InputControlPath.ToHumanReadableString(PhysicalControlPath(binding.effectivePath),
                    InputControlPath.HumanReadableStringOptions.OmitDevice |
                    InputControlPath.HumanReadableStringOptions.UseShortNames);
            }

            return string.Empty;
        }

        public void Dispose()
        {
            Track(_roverMap, false);
            Track(Menu.Map, false);
            _roverMap.Disable();
            Menu.Disable();
        }

        private InputAction Resolve(RoverAction action)
        {
            switch (action)
            {
                case RoverAction.Ping:
                    return _ping;
                case RoverAction.Excavate:
                    return _excavate;
                case RoverAction.Tether:
                    return _tether;
                case RoverAction.Winch:
                    return _winch;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown rover action.");
            }
        }

        /// <summary>"&lt;Mouse&gt;/scroll/y" -> "&lt;Mouse&gt;/scroll": the device plus its first control.</summary>
        private static string PhysicalControlPath(string path)
        {
            int device = path.IndexOf('/');
            int control = device < 0 ? -1 : path.IndexOf('/', device + 1);
            return control < 0 ? path : path.Substring(0, control);
        }

        private void Track(InputActionMap map, bool track)
        {
            foreach (InputAction action in map.actions)
            {
                if (track)
                {
                    action.performed += _onPerformed;
                }
                else
                {
                    action.performed -= _onPerformed;
                }
            }
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            InputControl control = context.control;
            if (control == null || !control.IsActuated())
            {
                // A pass-through returning to rest (the mouse delta resetting each frame) says nothing about intent.
                return;
            }

            InputDevice device = control.device;
            if (device is Gamepad)
            {
                ActiveDevice = InputDeviceKind.Gamepad;
            }
            else if (device is Keyboard || device is Mouse)
            {
                ActiveDevice = InputDeviceKind.KeyboardMouse;
            }
        }
    }
}
