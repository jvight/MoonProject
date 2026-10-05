using System;

namespace MoonProject.Art
{
    /// <summary>
    /// Seeded, deterministic noise displacement applied to a primitive's shared vertices before it is placed and
    /// flattened, so the surface stays watertight. Each vertex moves along its smoothed normal by
    /// <see cref="Amplitude"/> * fractal value noise sampled at (local position * <see cref="Frequency"/>), noise in
    /// [-1, 1]. Units are the primitive's local units (before placement scale). <c>default</c> means no displacement.
    /// </summary>
    public readonly struct Displacement
    {
        public const int MaxOctaves = 6;

        public Displacement(int seed, float amplitude, float frequency, int octaves = 1)
        {
            if (frequency <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Frequency must be positive.");
            }

            if (octaves < 1 || octaves > MaxOctaves)
            {
                throw new ArgumentOutOfRangeException(nameof(octaves), octaves, $"Octaves must be 1..{MaxOctaves}.");
            }

            Seed = seed;
            Amplitude = amplitude;
            Frequency = frequency;
            Octaves = octaves;
        }

        public int Seed { get; }

        public float Amplitude { get; }

        public float Frequency { get; }

        public int Octaves { get; }

        public bool IsNone => Amplitude == 0f || Octaves == 0;
    }
}
