using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.World
{
    /// <summary>
    /// Drives the lunar day clock, the opening eclipse hold and the scene's sky and lighting blend at runtime (0.5.7
    /// SKY-CYCLE), and registers <see cref="ISunlight"/> plus the <see cref="WorldSaveKeys.Sky"/> save section. Thin:
    /// all clock, eclipse and horizon-marching math lives in <see cref="SunClock"/>, and the scene/shader writes live
    /// in <see cref="WorldAtmosphere"/> and <see cref="SkyShaderGlobals"/>.
    /// </summary>
    public sealed class SkyCycleSystem : MonoBehaviour, IGameSystem, ISunlight
    {
        private const float AmbientProbeStepThreshold = 0.015f;

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset).")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The single shadow-casting directional key light (RenderSettings.sun).")]
        [SerializeField] private Light _keyLight;

        [Tooltip("The shadowless directional fill light opposite the key.")]
        [SerializeField] private Light _fillLight;

        private GameContext _context;
        private ITerrainQuery _terrain;
        private IWorldLayout _layout;
        private IRoverState _rover;
        private ISaveService _save;
        private IDisposable _saveToken;
        private IDisposable _milestones;
        private OpeningSkyPacing _pacing;
        private SunClock _clock;
        private bool _saveResolved;
        private bool _loggedMissingRover;
        private float _smoothedSunlight;
        private float _smoothedLocalSunlight;
        private float _lastProbeSunlight = -1f;
        private float _lastProbeLocalSunlight = -1f;
        private float _lastProbeEclipse = -1f;

        public SunClock Clock => _clock;

        public float TimeSeconds
        {
            get
            {
                EnsureSaveResolved();
                return _clock.TimeSeconds;
            }
        }

        public bool IsHoldingEclipse
        {
            get
            {
                EnsureSaveResolved();
                return _clock.IsHoldingEclipse;
            }
        }

        public Vector3 SunDirection
        {
            get
            {
                EnsureSaveResolved();
                return _clock.SunDirection;
            }
        }

        public float EclipseFactor
        {
            get
            {
                EnsureSaveResolved();
                return _clock.EclipseFactor;
            }
        }

        public float EarthPhase
        {
            get
            {
                EnsureSaveResolved();
                return _clock.EarthPhase;
            }
        }

        public SkyPhase Phase
        {
            get
            {
                EnsureSaveResolved();
                return _clock.Phase;
            }
        }

        public float SmoothedSunlight => _smoothedSunlight;

        public float SmoothedLocalSunlight => _smoothedLocalSunlight;

        public void Initialize(GameContext context)
        {
            string problem = SetupProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(SkyCycleSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));
            _terrain = context.Get<ITerrainQuery>();
            _layout = context.Get<IWorldLayout>();
            _save = context.Get<ISaveService>();
            _clock = new SunClock(_settings.SkyCycle, _settings.Sky);
            _pacing = new OpeningSkyPacing(_settings.SkyCycle, _clock);
            _milestones = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestone);
            _saveResolved = false;
            _loggedMissingRover = false;
            _saveToken = _save.Register(_clock.CreateSaveSection());

            context.Register<ISunlight>(this);
        }

        public float SunlightAt(Vector3 position)
        {
            EnsureSaveResolved();
            return _clock.SunlightAt(_terrain, position);
        }

        public void ReleaseOpeningEclipse()
        {
            EnsureSaveResolved();
            _clock.ReleaseOpeningEclipse();
        }

        /// <summary>
        /// Sets the clock directly (used by capture sessions and tests) and optionally snaps the smoothed key-light
        /// blend to the new exposure at once.
        /// </summary>
        public void SetClockTime(float timeSeconds, bool holdingEclipse = false, bool snapLighting = true)
        {
            _saveResolved = true;
            _clock.SetTime(timeSeconds, holdingEclipse);
            if (snapLighting)
            {
                _smoothedSunlight = _clock.GlobalSunlight;
                _smoothedLocalSunlight = _clock.SunlightAt(_terrain, SampleFocusPosition());
            }

            ApplyVisuals(true);
        }

        /// <summary>Advances the sky cycle by one frame of <paramref name="deltaTime"/> seconds. Allocation-free.</summary>
        public void StepFrame(float deltaTime)
        {
            EnsureSaveResolved();
            _clock.Tick(_pacing.ClockDelta(_clock.TimeSeconds, deltaTime), _context.Events);

            float targetGlobalSunlight = _clock.GlobalSunlight;
            float targetLocalSunlight = _clock.SunlightAt(_terrain, SampleFocusPosition());
            float smoothing = Mathf.Max(_settings.SkyCycle.KeySmoothingSeconds, 0.05f);
            if (deltaTime > 0f)
            {
                float step = deltaTime / smoothing;
                _smoothedSunlight = Mathf.MoveTowards(_smoothedSunlight, targetGlobalSunlight, step);
                _smoothedLocalSunlight = Mathf.MoveTowards(_smoothedLocalSunlight, targetLocalSunlight, step);
            }

            bool updateProbe = Mathf.Abs(_smoothedSunlight - _lastProbeSunlight) >= AmbientProbeStepThreshold
                || Mathf.Abs(_smoothedLocalSunlight - _lastProbeLocalSunlight) >= AmbientProbeStepThreshold
                || Mathf.Abs(_clock.EclipseFactor - _lastProbeEclipse) >= AmbientProbeStepThreshold;
            ApplyVisuals(updateProbe);
        }

        private void Start()
        {
            if (_clock == null)
            {
                return;
            }

            EnsureSaveResolved();
            _smoothedSunlight = _clock.GlobalSunlight;
            _smoothedLocalSunlight = _clock.SunlightAt(_terrain, SampleFocusPosition());
            ApplyVisuals(true);
        }

        private void Update()
        {
            if (_clock == null)
            {
                return;
            }

            StepFrame(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _saveToken?.Dispose();
            _milestones?.Dispose();
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            EnsureSaveResolved();
            _pacing.Reach(reached.Milestone, reached.Restored, _clock);
            if (reached.Restored)
            {
                _smoothedSunlight = _clock.GlobalSunlight;
                _smoothedLocalSunlight = _clock.SunlightAt(_terrain, SampleFocusPosition());
                ApplyVisuals(true);
            }
        }

        private void EnsureSaveResolved()
        {
            if (_saveResolved || _clock == null || !_save.IsLoaded)
            {
                return;
            }

            _saveResolved = true;
            _clock.ResolveLoadedSave(_save);
            _smoothedSunlight = _clock.GlobalSunlight;
            _smoothedLocalSunlight = _clock.SunlightAt(_terrain, SampleFocusPosition());
        }

        private void ApplyVisuals(bool updateProbe)
        {
            if (updateProbe)
            {
                _lastProbeSunlight = _smoothedSunlight;
                _lastProbeLocalSunlight = _smoothedLocalSunlight;
                _lastProbeEclipse = _clock.EclipseFactor;
            }

            WorldAtmosphere.ApplyCycle(_settings.Atmosphere, _settings.Sky, _settings.SkyCycle, _clock,
                _smoothedSunlight, _keyLight, _fillLight, updateProbe, _smoothedLocalSunlight);
            SkyShaderGlobals.ApplyCycle(_settings.Sky, _settings.SkyCycle, _clock, _smoothedSunlight);
        }

        private Vector3 SampleFocusPosition()
        {
            if (_rover == null && !_context.TryGet(out _rover))
            {
                if (!_loggedMissingRover)
                {
                    _loggedMissingRover = true;
                    Debug.LogError(
                        $"{nameof(SkyCycleSystem)}: {nameof(IRoverState)} is missing from {nameof(GameContext)}; " +
                        $"using {nameof(IWorldLayout)}.{nameof(IWorldLayout.BasePosition)}.",
                        this);
                }

                return _layout.BasePosition;
            }

            return _rover.Position;
        }

        private string SetupProblem()
        {
            if (_settings == null)
            {
                return "World Settings is not assigned.";
            }

            if (_keyLight == null)
            {
                return "Key Light is not assigned.";
            }

            if (_fillLight == null || _fillLight == _keyLight)
            {
                return "Fill Light must be assigned and must not be the Key Light.";
            }

            return _settings.SkyCycle.Validate();
        }
    }
}
