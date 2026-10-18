using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// How long each check of 07's power-on self-test takes (seconds): the motion on 07 for that line. The whole test
    /// from the pull-out to the release is meant to land at 25-30 s with the lens stages before it.
    /// </summary>
    [Serializable]
    public sealed class PostSettings
    {
        [Header("The checks (s each)")]
        [Tooltip("The iris focuses.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _lensSeconds = 1f;

        [Tooltip("The lamps flicker unevenly, fall dark, then ramp up: at least the lamp power-up's own length.")]
        [Range(1f, 5f)]
        [SerializeField] private float _lampsSeconds = 2.4f;

        [Tooltip("The neck and head sweep and nod.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _headSeconds = 1.8f;

        [Tooltip("The four wheels twitch in turn.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _wheelsSeconds = 1.8f;

        [Tooltip("The motor shudders and 07 creeps forward a little.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _motorSeconds = 1.6f;

        [Tooltip("The cutter's turret tries to ignite two or three times and dies.")]
        [Range(1f, 5f)]
        [SerializeField] private float _cutterSeconds = 2.4f;

        [Tooltip("The solar wing jerks half open, stalls and folds back.")]
        [Range(1f, 5f)]
        [SerializeField] private float _solarWingSeconds = 2f;

        [Tooltip("The antenna rises and the orbit's signal hisses.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _antennaSeconds = 1.8f;

        [Tooltip("The cell lamps fill to half.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _batterySeconds = 1.2f;

        [Tooltip("The assembly line: no blueprints.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _assemblySeconds = 0.7f;

        [Tooltip("07 turns its head to the lander's beacon, shivers its dust off and the recovery begins.")]
        [Range(2f, 8f)]
        [SerializeField] private float _beginSeconds = 4f;

        [Header("The motions")]
        [Tooltip("How far (deg) the neck sweeps to each side in the head check.")]
        [Range(5f, 45f)]
        [SerializeField] private float _headSweepDegrees = 24f;

        [Tooltip("How far (deg) the head nods down.")]
        [Range(2f, 25f)]
        [SerializeField] private float _headNodDegrees = 9f;

        [Tooltip("How far (deg) each wheel twitches forward and back.")]
        [Range(2f, 40f)]
        [SerializeField] private float _wheelTwitchDegrees = 14f;

        [Tooltip("The motor's shudder: the chassis' vertical shake (m).")]
        [Range(0f, 0.04f)]
        [SerializeField] private float _shudderMetres = 0.012f;

        [Tooltip("The motor's creep: how far (m) the chassis lurches forward.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _lurchMetres = 0.06f;

        [Tooltip("How far (deg) the cutter's turret kicks sideways on each ignition attempt.")]
        [Range(0f, 25f)]
        [SerializeField] private float _turretKickDegrees = 10f;

        [Tooltip("How far (0..1) the solar wing jerks open before it stalls.")]
        [Range(0.2f, 0.9f)]
        [SerializeField] private float _wingJerkOpen = 0.5f;

        [Tooltip("How high (m) the antenna rises in its check.")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _antennaRiseMetres = 0.12f;

        [Tooltip("The cell lamps fill to this share of full (the battery's 50%).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _batteryFill = 0.5f;

        public float HeadSweepDegrees => _headSweepDegrees;

        public float HeadNodDegrees => _headNodDegrees;

        public float WheelTwitchDegrees => _wheelTwitchDegrees;

        public float ShudderMetres => _shudderMetres;

        public float LurchMetres => _lurchMetres;

        public float TurretKickDegrees => _turretKickDegrees;

        public float WingJerkOpen => _wingJerkOpen;

        public float AntennaRiseMetres => _antennaRiseMetres;

        public float BatteryFill => _batteryFill;

        /// <summary>Seconds <paramref name="step"/> takes; the lens stages' steps (carrier, resume, agenda) have none here.</summary>
        public float SecondsOf(BootCheckStep step)
        {
            switch (step)
            {
                case BootCheckStep.Lens: return _lensSeconds;
                case BootCheckStep.Lamps: return _lampsSeconds;
                case BootCheckStep.Head: return _headSeconds;
                case BootCheckStep.Wheels: return _wheelsSeconds;
                case BootCheckStep.Motor: return _motorSeconds;
                case BootCheckStep.Cutter: return _cutterSeconds;
                case BootCheckStep.SolarWing: return _solarWingSeconds;
                case BootCheckStep.Antenna: return _antennaSeconds;
                case BootCheckStep.Battery: return _batterySeconds;
                case BootCheckStep.Assembly: return _assemblySeconds;
                case BootCheckStep.Begin: return _beginSeconds;
                default: return 0f;
            }
        }
    }
}
