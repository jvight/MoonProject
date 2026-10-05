using System;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Deterministic 3D value noise keyed by an integer seed: the same (position, seed) gives the same value on every
    /// run, independent of UnityEngine.Random or any global state.
    /// </summary>
    internal static class SeededNoise
    {
        private const float Lacunarity = 2.03f;
        private const float Gain = 0.5f;
        private const int OctaveSeedStride = 7919;
        private const float InverseTwoPow24 = 1f / 16777216f;

        /// <summary>Fractal sum of <paramref name="octaves"/> value-noise layers, normalised to [-1, 1].</summary>
        public static float Fractal(Vector3 position, int seed, int octaves)
        {
            float sum = 0f;
            float weight = 1f;
            float totalWeight = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += weight * Value(position, seed + i * OctaveSeedStride);
                totalWeight += weight;
                weight *= Gain;
                position *= Lacunarity;
            }

            return sum / totalWeight;
        }

        /// <summary>Smoothly interpolated lattice noise in [-1, 1].</summary>
        public static float Value(Vector3 position, int seed)
        {
            int x0 = (int)Math.Floor(position.x);
            int y0 = (int)Math.Floor(position.y);
            int z0 = (int)Math.Floor(position.z);
            float tx = Fade(position.x - x0);
            float ty = Fade(position.y - y0);
            float tz = Fade(position.z - z0);

            float c000 = Lattice(x0, y0, z0, seed);
            float c100 = Lattice(x0 + 1, y0, z0, seed);
            float c010 = Lattice(x0, y0 + 1, z0, seed);
            float c110 = Lattice(x0 + 1, y0 + 1, z0, seed);
            float c001 = Lattice(x0, y0, z0 + 1, seed);
            float c101 = Lattice(x0 + 1, y0, z0 + 1, seed);
            float c011 = Lattice(x0, y0 + 1, z0 + 1, seed);
            float c111 = Lattice(x0 + 1, y0 + 1, z0 + 1, seed);

            float x00 = c000 + (c100 - c000) * tx;
            float x10 = c010 + (c110 - c010) * tx;
            float x01 = c001 + (c101 - c001) * tx;
            float x11 = c011 + (c111 - c011) * tx;
            float y0Value = x00 + (x10 - x00) * ty;
            float y1Value = x01 + (x11 - x01) * ty;
            return y0Value + (y1Value - y0Value) * tz;
        }

        /// <summary>Avalanching integer hash (murmur3 finaliser over a combined key).</summary>
        public static uint Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA77u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE3Du;
                h = (h << 17) | (h >> 15);
                h ^= (uint)z * 0x27D4EB2Fu;
                h = (h << 11) | (h >> 21);
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }

        private static float Lattice(int x, int y, int z, int seed)
        {
            return (Hash(x, y, z, seed) >> 8) * InverseTwoPow24 * 2f - 1f;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }
    }
}
