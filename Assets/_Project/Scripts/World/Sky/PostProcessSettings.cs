using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The filmic lofi finish (pillar 6), written into the URP Volume profile
    /// (Generated/World/WorldVolumeProfile.asset) by the World builder: gentle tonemapping, soft bloom for the warm
    /// lights that are alive, cool shadows and warm highlights, a light vignette and very fine film grain. Nothing
    /// sharp, nothing harsh. The ambient occlusion goes into the PC renderer's SSAO feature, through the same builder.
    /// </summary>
    [Serializable]
    public sealed class PostProcessSettings
    {
        [Header("Bloom")]
        [Tooltip("Brightness above which pixels bloom. It scales with the earthlight: Art caps lit surfaces just under " +
            "its own threshold in its reference light, so a key that much brighter needs a threshold that much " +
            "higher. Palette emissives (lamps, eye, Earth) sit above it.")]
        [Range(0f, 2f)]
        [SerializeField] private float _bloomThreshold = 1.25f;

        [Tooltip("Bloom strength. Soft: a glow, not a flare.")]
        [Range(0f, 3f)]
        [SerializeField] private float _bloomIntensity = 1.1f;

        [Tooltip("How far the bloom spreads (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _bloomScatter = 0.7f;

        [Tooltip("Tint of the bloom: a touch warm, so the lamps glow like home.")]
        [SerializeField] private Color _bloomTint = new Color(1f, 0.93f, 0.86f);

        [Header("Colour")]
        [Tooltip("Exposure offset in EV. Neutral under look A \"Moon grey\": the night reads dark, the lit faces and " +
            "the warm lights carry the frame.")]
        [Range(-3f, 3f)]
        [SerializeField] private float _postExposure = 0f;

        [Tooltip("Grade in HDR, before tonemapping (the PC pipeline asset's grading mode), so the warm windows grade " +
            "with their full range and the shadows keep a real black.")]
        [SerializeField] private bool _gradeInHdr = true;

        [Tooltip("Contrast (-100..100). Enough for a real black and a real highlight; the night stays calm.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _contrast = 22f;

        [Tooltip("Saturation (-100..100). Under neutral, so the grey ground stays grey and the warm lamps stand out; " +
            "split toning carries the hue.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _saturation = -18f;

        [Tooltip("Lift: tints the shadows (rgb) and raises them (w). Cool, but never raised: the frame keeps a black.")]
        [SerializeField] private Vector4 _lift = new Vector4(0.96f, 0.97f, 1.08f, 0f);

        [Tooltip("Gamma: tints and shifts the midtones (rgb, w).")]
        [SerializeField] private Vector4 _gamma = new Vector4(1f, 1f, 1f, 0f);

        [Tooltip("Gain: tints and scales the highlights (rgb, w). Slightly warm.")]
        [SerializeField] private Vector4 _gain = new Vector4(1.04f, 1f, 0.95f, 0f);

        [Tooltip("Split toning, shadows: the cool hue pushed into the dark tones.")]
        [SerializeField] private Color _splitShadows = new Color(0.36f, 0.39f, 0.46f);

        [Tooltip("Split toning, highlights: the warm hue pushed into the light tones.")]
        [SerializeField] private Color _splitHighlights = new Color(1f, 0.92f, 0.8f);

        [Tooltip("Split toning balance (-100..100): below zero more of the range counts as shadow.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _splitBalance = -15f;

        [Header("Ambient occlusion")]
        [Tooltip("SSAO strength. Strong enough that every foot, leg and wheel sits in a dark crease and stops floating.")]
        [Range(0f, 4f)]
        [SerializeField] private float _occlusionIntensity = 1.2f;

        [Tooltip("SSAO sampling radius, metres. Sized for the chase camera's 10-30 m: smaller vanishes into a few " +
            "pixels, larger greys whole walls.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _occlusionRadius = 1.2f;

        [Tooltip("How much the occlusion also darkens directly lit faces (0 = ambient only). Enough that a prop's " +
            "lit foot still reads as grounded.")]
        [Range(0f, 1f)]
        [SerializeField] private float _occlusionDirectLighting = 0.55f;

        [Header("Home's grade")]
        [Tooltip("Split toning balance the grade reaches once the whole base is awake (the Lift). Each power stage " +
            "moves it a step from the balance above, so more of the frame takes the warm highlight hue as home wakes.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _wokenSplitBalance = 8f;

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
        public bool GradeInHdr => _gradeInHdr;
        public float PostExposure => _postExposure;
        public float Contrast => _contrast;
        public float Saturation => _saturation;
        public Vector4 Lift => _lift;
        public Vector4 Gamma => _gamma;
        public Vector4 Gain => _gain;
        public Color SplitShadows => _splitShadows;
        public Color SplitHighlights => _splitHighlights;
        public float SplitBalance => _splitBalance;
        public float WokenSplitBalance => _wokenSplitBalance;
        public float OcclusionIntensity => _occlusionIntensity;
        public float OcclusionRadius => _occlusionRadius;
        public float OcclusionDirectLighting => _occlusionDirectLighting;
        public float VignetteIntensity => _vignetteIntensity;
        public float VignetteSmoothness => _vignetteSmoothness;
        public Color VignetteColor => _vignetteColor;
        public float GrainIntensity => _grainIntensity;
        public float GrainResponse => _grainResponse;
    }
}
