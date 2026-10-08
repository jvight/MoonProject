using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Purchases a station has yet to play its crafting moment for, oldest first: bought while an earlier moment is
    /// still playing, each waits its turn. Fixed capacity, allocation-free.
    /// </summary>
    public sealed class PurchaseQueue
    {
        private readonly string[] _ids;
        private readonly int[] _levels;
        private int _head;

        /// <param name="capacity">Every level the station sells: it can never owe more moments than that.</param>
        public PurchaseQueue(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A station sells something.");
            }

            _ids = new string[capacity];
            _levels = new int[capacity];
        }

        public int Count { get; private set; }

        /// <summary>Every level a station selling <paramref name="definitions"/> could ever owe a moment for.</summary>
        public static int CapacityFor(UpgradeDefinition[] definitions)
        {
            int levels = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                levels += definitions[i].MaxLevel;
            }

            return levels;
        }

        public void Enqueue(string upgradeId, int level)
        {
            if (Count == _ids.Length)
            {
                throw new InvalidOperationException($"More purchases waiting than the station sells ({Count}).");
            }

            int tail = (_head + Count) % _ids.Length;
            _ids[tail] = upgradeId;
            _levels[tail] = level;
            Count++;
        }

        /// <summary>Forgets every waiting purchase (a load shows the outcome at once).</summary>
        public void Clear()
        {
            for (int i = 0; i < _ids.Length; i++)
            {
                _ids[i] = null;
            }

            _head = 0;
            Count = 0;
        }

        public bool TryDequeue(out string upgradeId, out int level)
        {
            if (Count == 0)
            {
                upgradeId = null;
                level = 0;
                return false;
            }

            upgradeId = _ids[_head];
            level = _levels[_head];
            _ids[_head] = null;
            _head = (_head + 1) % _ids.Length;
            Count--;
            return true;
        }
    }
}
