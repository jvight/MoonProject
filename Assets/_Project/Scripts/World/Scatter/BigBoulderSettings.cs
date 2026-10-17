using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The rare big boulders on the basin floor (Art's R1 Rock_10..13, 3-8 m): a sparse tier above the scatter's
    /// boulders, landmarks and cover the player steers by, never on a route, a footprint or the pad.
    /// </summary>
    [Serializable]
    public sealed class BigBoulderSettings
    {
        [Tooltip("Art's big R1 rocks the tier picks from (Generated/Art/R1/Models/Rocks).")]
        [SerializeField] private string[] _rocks = { "Rock_10", "Rock_11", "Rock_12", "Rock_13" };

        [Tooltip("Minimum distance between big boulder sites, metres: far apart, each one a landmark.")]
        [Range(30f, 200f)]
        [SerializeField] private float _spacing = 85f;

        [Tooltip("Share of the sites that get a boulder (0..1): rare.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chance = 0.3f;

        [Tooltip("Radius of the basin floor the tier covers, metres (the drivable floor ends at 320).")]
        [Range(50f, 320f)]
        [SerializeField] private float _extent = 290f;

        [Tooltip("Open ground kept between a big boulder and the base pad's centre, metres.")]
        [Range(0f, 120f)]
        [SerializeField] private float _padClearance = 45f;

        [Tooltip("Open ground kept between a big boulder and the edge of every anchored place, metres.")]
        [Range(0f, 60f)]
        [SerializeField] private float _anchorClearance = 18f;

        [Tooltip("Half width of the corridor kept open along each opening route and cable, metres.")]
        [Range(0f, 60f)]
        [SerializeField] private float _routeClearance = 20f;

        [Tooltip("Steepest ground a big boulder rests on, degrees.")]
        [Range(0f, 30f)]
        [SerializeField] private float _maxSlope = 12f;

        [Tooltip("How deep a big boulder settles in the dust, as a share of its height (0..1).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _sink = 0.18f;

        public string[] Rocks => _rocks;
        public float Spacing => _spacing;
        public float Chance => _chance;
        public float Extent => _extent;
        public float PadClearance => _padClearance;
        public float AnchorClearance => _anchorClearance;
        public float RouteClearance => _routeClearance;
        public float MaxSlope => _maxSlope;
        public float Sink => _sink;
    }
}
