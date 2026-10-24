using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The fresh scar the Peak road's rockfall tore down the Peak's flank (base-home-and-crater-exits §4): a pale
    /// streak of newly bared regolith, narrow where the slope let go and fanning out over the boulders it dropped
    /// across the road, so the eye finds the closed road from the base at minute 1. Pale, never white.
    /// </summary>
    [Serializable]
    public sealed class RockfallScarSettings
    {
        [Tooltip("Where the scar's fan meets the road, metres up the road from its foot: just under the boulders.")]
        [Range(0f, 60f)]
        [SerializeField] private float _start = 8f;

        [Tooltip("How far the scar runs up the Peak's flank along the road's bearing, metres.")]
        [Range(10f, 200f)]
        [SerializeField] private float _length = 110f;

        [Tooltip("Half width of the scar's fan at the road, metres: wide where the boulders spread.")]
        [Range(2f, 40f)]
        [SerializeField] private float _fanHalfWidth = 24f;

        [Tooltip("Half width of the scar at its top, metres: narrow where the slope let go.")]
        [Range(1f, 30f)]
        [SerializeField] private float _topHalfWidth = 9f;

        [Tooltip("Soft run of the scar's edges, metres: torn ground blends into the old grey, never a cut line.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _edge = 6f;

        [Tooltip("How much the scar's edges wander in and out, as a share of its width.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _wander = 0.3f;

        [Tooltip("Wavelength of the edges' wander along the scar, metres.")]
        [Range(2f, 60f)]
        [SerializeField] private float _wanderWavelength = 14f;

        public float Start => _start;
        public float Length => _length;
        public float FanHalfWidth => _fanHalfWidth;
        public float TopHalfWidth => _topHalfWidth;
        public float Edge => _edge;
        public float Wander => _wander;
        public float WanderWavelength => _wanderWavelength;
    }
}
