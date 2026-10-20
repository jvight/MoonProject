using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The call's point of light beside dark Earth (opening v3 purpose §2): a calm, tiny star that keys three short
    /// and one long with the carrier (<see cref="MoonProject.Core.ICallSignal"/>), over a soft glow that holds between
    /// the tones; dimmer and breaking up while the call is broken, steady once 07 has power.
    /// </summary>
    [Serializable]
    public sealed class CallSkyPointSettings
    {
        [Tooltip("Colour of the light: a far, pale white, a touch warm (the Ark's sweep, not a lamp).")]
        [SerializeField] private Color _color = new Color(1f, 0.95f, 0.86f, 1f);

        [Tooltip("Brightness at the top of a tone. Kept under the bloom threshold (1.25), so it glows and never flares.")]
        [Range(0.1f, 1.2f)]
        [SerializeField] private float _peak = 1f;

        [Tooltip("Share of the peak the light holds between its tones once the call is clear of the break-up.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rest = 0.35f;

        [Tooltip("Share of the peak it holds between its tones while the call is broken: dimmer, so the broken tones " +
            "carry it.")]
        [Range(0f, 1f)]
        [SerializeField] private float _brokenRest = 0.12f;

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

        public Color Color => _color;
        public float Peak => _peak;
        public float Rest => _rest;
        public float BrokenRest => _brokenRest;
        public float StateFadeSeconds => _stateFadeSeconds;
        public float CorePixels => _corePixels;
        public float GlowPixels => _glowPixels;
        public float GlowShare => _glowShare;
    }
}
