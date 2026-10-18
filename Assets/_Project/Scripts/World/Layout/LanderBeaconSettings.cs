using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// The landed cargo lander's amber emergency beacon (opening v2 §4, the opening's hook): Art's R1 LanderBeacon on
    /// top of the hull, its lens a small HDR core just over the bloom threshold with a tight halo and a warm light,
    /// pulsing slowly and never fully dark, the brightest warm point in the wake's first frame 165 m away; and a slow
    /// wave of the old cable's marker lamps running from 07's dead post toward it, leading the eye. On a New Game the
    /// beacon blinks sharply with each spark of 07's failing cutter and one bright wave runs the cable when 07
    /// begins its recovery.
    /// </summary>
    [Serializable]
    public sealed class LanderBeaconSettings
    {
        [Tooltip("Colour of the beacon's halo and light: an amber emergency light.")]
        [SerializeField] private Color _color = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Seconds of one pulse: slow, a calm signal.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _period = 3.2f;

        [Tooltip("How short the peak is within its period (a power of the wave): higher is a crisper flash.")]
        [Range(1f, 12f)]
        [SerializeField] private float _sharpness = 3f;

        [Tooltip("The beacon's level between flashes (0..1): it never goes fully dark, so the lander still reads.")]
        [Range(0f, 1f)]
        [SerializeField] private float _floor = 0.35f;

        [Tooltip("Emission strength of the R1 lens at a flash's peak (R1Lit _EmissionStrength): enough for the lens's " +
            "small HDR hot core to cross the bloom threshold.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lensGlow = 3.2f;

        [Tooltip("Brightness of the beacon's halo at a flash's peak.")]
        [Range(0f, 6f)]
        [SerializeField] private float _haloIntensity = 2.2f;

        [Tooltip("Radius of the halo up close, metres: tight on the lens; it grows to its minimum apparent size only " +
            "from far off.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _haloRadius = 0.3f;

        [Tooltip("Smallest apparent radius of the halo, degrees, so the beacon reads as a clear warm point from the " +
            "wake 165 m away without becoming a blob.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.7f;

        [Tooltip("Intensity of the warm light the beacon casts on the hull and the dust at a flash's peak.")]
        [Range(0f, 10f)]
        [SerializeField] private float _lightIntensity = 2.2f;

        [Tooltip("Range of the beacon's light, metres.")]
        [Range(2f, 40f)]
        [SerializeField] private float _lightRange = 16f;

        [Tooltip("Emission strength the old cable's marker lamps rest at (R1Lit _EmissionStrength): a faint amber " +
            "dotted line on the dust.")]
        [Range(0f, 2f)]
        [SerializeField] private float _markerGlow = 0.35f;

        [Tooltip("Extra emission strength a marker lamp gains as the wave passes it.")]
        [Range(0f, 3f)]
        [SerializeField] private float _markerPulse = 1.2f;

        [Tooltip("Seconds the wave takes to run the cable's markers from 07's post to the lander.")]
        [Range(1f, 20f)]
        [SerializeField] private float _markerRunSeconds = 4f;

        [Tooltip("Seconds between waves, start to start: calm, never a chase light.")]
        [Range(2f, 30f)]
        [SerializeField] private float _markerRepeatSeconds = 9f;

        [Tooltip("Share of the run over which a marker brightens and fades as the wave passes (0..1).")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _markerWaveWidth = 0.18f;

        [Header("The self-test's cues (New Game only)")]
        [Tooltip("The first of 07's cutter pulses the beacon answers with a flash (Rover's BootCheckPulse index): " +
            "the turret's spark attempts, so the beacon blinks in sync with the sparks (the spare part is at the " +
            "lander).")]
        [Range(0, 10)]
        [SerializeField] private int _firstSparkPulse = 3;

        [Tooltip("The last of 07's cutter pulses the beacon answers with a flash.")]
        [Range(0, 10)]
        [SerializeField] private int _lastSparkPulse = 5;

        [Tooltip("Seconds an answering flash is lit: short, a hard signal unlike the slow pulse.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _flashSeconds = 0.15f;

        [Tooltip("Seconds a flash takes to light and to fade: small, so it reads sharp without popping.")]
        [Range(0.005f, 0.1f)]
        [SerializeField] private float _flashEdgeSeconds = 0.016f;

        [Tooltip("How many times the slow pulse's peak the lens, halo and light reach in an answering flash.")]
        [Range(1f, 4f)]
        [SerializeField] private float _flashBoost = 3f;

        [Tooltip("How many times wider the halo swells at an answering flash's peak, its smallest apparent size " +
            "too: a point that brightens from the wake, never a bloom blob.")]
        [Range(1f, 3f)]
        [SerializeField] private float _flashHaloSwell = 1.25f;

        [Tooltip("Extra emission strength a marker lamp gains as the single wave sent when 07 begins its recovery " +
            "passes it: brighter than the calm waves, leading 07 to the lander.")]
        [Range(0f, 6f)]
        [SerializeField] private float _beginWavePulse = 4f;

        [Tooltip("Brightness of the small halo a marker lamp blooms with as the Begin wave passes it (the calm waves " +
            "never light it).")]
        [Range(0f, 4f)]
        [SerializeField] private float _markerHaloIntensity = 1.6f;

        [Tooltip("Radius of a marker lamp's halo, metres: small, a soft bloom around the lamp.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _markerHaloRadius = 0.14f;

        [Tooltip("How quickly a marker's halo fades behind the wave (a power of the wave's lift): higher is a " +
            "quicker decay.")]
        [Range(1f, 6f)]
        [SerializeField] private float _markerHaloFalloff = 2f;

        public Color Color => _color;
        public float Period => _period;
        public float Sharpness => _sharpness;
        public float Floor => _floor;
        public float LensGlow => _lensGlow;
        public float HaloIntensity => _haloIntensity;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
        public float LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
        public float MarkerGlow => _markerGlow;
        public float MarkerPulse => _markerPulse;
        public float MarkerRunSeconds => _markerRunSeconds;
        public float MarkerRepeatSeconds => _markerRepeatSeconds;
        public float MarkerWaveWidth => _markerWaveWidth;
        public int FirstSparkPulse => _firstSparkPulse;
        public int LastSparkPulse => _lastSparkPulse;
        public float FlashSeconds => _flashSeconds;
        public float FlashEdgeSeconds => _flashEdgeSeconds;
        public float FlashBoost => _flashBoost;
        public float FlashHaloSwell => _flashHaloSwell;
        public float BeginWavePulse => _beginWavePulse;
        public float MarkerHaloIntensity => _markerHaloIntensity;
        public float MarkerHaloRadius => _markerHaloRadius;
        public float MarkerHaloFalloff => _markerHaloFalloff;
    }
}
