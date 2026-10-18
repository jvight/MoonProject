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
        [SerializeField] private float _lensSeconds = 1.2f;

        [Tooltip("The lamps flicker unevenly, fall dark, then ramp up: at least the lamp power-up's own length.")]
        [Range(1f, 5f)]
        [SerializeField] private float _lampsSeconds = 2.4f;

        [Tooltip("The neck and head sweep and nod.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _headSeconds = 2f;

        [Tooltip("The four wheels twitch in turn.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _wheelsSeconds = 2f;

        [Tooltip("The motor shudders and 07 creeps forward a little.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _motorSeconds = 1.8f;

        [Tooltip("The cutter's turret tries to ignite two or three times and dies.")]
        [Range(1f, 5f)]
        [SerializeField] private float _cutterSeconds = 2.6f;

        [Tooltip("The solar wing jerks half open, stalls and folds back.")]
        [Range(1f, 5f)]
        [SerializeField] private float _solarWingSeconds = 2.2f;

        [Tooltip("The antenna rises and the orbit's signal hisses.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _antennaSeconds = 2f;

        [Tooltip("The cell lamps fill to half.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _batterySeconds = 1.4f;

        [Tooltip("The assembly line: no blueprints.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _assemblySeconds = 0.8f;

        [Tooltip("07 turns its head to the lander's beacon, shivers its dust off and the recovery begins.")]
        [Range(2f, 8f)]
        [SerializeField] private float _beginSeconds = 4.5f;

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
