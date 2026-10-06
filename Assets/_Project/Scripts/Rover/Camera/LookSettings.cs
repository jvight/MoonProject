using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The player's look preferences and how they shape raw look input: starts from the tuned defaults, keeps the
    /// sensitivity inside the tuned range, and turns mouse pixels and stick deflection into orbit degrees.
    /// </summary>
    public sealed class LookSettings : ILookSettings
    {
        private readonly RoverCameraTuning _tuning;
        private float _sensitivity = 1f;

        public LookSettings(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            InvertY = tuning.InvertY;
            Sensitivity = 1f;
        }

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = float.IsNaN(value)
                ? 1f
                : Mathf.Clamp(value, _tuning.MinSensitivity, _tuning.MaxSensitivity);
        }

        public bool InvertY { get; set; }

        /// <summary>
        /// Orbit change (deg) for this frame: x = orbit right, y = camera up. Mouse deltas are already per frame
        /// (pixels); the stick is a rate, so it is scaled by <paramref name="deltaTime"/>. Pushing up looks up unless
        /// inverted.
        /// </summary>
        public Vector2 OrbitDegrees(Vector2 mouseDelta, Vector2 stick, float deltaTime)
        {
            Vector2 look = (mouseDelta * _tuning.MouseSensitivity + stick * (_tuning.StickRate * deltaTime))
                * _sensitivity;
            return new Vector2(look.x, InvertY ? look.y : -look.y);
        }
    }
}
