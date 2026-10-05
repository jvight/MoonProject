namespace MoonProject.Art
{
    /// <summary>Small deterministic xorshift generator for procedural recipes (no global state).</summary>
    internal struct SeededRandom
    {
        private const float InverseTwoPow24 = 1f / 16777216f;
        private uint _state;

        public SeededRandom(int seed)
        {
            _state = SeededNoise.Hash(seed, 0x5EED, 0x0A27, 0x7A1E) | 1u;
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
    }
}
