using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// How home's warm points wake when power reaches their stage (docs/features/M3-16, pillar 6): one by one, each
    /// easing up slowly, so the base comes alive like a house at dusk rather than switching on.
    /// </summary>
    [Serializable]
    public sealed class HomeLightingSettings
    {
        [Tooltip("Seconds between one warm point of a stage starting to wake and the next (windows, then porch " +
            "lamps, then the halo).")]
        [Range(0f, 5f)]
        [SerializeField] private float _wakeStagger = 1.2f;

        [Tooltip("Seconds each warm point takes to ease from dark to fully lit.")]
        [Range(0.1f, 8f)]
        [SerializeField] private float _wakeFade = 2.4f;

        [Tooltip("Steps the windows wake in, one warm step per window, each paired with Audio's soft click.")]
        [Range(1, 8)]
        [SerializeField] private int _windowSteps = 4;

        [Tooltip("Seconds each window step eases up over: never a snap.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _windowStepEase = 0.35f;

        [Tooltip("Seconds power takes to travel a ground cable from the tower to what it wakes, segment by segment; " +
            "that place's warm points start waking when it arrives.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _cableTravel = 1.2f;

        [Tooltip("Seconds each cable segment takes to ease from dark to lit.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _cableSegmentFade = 0.12f;

        [Tooltip("Glow of a cable bead as the power pulse passes it (linear glow multiplier): unmistakable from the " +
            "chase camera.")]
        [Range(1f, 10f)]
        [SerializeField] private float _cablePulsePeak = 4f;

        [Tooltip("Seconds a bead takes to settle from the pulse's flare to its steady glow.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _cablePulseSeconds = 0.6f;

        [Tooltip("Steady glow of a powered cable (linear glow multiplier): soft, so home reads as connected.")]
        [Range(0f, 2f)]
        [SerializeField] private float _cableSteady = 0.45f;

        [Tooltip("Intensity of the warm light riding the power pulse along the cable: it lights the ground as it " +
            "races home.")]
        [Range(0f, 20f)]
        [SerializeField] private float _cablePulseLight = 6f;

        [Tooltip("Range of the light riding the pulse, metres.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _cablePulseLightRange = 4f;

        [Header("The tower's power preview (M3-24 B)")]
        [Tooltip("Seconds the preview's slow light takes to run one dark cable from the tower's foot to its end, the " +
            "first time 07 comes near the tower: it shows what each level will power.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _previewTravel = 2.6f;

        [Tooltip("Seconds between the preview's runs: the lander's cable first, then the bay's, then the chart's.")]
        [Range(0f, 4f)]
        [SerializeField] private float _previewStagger = 1.3f;

        [Tooltip("Seconds a cable segment takes to brighten as the preview's light reaches it.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _previewRise = 0.3f;

        [Tooltip("Seconds a segment's preview glow lingers before it is dark again: a soft trail behind the light.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _previewTail = 1.4f;

        [Tooltip("Peak glow of the preview (0..1 of a powered cable): a promise, softer than real power.")]
        [Range(0f, 1f)]
        [SerializeField] private float _previewPeak = 0.75f;

        [Tooltip("Seconds between blinks of the tower's pilot light while the base is asleep. Slow: a heartbeat " +
            "saying something here still waits.")]
        [Range(1f, 10f)]
        [SerializeField] private float _pilotPeriod = 3.5f;

        [Tooltip("How short each pilot blink is: higher = a briefer flash in a longer dark.")]
        [Range(1f, 16f)]
        [SerializeField] private float _pilotSharpness = 6f;

        [Tooltip("Steady level of the tower's top light once home is awake (0..1 of the pilot intensity): home " +
            "stays a warm dot from across the basin.")]
        [Range(0f, 1f)]
        [SerializeField] private float _topLightLevel = 0.6f;

        [Tooltip("Smallest apparent radius of the tower's top light, degrees (the Peak beacon's trick), so home " +
            "never shrinks below a readable warm dot at 300 m.")]
        [Range(0f, 2f)]
        [SerializeField] private float _pilotMinAngle = 0.4f;

        [Header("The network's handshake home (north-star beat 7)")]
        [Tooltip("Seconds the handshake's echo takes to run a powered cable from the tower's foot to home, when the " +
            "network pulse reaches the tower.")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _echoTravel = 1.2f;

        [Tooltip("Seconds a cable segment takes to brighten as the echo reaches it.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _echoRise = 0.15f;

        [Tooltip("Seconds a segment's echo glow eases back to the steady glow.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _echoTail = 0.9f;

        [Tooltip("Peak glow of the echo (linear glow scale; a powered cable rests at Cable Steady): softer than " +
            "power arriving.")]
        [Range(0f, 4f)]
        [SerializeField] private float _echoPeak = 1.8f;

        [Header("The tower comes online (feel-pass §8.2)")]
        [Tooltip("Seconds a new power stage waits for the tower's build beat to reach its Follow before it applies " +
            "anyway (the beat takes about 6 s).")]
        [Range(1f, 20f)]
        [SerializeField] private float _buildHoldTimeout = 8f;

        [Tooltip("How near the tower's top light (metres) a Build beat must happen to be the tower's.")]
        [Range(1f, 60f)]
        [SerializeField] private float _towerBeatRadius = 20f;

        [Tooltip("How much brighter the top light flares on the beat's Impact (times its intensity).")]
        [Range(0f, 8f)]
        [SerializeField] private float _beaconFlareGain = 3f;

        [Tooltip("How much wider the top light's glow grows at the flare's peak (times its radius).")]
        [Range(0f, 10f)]
        [SerializeField] private float _beaconFlareGrow = 4f;

        [Tooltip("Seconds the flare takes to rise and settle.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _beaconFlareSeconds = 1.4f;

        [Tooltip("Seconds of one slow turn of the top light once the tower is online.")]
        [Range(2f, 30f)]
        [SerializeField] private float _beaconTurnPeriod = 7f;

        [Tooltip("Share of the top light that sweeps with the turn (0 = steady, 1 = dark when facing away).")]
        [Range(0f, 1f)]
        [SerializeField] private float _beaconSweep = 0.55f;

        [Tooltip("How narrow the turning beam is (a power of its facing).")]
        [Range(1f, 16f)]
        [SerializeField] private float _beaconSweepSharpness = 4f;

        [Tooltip("Halo intensity of the pilot light at the top of a blink (LofiBeaconHalo _Intensity).")]
        [Range(0f, 4f)]
        [SerializeField] private float _pilotIntensity = 1.4f;

        [Tooltip("Radius of the pilot light's halo, metres.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _pilotRadius = 0.12f;

        [Tooltip("Distance (metres) from which the top light's minimum-size halo starts to fade in. Closer, it is a " +
            "small lamp with a tight glow on its housing.")]
        [Range(0f, 300f)]
        [SerializeField] private float _pilotHaloFrom = 60f;

        [Tooltip("Distance (metres) from which the top light keeps its full minimum size: a warm dot for home.")]
        [Range(1f, 400f)]
        [SerializeField] private float _pilotHaloFull = 80f;

        [Header("Light pools")]
        [Tooltip("Colour of the warm light pools on the ground: the lamps' own warm.")]
        [SerializeField] private Color _poolColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("The warm pools on home's ground, growing with each stage: the dock while the base sleeps; at Home " +
            "the way in under the belly, the lander's own floor, the belly lift's spill, the lab inside and the " +
            "tower's foot; the bay's apron at Bay; the chart's glow through the big porthole at Chart (M3-19: warm " +
            "pools round the three masses).")]
        [SerializeField] private LightPoolTuning[] _pools =
        {
            new LightPoolTuning(HomeLight.Dock, "DockAnchor", new Vector3(0f, -0.02f, 0.8f), 3.5f, 0.38f, true,
                new Color(0.7f, 0.95f, 1.35f)),
            new LightPoolTuning(HomeLight.PorchLamps, "Lander", new Vector3(0f, 2.2f, 5.6f), 9f, 4f, false),
            new LightPoolTuning(HomeLight.Windows, "Lander", new Vector3(0f, 2.9f, 0f), 10f, 3f, false),
            new LightPoolTuning(HomeLight.BayLamps, "RoverBay", new Vector3(0f, 0.03f, 4f), 7f, 1f, true),
            new LightPoolTuning(HomeLight.LiftLamps, "BellyLift", new Vector3(0f, 2.25f, 1.2f), 5.5f, 3.5f, false),
            new LightPoolTuning(HomeLight.LabLamps, "Lab", new Vector3(0f, 1.9f, 0f), 4.5f, 2.2f, false),
            new LightPoolTuning(HomeLight.ChartTable, "BigPorthole", new Vector3(-3.3f, 3.1f, -3.3f), 6f, 1.6f,
                false),
            new LightPoolTuning(HomeLight.PorchLamps, "TowerAnchor", new Vector3(0f, 2.6f, 2.2f), 7.5f, 2.6f, false),
        };

        public float WakeStagger => _wakeStagger;
        public float WakeFade => _wakeFade;
        public int WindowSteps => _windowSteps;
        public float WindowStepEase => _windowStepEase;
        public float CableTravel => _cableTravel;
        public float CablePulsePeak => _cablePulsePeak;
        public float CablePulseSeconds => _cablePulseSeconds;
        public float CableSteady => _cableSteady;
        public float CablePulseLight => _cablePulseLight;
        public float CablePulseLightRange => _cablePulseLightRange;
        public float CableSegmentFade => _cableSegmentFade;
        public float PreviewTravel => _previewTravel;
        public float PreviewStagger => _previewStagger;
        public float PreviewRise => _previewRise;
        public float PreviewTail => _previewTail;
        public float PreviewPeak => _previewPeak;
        public float EchoTravel => _echoTravel;
        public float EchoRise => _echoRise;
        public float EchoTail => _echoTail;
        public float EchoPeak => _echoPeak;
        public float PilotPeriod => _pilotPeriod;
        public float PilotSharpness => _pilotSharpness;
        public float PilotIntensity => _pilotIntensity;
        public float BuildHoldTimeout => _buildHoldTimeout;
        public float TowerBeatRadius => _towerBeatRadius;
        public float BeaconFlareGain => _beaconFlareGain;
        public float BeaconFlareSeconds => _beaconFlareSeconds;
        public float BeaconFlareGrow => _beaconFlareGrow;
        public float BeaconTurnPeriod => _beaconTurnPeriod;
        public float BeaconSweep => _beaconSweep;
        public float BeaconSweepSharpness => _beaconSweepSharpness;
        public float PilotRadius => _pilotRadius;
        public float TopLightLevel => _topLightLevel;
        public float PilotMinAngle => _pilotMinAngle;
        public float PilotHaloFrom => _pilotHaloFrom;
        public float PilotHaloFull => _pilotHaloFull;
        public Color PoolColor => _poolColor;
        public IReadOnlyList<LightPoolTuning> Pools => _pools;
    }
}
