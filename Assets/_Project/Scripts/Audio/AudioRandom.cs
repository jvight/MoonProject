namespace MoonProject.Audio
{
    /// <summary>
    /// Small deterministic xorshift32 generator for variance picking (no shared global state, testable).
    /// </summary>
    public sealed class AudioRandom
    {
        private const uint FallbackSeed = 0x9E3779B9u;
        private const float InverseTwoPow24 = 1f / 16777216f;

        private uint _state;

        public AudioRandom(uint seed)
        {
            _state = seed == 0u ? FallbackSeed : seed;
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

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * InverseTwoPow24;
        }

        /// <summary>Uniform in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive); minInclusive for an empty range.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            int span = maxExclusive - minInclusive;
            return span <= 0 ? minInclusive : minInclusive + (int)(NextUInt() % (uint)span);
        }

        /// <summary>A uniform index in [0, count) that differs from <paramref name="avoid"/> whenever count > 1.</summary>
        public int PickAvoiding(int count, int avoid)
        {
            if (count <= 1)
            {
                return 0;
            }

            if (avoid < 0 || avoid >= count)
            {
                return Range(0, count);
            }

            int index = Range(0, count - 1);
            return index >= avoid ? index + 1 : index;
        }
    }
}
