using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The floaty drone camera behind 07: orbit input, idle recentering, downhill lift, speed FOV, landing bump, camera
    /// moments, the lonely wide shot when 07 rests, and the Cinemachine damping it hands to the orbital follow and
    /// composer.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverCameraTuning", menuName = "MoonProject/Rover/Rover Camera Tuning")]
    public sealed class RoverCameraTuning : ScriptableObject
    {
        [Header("Look input")]
        [Tooltip("Orbit degrees per pixel of mouse movement.")]
        [Range(0.005f, 1f)]
        [SerializeField] private float _mouseSensitivity = 0.08f;

        [Tooltip("Orbit speed (deg/s) at full gamepad stick.")]
        [Range(10f, 400f)]
        [SerializeField] private float _stickRate = 110f;

        [Tooltip("Invert vertical look by default (the player's setting replaces it at runtime).")]
        [SerializeField] private bool _invertY;

        [Tooltip("Lowest look sensitivity multiplier the settings menu can choose.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _minSensitivity = 0.25f;

        [Tooltip("Highest look sensitivity multiplier the settings menu can choose.")]
        [Range(1f, 10f)]
        [SerializeField] private float _maxSensitivity = 3f;

        [Tooltip("Half-life (s) smoothing the look speed, so the orbit glides instead of stepping.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _lookHalfLife = 0.05f;

        [Header("Orbit")]
        [Tooltip("Distance (m) from the follow point to the camera.")]
        [Range(2f, 30f)]
        [SerializeField] private float _distance = 7.5f;

        [Tooltip("Height (m) of the follow point above the rover's ground contact.")]
        [Range(0f, 4f)]
        [SerializeField] private float _targetHeight = 1.1f;

        [Tooltip("Resting orbit elevation (deg above the horizon).")]
        [Range(-10f, 80f)]
        [SerializeField] private float _defaultPitch = 16f;

        [Tooltip("Lowest orbit elevation (deg).")]
        [Range(-20f, 60f)]
        [SerializeField] private float _minPitch = 3f;

        [Tooltip("Orbit elevation (deg) of the opening shot: low, so the camera looks almost level across the basin "
            + "at Earth and The Peak (design ruling 8: at most ~6 deg down). It eases to the resting elevation once 07 "
            + "starts driving.")]
        [Range(-10f, 80f)]
        [SerializeField] private float _openingPitch = 6f;

        [Tooltip("Highest orbit elevation (deg).")]
        [Range(10f, 89f)]
        [SerializeField] private float _maxPitch = 60f;

        [Header("Auto-recenter")]
        [Tooltip("Seconds without look input before the camera eases back behind 07.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _recenterDelay = 3f;

        [Tooltip("Seconds over which recentering fades in once it starts (no sudden pull).")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _recenterRampTime = 1.5f;

        [Tooltip("Half-life (s) of the recentering itself, once fully faded in.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _recenterHalfLife = 0.9f;

        [Tooltip("Recenter only while moving faster than this (m/s): a parked 07 lets you admire the view.")]
        [Range(0f, 5f)]
        [SerializeField] private float _recenterMinSpeed = 0.5f;

        [Header("Downhill lift")]
        [Tooltip("Extra elevation (deg) when driving downhill, to see the slope ahead.")]
        [Range(0f, 40f)]
        [SerializeField] private float _downhillLift = 10f;

        [Tooltip("Descent angle (deg) that earns the full lift.")]
        [Range(1f, 60f)]
        [SerializeField] private float _downhillFullAngle = 20f;

        [Tooltip("Half-life (s) of the lift easing in and out.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _liftHalfLife = 0.7f;

        [Header("Field of view")]
        [Tooltip("Vertical field of view (deg) at rest.")]
        [Range(20f, 100f)]
        [SerializeField] private float _baseFov = 52f;

        [Tooltip("Extra field of view (deg) at top speed.")]
        [Range(0f, 30f)]
        [SerializeField] private float _speedFovBoost = 6f;

        [Tooltip("Half-life (s) of the FOV change.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _fovHalfLife = 0.6f;

        [Tooltip("Near clip plane (m). As large as comfortable for depth precision across the huge far range.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _nearClip = 0.3f;

        [Tooltip("Far clip plane (m). The rim backdrop and fog reach ~2.8 km and Earth sits at the far plane, "
            + "so keep it >= 3000.")]
        [Range(3000f, 10000f)]
        [SerializeField] private float _farClip = 3200f;

        [Header("Landing bump")]
        [Tooltip("Camera dip speed (m/s) per m/s of landing impact.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bumpPerImpact = 0.18f;

        [Tooltip("Largest dip speed (m/s), however hard the landing.")]
        [Range(0f, 5f)]
        [SerializeField] private float _maxBumpKick = 0.9f;

        [Tooltip("Spring frequency (Hz) of the camera bump.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _bumpFrequency = 1.4f;

        [Tooltip("Damping ratio of the camera bump (just under 1: one barely visible rebound).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _bumpDamping = 0.8f;

        [Header("Camera moments")]
        [Tooltip("Digging: while the excavation beam is on, ease to the side and up so the beam and the rising "
            + "relic are seen beside 07, not hidden behind it. Flows into the relic moment when it surfaces.")]
        [SerializeField] private CameraMomentSettings _digMoment =
            new CameraMomentSettings(1.2f, 0f, 1f, 0.5f, 3f, 0.6f, 35f, 70f, 14f, 0.25f, 15f);

        [Tooltip("A relic finishing surfacing: ease round to frame 07 and the floating relic, then back (~3 s).")]
        [SerializeField] private CameraMomentSettings _relicMoment =
            new CameraMomentSettings(0.9f, 1f, 1.1f, 0.35f, 2.5f, 0.85f, 25f, 60f, 8f, 0.35f, 25f);

        [Tooltip("A Hover-Jump leap: lift and pull back, looking ahead to where 07 will land; held until it lands.")]
        [SerializeField] private CameraMomentSettings _leapMoment =
            new CameraMomentSettings(0.8f, 0.3f, 1.4f, 0.35f, 6f, 0f, 0f, 0f, 10f, 0.35f, 60f);

        [Tooltip("Leaps weaker than this (0..1) are hops: the camera just follows.")]
        [Range(0f, 1f)]
        [SerializeField] private float _leapMomentMinStrength = 0.3f;

        [Tooltip("Seconds of travel ahead the leap camera looks, toward the landing.")]
        [Range(0f, 6f)]
        [SerializeField] private float _leapLookAhead = 2f;

        [Tooltip("An upgrade bought at the base: lift and pull back to take in the base, its tower and warm ring.")]
        [SerializeField] private CameraMomentSettings _upgradeMoment =
            new CameraMomentSettings(1.3f, 1.8f, 1.5f, 0.5f, 6f, 0.6f, 0f, 45f, 14f, 0.5f, 40f);

        [Tooltip("A relay mast 07 restored lights up: the camera eases low, back and a little to the side, looking up "
            + "past 07 at the mast and its warming lamp against the sky, then returns (~8 s).")]
        [SerializeField] private CameraMomentSettings _relayMoment =
            new CameraMomentSettings(3f, 2.6f, 2.6f, 0.22f, 3f, 0.5f, 26f, 40f, -13f, 0.65f, 40f);

        [Tooltip("A kit piece fitted at Kenji's bench: the camera eases round, low and closer, to a view of the new "
            + "piece (Kit views), holds while it settles and 07 strikes its proud pose, then returns (~3.6 s).")]
        [SerializeField] private CameraMomentSettings _installMoment =
            new CameraMomentSettings(1.2f, 1.4f, 1.0f, 0f, 0f, 1f, 0f, 170f, -7f, -0.3f, 40f);

        [Tooltip("A friend's gift appearing as 07 comes home: a softer, shorter swing toward the gift, then back.")]
        [SerializeField] private CameraMomentSettings _giftMoment =
            new CameraMomentSettings(1.4f, 1.0f, 1.2f, 0f, 0f, 0.5f, 0f, 80f, -3f, -0.15f, 40f);

        [Tooltip("Which way the install moment looks for each kit piece and gift.")]
        [SerializeField] private KitViewSettings _kitViews = new KitViewSettings();

        [Tooltip("Seconds a camera moment takes to ease away when the player looks around (always skippable).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _momentCancelEaseOut = 0.6f;

        [Tooltip("Half-life (s) smoothing a moment's framing, so one moment flowing into the next never jumps.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _momentBlendHalfLife = 0.3f;

        [Header("Wide shot")]
        [Tooltip("Resting a while: the camera drifts out to a wide, lonely frame with 07 small against the land and "
            + "the sky (VISION pillar 6), and hands back on any drive or look input.")]
        [SerializeField] private WideShotSettings _wideShot = new WideShotSettings();

        [Header("Cinemachine damping")]
        [Tooltip("Orbital follow position damping (x, y, z): high = floaty drone.")]
        [SerializeField] private Vector3 _positionDamping = new Vector3(1.1f, 0.9f, 1.3f);

        [Tooltip("Orbital follow yaw damping: how lazily the camera swings behind 07 when it turns.")]
        [Range(0f, 20f)]
        [SerializeField] private float _yawDamping = 1.8f;

        [Tooltip("Rotation composer damping (horizontal, vertical).")]
        [SerializeField] private Vector2 _aimDamping = new Vector2(0.6f, 0.5f);

        [Tooltip("Where 07 sits on screen (0 = centre, +y = lower), leaving room for the sky and Earth.")]
        [SerializeField] private Vector2 _screenPosition = new Vector2(0f, 0.1f);

        [Header("Terrain avoidance")]
        [Tooltip("Clearance (m) the camera keeps from terrain and rocks.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _cameraRadius = 0.45f;

        [Tooltip("Seconds the camera takes to ease back out after terrain pushed it in.")]
        [Range(0f, 5f)]
        [SerializeField] private float _avoidanceDamping = 0.8f;

        [Tooltip("Seconds the camera takes to move in when terrain blocks the view (small: never inside a hill).")]
        [Range(0f, 2f)]
        [SerializeField] private float _occlusionDamping = 0.15f;

        [Tooltip("Seconds the closest safe camera distance is held, so passing rocks do not make it pump.")]
        [Range(0f, 2f)]
        [SerializeField] private float _occlusionSmoothing = 0.3f;

        [Tooltip("Length (m) of the ray that finds the terrain surface under the camera.")]
        [Range(1f, 50f)]
        [SerializeField] private float _terrainProbe = 12f;

        public float MouseSensitivity => _mouseSensitivity;

        public float StickRate => _stickRate;

        public bool InvertY => _invertY;

        public float MinSensitivity => _minSensitivity;

        public float MaxSensitivity => Mathf.Max(_minSensitivity, _maxSensitivity);

        public float LookHalfLife => _lookHalfLife;

        public float Distance => _distance;

        public float TargetHeight => _targetHeight;

        public float DefaultPitch => Mathf.Clamp(_defaultPitch, MinPitch, MaxPitch);

        public float OpeningPitch => Mathf.Clamp(_openingPitch, MinPitch, MaxPitch);

        public float MinPitch => Mathf.Min(_minPitch, _maxPitch);

        public float MaxPitch => _maxPitch;

        public float RecenterDelay => _recenterDelay;

        public float RecenterRampTime => _recenterRampTime;

        public float RecenterHalfLife => _recenterHalfLife;

        public float RecenterMinSpeed => _recenterMinSpeed;

        public float DownhillLift => _downhillLift;

        public float DownhillFullAngle => _downhillFullAngle;

        public float LiftHalfLife => _liftHalfLife;

        public float BaseFov => _baseFov;

        public float SpeedFovBoost => _speedFovBoost;

        public float FovHalfLife => _fovHalfLife;

        public float NearClip => _nearClip;

        public float FarClip => _farClip;

        public float BumpPerImpact => _bumpPerImpact;

        public float MaxBumpKick => _maxBumpKick;

        public float BumpFrequency => _bumpFrequency;

        public float BumpDamping => _bumpDamping;

        public CameraMomentSettings DigMoment => _digMoment;

        public CameraMomentSettings RelicMoment => _relicMoment;

        public CameraMomentSettings UpgradeMoment => _upgradeMoment;

        public CameraMomentSettings LeapMoment => _leapMoment;

        public CameraMomentSettings RelayMoment => _relayMoment;

        public CameraMomentSettings InstallMoment => _installMoment;

        public CameraMomentSettings GiftMoment => _giftMoment;

        public KitViewSettings KitViews => _kitViews;

        public float LeapMomentMinStrength => _leapMomentMinStrength;

        public float LeapLookAhead => _leapLookAhead;

        public float MomentCancelEaseOut => _momentCancelEaseOut;

        public float MomentBlendHalfLife => _momentBlendHalfLife;

        public WideShotSettings WideShot => _wideShot;

        public Vector3 PositionDamping => _positionDamping;

        public float YawDamping => _yawDamping;

        public Vector2 AimDamping => _aimDamping;

        public Vector2 ScreenPosition => _screenPosition;

        public float CameraRadius => _cameraRadius;

        public float AvoidanceDamping => _avoidanceDamping;

        public float OcclusionDamping => _occlusionDamping;

        public float OcclusionSmoothing => _occlusionSmoothing;

        public float TerrainProbe => _terrainProbe;
    }
}
