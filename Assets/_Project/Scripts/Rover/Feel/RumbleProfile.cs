using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// One gamepad rumble shape (docs/design/feel-pass.md §4): each motor ramps linearly from its start to its end
    /// level over the profile's seconds, then stops, or holds its end level until stopped when
    /// <see cref="HoldsEnd"/> (a charge-up waiting for ignition, a sustained beam).
    /// </summary>
    [Serializable]
    public sealed class RumbleProfile
    {
        [Tooltip("Low (heavy) motor level at the start, 0..1 before strength and the 0.35 cap.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowStart;

        [Tooltip("Low (heavy) motor level at the end.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowEnd;

        [Tooltip("High (light) motor level at the start.")]
        [Range(0f, 1f)]
        [SerializeField] private float _highStart;

        [Tooltip("High (light) motor level at the end.")]
        [Range(0f, 1f)]
        [SerializeField] private float _highEnd;

        [Tooltip("Seconds of the ramp (a beat may pass its own length instead, e.g. the charge-up's).")]
        [Range(0f, 5f)]
        [SerializeField] private float _seconds;

        [Tooltip("Keep the end level after the ramp until stopped, instead of ending.")]
        [SerializeField] private bool _holdsEnd;

        public RumbleProfile()
        {
        }

        public RumbleProfile(float lowStart, float lowEnd, float highStart, float highEnd, float seconds,
            bool holdsEnd)
        {
            _lowStart = lowStart;
            _lowEnd = lowEnd;
            _highStart = highStart;
            _highEnd = highEnd;
            _seconds = seconds;
            _holdsEnd = holdsEnd;
        }

        public float Seconds => _seconds;

        public bool HoldsEnd => _holdsEnd;

        /// <summary>The low motor's level at <paramref name="share"/> (0..1) of the ramp.</summary>
        public float Low(float share)
        {
            return Mathf.Lerp(_lowStart, _lowEnd, share);
        }

        /// <summary>The high motor's level at <paramref name="share"/> (0..1) of the ramp.</summary>
        public float High(float share)
        {
            return Mathf.Lerp(_highStart, _highEnd, share);
        }
    }
}
