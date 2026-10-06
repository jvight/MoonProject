using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How one piece of UI eases in and out: opacity over a fade, a short slide, and a scale that springs up with a
    /// soft overshoot. Nothing pops.
    /// </summary>
    [Serializable]
    public sealed class RevealSettings
    {
        [Tooltip("Seconds to fade fully in.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _fadeIn = 0.35f;

        [Tooltip("Seconds to fade fully out.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _fadeOut = 0.35f;

        [Tooltip("Pixels (at 1080p) the element drifts while fading: positive rises into place from below.")]
        [Range(-60f, 60f)]
        [SerializeField] private float _slide = 10f;

        [Tooltip("Scale while hidden; it springs to 1 when shown.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _scaleFrom = 0.94f;

        [Tooltip("Spring frequency (Hz) of the scale: higher settles sooner.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _springFrequency = 1.6f;

        [Tooltip("Spring damping ratio of the scale: 1 = no overshoot, lower = a softer, longer settle.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _springDamping = 0.6f;

        public RevealSettings()
        {
        }

        public RevealSettings(float fadeIn, float fadeOut, float slide, float scaleFrom, float springFrequency,
            float springDamping)
        {
            _fadeIn = fadeIn;
            _fadeOut = fadeOut;
            _slide = slide;
            _scaleFrom = scaleFrom;
            _springFrequency = springFrequency;
            _springDamping = springDamping;
        }

        public float FadeIn => _fadeIn;

        public float FadeOut => _fadeOut;

        public float Slide => _slide;

        public float ScaleFrom => _scaleFrom;

        public float SpringFrequency => _springFrequency;

        public float SpringDamping => _springDamping;
    }
}
