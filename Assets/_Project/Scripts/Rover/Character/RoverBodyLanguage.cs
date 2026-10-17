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

        /// <summary>A fan this faint is off: its dark rays never show at rest (nor in the portrait).</summary>
        private const float FanDark = 1e-3f;

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
        private float _cutterYaw;
        private float _cutterPitch;
        private bool _digging;
        private float _digFocus;
        private float _scoopKick;
        private bool _tetherActive;
        private float _winchReel;
        private float _winchAngle;
        private float _cargoTarget;
        private float _cargoSag;
        private float _cutEnds = -1f;
        private RoverVoice _voice;
        private ChirpPose _chirpPose;
        private readonly CargoLoad _cargo = new CargoLoad();
        private float _sigh;

        private RoverMood _mood;
        private LookAround _lookAround;
        private AntennaListen _listen;
        private IWorldLayout _world;
        private GameContext _context;
        private ISkyGaze _sky;
        private EventBus _events;
        private IDisposable[] _subscriptions;
        private float _glanceEnds = -1f;
        private MaterialPropertyBlock _glowBlock;
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
        private bool _proudPending;
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
        private bool _initialized;

        private const float ScreenInspectBlendSeconds = 0.30f;
        private const float ScreenInspectYawLimit = 35f;

        /// <summary>Read-only view of the mood, for audio/debug tooling.</summary>
        public RoverMood Mood => _mood;

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

        /// <summary>CRT barrel curvature parameter read from the screen's shared material.</summary>
        public float Curvature => _faceDirector != null ? _faceDirector.Curvature : 0.10f;

        /// <summary>Transform of 07's CRT <c>Screen</c> mesh.</summary>
        public Transform ScreenTransform =>
            _faceDirector != null && _faceDirector.Display != null && _faceDirector.Display.ScreenRenderer != null
                ? _faceDirector.Display.ScreenRenderer.transform
                : _head;

        /// <summary>
        /// Enters or exits screen-inspect mode: lifts 07's head by <paramref name="targetPitchDeg"/> (+10 deg), turns
        /// the neck toward <paramref name="targetYawDeg"/> (clamped to +/-35 deg), suppresses idle head noise so the
        /// CRT screen is rock-steady for clicking, and switches <see cref="FaceDirector"/> into screen content mode.
        /// </summary>
        public void SetScreenInspect(
            bool open,
            float targetYawDeg = 0f,
            float targetPitchDeg = 10f,
            Texture contentTexture = null)
        {
            _screenInspectOpen = open;
            if (open)
            {
                _screenInspectYawDeg = Mathf.Clamp(targetYawDeg, -ScreenInspectYawLimit, ScreenInspectYawLimit);
                _screenInspectPitchDeg = targetPitchDeg;
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
            Quaternion headWorldRot = headParentRot * (headRest * Quaternion.AngleAxis(-headPitchDeg, Vector3.right));

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

            EventBus events = context.Events;
            _chirpPose = new ChirpPose(_tuning.Chirp);
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
                events.Subscribe<RoverJumped>(OnJumped),
                events.Subscribe<RoverWideShotChanged>(OnWideShotChanged),
                events.Subscribe<RelayRestored>(OnRelayRestored),
                events.Subscribe<BellSignalPicked>(OnBellSignalPicked),
                events.Subscribe<RadioHopFinished>(OnRadioHopFinished),
                events.Subscribe<ChargingChanged>(OnChargingChanged),
                events.Subscribe<BatteryWarned>(OnBatteryWarned),
                events.Subscribe<RoverLeapRefused>(OnLeapRefused),
                events.Subscribe<FeelBeat>(OnFeelBeat),
                events.Subscribe<RoverChirpSyllable>(OnChirpSyllable),
                events.Subscribe<BasePowerChanged>(OnBasePowerChanged),
                events.Subscribe<FriendSpotted>(spotted => Glance(spotted.Position)),
                events.Subscribe<CargoChanged>(OnCargoChanged),
                events.Subscribe<SonarPinged>(OnListened),
            };
            _initialized = true;
            _faceDirector.Initialize(context);
            Apply(0f);
        }

        /// <summary>
        /// The title is up (docs/features/M3-09): 07 stays asleep, neither the settle delay nor the player's input
        /// wakes it, until the opening or the short wake begins.
        /// </summary>
        public void HoldAsleep()
        {
            _mood.Wake.Hold();
        }

        /// <summary>No title in this game (fixtures, test runs): 07 wakes on its own, as it always did.</summary>
        public void WakeOnItsOwn()
        {
            _mood.Wake.Release();
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

        /// <summary>The wake is done: the face and the head are 07's own again.</summary>
        public void FinishWake()
        {
            _mood.Wake.FinishScripted();
            _mood.SetWakeLid(0f, 0f);
            _wakeTilt = 0f;
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

        private void OnJumped(RoverJumped jumped)
        {
            PerkUp(Mathf.Lerp(_tuning.HopPerk, _tuning.LeapPerk, jumped.Strength));
        }

        private void OnRecovering(RoverRecovering recovering)
        {
            PerkUp(_tuning.RecoveryPerk);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _tetherActive = true;
            _winchReel = 1f;
        }

        private void OnTetherReleased(TetherReleased release)
        {
            _tetherActive = false;
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

        /// <summary>
        /// A part seated on 07 (Install Impact): a gift's soft settle (Strength under 1) perks it up now; a fitted part
        /// strikes the proud pose once the bay lets 07 go, turned to show the piece.
        /// </summary>
        private void OnSeated(FeelBeat beat)
        {
            if (beat.Strength < 1f)
            {
                PerkUp(_tuning.GiftPerk);
            }
            else
            {
                _proudPending = true;
            }
        }

        /// <summary>A charger started filling the cells: 07 perks up (head lift, brighter eye, antenna wiggle).</summary>
        private void OnChargingChanged(ChargingChanged charging)
        {
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

        /// <summary>The coils fizzled at the lip: 07 glances at its flank lamps, "not enough".</summary>
        private void OnLeapRefused(RoverLeapRefused refused)
        {
            Glance(_rover.FlankLamps.GlancePoint, _tuning.ReactionGazePriority, _tuning.Battery.LampGlanceSeconds);
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

            _mood.SetEffort(_rover.JumpCharge);
            _mood.SetWingMended(_rover.Kit.HasMendedWing);
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

            if (_proudPending && !_rover.Kit.IsFitting)
            {
                _proudPending = false;
                PerkUp(_tuning.KitProudPerk);
                _events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Reward, _rover.Position, Vector3.up, 1f,
                    0f));
            }

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
            _rig.SetAntennaLift(_listen.Lift);
            _lookAround.Step(deltaTime);
            float sky = SkyLift();
            StepGaze(sky, deltaTime);
            Apply(sky);
            _faceDirector.Step(deltaTime, _hasActiveGaze, _targetGazeAim.x, _targetGazeAim.y, _yaw.Value, _pitch.Value);
        }

        private void StepTools(float halfLife, float deltaTime)
        {
            float targetCutterYaw = 0f;
            float targetCutterPitch = 0f;
            if (_cutting && _isCutTool && _cutterTurret != null && _cutterHead != null)
            {
                Transform frame = _cutterTurret.parent != null ? _cutterTurret.parent : _cutterTurret;
                Vector3 localDir = frame.InverseTransformDirection(_cutTarget - _cutterHead.position);
                float horiz = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
                if (horiz > 1e-4f || Mathf.Abs(localDir.y) > 1e-4f)
                {
                    targetCutterYaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -55f, 55f);
                    targetCutterPitch = Mathf.Clamp(-Mathf.Atan2(localDir.y, horiz) * Mathf.Rad2Deg, -35f, 35f);
                }
            }

            _cutterYaw = Smoothing.Damp(_cutterYaw, targetCutterYaw, halfLife, deltaTime);
            _cutterPitch = Smoothing.Damp(_cutterPitch, targetCutterPitch, halfLife, deltaTime);
            _digFocus = Smoothing.Damp(_digFocus, _digging ? 1f : 0f, halfLife, deltaTime);
            _scoopKick = Smoothing.Damp(_scoopKick, 0f, halfLife, deltaTime);
            _winchReel = Smoothing.Damp(_winchReel, _tetherActive ? 1f : 0f, 0.15f, deltaTime);
            _winchAngle = Mathf.Repeat(_winchAngle + _winchReel * 360f * deltaTime, 360f);
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

            if (_lookAround.IsActive)
            {
                Vector2 look = _lookAround.Aim;
                float limit = _tuning.NeckYawLimit;
                aim = new Vector2(Mathf.Clamp(look.x, -limit, limit), _tuning.TravelHeadPitch + look.y);
                frequency = _tuning.TargetGazeFrequency;
                hasExplicitGaze = true;
            }
            else if (_rover.Gaze.TryGetTop(out Vector3 target, out int priority))
            {
                Vector3 localFromHead = frame.InverseTransformDirection(target - _head.position);
                ScanLookSettings scan = _tuning.ScanLook;
                scanGaze = priority >= scan.GazePriority;
                if (scanGaze)
                {
                    aim = ScanAim.LensAngles(localFromHead, _lensOffsetInHead, _tuning.NeckYawLimit,
                        _tuning.HeadPitchUpLimit, _tuning.HeadPitchDownLimit);
                    bool playerSteers = Mathf.Abs(_rover.DriveInput.x) > 0.1f;
                    _scanBodyTurning = !playerSteers && ScanAim.NeedsBodyTurn(localFromHead, _lensOffsetInHead,
                        scan.HeadYawReach, scan.BodyStopYaw, _tuning.HeadPitchUpLimit, _tuning.HeadPitchDownLimit,
                        _scanBodyTurning, out _scanBodyYawDelta);
                }
                else
                {
                    aim = Aim(localFromHead);
                }

                frequency = _tuning.TargetGazeFrequency;
                hasExplicitGaze = true;
            }
            else
            {
                var travel = new Vector2(_rover.SteerInput * _tuning.LookIntoTurn, _tuning.TravelHeadPitch);
                float idle = _mood.EarthGaze;
                Vector2 earth = Aim(frame.InverseTransformDirection(_world.EarthDirection));
                aim = Vector2.Lerp(travel, earth, idle);
                frequency = Mathf.Lerp(_tuning.TravelGazeFrequency, _tuning.IdleGazeFrequency, idle);
                hasExplicitGaze = Mathf.Abs(_rover.SteerInput) > 0.01f || idle > 0.05f || sky > 0.01f;
            }

            if (!scanGaze)
            {
                _scanBodyTurning = false;
                _scanBodyYawDelta = 0f;
            }

            _scanAimBlend = Smoothing.Damp(_scanAimBlend, scanGaze ? 1f : 0f, _tuning.ScanLook.OpenHalfLife, deltaTime);
            aim.y = Mathf.Lerp(aim.y, _tuning.StargazeHeadPitch, sky * (1f - _scanAimBlend));
            float earthTurn = _sky != null ? _sky.EarthTurn * (1f - _scanAimBlend) : 0f;
            if (earthTurn > 0f)
            {
                aim.x = Mathf.Lerp(aim.x, Aim(frame.InverseTransformDirection(_world.EarthDirection)).x, earthTurn);
                hasExplicitGaze = true;
            }

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
            float idleScale = (1f - inspect) * (1f - _scanAimBlend);

            float breathing = _tuning.IdleHeadBreath * _mood.Idle * (2f * _mood.Breath - 1f);
            BatteryMoodSettings battery = _tuning.Battery;
            float bow = _tired * battery.LowHeadBow + _crawl * battery.CrawlHeadBow;
            float rawPitch = _pitch.Value + (_mood.HeadPitchOffset + breathing - bow - _headNudge.Pitch
                - _chirpPose.HeadDip) * idleScale;
            float pitch = Mathf.Lerp(rawPitch, _screenInspectPitchDeg, inspect);
            float yaw = Mathf.Lerp(_yaw.Value, _screenInspectYawDeg, inspect);

            _neck.localRotation = _neckRest * Quaternion.AngleAxis(yaw, Vector3.up);
            if (_neckLift != null)
            {
                float neckDroop = (_tuning.NeckSleepDroop * _mood.Wake.Sleep + _tuning.NeckTiredDroop * _tired
                    + _tuning.NeckCrawlExtend * _crawl + _tuning.NeckCargoExtend * _cargoSag
                    - _tuning.NeckAlertLift * Mathf.Clamp01(_mood.Perk)) * idleScale;
                _neckLift.localRotation = _neckLiftRest * Quaternion.AngleAxis(neckDroop, Vector3.right);
            }

            float roll = (_wakeTilt + _chirpPose.HeadRoll) * idleScale;
            _head.localRotation = _headRest * Quaternion.AngleAxis(-pitch, Vector3.right)
                * Quaternion.AngleAxis(roll, Vector3.forward);
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
                _winchSpool.localRotation = _winchSpoolRest * Quaternion.AngleAxis(_winchAngle, Vector3.right);
            }

            float wingOpen = Mathf.Max(_mood.WingOpen * Mathf.Lerp(1f, battery.LowWingOpen, _tired),
                sky * _tuning.StargazeWingOpen);
            float wingAngle = Mathf.Lerp(_tuning.WingClosedAngle, _tuning.WingOpenAngle, wingOpen)
                + _sigh * _tuning.Chirp.SighWing;
            _solarWing.localRotation = _wingRest * Quaternion.AngleAxis(wingAngle, _tuning.WingHingeAxis);

            float eyeGlow = _mood.EyeGlow * Mathf.Lerp(1f, _tuning.StargazeEyeGlow, sky)
                * Mathf.Lerp(1f, battery.LowEyeGlow, _tired) * Mathf.Lerp(1f, battery.CrawlEyeGlow, _crawl)
                * Mathf.Lerp(1f, _tuning.CutFocus.EyeGlow, _cutFocus) * _chirpPose.EyeGlow
                * (1f + _sigh * _tuning.Chirp.SighEye) * _scanLook.EyeGlow;
            SetGlow(_eyeRenderer, eyeGlow);
            ScanLookSettings scan = _tuning.ScanLook;
            _scanFan.transform.localRotation = _scanFanRest * Quaternion.AngleAxis(_scanLook.Sweep, Vector3.up);
            bool fanLit = _scanLook.Fan > FanDark;
            if (_scanFan.enabled != fanLit)
            {
                _scanFan.enabled = fanLit;
            }

            if (fanLit)
            {
                SetGlow(_scanFan, scan.FanColour.linear * (_scanLook.Fan * scan.FanGlow));
            }
            SetGlow(_antennaTipRenderer, _listen.TipEmission(_mood.TipGlow));
            _eyeLight.intensity = _tuning.EyeLightIntensity * eyeGlow;
        }

        /// <summary>
        /// The cut through 07's head (docs/design/feel-pass.md §3.1): it turns to the cut point with a narrowed,
        /// brighter lens, nods at ignition, follows the piece as it tumbles, and lets go once the bundle or part is
        /// received, the follow window expires, or the player releases. The dig is answered the same way.
        /// </summary>
        private void OnFeelBeat(FeelBeat beat)
        {
            CutFocusSettings focus = _tuning.CutFocus;
            if (beat.Kind == FeelKind.Dock && beat.Phase == FeelPhase.Reward)
            {
                _sigh = 1f;
                return;
            }

            if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Impact)
            {
                OnSeated(beat);
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
                if (beat.Phase == FeelPhase.Anticipate || beat.Phase == FeelPhase.Act)
                {
                    _tetherActive = true;
                }
                else if (beat.Phase == FeelPhase.Cancel || beat.Phase == FeelPhase.Reward)
                {
                    _tetherActive = false;
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
                    _scanLook.Begin();
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
