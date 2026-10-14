using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// When each place or relic answers a listen. A listen sweeps out with an ease-out (fast near 07, slowing as it
    /// reaches the edge of its range), and a place answers a fixed lag after the sweep reaches it, so closer answers
    /// come sooner. Answers are kept in time order; a place already waiting to answer keeps its earlier time.
    /// Preallocated: listening and popping never allocate.
    /// </summary>
    public sealed class SonarSchedule
    {
        private readonly int[] _relic;
        private readonly float[] _time;
        private readonly float[] _distance;

        public SonarSchedule(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity is not negative.");
            }

            _relic = new int[capacity];
            _time = new float[capacity];
            _distance = new float[capacity];
        }

        /// <summary>Answers still to come.</summary>
        public int Pending { get; private set; }

        /// <summary>Seconds after a listen when the sweep reaches <paramref name="distance"/> metres.</summary>
        public static float Arrival(float distance, float duration, float range)
        {
            return range <= 0f ? 0f : duration * Ease.InverseOutQuad(distance / range);
        }

        /// <summary>
        /// Schedules answerer <paramref name="relic"/>, <paramref name="distance"/> metres from a listen made at
        /// <paramref name="pingTime"/>. Returns false when it is out of range.
        /// </summary>
        public bool Add(int relic, float distance, float pingTime, float range, float sweepDuration, float lag)
        {
            if (distance > range)
            {
                return false;
            }

            float time = pingTime + Arrival(distance, sweepDuration, range) + lag;
            for (int i = 0; i < Pending; i++)
            {
                if (_relic[i] != relic)
                {
                    continue;
                }

                if (_time[i] <= time)
                {
                    return true;
                }

                RemoveAt(i);
                break;
            }

            if (Pending == _relic.Length)
            {
                throw new InvalidOperationException($"{nameof(SonarSchedule)} is full ({_relic.Length}).");
            }

            int index = Pending;
            while (index > 0 && _time[index - 1] > time)
            {
                _relic[index] = _relic[index - 1];
                _time[index] = _time[index - 1];
                _distance[index] = _distance[index - 1];
                index--;
            }

            _relic[index] = relic;
            _time[index] = time;
            _distance[index] = distance;
            Pending++;
            return true;
        }

        /// <summary>Takes the earliest answer due at or before <paramref name="now"/>.</summary>
        public bool TryPop(float now, out int relic, out float distance)
        {
            if (Pending == 0 || _time[0] > now)
            {
                relic = -1;
                distance = 0f;
                return false;
            }

            relic = _relic[0];
            distance = _distance[0];
            RemoveAt(0);
            return true;
        }

        /// <summary>Drops an answerer's pending answer (it was dug up or grabbed meanwhile).</summary>
        public void Cancel(int relic)
        {
            for (int i = 0; i < Pending; i++)
            {
                if (_relic[i] == relic)
                {
                    RemoveAt(i);
                    return;
                }
            }
        }

        private void RemoveAt(int index)
        {
            for (int i = index; i < Pending - 1; i++)
            {
                _relic[i] = _relic[i + 1];
                _time[i] = _time[i + 1];
                _distance[i] = _distance[i + 1];
            }

            Pending--;
        }
    }
}
