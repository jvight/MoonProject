using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The call's point of light beside dark Earth (opening v3 purpose §2): unmistakably a signal, not a star. A warm
    /// amber-white core warmer than any star keys three short and one long with the carrier
    /// (<see cref="MoonProject.Core.ICallSignal"/>), a thin ring blooms out of it as each tone begins, a dim ember holds
    /// between the tones, and no star stands near it. Dimmer and breaking up while the call is broken, steady once 07
    /// has power.
    /// </summary>
    [Serializable]
    public sealed class CallSkyPointSettings
    {
        [Tooltip("Colour of the core on a tone: amber-white, warmer than any star in the sky.")]
        [SerializeField] private Color _color = new Color(1f, 0.82f, 0.56f, 1f);

        [Tooltip("Colour of the ember it holds between the tones: a deeper amber.")]
        [SerializeField] private Color _emberColor = new Color(1f, 0.58f, 0.26f, 1f);

        [Tooltip("Brightness at the top of a tone. With the ring it stays under the bloom threshold (1.25; checked by " +
            "WorldSettings), so it glows and never flares.")]
        [Range(0.1f, 1.2f)]
        [SerializeField] private float _peak = 0.85f;

        [Tooltip("Share of the peak the ember holds between the tones once the call is clear of the break-up.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rest = 0.22f;

        [Tooltip("Share of the peak the ember holds between the tones while the call is broken: dimmer, so the broken " +
            "tones carry it.")]
        [Range(0f, 1f)]
        [SerializeField] private float _brokenRest = 0.1f;

        [Tooltip("Seconds the light takes to settle from broken to steady (or back).")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _stateFadeSeconds = 1.2f;

        [Tooltip("Radius of the bright core in screen pixels: a point, the same from every camera.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _corePixels = 2.2f;

        [Tooltip("Radius of the soft glow round it in screen pixels.")]
        [Range(2f, 32f)]
        [SerializeField] private float _glowPixels = 12f;

        [Tooltip("How much of the light the soft glow carries against the core.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowShare = 0.3f;

        [Header("The ring each tone sends out")]
        [Tooltip("Seconds a ring takes to bloom out and fade.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _pulseSeconds = 0.3f;

        [Tooltip("How far the ring blooms out, screen pixels.")]
        [Range(4f, 64f)]
        [SerializeField] private float _ringPixels = 26f;

        [Tooltip("Thickness of the ring, screen pixels.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _ringWidthPixels = 1.6f;

        [Tooltip("Brightness of the ring as it leaves the core (it fades as it grows); with the core it stays under " +
            "the bloom threshold.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ringBrightness = 0.38f;

        [Tooltip("Keying above which a tone has begun (and sends a ring).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _toneOn = 0.5f;

        [Tooltip("Keying below which the tone has ended, so the next can send its own ring.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float _toneOff = 0.2f;

        [Header("The clear sky round it")]
        [Tooltip("Degrees round the point kept clear of stars, so none competes with it (they fade out at the edge).")]
        [Range(0f, 8f)]
        [SerializeField] private float _clearDegrees = 3f;

        public Color Color => _color;
        public Color EmberColor => _emberColor;
        public float Peak => _peak;
        public float Rest => _rest;
        public float BrokenRest => _brokenRest;
        public float StateFadeSeconds => _stateFadeSeconds;
        public float CorePixels => _corePixels;
        public float GlowPixels => _glowPixels;
        public float GlowShare => _glowShare;
        public float PulseSeconds => _pulseSeconds;
        public float RingPixels => _ringPixels;
        public float RingWidthPixels => _ringWidthPixels;
        public float RingBrightness => _ringBrightness;
        public float ToneOn => _toneOn;
        public float ToneOff => _toneOff;
        public float ClearDegrees => _clearDegrees;
    }
}
