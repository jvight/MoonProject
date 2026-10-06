using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>Designer placement of a play bowl: a wide, shallow, flat-bottomed dip to swoop through.</summary>
    [Serializable]
    public struct BowlPlacement
    {
        [Tooltip("Direction of the bowl from the base, degrees clockwise from +Z (north).")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing;

        [Tooltip("Distance of the bowl centre from the base, metres.")]
        [Min(0f)]
        [SerializeField] private float _distance;

        [Tooltip("Radius of the bowl where it meets the surrounding ground, metres.")]
        [Min(4f)]
        [SerializeField] private float _radius;

        [Tooltip("Depth of the flat bottom below the surrounding ground, metres.")]
        [Min(0f)]
        [SerializeField] private float _depth;

        [Tooltip("Height of the soft lip around the bowl, metres. A small lip gives a little pop on the way out.")]
        [Min(0f)]
        [SerializeField] private float _lipHeight;

        public BowlPlacement(float bearing, float distance, float radius, float depth, float lipHeight)
        {
            _bearing = bearing;
            _distance = distance;
            _radius = radius;
            _depth = depth;
            _lipHeight = lipHeight;
        }

        public float Bearing => _bearing;

        public float Distance => _distance;

        public float Radius => _radius;

        public float Depth => _depth;

        public float LipHeight => _lipHeight;
    }
}
