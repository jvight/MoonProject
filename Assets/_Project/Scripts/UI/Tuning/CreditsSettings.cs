using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The credits screen: how it eases in and how its slow auto-scroll behaves.</summary>
    [Serializable]
    public sealed class CreditsSettings
    {
        [Tooltip("The credits panel easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.5f, 0.4f, 12f, 0.97f, 1.4f, 0.75f);

        [Tooltip("Pixels per second the credits drift upward by themselves (slow and calm).")]
        [Range(5f, 120f)]
        [SerializeField] private float _scrollSpeed = 34f;

        [Tooltip("Seconds the credits rest at the top before they begin to drift.")]
        [Range(0f, 10f)]
        [SerializeField] private float _startDelaySeconds = 2f;

        [Tooltip("Seconds the drift waits after the player scrolls (wheel, stick or keys) before it carries on.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _resumeAfterSeconds = 5f;

        [Tooltip("Pixels one press of up or down (or a stick tick) scrolls.")]
        [Range(20f, 400f)]
        [SerializeField] private float _lineStepPixels = 110f;

        public RevealSettings Reveal => _reveal;

        public float ScrollSpeed => _scrollSpeed;

        public float StartDelaySeconds => _startDelaySeconds;

        public float ResumeAfterSeconds => _resumeAfterSeconds;

        public float LineStepPixels => _lineStepPixels;
    }
}
