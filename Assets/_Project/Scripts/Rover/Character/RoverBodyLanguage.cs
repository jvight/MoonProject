using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's procedural body language on the RoverModel's Neck, Head, Eyelid, SolarWing, Eye and AntennaTip:
    /// the head looks at whatever it interacts with (gaze requests made through <see cref="IRoverRig"/>) or along its
    /// way with a lagging glance into turns; left alone it drifts into a daydream (<see cref="RoverMood"/>) and looks
    /// up toward Earth. Each session opens with 07 asleep; it wakes on its own (or as soon as the player drives),
    /// publishing <see cref="RoverAwoke"/>. It reacts to the game: soft landings, waking up, salvage (happier as a
    /// combo climbs), a relic answering or surfacing (a glance and a perk-up), a deposit (a contented nod), a snapped
    /// tether (a sigh), and hard landings (a small "oof"). When the camera opens to the lonely wide shot
    /// (<see cref="RoverWideShotChanged"/>) its daydream sigh lands with the frame. A relay mast it restored gets a
    /// long look up at its lamp and a perk-up; a new signal of Bell's within range gets a glance; landing from
    /// a radio-hop, 07 rouses and looks around (<see cref="LookAround"/>). Once the Rover Bay has fitted a kit piece
    /// (its Install Impact) and lets 07 go, turned to show it, 07 strikes a proud pose (head up, eye bright,
    /// antenna wiggle); a friend's gift gets a softer one as it settles; with Tilly's cell in its wing the wing settles
    /// open wider. Its battery shows in it too (docs/features/M3-20): at low charge the eye dims a little, the head
    /// bows and the wing stays flatter, quieter still while it crawls on the reserve; a charger perks it up; a
    /// refused leap gets a glance at its flank lamps and the VeryLow warning a look toward the nearest lit charger. From
    /// tower level 1 its antenna tip lights cyan, and each time 07 listens the antenna lifts and the tip blinks
    /// (<see cref="AntennaListen"/>).
    /// Needs <see cref="IWorldLayout"/>, so it must be initialised after the World systems.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    public sealed class RoverBodyLanguage : MonoBehaviour, IGameSystem
    {
        /// <summary>07's blinks are deterministic; the seed is its serial number.</summary>
        private const uint MoodSeed = 7u;

        /// <summary>How much brighter (share) the eye glows at the focus pulse's peak in the self-test's lens check.</summary>
        private const float PostEyeGlow = 0.6f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("Character tuning (Assets/_Project/Data/Tuning/RoverCharacterTuning.asset).")]
        [SerializeField] private RoverCharacterTuning _tuning;

        [Tooltip("The rover this body language belongs to.")]
        [SerializeField] private RoverController _rover;

        [Tooltip("The rover's visual rig (antenna wiggle, chassis squash).")]
        [SerializeField] private RoverVisualRig _rig;

        [Tooltip("RoverModel 'Neck' (yaw = local Y).")]
        [SerializeField] private Transform _neck;

        [Tooltip("RoverModel 'NeckLift' (local X: + = droop/sag, - = alert lift).")]
        [SerializeField] private Transform _neckLift;

        [Tooltip("RoverModel 'Head' (pitch = local X, roll = local Z).")]
        [SerializeField] private Transform _head;

        [Tooltip("RoverModel 'SolarWing' (local X: 0 = folded, + = opening).")]
        [SerializeField] private Transform _solarWing;

        [Tooltip("RoverModel 'CutterTurret' (chest cutter yaw = local Y).")]
        [SerializeField] private Transform _cutterTurret;

        [Tooltip("RoverModel 'CutterHead' (chest cutter pitch = local X).")]
        [SerializeField] private Transform _cutterHead;

        [Tooltip("RoverModel 'ScoopArms' (belly scoop arms pitch = local X).")]
        [SerializeField] private Transform _scoopArms;

        [Tooltip("RoverModel 'ScoopBucket' (belly scoop bucket curl = local X).")]
        [SerializeField] private Transform _scoopBucket;

        [Tooltip("RoverModel 'WinchSpool' (chest winch spool spin = local X).")]
        [SerializeField] private Transform _winchSpool;

        [Tooltip("MeshRenderer of RoverModel 'Eye' (glow modulated through _EmissionColor).")]
        [SerializeField] private Renderer _eyeRenderer;

        [Tooltip("MeshRenderer of RoverModel 'AntennaTip' (glow modulated through _EmissionColor).")]
        [SerializeField] private Renderer _antennaTipRenderer;

        [Tooltip("Small warm point light inside the eye; breathes with the glow.")]
        [SerializeField] private Light _eyeLight;

        [Tooltip("Art's ScanFan on Eye/ScanFanSocket: the scan's sweeping fan of light, dark until a scan.")]
        [SerializeField] private Renderer _scanFan;

        [Tooltip("07's CRT face director on Neck/NeckLift/Head/Screen (0.5.7).")]
        [SerializeField] private RoverFaceDirector _faceDirector;

        /// <summary>Owner of 07's own glances in the shared gaze requests.</summary>
        private readonly object _glanceOwner = new object();
        private readonly object _cutOwner = new object();
        /// <summary>Save section "rover.character": what 07 remembers of its own moments. Never rename the key.</summary>
        private const string SaveKey = "rover.character";
        private const int SaveVersion = 1;

        private readonly object _scanOwner = new object();
        private ScanLook _scanLook;
        private ElderGlance _elderGlance;
        private Vector3 _elderPoint;
        private bool _hasElder;
        private IDisposable _saveToken;
        private Quaternion _scanFanRest;
        private bool _scanGazing;
        private readonly ImpactNudge _headNudge = new ImpactNudge();
        private float _cutFocus;
        private bool _cutting;
        private bool _isCutTool;
        private Vector3 _cutTarget;
        private CutterRefusal _cutterRefusal;
        private float _lampPower = 1f;
        private float _postWing = -1f;
        private float _postEye;
        private float _postAntenna;
        private float _postTurret;
        private float _cutterYaw;
        private float _cutterPitch;
        private bool _digging;
        private float _digFocus;
        private float _scoopKick;
        private bool _tetherActive;
        private float _tetherStrain;
        private float _winchReel;
        private float _winchAngle;
        private float _towSpool;
        private float _towStrain;
        private float _cargoTarget;
        private float _cargoSag;
        private float _cutEnds = -1f;
        private RoverVoice _voice;
        private ChirpPose _chirpPose;
        private readonly CargoLoad _cargo = new CargoLoad();
        private float _sigh;

        private RoverMood _mood;
        private LookAround _lookAround;
        private const int ReleaseLureCount = 2;

        private AntennaListen _listen;
        private CallListen _call;
        private ICallSource _callSource;
        private ICallSignal _callSignal;
        private bool _charging;
        private IWorldLayout _world;
        private GameContext _context;
        private ISkyGaze _sky;
        private EventBus _events;
        private IDisposable[] _subscriptions;
        private float _glanceEnds = -1f;
        private readonly Vector3[] _releaseLooks = new Vector3[ReleaseLureCount];
        private int _releaseLook = -1;
        private float _releaseNextAt;
        private MaterialPropertyBlock _glowBlock;
        private RoverDustCoat _dustCoat;
        private DampedSpring _yaw;
        private DampedSpring _pitch;
        private bool _hasActiveGaze;
        private Vector2 _targetGazeAim;
        private Quaternion _neckRest;
        private Quaternion _neckLiftRest;
        private Quaternion _headRest;
        private Quaternion _wingRest;
        private Quaternion _cutterTurretRest;
        private Quaternion _cutterHeadRest;
        private Quaternion _scoopArmsRest;
        private Quaternion _scoopBucketRest;
        private Quaternion _winchSpoolRest;
        private float _wakeTilt;
        private BatteryLink _battery;
        private float _tired;
        private float _crawl;
        private bool _screenInspectOpen;
        private float _screenInspectBlend;
        private float _screenInspectYawDeg;
        private float _screenInspectPitchDeg = 10f;
        private Vector3 _lensOffsetInHead;
        private float _scanAimBlend;
        private bool _scanBodyTurning;
        private float _scanBodyYawDelta;
        private float _screenInspectCounterRollShare = 0.5f;
        private float _screenInspectCounterRollMaxDeg = 8f;
        private float _openingPoseBlend;
        private float _openingYawDeg;
        private float _openingPitchDeg;
        private bool _openingLevels;
        private Color _eyeLightRestColor = Color.white;
        private Vector3 _eyeLightRestLocalPos;
        private bool _initialized;

        private const float ScreenInspectBlendSeconds = 0.30f;
        private const float ScreenInspectYawLimit = 35f;
        private const float ScanEyeEmissionBoost = 1.25f;
        private const float ScanEyeOpenThreshold = 1e-3f;

        /// <summary>Read-only view of the mood, for audio/debug tooling.</summary>
        public RoverMood Mood => _mood;

        /// <summary>Current normalized tether strain [0..1] reported by the tether feel beats.</summary>
        public float TetherStrain => _tetherStrain;

        /// <summary>07's CRT face director wired on <c>Neck/NeckLift/Head/Screen</c>.</summary>
        public RoverFaceDirector FaceDirector => _faceDirector;

        /// <summary>True when <see cref="StepGaze"/> has an active gaze target this frame.</summary>
        public bool HasActiveGaze => _hasActiveGaze;

        /// <summary>Target (yaw, pitch) gaze angles in degrees selected by <see cref="StepGaze"/>.</summary>
        public Vector2 TargetGazeAim => _targetGazeAim;

        /// <summary>Current slewed (yaw, pitch) head angles in degrees.</summary>
        public Vector2 CurrentHeadAngles => new Vector2(_yaw.Value, _pitch.Value);

        /// <summary>True while 07's head is lifted and steadied for CRT screen inspection.</summary>
        public bool IsScreenInspectOpen => _screenInspectOpen;

        /// <summary>
        /// True when a scan target lies outside 07's head reach and 07 is turning its body in place toward it.
        /// </summary>
        public bool TryGetScanBodyTurn(out float bodyYawDeltaDeg, out float maxTurnRateDegPerSec, out float halfLife)
        {
            if (_initialized && _scanBodyTurning && !_screenInspectOpen)
            {
                ScanLookSettings scan = _tuning.ScanLook;
                bodyYawDeltaDeg = _scanBodyYawDelta;
                maxTurnRateDegPerSec = scan.BodyTurnRate;
                halfLife = scan.BodyTurnHalfLife;
                return true;
            }

            bodyYawDeltaDeg = 0f;
            maxTurnRateDegPerSec = 0f;
            halfLife = 0.1f;
            return false;
        }

        /// <summary>Smoothed blend factor in [0..1] for screen-inspect head steadying.</summary>
        public float ScreenInspectBlend => _screenInspectBlend * _screenInspectBlend * (3f - 2f * _screenInspectBlend);

        /// <summary>Transform of 07's <c>Neck</c> yaw joint.</summary>
        public Transform NeckTransform => _neck;

        /// <summary>CRT curvature is physically modelled by the convex Screen mesh (0 UV warp).</summary>
        public float Curvature => _faceDirector != null ? _faceDirector.Curvature : 0f;

        /// <summary>Transform of 07's CRT <c>Screen</c> mesh.</summary>
        public Transform ScreenTransform =>
            _faceDirector != null && _faceDirector.Display != null && _faceDirector.Display.ScreenRenderer != null
                ? _faceDirector.Display.ScreenRenderer.transform
                : _head;

        /// <summary>
        /// Configures the share (0..1) and maximum angle (deg) of the head counter-roll 07 applies toward level when
        /// lifting its head for CRT screen inspection on a slope.
        /// </summary>
        public void SetScreenInspectCounterRoll(float counterRollShare, float counterRollMaxDeg)
        {
            _screenInspectCounterRollShare = Mathf.Clamp01(counterRollShare);
            _screenInspectCounterRollMaxDeg = Mathf.Max(0f, counterRollMaxDeg);
        }

        /// <summary>
        /// Computes the signed head roll (deg around the head's local +Z forward axis) that eases a pitched head with
        /// world rotation <paramref name="pitchedHeadWorldRotation"/> toward world level by
        /// <paramref name="counterRollShare"/>, clamped to <c>+/-<paramref name="counterRollMaxDeg"/></c>.
        /// </summary>
        public static float ComputeInspectHeadCounterRoll(
            Quaternion pitchedHeadWorldRotation,
            float counterRollShare,
            float counterRollMaxDeg)
        {
            if (counterRollShare <= 0f || counterRollMaxDeg <= 0f)
            {
                return 0f;
            }

            Vector3 headForward = pitchedHeadWorldRotation * Vector3.forward;
            Vector3 headUp = pitchedHeadWorldRotation * Vector3.up;
            Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, headForward);
            if (levelUp.sqrMagnitude <= 1e-6f)
            {
                return 0f;
            }

            levelUp.Normalize();
            float levelRoll = Vector3.SignedAngle(headUp, levelUp, headForward);
            float maxRoll = Mathf.Abs(counterRollMaxDeg);
            return Mathf.Clamp(levelRoll * Mathf.Clamp01(counterRollShare), -maxRoll, maxRoll);
        }

        /// <summary>
        /// Forward neck droop (<paramref name="liftDeg"/> &gt; 0) bows into the forward hemisphere (<c>|yaw| &lt; 90</c>)
        /// and fades to zero as the neck turns over the rear deck (<c>|yaw| &gt;= 90</c>), so a turned head and its
        /// brow-mounted headlamp pods never bow backward into the raised <c>SolarWing</c>. Alert perk lift
        /// (<paramref name="liftDeg"/> &lt;= 0) is unchanged.
        /// </summary>
        public static float EffectiveNeckLift(float liftDeg, float yawDeg)
        {
            return liftDeg > 0f ? liftDeg * Mathf.Max(0f, Mathf.Cos(yawDeg * Mathf.Deg2Rad)) : liftDeg;
        }

        /// <summary>
        /// Enters or exits screen-inspect mode: lifts 07's head by <paramref name="targetPitchDeg"/> (+10 deg), turns
        /// the neck toward <paramref name="targetYawDeg"/> (clamped to +/-35 deg), eases the head roll toward level,
        /// suppresses idle head noise so the CRT screen is rock-steady for clicking, and switches
        /// <see cref="FaceDirector"/> into screen content mode.
        /// </summary>
        public void SetScreenInspect(
            bool open,
            float targetYawDeg = 0f,
            float targetPitchDeg = 10f,
            Texture contentTexture = null,
            float counterRollShare = 0.5f,
            float counterRollMaxDeg = 8f)
        {
            _screenInspectOpen = open;
            if (open)
            {
                _screenInspectYawDeg = Mathf.Clamp(targetYawDeg, -ScreenInspectYawLimit, ScreenInspectYawLimit);
                _screenInspectPitchDeg = targetPitchDeg;
                _screenInspectCounterRollShare = Mathf.Clamp01(counterRollShare);
                _screenInspectCounterRollMaxDeg = Mathf.Max(0f, counterRollMaxDeg);
            }

            if (_faceDirector != null)
            {
                _faceDirector.SetScreenMode(open, contentTexture);
            }
        }

        /// <summary>
        /// Evaluates the exact world-space position and rotation of 07's CRT <c>Screen</c> when the neck is turned by
        /// <paramref name="neckYawDeg"/> and the head is pitched up by <paramref name="headPitchDeg"/> with all idle
        /// head noise suppressed.
        /// </summary>
        public void EvaluateScreenPose(
            float neckYawDeg,
            float headPitchDeg,
            out Vector3 screenPosition,
            out Quaternion screenRotation)
        {
            EvaluateScreenPose(neckYawDeg, headPitchDeg, out _, out _, out screenPosition, out screenRotation);
        }

        /// <summary>
        /// Evaluates the exact world-space position and rotation of 07's CRT <c>Screen</c> when the neck is turned by
        /// <paramref name="neckYawDeg"/> and the head is pitched up by <paramref name="headPitchDeg"/> with all idle
        /// head noise suppressed.
        /// </summary>
        public void EvaluateScreenPose(
            float neckYawDeg,
            float headPitchDeg,
            out Vector3 neckPivotWorld,
            out Quaternion roverRotation,
            out Vector3 screenPosition,
            out Quaternion screenRotation)
        {
            Transform modelRoot = _neck != null && _neck.parent != null ? _neck.parent : transform;
            roverRotation = modelRoot.rotation;
            neckPivotWorld = _neck != null ? modelRoot.TransformPoint(_neck.localPosition) : modelRoot.position;

            Quaternion neckRest = _initialized ? _neckRest : (_neck != null ? _neck.localRotation : Quaternion.identity);
            Quaternion neckLiftRest = _initialized ? _neckLiftRest : (_neckLift != null ? _neckLift.localRotation : Quaternion.identity);
            Quaternion headRest = _initialized ? _headRest : (_head != null ? _head.localRotation : Quaternion.identity);

            float clampedYaw = Mathf.Clamp(neckYawDeg, -ScreenInspectYawLimit, ScreenInspectYawLimit);
            Quaternion neckWorldRot = roverRotation * (neckRest * Quaternion.AngleAxis(clampedYaw, Vector3.up));

            Vector3 headParentPos = neckPivotWorld;
            Quaternion headParentRot = neckWorldRot;
            if (_neckLift != null)
            {
                headParentPos = neckPivotWorld + neckWorldRot * _neckLift.localPosition;
                headParentRot = neckWorldRot * neckLiftRest;
            }

            Vector3 headLocalPos = _head != null ? _head.localPosition : Vector3.zero;
            Vector3 headWorldPos = headParentPos + headParentRot * headLocalPos;
            Quaternion pitchedHeadWorldRot = headParentRot * (headRest * Quaternion.AngleAxis(-headPitchDeg, Vector3.right));
            float counterRoll = ComputeInspectHeadCounterRoll(
                pitchedHeadWorldRot,
                _screenInspectCounterRollShare,
                _screenInspectCounterRollMaxDeg);
            Quaternion headWorldRot = pitchedHeadWorldRot * Quaternion.AngleAxis(counterRoll, Vector3.forward);

            Transform screen = ScreenTransform;
            if (screen != null && screen != _head)
            {
                Vector3 localOffset = screen.localPosition;
                if (_faceDirector != null && _faceDirector.Display != null && _faceDirector.Display.ScreenRenderer != null)
                {
                    localOffset += screen.localRotation * Vector3.Scale(
                        _faceDirector.Display.ScreenRenderer.localBounds.center,
                        screen.localScale);
                }

                screenPosition = headWorldPos + headWorldRot * localOffset;
                screenRotation = headWorldRot * screen.localRotation;
            }
            else
            {
                screenPosition = headWorldPos;
                screenRotation = headWorldRot;
            }
        }

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _context = context;
            _world = context.Get<IWorldLayout>();
            _mood = new RoverMood(_tuning, MoodSeed, _tuning.SleepOnBoot);
            _lookAround = new LookAround(_tuning);
            _listen = new AntennaListen(_tuning.Listening);
            _call = new CallListen(_tuning.CallListen);
            _glowBlock = new MaterialPropertyBlock();
            _battery = new BatteryLink(context);
            _neckRest = _neck.localRotation;
            _neckLiftRest = _neckLift != null ? _neckLift.localRotation : Quaternion.identity;
            _headRest = _head.localRotation;
            _wingRest = _solarWing.localRotation;
            _cutterTurretRest = _cutterTurret != null ? _cutterTurret.localRotation : Quaternion.identity;
            _cutterHeadRest = _cutterHead != null ? _cutterHead.localRotation : Quaternion.identity;
            _scoopArmsRest = _scoopArms != null ? _scoopArms.localRotation : Quaternion.identity;
            _scoopBucketRest = _scoopBucket != null ? _scoopBucket.localRotation : Quaternion.identity;
            _winchSpoolRest = _winchSpool != null ? _winchSpool.localRotation : Quaternion.identity;
            _scanFanRest = _scanFan.transform.localRotation;
            _lensOffsetInHead = _head != null && _rover.ScanOrigin != null
                ? _head.InverseTransformPoint(_rover.ScanOrigin.position)
                : Vector3.zero;
            _scanLook = new ScanLook(_tuning.ScanLook);
            InitializeElder(context);
            _scanFan.enabled = false;
            _yaw.Reset(0f);
            _pitch.Reset(_tuning.TravelHeadPitch);
            _eyeLight.type = LightType.Point;
            _eyeLight.range = _tuning.EyeLightRange;
            _eyeLight.shadows = LightShadows.None;
            _eyeLightRestColor = _eyeLight.color;
            _eyeLightRestLocalPos = _eyeLight.transform.localPosition;

            EventBus events = context.Events;
            _chirpPose = new ChirpPose(_tuning.Chirp);
            _cutterRefusal = new CutterRefusal(_tuning.CutFocus);
            _voice = new RoverVoice(events, _tuning.Chirp, () => Time.time);
            _events = events;
            _subscriptions = new[]
            {
                events.Subscribe<RoverLanded>(OnLanded),
                events.Subscribe<MaterialSalvaged>(OnMaterialSalvaged),
                events.Subscribe<RelicAnswered>(OnRelicAnswered),
                events.Subscribe<RelicSurfaced>(OnRelicSurfaced),
                events.Subscribe<RelicDeposited>(OnRelicDeposited),
                events.Subscribe<TetherAttached>(OnTetherAttached),
                events.Subscribe<TetherReleased>(OnTetherReleased),
                events.Subscribe<RoverRecovering>(OnRecovering),
                events.Subscribe<RoverWideShotChanged>(OnWideShotChanged),
                events.Subscribe<RelayRestored>(OnRelayRestored),
                events.Subscribe<BellSignalPicked>(OnBellSignalPicked),
                events.Subscribe<RadioHopFinished>(OnRadioHopFinished),
                events.Subscribe<ChargingChanged>(OnChargingChanged),
                events.Subscribe<BatteryWarned>(OnBatteryWarned),
                events.Subscribe<FeelBeat>(OnFeelBeat),
                events.Subscribe<CutterRefused>(OnCutterRefused),
                events.Subscribe<RoverChirpSyllable>(OnChirpSyllable),
                events.Subscribe<BasePowerChanged>(OnBasePowerChanged),
                events.Subscribe<FriendSpotted>(spotted => Glance(spotted.Position)),
                events.Subscribe<CargoChanged>(OnCargoChanged),
                events.Subscribe<SonarPinged>(OnListened),
                events.Subscribe<ReleaseViewOpened>(OnReleaseView),
                events.Subscribe<CallReplyFailed>(_ => _call.ReplyFailed()),
            };
            _initialized = true;
            _faceDirector.Initialize(context);
            Apply(0f);
        }

        /// <summary>
        /// The title is up (docs/features/M3-09): 07 stays asleep, neither the settle delay nor the player's input
        /// wakes it, until the opening or the short wake begins.
        /// </summary>
        /// <summary>
        /// How much of its lamp power 07 has (the wake's flicker and ramp, 1 normally): scales the eye lamp, and the
        /// road light through <see cref="LampPower01"/>.
        /// </summary>
        public void SetLampPower(float power)
        {
            _lampPower = Mathf.Clamp01(power);
        }

        /// <summary>The lamp power the wake allows now, 0..1.</summary>
        public float LampPower01 => _lampPower;

        /// <summary>
        /// What the self-test's checks do on 07 beyond its running gear: the wing's openness (a negative value leaves it
        /// to its mood), the antenna's extra rise (m), the cutter turret's sideways kick (deg) and the eye's focus
        /// pulse (0..1).
        /// </summary>
        public void SetPostPose(float wingOpen, float antennaLift, float turretYaw, float eyeFocus)
        {
            _postEye = eyeFocus;
            _postWing = wingOpen;
            _postAntenna = antennaLift;
            _postTurret = turretYaw;
        }

        /// <summary>The self-test's motion on the running gear (the motor's shudder and lurch, the wheels' twitches).</summary>
        public void SetPostMotion(in PostBodyMotion motion)
        {
            _rig.SetPostMotion(motion);
        }

        /// <summary>
        /// Lays the long-sleep regolith coat on 07 as <paramref name="dust"/> says it is, part by part; at all zero 07 is
        /// as the driving left it. <paramref name="heavyAmount"/> is the dust amount of a part under the full coat.
        /// </summary>
        public void SetPostDust(PostDust dust, float heavyAmount)
        {
            if (_dustCoat == null)
            {
                _dustCoat = new RoverDustCoat(transform, _head, _solarWing, _cutterTurret, _rig.Antenna,
                    _rig.LowerBodyRenderers);
            }

            for (int i = 0; i < PostDust.ZoneCount; i++)
            {
                var zone = (DustZone)i;
                if (zone != DustZone.Wheels)
                {
                    _dustCoat.Set(zone, dust.Level(zone), heavyAmount);
                }
            }

            _rig.SetPostDust(dust.Level(DustZone.Wheels), heavyAmount);
        }

        /// <summary>Where a part is, for the dust that falls off it (the coat must have been laid at least once).</summary>
        public Vector3 DustAnchor(DustZone zone)
        {
            return _dustCoat.AnchorPosition(zone);
        }

        public void HoldAsleep()
        {
            _mood.Wake.Hold();
            if (_faceDirector != null)
            {
                _faceDirector.SetOpeningDarkHold(true);
            }
        }

        /// <summary>No title in this game (fixtures, test runs): 07 wakes on its own, as it always did.</summary>
        public void WakeOnItsOwn()
        {
            if (_faceDirector != null)
            {
                _faceDirector.SetOpeningDarkHold(false);
            }

            _mood.Wake.Release();
        }

        /// <summary>Holds or releases the CRT face dark-hold for the opening sequence.</summary>
        public void HoldOpeningScreenDark(bool hold)
        {
            if (_faceDirector != null)
            {
                _faceDirector.SetOpeningDarkHold(hold);
            }
        }

        /// <summary>The opening or the short wake takes 07's wake over (RoverAwoke follows on the next frame).</summary>
        public void BeginWake()
        {
            _mood.Wake.BeginScripted();
        }

        /// <summary>
        /// The wake's pose this frame: <paramref name="sleep"/> bows the head and darkens the eye (1 asleep),
        /// <paramref name="lidOpen"/> is the lid's openness taking over by <paramref name="lidWeight"/>, and
        /// <paramref name="tilt"/> rolls the head (deg) to one side.
        /// </summary>
        public void PoseWake(float sleep, float lidOpen, float lidWeight, float tilt)
        {
            _mood.Wake.SetScriptedSleep(sleep);
            _mood.SetWakeLid(lidOpen, lidWeight);
            _wakeTilt = tilt;
        }

        /// <summary>
        /// Poses 07's neck and head directly for the opening sequence: bowed while it sleeps, then lifted into the
        /// opening's one shot of its face, with each check's head motion on top.
        /// </summary>
        /// <param name="levels">
        /// Roll the head toward level as the inventory's close-up does (<see cref="SetScreenInspectCounterRoll"/>), so the
        /// face sits exactly where that close-up's composer expects it.
        /// </param>
        public void PoseOpeningHead(float yawDeg, float pitchDeg, float blend, bool levels)
        {
            _openingPoseBlend = Mathf.Clamp01(blend);
            _openingYawDeg = yawDeg;
            _openingPitchDeg = pitchDeg;
            _openingLevels = levels;
            if (_openingPoseBlend >= 0.999f)
            {
                _yaw.Reset(yawDeg);
                _pitch.Reset(pitchDeg);
            }

            if (_initialized)
            {
                Apply(SkyLift());
            }
        }

        /// <summary>The wake is done: the face and the head are 07's own again.</summary>
        public void FinishWake()
        {
            _mood.Wake.FinishScripted();
            _mood.SetWakeLid(0f, 0f);
            _wakeTilt = 0f;
            _openingPoseBlend = 0f;
            _openingLevels = false;
        }

        /// <summary>One blink of the eyes on 07's face (the wake's ready performance).</summary>
        public void BlinkFace()
        {
            if (_faceDirector != null)
            {
                _faceDirector.Blink();
            }
        }

        /// <summary>07's first blink (O11): the solar wing sighs and the antenna starts the call sign.</summary>
        public void GreetWithBlink(float sigh)
        {
            _mood.Sigh(sigh);
            _mood.RestartCallSign();
        }

        private bool ValidateWiring()
        {
            return Require(_tuning != null, "RoverCharacterTuning is not assigned.")
                & Require(_rover != null && _rig != null, "RoverController and RoverVisualRig must be assigned.")
                & Require(_neck != null && _neckLift != null && _head != null,
                    "Neck, NeckLift and Head nodes are not assigned.")
                & Require(_solarWing != null, "SolarWing node is not assigned.")
                & Require(_eyeRenderer != null && _antennaTipRenderer != null, "Eye/AntennaTip renderers are missing.")
                & Require(_eyeLight != null, "Eye light is not assigned.")
                & Require(_scanFan != null, "The ScanFan on Eye/ScanFanSocket is not assigned (0.5.5 scan contract).")
                & Require(_faceDirector != null && _faceDirector.ValidateWiring(),
                    "RoverFaceDirector on Screen is not assigned or incomplete (0.5.7 face contract).");
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverBodyLanguage)}: {message}", this);
            }

            return condition;
        }

        private void PerkUp(float strength)
        {
            _mood.PerkUp(strength);
            _rig.KickAntenna(Mathf.Clamp01(strength) * _tuning.PerkAntennaKick);
        }

        /// <summary>07 glances at <paramref name="point"/> for a moment (gameplay requests can override it).</summary>
        /// <summary>
        /// The release's view from the rise: 07's head looks at relay.1's lamp, the next goal, then briefly at the
        /// Peak's dish where the glint is, so the player's eye is led to the lamp first. Head only: the body stays put.
        /// </summary>
        private void OnReleaseView(ReleaseViewOpened opened)
        {
            _releaseLooks[0] = opened.Relay;
            _releaseLooks[1] = opened.Dish;
            _releaseLook = 0;
            _releaseNextAt = Time.time + _tuning.ReleaseRelayLookSeconds;
            Glance(_releaseLooks[0], _tuning.ReactionGazePriority, _tuning.ReleaseRelayLookSeconds);
        }

        /// <summary>Moves 07's release look on from the lamp to the dish when the lamp's time is up.</summary>
        private void StepReleaseView()
        {
            if (_releaseLook < 0 || Time.time < _releaseNextAt)
            {
                return;
            }

            _releaseLook++;
            if (_releaseLook >= ReleaseLureCount)
            {
                _releaseLook = -1;
                return;
            }

            _releaseNextAt = Time.time + _tuning.ReleasePeakLookSeconds;
            Glance(_releaseLooks[_releaseLook], _tuning.ReactionGazePriority, _tuning.ReleasePeakLookSeconds);
        }

        private void Glance(Vector3 point)
        {
            Glance(point, _tuning.ReactionGazePriority, _tuning.RelicGlanceSeconds);
        }

        /// <summary>07 looks at <paramref name="point"/> for a while at a gaze priority.</summary>
        private void Glance(Vector3 point, int priority, float seconds)
        {
            _rover.Gaze.Set(_glanceOwner, point, priority);
            _glanceEnds = Time.time + seconds;
        }

        private void OnMaterialSalvaged(MaterialSalvaged salvage)
        {
            float strength = _tuning.SalvagePerk + _tuning.SalvageComboPerk * salvage.ComboStep;
            PerkUp(Mathf.Min(strength, _tuning.SalvagePerkMax));
        }

        private void OnRelicAnswered(RelicAnswered relic)
        {
            Glance(relic.Position);
            PerkUp(_tuning.RelicAnsweredPerk);
        }

        private void OnRelicSurfaced(RelicSurfaced relic)
        {
            if (!_cutting)
            {
                EndCutFocus();
            }

            Glance(relic.Position);
            PerkUp(_tuning.RelicSurfacedPerk);
        }

        private void OnRelicDeposited(RelicDeposited deposit)
        {
            _mood.NodContentedly(_tuning.DepositNod);
        }

        private void OnRecovering(RoverRecovering recovering)
        {
            PerkUp(_tuning.RecoveryPerk);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _tetherActive = true;
            _tetherStrain = 0f;
            _winchReel = 1f;
        }

        private void OnTetherReleased(TetherReleased release)
        {
            _tetherActive = false;
            _tetherStrain = 0f;
            if (release.Snapped)
            {
                _mood.Sigh(_tuning.SnapSigh);
            }
        }

        private void OnWideShotChanged(RoverWideShotChanged wideShot)
        {
            _mood.SetWideShot(wideShot.Wide);
        }

        /// <summary>The mast 07 just restored lights up: a long look up at its warming lamp, and a perk-up.</summary>
        private void OnRelayRestored(RelayRestored relay)
        {
            Glance(relay.Position, _tuning.RelayLookPriority, _tuning.RelayLookSeconds);
            PerkUp(_tuning.RelayRestoredPerk);
        }

        /// <summary>Bell pointed at something: a glance toward the new signal if it is within range.</summary>
        private void OnBellSignalPicked(BellSignalPicked signal)
        {
            Vector3 offset = signal.Position - _rover.Position;
            float range = _tuning.SignalGlanceRange;
            if (offset.x * offset.x + offset.z * offset.z > range * range)
            {
                return;
            }

            Glance(signal.Position + Vector3.up * _tuning.SignalGlanceLift, _tuning.ReactionGazePriority,
                _tuning.SignalGlanceSeconds);
        }

        /// <summary>A charger started filling the cells: 07 perks up (head lift, brighter eye, antenna wiggle).</summary>
        private void OnChargingChanged(ChargingChanged charging)
        {
            _charging = charging.Charging;
            if (charging.Charging)
            {
                PerkUp(_tuning.Battery.ChargePerk);
            }
        }

        /// <summary>The VeryLow warning: 07 looks toward the nearest charger that works, a gentle "home is there".</summary>
        private void OnBatteryWarned(BatteryWarned warned)
        {
            if (warned.Warning == BatteryWarning.VeryLow)
            {
                BatteryMoodSettings battery = _tuning.Battery;
                Glance(warned.NearestLitCharger + Vector3.up * battery.ChargerGlanceLift,
                    _tuning.ReactionGazePriority, battery.ChargerGlanceSeconds);
            }
        }

        /// <summary>Landed from a radio-hop: 07 rouses from any daydream and looks around, "where am I?".</summary>
        private void OnRadioHopFinished(RadioHopFinished hop)
        {
            _mood.Rouse();
            _lookAround.Start();
            PerkUp(_tuning.HopArrivalPerk);
        }

        /// <summary>
        /// From tower level 1 07 can listen: its antenna tip lights cyan (at once on a load). As home's windows light
        /// (not on a load) 07 also lifts its head, perks up and glances home.
        /// </summary>
        private void OnBasePowerChanged(BasePowerChanged changed)
        {
            _listen.SetAwake(changed.Stage >= BasePowerStage.Home, changed.Restored);
            if (!changed.Restored && changed.Stage != BasePowerStage.Asleep)
            {
                PerkUp(_tuning.KitProudPerk);
                Glance(_world.BasePosition);
            }
        }

        private void OnListened(SonarPinged listened)
        {
            _listen.Listen();
        }

        private void OnLanded(RoverLanded landed)
        {
            float oof = _mood.OofStrength(landed.ImpactSpeed);
            if (oof > 0f)
            {
                _mood.FeelImpact(oof);
                _rig.KickHeave(-oof * _tuning.OofSquashKick);
            }
            else
            {
                PerkUp(Mathf.Max(_tuning.SoftLandingPerk, _mood.LandingJoy(landed.AirTime)));
            }
        }

        /// <summary>
        /// 06, 07's elder, slumped by relay.2's hut (world's micro.06 site), and whether 07 has met it (save section
        /// "rover.character").
        /// </summary>
        private void InitializeElder(GameContext context)
        {
            _elderGlance = new ElderGlance(_tuning.ElderGlance);
            _saveToken = context.Get<ISaveService>().Register(new SaveSection<RoverSaveData>(SaveKey, SaveVersion,
                () => new RoverSaveData { metElder06 = _elderGlance.Greeted },
                data => _elderGlance.Restore(data.metElder06)));
            _hasElder = context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.Micro06, out WorldAnchor site);
            if (!_hasElder)
            {
                Debug.LogError($"{nameof(RoverBodyLanguage)}: the world has no '{WorldAnchorIds.Micro06}' anchor; 07 "
                    + "cannot look at its elder 06.", this);
                return;
            }

            _elderPoint = site.Position + Vector3.up * _tuning.ElderGlance.LookHeight;
        }

        /// <summary>The first time 07 comes near 06: one held look and a soft, low chirp. No text.</summary>
        private void StepElder()
        {
            if (_hasElder && _elderGlance.Step(_rover.Position, _elderPoint))
            {
                ElderGlanceSettings elder = _tuning.ElderGlance;
                Glance(_elderPoint, elder.GazePriority, elder.HoldSeconds);
                _voice.Elder(elder.ChirpIntensity);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

            _screenInspectBlend = Mathf.MoveTowards(
                _screenInspectBlend,
                _screenInspectOpen ? 1f : 0f,
                deltaTime / ScreenInspectBlendSeconds);

            StepElder();

            _mood.SetEffort(0f);
            float activeInput = Mathf.Max(_rover.DriveInput.magnitude,
                _mood.Wake.Phase == WakePhase.Awake ? _scanAimBlend : 0f);
            switch (_mood.Step(_rover.Speed, activeInput, deltaTime))
            {
                case MoodTransition.BeganWaking:
                    _events.Publish(new RoverAwoke(_rover.Position, _mood.Wake.WokenByPlayer));
                    break;
                case MoodTransition.FinishedWaking:
                    PerkUp(_mood.Wake.WokenByPlayer ? 0f : _tuning.AwakenedPerk);
                    break;
                case MoodTransition.WokeFromDaydream:
                    PerkUp(_tuning.WakePerk);
                    break;
            }

            StepReleaseView();
            if (_glanceEnds >= 0f && Time.time >= _glanceEnds)
            {
                _rover.Gaze.Clear(_glanceOwner);
                _glanceEnds = -1f;
            }

            if (!_cutting && _cutEnds >= 0f && Time.time >= _cutEnds)
            {
                EndCutFocus();
            }

            StepBattery(deltaTime);
            float focusHalfLife = _tuning.CutFocus.HalfLife;
            _cutFocus = Smoothing.Damp(_cutFocus, _cutting ? 1f : 0f, focusHalfLife, deltaTime);
            StepTools(focusHalfLife, deltaTime);
            _scanLook.Step(deltaTime);
            if (_scanGazing && !_scanLook.HoldsGaze)
            {
                _scanGazing = false;
                _rover.Gaze.Clear(_scanOwner);
            }
            _chirpPose.Step(deltaTime);
            _sigh = Smoothing.Damp(_sigh, 0f, _tuning.Chirp.SighHalfLife, deltaTime);
            _cargoSag = Smoothing.Damp(_cargoSag, _cargoTarget, 0.25f, deltaTime);
            _headNudge.Step(deltaTime);
            _listen.Step(deltaTime);
            StepCall(deltaTime);
            _rig.SetAntennaLift(_listen.Lift + _postAntenna + _call.Reach * _tuning.CallListen.ReachAntennaLift);
            _lookAround.Step(deltaTime);
            float sky = SkyLift();
            StepGaze(sky, deltaTime);
            Apply(sky);
            _faceDirector.Step(deltaTime, _hasActiveGaze, _targetGazeAim.x, _targetGazeAim.y, _yaw.Value, _pitch.Value);
        }

        /// <summary>Glow the antenna lamp adds for the call now (the stutter while it is broken, the keyed pattern once it
        /// locks).</summary>
        public float CallLampGlow => _call != null ? _call.LampGlow : 0f;

        /// <summary>The call-listening tuning in use.</summary>
        public CallListenSettings CallSettings => _tuning.CallListen;

        /// <summary>
        /// The call from the sky, once Gameplay has registered it (picked up on the first frame it exists; a scene
        /// without one has no call to listen to): 07 listens while it idles or charges.
        /// </summary>
        private void StepCall(float deltaTime)
        {
            if (_callSource == null)
            {
                _context.TryGet(out _callSource);
            }

            if (_callSource == null)
            {
                return;
            }

            if (_callSignal == null)
            {
                _context.TryGet(out _callSignal);
            }

            bool listening = _mood.Idle > _tuning.CallListen.IdleShare || _charging;
            _call.Step(deltaTime, _callSource.State, listening, _callSignal != null ? _callSignal.Keying : 0f);
        }

        private void StepTools(float halfLife, float deltaTime)
        {
            _cutterRefusal.Step(deltaTime);
            _cutterYaw = _cutterRefusal.Yaw + _postTurret;
            _cutterPitch = _cutterRefusal.Pitch;
            _digFocus = Smoothing.Damp(_digFocus, _digging ? 1f : 0f, halfLife, deltaTime);
            _scoopKick = Smoothing.Damp(_scoopKick, 0f, halfLife, deltaTime);
            _winchReel = Smoothing.Damp(_winchReel, _tetherActive ? 1f : 0f, 0.15f, deltaTime);
            _winchAngle = Mathf.Repeat(_winchAngle + _winchReel * 360f * deltaTime, 360f);
            _mood.SetEffort(Mathf.Max(_tetherStrain, _towStrain));
        }

        /// <summary>
        /// The tow cable's work this frame (<see cref="RoverTowCable"/>): the degrees the winch drum has turned for the
        /// rope's travel, out positive, and how hard the cable pulls, 0..1, which 07 leans into.
        /// </summary>
        public void SetTow(float spoolDegrees, float strain01)
        {
            _towSpool = spoolDegrees;
            _towStrain = Mathf.Clamp01(strain01);
        }

        /// <summary>The tired look below the Low share and the quieter crawl, each eased in and out.</summary>
        private void StepBattery(float deltaTime)
        {
            BatteryMoodSettings battery = _tuning.Battery;
            bool tired = _battery.Charge01 < battery.LowShare;
            _tired = Smoothing.Damp(_tired, tired ? 1f : 0f, battery.HalfLife, deltaTime);
            _crawl = Smoothing.Damp(_crawl, _battery.IsLowPower ? 1f : 0f, battery.HalfLife, deltaTime);
        }

        /// <summary>
        /// How far 07 joins the player's look at the sky. The camera rig registers it after 07's systems initialise,
        /// so it is picked up on the first frame it exists.
        /// </summary>
        private float SkyLift()
        {
            if (_sky == null)
            {
                _context.TryGet(out _sky);
            }

            return _sky != null ? _sky.SkyLift : 0f;
        }

        private void StepGaze(float sky, float deltaTime)
        {
            Transform frame = _neck.parent;
            Vector2 aim;
            float frequency;
            bool hasExplicitGaze;
            bool scanGaze = false;
            bool cutterGaze = false;

            if (_lookAround.IsActive)
            {
                Vector2 look = _lookAround.Aim;
                float limit = _tuning.NeckYawLimit;
                aim = new Vector2(Mathf.Clamp(look.x, -limit, limit), _tuning.TravelHeadPitch + look.y);
                frequency = _tuning.TargetGazeFrequency;
                hasExplicitGaze = true;
            }
            else if (_rover.Gaze.TryGetTop(out Vector3 target, out int priority, out object owner))
            {
                const int focusGazePriority = 2;
                if (owner is ICutterAimOwner && priority >= focusGazePriority && !_cutting && _cutEnds >= 0f)
                {
                    EndCutFocus();
                }

                Vector3 localFromHead = frame.InverseTransformDirection(target - _head.position);
                ScanLookSettings scan = _tuning.ScanLook;
                scanGaze = priority >= scan.GazePriority;
                // The cut beat's own focus gaze outranks the field's aim request the moment the charge starts; it must
                // keep the body turning until the nozzle is centred, not hand the turn over at the firing cone's edge.
                bool cutterAims = owner is ICutterAimOwner && priority >= focusGazePriority
                    || ReferenceEquals(owner, _cutOwner) && _cutting && _isCutTool;
                cutterGaze = !scanGaze && cutterAims;
                if (scanGaze)
                {
                    aim = ScanAim.LensAngles(localFromHead, _lensOffsetInHead, _tuning.NeckYawLimit,
                        _tuning.HeadPitchUpLimit, _tuning.HeadPitchDownLimit);
                    bool playerSteers = Mathf.Abs(_rover.DriveInput.x) > 0.1f;
                    _scanBodyTurning = !playerSteers && ScanAim.NeedsBodyTurn(localFromHead, _lensOffsetInHead,
                        scan.HeadYawReach, scan.BodyStopYaw, _tuning.HeadPitchUpLimit, _tuning.HeadPitchDownLimit,
                        _scanBodyTurning, out _scanBodyYawDelta);
                    if (_scanBodyTurning)
                    {
                        aim.x = Mathf.Clamp(aim.x, -scan.BodyStopYaw, scan.BodyStopYaw);
                    }
                }
                else
                {
                    aim = Aim(localFromHead);
                    if (cutterGaze)
                    {
                        bool playerSteers = Mathf.Abs(_rover.DriveInput.x) > 0.1f;
                        _scanBodyTurning = !playerSteers && ScanAim.NeedsCutterBodyTurn(
                            _rover.CutterOrigin,
                            target,
                            ScanAim.DefaultCutterStartTurnYawDeg,
                            ScanAim.DefaultCutterStopTurnYawDeg,
                            _scanBodyTurning,
                            out _scanBodyYawDelta);
                    }
                }

                frequency = _tuning.TargetGazeFrequency;
                hasExplicitGaze = true;
            }
            else
            {
                var travel = new Vector2(_rover.SteerInput * _tuning.LookIntoTurn, _tuning.TravelHeadPitch);
                float idle = Mathf.Max(_mood.EarthGaze, _call.HeadWeight);
                Vector3 skyDirection = _callSource != null ? _callSource.Direction : _world.EarthDirection;
                Vector2 earth = Aim(frame.InverseTransformDirection(skyDirection));
                aim = Vector2.Lerp(travel, earth, idle);
                frequency = Mathf.Lerp(_tuning.TravelGazeFrequency, _tuning.IdleGazeFrequency, idle);
                hasExplicitGaze = Mathf.Abs(_rover.SteerInput) > 0.01f || idle > 0.05f || sky > 0.01f;
            }

            if (!scanGaze && !cutterGaze)
            {
                _scanBodyTurning = false;
                _scanBodyYawDelta = 0f;
            }

            bool precisionAim = scanGaze || cutterGaze;
            _scanAimBlend = Smoothing.Damp(_scanAimBlend, precisionAim ? 1f : 0f, _tuning.ScanLook.OpenHalfLife, deltaTime);
            aim.y = Mathf.Lerp(aim.y, _tuning.StargazeHeadPitch, sky * (1f - _scanAimBlend));
            float inspect = ScreenInspectBlend;
            if (inspect > 0f)
            {
                aim = Vector2.Lerp(aim, new Vector2(_screenInspectYawDeg, _screenInspectPitchDeg), inspect);
                frequency = Mathf.Lerp(frequency, Mathf.Max(_tuning.TargetGazeFrequency, 10f), inspect);
                hasExplicitGaze = true;
            }

            _hasActiveGaze = hasExplicitGaze;
            _targetGazeAim = aim;
            _yaw.Step(aim.x, frequency, _tuning.GazeDamping, deltaTime);
            _pitch.Step(aim.y, frequency, _tuning.GazeDamping, deltaTime);
        }

        private Vector2 Aim(Vector3 localDirection)
        {
            return HeadAim.Angles(localDirection, _tuning.NeckYawLimit, _tuning.HeadPitchUpLimit,
                _tuning.HeadPitchDownLimit);
        }

        private void Apply(float sky)
        {
            float inspect = ScreenInspectBlend;
            float opening = _openingPoseBlend;
            float steady = Mathf.Max(inspect, opening);
            float idleScale = (1f - steady) * (1f - _scanAimBlend);

            float breathing = _tuning.IdleHeadBreath * _mood.Idle * (2f * _mood.Breath - 1f);
            BatteryMoodSettings battery = _tuning.Battery;
            float bow = _tired * battery.LowHeadBow + _crawl * battery.CrawlHeadBow;
            CallListenSettings call = _tuning.CallListen;
            float rawPitch = _pitch.Value + (_mood.HeadPitchOffset + breathing - bow - _headNudge.Pitch
                - _chirpPose.HeadDip + _call.Reach * call.ReachHeadPitch - _call.Dejection * call.DejectedHeadPitch) * idleScale;
            float targetPitch = opening >= inspect ? _openingPitchDeg : _screenInspectPitchDeg;
            float targetYaw = opening >= inspect ? _openingYawDeg : _screenInspectYawDeg;
            float scanSweepWeight = (1f - steady) * _scanAimBlend;
            float pitch = Mathf.Lerp(rawPitch, targetPitch, steady) + _scanLook.HeadSweepPitch * scanSweepWeight;
            float yaw = Mathf.Lerp(_yaw.Value, targetYaw, steady) + _scanLook.HeadSweepYaw * scanSweepWeight;

            _neck.localRotation = _neckRest * Quaternion.AngleAxis(yaw, Vector3.up);
            if (_neckLift != null)
            {
                float neckDroop = EffectiveNeckLift((_tuning.NeckSleepDroop * _mood.Wake.Sleep + _tuning.NeckTiredDroop * _tired
                    + _tuning.NeckCrawlExtend * _crawl + _tuning.NeckCargoExtend * _cargoSag
                    - _tuning.NeckAlertLift * Mathf.Clamp01(_mood.Perk) - _call.Reach * call.ReachNeckLift + _call.Dejection * call.DejectedNeckDroop) * idleScale, yaw);
                _neckLift.localRotation = _neckLiftRest * Quaternion.AngleAxis(neckDroop, Vector3.right);
            }

            Quaternion pitchedHeadLocal = _headRest * Quaternion.AngleAxis(-pitch, Vector3.right);
            float inspectRoll = 0f;
            if ((inspect > 0f || _openingLevels) && _head.parent != null)
            {
                Quaternion pitchedHeadWorld = _head.parent.rotation * pitchedHeadLocal;
                inspectRoll = ComputeInspectHeadCounterRoll(
                    pitchedHeadWorld,
                    _screenInspectCounterRollShare,
                    _screenInspectCounterRollMaxDeg);
            }

            float idleRoll = _wakeTilt + _chirpPose.HeadRoll;
            float baseRoll = Mathf.Lerp(idleRoll, inspectRoll, inspect);
            float roll = opening > 0f ? Mathf.Lerp(baseRoll, _openingLevels ? inspectRoll : 0f, opening) : baseRoll;
            _head.localRotation = pitchedHeadLocal * Quaternion.AngleAxis(roll, Vector3.forward);
            if (_cutterTurret != null)
            {
                _cutterTurret.localRotation = _cutterTurretRest * Quaternion.AngleAxis(_cutterYaw, Vector3.up);
            }

            if (_cutterHead != null)
            {
                _cutterHead.localRotation = _cutterHeadRest * Quaternion.AngleAxis(_cutterPitch, Vector3.right);
            }

            if (_scoopArms != null)
            {
                _scoopArms.localRotation = _scoopArmsRest
                    * Quaternion.AngleAxis(25f * _digFocus - 12f * _scoopKick, Vector3.right);
            }

            if (_scoopBucket != null)
            {
                _scoopBucket.localRotation = _scoopBucketRest
                    * Quaternion.AngleAxis(35f * _digFocus + 20f * _scoopKick, Vector3.right);
            }

            if (_winchSpool != null)
            {
                _winchSpool.localRotation = _winchSpoolRest * Quaternion.AngleAxis(_winchAngle + _towSpool, Vector3.right);
            }

            float wingOpen = Mathf.Max(_mood.WingOpen * Mathf.Lerp(1f, battery.LowWingOpen, _tired),
                sky * _tuning.StargazeWingOpen);
            if (_postWing >= 0f)
            {
                wingOpen = _postWing;
            }

            float wingAngle = Mathf.Lerp(_tuning.WingClosedAngle, _tuning.WingOpenAngle, wingOpen)
                + _sigh * _tuning.Chirp.SighWing;
            _solarWing.localRotation = _wingRest * Quaternion.AngleAxis(wingAngle, _tuning.WingHingeAxis);

            float baseEyeGlow = _mood.EyeGlow * Mathf.Lerp(1f, _tuning.StargazeEyeGlow, sky)
                * Mathf.Lerp(1f, battery.LowEyeGlow, _tired) * Mathf.Lerp(1f, battery.CrawlEyeGlow, _crawl)
                * Mathf.Lerp(1f, _tuning.CutFocus.EyeGlow, _cutFocus) * _chirpPose.EyeGlow
                * (1f + _sigh * _tuning.Chirp.SighEye) * (1f + PostEyeGlow * _postEye);
            float eyeGlow = baseEyeGlow * _scanLook.EyeGlow;
            float scanOpen = _scanLook.Open;
            float inspectLightDim = 1f - _screenInspectBlend * 0.92f;
            if (scanOpen > ScanEyeOpenThreshold)
            {
                Color fanColor = _tuning.ScanLook.FanColour;
                Color restEyeColor = _eyeLightRestColor * eyeGlow;
                Color scanEyeColor = fanColor * (eyeGlow * ScanEyeEmissionBoost);
                SetGlow(_eyeRenderer, Color.Lerp(restEyeColor, scanEyeColor, scanOpen));
            }
            else
            {
                SetGlow(_eyeRenderer, eyeGlow);
            }

            _eyeLight.color = _eyeLightRestColor;
            _eyeLight.range = _tuning.EyeLightRange;
            _eyeLight.intensity = _tuning.EyeLightIntensity * baseEyeGlow * inspectLightDim * _lampPower;
            _eyeLight.transform.localPosition = _eyeLightRestLocalPos;

            _scanFan.transform.localRotation = _scanFanRest * Quaternion.AngleAxis(_scanLook.Sweep, Vector3.up);
            if (_scanFan.enabled)
            {
                _scanFan.enabled = false;
            }
            SetGlow(_antennaTipRenderer, _listen.TipEmission(_mood.TipGlow + _call.LampGlow));
        }

        /// <summary>
        /// The cut through 07's head (docs/design/feel-pass.md §3.1): it turns to the cut point with a narrowed,
        /// brighter lens, nods at ignition, follows the piece as it tumbles, and lets go once the bundle or part is
        /// received, the follow window expires, or the player releases. The dig is answered the same way.
        /// </summary>
        /// <summary>A press the offline cutter refused: its turret shakes, then stays parked.</summary>
        private void OnCutterRefused(CutterRefused refused)
        {
            _cutterRefusal.Trigger();
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            CutFocusSettings focus = _tuning.CutFocus;
            if (beat.Kind == FeelKind.Dock && beat.Phase == FeelPhase.Reward)
            {
                _sigh = 1f;
                return;
            }

            if (beat.Kind == FeelKind.Build && beat.Phase == FeelPhase.Impact)
            {
                PerkUp(_tuning.KitProudPerk);
                Glance(beat.Position);
                return;
            }

            if (beat.Kind == FeelKind.Collect
                && (beat.Phase == FeelPhase.Reward || beat.Phase == FeelPhase.Impact)
                && !_cutting)
            {
                EndCutFocus();
                return;
            }

            if (beat.Kind == FeelKind.Cargo && beat.Phase == FeelPhase.Impact && !_cutting)
            {
                EndCutFocus();
                return;
            }

            if (beat.Kind == FeelKind.Scan)
            {
                OnScanBeat(beat);
                return;
            }

            if (beat.Kind == FeelKind.Tether)
            {
                _winchReel = 1f;
                if (beat.Phase == FeelPhase.Anticipate)
                {
                    _tetherActive = true;
                    _tetherStrain = 0f;
                }
                else if (beat.Phase == FeelPhase.Act)
                {
                    _tetherActive = true;
                    _tetherStrain = Mathf.Clamp01(beat.Strength);
                }
                else if (beat.Phase == FeelPhase.Cancel || beat.Phase == FeelPhase.Reward)
                {
                    _tetherActive = false;
                    _tetherStrain = 0f;
                }

                return;
            }

            if (beat.Kind != FeelKind.Cut && beat.Kind != FeelKind.Dig)
            {
                return;
            }

            bool isDig = beat.Kind == FeelKind.Dig;
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _cutting = true;
                    _isCutTool = !isDig;
                    _cutTarget = beat.Position;
                    _digging = isDig;
                    _cutEnds = -1f;
                    _rover.Gaze.Set(_cutOwner, beat.Position, focus.GazePriority);
                    break;
                case FeelPhase.Act:
                    if (beat.Strength <= 0f)
                    {
                        _headNudge.KickPitch(focus.Nod);
                    }

                    _isCutTool = !isDig;
                    _cutTarget = beat.Position;
                    _digging = isDig;
                    _rover.Gaze.Set(_cutOwner, beat.Position, focus.GazePriority);
                    break;
                case FeelPhase.Impact:
                    _cutting = false;
                    _digging = false;
                    if (isDig)
                    {
                        _scoopKick = 1f;
                    }

                    _cutEnds = Time.time + focus.FollowSeconds;
                    break;
                case FeelPhase.Follow:
                    if (!_cutting)
                    {
                        _rover.Gaze.Set(_cutOwner, beat.Position, focus.GazePriority);
                        _cutEnds = Time.time + focus.FollowSeconds;
                    }

                    break;
                case FeelPhase.Cancel:
                    EndCutFocus();
                    break;
            }
        }

        /// <summary>
        /// Each syllable of 07's chirp pulses its eye and flicks its antenna; Happy bounces, and the phrase's mood
        /// poses the head (<see cref="ChirpPose"/>).
        /// </summary>
        private void OnChirpSyllable(RoverChirpSyllable syllable)
        {
            ChirpSettings chirp = _tuning.Chirp;
            _chirpPose.Syllable(syllable.Mood, syllable.Index, syllable.Count, syllable.Seconds);
            _rig.KickAntenna(chirp.AntennaFlick);
            if (syllable.Mood == ChirpMood.Happy)
            {
                _rig.KickHeave(chirp.HappyBounce);
            }
        }

        /// <summary>The hold has just filled: 07 glances back once at its load (M3-25).</summary>
        private void OnCargoChanged(CargoChanged cargo)
        {
            _cargoTarget = cargo.SlotCount > 0 ? Mathf.Clamp01((float)cargo.UsedSlots / cargo.SlotCount) : 0f;
            if (_cargo.BecameFull(cargo.UsedSlots, cargo.SlotCount))
            {
                Glance(_rover.CargoSocket.position);
            }
        }

        /// <summary>
        /// A scan through 07's eye (0.5.5 §1.2, <see cref="ScanLook"/>): it looks at the fragment's ScanTarget with
        /// its lid fully open and the fan sweeping, pulses at each quarter, settles on letting go, and flares once as
        /// the lines stream in, still looking until the dissolve is done.
        /// </summary>
        private void OnScanBeat(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _scanLook.Begin(beat.Strength, beat.Seconds);
                    _scanGazing = true;
                    _rover.Gaze.Set(_scanOwner, beat.Position, _tuning.ScanLook.GazePriority);
                    break;
                case FeelPhase.Act:
                    _scanLook.Quarter(beat.Strength);
                    break;
                case FeelPhase.Cancel:
                    _scanLook.LetGo();
                    _scanGazing = false;
                    _rover.Gaze.Clear(_scanOwner);
                    break;
                case FeelPhase.Reward:
                    _scanLook.Learned();
                    break;
            }
        }

        private void EndCutFocus()
        {
            _cutting = false;
            _digging = false;
            _cutEnds = -1f;
            _rover.Gaze.Clear(_cutOwner);
        }

        private void SetGlow(Renderer target, float intensity)
        {
            _glowBlock.Clear();
            _glowBlock.SetFloat(EmissionStrengthId, Mathf.Clamp01(intensity));
            target.SetPropertyBlock(_glowBlock);
        }

        private void SetGlow(Renderer target, Color emission)
        {
            _glowBlock.Clear();
            float peak = Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b));
            _glowBlock.SetFloat(EmissionStrengthId, Mathf.Clamp01(peak));
            _glowBlock.SetVector(EmissionColorId, emission);
            target.SetPropertyBlock(_glowBlock);
        }

        private void OnDestroy()
        {
            if (_subscriptions == null)
            {
                return;
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _voice?.Dispose();
            _saveToken?.Dispose();
        }
    }
}
