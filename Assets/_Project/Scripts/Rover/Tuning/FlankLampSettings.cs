using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The flank lamps along 07's orange stripe (docs/features/M3-20 "How 07 shows it"): one small amber lamp per
    /// cell, lit when full, breathing from full to dark as the cell being drawn empties, dark glass when empty, and a
    /// faint breath in the first while 07 crawls on its sealed reserve; the road light sinks to a glow meanwhile.
    /// </summary>
    [Serializable]
    public sealed class FlankLampSettings
    {
        [Tooltip("Glow of a full cell's lamp (linear HDR multiplier; 1 = the authored glow).")]
        [Range(0f, 4f)]
        [SerializeField] private float _litGlow = 3.6f;

        [Tooltip("Seconds per breath of the lamp whose cell is being drawn.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _breathPeriod = 2.6f;

        [Tooltip("How deeply the drawn cell's lamp breathes (0 = steady).")]
        [Range(0f, 1f)]
        [SerializeField] private float _breathDepth = 0.4f;

        [Tooltip("Glow of the first lamp while 07 crawls on Kenji's sealed reserve cell (it breathes).")]
        [Range(0f, 1f)]
        [SerializeField] private float _reserveGlow = 0.18f;

        [Tooltip("Seconds for a lamp to ease halfway to a new glow, so cells fill and empty softly.")]
        [Range(0.01f, 2f)]
        [SerializeField] private float _halfLife = 0.18f;

        [Tooltip("Share of the road light while 07 crawls on the reserve: the lamp at a glow, motes still in it.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _crawlHeadlamp = 0.35f;

        [Tooltip("Seconds between the lamps lighting one by one as 07 wakes in the opening (after O5's flicker).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _wakeInterval = 0.4f;

        public float LitGlow => _litGlow;
        public float BreathPeriod => _breathPeriod;
        public float BreathDepth => _breathDepth;
        public float ReserveGlow => _reserveGlow;
        public float HalfLife => _halfLife;
        public float CrawlHeadlamp => _crawlHeadlamp;
        public float WakeInterval => _wakeInterval;
    }
}
