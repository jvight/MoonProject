using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's power-on self-test, seen in one calm shot of its face: how long the black, the picture's fade-in, the face's
    /// waking and each line take (seconds), the motions on 07 for each check, the long-sleep dust, the light, the one
    /// camera pose and the soft dip that hands over to the chase frame. The whole wake is meant to land at 30-35 s.
    /// </summary>
    [Serializable]
    public sealed class PostSettings
    {
        [Header("The wake on the face (s)")]
        [Tooltip("Black while 07's systems thump on, before the picture opens.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _darkSeconds = 1f;

        [Tooltip("The picture opens from black on 07, still asleep with its face dark.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _fadeInSeconds = 1.6f;

        [Tooltip("From the face screen flickering on to the eyes' first blink: the CRT boots and the eyes open.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _eyesBootSeconds = 1.6f;

        [Tooltip("From the eyes' blink to the first line on the face.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _eyesHoldSeconds = 1f;

        [Tooltip("The soft dip to black that hands over to the chase frame: the frame changes at its darkest middle.")]
        [Range(0.2f, 1.2f)]
        [SerializeField] private float _handoverSeconds = 0.4f;

        [Header("The lines of information (s each)")]
        [Tooltip("Why 07 woke: the orbital signal.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _carrierSeconds = 1.4f;

        [Tooltip("07 resumes where it stopped, so many days later.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _resumeSeconds = 1.4f;

        [Tooltip("The restored agenda: what 07 has to do.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _agendaSeconds = 1.6f;

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

        [Tooltip("07 glances at the lander's beacon, shivers its dust off and the recovery begins.")]
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

        public float DarkSeconds => _darkSeconds;

        public float FadeInSeconds => _fadeInSeconds;

        public float EyesBootSeconds => _eyesBootSeconds;

        public float EyesHoldSeconds => _eyesHoldSeconds;

        public float HandoverSeconds => _handoverSeconds;

        [Header("The long-sleep dust")]
        [Tooltip("The lunar-dust amount of a part under the full coat (the material's own amount is the clean state).")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _dustCoatAmount = 4f;

        [Tooltip("How much coat (0..1 of a part) has to fall before a puff of dust drops off it.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _dustPuffStep = 0.15f;

        [Tooltip("Grains in each puff that drops where coat falls off a part.")]
        [Range(1, 30)]
        [SerializeField] private int _dustPuffGrains = 6;

        [Tooltip("When each part sheds its coat: during which check, over which share of it, and how much is left after.")]
        [SerializeField] private PostDustSegment[] _dustSegments =
        {
            new PostDustSegment(DustZone.Head, BootCheckStep.Head, 0f, 0.8f, 0.4f),
            new PostDustSegment(DustZone.Wheels, BootCheckStep.Wheels, 0.05f, 0.95f, 0.35f),
            new PostDustSegment(DustZone.Body, BootCheckStep.Motor, 0.2f, 0.9f, 0.5f),
            new PostDustSegment(DustZone.Turret, BootCheckStep.Cutter, 0.1f, 0.9f, 0.3f),
            new PostDustSegment(DustZone.Wing, BootCheckStep.SolarWing, 0.1f, 0.9f, 0.2f),
            new PostDustSegment(DustZone.Antenna, BootCheckStep.Antenna, 0f, 0.7f, 0.3f),
        };

        [Header("The light")]
        [Tooltip("The cool earthshine-like rim that lets the body read in the eclipse.")]
        [SerializeField] private Color _rimColor = new Color(0.55f, 0.68f, 1f);

        [Tooltip("Rim intensity at full.")]
        [Min(0f)]
        [SerializeField] private float _rimIntensity = 6f;

        [Range(2f, 20f)]
        [SerializeField] private float _rimRange = 9f;

        [Range(20f, 120f)]
        [SerializeField] private float _rimSpotAngle = 70f;

        [Tooltip("How far (m) toward Earth the rim stands from 07, and how high (m).")]
        [Range(1f, 8f)]
        [SerializeField] private float _rimDistance = 4f;

        [Range(0f, 6f)]
        [SerializeField] private float _rimHeight = 2.2f;

        [Tooltip("The height (m) above 07's ground point the rim aims at.")]
        [Range(0f, 2f)]
        [SerializeField] private float _rimAimHeight = 0.8f;

        [Tooltip("The warm fill that comes off 07's own screen.")]
        [SerializeField] private Color _fillColor = new Color(1f, 0.78f, 0.5f);

        [Min(0f)]
        [SerializeField] private float _fillIntensity = 1.4f;

        [Range(0.5f, 6f)]
        [SerializeField] private float _fillRange = 3f;

        [Tooltip("How far (m) in front of the screen the fill sits.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _fillOffset = 0.5f;

        [Tooltip("Seconds after the face screen flickers on over which the fill grows in.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _fillRampSeconds = 2.5f;

        [Header("The camera: one shot for the whole wake")]
        [Tooltip("How far (m) the camera stands from 07's face before its push-in: all of 07 in frame as it wakes.")]
        [Range(2f, 9f)]
        [SerializeField] private float _shotDistance = 5f;

        [Tooltip("How far (deg) round from 07's front the camera stands: 35 is a three-quarter view.")]
        [Range(0f, 60f)]
        [SerializeField] private float _shotSideYaw = 35f;

        [Tooltip("How far (deg) above the face the camera stands; below 0 it sits a little low and looks softly up.")]
        [Range(-15f, 10f)]
        [SerializeField] private float _shotElevation = -6f;

        [Tooltip("The shot's field of view (deg).")]
        [Range(20f, 60f)]
        [SerializeField] private float _shotFov = 32f;

        [Tooltip("Share of the frame's height 07's face screen fills once the push-in has arrived: the lines read.")]
        [Range(0.2f, 0.5f)]
        [SerializeField] private float _faceFill = 0.375f;

        [Tooltip("Seconds the push-in takes, eased, from the first line on the face: very slow.")]
        [Range(3f, 20f)]
        [SerializeField] private float _pushInSeconds = 9f;

        [Header("The start's glance")]
        [Tooltip("How far (deg) at most the head turns from the camera toward the lander's beacon at the start.")]
        [Range(5f, 45f)]
        [SerializeField] private float _beaconGlanceYaw = 30f;

        [Tooltip("How far (deg) at most the head lifts or lowers toward the beacon.")]
        [Range(0f, 15f)]
        [SerializeField] private float _beaconGlancePitch = 8f;

        public float ShotDistance => _shotDistance;

        public float ShotSideYaw => _shotSideYaw;

        public float DustCoatAmount => _dustCoatAmount;

        public float DustPuffStep => _dustPuffStep;

        public int DustPuffGrains => _dustPuffGrains;

        /// <summary>When <paramref name="zone"/> sheds its coat.</summary>
        public PostDustSegment DustSegmentOf(DustZone zone)
        {
            for (int i = 0; i < _dustSegments.Length; i++)
            {
                if (_dustSegments[i].Zone == zone)
                {
                    return _dustSegments[i];
                }
            }

            throw new InvalidOperationException($"PostSettings has no dust segment for {zone}.");
        }

        public Color RimColor => _rimColor;

        public float RimIntensity => _rimIntensity;

        public float RimRange => _rimRange;

        public float RimSpotAngle => _rimSpotAngle;

        public float RimDistance => _rimDistance;

        public float RimHeight => _rimHeight;

        public float RimAimHeight => _rimAimHeight;

        public Color FillColor => _fillColor;

        public float FillIntensity => _fillIntensity;

        public float FillRange => _fillRange;

        public float FillOffset => _fillOffset;

        public float FillRampSeconds => _fillRampSeconds;

        public float ShotFov => _shotFov;

        public float ShotElevation => _shotElevation;

        public float FaceFill => _faceFill;

        public float PushInSeconds => _pushInSeconds;

        public float BeaconGlanceYaw => _beaconGlanceYaw;

        public float BeaconGlancePitch => _beaconGlancePitch;

        /// <summary>Seconds the line <paramref name="step"/> takes on the face.</summary>
        public float SecondsOf(BootCheckStep step)
        {
            switch (step)
            {
                case BootCheckStep.Carrier: return _carrierSeconds;
                case BootCheckStep.Resume: return _resumeSeconds;
                case BootCheckStep.Agenda: return _agendaSeconds;
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

    /// <summary>When one part of 07 sheds its long-sleep dust.</summary>
    [Serializable]
    public sealed class PostDustSegment
    {
        [SerializeField] private DustZone _zone;
        [SerializeField] private BootCheckStep _step;
        [Range(0f, 1f)]
        [SerializeField] private float _start;
        [Range(0f, 1f)]
        [SerializeField] private float _end = 0.9f;
        [Tooltip("Share of the coat left on the part after its check; the start's shiver takes the rest.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rest = 0.3f;

        public PostDustSegment(DustZone zone, BootCheckStep step, float start, float end, float rest)
        {
            _zone = zone;
            _step = step;
            _start = start;
            _end = end;
            _rest = rest;
        }

        public DustZone Zone => _zone;

        public BootCheckStep Step => _step;

        public float Start => _start;

        public float End => _end;

        public float Rest => _rest;
    }
}
