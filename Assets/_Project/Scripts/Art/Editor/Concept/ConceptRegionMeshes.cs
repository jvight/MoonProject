using System;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Blockout geometry for the region mood frames (preview only): a faceted heightfield, the mare's comms dish, the
    /// crater's frost, the lava tube's vault and the Peak's great dish.
    /// </summary>
    internal static class ConceptRegionMeshes
    {
        /// <summary>
        /// A square of ground <paramref name="size"/> across centred on <paramref name="centre"/>, in
        /// <paramref name="cells"/> by <paramref name="cells"/> faceted quads: heights and paint from world X, Z.
        /// </summary>
        public static LowPolyMeshBuilder Heightfield(Vector3 centre, float size, int cells,
            Func<float, float, float> height, Func<float, float, float, PaletteSwatch> paint)
        {
            var b = new LowPolyMeshBuilder(cells * cells * 2);
            float step = size / cells;
            var corner = new Vector3[4];
            var triangle = new Vector3[3];
            for (int i = 0; i < cells; i++)
            {
                for (int j = 0; j < cells; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        float x = centre.x - size * 0.5f + (i + (k == 1 || k == 2 ? 1 : 0)) * step;
                        float z = centre.z - size * 0.5f + (j + (k >= 2 ? 1 : 0)) * step;
                        corner[k] = new Vector3(x, height(x, z), z);
                    }

                    for (int t = 0; t < 2; t++)
                    {
                        triangle[0] = corner[0];
                        triangle[1] = corner[t == 0 ? 2 : 3];
                        triangle[2] = corner[t == 0 ? 1 : 2];
                        Vector3 mid = (triangle[0] + triangle[1] + triangle[2]) / 3f;
                        b.Polygon(triangle, Vector3.up, paint(mid.x, mid.z, mid.y));
                    }
                }
            }

            return b;
        }

        /// <summary>
        /// The crew's old comms dish: a lattice mast about 22 m, a tilted dish and its amber lamp on top.
        /// </summary>
        public static LowPolyMeshBuilder CommsDish()
        {
            var b = new LowPolyMeshBuilder(1200);
            Lattice(b, 22f, 2.2f);
            Dish(b, At(new Vector3(0f, 21f, 1.5f), new Vector3(55f, 0f, 0f)), 8f, PaletteSwatch.WornPanel);
            b.Icosphere(At(0f, 26f, 0f), 0.6f, 1, PaletteSwatch.WarmLamp);
            return b;
        }

        /// <summary>
        /// The Peak's great dish: a blocky station, a tall lattice and a wide dish, its red beacon above.
        /// </summary>
        public static LowPolyMeshBuilder GreatDish()
        {
            var b = new LowPolyMeshBuilder(1600);
            b.Box(At(0f, 3f, 0f), new Vector3(14f, 6f, 10f), PaletteSwatch.FadedPaint, 0.4f);
            b.Box(At(0f, 6.2f, 0f), new Vector3(14.4f, 0.5f, 10.4f), PaletteSwatch.FadedAccent);
            Lattice(b, 26f, 3f);
            Dish(b, At(new Vector3(0f, 25f, 2f), new Vector3(40f, 0f, 0f)), 14f, PaletteSwatch.Cream);
            SiteKit.Bar(b, new Vector3(0f, 26f, -1f), new Vector3(0f, 41f, -1f), 0.3f, PaletteSwatch.Metal);
            b.Icosphere(At(0f, 42f, -1f), 1.4f, 1, PaletteSwatch.SignalGlass);
            return b;
        }

        /// <summary>
        /// Frost specks, <paramref name="count"/> small pale facets strewn within <paramref name="radius"/> of the
        /// root: they catch only a lamp's light, so they glitter where 07's floods reach and stay dark elsewhere.
        /// </summary>
        public static LowPolyMeshBuilder Frost(int count, float radius, int seed)
        {
            var b = new LowPolyMeshBuilder(count * 12);
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)random.NextDouble()) * radius;
                var at = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                var tilt = new Vector3((float)random.NextDouble() * 70f, (float)random.NextDouble() * 360f, 0f);
                b.Box(At(at, tilt), new Vector3(0.05f, 0.005f, 0.04f), PaletteSwatch.Cream);
            }

            return b;
        }

        /// <summary>
        /// The lava tube's vault, <paramref name="length"/> along +Z from the root: a flat basalt floor and a half
        /// ring of thick slabs <paramref name="radius"/> out (seen from inside), open at the roof over
        /// <paramref name="skylight"/> for the skylight, with crystals glinting in the walls.
        /// </summary>
        public static LowPolyMeshBuilder Tube(float radius, float length, float skylight)
        {
            const int Slabs = 18;
            const float RingStep = 5f;
            var b = new LowPolyMeshBuilder(4000);
            b.Box(At(0f, -0.3f, length * 0.5f), new Vector3(radius * 2.4f, 0.6f, length), PaletteSwatch.RockDark);
            var random = new System.Random(77);
            for (float z = 0f; z < length; z += RingStep)
            {
                bool open = Mathf.Abs(z - skylight) < 7f;
                for (int s = 0; s < Slabs; s++)
                {
                    float degrees = s * 180f / (Slabs - 1);
                    if (open && degrees > 60f && degrees < 120f)
                    {
                        continue;
                    }

                    float angle = degrees * Mathf.Deg2Rad;
                    float r = radius + (float)random.NextDouble() * 0.8f;
                    var at = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * 0.8f, z);
                    b.Box(At(at, new Vector3(0f, 0f, degrees - 90f)), new Vector3(4.4f, 2.6f, RingStep + 0.8f),
                        PaletteSwatch.RockDark, 0.2f);
                    if (random.NextDouble() < 0.35)
                    {
                        Vector3 inward = -new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.2f;
                        PaletteSwatch crystal = random.NextDouble() < 0.5 ? PaletteSwatch.EyeGlass :
                            PaletteSwatch.BiolumMagenta;
                        b.Cone(At(at + inward, new Vector3(0f, 0f, degrees + 90f)), 0.35f, 1.4f, 5, crystal);
                    }
                }
            }

            b.Box(At(0f, radius * 0.4f, length + 1f), new Vector3(radius * 2.6f, radius * 1.2f, 2f),
                PaletteSwatch.RockDark);
            return b;
        }

        /// <summary>
        /// Dust motes hanging in a skylight's shaft: <paramref name="count"/> pale specks in a column
        /// <paramref name="radius"/> wide and <paramref name="height"/> tall, lit only where the sunlight falls, so
        /// the shaft reads as a column of light.
        /// </summary>
        public static LowPolyMeshBuilder Motes(int count, float radius, float height, int seed)
        {
            var b = new LowPolyMeshBuilder(count * 12);
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)random.NextDouble()) * radius;
                var at = new Vector3(Mathf.Cos(angle) * r, (float)random.NextDouble() * height, Mathf.Sin(angle) * r);
                b.Box(At(at, new Vector3(i * 37f, i * 53f, 0f)), Vector3.one * 0.06f, PaletteSwatch.Cream);
            }

            return b;
        }

        /// <summary>
        /// A switchback road up a slope, its legs crossing between <paramref name="left"/> and
        /// <paramref name="right"/> (world X) from <paramref name="foot"/> to <paramref name="top"/> (world Z): pale
        /// graded segments laid on <paramref name="height"/>, with a warm lamp post at every bend.
        /// </summary>
        public static LowPolyMeshBuilder Switchbacks(Func<float, float, float> height, float left, float right,
            float foot, float top, int legs)
        {
            var b = new LowPolyMeshBuilder(legs * 120);
            float rise = (top - foot) / legs;
            for (int leg = 0; leg < legs; leg++)
            {
                float fromX = leg % 2 == 0 ? left : right;
                float toX = leg % 2 == 0 ? right : left;
                var from = new Vector3(fromX, 0f, foot + leg * rise);
                var to = new Vector3(toX, 0f, foot + (leg + 1) * rise);
                const int Steps = 14;
                for (int s = 0; s < Steps; s++)
                {
                    Vector3 a = Vector3.Lerp(from, to, (float)s / Steps);
                    Vector3 c = Vector3.Lerp(from, to, (float)(s + 1) / Steps);
                    a.y = height(a.x, a.z) + 0.3f;
                    c.y = height(c.x, c.z) + 0.3f;
                    b.Box(Along(a, c), new Vector3(7f, 0.5f, Vector3.Distance(a, c) + 0.6f), PaletteSwatch.DustLight);
                }

                var bend = new Vector3(toX, height(toX, to.z) + 0.3f, to.z);
                SiteKit.Bar(b, bend, bend + Vector3.up * 4f, 0.3f, PaletteSwatch.Metal);
                b.Icosphere(At(bend + Vector3.up * 4.3f), 0.6f, 1, PaletteSwatch.WarmLamp);
            }

            return b;
        }

        // A square lattice mast of four legs with cross bracing, about height high and width across at its foot.
        private static void Lattice(LowPolyMeshBuilder b, float height, float width)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    SiteKit.Bar(b, new Vector3(x * width * 0.5f, 0f, z * width * 0.5f),
                        new Vector3(x * 0.4f, height, z * 0.4f), 0.25f, PaletteSwatch.Metal);
                }
            }

            for (float y = 3f; y < height; y += 4f)
            {
                float half = Mathf.Lerp(width * 0.5f, 0.4f, y / height);
                b.Box(At(0f, y, half), new Vector3(half * 2f, 0.15f, 0.15f), PaletteSwatch.Metal);
                b.Box(At(0f, y, -half), new Vector3(half * 2f, 0.15f, 0.15f), PaletteSwatch.Metal);
                b.Box(At(half, y, 0f), new Vector3(0.15f, 0.15f, half * 2f), PaletteSwatch.Metal);
                b.Box(At(-half, y, 0f), new Vector3(0.15f, 0.15f, half * 2f), PaletteSwatch.Metal);
            }
        }

        // A shallow dish of the given radius, its open face along the placement's +Y, with a feed horn on struts.
        private static void Dish(LowPolyMeshBuilder b, Matrix4x4 placement, float radius, PaletteSwatch paint)
        {
            b.Lathe(placement, new[]
            {
                new Vector2(0.001f, 0f), new Vector2(radius * 0.5f, radius * 0.07f),
                new Vector2(radius, radius * 0.28f),
                new Vector2(radius * 0.97f, radius * 0.3f), new Vector2(radius * 0.48f, radius * 0.1f),
                new Vector2(0.001f, radius * 0.04f),
            }, 16, paint);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 rim = placement.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle) * radius * 0.9f, radius * 0.27f,
                    Mathf.Sin(angle) * radius * 0.9f));
                Vector3 feed = placement.MultiplyPoint3x4(new Vector3(0f, radius * 0.8f, 0f));
                SiteKit.Bar(b, rim, feed, radius * 0.02f, PaletteSwatch.Metal);
            }

            b.Prism(placement * At(0f, radius * 0.8f, 0f), radius * 0.06f, radius * 0.12f, 8, PaletteSwatch.Charcoal);
        }
    }
}
