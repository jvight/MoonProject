using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Typed, allocation-free view over the Controls input asset: the "Rover" map (driving and tools) and the "UI"
    /// map (<see cref="Menu"/>). Systems poll it from Update; nothing else in the game touches InputSystem devices
    /// directly, so rebinding and gamepad support live here.
    /// </summary>
    public sealed class InputReader : IDisposable
    {
        public const string RoverMapName = "Rover";

        private readonly InputActionMap _roverMap;
        private readonly InputAction _drive;
        private readonly InputAction _lookDelta;
        private readonly InputAction _lookRate;
        private readonly InputAction _excavate;
        private readonly InputAction _tether;
        private readonly InputAction _winch;
        private readonly InputAction _jump;
        private readonly InputAction _recenter;
        private readonly InputAction _lights;
        private readonly InputAction _build;
        private readonly InputAction _deconstruct;
        private readonly Action<InputAction.CallbackContext> _onPerformed;
        private object _modeOwner;
        private bool _driveShared;

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
            _excavate = _roverMap.FindAction("Excavate", throwIfNotFound: true);
            _tether = _roverMap.FindAction("Tether", throwIfNotFound: true);
            _winch = _roverMap.FindAction("Winch", throwIfNotFound: true);
            _jump = _roverMap.FindAction("Jump", throwIfNotFound: true);
            _recenter = _roverMap.FindAction("Recenter", throwIfNotFound: true);
            _lights = _roverMap.FindAction("Lights", throwIfNotFound: true);
            _build = _roverMap.FindAction("Build", throwIfNotFound: true);
            _deconstruct = _roverMap.FindAction("Deconstruct", throwIfNotFound: true);
            Menu = new MenuInput(actions.FindActionMap(MenuInput.MapName, throwIfNotFound: true));

            _onPerformed = OnPerformed;
            Track(_roverMap, true);
            Track(Menu.Map, true);
        }

        /// <summary>True when a modal gameplay mode (such as Build Mode) owns Drive/Excavate/Winch input.</summary>
        public bool HasModeOwner => _modeOwner != null;

        /// <summary>Claims or releases exclusive ownership of Drive, Excavate, and Winch for a modal gameplay state.</summary>
        public void SetModeOwner(object owner, bool active)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (active)
            {
                _driveShared = _driveShared && ReferenceEquals(_modeOwner, owner);
                _modeOwner = owner;
            }
            else if (ReferenceEquals(_modeOwner, owner))
            {
                _modeOwner = null;
                _driveShared = false;
            }
        }

        /// <summary>
        /// The active mode owner lets Drive through to 07 (<paramref name="shared"/>) while it keeps Excavate and
        /// Winch: build mode's placing, where 07 drives the ghost to its spot. Ignored for anyone but the owner.
        /// </summary>
        public void ShareDrive(object owner, bool shared)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (ReferenceEquals(_modeOwner, owner))
            {
                _driveShared = shared;
            }
        }

        /// <summary>
        /// x = steer, y = throttle, each -1..1. Consumed (zero) while a mode owner is active, unless it shares Drive.
        /// </summary>
        public Vector2 Drive => _modeOwner != null && !_driveShared
            ? Vector2.zero
            : Vector2.ClampMagnitude(_drive.ReadValue<Vector2>(), 1f);

        /// <summary>Reads Drive for the active mode owner.</summary>
        public Vector2 ModeDrive(object owner) =>
            ReferenceEquals(_modeOwner, owner) ? Vector2.ClampMagnitude(_drive.ReadValue<Vector2>(), 1f) : Vector2.zero;

        /// <summary>Mouse movement this frame in pixels. Do not multiply by deltaTime.</summary>
        public Vector2 LookDelta => _lookDelta.ReadValue<Vector2>();

        /// <summary>Gamepad look stick, -1..1. Multiply by deltaTime and a rate.</summary>
        public Vector2 LookRate => _lookRate.ReadValue<Vector2>();

        public bool ExcavateHeld => _modeOwner == null && _excavate.IsPressed();

        public bool ExcavatePressed => _modeOwner == null && _excavate.WasPressedThisFrame();

        /// <summary>Reads ExcavateHeld for the active mode owner.</summary>
        public bool ModeExcavateHeld(object owner) => ReferenceEquals(_modeOwner, owner) && _excavate.IsPressed();

        /// <summary>Reads ExcavatePressed for the active mode owner.</summary>
        public bool ModeExcavatePressed(object owner) => ReferenceEquals(_modeOwner, owner) && _excavate.WasPressedThisFrame();

        public bool TetherHeld => _tether.IsPressed();

        public bool TetherPressed => _tether.WasPressedThisFrame();

        public bool TetherReleased => _tether.WasReleasedThisFrame();

        /// <summary>Reel in (+) / out (-), -1..1 per frame (scroll notch or held d-pad). Consumed while a mode owner is active.</summary>
        public float Winch => _modeOwner != null ? 0f : _winch.ReadValue<float>();

        /// <summary>Reads Winch for the active mode owner.</summary>
        public float ModeWinch(object owner) => ReferenceEquals(_modeOwner, owner) ? _winch.ReadValue<float>() : 0f;

        /// <summary>Hover-Jump charge button (hold to charge, release to leap; only once the upgrade is owned).</summary>
        public bool JumpHeld => _jump.IsPressed();

        public bool JumpPressed => _jump.WasPressedThisFrame();

        public bool JumpReleased => _jump.WasReleasedThisFrame();

        /// <summary>The camera's recenter button (middle mouse, the right stick's click) went down this frame.</summary>
        public bool RecenterPressed => _recenter.WasPressedThisFrame();

        /// <summary>The player switched 07's lamps on or off this frame (L, pad L3), in any mode.</summary>
        public bool LightsPressed => _lights.WasPressedThisFrame();

        /// <summary>The build-mode button (B on keyboard, Y / buttonNorth on gamepad) went down this frame.</summary>
        public bool BuildPressed => _build.WasPressedThisFrame();

        /// <summary>The deconstruct button (X on keyboard, RB / rightShoulder on gamepad) is held.</summary>
        public bool DeconstructHeld => _deconstruct.IsPressed();

        public bool DeconstructPressed => _deconstruct.WasPressedThisFrame();

        /// <summary>
        /// The "UI" map: pause, back, menu focus and cursor recapture. Enabled and disabled by the UI.
        /// </summary>
        public MenuInput Menu { get; }

        /// <summary>The device behind the last actuated action of either map (keyboard and mouse until then).</summary>
        public InputDeviceKind ActiveDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        /// <summary>True while the rover controls are live (false while the game is paused).</summary>
        public bool Enabled => _roverMap.enabled;

        /// <summary>
        /// Turns the rover controls on (the bootstrap does this once; the pause menu after resuming).
        /// </summary>
        public void Enable()
        {
            _roverMap.Enable();
        }

        /// <summary>Turns the rover controls off; the UI map is unaffected.</summary>
        public void Disable()
        {
            _modeOwner = null;
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
            return BindingLabels.For(Resolve(action), device);
        }

        /// <summary>
        /// Stable id of the physical control bound to <paramref name="action"/> for <paramref name="device"/>, e.g.
        /// "keyboard.space", "mouse.rightbutton", "gamepad.buttonsouth", for a localized keycap word ("Space", "RMB",
        /// "A"). Follows rebinding overrides. Empty when nothing is bound for that device. Allocates a string: cache
        /// it.
        /// </summary>
        public string GetBindingControl(RoverAction action, InputDeviceKind device)
        {
            return BindingLabels.ControlId(Resolve(action), device);
        }

        /// <summary>
        /// Adds to <paramref name="into"/> the short label of every control bound to <paramref name="action"/> for
        /// <paramref name="device"/>, in binding order: Excavate reads "E" and "LMB" on keyboard and mouse, Winch
        /// "Scroll", "Z" and "C". For a prompt that names all the ways to do one thing. Allocates strings: cache them.
        /// </summary>
        public void GetBindingLabels(RoverAction action, InputDeviceKind device, List<string> into)
        {
            BindingLabels.AllLabels(Resolve(action), device, into);
        }

        /// <summary>
        /// Adds to <paramref name="into"/> the stable control id ("keyboard.e", "mouse.leftbutton") of every control
        /// bound to <paramref name="action"/> for <paramref name="device"/>, in binding order, for localized keycaps.
        /// </summary>
        public void GetBindingControls(RoverAction action, InputDeviceKind device, List<string> into)
        {
            BindingLabels.AllControlIds(Resolve(action), device, into);
        }

        public void Dispose()
        {
            _modeOwner = null;
            Track(_roverMap, false);
            Track(Menu.Map, false);
            _roverMap.Disable();
            Menu.Disable();
        }

        private InputAction Resolve(RoverAction action)
        {
            switch (action)
            {
                case RoverAction.Excavate:
                    return _excavate;
                case RoverAction.Tether:
                    return _tether;
                case RoverAction.Winch:
                    return _winch;
                case RoverAction.Jump:
                    return _jump;
                case RoverAction.Recenter:
                    return _recenter;
                case RoverAction.Lights:
                    return _lights;
                case RoverAction.Build:
                    return _build;
                case RoverAction.Deconstruct:
                    return _deconstruct;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown rover action.");
            }
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
