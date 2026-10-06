using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Allocation-free text for small non-negative numbers: each string is built once, on first use, and reused, so
    /// counting labels never allocate per frame.
    /// </summary>
    internal sealed class IntText
    {
        private readonly string[] _cache;

        /// <param name="capacity">Numbers 0..capacity-1 are cached; larger ones still work but allocate.</param>
        public IntText(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
            }

            _cache = new string[capacity];
        }

        public string Get(int value)
        {
            if (value < 0 || value >= _cache.Length)
            {
                return value.ToString();
            }

            return _cache[value] ?? (_cache[value] = value.ToString());
        }
    }
}
