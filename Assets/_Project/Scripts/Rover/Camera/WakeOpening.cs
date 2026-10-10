using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The first beats of a fresh game (M3-16 P5). Beat 1 holds a low front three-quarter shot on 07's lens as it wakes
    /// and looks up at the stars; beat 2 eases round, as 07 turns its head to Earth, to a low shot over its shoulder
    /// aimed at Earth. Any input, or the hold running out, then eases into the chase frame.
    /// <see cref="Weight"/> is the opening's share of the camera; <see cref="Turn"/> is how far beat 2 has taken over.
    /// </summary>
    public sealed class WakeOpening
    {
        private readonly RoverCameraTuning _tuning;
        private float _held;
        private float _eased;

        public WakeOpening(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Whether the opening holds or is easing out.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Whether it has started easing out to the chase frame.</summary>
        public bool IsEasing { get; private set; }

        /// <summary>The opening's share of the camera, 1 while holding, 0 once handed to the chase frame.</summary>
        public float Weight { get; private set; }

        /// <summary>Beat 2's share of the opening: 0 on the lens, easing to 1 over Earth; 0 when inactive.</summary>
        public float Turn { get; private set; }

        /// <summary>
        /// Composer screen offset (+ = lower) that puts Earth, <paramref name="earthElevation"/> deg above the horizon,
        /// at viewport height <paramref name="earthHeight"/> for a camera at <paramref name="cameraElevation"/> deg
        /// above the follow point (which then sits that offset below the frame's centre).
        /// </summary>
        public static float EarthScreenY(float earthElevation, float cameraElevation, float earthHeight,
            float verticalFov)
        {
            float halfHeight = Mathf.Tan(0.5f * verticalFov * Mathf.Deg2Rad);
            float pitch = earthElevation - Mathf.Atan(2f * (earthHeight - 0.5f) * halfHeight) * Mathf.Rad2Deg;
            return Mathf.Tan((pitch + cameraElevation) * Mathf.Deg2Rad) / (2f * halfHeight);
        }

        /// <summary>Starts the opening; ignored while one is already running.</summary>
        public void Begin()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            IsEasing = false;
            _held = 0f;
            _eased = 0f;
            Weight = 1f;
            Turn = 0f;
        }

        /// <param name="playerInput">The player gave look or drive input this frame.</param>
        public void Step(bool playerInput, float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return;
            }

            if (!IsEasing)
            {
                _held += deltaTime;
                float turnStart = _tuning.WakeLensSeconds;
                Turn = Smoothing.SmoothStep(turnStart, turnStart + _tuning.WakeTurnSeconds, _held);
                IsEasing = playerInput || _held >= _tuning.WakeHoldSeconds;
                return;
            }

            _eased = Math.Min(1f, _eased + deltaTime / _tuning.WakeEaseSeconds);
            Weight = 1f - Smoothing.SmoothStep(0f, 1f, _eased);
            if (_eased >= 1f)
            {
                IsActive = false;
                IsEasing = false;
                Weight = 0f;
                Turn = 0f;
            }
        }
    }
}
