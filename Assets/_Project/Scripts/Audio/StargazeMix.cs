using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The stargazing beat's sound (M3-15 §5, <see cref="Core.Events.StargazingChanged"/>): the beat's start asks for
    /// one soft airy swell, never again within the swell's rest, so a beat that ends and restarts soon after stays
    /// quiet. Real time; allocation-free.
    /// </summary>
    public sealed class StargazeMix
    {
        private readonly SoundscapeTuning _tuning;
        private float _sinceSwell = float.PositiveInfinity;

        public StargazeMix(SoundscapeTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True while 07 is stargazing.</summary>
        public bool Gazing { get; private set; }

        /// <summary>
        /// The beat starts or ends. Returns true when its start should play the swell: not for a repeat of the same
        /// state, and not within the swell's rest after the last one.
        /// </summary>
        public bool SetGazing(bool gazing)
        {
            if (gazing == Gazing)
            {
                return false;
            }

            Gazing = gazing;
            if (!gazing || _sinceSwell < _tuning.StargazeSwellRest)
            {
                return false;
            }

            _sinceSwell = 0f;
            return true;
        }

        public void Step(float deltaTime)
        {
            _sinceSwell += Mathf.Max(0f, deltaTime);
        }
    }
}
