using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Pulses the landed cargo lander's amber emergency beacon (opening v2 §4): Art's R1 lens (its emission strength),
    /// World's tight halo and warm light, together in a slow <see cref="LocatorBlink"/>, and runs a calm wave of light
    /// along the old cable's marker lamps from 07's dead post toward it (<see cref="CableMarkerWave"/>). The scene
    /// build stands Art's beacon on the lander, hangs the halo and light on its light anchor and wires the markers in
    /// order from the post. 07's New Game self-test cues two answers (a Continue's short wake runs no self-test): the
    /// beacon flashes sharply when the cutter check fails (the spare part is at the lander), and one bright wave runs
    /// the cable when 07 begins its recovery. Allocation-free per frame.
    /// </summary>
    public sealed class LanderBeacon : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' lander beacon.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Art's R1 beacon lens (LanderBeacon/BeaconLens, R1_LampAmber).")]
        [SerializeField] private Renderer _lens;

        [Tooltip("World's halo on the beacon (LofiBeaconHalo).")]
        [SerializeField] private Renderer _halo;

        [Tooltip("World's warm light on the beacon.")]
        [SerializeField] private Light _light;

        [Tooltip("The old cable's marker lamps (GroundCableSegment/MarkerLamp), in order from 07's post to the lander.")]
        [SerializeField] private Renderer[] _markers = new Renderer[0];

        private MaterialPropertyBlock _block;
        private IDisposable _checksCompleted;
        private IDisposable _checksStarted;
        private float _flashStart = float.NegativeInfinity;
        private float _waveStart = float.NegativeInfinity;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _lens == null || _halo == null || _light == null || _markers == null
                || Array.IndexOf(_markers, null) >= 0)
            {
                Debug.LogError($"{nameof(LanderBeacon)}: World Settings, the lens, the halo, the light and every " +
                    "cable marker must be assigned (the scene build wires them on the lander).", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _checksCompleted = context.Events.Subscribe<BootCheckCompleted>(OnCheckCompleted);
            _checksStarted = context.Events.Subscribe<BootCheckStarted>(OnCheckStarted);
        }

        private void OnDestroy()
        {
            _checksCompleted?.Dispose();
            _checksStarted?.Dispose();
        }

        private void OnCheckCompleted(BootCheckCompleted check)
        {
            if (check.Step == BootCheckStep.Cutter)
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
            float flash = BeaconFlash.Level(time - _flashStart, beacon.FlashCount, beacon.FlashPeriod,
                beacon.FlashOnShare, beacon.FlashEdge);
            float level = Mathf.Max(LocatorBlink.Level(time, beacon.Period, beacon.Sharpness, beacon.Floor),
                flash * beacon.FlashBoost);
            Glow(_lens, level * beacon.LensGlow);
            _block.Clear();
            _block.SetFloat(IntensityId, level * beacon.HaloIntensity);
            _halo.SetPropertyBlock(_block);
            _light.intensity = level * beacon.LightIntensity;
            for (int i = 0; i < _markers.Length; i++)
            {
                float lift = CableMarkerWave.Lift(time, i, _markers.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerRepeatSeconds, beacon.MarkerWaveWidth);
                float begin = CableMarkerWave.Once(time - _waveStart, i, _markers.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerWaveWidth);
                Glow(_markers[i], beacon.MarkerGlow
                    + Mathf.Max(lift * beacon.MarkerPulse, begin * beacon.BeginWavePulse));
            }
        }

        private void Glow(Renderer target, float strength)
        {
            _block.Clear();
            _block.SetFloat(EmissionStrengthId, strength);
            target.SetPropertyBlock(_block);
        }
    }
}
