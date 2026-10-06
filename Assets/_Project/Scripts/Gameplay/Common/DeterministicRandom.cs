namespace MoonProject.Gameplay
{
    /// <summary>
    /// Small seeded xorshift generator for content placement: the same seed always yields the same sequence, so
    /// scrap fields and relic sites are identical on every run (no global random state).
    /// </summary>
    public struct DeterministicRandom
    {
        private const float InverseTwoPow24 = 1f / 16777216f;
        private uint _state;

        public DeterministicRandom(int seed)
        {
            uint x = (uint)seed * 0x9E3779B9u + 0x7F4A7C15u;
            x ^= x >> 16;
            x *= 0x85EBCA6Bu;
            x ^= x >> 13;
            x *= 0xC2B2AE35u;
            x ^= x >> 16;
            _state = x | 1u;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float Next01()
        {
            return (NextUInt() >> 8) * InverseTwoPow24;
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }

        /// <summary>Uniform integer in [<paramref name="min"/>, <paramref name="max"/>] (both inclusive).</summary>
        public int RangeInclusive(int min, int max)
        {
            if (max <= min)
            {
                return min;
            }

            return min + (int)(NextUInt() % (uint)(max - min + 1));
        }
    }
}
