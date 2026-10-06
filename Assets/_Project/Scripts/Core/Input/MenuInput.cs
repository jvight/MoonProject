using System;
using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Typed, allocation-free view over the Controls asset's "UI" map, owned by <see cref="InputReader"/>. It stays
    /// enabled while the rover controls are disabled (pause), so Pause can always be undone. UI Toolkit receives
    /// pointer, navigation and submit events through the Input System's own UI provider; this map carries what the
    /// game decides itself: pause, back, focus recovery and cursor recapture.
    /// </summary>
    public sealed class MenuInput
    {
        public const string MapName = "UI";

        private readonly InputActionMap _map;
        private readonly InputAction _navigate;
        private readonly InputAction _cancel;
        private readonly InputAction _pause;
        private readonly InputAction _click;

        internal MenuInput(InputActionMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _navigate = map.FindAction("Navigate", throwIfNotFound: true);
            _cancel = map.FindAction("Cancel", throwIfNotFound: true);
            _pause = map.FindAction("Pause", throwIfNotFound: true);
            _click = map.FindAction("Click", throwIfNotFound: true);
        }

        /// <summary>A menu direction (arrows, WASD, d-pad, stick) started being pushed this frame.</summary>
        public bool NavigatePressed => _navigate.WasPressedThisFrame();

        /// <summary>Back out of the topmost thing (Esc, gamepad East).</summary>
        public bool CancelPressed => _cancel.WasPressedThisFrame();

        /// <summary>Open or close the pause menu (Esc, gamepad Start).</summary>
        public bool PausePressed => _pause.WasPressedThisFrame();

        /// <summary>Primary mouse button pressed this frame (used to recapture the cursor).</summary>
        public bool ClickPressed => _click.WasPressedThisFrame();

        public bool Enabled => _map.enabled;

        /// <summary>
        /// Short name of the control that backs out ("Esc", "B") on <paramref name="device"/>, for the glyph on a
        /// dismissable card. Allocates a string: cache it.
        /// </summary>
        public string GetCancelLabel(InputDeviceKind device)
        {
            return BindingLabels.For(_cancel, device);
        }

        /// <summary>
        /// Stable id of the control that backs out ("keyboard.escape", "gamepad.buttoneast") on
        /// <paramref name="device"/>, for a localized keycap word. Allocates a string: cache it.
        /// </summary>
        public string GetCancelControl(InputDeviceKind device)
        {
            return BindingLabels.ControlId(_cancel, device);
        }

        internal InputActionMap Map => _map;

        public void Enable()
        {
            _map.Enable();
        }

        public void Disable()
        {
            _map.Disable();
        }
    }
}
