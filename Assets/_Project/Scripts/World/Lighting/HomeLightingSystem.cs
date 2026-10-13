using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Registers <see cref="IHomeLights"/> (docs/features/M3-16): follows <see cref="BasePowerChanged"/> and tells
    /// whoever drives home's lamps how lit each warm point is now. It drives World's own parts of the wake itself: the
    /// glass segments of Art's ground power cables (lit from the tower outward), the tower's pilot blink while the
    /// base sleeps, the warm light pools on the ground under home's warm points and home's grade, which warms a step
    /// with each stage. The first time 07 comes near the radio tower (<see cref="PlaceFirstApproached"/>), a slow light
    /// previews each dark cable, showing what the tower will power (docs/features/M3-24 B). When the network's
    /// handshake reaches home (<see cref="NetworkPulseReached"/>), a soft light runs along the powered cables
    /// (north-star beat 7). Thin: the timing lives in
    /// <see cref="HomeLightStages"/>.
    /// </summary>
    public sealed class HomeLightingSystem : MonoBehaviour, IGameSystem, IHomeLights
    {
        /// <summary>The radio tower's place id (docs/features/M3-16 contracts, published by Gameplay).</summary>
        private const string TowerPlace = "place.tower";

        /// <summary>Home's id among the relay network's nodes (RelayNode.Id, set by Gameplay).</summary>
        private const string HomeNode = "home";

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int MinAngleTanId = Shader.PropertyToID("_MinAngleTan");
        private static readonly int MinAngleFromId = Shader.PropertyToID("_MinAngleFrom");
        private static readonly int MinAngleFullId = Shader.PropertyToID("_MinAngleFull");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Home Lighting block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Glass segments of the lander's PowerCable_Home, ordered out from the tower (Glow_0..n).")]
        [SerializeField] private Renderer[] _homeCable = new Renderer[0];

        [Tooltip("Glass segments of the lander's PowerCable_Bay, ordered out from the tower.")]
        [SerializeField] private Renderer[] _bayCable = new Renderer[0];

        [Tooltip("Glass segments of the lander's PowerCable_Chart (tower L3's, to the survey chart), ordered out " +
            "from the tower.")]
        [SerializeField] private Renderer[] _chartCable = new Renderer[0];

        [Tooltip("The pilot light's halo at the top of the radio tower (LofiBeaconHalo).")]
        [SerializeField] private Renderer _pilot;

        [Tooltip("Global Volume with home's warm grade (Generated/World/HomeGradeProfile); its weight follows the " +
            "power stages.")]
        [SerializeField] private Volume _homeGrade;

        [Tooltip("The warm pools' point lights, one slot per Home Lighting pool in the same order, empty for disc " +
            "pools (the scene build hangs them from their anchors in the base).")]
        [SerializeField] private Light[] _pools = new Light[0];

        [Tooltip("The warm pools' light discs (Art's LightPool), one slot per pool, empty for point-light pools.")]
        [SerializeField] private Renderer[] _poolDiscs = new Renderer[0];

        private MaterialPropertyBlock _block;
        private HomeLightStages _stages;
        private IDisposable _powerChanged;
        private IDisposable _placeApproached;
        private float[] _homeApplied;
        private float[] _bayApplied;
        private float[] _chartApplied;
        private float _gradeApplied = -1f;
        private GameContext _context;
        private int _windowStepsLit;
        private TowerBuildBeat _build;
        private Light _pulseLight;
        private IDisposable _feelBeat;
        private IViewCamera _view;
        private float[] _poolsApplied;
        private IDisposable _networkPulse;

        public void Initialize(GameContext context)
        {
            string problem = SetupProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(HomeLightingSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _stages = new HomeLightStages(_settings.HomeLighting);
            _homeApplied = Unapplied(_homeCable.Length);
            _bayApplied = Unapplied(_bayCable.Length);
            _chartApplied = Unapplied(_chartCable.Length);
            _poolsApplied = Unapplied(_pools.Length);
            SetUpPools();
            _context = context;
            _build = new TowerBuildBeat(_settings.HomeLighting);
            _pulseLight = CreatePulseLight(_settings.HomeLighting);
            _powerChanged = context.Events.Subscribe<BasePowerChanged>(OnPowerChanged);
            _feelBeat = context.Events.Subscribe<FeelBeat>(OnFeelBeat);
            _placeApproached = context.Events.Subscribe<PlaceFirstApproached>(OnPlaceApproached);
            _networkPulse = context.Events.Subscribe<NetworkPulseReached>(OnNetworkPulse);
            context.Register<IHomeLights>(this);
        }

        public float Level(HomeLight light)
        {
            return _stages.Level(light, Time.time);
        }

        private void Update()
        {
            if (_stages == null)
            {
                return;
            }

            float time = Time.time;
            if (_build.TryRelease(time, out BasePowerStage released))
            {
                // The beat's Follow: the light races along the cable to home, then the windows step on.
                _stages.SetStage(released, false, time);
            }

            ApplyPulseLight(time);
            ApplyCable(BasePowerStage.Home, _homeCable, _homeApplied, time);
            ApplyCable(BasePowerStage.Bay, _bayCable, _bayApplied, time);
            ApplyCable(BasePowerStage.Chart, _chartCable, _chartApplied, time);

            HomeLightingSettings tuning = _settings.HomeLighting;
            float pilot = _stages.PilotLevel(time);
            _block.Clear();
            float flare = _build.Flare(time);
            _pilot.enabled = pilot > 0f || flare > 0f;
            float glow = pilot * _build.Sweep(ToViewer(), time) + flare * tuning.BeaconFlareGain;
            _block.SetFloat(IntensityId, glow * tuning.PilotIntensity);
            _block.SetFloat(RadiusId, tuning.PilotRadius * (1f + flare * tuning.BeaconFlareGrow));
            _block.SetFloat(MinAngleTanId, Mathf.Tan(tuning.PilotMinAngle * Mathf.Deg2Rad));
            _block.SetFloat(MinAngleFromId, tuning.PilotHaloFrom);
            _block.SetFloat(MinAngleFullId, tuning.PilotHaloFull);
            _pilot.SetPropertyBlock(_block);
            ApplyPools(tuning, time);
            PublishWindowSteps(time);
            float warmth = _stages.GradeWarmth(time);
            if (!Mathf.Approximately(warmth, _gradeApplied))
            {
                _gradeApplied = warmth;
                _homeGrade.weight = warmth;
            }
        }

        private void ApplyCable(BasePowerStage stage, Renderer[] segments, float[] applied, float time)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                float level = _stages.CableLevel(stage, i, segments.Length, time);
                if (Mathf.Approximately(level, applied[i]))
                {
                    continue;
                }

                applied[i] = level;
                _block.Clear();
                _block.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
                segments[i].SetPropertyBlock(_block);
            }
        }

        private void SetUpPools()
        {
            HomeLightingSettings tuning = _settings.HomeLighting;
            for (int i = 0; i < _pools.Length; i++)
            {
                LightPoolTuning pool = tuning.Pools[i];
                if (pool.Disc)
                {
                    Transform disc = _poolDiscs[i].transform;
                    disc.localPosition = pool.Offset;
                    disc.localScale = Vector3.one * pool.Range;
                    _poolDiscs[i].enabled = false;
                    continue;
                }

                Light light = _pools[i];
                light.type = LightType.Point;
                light.color = tuning.PoolColor;
                light.range = pool.Range;
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                light.enabled = false;
                light.transform.localPosition = pool.Offset;
            }
        }

        private void ApplyPools(HomeLightingSettings tuning, float time)
        {
            for (int i = 0; i < _pools.Length; i++)
            {
                LightPoolTuning pool = tuning.Pools[i];
                float level = _stages.Level(pool.WakesWith, time);
                if (Mathf.Approximately(level, _poolsApplied[i]))
                {
                    continue;
                }

                _poolsApplied[i] = level;
                if (pool.Disc)
                {
                    float strength = level * pool.Intensity;
                    Color tint = pool.Tint;
                    _block.Clear();
                    _block.SetVector(EmissionColorId,
                        new Vector4(strength * tint.r, strength * tint.g, strength * tint.b, 1f));
                    _poolDiscs[i].SetPropertyBlock(_block);
                    _poolDiscs[i].enabled = level > 0f;
                    continue;
                }

                _pools[i].intensity = level * pool.Intensity;
                _pools[i].enabled = level > 0f;
            }
        }

        /// <summary>One <see cref="HomeWindowLit"/> per window step as it begins, so Audio clicks with each.</summary>
        private void PublishWindowSteps(float time)
        {
            int lit = _stages.WindowStepsLit(time);
            while (_windowStepsLit < lit)
            {
                _windowStepsLit++;
                _context.Events.Publish(new HomeWindowLit(_windowStepsLit, _settings.HomeLighting.WindowSteps));
            }

            _windowStepsLit = lit;
        }

        private Light CreatePulseLight(HomeLightingSettings tuning)
        {
            var host = new GameObject("CablePulseLight");
            host.transform.SetParent(transform, false);
            var light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = tuning.PoolColor;
            light.range = tuning.CablePulseLightRange;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        /// <summary>A warm light rides the power pulse along the waking stage's cable beads, tower to home.</summary>
        private void ApplyPulseLight(float time)
        {
            Renderer[] beads = null;
            bool racing = _stages.CablePulse(time, out BasePowerStage stage, out float progress);
            if (racing)
            {
                beads = stage == BasePowerStage.Home ? _homeCable : stage == BasePowerStage.Bay ? _bayCable
                    : stage == BasePowerStage.Chart ? _chartCable : null;
            }

            if (beads == null || beads.Length == 0)
            {
                _pulseLight.enabled = false;
                return;
            }

            float along = Mathf.Clamp01(progress) * (beads.Length - 1);
            int from = Mathf.Min(Mathf.FloorToInt(along), beads.Length - 1);
            int to = Mathf.Min(from + 1, beads.Length - 1);
            _pulseLight.transform.position = Vector3.Lerp(beads[from].bounds.center, beads[to].bounds.center,
                along - from);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, progress - 1f > 0f
                ? (progress - 1f) * _settings.HomeLighting.CableTravel / _settings.HomeLighting.CablePulseSeconds
                : 0f);
            _pulseLight.intensity = _settings.HomeLighting.CablePulseLight * fade;
            _pulseLight.enabled = fade > 0f;
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            float near = _settings.HomeLighting.TowerBeatRadius;
            if (beat.Kind == FeelKind.Build && (beat.Position - _pilot.transform.position).sqrMagnitude <= near * near)
            {
                _build.Beat(beat.Phase, beat.Strength, Time.time);
            }
        }

        /// <summary>Horizontal direction from the top light to the view camera (for the turn's sweep).</summary>
        private Vector3 ToViewer()
        {
            if (_view == null && _context.TryGet(out IViewCamera view))
            {
                _view = view;
            }

            Camera camera = _view != null ? _view.Camera : null;
            return camera != null ? camera.transform.position - _pilot.transform.position : Vector3.zero;
        }

        private void OnPowerChanged(BasePowerChanged change)
        {
            if (!_build.Arrive(change.Stage, change.Restored, Time.time))
            {
                return;
            }

            _stages.SetStage(change.Stage, change.Restored, Time.time);
            if (change.Restored)
            {
                // A loaded stage shows at once and silently: no clicks for windows that were already lit.
                _windowStepsLit = _stages.WindowStepsLit(Time.time);
            }
        }

        private void OnPlaceApproached(PlaceFirstApproached approached)
        {
            if (approached.PlaceId == TowerPlace)
            {
                _stages.Preview(Time.time);
            }
        }

        private void OnNetworkPulse(NetworkPulseReached reached)
        {
            if (reached.NodeId == HomeNode)
            {
                _stages.Echo(Time.time);
            }
        }

        private void OnDestroy()
        {
            _powerChanged?.Dispose();
            _placeApproached?.Dispose();
            _networkPulse?.Dispose();
            _feelBeat?.Dispose();
        }

        private static float[] Unapplied(int count)
        {
            var applied = new float[count];
            for (int i = 0; i < count; i++)
            {
                applied[i] = -1f;
            }

            return applied;
        }

        private static bool HasEmpty(Renderer[] segments)
        {
            if (segments == null || segments.Length == 0)
            {
                return true;
            }

            foreach (Renderer segment in segments)
            {
                if (segment == null)
                {
                    return true;
                }
            }

            return false;
        }

        private string SetupProblem()
        {
            if (_settings == null)
            {
                return "World Settings is not assigned.";
            }

            if (HasEmpty(_homeCable) || HasEmpty(_bayCable) || HasEmpty(_chartCable))
            {
                return "Home, Bay and Chart cable segments must be assigned (the scene build wires Art's Glow_ nodes).";
            }

            if (_pilot == null)
            {
                return "The tower's pilot halo is not assigned.";
            }

            int count = _settings.HomeLighting.Pools.Count;
            if (_pools == null || _poolDiscs == null || _pools.Length != count || _poolDiscs.Length != count)
            {
                return $"Light pools and discs must match Home Lighting's {count} pools (the scene build hangs them " +
                    "from the base).";
            }

            for (int i = 0; i < count; i++)
            {
                bool disc = _settings.HomeLighting.Pools[i].Disc;
                if (disc ? _poolDiscs[i] == null : _pools[i] == null)
                {
                    return $"Light pool {i} is missing its {(disc ? "disc" : "light")}.";
                }
            }

            return _homeGrade == null ? "Home's grade Volume is not assigned." : null;
        }
    }
}
