using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// Shuffle-bag track order: every track plays once per round, rounds are reshuffled, and a round never starts
    /// with the track that ended the previous one (no immediate repeats). The pool can grow or shrink up to its
    /// capacity (a collected tape joins the show) without allocating; existing indices keep their meaning.
    /// </summary>
    public sealed class PlaylistShuffler
    {
        private readonly int[] _order;
        private readonly AudioRandom _random;
        private int _count;
        private int _position;
        private int _last = -1;

        public PlaylistShuffler(int capacity, AudioRandom random)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            _order = new int[capacity];
            _count = capacity;
            _position = capacity;
        }

        /// <summary>Tracks currently in the pool.</summary>
        public int Count => _count;

        public int Capacity => _order.Length;

        /// <summary>
        /// Changes how many tracks are in the pool (0..capacity). The next pick starts a fresh round, still never
        /// repeating the track that played last.
        /// </summary>
        public void Resize(int count)
        {
            if (count < 0 || count > _order.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"Pool size must be 0..{_order.Length}.");
            }

            _count = count;
            _position = count;
        }

        /// <summary>The next track index, or -1 for an empty pool.</summary>
        public int Next()
        {
            if (_count == 0)
            {
                return -1;
            }

            if (_position >= _count)
            {
                Reshuffle();
            }

            _last = _order[_position++];
            return _last;
        }

        private void Reshuffle()
        {
            for (int i = 0; i < _count; i++)
            {
                _order[i] = i;
            }

            for (int i = _count - 1; i > 0; i--)
            {
                Swap(i, _random.Range(0, i + 1));
            }

            if (_count > 1 && _order[0] == _last)
            {
                Swap(0, _random.Range(1, _count));
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
