using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// The reach ring (polish wave 1): when the tower's reach grows, a mast lights or a new cell pack fits, a warm ring
    /// rolls out over the ground from the source to the new reach, once, so the player sees how far they can now go.
    /// Calm: an eased roll-out, a soft band, gone in a breath.
    /// </summary>
    [Serializable]
    public sealed class ReachRingSettings
    {
        [Tooltip("Seconds the ring takes to roll out to its reach (eased out: quick at first, settling).")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _duration = 2f;

        [Tooltip("Share of the roll-out over which the ring fades in at the start (0..1).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _fadeIn = 0.1f;

        [Tooltip("Share of the roll-out over which the ring fades away at the end (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _fadeOut = 0.45f;

        [Tooltip("Half width of the ring's soft band on the ground, metres.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _halfWidth = 2.5f;

        [Tooltip("Colour of the ring: the lamps' warm.")]
        [SerializeField] private Color _color = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Glow of the ring at full strength (linear): warm on the dust, under the bloom threshold.")]
        [Range(0f, 2f)]
        [SerializeField] private float _intensity = 0.55f;

        [Tooltip("Reach (metres) a newly lit relay mast's ring rolls out to.")]
        [Range(5f, 300f)]
        [SerializeField] private float _relayReach = 80f;

        [Tooltip("Reach (metres) the ring rolls out to around 07 when a new cell pack fits.")]
        [Range(5f, 300f)]
        [SerializeField] private float _packReach = 45f;

        public float Duration => _duration;
        public float FadeIn => _fadeIn;
        public float FadeOut => _fadeOut;
        public float HalfWidth => _halfWidth;
        public Color Color => _color;
        public float Intensity => _intensity;
        public float RelayReach => _relayReach;
        public float PackReach => _packReach;
    }
}
