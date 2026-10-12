using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The orange glass beads (M3-27 #4, Apollo 17's orange soil): the round woven sack 07 carries them home in, and
    /// the patches of the basin's old vent field, low orange drifts the player sees from 0.5 and digs from 0.6.
    /// Sack space: its bottom centre on the ground. Patch space: its centre on the ground, +Y up.
    /// </summary>
    internal static class OrangeBeadMeshes
    {
        /// <summary>How many patch shapes the field scatters.</summary>
        public const int PatchVariants = 3;

        private const float SackRadius = 0.16f;
        private const float SackHeight = 0.2f;
        private const int WeaveBands = 4;
        private const float BellyHeight = 0.55f;
        private const float NeckWidth = 0.84f;
        private const float BeadRadius = 0.022f;

        // Each patch: its half-length, half-width, how many bead clumps ride it, and whether an old vent mouth shows.
        private static readonly Vector2[] PatchSizes = { new Vector2(1.3f, 0.9f), new Vector2(0.9f, 0.7f),
            new Vector2(1.7f, 0.8f) };
        private static readonly int[] PatchClumps = { 9, 6, 11 };
        private static readonly bool[] PatchVents = { true, false, false };
        private const float PatchRise = 0.09f;
        private const int PatchLobes = 5;
        private const int Seed = 1972;

        /// <summary>
        /// The sack: a round woven bag in faded crew cloth with darker weave bands, bellied and drawn in to a rolled rim with a charcoal
        /// tie under it, heaped with orange beads, so it reads round and orange from the chase where Wiring's coil is
        /// flat and copper.
        /// </summary>
        public static LowPolyMeshBuilder Bundle()
        {
            var b = new LowPolyMeshBuilder(900);
            float belly = SackHeight * BellyHeight;
            Paint cloth = Paint.WithCaps(PaletteSwatch.Cream, PaletteSwatch.Rust);
            b.Frustum(At(Vector3.up * belly * 0.5f), SackRadius * 0.78f, SackRadius, belly, 12, cloth, true,
                new Displacement(Seed, 0.006f, 9f));
            b.Frustum(At(Vector3.up * (belly + (SackHeight - belly) * 0.5f)), SackRadius, SackRadius * NeckWidth,
                SackHeight - belly, 12, cloth, true,
                new Displacement(Seed + 1, 0.006f, 9f));
            for (int i = 0; i < WeaveBands; i++)
            {
                float t = (i + 0.6f) / (WeaveBands + 0.4f);
                float across = t < BellyHeight
                    ? Mathf.Lerp(0.78f, 1f, t / BellyHeight)
                    : Mathf.Lerp(1f, NeckWidth, (t - BellyHeight) / (1f - BellyHeight));
                b.Torus(At(Vector3.up * SackHeight * t), SackRadius * across + 0.002f, 0.006f, 14, 3,
                    PaletteSwatch.FadedPaint);
            }

            float rim = SackRadius * NeckWidth;
            b.Torus(At(Vector3.up * SackHeight), rim + 0.004f, 0.014f, 14, 4, PaletteSwatch.Cream);
            b.Torus(At(Vector3.up * (SackHeight - 0.03f)), rim + 0.006f, 0.008f, 14, 3, PaletteSwatch.Charcoal);
            b.Box(At(new Vector3(rim + 0.01f, SackHeight - 0.055f, 0f), new Vector3(0f, 0f, -12f)),
                new Vector3(0.014f, 0.05f, 0.012f), PaletteSwatch.Charcoal);
            Vector3 mouth = Vector3.up * (SackHeight + 0.005f);
            b.Icosphere(At(mouth, Quaternion.identity, new Vector3(1f, 0.45f, 1f)), rim * 0.98f, 1,
                PaletteSwatch.Rust);
            var random = new System.Random(Seed);
            for (int i = 0; i < 11; i++)
            {
                float angle = i * 2.4f;
                float reach = rim * 0.75f * Mathf.Sqrt((i + 0.5f) / 11f);
                var at = new Vector3(Mathf.Cos(angle) * reach, 0.05f - reach * 0.3f, Mathf.Sin(angle) * reach);
                b.Icosphere(At(mouth + at), BeadRadius * (0.8f + 0.4f * (float)random.NextDouble()), 0,
                    i % 3 == 0 ? PaletteSwatch.Honey : PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>
        /// Patch <paramref name="variant"/>: a low, lobed drift whose top is the beads' rust orange, fading into dust at
        /// its sides, with brighter heaps of fresh beads on it that catch the light like glass; the first carries the
        /// dark lip of an old vent the beads came from.
        /// </summary>
        public static LowPolyMeshBuilder Patch(int variant)
        {
            var b = new LowPolyMeshBuilder(2400);
            Vector2 size = PatchSizes[variant];
            var random = new System.Random(Seed + variant * 31);
            Paint drift = Paint.Facing(PaletteSwatch.Rust, PaletteSwatch.DustMid, Vector3.up, 0.6f);
            for (int lobe = 0; lobe < PatchLobes; lobe++)
            {
                float angle = lobe * Mathf.PI * 2f / PatchLobes + (float)random.NextDouble();
                float reach = lobe == 0 ? 0f : 0.45f + 0.2f * (float)random.NextDouble();
                var at = new Vector3(Mathf.Cos(angle) * size.x * reach, 0f, Mathf.Sin(angle) * size.y * reach);
                float scale = lobe == 0 ? 0.75f : 0.4f + 0.2f * (float)random.NextDouble();
                b.Icosphere(At(at, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f),
                        new Vector3(size.x * scale, PatchRise * (lobe == 0 ? 1f : 0.7f), size.y * scale)), 1f, 2,
                    drift, new Displacement(Seed + variant * 7 + lobe, 0.12f, 2.2f, 2));
            }

            for (int i = 0; i < PatchClumps[variant]; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float reach = 0.7f * Mathf.Sqrt((float)random.NextDouble());
                var at = new Vector3(Mathf.Cos(angle) * size.x * reach, 0f, Mathf.Sin(angle) * size.y * reach);
                float radius = 0.06f + 0.08f * (float)random.NextDouble();
                b.Icosphere(At(at + Vector3.up * PatchRise * 0.7f, Quaternion.identity, new Vector3(1f, 0.6f, 1f)),
                    radius, 1, i % 3 == 0 ? PaletteSwatch.Honey : PaletteSwatch.WarmAccent,
                    new Displacement(Seed + i, radius * 0.2f, 6f));
            }

            if (PatchVents[variant])
            {
                Vector3 vent = new Vector3(size.x * 0.35f, PatchRise * 0.6f, -size.y * 0.2f);
                b.Frustum(At(vent), 0.34f, 0.22f, 0.12f, 9, PaletteSwatch.RockDark, true,
                    new Displacement(Seed + 7, 0.03f, 4f));
                b.Prism(At(vent + Vector3.up * 0.055f), 0.17f, 0.012f, 9, PaletteSwatch.Charcoal);
            }

            return b;
        }
    }
}
