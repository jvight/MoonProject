using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's procedural body language on the RoverModel's Neck, Head, Eyelid, SolarWing, Eye and AntennaTip:
    /// the head looks at whatever it interacts with (<see cref="IRoverGaze"/>) or along its way with a lagging glance
    /// into turns; left alone it drifts into a daydream (<see cref="RoverMood"/>) and looks up toward Earth. Soft landings
    /// and driving off from a daydream make it perk up; hard landings get a small "oof".
    /// Needs <see cref="IWorldLayout"/>, so it must be initialised after the World systems.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    public sealed class RoverBodyLanguage : MonoBehaviour, IGameSystem, IRoverGaze
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

        private readonly GazeRequests _requests = new GazeRequests();
        private RoverMood _mood;
        private IWorldLayout _world;
        private IDisposable _landedSubscription;
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
            _mood = new RoverMood(_tuning, MoodSeed);
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

            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnLanded);
            context.Register<IRoverGaze>(this);
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

        public void SetGazeTarget(GazePriority priority, Vector3? worldPoint)
        {
            _requests.Set(priority, worldPoint);
        }

        public void PerkUp(float strength)
        {
            if (!_initialized)
            {
                return;
            }

            _mood.PerkUp(strength);
            _rig.KickAntenna(Mathf.Clamp01(strength) * _tuning.PerkAntennaKick);
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
                PerkUp(_tuning.SoftLandingPerk);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

            if (_mood.Step(_rover.Speed, _rover.DriveInput.magnitude, deltaTime))
            {
                PerkUp(_tuning.WakePerk);
            }

            StepGaze(deltaTime);
            Apply();
        }

        private void StepGaze(float deltaTime)
        {
            Transform frame = _neck.parent;
            Vector2 aim;
            float frequency;

            if (_requests.TryGetTop(out Vector3 target, out _))
            {
                aim = Aim(frame.InverseTransformDirection(target - _head.position));
                frequency = _tuning.TargetGazeFrequency;
            }
            else
            {
                var travel = new Vector2(_rover.SteerInput * _tuning.LookIntoTurn, _tuning.TravelHeadPitch);
                float idle = _mood.Idle;
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
            _landedSubscription?.Dispose();
        }
    }
}
