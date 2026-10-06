using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Linear progress 0..1 that walks towards shown or hidden at separate speeds, read through one symmetric ease,
    /// so turning around midway continues smoothly from where it was instead of jumping.
    /// </summary>
    internal sealed class Fade
    {
        private readonly RevealSettings _settings;

        public Fade(RevealSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True when heading towards (or at) fully shown.</summary>
        public bool Target { get; private set; }

        /// <summary>Linear progress, 0 = hidden, 1 = shown.</summary>
        public float Progress { get; private set; }

        /// <summary>Eased visibility 0..1.</summary>
        public float Value => UiEase.InOutSine(Progress);

        public bool IsHidden => !Target && Progress <= 0f;

        public bool IsShown => Target && Progress >= 1f;

        public void Set(bool shown)
        {
            Target = shown;
        }

        /// <summary>
        /// Jumps straight to the end state (setup only, never for something the player is looking at).
        /// </summary>
        public void Snap(bool shown)
        {
            Target = shown;
            Progress = shown ? 1f : 0f;
        }

        /// <summary>Advances by <paramref name="deltaTime"/> seconds; returns true when the progress moved.</summary>
        public bool Step(float deltaTime)
        {
            float before = Progress;
            if (Target)
            {
                Progress = _settings.FadeIn <= 0f ? 1f : Mathf.Min(1f, Progress + deltaTime / _settings.FadeIn);
            }
            else
            {
                Progress = _settings.FadeOut <= 0f ? 0f : Mathf.Max(0f, Progress - deltaTime / _settings.FadeOut);
            }

            return before != Progress;
        }
    }
}
