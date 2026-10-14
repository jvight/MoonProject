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
    /// long look up at its lamp and a perk-up; a new signal pillar of Bell's within range gets a glance; landing from
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

        [Tooltip("Character tuning (Assets/_Project/Data/Tuning/RoverCharacterTuning.asset).")]
        [SerializeField] private RoverCharacterTuning _tuning;

        [Tooltip("The rover this body language belongs to.")]
        [SerializeField] private RoverController _rover;

        [Tooltip("The rover's visual rig (antenna wiggle, chassis squash).")]
        [SerializeField] private RoverVisualRig _rig;

        [Tooltip("RoverModel 'Neck' (yaw = local Y).")]
        [SerializeField] private Transform _neck;

        [Tooltip("RoverModel 'Head' (pitch = local X).")]
        [SerializeField] private Transform _head;

        [Tooltip("RoverModel 'Eyelid' (local X: 0 = open, + = closing).")]
        [SerializeField] private Transform _eyelid;

        [Tooltip("RoverModel 'SolarWing' (local X: 0 = folded, + = opening).")]
        [SerializeField] private Transform _solarWing;

        [Tooltip("MeshRenderer of RoverModel 'Eye' (glow modulated through _EmissionColor).")]
        [SerializeField] private Renderer _eyeRenderer;

        [Tooltip("MeshRenderer of RoverModel 'AntennaTip' (glow modulated through _EmissionColor).")]
        [SerializeField] private Renderer _antennaTipRenderer;

        [Tooltip("Small warm point light inside the eye; breathes with the glow.")]
        [SerializeField] private Light _eyeLight;

        [Tooltip("Art's ScanFan on Eye/ScanFanSocket: the scan's sweeping fan of light, dark until a scan.")]
        [SerializeField] private Renderer _scanFan;

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
        private Quaternion _neckRest;
        private Quaternion _headRest;
        private Quaternion _eyelidRest;
        private Quaternion _wingRest;
        private bool _proudPending;
        private float _wakeTilt;
        private BatteryLink _battery;
        private float _tired;
        private float _crawl;
        private bool _initialized;

        /// <summary>Read-only view of the mood, for audio/debug tooling.</summary>
        public RoverMood Mood => _mood;

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
            _headRest = _head.localRotation;
            _eyelidRest = _eyelid.localRotation;
            _wingRest = _solarWing.localRotation;
            _scanFanRest = _scanFan.transform.localRotation;
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
                & Require(_neck != null && _head != null, "Neck and Head nodes are not assigned.")
                & Require(_eyelid != null && _solarWing != null, "Eyelid and SolarWing nodes are not assigned.")
                & Require(_eyeRenderer != null && _antennaTipRenderer != null, "Eye/AntennaTip renderers are missing.")
                & Require(_eyeLight != null, "Eye light is not assigned.")
                & Require(_scanFan != null, "The ScanFan on Eye/ScanFanSocket is not assigned (0.5.5 scan contract).");
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

        private void OnRelicAnswered(RelicAnswered answer)
        {
            Glance(answer.Position);
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

        private void OnTetherReleased(TetherReleased release)
        {
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

        /// <summary>Bell pointed at something: a glance toward the new pillar of light if it is within range.</summary>
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

        /// <summary>07 listens: the antenna lifts and its tip blinks.</summary>
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

            StepElder();

            _mood.SetEffort(_rover.JumpCharge);
            _mood.SetWingMended(_rover.Kit.HasMendedWing);
            switch (_mood.Step(_rover.Speed, _rover.DriveInput.magnitude, deltaTime))
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
            _cutFocus = Smoothing.Damp(_cutFocus, _cutting ? 1f : 0f, _tuning.CutFocus.HalfLife, deltaTime);
            _scanLook.Step(deltaTime);
            if (_scanGazing && !_scanLook.HoldsGaze)
            {
                _scanGazing = false;
                _rover.Gaze.Clear(_scanOwner);
            }
            _chirpPose.Step(deltaTime);
            _sigh = Smoothing.Damp(_sigh, 0f, _tuning.Chirp.SighHalfLife, deltaTime);
            _headNudge.Step(deltaTime);
            _listen.Step(deltaTime);
            _rig.SetAntennaLift(_listen.Lift);
            _lookAround.Step(deltaTime);
            float sky = SkyLift();
            StepGaze(sky, deltaTime);
            Apply(sky);
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

            if (_lookAround.IsActive)
            {
                Vector2 look = _lookAround.Aim;
                float limit = _tuning.NeckYawLimit;
                aim = new Vector2(Mathf.Clamp(look.x, -limit, limit), _tuning.TravelHeadPitch + look.y);
                frequency = _tuning.TargetGazeFrequency;
            }
            else if (_rover.Gaze.TryGetTop(out Vector3 target))
            {
                aim = Aim(frame.InverseTransformDirection(target - _head.position));
                frequency = _tuning.TargetGazeFrequency;
            }
            else
            {
                var travel = new Vector2(_rover.SteerInput * _tuning.LookIntoTurn, _tuning.TravelHeadPitch);
                float idle = _mood.EarthGaze;
                Vector2 earth = Aim(frame.InverseTransformDirection(_world.EarthDirection));
                aim = Vector2.Lerp(travel, earth, idle);
                frequency = Mathf.Lerp(_tuning.TravelGazeFrequency, _tuning.IdleGazeFrequency, idle);
            }

            aim.y = Mathf.Lerp(aim.y, _tuning.StargazeHeadPitch, sky);
            float earthTurn = _sky != null ? _sky.EarthTurn : 0f;
            if (earthTurn > 0f)
            {
                aim.x = Mathf.Lerp(aim.x, Aim(frame.InverseTransformDirection(_world.EarthDirection)).x, earthTurn);
            }
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
            float breathing = _tuning.IdleHeadBreath * _mood.Idle * (2f * _mood.Breath - 1f);
            BatteryMoodSettings battery = _tuning.Battery;
            float bow = _tired * battery.LowHeadBow + _crawl * battery.CrawlHeadBow;
            float pitch = _pitch.Value + _mood.HeadPitchOffset + breathing - bow - _headNudge.Pitch
                - _chirpPose.HeadDip;

            _neck.localRotation = _neckRest * Quaternion.AngleAxis(_yaw.Value, Vector3.up);
            _head.localRotation = _headRest * Quaternion.AngleAxis(-pitch, Vector3.right)
                * Quaternion.AngleAxis(_wakeTilt + _chirpPose.HeadRoll, Vector3.forward);
            _eyelid.localRotation = _eyelidRest
                * Quaternion.AngleAxis(Mathf.Max(_mood.LidClosure, _cutFocus * _tuning.CutFocus.IrisNarrow)
                    * (1f - _scanLook.Open) * _tuning.EyelidClosedAngle, Vector3.right);
            float wingOpen = Mathf.Max(_mood.WingOpen * Mathf.Lerp(1f, battery.LowWingOpen, _tired),
                sky * _tuning.StargazeWingOpen);
            _solarWing.localRotation = _wingRest
                * Quaternion.AngleAxis(wingOpen * _tuning.WingOpenAngle + _sigh * _tuning.Chirp.SighWing,
                    Vector3.right);

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

            if (beat.Kind != FeelKind.Cut && beat.Kind != FeelKind.Dig)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _cutting = true;
                    _cutEnds = -1f;
                    _rover.Gaze.Set(_cutOwner, beat.Position, focus.GazePriority);
                    break;
                case FeelPhase.Act:
                    if (beat.Strength <= 0f)
                    {
                        _headNudge.KickPitch(focus.Nod);
                    }

                    _rover.Gaze.Set(_cutOwner, beat.Position, focus.GazePriority);
                    break;
                case FeelPhase.Impact:
                    _cutting = false;
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
                    break;
                case FeelPhase.Reward:
                    _scanLook.Learned();
                    break;
            }
        }

        private void EndCutFocus()
        {
            _cutting = false;
            _cutEnds = -1f;
            _rover.Gaze.Clear(_cutOwner);
        }

        private void SetGlow(Renderer target, float intensity)
        {
            SetGlow(target, new Color(intensity, intensity, intensity, 1f));
        }

        private void SetGlow(Renderer target, Color emission)
        {
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
