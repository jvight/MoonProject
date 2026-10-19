using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A stretch of the basin floor the gentle relief leaves level: a lane between two points, or a spot when both
    /// points are one, with the half width it keeps flat.
    /// </summary>
    [Serializable]
    public struct ProtectedLane
    {
        [Tooltip("One end of the lane (world XZ, metres).")]
        [SerializeField] private Vector2 _from;

        [Tooltip("The other end (world XZ, metres); the same as the first for a spot.")]
        [SerializeField] private Vector2 _to;

        [Tooltip("Half width kept level, metres.")]
        [Range(0f, 100f)]
        [SerializeField] private float _halfWidth;

        public ProtectedLane(Vector2 from, Vector2 to, float halfWidth)
        {
            _from = from;
            _to = to;
            _halfWidth = halfWidth;
        }

        public Vector2 From => _from;
        public Vector2 To => _to;
        public float HalfWidth => _halfWidth;
    }
}
