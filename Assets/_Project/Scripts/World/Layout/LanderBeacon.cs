using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Pulses the landed cargo lander's amber emergency beacon (opening v2 §4): Art's R1 lens (its emission strength),
    /// World's tight halo and warm light, together in a slow <see cref="LocatorBlink"/>, and runs a calm wave of light
    /// along the old cable's clip halos from the lander out to the tear (<see cref="CableMarkerWave"/>): the cable is
    /// torn, so its power gets no further. The scene build stands Art's beacon on the lander, hangs the halo and light
    /// on its light anchor and wires each side's clip halos in order toward the tear, and the spark at the torn
    /// copper. 07's New Game self-test cues two answers (a Continue's short wake runs no self-test): the beacon
    /// flashes sharply, its halo swelling a little, with each spark of 07's failing cutter (the spare part is at the
    /// lander), and one bright wave runs from 07's post when 07 begins its recovery and dies at the tear, where the
    /// last clip stutters and a tiny cold spark ticks once (<see cref="TearCue"/>). The lander's charge also glows along
    /// the port's cable across the build apron and in the port's pilot lamp; once its reserve is spent the beacon, the
    /// pilot lamp and both cables' lander-side glow die together (<see cref="LanderReserveLight"/>, opening v3 R4).
    /// Allocation-free per frame.
    /// </summary>
    public sealed class LanderBeacon : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int MinAngleTanId = Shader.PropertyToID("_MinAngleTan");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' lander beacon.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Art's R1 beacon lens (LanderBeacon/BeaconLens, R1_LampAmber).")]
        [SerializeField] private Renderer _lens;

        [Tooltip("World's halo on the beacon (LofiBeaconHalo).")]
        [SerializeField] private Renderer _halo;

        [Tooltip("World's warm light on the beacon.")]
        [SerializeField] private Light _light;

        [Tooltip("The halos on the old cable's stake clips on 07's side of the tear (LofiBeaconHalo), in order from " +
            "07's post to the tear: the Begin wave's path.")]
        [SerializeField] private Renderer[] _postClips = new Renderer[0];

        [Tooltip("The halos on the cable's stake clips on the lander's side of the tear, in order from the lander's " +
            "port to the tear: the calm waves' path.")]
        [SerializeField] private Renderer[] _landerClips = new Renderer[0];

        [Tooltip("The spark's halo at the torn copper on 07's side (LofiBeaconHalo, cold).")]
        [SerializeField] private Renderer _spark;

        [Tooltip("The halos on the stake clips of the port's cable across the build apron, in order from the port to " +
            "the apron: the lander's charge glows along it to the pad, and goes dark with the reserve.")]
        [SerializeField] private Renderer[] _apronClips = new Renderer[0];

        [Tooltip("Art's reserve pilot lamp on the lander's service port (ReservePilotLamp, R1_LanderBeaconAmber).")]
        [SerializeField] private Renderer _pilotLamp;

        private readonly LanderReserveLight _reserve = new LanderReserveLight();

        private MaterialPropertyBlock _block;
        private IDisposable _checkPulses;
        private IDisposable _checksStarted;
        private IDisposable _reserveChanges;
        private float _flashStart = float.NegativeInfinity;
        private float _waveStart = float.NegativeInfinity;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _lens == null || _halo == null || _light == null || _spark == null
                || _postClips == null || _postClips.Length == 0 || Array.IndexOf(_postClips, null) >= 0
                || _landerClips == null || _landerClips.Length == 0 || Array.IndexOf(_landerClips, null) >= 0
                || _apronClips == null || _apronClips.Length == 0 || Array.IndexOf(_apronClips, null) >= 0
                || _pilotLamp == null)
            {
                Debug.LogError($"{nameof(LanderBeacon)}: World Settings, the lens, the halo, the light, the tear's " +
                    "spark, the clip halos on both sides of the tear and along the apron cable, and the port's pilot " +
                    "lamp must be assigned (the scene build wires them on the lander).", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _checkPulses = context.Events.Subscribe<BootCheckPulse>(OnCheckPulse);
            _checksStarted = context.Events.Subscribe<BootCheckStarted>(OnCheckStarted);
            _reserveChanges = context.Events.Subscribe<LanderReserveChanged>(OnReserveChanged);
        }

        /// <summary>How lit the lander's own lights are: 1 while it holds a reserve, 0 once it has died.</summary>
        public float ReserveLevel => _reserve.Level;

        private void OnDestroy()
        {
            _checkPulses?.Dispose();
            _checksStarted?.Dispose();
            _reserveChanges?.Dispose();
        }

        private void OnReserveChanged(LanderReserveChanged changed)
        {
            _reserve.OnReserve(changed.Reserve01, changed.Restored);
        }

        private void OnCheckPulse(BootCheckPulse pulse)
        {
            LanderBeaconSettings beacon = _settings.Landmarks.LanderBeacon;
            if (pulse.Step == BootCheckStep.Cutter && pulse.Index >= beacon.FirstSparkPulse
                && pulse.Index <= beacon.LastSparkPulse)
            {
                _flashStart = Time.time;
            }
        }

        private void OnCheckStarted(BootCheckStarted check)
        {
            if (check.Step == BootCheckStep.Begin)
            {
                _waveStart = Time.time;
            }
        }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            float time = Time.time;
            LanderBeaconSettings beacon = _settings.Landmarks.LanderBeacon;
            _reserve.Step(Time.deltaTime, beacon.ReserveDarkSeconds);
            float live = _reserve.Level;
            float flash = BeaconFlash.Level(time - _flashStart, beacon.FlashSeconds, beacon.FlashEdgeSeconds);
            float level = live * Mathf.Max(LocatorBlink.Level(time, beacon.Period, beacon.Sharpness, beacon.Floor),
                flash * beacon.FlashBoost);
            Glow(_pilotLamp, live * beacon.PilotLampGlow);
            Glow(_lens, level * beacon.LensGlow);
            float swell = 1f + flash * (beacon.FlashHaloSwell - 1f);
            _block.Clear();
            _block.SetFloat(IntensityId, level * beacon.HaloIntensity);
            _block.SetFloat(RadiusId, beacon.HaloRadius * swell);
            _block.SetFloat(MinAngleTanId, Mathf.Tan(beacon.HaloMinAngle * swell * Mathf.Deg2Rad));
            _halo.SetPropertyBlock(_block);
            _light.intensity = level * beacon.LightIntensity;
            for (int i = 0; i < _landerClips.Length; i++)
            {
                float lift = CableMarkerWave.Lift(time, i, _landerClips.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerRepeatSeconds, beacon.MarkerWaveWidth);
                Halo(_landerClips[i], live * (beacon.ClipHaloRest + lift * beacon.ClipHaloPulse));
            }

            for (int i = 0; i < _apronClips.Length; i++)
            {
                float lift = CableMarkerWave.Lift(time, i, _apronClips.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerRepeatSeconds, beacon.MarkerWaveWidth);
                Halo(_apronClips[i], live * (beacon.ClipHaloRest + lift * beacon.ClipHaloPulse));
            }

            float sinceBegin = time - _waveStart;
            int last = _postClips.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                float begin = CableMarkerWave.Once(sinceBegin, i, _postClips.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerWaveWidth);
                float bloom = Mathf.Pow(begin, beacon.MarkerHaloFalloff);
                if (i == last)
                {
                    bloom = Mathf.Max(bloom, TearCue.Stutter(sinceBegin, beacon.MarkerRunSeconds,
                        beacon.TearStutterSeconds, beacon.TearStutterFlickers));
                }

                Halo(_postClips[i], beacon.ClipHaloRest + bloom * beacon.MarkerHaloIntensity);
            }

            Halo(_spark, TearCue.Spark(sinceBegin, beacon.MarkerRunSeconds, beacon.SparkDelay, beacon.SparkSeconds)
                * beacon.SparkIntensity);
        }

        private void Halo(Renderer target, float intensity)
        {
            _block.Clear();
            _block.SetFloat(IntensityId, intensity);
            target.SetPropertyBlock(_block);
        }

        private void Glow(Renderer target, float strength)
        {
            _block.Clear();
            _block.SetFloat(EmissionStrengthId, strength);
            target.SetPropertyBlock(_block);
        }
    }
}
