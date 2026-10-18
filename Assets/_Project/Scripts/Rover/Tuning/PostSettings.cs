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

        [Tooltip("Seconds after the checks start over which the fill grows in.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _fillRampSeconds = 2.5f;

        [Header("The camera")]
        [Tooltip("How far (m) the three-quarter front shot sits from the point it looks at.")]
        [Range(1f, 6f)]
        [SerializeField] private float _shotDistance = 2.6f;

        [Tooltip("How far (deg) round from 07's front the camera stands: 35 is a three-quarter view.")]
        [Range(0f, 90f)]
        [SerializeField] private float _shotSideYaw = 35f;

        [Tooltip("The shot's field of view (deg).")]
        [Range(20f, 70f)]
        [SerializeField] private float _shotFov = 42f;

        [Tooltip("After the checks the camera settles this far (m) to 07's right, high and behind it.")]
        [SerializeField] private float _overShoulderSide = 0.9f;

        [SerializeField] private float _overShoulderHeight = 1.6f;

        [SerializeField] private float _overShoulderBack = 2.6f;

        [Tooltip("The whole-body focus the pull-out arrives at and the lens check starts from.")]
        [SerializeField] private PostFocus _baseFocus = new PostFocus(BootCheckStep.Lens, new Vector3(0f, 0.8f, 0.3f), 1f, 1.35f);

        [Tooltip("What the camera eases toward for each check: a point in 07's frame (x right, y up, z forward), how " +
                 "close (a share of the shot distance) and how high (m).")]
        [SerializeField] private PostFocus[] _focus =
        {
            new PostFocus(BootCheckStep.Lens, new Vector3(0f, 0.8f, 0.3f), 1f, 1.35f),
            new PostFocus(BootCheckStep.Lamps, new Vector3(0f, 0.95f, 0.55f), 0.75f, 1.2f),
            new PostFocus(BootCheckStep.Head, new Vector3(0f, 1.15f, 0.25f), 0.8f, 1.35f),
            new PostFocus(BootCheckStep.Wheels, new Vector3(0f, 0.35f, 0.1f), 1.1f, 0.55f, 55f),
            new PostFocus(BootCheckStep.Motor, new Vector3(0.2f, 0.75f, 0f), 1.3f, 0.9f, -50f),
            new PostFocus(BootCheckStep.Cutter, new Vector3(0f, 0.8f, 0.6f), 0.62f, 0.65f, -15f),
            new PostFocus(BootCheckStep.SolarWing, new Vector3(0f, 0.9f, -0.3f), 1.2f, 1.9f, 145f),
            new PostFocus(BootCheckStep.Antenna, new Vector3(0f, 1.2f, -0.25f), 1.05f, 1.5f, 110f),
            new PostFocus(BootCheckStep.Battery, new Vector3(0.55f, 1.05f, 0f), 0.8f, 1.1f),
            new PostFocus(BootCheckStep.Assembly, new Vector3(0f, 0.8f, 0.3f), 1f, 1.35f),
            new PostFocus(BootCheckStep.Begin, new Vector3(0f, 1f, 0.3f), 0.9f, 1.35f),
        };

        public float ShotDistance => _shotDistance;

        public float ShotSideYaw => _shotSideYaw;

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

        public float OverShoulderSide => _overShoulderSide;

        public float OverShoulderHeight => _overShoulderHeight;

        public float OverShoulderBack => _overShoulderBack;

        public PostFocus BaseFocus => _baseFocus;

        /// <summary>What the camera frames for <paramref name="step"/> (the base focus for a step with none).</summary>
        public PostFocus FocusOf(BootCheckStep step)
        {
            for (int i = 0; i < _focus.Length; i++)
            {
                if (_focus[i].Step == step)
                {
                    return _focus[i];
                }
            }

            return _baseFocus;
        }

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

    /// <summary>Where the self-test's camera looks during one check, and how near and high it stands.</summary>
    [Serializable]
    public sealed class PostFocus
    {
        [SerializeField] private BootCheckStep _step;
        [SerializeField] private Vector3 _local;
        [SerializeField] private float _distance = 1f;
        [SerializeField] private float _height = 1.35f;
        [SerializeField] private float _sideYaw;

        public PostFocus(BootCheckStep step, Vector3 local, float distance, float height, float sideYaw = 0f)
        {
            _sideYaw = sideYaw;
            _step = step;
            _local = local;
            _distance = distance;
            _height = height;
        }

        public BootCheckStep Step => _step;

        /// <summary>The point looked at, in 07's frame (x right, y up, z forward).</summary>
        public Vector3 Local => _local;

        /// <summary>How near the camera stands, as a share of the shot distance.</summary>
        public float Distance => _distance;

        /// <summary>How high above 07's ground point (m).</summary>
        public float Height => _height;

        /// <summary>Degrees the camera swings round 07 from the three-quarter front, to see a part on its side or back.</summary>
        public float SideYaw => _sideYaw;
    }
}
