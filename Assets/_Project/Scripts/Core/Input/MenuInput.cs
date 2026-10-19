using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Typed, allocation-free view over the Controls asset's "UI" map, owned by <see cref="InputReader"/>. It stays
    /// enabled while the rover controls are disabled (pause), so Pause can always be undone. UI Toolkit receives
    /// pointer, navigation and submit events through the Input System's own UI provider; this map carries what the
    /// game decides itself: pause, back, focus recovery and cursor recapture, and on 07's face the page flip (Q / E),
    /// the databank's colour toggle (F) and its model turn (right stick). It also reads the rover map's Build key while
    /// that map is off (07's face screen is up), so the face can hand over to build mode.
    /// </summary>
    public sealed class MenuInput
    {
        public const string MapName = "UI";

        private readonly InputActionMap _map;
        private readonly InputAction _navigate;
        private readonly InputAction _submit;
        private readonly InputAction _cancel;
        private readonly InputAction _pause;
        private readonly InputAction _click;
        private readonly InputAction _cargo;
        private readonly InputAction _pagePrev;
        private readonly InputAction _pageNext;
        private readonly InputAction _colour;
        private readonly InputAction _turn;
        private readonly InputAction _roverBuild;

        internal MenuInput(InputActionMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _navigate = map.FindAction("Navigate", throwIfNotFound: true);
            _submit = map.FindAction("Submit", throwIfNotFound: false);
            _cancel = map.FindAction("Cancel", throwIfNotFound: true);
            _pause = map.FindAction("Pause", throwIfNotFound: true);
            _click = map.FindAction("Click", throwIfNotFound: true);
            _cargo = map.FindAction("Cargo", throwIfNotFound: true);
            _pagePrev = map.FindAction("PagePrev", throwIfNotFound: true);
            _pageNext = map.FindAction("PageNext", throwIfNotFound: true);
            _colour = map.FindAction("Colour", throwIfNotFound: true);
            _turn = map.FindAction("Turn", throwIfNotFound: true);
            InputActionMap rover = map.asset != null
                ? map.asset.FindActionMap(InputReader.RoverMapName, throwIfNotFound: true)
                : throw new ArgumentException("The UI map must live in the Controls asset beside the Rover map.",
                    nameof(map));
            _roverBuild = rover.FindAction("Build", throwIfNotFound: true);
        }

        /// <summary>Current menu navigation direction (-1..1 on x and y).</summary>
        public Vector2 Navigate => _navigate.ReadValue<Vector2>();

        /// <summary>A menu direction (arrows, WASD, d-pad, stick) started being pushed this frame.</summary>
        public bool NavigatePressed => _navigate.WasPressedThisFrame();

        /// <summary>Current 2D navigation vector read from the UI Navigate action.</summary>
        public Vector2 NavigateVector => _navigate.ReadValue<Vector2>();

        /// <summary>Confirm/select the focused item (Enter, Space, gamepad South / A).</summary>
        public bool SubmitPressed
        {
            get
            {
                if (!_map.enabled)
                {
                    return false;
                }

                if (_submit != null && _submit.WasPressedThisFrame())
                {
                    return true;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.enterKey.wasPressedThisFrame
                    || kb.numpadEnterKey.wasPressedThisFrame
                    || kb.spaceKey.wasPressedThisFrame))
                {
                    return true;
                }

                Gamepad pad = Gamepad.current;
                return pad != null && pad.buttonSouth.wasPressedThisFrame;
            }
        }

        /// <summary>Back out of the topmost thing (Esc, gamepad East).</summary>
        public bool CancelPressed => _cancel.WasPressedThisFrame();

        /// <summary>Open or close the pause menu (Esc, gamepad Start).</summary>
        public bool PausePressed => _pause.WasPressedThisFrame();

        /// <summary>Open or close the cargo screen (Tab, gamepad View).</summary>
        public bool CargoPressed => _cargo.WasPressedThisFrame();

        /// <summary>
        /// The rover's Build key (B, gamepad North) went down this frame, read from its controls so it counts while the
        /// rover map is off behind a menu.
        /// </summary>
        public bool BuildPressed => _map.enabled && AnyPressedThisFrame(_roverBuild);

        /// <summary>Flip 07's face to the other page (Q or E, gamepad LB or RB) this frame.</summary>
        public bool PagePressed => _pagePrev.WasPressedThisFrame() || _pageNext.WasPressedThisFrame();

        /// <summary>The databank's full-colour toggle (F, gamepad Y) went down this frame.</summary>
        public bool ColourPressed => _colour.WasPressedThisFrame();

        /// <summary>The databank model's turn (right stick, -1..1).</summary>
        public Vector2 Turn => _turn.ReadValue<Vector2>();

        /// <summary>Primary mouse button pressed this frame (used to recapture the cursor).</summary>
        public bool ClickPressed => _click.WasPressedThisFrame();

        /// <summary>Primary mouse button released this frame.</summary>
        public bool ClickReleased => _click.WasReleasedThisFrame();

        /// <summary>Current mouse pointer screen position (bottom-left origin), or NaN when no mouse is present.</summary>
        public Vector2 PointerPosition =>
            Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(float.NaN, float.NaN);

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

        private static bool AnyPressedThisFrame(InputAction action)
        {
            ReadOnlyArray<InputControl> controls = action.controls;
            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i] is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

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
