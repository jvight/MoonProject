using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

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
    /// (<see cref="RoverKitFitted"/>) and lets 07 go, turned to show it, 07 strikes a proud pose (head up, eye bright,
    /// antenna wiggle); a friend's gift gets a softer one as it settles; with Tilly's cell in its wing the wing settles
    /// open wider.
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

        /// <summary>Owner of 07's own glances in the shared gaze requests.</summary>
        private readonly object _glanceOwner = new object();

        private RoverMood _mood;
        private LookAround _lookAround;
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
            _glowBlock = new MaterialPropertyBlock();
            _neckRest = _neck.localRotation;
            _headRest = _head.localRotation;
            _eyelidRest = _eyelid.localRotation;
            _wingRest = _solarWing.localRotation;
            _yaw.Reset(0f);
            _pitch.Reset(_tuning.TravelHeadPitch);
            _eyeLight.type = LightType.Point;
            _eyeLight.range = _tuning.EyeLightRange;
            _eyeLight.shadows = LightShadows.None;

            EventBus events = context.Events;
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
                events.Subscribe<RoverKitFitted>(OnKitFitted),
            };
            _initialized = true;
            Apply(0f);
        }

        private bool ValidateWiring()
        {
            return Require(_tuning != null, "RoverCharacterTuning is not assigned.")
                & Require(_rover != null && _rig != null, "RoverController and RoverVisualRig must be assigned.")
                & Require(_neck != null && _head != null, "Neck and Head nodes are not assigned.")
                & Require(_eyelid != null && _solarWing != null, "Eyelid and SolarWing nodes are not assigned.")
                & Require(_eyeRenderer != null && _antennaTipRenderer != null, "Eye/AntennaTip renderers are missing.")
                & Require(_eyeLight != null, "Eye light is not assigned.");
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
        /// A gift settled onto 07: a soft perk-up now. A kit piece fitted by the bay: the proud pose once the bay
        /// lets 07 go, turned to show the piece.
        /// </summary>
        private void OnKitFitted(RoverKitFitted fitted)
        {
            if (fitted.Gift)
            {
                PerkUp(_tuning.GiftPerk);
            }
            else
            {
                _proudPending = true;
            }
        }

        /// <summary>Landed from a radio-hop: 07 rouses from any daydream and looks around, "where am I?".</summary>
        private void OnRadioHopFinished(RadioHopFinished hop)
        {
            _mood.Rouse();
            _lookAround.Start();
            PerkUp(_tuning.HopArrivalPerk);
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

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

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
            }

            if (_glanceEnds >= 0f && Time.time >= _glanceEnds)
            {
                _rover.Gaze.Clear(_glanceOwner);
                _glanceEnds = -1f;
            }

            _lookAround.Step(deltaTime);
            float sky = SkyLift();
            StepGaze(sky, deltaTime);
            Apply(sky);
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
            float pitch = _pitch.Value + _mood.HeadPitchOffset + breathing;

            _neck.localRotation = _neckRest * Quaternion.AngleAxis(_yaw.Value, Vector3.up);
            _head.localRotation = _headRest * Quaternion.AngleAxis(-pitch, Vector3.right);
            _eyelid.localRotation = _eyelidRest
                * Quaternion.AngleAxis(_mood.LidClosure * _tuning.EyelidClosedAngle, Vector3.right);
            float wingOpen = Mathf.Max(_mood.WingOpen, sky * _tuning.StargazeWingOpen);
            _solarWing.localRotation = _wingRest
                * Quaternion.AngleAxis(wingOpen * _tuning.WingOpenAngle, Vector3.right);

            float eyeGlow = _mood.EyeGlow * Mathf.Lerp(1f, _tuning.StargazeEyeGlow, sky);
            SetGlow(_eyeRenderer, eyeGlow);
            SetGlow(_antennaTipRenderer, _mood.TipGlow);
            _eyeLight.intensity = _tuning.EyeLightIntensity * eyeGlow;
        }

        private void SetGlow(Renderer target, float intensity)
        {
            _glowBlock.SetVector(EmissionColorId, new Vector4(intensity, intensity, intensity, 1f));
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
        }
    }
}
