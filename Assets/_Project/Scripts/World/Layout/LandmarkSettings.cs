using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where World places the crew's remnants from Art's landmark kit (M3-18): the route posts along the road to the
    /// canyon (L2), the broken cable bridge over the chasm (L4) and the toppled dish frames of the antenna farm (L7).
    /// </summary>
    [Serializable]
    public sealed class LandmarkSettings
    {
        [Header("Route posts (L2)")]
        [Tooltip("Bearing of the crew's road to the canyon mouth, degrees clockwise from +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _routeBearing = 72f;

        [Tooltip("Distances of the route posts from home, metres: about every 45 m from the pad's edge to the mouth.")]
        [SerializeField] private float[] _routePostDistances = { 70f, 115f, 160f, 205f, 250f, 275f };

        [Tooltip("How far the posts stand to the right of the road's centre line, metres, so 07's ruts run past them.")]
        [Range(0f, 10f)]
        [SerializeField] private float _routePostSide = 2.6f;

        [Tooltip("Glow of a post's reflector (linear glow multiplier): a faint glint, never a lamp.")]
        [Range(0f, 2f)]
        [SerializeField] private float _reflectorGlow = 0.3f;

        [Header("Chasm bridge (L4)")]
        [Tooltip("Glow of the lamp on the bridge's far anchor (linear glow multiplier): a warm point across the gap.")]
        [Range(0f, 3f)]
        [SerializeField] private float _bridgeLampGlow = 1f;

        [Tooltip("How far the bridge's anchors stand to the side of the leap line, metres, clear of 07's lane.")]
        [Range(0f, 10f)]
        [SerializeField] private float _bridgeSide = 3.5f;

        [Header("Antenna farm (L7)")]
        [Tooltip("How far the toppled dish frames lie from the farm's centre, metres.")]
        [Range(0f, 20f)]
        [SerializeField] private float _dishSpread = 6.5f;

        public float RouteBearing => _routeBearing;
        public float[] RoutePostDistances => _routePostDistances;
        public float RoutePostSide => _routePostSide;
        public float ReflectorGlow => _reflectorGlow;
        public float BridgeLampGlow => _bridgeLampGlow;
        public float BridgeSide => _bridgeSide;
        public float DishSpread => _dishSpread;
    }
}
