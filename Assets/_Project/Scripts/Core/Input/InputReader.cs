using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Typed, allocation-free view over the Controls input asset (map "Rover"). Systems poll it from Update;
    /// nothing else in the game touches InputSystem devices directly, so rebinding and gamepad support live here.
    /// </summary>
    public sealed class InputReader : IDisposable
    {
        public const string RoverMapName = "Rover";

        private readonly InputActionMap _roverMap;
        private readonly InputAction _drive;
        private readonly InputAction _lookDelta;
        private readonly InputAction _lookRate;
        private readonly InputAction _ping;
        private readonly InputAction _excavate;
        private readonly InputAction _tether;
        private readonly InputAction _winch;
        private readonly InputAction _pause;

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
            _pause = _roverMap.FindAction("Pause", throwIfNotFound: true);
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

        public bool PausePressed => _pause.WasPressedThisFrame();

        public bool Enabled => _roverMap.enabled;

        public void Enable()
        {
            _roverMap.Enable();
        }

        public void Disable()
        {
            _roverMap.Disable();
        }

        public void Dispose()
        {
            _roverMap.Disable();
        }
    }
}
