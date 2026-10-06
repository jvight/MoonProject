using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Keeps pickups at least <see cref="MinInterval"/> apart so a whole cluster arriving at once plays as an
    /// arpeggio rather than a chord. A piece that cannot claim a slot waits a moment at the cargo socket.
    /// </summary>
    public sealed class PickupCadence
    {
        private float _lastTime;
        private bool _hasLast;

        public PickupCadence(float minInterval)
        {
            if (minInterval < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minInterval), minInterval, "Interval is not negative.");
            }

            MinInterval = minInterval;
        }

        public float MinInterval { get; }

        /// <summary>True (and the slot is taken) when a pickup may happen at <paramref name="time"/>.</summary>
        public bool TryClaim(float time)
        {
            if (_hasLast && time - _lastTime < MinInterval)
            {
                return false;
            }

            _lastTime = time;
            _hasLast = true;
            return true;
        }
    }
}
