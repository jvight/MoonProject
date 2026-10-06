using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The pickup melody: the first pickup of a chain is step 0, and every pickup that follows within
    /// <see cref="Window"/> seconds of the previous one climbs a step. A longer pause starts a new chain.
    /// </summary>
    public sealed class ComboCounter
    {
        private float _lastTime;
        private bool _hasLast;

        public ComboCounter(float window)
        {
            if (window < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(window), window, "The combo window is not negative.");
            }

            Window = window;
        }

        public float Window { get; }

        /// <summary>Step of the most recent pickup (0 before any pickup).</summary>
        public int Step { get; private set; }

        /// <summary>Records a pickup at <paramref name="time"/> (seconds) and returns its melody step.</summary>
        public int Register(float time)
        {
            Step = _hasLast && time - _lastTime <= Window ? Step + 1 : 0;
            _lastTime = time;
            _hasLast = true;
            return Step;
        }
    }
}
