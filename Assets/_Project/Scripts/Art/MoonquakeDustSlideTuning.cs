using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Tuning parameters for the 0.6 gentle moonquake dust slide lunar phenomenon (docs/design/0.6-living-moon.md §2):
    /// vacuum-honest regolith sheeting slowly down a slope with discrete ballistic grain arcs under lunar gravity
    /// (1.62 m/s^2) and a low ground-skimming settle apron at the toe (zero billowing smoke).
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Art/Moonquake Dust Slide Tuning", fileName = "MoonquakeDustSlideTuning")]
    public sealed class MoonquakeDustSlideTuning : ScriptableObject
    {
        [Header("Slide Envelope Timing")]
        [Tooltip("Eased ramp-up duration in seconds as the seismic tremor loosens surface regolith.")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _rampUpSeconds = 1.8f;

        [Tooltip("Active down-slope sheeting flow duration in seconds.")]
        [Range(1.0f, 30f)]
        [SerializeField] private float _slideDurationSeconds = 7.5f;

        [Tooltip("Ballistic settling duration in seconds as grains drop cleanly to rest at the toe.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _settleDurationSeconds = 3.2f;

        [Header("Slope Geometry & Vacuum Physics")]
        [Tooltip("Length of the regolith slide path down the slope in metres.")]
        [Range(2.0f, 30f)]
        [SerializeField] private float _slopeLengthMeters = 6.2f;

        [Tooltip("Width of the sheeting regolith streams across the slope in metres.")]
        [Range(1.0f, 20f)]
        [SerializeField] private float _slopeWidthMeters = 4.6f;

        [Tooltip("Inclination angle of the lunar slope in degrees.")]
        [Range(10f, 55f)]
        [SerializeField] private float _slopeAngleDegrees = 25.0f;

        [Tooltip("Lunar surface gravity governing ballistic grain arcs in metres per second squared.")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float _lunarGravityMetersPerSecondSquared = 1.62f;

        [Tooltip("Down-slope sheeting velocity of the regolith streams in metres per second.")]
        [Range(0.3f, 6.0f)]
        [SerializeField] private float _sheetSpeedMetersPerSecond = 1.65f;

        [Tooltip("Low-angle ballistic hop speed of saltating regolith grain clusters in metres per second.")]
        [Range(0.2f, 4.0f)]
        [SerializeField] private float _ballisticHopSpeedMetersPerSecond = 1.15f;

        [Tooltip("Maximum ground-skimming height of the vacuum toe settle veil in metres (never billows upward).")]
        [Range(0.05f, 1.2f)]
        [SerializeField] private float _settleCloudMaxHeightMeters = 0.42f;

        [Header("Ballistic Grain Pool & Look")]
        [Tooltip("Number of discrete angular regolith grain clusters in the ballistic particle pool.")]
        [Range(8, 64)]
        [SerializeField] private int _grainCount = 36;

        [Tooltip("Characteristic size of each angular regolith grain cluster in metres.")]
        [Range(0.02f, 0.25f)]
        [SerializeField] private float _grainSizeMeters = 0.085f;

        [Tooltip("Day C (low golden sun) lit regolith dust colour and base alpha.")]
        [SerializeField] private Color _dayDustColor = new Color(0.76f, 0.72f, 0.66f, 0.86f);

        [Tooltip("Night A (cool earthshine) lit regolith dust colour and base alpha.")]
        [SerializeField] private Color _nightDustColor = new Color(0.54f, 0.62f, 0.78f, 0.82f);

        [Tooltip("Self-shadowed regolith trough tint.")]
        [SerializeField] private Color _shadowDustColor = new Color(0.28f, 0.32f, 0.42f, 1.00f);

        [Tooltip("Peak opacity of the sheeting regolith stream during active flow.")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float _maxOpacity = 0.84f;

        [Tooltip("Bloom-safe ceiling enforced on dust shaders so regolith never blooms.")]
        [Range(0.4f, 1.20f)]
        [SerializeField] private float _bloomCeiling = 1.05f;

        public float RampUpSeconds => _rampUpSeconds;
        public float SlideDurationSeconds => _slideDurationSeconds;
        public float SettleDurationSeconds => _settleDurationSeconds;
        public float TotalDurationSeconds => _rampUpSeconds + _slideDurationSeconds + _settleDurationSeconds;
        public float SlopeLengthMeters => _slopeLengthMeters;
        public float SlopeWidthMeters => _slopeWidthMeters;
        public float SlopeAngleDegrees => _slopeAngleDegrees;
        public float LunarGravityMetersPerSecondSquared => _lunarGravityMetersPerSecondSquared;
        public float SheetSpeedMetersPerSecond => _sheetSpeedMetersPerSecond;
        public float BallisticHopSpeedMetersPerSecond => _ballisticHopSpeedMetersPerSecond;
        public float SettleCloudMaxHeightMeters => _settleCloudMaxHeightMeters;
        public int GrainCount => _grainCount;
        public float GrainSizeMeters => _grainSizeMeters;
        public Color DayDustColor => _dayDustColor;
        public Color NightDustColor => _nightDustColor;
        public Color ShadowDustColor => _shadowDustColor;
        public float MaxOpacity => _maxOpacity;
        public float BloomCeiling => _bloomCeiling;

        /// <summary>
        /// Evaluates the eased sheeting stream envelope (0..1) at <paramref name="timeSeconds"/>.
        /// </summary>
        public float EvaluateSheetEnvelope(float timeSeconds)
        {
            if (timeSeconds <= 0f)
            {
                return 0f;
            }

            float ramp = Mathf.Max(0.01f, _rampUpSeconds);
            if (timeSeconds < ramp)
            {
                return Mathf.SmoothStep(0f, 1f, timeSeconds / ramp);
            }

            float slideEnd = ramp + Mathf.Max(0f, _slideDurationSeconds);
            if (timeSeconds <= slideEnd)
            {
                return 1f;
            }

            float settle = Mathf.Max(0.01f, _settleDurationSeconds);
            float settleElapsed = timeSeconds - slideEnd;
            if (settleElapsed >= settle)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, settleElapsed / settle);
        }

        /// <summary>
        /// Evaluates the low vacuum toe settle veil envelope (0..1) at <paramref name="timeSeconds"/>.
        /// Builds as sheeting grains reach the toe of the slope and drops cleanly to zero under lunar gravity.
        /// </summary>
        public float EvaluateToeSettleEnvelope(float timeSeconds)
        {
            float delay = _rampUpSeconds * 0.45f;
            if (timeSeconds <= delay)
            {
                return 0f;
            }

            float buildDuration = Mathf.Max(0.1f, _rampUpSeconds * 0.85f + _slideDurationSeconds * 0.35f);
            float activeElapsed = timeSeconds - delay;
            if (activeElapsed < buildDuration)
            {
                return Mathf.SmoothStep(0f, 1f, activeElapsed / buildDuration);
            }

            float slideEnd = _rampUpSeconds + _slideDurationSeconds;
            if (timeSeconds <= slideEnd)
            {
                return 1f;
            }

            float settle = Mathf.Max(0.01f, _settleDurationSeconds);
            float fallElapsed = timeSeconds - slideEnd;
            if (fallElapsed >= settle)
            {
                return 0f;
            }

            // Ballistic drop: quadratic gravity falloff (no lingering atmospheric suspension)
            float normalizedFall = Mathf.Clamp01(fallElapsed / settle);
            float remaining = 1f - normalizedFall * normalizedFall;
            return Mathf.SmoothStep(0f, 1f, remaining);
        }

        /// <summary>
        /// Evaluates the deterministic local position and alpha (0..1) of ballistic regolith grain
        /// <paramref name="grainIndex"/> at <paramref name="timeSeconds"/> in the slope's local coordinate frame
        /// (where local +Z runs down the horizontal projection of the slope and local +Y is vertical).
        /// Every grain follows a strict parabolic arc under lunar gravity (1.62 m/s^2) with zero upward buoyancy.
        /// </summary>
        public Vector3 EvaluateBallisticGrain(int grainIndex, float timeSeconds, out float grainAlpha)
        {
            float sheetEnvelope = EvaluateSheetEnvelope(timeSeconds);
            if (sheetEnvelope <= 0.001f)
            {
                grainAlpha = 0f;
                return Vector3.zero;
            }

            int count = Mathf.Max(1, _grainCount);
            float seed = Frac(Mathf.Sin((grainIndex + 1) * 57.319f) * 43758.5453f);
            // Irregular non-periodic placement clustered around the asymmetric avalanche chutes (no even spacing)
            float chuteAnchor = (grainIndex % 3 == 0) ? 0.24f : -0.21f;
            float spread = (Frac((grainIndex + 1) * 0.6180339887f + seed * 0.37f) - 0.5f) * 0.48f;
            float laneX = Mathf.Clamp(chuteAnchor + spread, -0.46f, 0.46f) * _slopeWidthMeters;

            float slopeRad = _slopeAngleDegrees * Mathf.Deg2Rad;
            float cosSlope = Mathf.Cos(slopeRad);
            float sinSlope = Mathf.Sin(slopeRad);

            // Hop cycle duration in vacuum: t_flight = 2 * v_perp / (g * cos(theta))
            float hopNormalSpeed = _ballisticHopSpeedMetersPerSecond * Mathf.Lerp(0.32f, 0.72f, Frac(seed * 3.7f));
            float effectiveGravityNormal = Mathf.Max(0.2f, _lunarGravityMetersPerSecondSquared * cosSlope);
            float hopPeriod = Mathf.Max(0.35f, (2f * hopNormalSpeed) / effectiveGravityNormal);

            // Distance along slope (0 = top crest, _slopeLengthMeters = toe)
            float travelRate = _sheetSpeedMetersPerSecond * Mathf.Lerp(0.82f, 1.18f, seed);
            float alongSlope = Frac((timeSeconds * travelRate / Mathf.Max(0.5f, _slopeLengthMeters)) + seed) * _slopeLengthMeters;

            // Parabolic ballistic hop height perpendicular to the slope: h(u) = 4 * h_max * u * (1 - u)
            float hopPhase = Frac((timeSeconds / hopPeriod) + seed * 4.13f);
            float maxHopHeight = (hopNormalSpeed * hopNormalSpeed) / (2f * effectiveGravityNormal);
            maxHopHeight = Mathf.Min(maxHopHeight, _settleCloudMaxHeightMeters);
            float hopHeight = 4f * maxHopHeight * hopPhase * (1f - hopPhase);

            float topY = _slopeLengthMeters * sinSlope;
            float baseSlopeY = topY - alongSlope * sinSlope;
            float baseSlopeZ = alongSlope * cosSlope;

            Vector3 pos = new Vector3(
                laneX,
                baseSlopeY + hopHeight * cosSlope,
                baseSlopeZ + hopHeight * sinSlope);

            float edgeFade = Mathf.SmoothStep(0f, 0.14f, alongSlope / _slopeLengthMeters)
                * Mathf.SmoothStep(1f, 0.84f, alongSlope / _slopeLengthMeters);
            grainAlpha = Mathf.Clamp01(sheetEnvelope * edgeFade);
            return pos;
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        private void OnValidate()
        {
            _rampUpSeconds = Mathf.Clamp(_rampUpSeconds, 0.2f, 10f);
            _slideDurationSeconds = Mathf.Clamp(_slideDurationSeconds, 1.0f, 30f);
            _settleDurationSeconds = Mathf.Clamp(_settleDurationSeconds, 0.5f, 15f);
            _slopeLengthMeters = Mathf.Clamp(_slopeLengthMeters, 2.0f, 30f);
            _slopeWidthMeters = Mathf.Clamp(_slopeWidthMeters, 1.0f, 20f);
            _slopeAngleDegrees = Mathf.Clamp(_slopeAngleDegrees, 10f, 55f);
            _lunarGravityMetersPerSecondSquared = Mathf.Clamp(_lunarGravityMetersPerSecondSquared, 0.5f, 5.0f);
            _sheetSpeedMetersPerSecond = Mathf.Clamp(_sheetSpeedMetersPerSecond, 0.3f, 6.0f);
            _ballisticHopSpeedMetersPerSecond = Mathf.Clamp(_ballisticHopSpeedMetersPerSecond, 0.2f, 4.0f);
            _settleCloudMaxHeightMeters = Mathf.Clamp(_settleCloudMaxHeightMeters, 0.05f, 1.2f);
            _grainCount = Mathf.Clamp(_grainCount, 8, 64);
            _grainSizeMeters = Mathf.Clamp(_grainSizeMeters, 0.02f, 0.25f);
            _maxOpacity = Mathf.Clamp(_maxOpacity, 0.1f, 1.0f);
            _bloomCeiling = Mathf.Clamp(_bloomCeiling, 0.4f, 1.20f);
        }
    }
}
