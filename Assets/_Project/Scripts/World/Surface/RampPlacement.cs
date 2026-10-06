using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>Designer placement of a play ramp: a smooth asymmetric hump that launches a floaty hop.</summary>
    [Serializable]
    public struct RampPlacement
    {
        [Tooltip("Direction of the ramp from the base, degrees clockwise from +Z (north).")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing;

        [Tooltip("Distance of the ramp crest from the base, metres.")]
        [Min(0f)]
        [SerializeField] private float _distance;

        [Tooltip("Driving direction up the ramp relative to the bearing, degrees (0 = launch away from the base).")]
        [Range(-180f, 180f)]
        [SerializeField] private float _headingOffset;

        [Tooltip("Length of the climbing side, metres. Shorter = steeper take-off.")]
        [Min(1f)]
        [SerializeField] private float _riseLength;

        [Tooltip("Length of the landing side, metres. Longer = softer landing.")]
        [Min(1f)]
        [SerializeField] private float _fallLength;

        [Tooltip("Half width of the ramp, metres.")]
        [Min(1f)]
        [SerializeField] private float _halfWidth;

        [Tooltip("Height of the crest above the surrounding ground, metres.")]
        [Min(0f)]
        [SerializeField] private float _height;

        public RampPlacement(float bearing, float distance, float headingOffset, float riseLength, float fallLength,
            float halfWidth, float height)
        {
            _bearing = bearing;
            _distance = distance;
            _headingOffset = headingOffset;
            _riseLength = riseLength;
            _fallLength = fallLength;
            _halfWidth = halfWidth;
            _height = height;
        }

        public float Bearing => _bearing;

        public float Distance => _distance;

        public float HeadingOffset => _headingOffset;

        public float RiseLength => _riseLength;

        public float FallLength => _fallLength;

        public float HalfWidth => _halfWidth;

        public float Height => _height;
    }
}
