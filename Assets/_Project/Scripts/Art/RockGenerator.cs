using System;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Procedural low-poly rocks: a noise-displaced, stretched icosphere, two-toned by face normal (RockLight on
    /// faces looking up, RockDark elsewhere). Deterministic per (seed, size, style). Before placement, the rock's
    /// origin sits on the ground at its horizontal centre with <see cref="BuriedFraction"/> of its height below
    /// y = 0 (drop it straight onto a terrain point) and its horizontal extent (the larger of X and Z) equals the
    /// requested size.
    /// </summary>
    public static class RockGenerator
    {
        /// <summary>Faces whose normal has at least this much +Y get the light rock swatch.</summary>
        public const float LightFacingMinDot = 0.35f;

        private const float UnitRadius = 0.5f;
        private const int StyleSeedStride = 1_000_003;

        /// <summary>Share of a rock's height that lies below its origin (ground contact) before placement.</summary>
        public static float BuriedFraction(RockStyle style)
        {
            return Recipe(style).Buried;
        }

        /// <summary>Builds one rock as a new mesh (allocates a builder; use the builder overload in loops).</summary>
        public static Mesh Build(int seed, float size, RockStyle style)
        {
            var builder = new LowPolyMeshBuilder(Recipe(style).Subdivisions >= 2 ? 320 : 80);
            Build(builder, seed, size, style, Matrix4x4.identity);
            return builder.ToMesh($"Rock_{style}_{seed}");
        }

        /// <summary>
        /// Appends one rock to <paramref name="builder"/> (for batching many rocks into one mesh), placed by
        /// <paramref name="placement"/> after it has been given a seeded yaw, grounded and sized.
        /// </summary>
        public static MeshRange Build(LowPolyMeshBuilder builder, int seed, float size, RockStyle style,
            Matrix4x4 placement)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!(size > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Rock size must be positive.");
            }

            StyleRecipe recipe = Recipe(style);
            var random = new SeededRandom(seed + (int)style * StyleSeedStride);
            var stretch = new Vector3(
                random.Range(recipe.StretchMin.x, recipe.StretchMax.x),
                random.Range(recipe.StretchMin.y, recipe.StretchMax.y),
                random.Range(recipe.StretchMin.z, recipe.StretchMax.z));
            var displacement = new Displacement((int)random.NextUInt(), recipe.Amplitude, recipe.Frequency,
                recipe.Octaves);
            Paint twoTone = Paint.Facing(PaletteSwatch.RockLight, PaletteSwatch.RockDark, Vector3.up,
                LightFacingMinDot);

            float halfYaw = random.Range(0f, Mathf.PI);
            var yaw = new Quaternion(0f, Mathf.Sin(halfYaw), 0f, Mathf.Cos(halfYaw));

            MeshRange range = builder.Icosphere(Matrix4x4.Rotate(yaw) * Matrix4x4.Scale(stretch), UnitRadius,
                recipe.Subdivisions, twoTone, displacement);

            Bounds raw = builder.GetBounds(range);
            var grounded = new Vector3(-raw.center.x, -raw.min.y - recipe.Buried * raw.size.y, -raw.center.z);
            float scale = size / Mathf.Max(raw.size.x, raw.size.z);
            builder.Transform(range, placement * Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(grounded));
            return range;
        }

        private static StyleRecipe Recipe(RockStyle style)
        {
            switch (style)
            {
                case RockStyle.Pebble:
                    return new StyleRecipe(1, new Vector3(1f, 0.5f, 0.8f), new Vector3(1.3f, 0.7f, 1.1f),
                        0.06f, 2f, 1, 0.15f);
                case RockStyle.Rounded:
                    return new StyleRecipe(1, new Vector3(0.9f, 0.65f, 0.9f), new Vector3(1.2f, 0.9f, 1.2f),
                        0.09f, 1.8f, 2, 0.2f);
                case RockStyle.Slab:
                    return new StyleRecipe(1, new Vector3(1.2f, 0.35f, 0.8f), new Vector3(1.6f, 0.5f, 1.1f),
                        0.08f, 2.2f, 2, 0.15f);
                case RockStyle.Jagged:
                    return new StyleRecipe(1, new Vector3(0.8f, 0.9f, 0.8f), new Vector3(1f, 1.3f, 1f),
                        0.14f, 2.4f, 2, 0.15f);
                case RockStyle.Boulder:
                    return new StyleRecipe(2, new Vector3(1f, 0.7f, 1f), new Vector3(1.25f, 0.9f, 1.25f),
                        0.12f, 1.6f, 3, 0.25f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown rock style.");
            }
        }

        private readonly struct StyleRecipe
        {
            public StyleRecipe(int subdivisions, Vector3 stretchMin, Vector3 stretchMax, float amplitude,
                float frequency, int octaves, float buried)
            {
                Subdivisions = subdivisions;
                StretchMin = stretchMin;
                StretchMax = stretchMax;
                Amplitude = amplitude;
                Frequency = frequency;
                Octaves = octaves;
                Buried = buried;
            }

            public int Subdivisions { get; }

            public Vector3 StretchMin { get; }

            public Vector3 StretchMax { get; }

            public float Amplitude { get; }

            public float Frequency { get; }

            public int Octaves { get; }

            public float Buried { get; }
        }
    }
}
