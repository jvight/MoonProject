using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The charge-up's Cells hardware on 07 (docs/design/0.5.5-owner-feedback.md §5.4, the owner's pick): the stage
    /// cells' sleeve travel, the glow levels of the cells, the conduits and the lock ring, and the drain, fade and
    /// warm-trace timings. One energy colour, going toward white at the full lock.
    /// </summary>
    [Serializable]
    public sealed class ChargeCellsSettings
    {
        [Tooltip("The energy colour: tech-cyan. Emissive only on the parts that carry it.")]
        [SerializeField] private Color _colour = new Color(0.45f, 0.9f, 1f);

        [Tooltip("How far (m) each stage cell's glass sleeve slides forward (local z) as it fills.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _sleeveTravel = 0.065f;

        [Tooltip("A filled cell's and the lit conduits' glow while building.")]
        [Range(0f, 4f)]
        [SerializeField] private float _stageGlow = 1f;

        [Tooltip("The full lock: every lit part's glow (and the lock ring's), pushed toward white.")]
        [Range(0f, 8f)]
        [SerializeField] private float _lockGlow = 2.4f;

        [Tooltip("How far the colour goes toward white at the full lock (0 none, 1 white).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lockWhite = 0.6f;

        [Tooltip("Seconds the full lock's glow and whitening take to come in: a firm step, never a hard flash.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _lockSeconds = 0.08f;

        [Tooltip("Seconds a release before the full lock takes to drain the cells back toward the battery, the last "
            + "stage first.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _drainSeconds = 0.2f;

        [Tooltip("Seconds the cells take to fade once the cut is over (the break, or a release after firing).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _fadeSeconds = 0.4f;

        [Tooltip("The warm emitter's trace: the conduits' faint glow through the warm window, so a quick next charge "
            + "reads.")]
        [Range(0f, 1f)]
        [SerializeField] private float _warmGlow = 0.3f;

        public Color Colour => _colour;

        public float SleeveTravel => _sleeveTravel;

        public float StageGlow => _stageGlow;

        public float LockGlow => _lockGlow;

        public float LockWhite => _lockWhite;

        public float LockSeconds => _lockSeconds;

        public float DrainSeconds => _drainSeconds;

        public float FadeSeconds => _fadeSeconds;

        public float WarmGlow => _warmGlow;
    }
}
