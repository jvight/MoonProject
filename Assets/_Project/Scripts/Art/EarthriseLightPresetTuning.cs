using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Snapshot of evaluated lighting and colour-grade parameters for the 0.6 Earthrise stillness beat.
    /// </summary>
    public struct EarthriseLightState
    {
        public float BlendWeight;
        public Color KeyLightColor;
        public float KeyLightIntensity;
        public float KeyElevationDegrees;
        public Color RimLightColor;
        public float RimLightIntensity;
        public Color AmbientSkyColor;
        public Color AmbientEquatorColor;
        public Color AmbientGroundColor;
        public Color SkyBackgroundColor;
        public Color HorizonHazeColor;
        public Color GradeColorFilter;
    }

    /// <summary>
    /// Tuning parameters for the 0.6 Earthrise light tint preset (docs/design/0.6-living-moon.md §2):
    /// a warm-blue lighting and colour-grade preset for the Earthrise stillness beat ("Earth climbing over a ridge,
    /// the whole frame warm-blue"), preview only.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Art/Earthrise Light Preset Tuning", fileName = "EarthriseLightPresetTuning")]
    public sealed class EarthriseLightPresetTuning : ScriptableObject
    {
        [Header("Stillness Beat Timing")]
        [Tooltip("Eased fade-in duration in seconds as Earth climbs above the lunar ridge.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _fadeInSeconds = 5.0f;

        [Tooltip("Stillness plateau duration in seconds at full warm-blue Earthrise grade.")]
        [Range(2.0f, 120f)]
        [SerializeField] private float _holdSeconds = 15.0f;

        [Tooltip("Eased fade-out duration in seconds as lighting returns to the baseline look.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _fadeOutSeconds = 6.0f;

        [Header("Earthrise Key & Warm Rim Lights")]
        [Tooltip("Luminous cerulean-azure Earthshine directional key colour.")]
        [SerializeField] private Color _earthKeyColor = new Color(0.46f, 0.69f, 0.98f, 1.00f);

        [Tooltip("Directional intensity of the Earthshine key light at full Earthrise.")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float _earthKeyIntensity = 0.94f;

        [Tooltip("Starting elevation angle in degrees when Earth first clears the horizon ridge.")]
        [Range(1.0f, 25.0f)]
        [SerializeField] private float _startElevationDegrees = 4.5f;

        [Tooltip("Peak elevation angle in degrees of the Earth key light at full stillness.")]
        [Range(4.0f, 45.0f)]
        [SerializeField] private float _peakElevationDegrees = 14.5f;

        [Tooltip("Warm golden-ivory horizon rim fill colour (sunlit cloud-top reflection kissing ridge crests).")]
        [SerializeField] private Color _warmRimColor = new Color(0.96f, 0.74f, 0.52f, 1.00f);

        [Tooltip("Directional intensity of the warm golden-ivory counter-rim light.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _warmRimIntensity = 0.42f;

        [Header("Warm-Blue Ambient Trilight & Sky")]
        [Tooltip("Serene cobalt-blue sky ambient colour during the Earthrise stillness beat.")]
        [SerializeField] private Color _ambientSkyColor = new Color(0.19f, 0.29f, 0.48f, 1.00f);

        [Tooltip("Warm-blue slate equator ambient colour (bridges cool azure shadows and warm ivory highlights).")]
        [SerializeField] private Color _ambientEquatorColor = new Color(0.22f, 0.26f, 0.38f, 1.00f);

        [Tooltip("Deep indigo-basalt ground bounce ambient colour.")]
        [SerializeField] private Color _ambientGroundColor = new Color(0.08f, 0.11f, 0.18f, 1.00f);

        [Tooltip("Deep warm-blue night sky background colour.")]
        [SerializeField] private Color _skyBackgroundColor = new Color(0.06f, 0.10f, 0.19f, 1.00f);

        [Tooltip("Warm-blue horizon haze tint cast across distant Mare ridges.")]
        [SerializeField] private Color _horizonHazeColor = new Color(0.36f, 0.56f, 0.88f, 0.45f);

        [Tooltip("Warm-blue colour grade filter tint applied at peak Earthrise.")]
        [SerializeField] private Color _gradeColorFilter = new Color(0.86f, 0.93f, 1.00f, 1.00f);

        public float FadeInSeconds => _fadeInSeconds;
        public float HoldSeconds => _holdSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
        public float TotalDurationSeconds => _fadeInSeconds + _holdSeconds + _fadeOutSeconds;
        public Color EarthKeyColor => _earthKeyColor;
        public float EarthKeyIntensity => _earthKeyIntensity;
        public float StartElevationDegrees => _startElevationDegrees;
        public float PeakElevationDegrees => _peakElevationDegrees;
        public Color WarmRimColor => _warmRimColor;
        public float WarmRimIntensity => _warmRimIntensity;
        public Color AmbientSkyColor => _ambientSkyColor;
        public Color AmbientEquatorColor => _ambientEquatorColor;
        public Color AmbientGroundColor => _ambientGroundColor;
        public Color SkyBackgroundColor => _skyBackgroundColor;
        public Color HorizonHazeColor => _horizonHazeColor;
        public Color GradeColorFilter => _gradeColorFilter;

        /// <summary>
        /// Evaluates the eased blend weight (0..1) of the Earthrise preset at <paramref name="timeSeconds"/>.
        /// </summary>
        public float EvaluateBlendWeight(float timeSeconds)
        {
            if (timeSeconds <= 0f)
            {
                return 0f;
            }

            float fadeIn = Mathf.Max(0.01f, _fadeInSeconds);
            if (timeSeconds < fadeIn)
            {
                return Mathf.SmoothStep(0f, 1f, timeSeconds / fadeIn);
            }

            float holdEnd = fadeIn + Mathf.Max(0f, _holdSeconds);
            if (timeSeconds <= holdEnd)
            {
                return 1f;
            }

            float fadeOut = Mathf.Max(0.01f, _fadeOutSeconds);
            float outElapsed = timeSeconds - holdEnd;
            if (outElapsed >= fadeOut)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, outElapsed / fadeOut);
        }

        /// <summary>
        /// Evaluates the full interpolated <see cref="EarthriseLightState"/> between a baseline lighting configuration
        /// and the peak warm-blue Earthrise stillness preset for a given normalized <paramref name="blendWeight"/> (0..1).
        /// </summary>
        public EarthriseLightState EvaluateState(
            float blendWeight,
            Color baseKeyColor,
            float baseKeyIntensity,
            float baseKeyElevation,
            Color baseRimColor,
            float baseRimIntensity,
            Color baseAmbientSky,
            Color baseAmbientEquator,
            Color baseAmbientGround,
            Color baseSkyBackground)
        {
            float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendWeight));
            return new EarthriseLightState
            {
                BlendWeight = w,
                KeyLightColor = Color.Lerp(baseKeyColor, _earthKeyColor, w),
                KeyLightIntensity = Mathf.Lerp(baseKeyIntensity, _earthKeyIntensity, w),
                KeyElevationDegrees = Mathf.Lerp(baseKeyElevation, _peakElevationDegrees, w),
                RimLightColor = Color.Lerp(baseRimColor, _warmRimColor, w),
                RimLightIntensity = Mathf.Lerp(baseRimIntensity, _warmRimIntensity, w),
                AmbientSkyColor = Color.Lerp(baseAmbientSky, _ambientSkyColor, w),
                AmbientEquatorColor = Color.Lerp(baseAmbientEquator, _ambientEquatorColor, w),
                AmbientGroundColor = Color.Lerp(baseAmbientGround, _ambientGroundColor, w),
                SkyBackgroundColor = Color.Lerp(baseSkyBackground, _skyBackgroundColor, w),
                HorizonHazeColor = Color.Lerp(Color.clear, _horizonHazeColor, w),
                GradeColorFilter = Color.Lerp(Color.white, _gradeColorFilter, w),
            };
        }

        private void OnValidate()
        {
            _fadeInSeconds = Mathf.Clamp(_fadeInSeconds, 0.5f, 30f);
            _holdSeconds = Mathf.Clamp(_holdSeconds, 2.0f, 120f);
            _fadeOutSeconds = Mathf.Clamp(_fadeOutSeconds, 0.5f, 30f);
            _earthKeyIntensity = Mathf.Clamp(_earthKeyIntensity, 0.1f, 3.0f);
            _startElevationDegrees = Mathf.Clamp(_startElevationDegrees, 1.0f, 25.0f);
            _peakElevationDegrees = Mathf.Clamp(_peakElevationDegrees, 4.0f, 45.0f);
            _warmRimIntensity = Mathf.Clamp(_warmRimIntensity, 0.05f, 1.5f);
        }
    }
}
