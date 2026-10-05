using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// Shuffle-bag track order: every track plays once per round, rounds are reshuffled, and a round never starts
    /// with the track that ended the previous one (no immediate repeats).
    /// </summary>
    public sealed class PlaylistShuffler
    {
        private readonly int[] _order;
        private readonly AudioRandom _random;
        private int _position;
        private int _last = -1;

        public PlaylistShuffler(int count, AudioRandom random)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Track count cannot be negative.");
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            _order = new int[count];
            _position = count;
        }

        public int Count => _order.Length;

        /// <summary>The next track index, or -1 for an empty playlist.</summary>
        public int Next()
        {
            if (_order.Length == 0)
            {
                return -1;
            }

            if (_position >= _order.Length)
            {
                Reshuffle();
            }

            _last = _order[_position++];
            return _last;
        }

        private void Reshuffle()
        {
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i] = i;
            }

            for (int i = _order.Length - 1; i > 0; i--)
            {
                Swap(i, _random.Range(0, i + 1));
            }

            if (_order.Length > 1 && _order[0] == _last)
            {
                Swap(0, _random.Range(1, _order.Length));
            }

            _position = 0;
        }

        private void Swap(int a, int b)
        {
            int tmp = _order[a];
            _order[a] = _order[b];
            _order[b] = tmp;
        }
    }
}
