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
    /// publishing <see cref="RoverAwoke"/>. It reacts to the game: soft landings, waking up, scrap (happier as a
    /// combo climbs), a relic answering or surfacing (a glance and a perk-up), a deposit (a contented nod), a snapped
    /// tether (a sigh), and hard landings (a small "oof").
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
        private IWorldLayout _world;
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

            _world = context.Get<IWorldLayout>();
            _mood = new RoverMood(_tuning, MoodSeed, _tuning.SleepOnBoot);
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
                events.Subscribe<ScrapCollected>(OnScrapCollected),
                events.Subscribe<RelicAnswered>(OnRelicAnswered),
                events.Subscribe<RelicSurfaced>(OnRelicSurfaced),
                events.Subscribe<RelicDeposited>(OnRelicDeposited),
                events.Subscribe<TetherReleased>(OnTetherReleased),
                events.Subscribe<RoverRecovering>(OnRecovering),
                events.Subscribe<RoverJumped>(OnJumped),
            };
            _initialized = true;
            Apply();
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
            _rover.Gaze.Set(_glanceOwner, point, _tuning.ReactionGazePriority);
            _glanceEnds = Time.time + _tuning.RelicGlanceSeconds;
        }

        private void OnScrapCollected(ScrapCollected scrap)
        {
            float strength = _tuning.ScrapPerk + _tuning.ScrapComboPerk * scrap.ComboStep;
            PerkUp(Mathf.Min(strength, _tuning.ScrapPerkMax));
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

            if (_glanceEnds >= 0f && Time.time >= _glanceEnds)
            {
                _rover.Gaze.Clear(_glanceOwner);
                _glanceEnds = -1f;
            }

            StepGaze(deltaTime);
            Apply();
        }

        private void StepGaze(float deltaTime)
        {
            Transform frame = _neck.parent;
            Vector2 aim;
            float frequency;

            if (_rover.Gaze.TryGetTop(out Vector3 target))
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

            _yaw.Step(aim.x, frequency, _tuning.GazeDamping, deltaTime);
            _pitch.Step(aim.y, frequency, _tuning.GazeDamping, deltaTime);
        }

        private Vector2 Aim(Vector3 localDirection)
        {
            return HeadAim.Angles(localDirection, _tuning.NeckYawLimit, _tuning.HeadPitchUpLimit,
                _tuning.HeadPitchDownLimit);
        }

        private void Apply()
        {
            float breathing = _tuning.IdleHeadBreath * _mood.Idle * (2f * _mood.Breath - 1f);
            float pitch = _pitch.Value + _mood.HeadPitchOffset + breathing;

            _neck.localRotation = _neckRest * Quaternion.AngleAxis(_yaw.Value, Vector3.up);
            _head.localRotation = _headRest * Quaternion.AngleAxis(-pitch, Vector3.right);
            _eyelid.localRotation = _eyelidRest
                * Quaternion.AngleAxis(_mood.LidClosure * _tuning.EyelidClosedAngle, Vector3.right);
            _solarWing.localRotation = _wingRest
                * Quaternion.AngleAxis(_mood.WingOpen * _tuning.WingOpenAngle, Vector3.right);

            SetGlow(_eyeRenderer, _mood.EyeGlow);
            SetGlow(_antennaTipRenderer, _mood.TipGlow);
            _eyeLight.intensity = _tuning.EyeLightIntensity * _mood.EyeGlow;
        }

        private void SetGlow(Renderer target, float intensity)
        {
            _glowBlock.SetColor(EmissionColorId, Color.white * intensity);
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
