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

        [Tooltip("Share of the flank lamps' glow while the opening runs on the reserve (until the first charge).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _openingReserveGlow = 0.5f;

        [Tooltip("Share of the road light while the opening runs on the reserve (until the first charge).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _openingReserveHeadlamp = 0.55f;

        [Tooltip("Seconds between the lamps lighting one by one as 07 wakes in the opening (after O5's flicker).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _wakeInterval = 0.4f;

        [Header("While charging on a pad")]
        [Tooltip("Seconds per slow warm pulse that runs along the lamp row while 07 charges.")]
        [Range(0.8f, 6f)]
        [SerializeField] private float _chargePulsePeriod = 2.4f;

        [Tooltip("How much brighter (a share of a lamp's glow) the pulse makes a full cell at its crest.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargePulseDepth = 0.45f;

        [Tooltip("Seconds of delay between neighbouring lamps in the pulse: it travels along the row, filling to the end.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargePulseLag = 0.28f;

        [Tooltip("Seconds per breath of the lamp being filled while 07 charges: quicker than the idle breath, deeper.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _chargeBreathPeriod = 1.5f;

        [Tooltip("How deeply the lamp being filled breathes while 07 charges (0 = steady).")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeBreathDepth = 0.65f;

        [Tooltip("How much brighter (a share of the glow) the whole row flashes when the last cell fills.")]
        [Range(0f, 2f)]
        [SerializeField] private float _fullBoost = 0.9f;

        [Tooltip("Seconds the row's full flash takes to settle back to its glow.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _fullFlashSeconds = 1.8f;

        public float ChargePulsePeriod => _chargePulsePeriod;
        public float ChargePulseDepth => _chargePulseDepth;
        public float ChargePulseLag => _chargePulseLag;
        public float ChargeBreathPeriod => _chargeBreathPeriod;
        public float ChargeBreathDepth => _chargeBreathDepth;
        public float FullBoost => _fullBoost;
        public float FullFlashSeconds => _fullFlashSeconds;
        public float LitGlow => _litGlow;
        public float BreathPeriod => _breathPeriod;
        public float BreathDepth => _breathDepth;
        public float ReserveGlow => _reserveGlow;
        public float HalfLife => _halfLife;
        public float CrawlHeadlamp => _crawlHeadlamp;
        public float OpeningReserveGlow => _openingReserveGlow;
        public float OpeningReserveHeadlamp => _openingReserveHeadlamp;
        public float WakeInterval => _wakeInterval;
    }
}
