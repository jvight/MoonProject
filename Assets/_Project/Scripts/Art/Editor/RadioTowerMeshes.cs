using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The base radio tower in three upgrade stages that read apart from 50 m by height, dish size and lights:
    /// L1 a cobbled pole with a small dish and one bulb, L2 a lattice mast with a dish platform and four lamps,
    /// L3 a tall lattice tower with two dishes, two platforms and rings of lamps. Every stage stands on the same
    /// 1.6 m plinth with the old radio cabinet; origin on the ground at the plinth centre, +Z = the cabinet front.
    /// </summary>
    internal static class RadioTowerMeshes
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 3;
        public const float PlinthSize = 1.6f;
        public const float PlinthHeight = 0.16f;

        private static readonly Vector2[] DishProfile =
        {
            new Vector2(0f, -0.24f), new Vector2(0.5f, -0.17f), new Vector2(1f, 0.12f), new Vector2(0.92f, 0.14f),
            new Vector2(0.48f, -0.07f), new Vector2(0f, -0.12f),
        };

        /// <summary>Top of the tower: where the beacon (gameplay's signal light) sits.</summary>
        public static Vector3 BeaconPosition(int level)
        {
            switch (RequireLevel(level))
            {
                case 1:
                    return new Vector3(0f, 3.78f, -0.3f);
                case 2:
                    return new Vector3(0f, 6.55f, 0f);
                default:
                    return new Vector3(0f, 10f, 0f);
            }
        }

        public static LowPolyMeshBuilder Structure(int level)
        {
            var b = new LowPolyMeshBuilder(2400);
            Plinth(b);
            switch (RequireLevel(level))
            {
                case 1:
                    Pole(b);
                    break;
                case 2:
                    Mast(b);
                    break;
                default:
                    Tower(b);
                    break;
            }

            return b;
        }

        /// <summary>The stage's warm lamps: one bulb, then four, then a ring and a crown.</summary>
        public static LowPolyMeshBuilder Lights(int level)
        {
            var b = new LowPolyMeshBuilder(400);
            b.Box(At(0.17f, 0.85f, 0.5f), new Vector3(0.05f, 0.05f, 0.02f), PaletteSwatch.WarmLamp);
            switch (RequireLevel(level))
            {
                case 1:
                    b.Icosphere(At(0f, 3.62f, -0.3f), 0.07f, 1, PaletteSwatch.WarmLamp);
                    break;
                case 2:
                    Corners(b, 3.55f, 0.6f, 0.085f, PaletteSwatch.WarmLamp);
                    b.Icosphere(At(0f, 6.4f, 0f), 0.11f, 1, PaletteSwatch.WarmLamp);
                    break;
                default:
                    Corners(b, 3.35f, 0.62f, 0.085f, PaletteSwatch.PilotLight);
                    Corners(b, 6.75f, 0.58f, 0.1f, PaletteSwatch.WarmLamp);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        b.Icosphere(At(0.42f * Mathf.Sin(angle), 9.45f, 0.42f * Mathf.Cos(angle)), 0.08f, 1,
                            PaletteSwatch.PilotLight);
                    }

                    b.Icosphere(At(0f, 9.87f, 0f), 0.13f, 1, PaletteSwatch.WarmLamp);
                    break;
            }

            return b;
        }

        private static int RequireLevel(int level)
        {
            if (level < MinLevel || level > MaxLevel)
            {
                throw new System.ArgumentOutOfRangeException(nameof(level), level, "Radio tower levels are 1..3.");
            }

            return level;
        }

        /// <summary>The shared footing: a metal plinth and the old radio cabinet that the tower amplifies.</summary>
        private static void Plinth(LowPolyMeshBuilder b)
        {
            b.Box(At(0f, PlinthHeight * 0.5f, 0f), new Vector3(PlinthSize, PlinthHeight, PlinthSize),
                PaletteSwatch.Metal, 0.03f);
            Matrix4x4 cabinet = At(0f, PlinthHeight + 0.37f, 0.27f);
            b.Box(cabinet, new Vector3(0.62f, 0.74f, 0.42f), PaletteSwatch.Enamel, 0.06f);
            b.Box(cabinet * At(0f, 0.2f, 0f), new Vector3(0.632f, 0.07f, 0.432f), PaletteSwatch.WarmAccent);
            for (int i = 0; i < 4; i++)
            {
                b.Box(cabinet * At(-0.1f, -0.06f - i * 0.045f, 0.215f), new Vector3(0.26f, 0.018f, 0.012f),
                    PaletteSwatch.Charcoal);
            }

            b.Prism(cabinet * At(new Vector3(0.17f, -0.1f, 0.22f), AlongZ), 0.07f, 0.03f, 10, PaletteSwatch.Charcoal);
            b.Box(cabinet * At(new Vector3(0.17f, -0.08f, 0.24f), new Vector3(0f, 0f, -30f)),
                new Vector3(0.012f, 0.06f, 0.01f), PaletteSwatch.Honey);
        }

        /// <summary>L1: a cobbled pole with a cable wound down it, a small dish, a crossbar and one bulb.</summary>
        private static void Pole(LowPolyMeshBuilder b)
        {
            var foot = new Vector3(0f, PlinthHeight, -0.3f);
            var top = new Vector3(0f, 3.5f, -0.3f);
            RecipeKit.Rod(b, foot, top, 0.06f, 8, PaletteSwatch.Metal);
            RecipeKit.Rod(b, foot + new Vector3(0.09f, 0f, 0.02f), foot + new Vector3(0.07f, 1.7f, 0f), 0.035f, 6,
                PaletteSwatch.Metal);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(At(foot + Vector3.up * (0.6f + i * 0.5f)), 0.1f, 0.02f, 8, 3, PaletteSwatch.Charcoal);
            }

            Vector3 previous = foot + new Vector3(0.07f, 0.2f, 0.05f);
            for (int i = 1; i <= 6; i++)
            {
                float angle = i * 1.9f;
                var offset = new Vector3(0.075f * Mathf.Sin(angle), 0.2f + i * 0.45f, 0.075f * Mathf.Cos(angle));
                Vector3 next = foot + offset;
                RecipeKit.Rod(b, previous, next, 0.015f, 5, PaletteSwatch.Charcoal);
                previous = next;
            }

            Dish(b, foot + new Vector3(0f, 2.45f, 0.12f), new Vector3(0f, 0f, 0f), 0.4f);
            Vector3 bar = top + Vector3.down * 0.25f;
            RecipeKit.Rod(b, bar + Vector3.left * 0.42f, bar + Vector3.right * 0.42f, 0.025f, 6, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 end = bar + Vector3.right * (side * 0.4f);
                RecipeKit.Rod(b, end, end + new Vector3(0f, -0.45f, 0.03f), 0.012f, 5, PaletteSwatch.Metal);
            }

            b.Frustum(At(top + Vector3.up * 0.2f), 0.13f, 0.05f, 0.08f, 8, PaletteSwatch.Charcoal);
        }

        /// <summary>L2: a four-leg lattice mast with a mid platform, a medium dish, lamps and a whip.</summary>
        private static void Mast(LowPolyMeshBuilder b)
        {
            const float top = 6.2f;
            Lattice(b, PlinthHeight, top, 0.62f, 0.28f, 4);
            Platform(b, 3.4f, 0.66f);
            Dish(b, new Vector3(0f, 4.05f, 0.75f), new Vector3(-8f, 0f, 0f), 0.62f);
            b.Box(At(0f, top + 0.08f, 0f), new Vector3(0.7f, 0.12f, 0.7f), PaletteSwatch.Metal, 0.02f);
            b.Frustum(At(0f, top + 0.27f, 0f), 0.14f, 0.06f, 0.1f, 8, PaletteSwatch.Charcoal);
            b.Box(At(0f, 5.3f, 0.34f), new Vector3(0.42f, 0.16f, 0.012f), PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, new Vector3(0.25f, top + 0.14f, -0.25f), new Vector3(0.3f, top + 1.1f, -0.3f), 0.015f, 5,
                PaletteSwatch.Metal);
        }

        /// <summary>L3: a tall tapering lattice tower, two platforms, two dishes and a lamp crown.</summary>
        private static void Tower(LowPolyMeshBuilder b)
        {
            const float top = 9.55f;
            Lattice(b, PlinthHeight, top, 0.66f, 0.22f, 6);
            Platform(b, 3.2f, 0.7f);
            Platform(b, 6.6f, 0.66f);
            Dish(b, new Vector3(0f, 7.35f, 0.85f), new Vector3(-15f, 0f, 0f), 1f);
            Dish(b, new Vector3(-0.72f, 3.75f, 0f), new Vector3(0f, -90f, 0f), 0.45f);
            b.Box(At(0f, top + 0.06f, 0f), new Vector3(0.6f, 0.1f, 0.6f), PaletteSwatch.Metal, 0.02f);
            b.Torus(At(0f, 9.45f, 0f), 0.42f, 0.025f, 12, 3, PaletteSwatch.Metal);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var foot = new Vector3(0.24f * Mathf.Sin(angle), top + 0.1f, 0.24f * Mathf.Cos(angle));
                RecipeKit.Rod(b, foot, new Vector3(0f, top + 0.45f, 0f), 0.018f, 5, PaletteSwatch.Metal);
            }

            b.Frustum(At(0f, top + 0.5f, 0f), 0.16f, 0.07f, 0.1f, 8, PaletteSwatch.Charcoal);
            b.Box(At(0f, 8.4f, 0.24f), new Vector3(0.46f, 0.18f, 0.012f), PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, new Vector3(0.22f, top + 0.12f, -0.22f), new Vector3(0.28f, top + 1.5f, -0.28f), 0.016f,
                5, PaletteSwatch.Metal);
        }

        /// <summary>
        /// Four tapering legs (half-spread <paramref name="bottomHalf"/> to <paramref name="topHalf"/>) with an
        /// X-brace and a ring on every face of every segment.
        /// </summary>
        private static void Lattice(LowPolyMeshBuilder b, float bottom, float top, float bottomHalf, float topHalf,
            int segments)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                RecipeKit.Strut(b, Corner(corner, bottom, bottomHalf), Corner(corner, top, topHalf),
                    new Vector2(0.08f, 0.08f), PaletteSwatch.Metal);
            }

            for (int segment = 0; segment < segments; segment++)
            {
                float y0 = Mathf.Lerp(bottom, top, (float)segment / segments);
                float y1 = Mathf.Lerp(bottom, top, (float)(segment + 1) / segments);
                float half0 = Mathf.Lerp(bottomHalf, topHalf, (float)segment / segments);
                float half1 = Mathf.Lerp(bottomHalf, topHalf, (float)(segment + 1) / segments);
                for (int face = 0; face < 4; face++)
                {
                    int next = (face + 1) % 4;
                    Brace(b, Corner(face, y0, half0), Corner(next, y1, half1));
                    Brace(b, Corner(next, y0, half0), Corner(face, y1, half1));
                    Brace(b, Corner(face, y1, half1), Corner(next, y1, half1));
                }
            }
        }

        private static Vector3 Corner(int corner, float y, float half)
        {
            float x = corner == 0 || corner == 3 ? half : -half;
            float z = corner < 2 ? half : -half;
            return new Vector3(x, y, z);
        }

        private static void Brace(LowPolyMeshBuilder b, Vector3 from, Vector3 to)
        {
            b.Box(Along(from, to), new Vector3(0.035f, 0.035f, Vector3.Distance(from, to)), PaletteSwatch.Metal);
        }

        private static void Platform(LowPolyMeshBuilder b, float y, float half)
        {
            b.Box(At(0f, y, 0f), new Vector3(2f * half + 0.2f, 0.05f, 2f * half + 0.2f), PaletteSwatch.Charcoal,
                0.01f);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 post = Corner(corner, y, half + 0.08f);
                Vector3 nextPost = Corner((corner + 1) % 4, y, half + 0.08f);
                RecipeKit.Rod(b, post + Vector3.up * 0.4f, nextPost + Vector3.up * 0.4f, 0.015f, 5,
                    PaletteSwatch.WarmAccent);
                RecipeKit.Rod(b, post, post + Vector3.up * 0.42f, 0.018f, 5, PaletteSwatch.Metal);
            }
        }

        /// <summary>A dish on a short boom, facing +Z after <paramref name="euler"/> (0 = straight ahead).</summary>
        private static void Dish(LowPolyMeshBuilder b, Vector3 centre, Vector3 euler, float radius)
        {
            Matrix4x4 frame = At(centre, euler);
            b.Lathe(frame * At(Vector3.zero, AlongZ, Vector3.one * radius), DishProfile, 12, PaletteSwatch.Enamel);
            RecipeKit.Rod(b, frame.MultiplyPoint3x4(new Vector3(0f, 0f, -0.2f * radius)),
                frame.MultiplyPoint3x4(new Vector3(0f, 0f, 0.55f * radius)), 0.02f, 5, PaletteSwatch.Metal);
            b.Icosphere(frame * At(0f, 0f, 0.58f * radius), 0.04f, 0, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, frame.MultiplyPoint3x4(new Vector3(0f, 0f, -0.2f * radius)),
                frame.MultiplyPoint3x4(new Vector3(0f, 0f, -0.2f * radius - 0.35f)), 0.035f, 6, PaletteSwatch.Metal);
        }

        private static void Corners(LowPolyMeshBuilder b, float y, float half, float radius, PaletteSwatch swatch)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                b.Icosphere(At(Corner(corner, y, half)), radius, 1, swatch);
            }
        }
    }
}
