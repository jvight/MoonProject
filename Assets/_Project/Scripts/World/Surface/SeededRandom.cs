namespace MoonProject.World
{
    /// <summary>
    /// Small deterministic PCG32 generator. Used instead of System.Random / UnityEngine.Random so that world
    /// generation is bit-identical across runtimes (Mono, IL2CPP, .NET) and never touches global state.
    /// </summary>
    public struct SeededRandom
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong Increment = 1442695040888963407UL;

        private ulong _state;

        public SeededRandom(uint seed)
        {
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = old * Multiplier + Increment;
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        /// <summary>Uniform in [<paramref name="min"/>, <paramref name="max"/>).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Uniform integer in [<paramref name="min"/>, <paramref name="maxExclusive"/>).</summary>
        public int Range(int min, int maxExclusive)
        {
            return min + (int)(NextUInt() % (uint)(maxExclusive - min));
        }
    }
}
