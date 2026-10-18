using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The lander's blast-swept zone (opening v2 §4): its descent engine blew the regolith outward as it touched down,
    /// leaving a lighter, swept halo streaked in rays round it like the Apollo sites, bare of rocks inside and piled
    /// thicker with them at its edge. Rovers park outside it, which is why 07's old post stood out on the plain.
    /// </summary>
    [Serializable]
    public sealed class BlastZoneSettings
    {
        [Tooltip("Radius of the swept zone round the lander's centre, metres.")]
        [Range(10f, 60f)]
        [SerializeField] private float _radius = 34f;

        [Tooltip("Metres inside the radius over which the sweep fades into the untouched dust (the rays reach " +
            "across it, so the edge is fingered, never a ring).")]
        [Range(1f, 20f)]
        [SerializeField] private float _edgeBand = 9f;

        [Tooltip("How much lighter the swept regolith is than the dust round it (0 = no change).")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _albedoLift = 0.45f;

        [Tooltip("How much the outward rays lighten or shade the swept regolith either way of its tone.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _rayLift = 0.22f;

        [Tooltip("Rays round the full circle (whole number, so they close seamlessly).")]
        [Range(8, 160)]
        [SerializeField] private int _rayCount = 56;

        [Tooltip("Metres along a ray over which its tone changes: long values give long unbroken streaks.")]
        [Range(2f, 60f)]
        [SerializeField] private float _rayLength = 14f;

        [Tooltip("Extra chance of a pebble on the zone's edge, where the blast piled what it swept (0 = none).")]
        [Range(0f, 2f)]
        [SerializeField] private float _edgePebbles = 0.55f;

        [Tooltip("Width of the pebble pile round the edge, metres, centred just outside the radius.")]
        [Range(1f, 20f)]
        [SerializeField] private float _edgePileWidth = 8f;

        public float Radius => _radius;
        public float EdgeBand => _edgeBand;
        public float AlbedoLift => _albedoLift;
        public float RayLift => _rayLift;
        public int RayCount => _rayCount;
        public float RayLength => _rayLength;
        public float EdgePebbles => _edgePebbles;
        public float EdgePileWidth => _edgePileWidth;
    }
}
