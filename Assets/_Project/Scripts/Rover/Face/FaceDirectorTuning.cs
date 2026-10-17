using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Authoritative tuning asset for <see cref="FaceDirector"/> and <see cref="RoverFaceDirector"/> (0.5.7),
    /// holding every priority, hold duration, event threshold, low-power ramp, night-idle drift, eye-lead, and
    /// rear-vent emission parameter.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Rover/Face Director Tuning", fileName = "FaceDirectorTuning")]
    public sealed class FaceDirectorTuning : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Tuning/FaceDirectorTuning.asset";

        [Header("Priorities (Higher Wins)")]
        [Tooltip("Priority of Asleep while charging on a pad or beacon (overrides low battery, low power, and short beats).")]
        [Min(0)]
        [SerializeField] private int _chargingPriority = 100;

        [Tooltip("Priority of Tired when battery is empty or in low-power crawl mode.")]
        [Min(0)]
        [SerializeField] private int _lowPowerPriority = 90;

        [Tooltip("Priority of the Startled beat on hard landing, fall, or tumble recovery.")]
        [Min(0)]
        [SerializeField] private int _startledPriority = 85;

        [Tooltip("Priority of the Proud beat when an upgrade is purchased or equipment is fitted.")]
        [Min(0)]
        [SerializeField] private int _proudPriority = 75;

        [Tooltip("Priority of the Happy beat when a fragment is scanned or a blueprint is completed.")]
        [Min(0)]
        [SerializeField] private int _happyPriority = 70;

        [Tooltip("Priority of Curious while the scanner is actively sweeping or locked onto a target.")]
        [Min(0)]
        [SerializeField] private int _scanningPriority = 65;

        [Tooltip("Priority of Worried when battery is below the low-battery warning threshold.")]
        [Min(0)]
        [SerializeField] private int _lowBatteryPriority = 50;

        [Tooltip("Priority of the gentle Curious glance when Earth is in view (yields to battery warnings and active beats).")]
        [Min(0)]
        [SerializeField] private int _earthGlancePriority = 20;

        [Tooltip("Base priority of the default Calm state and night-idle Calm state.")]
        [Min(0)]
        [SerializeField] private int _calmPriority = 10;

        [Header("Hold Times")]
        [Tooltip("Minimum duration in seconds a feeling is held before transitioning to a lower-priority feeling, preventing flicker.")]
        [Range(0.05f, 2.0f)]
        [SerializeField] private float _minFeelingHoldSeconds = 0.35f;

        [Tooltip("Minimum duration in seconds Curious is held once scanning starts so brief scan taps never flicker.")]
        [Range(0.10f, 2.0f)]
        [SerializeField] private float _scanMinHoldSeconds = 0.45f;

        [Tooltip("Duration in seconds of the short Happy beat after scanning a fragment.")]
        [Range(0.30f, 5.0f)]
        [SerializeField] private float _fragmentHappyHoldSeconds = 1.60f;

        [Tooltip("Duration in seconds of the longer Happy beat after completing a full blueprint.")]
        [Range(0.50f, 8.0f)]
        [SerializeField] private float _blueprintHappyHoldSeconds = 3.20f;

        [Tooltip("Duration in seconds of the Proud beat after purchasing an upgrade or fitting equipment.")]
        [Range(0.50f, 6.0f)]
        [SerializeField] private float _proudHoldSeconds = 2.40f;

        [Tooltip("Duration in seconds of the brief Startled beat on a hard landing or fall.")]
        [Range(0.15f, 3.0f)]
        [SerializeField] private float _startledHoldSeconds = 0.75f;

        [Tooltip("Duration in seconds of the gentle Curious glance when Earth enters view.")]
        [Range(0.50f, 6.0f)]
        [SerializeField] private float _earthGlanceHoldSeconds = 2.20f;

        [Tooltip("Minimum cooldown in seconds between successive Earth-in-view Curious glances.")]
        [Range(1.0f, 60.0f)]
        [SerializeField] private float _earthGlanceCooldownSeconds = 10.0f;

        [Header("Thresholds")]
        [Tooltip("Normalized battery charge (0..1) below which 07 enters Worried with the battery symbol (matches BatteryTuning.WarnThreshold = 0.30).")]
        [Range(0.05f, 0.50f)]
        [SerializeField] private float _lowBatteryThreshold = 0.30f;

        [Tooltip("Normalized battery charge (0..1) above which low-battery Worried clears (hysteresis prevents threshold chatter).")]
        [Range(0.05f, 0.60f)]
        [SerializeField] private float _lowBatteryClearThreshold = 0.33f;

        [Tooltip("Downward touchdown speed in m/s (RoverLanded.ImpactSpeed) above which 07 plays the brief Startled beat.")]
        [Range(1.0f, 15.0f)]
        [SerializeField] private float _hardLandingImpactThreshold = 3.5f;

        [Tooltip("Downward vertical speed in m/s while airborne that triggers a brief Startled beat.")]
        [Range(1.0f, 20.0f)]
        [SerializeField] private float _fallVerticalSpeedThreshold = 5.5f;

        [Tooltip("Minimum airborne duration in seconds before downward speed triggers a fall Startled beat.")]
        [Range(0.10f, 3.0f)]
        [SerializeField] private float _fallMinAirborneSeconds = 0.45f;

        [Tooltip("Minimum dot product between the head forward axis and the direction to Earth to count Earth as in view.")]
        [Range(0.10f, 0.98f)]
        [SerializeField] private float _earthViewMinDot = 0.55f;

        [Tooltip("Planar speed in m/s below which 07 is considered stationary for night-idle detection.")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float _idleSpeedThreshold = 0.15f;

        [Header("Low Power & Night Idle")]
        [Tooltip("Seconds for FaceState.LowPower to ramp from 0 to 1 when entering empty-battery or crawl mode.")]
        [Range(0.10f, 5.0f)]
        [SerializeField] private float _lowPowerRampSeconds = 0.80f;

        [Tooltip("Seconds for FaceState.LowPower to ramp back to 0 when leaving low-power mode or starting to charge.")]
        [Range(0.05f, 3.0f)]
        [SerializeField] private float _lowPowerRecoverSeconds = 0.40f;

        [Tooltip("Stationary time in seconds at night (out of sunlight) before 07 enters slow night-idle Calm looks.")]
        [Range(0.50f, 20.0f)]
        [SerializeField] private float _nightIdleDelaySeconds = 4.0f;

        [Tooltip("Horizontal head-space look amplitude during slow night-idle Calm looks.")]
        [Range(0.05f, 1.0f)]
        [SerializeField] private float _nightSlowLookAmplitudeX = 0.35f;

        [Tooltip("Vertical head-space look amplitude during slow night-idle Calm looks.")]
        [Range(0.02f, 0.8f)]
        [SerializeField] private float _nightSlowLookAmplitudeY = 0.18f;

        [Tooltip("Cycle period in seconds of the slow sinusoidal look drift during night idle.")]
        [Range(1.5f, 20.0f)]
        [SerializeField] private float _nightSlowLookPeriodSeconds = 6.5f;

        [Header("Gaze & Look Lead")]
        [Tooltip("How strongly the un-slewed angle between current head aim and target gaze boosts the eye LookAt vector so the eyes lead the head slightly.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _eyeLeadFactor = 0.35f;

        [Tooltip("Angular offset in degrees at which the head-space look vector reaches unit magnitude for FaceState.LookAt.")]
        [Range(5.0f, 90.0f)]
        [SerializeField] private float _lookDirectionFullScaleDegrees = 35.0f;

        [Header("Rear Vent Strip Echo")]
        [Tooltip("Baseline _EmissionStrength on RearVentStrip when the CRT screen is at full normal brightness (1.0).")]
        [Range(0.10f, 3.0f)]
        [SerializeField] private float _rearVentBaseEmission = 1.45f;

        [Tooltip("Minimum _EmissionStrength on RearVentStrip when the CRT screen is dimmed or booting.")]
        [Range(0f, 1.0f)]
        [SerializeField] private float _rearVentMinEmission = 0.08f;

        [Tooltip("Multiplier applied to RearVentStrip _EmissionStrength during Happy and Proud feelings so mood reads from behind.")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float _rearVentHappyBoost = 1.25f;

        [Tooltip("Subtle pulse amplitude on RearVentStrip _EmissionStrength while Worried.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _rearVentWorriedPulseAmplitude = 0.18f;

        [Tooltip("Pulse frequency in Hz on RearVentStrip _EmissionStrength while Worried.")]
        [Range(0.2f, 6.0f)]
        [SerializeField] private float _rearVentWorriedPulseHz = 1.6f;

        public int ChargingPriority => _chargingPriority;
        public int LowPowerPriority => _lowPowerPriority;
        public int StartledPriority => _startledPriority;
        public int ProudPriority => _proudPriority;
        public int HappyPriority => _happyPriority;
        public int ScanningPriority => _scanningPriority;
        public int LowBatteryPriority => _lowBatteryPriority;
        public int EarthGlancePriority => _earthGlancePriority;
        public int CalmPriority => _calmPriority;

        public float MinFeelingHoldSeconds => _minFeelingHoldSeconds;
        public float ScanMinHoldSeconds => _scanMinHoldSeconds;
        public float FragmentHappyHoldSeconds => _fragmentHappyHoldSeconds;
        public float BlueprintHappyHoldSeconds => _blueprintHappyHoldSeconds;
        public float ProudHoldSeconds => _proudHoldSeconds;
        public float StartledHoldSeconds => _startledHoldSeconds;
        public float EarthGlanceHoldSeconds => _earthGlanceHoldSeconds;
        public float EarthGlanceCooldownSeconds => _earthGlanceCooldownSeconds;

        public float LowBatteryThreshold => _lowBatteryThreshold;
        public float LowBatteryClearThreshold => _lowBatteryClearThreshold;
        public float HardLandingImpactThreshold => _hardLandingImpactThreshold;
        public float FallVerticalSpeedThreshold => _fallVerticalSpeedThreshold;
        public float FallMinAirborneSeconds => _fallMinAirborneSeconds;
        public float EarthViewMinDot => _earthViewMinDot;
        public float IdleSpeedThreshold => _idleSpeedThreshold;

        public float LowPowerRampSeconds => _lowPowerRampSeconds;
        public float LowPowerRecoverSeconds => _lowPowerRecoverSeconds;
        public float NightIdleDelaySeconds => _nightIdleDelaySeconds;
        public float NightSlowLookAmplitudeX => _nightSlowLookAmplitudeX;
        public float NightSlowLookAmplitudeY => _nightSlowLookAmplitudeY;
        public float NightSlowLookPeriodSeconds => _nightSlowLookPeriodSeconds;

        public float EyeLeadFactor => _eyeLeadFactor;
        public float LookDirectionFullScaleDegrees => _lookDirectionFullScaleDegrees;

        public float RearVentBaseEmission => _rearVentBaseEmission;
        public float RearVentMinEmission => _rearVentMinEmission;
        public float RearVentHappyBoost => _rearVentHappyBoost;
        public float RearVentWorriedPulseAmplitude => _rearVentWorriedPulseAmplitude;
        public float RearVentWorriedPulseHz => _rearVentWorriedPulseHz;
    }
}
