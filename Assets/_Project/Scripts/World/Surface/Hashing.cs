namespace MoonProject.World
{
    /// <summary>
    /// Stateless integer hashes for seeded, order-independent randomness (vertex jitter, per-face palette dither,
    /// placement). Pure functions of their inputs, so results are identical across threads, platforms and runs.
    /// </summary>
    public static class Hashing
    {
        /// <summary>Lowbias32 integer finaliser (Chris Wellons): excellent avalanche for two multiplies.</summary>
        public static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;
            return x;
        }

        public static uint Hash(int x, int y, uint seed)
        {
            return Mix((uint)x * 0x8DA6B343u ^ Mix((uint)y * 0xD8163841u ^ seed));
        }

        public static uint Hash(int x, int y, int z, uint seed)
        {
            return Mix((uint)z * 0xCB1AB31Fu ^ Hash(x, y, seed));
        }

        /// <summary>Maps a hash to [0, 1).</summary>
        public static float ToUnit(uint hash)
        {
            return (hash >> 8) * (1f / 16777216f);
        }

        /// <summary>Maps a hash to [-1, 1).</summary>
        public static float ToSigned(uint hash)
        {
            return ToUnit(hash) * 2f - 1f;
        }
    }
}
