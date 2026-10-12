using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One roll-out of the reach ring: from its centre to its reach over the tuned duration, eased out, fading in at
    /// the start and away at the end. A new roll-out replaces the one in flight. Plain logic, allocation-free.
    /// </summary>
    public sealed class ReachRing
    {
        private readonly ReachRingSettings _settings;
        private float _startedAt = float.NegativeInfinity;

        public ReachRing(ReachRingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Vector3 Centre { get; private set; }
        public float Reach { get; private set; }

        /// <summary>Starts a roll-out from <paramref name="centre"/> to <paramref name="reach"/> metres.</summary>
        public void Start(Vector3 centre, float reach, float time)
        {
            Centre = centre;
            Reach = reach;
            _startedAt = time;
        }

        /// <summary>The ring at <paramref name="time"/>: false once it has faded (or never started).</summary>
        public bool Sample(float time, out float radius, out float strength)
        {
            float t = (time - _startedAt) / _settings.Duration;
            if (t < 0f || t >= 1f)
            {
                radius = 0f;
                strength = 0f;
                return false;
            }

            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            radius = Reach * eased;
            float fadeIn = _settings.FadeIn > 0f ? Mathf.SmoothStep(0f, 1f, t / _settings.FadeIn) : 1f;
            float fadeOut = _settings.FadeOut > 0f ? Mathf.SmoothStep(0f, 1f, (1f - t) / _settings.FadeOut) : 1f;
            strength = fadeIn * fadeOut;
            return true;
        }
    }
}
