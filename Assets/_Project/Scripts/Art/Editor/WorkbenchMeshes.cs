using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's workbench (docs/STORY.md: he fixed everything, 07 seven times): a sturdy honey-topped bench with a
    /// pegboard of wrenches and seven tally marks, a sage vice, a toolbox, a coffee mug with a chip, a hanging work
    /// lamp, a "07 PIT CREW" sticker on the apron, spare coil and a spare 07 wheel he hid "for next time". Built in its
    /// own space: origin on the ground at the bench centre, +Z = the front where 07 parks, 2.3 m wide.
    /// </summary>
    internal static class WorkbenchMeshes
    {
        public const float Width = 2.3f;
        public const float Depth = 0.9f;
        public const float WorktopHeight = 0.95f;

        /// <summary>Centre of the work lamp's bulb: the pivot of the Lights glow renderer.</summary>
        public static readonly Vector3 LampBulb = new Vector3(0.55f, 2.02f, 0.18f);

        /// <summary>Between the vice jaws, where upgrade sparks fly (+Z out of the bench front).</summary>
        public static readonly Vector3 Sparks = new Vector3(-0.82f, WorktopHeight + 0.19f, 0.32f);

        private const float LegInset = 0.12f;
        private const float BoardZ = -0.42f;
        private const float BoardTop = 2.3f;

        private static readonly Vector2[] MugProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.05f, 0f), new Vector2(0.055f, 0.01f), new Vector2(0.055f, 0.11f),
            new Vector2(0.045f, 0.11f), new Vector2(0.045f, 0.015f), new Vector2(0f, 0.015f),
        };

        public static LowPolyMeshBuilder Bench()
        {
            var b = new LowPolyMeshBuilder(4000);
            Frame(b);
            Pegboard(b);
            Vice(b, new Vector3(-0.82f, WorktopHeight, 0.26f));
            Toolbox(b, new Vector3(0.05f, WorktopHeight, -0.18f));
            Mug(b, new Vector3(0.78f, WorktopHeight, 0.18f));
            LampArm(b);
            Clutter(b);
            b.Box(At(0f, WorktopHeight - 0.155f, Depth * 0.5f - 0.005f), new Vector3(0.86f, 0.12f, 0.008f),
                PaletteSwatch.Cream);
            Matrix4x4 sticker = At(0f, WorktopHeight - 0.155f, Depth * 0.5f);
            // Read from the front, +X is on the reader's left: "07" first, then "PIT CREW".
            Glyphs.Write(b, sticker * At(0.27f, 0f, 0f), "07", 0.075f, PaletteSwatch.WarmAccent, PaletteSwatch.Cream);
            Glyphs.Write(b, sticker * At(-0.1f, 0f, 0f), "PIT CREW", 0.065f, PaletteSwatch.Charcoal,
                PaletteSwatch.Cream);
            return b;
        }

        /// <summary>The bench's rust: collars at the leg feet and streaks down the apron from its bolts.</summary>
        public static LowPolyMeshBuilder Rust()
        {
            var b = new LowPolyMeshBuilder(300);
            float x = Width * 0.5f - LegInset;
            float z = Depth * 0.5f - LegInset;
            for (int i = 0; i < 4; i++)
            {
                Weathering.Collar(b, At(i % 2 == 0 ? -x : x, 0.1f, i < 2 ? z : -z), 0.075f, 0.12f);
            }

            Matrix4x4 apron = SiteKit.Face(new Vector3(0f, 0f, Depth * 0.5f - 0.01f + Weathering.RustLift),
                Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, apron, -0.9f, WorktopHeight - 0.1f, 0.2f);
            SiteKit.RustStreak(b, apron, 0.95f, WorktopHeight - 0.1f, 0.16f);
            return b;
        }

        /// <summary>The work lamp's bulb (the Lights glow renderer); origin = bulb centre.</summary>
        public static LowPolyMeshBuilder LampBulbMesh()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, 0.065f, 1, PaletteSwatch.WarmLamp);
            return b;
        }

        private static void Frame(LowPolyMeshBuilder b)
        {
            float x = Width * 0.5f - LegInset;
            float z = Depth * 0.5f - LegInset;
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? 1f : -1f;
                float legHeight = WorktopHeight - 0.08f;
                b.Box(At(sx * x, legHeight * 0.5f, sz * z), new Vector3(0.1f, legHeight, 0.1f), PaletteSwatch.Metal,
                    0.015f);
                b.Prism(At(sx * x, 0.015f, sz * z), 0.08f, 0.03f, 8, PaletteSwatch.Charcoal);
            }

            b.Box(At(0f, 0.24f, 0f), new Vector3(Width - 0.2f, 0.05f, Depth - 0.12f), PaletteSwatch.Enamel, 0.012f);
            b.Box(At(0f, WorktopHeight - 0.045f, 0f), new Vector3(Width, 0.09f, Depth), PaletteSwatch.Honey, 0.02f);
            b.Box(At(0f, WorktopHeight - 0.075f, Depth * 0.5f + 0.006f), new Vector3(Width, 0.03f, 0.02f),
                PaletteSwatch.Metal);
            b.Box(At(0f, WorktopHeight - 0.155f, Depth * 0.5f - 0.03f), new Vector3(Width - 0.2f, 0.15f, 0.04f),
                PaletteSwatch.Enamel, 0.01f);
        }

        /// <summary>Back pegboard on two posts: wrenches, a hammer, seven tally marks for 07's seven fixes.</summary>
        private static void Pegboard(LowPolyMeshBuilder b)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (Width * 0.5f - 0.08f), (WorktopHeight + BoardTop) * 0.5f, BoardZ),
                    new Vector3(0.08f, BoardTop - WorktopHeight, 0.08f), PaletteSwatch.Metal, 0.012f);
            }

            b.Box(At(0f, 1.62f, BoardZ + 0.02f), new Vector3(Width - 0.2f, 1.1f, 0.03f), PaletteSwatch.Enamel, 0.01f);
            b.Box(At(0f, BoardTop - 0.06f, BoardZ + 0.035f), new Vector3(Width - 0.24f, 0.07f, 0.012f),
                PaletteSwatch.WarmAccent);
            float face = BoardZ + 0.04f;
            for (int i = 0; i < 4; i++)
            {
                float length = 0.36f - i * 0.05f;
                Wrench(b, At(new Vector3(-0.78f + i * 0.17f, 1.72f - (0.36f - length) * 0.5f, face), Vector3.zero),
                    length);
            }

            Matrix4x4 hammer = At(new Vector3(0.05f, 1.7f, face + 0.015f), new Vector3(0f, 0f, -8f));
            b.Box(hammer, new Vector3(0.035f, 0.4f, 0.03f), PaletteSwatch.Honey);
            b.Box(hammer * At(0f, 0.2f, 0f), new Vector3(0.16f, 0.06f, 0.05f), PaletteSwatch.Metal, 0.008f);
            // Seven tally marks, one per time Kenji fixed 07: a crossed five, one more, and the latest in orange.
            for (int i = 0; i < 4; i++)
            {
                b.Box(At(0.48f + i * 0.045f, 1.82f, face + 0.002f), new Vector3(0.012f, 0.16f, 0.006f),
                    PaletteSwatch.Charcoal);
            }

            b.Box(At(new Vector3(0.548f, 1.82f, face + 0.006f), new Vector3(0f, 0f, 60f)),
                new Vector3(0.012f, 0.24f, 0.006f), PaletteSwatch.Charcoal);
            b.Box(At(0.72f, 1.82f, face + 0.002f), new Vector3(0.012f, 0.16f, 0.006f), PaletteSwatch.Charcoal);
            b.Box(At(0.765f, 1.82f, face + 0.002f), new Vector3(0.012f, 0.16f, 0.006f), PaletteSwatch.WarmAccent);
            for (int i = 0; i < 5; i++)
            {
                b.Prism(At(new Vector3(-0.9f + i * 0.45f, 1.25f, face), AlongZ), 0.012f, 0.03f, 6,
                    PaletteSwatch.Charcoal);
            }
        }

        /// <summary>A flat open-ended wrench hanging on the board, pointing down.</summary>
        private static void Wrench(LowPolyMeshBuilder b, Matrix4x4 at, float length)
        {
            b.Box(at, new Vector3(0.03f, length, 0.012f), PaletteSwatch.Metal);
            Vector2[] jaw =
            {
                new Vector2(-0.045f, -0.03f), new Vector2(-0.015f, -0.03f), new Vector2(-0.015f, 0.01f),
                new Vector2(0.015f, 0.01f), new Vector2(0.015f, -0.03f), new Vector2(0.045f, -0.03f),
                new Vector2(0.045f, 0.03f), new Vector2(-0.045f, 0.03f),
            };
            b.Extrude(at * At(0f, -length * 0.5f - 0.02f, 0f), jaw, 0.012f, PaletteSwatch.Metal);
            b.Prism(at * At(new Vector3(0f, length * 0.5f + 0.02f, 0f), AlongZ), 0.035f, 0.012f, 8,
                PaletteSwatch.Metal);
        }

        /// <summary>An old sage bench vice with a T-handle screw, bolted at the front-left corner.</summary>
        private static void Vice(LowPolyMeshBuilder b, Vector3 at)
        {
            b.Box(At(at + new Vector3(0f, 0.03f, 0f)), new Vector3(0.2f, 0.06f, 0.24f), PaletteSwatch.Charcoal, 0.01f);
            b.Box(At(at + new Vector3(0f, 0.12f, -0.03f)), new Vector3(0.16f, 0.12f, 0.16f), PaletteSwatch.Sage,
                0.015f);
            b.Box(At(at + new Vector3(0f, 0.14f, 0.09f)), new Vector3(0.16f, 0.1f, 0.06f), PaletteSwatch.Sage, 0.012f);
            b.Box(At(at + new Vector3(0f, 0.19f, 0.06f)), new Vector3(0.17f, 0.03f, 0.015f), PaletteSwatch.Metal);
            Vector3 screw = at + new Vector3(0f, 0.12f, 0.12f);
            RecipeKit.Rod(b, screw, screw + Vector3.forward * 0.12f, 0.012f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, screw + new Vector3(-0.08f, 0f, 0.12f), screw + new Vector3(0.08f, 0f, 0.12f), 0.009f, 6,
                PaletteSwatch.Metal);
        }

        private static void Toolbox(LowPolyMeshBuilder b, Vector3 at)
        {
            b.Box(At(at + new Vector3(0f, 0.11f, 0f)), new Vector3(0.46f, 0.22f, 0.22f), PaletteSwatch.WarmAccent,
                0.02f);
            b.Box(At(at + new Vector3(0f, 0.17f, 0.112f)), new Vector3(0.46f, 0.012f, 0.006f), PaletteSwatch.Charcoal);
            b.Box(At(at + new Vector3(0f, 0.15f, 0.114f)), new Vector3(0.05f, 0.03f, 0.01f), PaletteSwatch.Metal);
            b.Torus(At(at + new Vector3(0f, 0.22f, 0f), AlongZ, new Vector3(1.4f, 1f, 1f)), 0.06f, 0.012f, 10, 4,
                PaletteSwatch.Charcoal);
        }

        /// <summary>Kenji's coffee mug with a chip out of its rim.</summary>
        private static void Mug(LowPolyMeshBuilder b, Vector3 at)
        {
            MeshRange cup = b.Lathe(At(at), MugProfile, 12, PaletteSwatch.Enamel);
            Vector3 chip = new Vector3(0.6f, 1f, 0.5f).normalized;
            b.Shave(cup, chip, b.Support(cup, chip) - 0.012f);
            b.Prism(At(at + new Vector3(0f, 0.06f, 0f)), 0.0565f, 0.025f, 12, PaletteSwatch.WarmAccent, false);
            b.Torus(At(at + new Vector3(-0.065f, 0.058f, 0f), AlongZ), 0.026f, 0.008f, 8, 3, PaletteSwatch.Enamel);
        }

        /// <summary>An arm from the right post that holds the hanging work lamp's shade over the vice side.</summary>
        private static void LampArm(LowPolyMeshBuilder b)
        {
            var post = new Vector3(Width * 0.5f - 0.08f, BoardTop - 0.02f, BoardZ);
            var elbow = new Vector3(LampBulb.x + 0.05f, BoardTop + 0.04f, LampBulb.z - 0.05f);
            RecipeKit.Rod(b, post, elbow, 0.022f, 6, PaletteSwatch.Metal);
            b.Icosphere(At(elbow), 0.03f, 0, PaletteSwatch.Charcoal);
            Vector3 shadeTop = LampBulb + Vector3.up * 0.13f;
            RecipeKit.Rod(b, elbow, shadeTop, 0.008f, 5, PaletteSwatch.Charcoal);
            b.Frustum(At(LampBulb + Vector3.up * 0.06f), 0.17f, 0.05f, 0.15f, 10, PaletteSwatch.WarmAccent, false);
            b.Prism(At(LampBulb + Vector3.up * 0.135f), 0.05f, 0.02f, 10, PaletteSwatch.Charcoal);
        }

        /// <summary>The spare 07 wheel he hid "for next time", a copper coil and a crate on the lower shelf.</summary>
        private static void Clutter(LowPolyMeshBuilder b)
        {
            Matrix4x4 wheel = At(new Vector3(Width * 0.5f + 0.2f, 0.36f, 0.12f), new Vector3(0f, 12f, -10f));
            b.Prism(wheel * Matrix4x4.Rotate(Rotation(AlongX)), 0.33f, 0.22f, 12, PaletteSwatch.Charcoal);
            b.Prism(wheel * At(new Vector3(0.12f, 0f, 0f), AlongX), 0.22f, 0.03f, 12, PaletteSwatch.Cream);
            b.Frustum(wheel * At(new Vector3(0.145f, 0f, 0f), AlongX), 0.09f, 0.06f, 0.035f, 8, PaletteSwatch.Metal);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(At(-0.55f, 0.29f + i * 0.04f, 0.05f), 0.13f, 0.022f, 12, 4, PaletteSwatch.WarmAccent);
            }

            b.Box(At(new Vector3(0.45f, 0.43f, -0.05f), new Vector3(0f, 8f, 0f)), new Vector3(0.42f, 0.33f, 0.38f),
                PaletteSwatch.Enamel, 0.02f);
            b.Box(At(new Vector3(0.45f, 0.48f, 0.142f), new Vector3(0f, 8f, 0f)), new Vector3(0.2f, 0.06f, 0.01f),
                PaletteSwatch.Charcoal);
        }
    }
}
