using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The filmic lofi finish (pillar 6), written into the URP Volume profile
    /// (Generated/World/WorldVolumeProfile.asset) by the World builder: gentle tonemapping, soft bloom for the warm
    /// lights that are alive, cool shadows and warm highlights, a light vignette and very fine film grain. Nothing
    /// sharp, nothing harsh.
    /// </summary>
    [Serializable]
    public sealed class PostProcessSettings
    {
        [Header("Bloom")]
        [Tooltip("Brightness above which pixels bloom. Palette emissives (lamps, eye, Earth) sit just above it.")]
        [Range(0f, 2f)]
        [SerializeField] private float _bloomThreshold = 1f;

        [Tooltip("Bloom strength. Soft: a glow, not a flare.")]
        [Range(0f, 3f)]
        [SerializeField] private float _bloomIntensity = 1.1f;

        [Tooltip("How far the bloom spreads (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _bloomScatter = 0.7f;

        [Tooltip("Tint of the bloom: a touch warm, so the lamps glow like home.")]
        [SerializeField] private Color _bloomTint = new Color(1f, 0.93f, 0.86f);

        [Header("Colour")]
        [Tooltip("Exposure offset in EV.")]
        [Range(-3f, 3f)]
        [SerializeField] private float _postExposure = 0.12f;

        [Tooltip("Contrast (-100..100). Low keeps the night soft.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _contrast = 10f;

        [Tooltip("Saturation (-100..100).")]
        [Range(-100f, 100f)]
        [SerializeField] private float _saturation = -12f;

        [Tooltip("Lift: tints the shadows (rgb) and raises them (w). A cool lift gives the lofi, faded-print feel.")]
        [SerializeField] private Vector4 _lift = new Vector4(0.96f, 0.97f, 1.08f, 0.01f);

        [Tooltip("Gamma: tints and shifts the midtones (rgb, w).")]
        [SerializeField] private Vector4 _gamma = new Vector4(1f, 1f, 1f, 0f);

        [Tooltip("Gain: tints and scales the highlights (rgb, w). Slightly warm.")]
        [SerializeField] private Vector4 _gain = new Vector4(1.04f, 1f, 0.95f, 0f);

        [Tooltip("Split toning, shadows: the cool hue pushed into the dark tones.")]
        [SerializeField] private Color _splitShadows = new Color(0.32f, 0.35f, 0.55f);

        [Tooltip("Split toning, highlights: the warm hue pushed into the light tones.")]
        [SerializeField] private Color _splitHighlights = new Color(1f, 0.86f, 0.68f);

        [Tooltip("Split toning balance (-100..100): below zero more of the range counts as shadow.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _splitBalance = -10f;

        [Header("Lens")]
        [Tooltip("Vignette strength.")]
        [Range(0f, 1f)]
        [SerializeField] private float _vignetteIntensity = 0.3f;

        [Tooltip("Vignette softness.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _vignetteSmoothness = 0.5f;

        [Tooltip("Vignette colour.")]
        [SerializeField] private Color _vignetteColor = new Color(0.05f, 0.03f, 0.12f);

        [Tooltip("Film grain strength. Barely there.")]
        [Range(0f, 1f)]
        [SerializeField] private float _grainIntensity = 0.12f;

        [Tooltip("How much the grain fades in bright areas (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _grainResponse = 0.8f;

        public float BloomThreshold => _bloomThreshold;
        public float BloomIntensity => _bloomIntensity;
        public float BloomScatter => _bloomScatter;
        public Color BloomTint => _bloomTint;
        public float PostExposure => _postExposure;
        public float Contrast => _contrast;
        public float Saturation => _saturation;
        public Vector4 Lift => _lift;
        public Vector4 Gamma => _gamma;
        public Vector4 Gain => _gain;
        public Color SplitShadows => _splitShadows;
        public Color SplitHighlights => _splitHighlights;
        public float SplitBalance => _splitBalance;
        public float VignetteIntensity => _vignetteIntensity;
        public float VignetteSmoothness => _vignetteSmoothness;
        public Color VignetteColor => _vignetteColor;
        public float GrainIntensity => _grainIntensity;
        public float GrainResponse => _grainResponse;
    }
}
