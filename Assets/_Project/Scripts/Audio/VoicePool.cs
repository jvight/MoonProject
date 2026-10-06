using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// Bookkeeping for a fixed set of one-shot voices: a free voice is reused first; when all are busy the voice
    /// that will finish soonest is stolen (the least audible loss). Times are seconds on any monotonic clock.
    /// </summary>
    public sealed class VoicePool
    {
        private readonly float[] _endTimes;

        public VoicePool(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A voice pool needs voices.");
            }

            _endTimes = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _endTimes[i] = float.NegativeInfinity;
            }
        }

        public int Capacity => _endTimes.Length;

        /// <summary>Claims a voice for <paramref name="duration"/> seconds starting at <paramref name="now"/>.</summary>
        public int Acquire(float now, float duration)
        {
            int chosen = 0;
            float soonest = float.PositiveInfinity;
            for (int i = 0; i < _endTimes.Length; i++)
            {
                if (_endTimes[i] <= now)
                {
                    chosen = i;
                    break;
                }

                if (_endTimes[i] < soonest)
                {
                    soonest = _endTimes[i];
                    chosen = i;
                }
            }

            _endTimes[chosen] = now + Math.Max(0f, duration);
            return chosen;
        }

        public bool IsBusy(int voice, float now)
        {
            return _endTimes[voice] > now;
        }

        public int CountBusy(float now)
        {
            int busy = 0;
            for (int i = 0; i < _endTimes.Length; i++)
            {
                if (_endTimes[i] > now)
                {
                    busy++;
                }
            }

            return busy;
        }

        public void Release(int voice)
        {
            _endTimes[voice] = float.NegativeInfinity;
        }
    }
}
