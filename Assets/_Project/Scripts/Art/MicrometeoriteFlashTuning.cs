using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Tuning parameters for the 0.6 micrometeorite flash lunar phenomenon (docs/design/0.6-living-moon.md §2):
    /// a brief distant white starlight bloom flash and clean ballistic ejecta spray, revealing an irregular soft-rimmed
    /// small crater whose molten impact glass shards cool over ~10 minutes (600 s) from white-amber incandescence down
    /// to a faint permanent glint that stays collectable forever.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Art/Micrometeorite Flash Tuning", fileName = "MicrometeoriteFlashTuning")]
    public sealed class MicrometeoriteFlashTuning : ScriptableObject
    {
        [Header("Impact Flash Burst")]
        [Tooltip("Eased rise duration of the initial white impact flash in seconds (prevents a harsh frame-zero snap).")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _flashRiseSeconds = 0.03f;

        [Tooltip("Eased decay duration of the white impact flash in seconds.")]
        [Range(0.1f, 2.5f)]
        [SerializeField] private float _flashDecaySeconds = 0.22f;

        [Tooltip("Peak HDR intensity of the brief white impact flash burst.")]
        [Range(0.5f, 4.0f)]
        [SerializeField] private float _flashPeakIntensity = 2.15f;

        [Tooltip("Maximum visual radius of the compact impact bloom flash in metres.")]
        [Range(0.2f, 4.0f)]
        [SerializeField] private float _flashRadiusMeters = 0.92f;

        [Tooltip("Warm-white starlight colour of the brief impact flash.")]
        [SerializeField] private Color _flashColor = new Color(1.00f, 0.96f, 0.88f, 1.00f);

        [Header("Vacuum Ballistic Ejecta")]
        [Tooltip("Number of discrete ballistic ejecta shards launched at impact.")]
        [Range(6, 48)]
        [SerializeField] private int _ejectaCount = 24;

        [Tooltip("Initial launch speed of the fastest vacuum ejecta shards in metres per second.")]
        [Range(1.0f, 12.0f)]
        [SerializeField] private float _ejectaSpeedMetersPerSecond = 4.20f;

        [Tooltip("Lunar surface gravity governing the parabolic ejecta arcs in metres per second squared.")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float _lunarGravityMetersPerSecondSquared = 1.62f;

        [Tooltip("Duration in seconds until the last ballistic ejecta shards settle onto the crater apron.")]
        [Range(0.4f, 4.0f)]
        [SerializeField] private float _ejectaDurationSeconds = 1.65f;

        [Tooltip("Visual scale of individual faceted ejecta shards in metres.")]
        [Range(0.015f, 0.15f)]
        [SerializeField] private float _ejectaShardSizeMeters = 0.048f;

        [Header("Crater & 10-Minute Impact Glass Cool-Down")]
        [Tooltip("Radius of the small fresh impact crater rim in metres.")]
        [Range(0.25f, 2.5f)]
        [SerializeField] private float _craterRadiusMeters = 0.75f;

        [Tooltip("Total thermal cool-down duration in seconds (~10 minutes = 600 s) from molten glass to permanent glint.")]
        [Range(60f, 1200f)]
        [SerializeField] private float _coolDownDurationSeconds = 600.0f;

        [Tooltip("Radiative cooling exponent shaping the fast initial incandescent drop and long warm-ember tail.")]
        [Range(1.2f, 5.0f)]
        [SerializeField] private float _coolingExponent = 2.40f;

        [Tooltip("Initial peak linear emissive intensity of the freshly fused impact glass pool.")]
        [Range(0.5f, 3.5f)]
        [SerializeField] private float _initialHeatIntensity = 1.85f;

        [Tooltip("Incandescent white-amber colour of the impact glass immediately after impact.")]
        [SerializeField] private Color _hotCoreColor = new Color(1.00f, 0.94f, 0.80f, 1.00f);

        [Tooltip("Molten amber-orange colour of the impact glass during the early cooling phase.")]
        [SerializeField] private Color _warmAmberColor = new Color(1.00f, 0.46f, 0.14f, 1.00f);

        [Tooltip("Deep cherry-ember colour of the impact glass during late thermal cooling.")]
        [SerializeField] private Color _deepEmberColor = new Color(0.62f, 0.16f, 0.05f, 1.00f);

        [Tooltip("Dark vitreous albedo of the cooled lunar impactite glass.")]
        [SerializeField] private Color _glassAlbedoColor = new Color(0.10f, 0.13f, 0.18f, 1.00f);

        [Tooltip("Freshly overturned regolith albedo of the small crater bowl and terrain-decal rim.")]
        [SerializeField] private Color _craterRimAlbedoColor = new Color(0.25f, 0.27f, 0.32f, 1.00f);

        [Tooltip("Permanent non-zero glint intensity floor remaining after the 10-minute cool-down (< BloomThreshold).")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _permanentGlintFloor = 0.24f;

        [Tooltip("Pale starlight/earthlight tint of the permanent cooled impact glass glint.")]
        [SerializeField] private Color _permanentGlintColor = new Color(0.78f, 0.90f, 1.00f, 1.00f);

        public float FlashRiseSeconds => _flashRiseSeconds;
        public float FlashDecaySeconds => _flashDecaySeconds;
        public float FlashTotalSeconds => _flashRiseSeconds + _flashDecaySeconds;
        public float FlashPeakIntensity => _flashPeakIntensity;
        public float FlashRadiusMeters => _flashRadiusMeters;
        public Color FlashColor => _flashColor;
        public int EjectaCount => _ejectaCount;
        public float EjectaSpeedMetersPerSecond => _ejectaSpeedMetersPerSecond;
        public float LunarGravityMetersPerSecondSquared => _lunarGravityMetersPerSecondSquared;
        public float EjectaDurationSeconds => _ejectaDurationSeconds;
        public float EjectaShardSizeMeters => _ejectaShardSizeMeters;
        public float CraterRadiusMeters => _craterRadiusMeters;
        public float CoolDownDurationSeconds => _coolDownDurationSeconds;
        public float CoolingExponent => _coolingExponent;
        public float InitialHeatIntensity => _initialHeatIntensity;
        public Color HotCoreColor => _hotCoreColor;
        public Color WarmAmberColor => _warmAmberColor;
        public Color DeepEmberColor => _deepEmberColor;
        public Color GlassAlbedoColor => _glassAlbedoColor;
        public Color CraterRimAlbedoColor => _craterRimAlbedoColor;
        public float PermanentGlintFloor => _permanentGlintFloor;
        public Color PermanentGlintColor => _permanentGlintColor;

        /// <summary>
        /// Evaluates the normalized (0..1) brief white impact flash envelope at <paramref name="timeSeconds"/>.
        /// Rises smoothly over <see cref="FlashRiseSeconds"/> and decays smoothly over <see cref="FlashDecaySeconds"/>.
        /// </summary>
        public float EvaluateFlashEnvelope(float timeSeconds)
        {
            if (timeSeconds <= 0f)
            {
                return 0f;
            }

            float rise = Mathf.Max(0.005f, _flashRiseSeconds);
            if (timeSeconds < rise)
            {
                return Mathf.SmoothStep(0f, 1f, timeSeconds / rise);
            }

            float decay = Mathf.Max(0.01f, _flashDecaySeconds);
            float decayElapsed = timeSeconds - rise;
            if (decayElapsed >= decay)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, decayElapsed / decay);
        }

        /// <summary>
        /// Evaluates the normalized (0..1) thermal heat of the impact glass at <paramref name="timeSeconds"/>.
        /// Starts at 1.0 upon impact and cools smoothly over <see cref="CoolDownDurationSeconds"/> (~600 s) to 0.0.
        /// </summary>
        public float EvaluateNormalizedHeat(float timeSeconds)
        {
            if (timeSeconds <= 0f)
            {
                return 1f;
            }

            float duration = Mathf.Max(1f, _coolDownDurationSeconds);
            if (timeSeconds >= duration)
            {
                return 0f;
            }

            float normalizedTime = Mathf.Clamp01(timeSeconds / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, normalizedTime);
            return Mathf.Pow(1f - easedProgress, Mathf.Max(1f, _coolingExponent));
        }

        /// <summary>
        /// Evaluates the linear emissive/glint colour of the impact glass at <paramref name="timeSeconds"/>,
        /// transitioning from white-hot core through molten amber and deep ember down to the permanent glint floor.
        /// </summary>
        public Color EvaluateGlassEmission(float timeSeconds, out float effectiveIntensity)
        {
            float heat = EvaluateNormalizedHeat(timeSeconds);
            Color thermalTone;
            if (heat > 0.55f)
            {
                float tHot = Mathf.Clamp01((heat - 0.55f) / 0.45f);
                thermalTone = Color.Lerp(_warmAmberColor, _hotCoreColor, tHot);
            }
            else
            {
                float tWarm = Mathf.Clamp01(heat / 0.55f);
                thermalTone = Color.Lerp(_deepEmberColor, _warmAmberColor, tWarm);
            }

            float thermalIntensity = heat * _initialHeatIntensity;
            effectiveIntensity = Mathf.Max(_permanentGlintFloor, thermalIntensity);

            Color thermalRgb = thermalTone * thermalIntensity;
            Color glintRgb = _permanentGlintColor * _permanentGlintFloor;
            return new Color(
                Mathf.Max(thermalRgb.r, glintRgb.r),
                Mathf.Max(thermalRgb.g, glintRgb.g),
                Mathf.Max(thermalRgb.b, glintRgb.b),
                1f);
        }

        /// <summary>
        /// Evaluates the deterministic vacuum ballistic position and alpha (0..1) of ejecta shard
        /// <paramref name="shardIndex"/> at <paramref name="timeSeconds"/> under lunar gravity (1.62 m/s^2, zero air drag).
        /// </summary>
        public Vector3 EvaluateEjectaShard(int shardIndex, float timeSeconds, out float shardAlpha)
        {
            if (timeSeconds <= 0f || timeSeconds >= _ejectaDurationSeconds)
            {
                shardAlpha = 0f;
                return Vector3.zero;
            }

            float seed = Frac(Mathf.Sin((shardIndex + 1) * 91.137f) * 43758.5453f);
            float azimuth = Frac((shardIndex + 1) * 0.6180339887f + seed * 0.21f) * 6.2831853f;

            // Low-angle vacuum ejecta curtain (~20..44 degrees above horizontal, no vertical beam)
            float elevationRad = Mathf.Lerp(20f, 44f, Frac(seed * 5.17f)) * Mathf.Deg2Rad;
            float speed = _ejectaSpeedMetersPerSecond * Mathf.Lerp(0.38f, 0.96f, Frac(seed * 11.3f));

            Vector3 radialDir = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
            Vector3 startPos = radialDir * (_craterRadiusMeters * 0.26f) + new Vector3(0f, 0.02f, 0f);
            Vector3 velocity = radialDir * (Mathf.Cos(elevationRad) * speed)
                + new Vector3(0f, Mathf.Sin(elevationRad) * speed, 0f);

            float x = startPos.x + velocity.x * timeSeconds;
            float z = startPos.z + velocity.z * timeSeconds;
            float y = startPos.y + velocity.y * timeSeconds
                - 0.5f * _lunarGravityMetersPerSecondSquared * timeSeconds * timeSeconds;

            if (y <= 0f)
            {
                shardAlpha = 0f;
                return new Vector3(x, 0f, z);
            }

            float normalizedLife = Mathf.Clamp01(timeSeconds / Mathf.Max(0.05f, _ejectaDurationSeconds));
            shardAlpha = Mathf.SmoothStep(0f, 0.08f, normalizedLife) * (1f - Mathf.SmoothStep(0.65f, 1f, normalizedLife));
            return new Vector3(x, y, z);
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        private void OnValidate()
        {
            _flashRiseSeconds = Mathf.Clamp(_flashRiseSeconds, 0.02f, 0.5f);
            _flashDecaySeconds = Mathf.Clamp(_flashDecaySeconds, 0.1f, 2.5f);
            _flashPeakIntensity = Mathf.Clamp(_flashPeakIntensity, 0.5f, 4.0f);
            _flashRadiusMeters = Mathf.Clamp(_flashRadiusMeters, 0.2f, 4.0f);
            _ejectaCount = Mathf.Clamp(_ejectaCount, 6, 48);
            _ejectaSpeedMetersPerSecond = Mathf.Clamp(_ejectaSpeedMetersPerSecond, 1.0f, 12.0f);
            _lunarGravityMetersPerSecondSquared = Mathf.Clamp(_lunarGravityMetersPerSecondSquared, 0.5f, 5.0f);
            _ejectaDurationSeconds = Mathf.Clamp(_ejectaDurationSeconds, 0.4f, 4.0f);
            _ejectaShardSizeMeters = Mathf.Clamp(_ejectaShardSizeMeters, 0.015f, 0.15f);
            _craterRadiusMeters = Mathf.Clamp(_craterRadiusMeters, 0.25f, 2.5f);
            _coolDownDurationSeconds = Mathf.Clamp(_coolDownDurationSeconds, 60f, 1200f);
            _coolingExponent = Mathf.Clamp(_coolingExponent, 1.2f, 5.0f);
            _initialHeatIntensity = Mathf.Clamp(_initialHeatIntensity, 0.5f, 3.5f);
            _permanentGlintFloor = Mathf.Clamp(_permanentGlintFloor, 0.05f, 0.8f);
        }
    }
}
