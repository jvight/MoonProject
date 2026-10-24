using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Tuning parameters for the 0.6 levitating dust glow lunar phenomenon (docs/design/0.6-living-moon.md §2):
    /// a soft forward-scattered horizon haze band along the terminator ridge paired with thin vertical columns of
    /// slowly rising electrostatically levitated regolith motes.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Art/Levitating Dust Glow Tuning", fileName = "LevitatingDustGlowTuning")]
    public sealed class LevitatingDustGlowTuning : ScriptableObject
    {
        [Header("Envelope Timing")]
        [Tooltip("Eased fade-in duration in seconds as the terminator electrostatic field wakes.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _fadeInSeconds = 4.0f;

        [Tooltip("Steady shimmer plateau duration in seconds at full terminator haze visibility.")]
        [Range(1.0f, 120f)]
        [SerializeField] private float _holdSeconds = 16.0f;

        [Tooltip("Eased fade-out duration in seconds as the levitated dust settles back to the regolith.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _fadeOutSeconds = 6.0f;

        [Header("Horizon Haze Band")]
        [Tooltip("Horizontal span of the terminator horizon haze band in metres.")]
        [Range(4f, 120f)]
        [SerializeField] private float _bandWidthMeters = 24.0f;

        [Tooltip("Vertical height of the horizon haze band above the ridge in metres.")]
        [Range(0.5f, 12f)]
        [SerializeField] private float _bandHeightMeters = 3.4f;

        [Tooltip("Day C (grazing golden terminator sun) colour and alpha of the horizon haze band.")]
        [SerializeField] private Color _dayBandColor = new Color(0.95f, 0.83f, 0.64f, 0.78f);

        [Tooltip("Night A (cool earthshine) colour and alpha of the horizon haze band.")]
        [SerializeField] private Color _nightBandColor = new Color(0.64f, 0.78f, 0.98f, 0.74f);

        [Tooltip("Electrostatic edge shimmer tint of the rising dust columns.")]
        [SerializeField] private Color _shimmerRimColor = new Color(1.00f, 0.92f, 0.78f, 1.00f);

        [Tooltip("Peak linear intensity of the horizon haze band (kept strictly below Palette.BloomThreshold = 1.25).")]
        [Range(0.05f, 1.15f)]
        [SerializeField] private float _bandMaxIntensity = 0.88f;

        [Header("Rising Electrostatic Mote Columns")]
        [Tooltip("Number of thin vertical mote columns distributed across the horizon haze span.")]
        [Range(2, 16)]
        [SerializeField] private int _columnCount = 9;

        [Tooltip("Number of discrete faceted dust motes per vertical column.")]
        [Range(4, 32)]
        [SerializeField] private int _motesPerColumn = 12;

        [Tooltip("Horizontal radius of each thin rising mote column in metres.")]
        [Range(0.08f, 1.5f)]
        [SerializeField] private float _columnRadiusMeters = 0.42f;

        [Tooltip("Maximum height reached by the rising mote columns in metres.")]
        [Range(0.8f, 8.0f)]
        [SerializeField] private float _columnHeightMeters = 3.8f;

        [Tooltip("Slow upward electrostatic drift velocity of the dust motes in metres per second.")]
        [Range(0.02f, 0.8f)]
        [SerializeField] private float _riseSpeedMetersPerSecond = 0.24f;

        [Tooltip("Characteristic visual size of each elongated crystalline dust mote in metres.")]
        [Range(0.015f, 0.16f)]
        [SerializeField] private float _moteScaleMeters = 0.065f;

        [Tooltip("Peak linear intensity of the rising mote columns (strictly below Palette.BloomThreshold = 1.25).")]
        [Range(0.1f, 1.2f)]
        [SerializeField] private float _moteMaxIntensity = 1.06f;

        [Tooltip("Slow electrostatic shimmer frequency in hertz.")]
        [Range(0.05f, 2.0f)]
        [SerializeField] private float _shimmerFrequencyHz = 0.35f;

        [Tooltip("Bloom-safe ceiling enforced on shader output so the broad haze never washes out the frame.")]
        [Range(0.5f, 1.24f)]
        [SerializeField] private float _bloomCeiling = 1.15f;

        public float FadeInSeconds => _fadeInSeconds;
        public float HoldSeconds => _holdSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
        public float TotalDurationSeconds => _fadeInSeconds + _holdSeconds + _fadeOutSeconds;
        public float BandWidthMeters => _bandWidthMeters;
        public float BandHeightMeters => _bandHeightMeters;
        public Color DayBandColor => _dayBandColor;
        public Color NightBandColor => _nightBandColor;
        public Color ShimmerRimColor => _shimmerRimColor;
        public float BandMaxIntensity => _bandMaxIntensity;
        public int ColumnCount => _columnCount;
        public int MotesPerColumn => _motesPerColumn;
        public int TotalMoteCount => _columnCount * _motesPerColumn;
        public float ColumnRadiusMeters => _columnRadiusMeters;
        public float ColumnHeightMeters => _columnHeightMeters;
        public float RiseSpeedMetersPerSecond => _riseSpeedMetersPerSecond;
        public float MoteScaleMeters => _moteScaleMeters;
        public float MoteMaxIntensity => _moteMaxIntensity;
        public float ShimmerFrequencyHz => _shimmerFrequencyHz;
        public float BloomCeiling => _bloomCeiling;

        /// <summary>
        /// Evaluates the eased visibility envelope (0..1) at <paramref name="timeSeconds"/> into the phenomenon.
        /// Uses SmoothStep on both rise and fall so there is never a hard snap on frame zero or completion.
        /// </summary>
        public float EvaluateEnvelope(float timeSeconds)
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

            float plateauEnd = fadeIn + Mathf.Max(0f, _holdSeconds);
            if (timeSeconds <= plateauEnd)
            {
                return 1f;
            }

            float fadeOut = Mathf.Max(0.01f, _fadeOutSeconds);
            float fallElapsed = timeSeconds - plateauEnd;
            if (fallElapsed >= fadeOut)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, fallElapsed / fadeOut);
        }

        /// <summary>
        /// Computes the deterministic local position and eased alpha (0..1) of a single electrostatically levitated
        /// mote in column <paramref name="columnIndex"/> and slot <paramref name="moteIndex"/> at
        /// <paramref name="timeSeconds"/>.
        /// </summary>
        public Vector3 EvaluateMotePosition(int columnIndex, int moteIndex, float timeSeconds, out float moteAlpha)
        {
            int columns = Mathf.Max(1, _columnCount);
            int motes = Mathf.Max(1, _motesPerColumn);
            float normalizedColumn = columns == 1 ? 0f : (columnIndex / (float)(columns - 1)) - 0.5f;

            // Deterministic irregular column offset so columns never form a mechanical grid
            float colHash = Frac(Mathf.Sin((columnIndex + 1) * 12.9898f) * 43758.5453f);
            float xCenter = normalizedColumn * (_bandWidthMeters * 0.78f) + (colHash - 0.5f) * (_bandWidthMeters * 0.06f);
            float zCenter = (Frac(colHash * 7.13f) - 0.5f) * 0.85f;

            float moteSeed = Frac(Mathf.Sin((columnIndex * 31 + moteIndex + 1) * 78.233f) * 43758.5453f);
            float angle = (moteSeed * 6.2831853f) + timeSeconds * 0.18f * (moteIndex % 2 == 0 ? 1f : -1f);
            float radialFraction = 0.25f + 0.75f * Frac(moteSeed * 3.71f);
            float radius = _columnRadiusMeters * radialFraction;

            float height = Mathf.Max(0.2f, _columnHeightMeters);
            float basePhase = (moteIndex / (float)motes) + colHash * 0.37f;
            float normalizedY = Frac(basePhase + (Mathf.Max(0f, timeSeconds) * _riseSpeedMetersPerSecond / height));
            float y = normalizedY * height;

            // Smooth fade at bottom launch and top dispersion of the column
            float verticalFade = Mathf.SmoothStep(0f, 0.22f, normalizedY) * Mathf.SmoothStep(1f, 0.68f, normalizedY);
            float shimmer = 0.78f + 0.22f * Mathf.Sin((timeSeconds * _shimmerFrequencyHz + moteSeed) * 6.2831853f);
            moteAlpha = Mathf.Clamp01(verticalFade * shimmer);

            return new Vector3(
                xCenter + Mathf.Cos(angle) * radius,
                y,
                zCenter + Mathf.Sin(angle) * (radius * 0.45f));
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        private void OnValidate()
        {
            _fadeInSeconds = Mathf.Clamp(_fadeInSeconds, 0.5f, 30f);
            _holdSeconds = Mathf.Clamp(_holdSeconds, 1.0f, 120f);
            _fadeOutSeconds = Mathf.Clamp(_fadeOutSeconds, 0.5f, 30f);
            _bandWidthMeters = Mathf.Clamp(_bandWidthMeters, 4f, 120f);
            _bandHeightMeters = Mathf.Clamp(_bandHeightMeters, 0.5f, 12f);
            _bandMaxIntensity = Mathf.Clamp(_bandMaxIntensity, 0.05f, 1.15f);
            _columnCount = Mathf.Clamp(_columnCount, 2, 16);
            _motesPerColumn = Mathf.Clamp(_motesPerColumn, 4, 32);
            _columnRadiusMeters = Mathf.Clamp(_columnRadiusMeters, 0.08f, 1.5f);
            _columnHeightMeters = Mathf.Clamp(_columnHeightMeters, 0.8f, 8.0f);
            _riseSpeedMetersPerSecond = Mathf.Clamp(_riseSpeedMetersPerSecond, 0.02f, 0.8f);
            _moteScaleMeters = Mathf.Clamp(_moteScaleMeters, 0.015f, 0.16f);
            _moteMaxIntensity = Mathf.Clamp(_moteMaxIntensity, 0.1f, 1.2f);
            _shimmerFrequencyHz = Mathf.Clamp(_shimmerFrequencyHz, 0.05f, 2.0f);
            _bloomCeiling = Mathf.Clamp(_bloomCeiling, 0.5f, 1.24f);
        }
    }
}
