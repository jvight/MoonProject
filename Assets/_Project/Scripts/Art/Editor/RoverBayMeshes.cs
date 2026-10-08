using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's Rover Bay (docs/features/M3-14-built-for-07.md, VISION ruling 14: 07 has no hands): a drive-in service
    /// bay the crew built for a rover. A sheltered frame open at the front, a low ramp up to a turntable sized for
    /// 07 with room all round, three gantry arms hanging from the overhead rail that fit kit onto 07 with welding
    /// tips, a material hopper at rover height by the entrance that 07's beam feeds, two work lamps, a cable reel, a
    /// "PIT 07" board, and Kenji's old human bench left beside it as a remnant. Built in its own space: origin on the
    /// ground at WorkshopAnchor, +Z = the open front 07 drives in from. Long left alone (ruling 12): faded paint, a
    /// roof panel gone, rust and dust in its weather layers.
    /// </summary>
    internal static class RoverBayMeshes
    {
        public const int ArmCount = 3;
        public const int LampCount = 2;

        /// <summary>Half the inside width and depth of the frame (between the post centres).</summary>
        public const float HalfWidth = 2.2f;

        public const float FrontZ = 1.9f;
        public const float BackZ = -2.5f;
        public const float EaveHeight = 3.4f;

        /// <summary>The turntable: centre of its top, its radius and the height of its top.</summary>
        public static readonly Vector3 TurntableCentre = new Vector3(0f, PadTop, -0.35f);

        public const float TurntableRadius = 1.75f;
        public const float PadTop = 0.12f;

        /// <summary>The overhead rail the arms hang from, and the arms' link lengths.</summary>
        public const float RailHeight = 3.18f;

        public const float UpperLength = 0.9f;
        public const float LowerLength = 0.8f;
        public const float TipLength = 0.26f;

        /// <summary>The hopper's mouth (its centre, at rover height) and the way it faces: at the turntable.</summary>
        public static readonly Vector3 HopperMouth = new Vector3(2.85f, 1.2f, 1.5f);

        /// <summary>Half the width of the bay hopper's mouth.</summary>
        public const float HopperSize = 0.5f;

        private const float Post = 0.16f;
        private const float Beam = 0.14f;

        /// <summary>
        /// Shoulder <paramref name="index"/> on the overhead rail: left, back and right of the turntable.
        /// </summary>
        public static Vector3 Shoulder(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(-1.3f, RailHeight, TurntableCentre.z);
                case 1:
                    return new Vector3(0f, RailHeight, TurntableCentre.z - 1.3f);
                case 2:
                    return new Vector3(1.3f, RailHeight, TurntableCentre.z);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Bay arms are 0..2.");
            }
        }

        /// <summary>
        /// The yaw that turns arm <paramref name="index"/>'s +Z from its shoulder towards the turntable.
        /// </summary>
        public static float ShoulderYaw(int index)
        {
            Vector3 toCentre = TurntableCentre - Shoulder(index);
            return Mathf.Atan2(toCentre.x, toCentre.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The work lamp glass <paramref name="index"/>: on the front beam, aimed down at the turntable.
        /// </summary>
        public static Vector3 LampGlass(int index)
        {
            return new Vector3((index == 0 ? -1f : 1f) * 1.55f, EaveHeight - 0.32f, FrontZ - 0.25f);
        }

        /// <summary>The lamps' aim: down and back at the turntable.</summary>
        public static readonly Vector3 LampEuler = new Vector3(55f, 180f, 0f);

        /// <summary>
        /// The frame, roof, walls, ramp, hopper, reel, board and Kenji's bench (everything that stays put).
        /// </summary>
        public static LowPolyMeshBuilder Frame()
        {
            var b = new LowPolyMeshBuilder(6000);
            Structure(b);
            Roof(b);
            Walls(b);
            Floor(b);
            Hopper(b);
            SiteKit.Drum(b, At(new Vector3(-HalfWidth - 0.45f, 0.56f, -1.4f), new Vector3(0f, 90f, 0f)), 0.5f, 0.6f,
                PaletteSwatch.WarmAccent);
            Board(b);
            for (int i = 0; i < LampCount; i++)
            {
                Matrix4x4 lamp = At(LampGlass(i), LampEuler);
                b.Prism(lamp * At(0f, 0f, -0.08f) * Matrix4x4.Rotate(Rotation(AlongZ)), 0.15f, 0.16f, 10,
                    PaletteSwatch.FadedPaint);
                b.Torus(lamp * At(new Vector3(0f, 0f, 0.01f), AlongZ), 0.14f, 0.02f, 10, 3, PaletteSwatch.Metal);
                RecipeKit.Rod(b, LampGlass(i) + new Vector3(0f, 0.1f, -0.1f), new Vector3(LampGlass(i).x, EaveHeight,
                    FrontZ), 0.025f, 5, PaletteSwatch.Metal);
            }

            b.Append(WorkbenchMeshes.Bench(), At(new Vector3(-HalfWidth - 1.95f, 0f, 0.9f), new Vector3(0f, 12f, 0f)));
            return b;
        }

        /// <summary>
        /// The turntable (origin at its top centre): a ribbed plate on a ring, 07's tyre tracks worn in.
        /// </summary>
        public static LowPolyMeshBuilder Turntable()
        {
            var b = new LowPolyMeshBuilder(400);
            b.Prism(At(0f, -0.045f, 0f), TurntableRadius, 0.09f, 20, PaletteSwatch.Metal);
            b.Torus(At(0f, -0.015f, 0f), TurntableRadius - 0.05f, 0.03f, 20, 3, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.64f, 0.004f, 0f), new Vector3(0.26f, 0.008f, TurntableRadius * 1.7f),
                    PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < 6; i++)
            {
                b.Box(At(new Vector3(0f, 0.003f, 0f), new Vector3(0f, i * 30f, 0f)), new Vector3(0.05f, 0.006f,
                    TurntableRadius * 1.9f), PaletteSwatch.FadedPaint);
            }

            return b;
        }

        /// <summary>An arm's shoulder mount on the rail (Arm_n's own mesh): a trolley and a motor drum.</summary>
        public static LowPolyMeshBuilder ShoulderMount()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(0f, 0.1f, 0f), new Vector3(0.3f, 0.18f, 0.34f), PaletteSwatch.FadedPaint, 0.03f);
            b.Prism(At(new Vector3(0f, 0f, 0f), AlongX), 0.11f, 0.36f, 10, PaletteSwatch.Metal);
            b.Prism(At(new Vector3(0.2f, 0f, 0f), AlongX), 0.08f, 0.06f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// An arm link hanging down its local -Y from the joint at the origin, a piston along its back.
        /// </summary>
        public static LowPolyMeshBuilder Link(float length, float thickness)
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(0f, -length * 0.5f, 0f), new Vector3(thickness, length, thickness), PaletteSwatch.FadedPaint,
                thickness * 0.15f);
            b.Prism(At(new Vector3(0f, -length, 0f), AlongX), thickness * 0.75f, thickness * 1.3f, 10,
                PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, new Vector3(0f, -length * 0.15f, -thickness * 0.8f),
                new Vector3(0f, -length * 0.8f, -thickness * 0.8f), thickness * 0.22f, 6, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The fitting head at the end of an arm: a cradle of two soft prongs and a welding nozzle.</summary>
        public static LowPolyMeshBuilder Tip()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(0f, -0.07f, 0f), new Vector3(0.16f, 0.14f, 0.14f), PaletteSwatch.Metal, 0.02f);
            for (int side = -1; side <= 1; side += 2)
            {
                RecipeKit.Rod(b, new Vector3(side * 0.06f, -0.12f, 0f), new Vector3(side * 0.09f, -TipLength, 0.02f),
                    0.022f, 6, PaletteSwatch.Charcoal);
            }

            b.Frustum(At(new Vector3(0f, -0.2f, 0.06f)), 0.012f, 0.03f, 0.1f, 6, PaletteSwatch.Rust);
            return b;
        }

        /// <summary>The two work lamps' dark glass (each a glow renderer, origin at its centre, facing +Z).</summary>
        public static LowPolyMeshBuilder LampGlassMesh()
        {
            var b = new LowPolyMeshBuilder(30);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), 0.12f, 0.02f, 10, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// The bay's rust: streaks down the posts and the back wall from their bolts, collars at the post feet and
        /// the hopper's legs, rust inside the hopper's lip.
        /// </summary>
        public static LowPolyMeshBuilder Rust()
        {
            var b = new LowPolyMeshBuilder(700);
            foreach (float x in new[] { -HalfWidth, HalfWidth })
            {
                foreach (float z in new[] { FrontZ, BackZ })
                {
                    Weathering.Collar(b, At(x, 0.2f, z), 0.13f, 0.18f);
                    Matrix4x4 post = SiteKit.Face(new Vector3(x, 0f, z + Post * 0.5f + Weathering.RustLift),
                        Vector3.forward, Vector3.up);
                    SiteKit.RustStreak(b, post, 0f, EaveHeight - 0.25f, 0.8f, 0.04f);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                float x = -HalfWidth + (i + 0.5f) * HalfWidth * 0.5f;
                Matrix4x4 back = SiteKit.Face(new Vector3(x, 0f, BackZ - 0.08f + Weathering.RustLift),
                    Vector3.forward, Vector3.up);
                SiteKit.RustStreak(b, back, (i % 2 == 0 ? 0.3f : -0.25f), EaveHeight - 0.3f, 0.6f + i * 0.15f);
            }

            ServiceKit.HopperRust(b, HopperMouth, HopperFacing, HopperSize);
            return b;
        }

        /// <summary>Dust drifted against the back wall and the left wall.</summary>
        public static LowPolyMeshBuilder Drifts()
        {
            var b = new LowPolyMeshBuilder(900);
            SiteKit.Drift(b, new Vector3(-0.8f, 0f, BackZ - 0.45f), 3.2f, 0.8f, 0.35f, 90f, 101);
            SiteKit.Drift(b, new Vector3(-HalfWidth - 0.4f, 0f, -0.9f), 2.6f, 0.7f, 0.28f, 0f, 102);
            return b;
        }

        /// <summary>Posts, eave beams, the overhead rail ring the arms run on.</summary>
        private static void Structure(LowPolyMeshBuilder b)
        {
            float[] xs = { -HalfWidth, HalfWidth };
            float[] zs = { FrontZ, BackZ };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    SiteKit.Bar(b, new Vector3(x, Post * 0.5f, z), new Vector3(x, EaveHeight, z), Post,
                        PaletteSwatch.FadedPaint);
                    b.Box(At(x, 0.04f, z), new Vector3(0.36f, 0.08f, 0.36f), PaletteSwatch.Charcoal, 0.02f);
                }

                SiteKit.Bar(b, new Vector3(x, EaveHeight, FrontZ), new Vector3(x, EaveHeight, BackZ), Beam,
                    PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(x, 1.2f, BackZ), new Vector3(x, 1.2f, FrontZ - 1.4f), 0.08f,
                    PaletteSwatch.Metal);
            }

            foreach (float z in zs)
            {
                SiteKit.Bar(b, new Vector3(-HalfWidth, EaveHeight, z), new Vector3(HalfWidth, EaveHeight, z), Beam,
                    PaletteSwatch.Metal);
            }

            // The arms' rail: a square ring under the roof round the turntable, hung on drop rods.
            float ring = 1.3f;
            Vector3 centre = new Vector3(TurntableCentre.x, RailHeight + 0.2f, TurntableCentre.z);
            Vector3[] corners =
            {
                centre + new Vector3(-ring, 0f, -ring), centre + new Vector3(ring, 0f, -ring),
                centre + new Vector3(ring, 0f, ring), centre + new Vector3(-ring, 0f, ring),
            };
            for (int i = 0; i < corners.Length; i++)
            {
                SiteKit.Bar(b, corners[i], corners[(i + 1) % corners.Length], 0.12f, PaletteSwatch.Metal);
                SiteKit.Bar(b, corners[i], new Vector3(corners[i].x, EaveHeight, corners[i].z), 0.05f,
                    PaletteSwatch.Metal);
            }
        }

        /// <summary>A shallow roof of corrugated sheets with one sheet long gone, open sky over the left bay.</summary>
        private static void Roof(LowPolyMeshBuilder b)
        {
            const int sheets = 5;
            float depth = FrontZ - BackZ + 0.3f;
            float width = (2f * HalfWidth + 0.3f) / sheets;
            for (int i = 0; i < sheets; i++)
            {
                if (i == 1)
                {
                    continue;
                }

                float x = -HalfWidth - 0.15f + (i + 0.5f) * width;
                Matrix4x4 sheet = SiteKit.Face(new Vector3(x, EaveHeight + 0.1f + i * 0.015f, (FrontZ + BackZ) * 0.5f),
                    Vector3.up, Vector3.back);
                SiteKit.Sheet(b, sheet, width - 0.02f, depth, PaletteSwatch.FadedPaint);
            }
        }

        /// <summary>Corrugated sheets on the back and the left side, up to a low skirt on the right.</summary>
        private static void Walls(LowPolyMeshBuilder b)
        {
            float height = EaveHeight - 0.2f;
            for (int i = 0; i < 4; i++)
            {
                float x = -HalfWidth + (i + 0.5f) * HalfWidth * 0.5f;
                Matrix4x4 back = SiteKit.Face(new Vector3(x, height * 0.5f + 0.1f, BackZ - 0.1f), Vector3.back,
                    Vector3.up);
                SiteKit.Sheet(b, back, HalfWidth * 0.5f - 0.02f, height, PaletteSwatch.FadedPaint);
            }

            float span = FrontZ - BackZ;
            for (int i = 0; i < 3; i++)
            {
                float z = BackZ + (i + 0.5f) * span / 3f;
                Matrix4x4 left = SiteKit.Face(new Vector3(-HalfWidth - 0.1f, height * 0.5f + 0.1f, z), Vector3.left,
                    Vector3.up);
                if (i < 2)
                {
                    SiteKit.Sheet(b, left, span / 3f - 0.02f, height, PaletteSwatch.FadedPaint);
                }

                Matrix4x4 right = SiteKit.Face(new Vector3(HalfWidth + 0.1f, 0.55f, z), Vector3.right, Vector3.up);
                SiteKit.Sheet(b, right, span / 3f - 0.02f, 0.9f, PaletteSwatch.FadedPaint);
            }
        }

        /// <summary>The slab, the turntable's sunken ring and the low ramp in from the front.</summary>
        private static void Floor(LowPolyMeshBuilder b)
        {
            float depth = FrontZ - BackZ;
            b.Box(At(0f, PadTop * 0.5f, (FrontZ + BackZ) * 0.5f), new Vector3(2f * HalfWidth, PadTop, depth),
                PaletteSwatch.Metal, 0.03f);
            b.Torus(At(TurntableCentre + Vector3.down * 0.01f), TurntableRadius + 0.05f, 0.04f, 20, 3,
                PaletteSwatch.Charcoal);
            b.Wedge(At(0f, PadTop * 0.5f, FrontZ + 0.55f), new Vector3(2.6f, PadTop, 1.1f), PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 1.38f, PadTop + 0.03f, FrontZ + 0.4f), new Vector3(0.08f, 0.06f, 1.2f),
                    PaletteSwatch.FadedAccent, 0.01f);
            }
        }

        /// <summary>
        /// The material hopper by the entrance: a funnel on four legs, its mouth at rover height turned to the
        /// turntable so 07's beam can feed it, a chute down into the bay's back.
        /// </summary>
        private static void Hopper(LowPolyMeshBuilder b)
        {
            ServiceKit.Hopper(b, HopperMouth, HopperFacing, HopperSize);
            RecipeKit.Rod(b, new Vector3(HopperMouth.x - 0.2f, 0.6f, HopperMouth.z - 0.6f),
                new Vector3(HalfWidth - 0.4f, 0.4f, BackZ + 0.6f), 0.08f, 8, PaletteSwatch.Metal);
        }

        /// <summary>
        /// The hopper mouth's facing: +Z out of the mouth, turned towards the turntable and tipped up.
        /// </summary>
        public static Vector3 HopperFacing
        {
            get
            {
                Vector3 toPad = TurntableCentre - HopperMouth;
                float yaw = Mathf.Atan2(toPad.x, toPad.z) * Mathf.Rad2Deg;
                return new Vector3(-ServiceKit.MouthTilt, yaw, 0f);
            }
        }

        /// <summary>The "PIT 07" board over the entrance, hand-painted like the bench sticker.</summary>
        private static void Board(LowPolyMeshBuilder b)
        {
            Matrix4x4 board = At(new Vector3(0.4f, EaveHeight - 0.32f, FrontZ + 0.1f), new Vector3(0f, 0f, -2.5f));
            b.Box(board, new Vector3(1.5f, 0.42f, 0.05f), PaletteSwatch.Enamel, 0.012f);
            // Read from the front, +X is on the reader's left: "PIT" first, then "07".
            Glyphs.Write(b, board * At(0.25f, 0f, 0.026f), "PIT", 0.24f, PaletteSwatch.Charcoal, PaletteSwatch.Enamel);
            Glyphs.Write(b, board * At(-0.42f, 0f, 0.026f), "07", 0.24f, PaletteSwatch.WarmAccent,
                PaletteSwatch.Enamel);
        }
    }
}
