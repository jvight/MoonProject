using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// One body response of <see cref="ImpactNudge"/> (docs/design/feel-pass.md §4): how far and how much it moves
    /// 07, and the spring that brings it back (damping under 1 gives the one overshoot).
    /// </summary>
    [Serializable]
    public sealed class NudgeSettings
    {
        [Tooltip("Peak travel (m): recoil back, or the dip down.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _distance;

        [Tooltip("Peak tilt (deg): forward lean, nod, or the rear's dip.")]
        [Range(0f, 10f)]
        [SerializeField] private float _angle;

        [Tooltip("Spring frequency (Hz) bringing it back: higher recovers faster.")]
        [Range(0.2f, 12f)]
        [SerializeField] private float _frequency = 3f;

        [Tooltip("Damping ratio: 1 settles without overshoot, lower gives one rebound.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _damping = 1f;

        public NudgeSettings()
        {
        }

        public NudgeSettings(float distance, float angle, float frequency, float damping)
        {
            _distance = distance;
            _angle = angle;
            _frequency = frequency;
            _damping = damping;
        }

        public float Distance => _distance;

        public float Angle => _angle;

        public float Frequency => _frequency;

        public float Damping => _damping;
    }
}
