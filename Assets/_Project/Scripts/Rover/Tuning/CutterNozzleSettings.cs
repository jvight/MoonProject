using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Tuning for 07's chest-mounted cutter nozzle aperture on <c>CutterOrigin</c>: the 6-blade mechanical iris,
    /// the glowing <c>R1_LampAmber</c> core disc, and the warm spill point light during charge, sustain, and release.
    /// </summary>
    [Serializable]
    public sealed class CutterNozzleSettings
    {
        [Tooltip("Radial offset (m) of each iris blade from the nozzle centre when closed.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _bladeClosedRadius = 0.011f;

        [Tooltip("Radial offset (m) of each iris blade from the nozzle centre when fully open.")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float _bladeOpenRadius = 0.036f;

        [Tooltip("Local +Z offset (m) of the iris blades in front of the nozzle lens apex.")]
        [Range(-0.02f, 0.05f)]
        [SerializeField] private float _bladeForwardOffset = 0.003f;

        [Tooltip("Base twist angle (deg) of each iris blade in the closed aperture.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _bladeAngleOffset = 18f;

        [Tooltip("Additional twist angle (deg) applied to each iris blade as the aperture opens.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _bladeOpenTwist = -28f;

        [Tooltip("Diameter (m) of the nozzle hot core disc when idle.")]
        [Range(0.002f, 0.05f)]
        [SerializeField] private float _coreRestDiameter = 0.012f;

        [Tooltip("Diameter (m) of the nozzle hot core disc at full cut glow.")]
        [Range(0.01f, 0.12f)]
        [SerializeField] private float _coreHotDiameter = 0.046f;

        [Tooltip("Local +Z offset (m) of the hot core disc in front of the nozzle lens apex.")]
        [Range(-0.02f, 0.05f)]
        [SerializeField] private float _coreForwardOffset = 0.001f;

        [Tooltip("Peak _EmissionStrength driven onto the R1_LampAmber hot core disc when firing.")]
        [Range(0f, 8f)]
        [SerializeField] private float _corePeakEmission = 2.5f;

        [Tooltip("Immediate iris opening floor (0..1) when a cut charge begins.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeInitialIrisOpen = 0.2f;

        [Tooltip("Iris opening fraction (0..1) reached at the end of the charge-up.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeFullIrisOpen = 0.88f;

        [Tooltip("Immediate core glow floor (0..1) when a cut charge begins.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeInitialCoreGlow = 0.35f;

        [Tooltip("Core glow fraction (0..1) reached at the end of the charge-up.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeFullCoreGlow = 0.85f;

        [Tooltip("Fallback charge duration (s) when a stage event does not specify one.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _defaultChargeDuration = 0.3f;

        [Tooltip("Minimum clamped charge duration (s) used to normalize charge progress.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _minChargeDuration = 0.05f;

        [Tooltip("Smoothing half-life (s) for snapping the iris and core open when firing.")]
        [Range(0.005f, 0.5f)]
        [SerializeField] private float _fireOpenHalfLife = 0.035f;

        [Tooltip("Smoothing half-life (s) for closing the iris and cooling the core after release.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _releaseHalfLife = 0.14f;

        [Tooltip("Fractional diameter pulse amplitude of the hot core while the beam is firing.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _fireCorePulseAmplitude = 0.08f;

        [Tooltip("Oscillation frequency (Hz) of the hot core diameter pulse while firing.")]
        [Range(0f, 60f)]
        [SerializeField] private float _fireCorePulseRate = 23f;

        [Tooltip("Spill point-light intensity at the start of the cutter charge.")]
        [Range(0f, 5f)]
        [SerializeField] private float _chargeMinSpillIntensity = 0.35f;

        [Tooltip("Spill point-light intensity at full cutter charge.")]
        [Range(0f, 10f)]
        [SerializeField] private float _chargeMaxSpillIntensity = 1.6f;

        [Tooltip("Base spill point-light intensity while the cutter beam is firing.")]
        [Range(0f, 10f)]
        [SerializeField] private float _fireBaseSpillIntensity = 3.2f;

        [Tooltip("Flicker amplitude added to the spill point-light intensity while firing.")]
        [Range(0f, 5f)]
        [SerializeField] private float _fireFlickerAmplitude = 0.55f;

        [Tooltip("Flicker frequency (Hz) of the spill point light while firing.")]
        [Range(0f, 60f)]
        [SerializeField] private float _fireFlickerRate = 19f;

        [Tooltip("Spill point-light intensity scale while the nozzle cools down after release.")]
        [Range(0f, 5f)]
        [SerializeField] private float _releaseSpillIntensity = 0.96f;

        [Header("Pilot light (cutter online)")]
        [Tooltip("Core glow (0..1) of the small steady pilot light while the cutter works and is idle; the offline " +
                 "nozzle is dark.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pilotCoreGlow = 0.14f;

        [Tooltip("Spill point-light intensity of the pilot light on the chest chassis.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pilotSpillIntensity = 0.12f;

        [Header("Spill light")]
        [Tooltip("Range (m) of the warm nozzle spill point light.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _spillRange = 0.55f;

        [Tooltip("Local +Z offset (m) of the spill point light in front of the nozzle aperture.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _spillForwardOffset = 0.03f;

        public float BladeClosedRadius => _bladeClosedRadius;

        public float BladeOpenRadius => _bladeOpenRadius;

        public float BladeForwardOffset => _bladeForwardOffset;

        public float BladeAngleOffset => _bladeAngleOffset;

        public float BladeOpenTwist => _bladeOpenTwist;

        public float CoreRestDiameter => _coreRestDiameter;

        public float CoreHotDiameter => _coreHotDiameter;

        public float CoreForwardOffset => _coreForwardOffset;

        public float CorePeakEmission => _corePeakEmission;

        public float ChargeInitialIrisOpen => _chargeInitialIrisOpen;

        public float ChargeFullIrisOpen => _chargeFullIrisOpen;

        public float ChargeInitialCoreGlow => _chargeInitialCoreGlow;

        public float ChargeFullCoreGlow => _chargeFullCoreGlow;

        public float DefaultChargeDuration => _defaultChargeDuration;

        public float MinChargeDuration => _minChargeDuration;

        public float FireOpenHalfLife => _fireOpenHalfLife;

        public float ReleaseHalfLife => _releaseHalfLife;

        public float FireCorePulseAmplitude => _fireCorePulseAmplitude;

        public float FireCorePulseRate => _fireCorePulseRate;

        public float ChargeMinSpillIntensity => _chargeMinSpillIntensity;

        public float ChargeMaxSpillIntensity => _chargeMaxSpillIntensity;

        public float FireBaseSpillIntensity => _fireBaseSpillIntensity;

        public float FireFlickerAmplitude => _fireFlickerAmplitude;

        public float FireFlickerRate => _fireFlickerRate;

        public float ReleaseSpillIntensity => _releaseSpillIntensity;

        public float PilotCoreGlow => _pilotCoreGlow;

        public float PilotSpillIntensity => _pilotSpillIntensity;

        public float SpillRange => _spillRange;

        public float SpillForwardOffset => _spillForwardOffset;
    }
}
