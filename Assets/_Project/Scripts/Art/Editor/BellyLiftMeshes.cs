using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The habitat lander's belly lift (docs/features/M3-19 §4): a round platform that is also the lab's turntable,
    /// hanging on two hoist cables from sheaves on short winch posts beside the shaft's top in the cabin floor, between
    /// four guide posts so its job reads from the apron; the floor hatch's two sliding leaves under the belly with
    /// their lamps, the belly's work lamp, and what jammed it decades ago (the right cable snapped and coiled slack on
    /// the platform, its frayed end hanging out of the shaft, the crew's tag on the post). Lift space is lander space:
    /// the origin on the ground under the shaft, +Z the front where 07 drives on.
    /// </summary>
    internal static class BellyLiftMeshes
    {
        public const float PlatformRadius = 1.7f;
        public const float PlatformThickness = 0.2f;

        /// <summary>The platform's top resting level on its ground stops.</summary>
        public const float DownY = 0.25f;

        /// <summary>The platform's top flush with the cabin floor.</summary>
        public const float UpY = LanderMeshes.FloorY;

        /// <summary>How much lower the jammed side hangs than the other, across the platform.</summary>
        public const float JamDrop = 0.3f;

        /// <summary>
        /// The hatch leaves' slide out of the shaft's mouth along ±X into their pockets in the stage, clear of the
        /// rising platform and its cable eyes.
        /// </summary>
        public const float HatchLeafTravel = 1.85f;

        // Where the hoist cables run, either side of the shaft: just inside its flats, clear of the platform's rim,
        // from sheaves on winch posts standing on the cabin floor, so the platform rises flush under them.
        private const float CableX = 1.72f;
        private const float SheaveY = LanderMeshes.FloorY + 0.35f;
        private const float PostX = 1.95f;
        private const float EyeDrop = 0.08f;
        private const float CableRadius = 0.022f;

        // Each leaf is half an octagon a lip wider than the shaft, set just inside the stage's underside so it slides
        // away into the stage's body.
        private const float LeafThickness = 0.06f;
        private const float LeafLip = 0.06f;
        private const float LeafOverlap = 0.03f;
        private const float LeafRecess = 0.02f;

        private const float GuideRadius = 1.98f;

        // The guide posts stop short of the stage so the hatch leaves slide out over them.
        private const float GuideTop = LanderMeshes.StageBottom - 0.18f;
        private const float GuideBar = 0.12f;
        private static readonly float[] GuideYaws = { 70f, 110f, -70f, -110f };

        private const int RimLampCount = 8;
        private const float PadRingRadius = 1.2f;

        private const int HatchRimLamps = 12;
        private const float HatchRimRadius = 2.38f;

        private static readonly Vector3 BellyLampAt = new Vector3(0f, LanderMeshes.StageBottom - 0.2f, 2.75f);

        /// <summary>
        /// The platform's roll in its jammed pose: the right (+X) side down, where its cable went slack.
        /// </summary>
        public static Quaternion JamTilt =>
            Rotation(new Vector3(0f, 0f, -Mathf.Asin(JamDrop / (PlatformRadius * 2f)) * Mathf.Rad2Deg));

        /// <summary>Where the jammed platform's top centre hangs: its low side resting on the ground stops.</summary>
        public static Vector3 JammedCentre => new Vector3(0f, DownY + JamDrop * 0.5f, 0f);

        /// <summary>
        /// A hoist cable's top: its sheave on the winch post beside the shaft (side -1 left, +1 right).
        /// </summary>
        public static Vector3 CableTop(int side)
        {
            return new Vector3(side * CableX, SheaveY, 0f);
        }

        /// <summary>
        /// The cable's length in the built pose, from its sheave down to the jammed platform's left eye.
        /// </summary>
        public static float BuiltCableLength
        {
            get
            {
                Vector3 eye = JammedCentre + JamTilt * new Vector3(-CableX, -EyeDrop, 0f);
                return SheaveY - eye.y;
            }
        }

        /// <summary>
        /// The static frame (appended to the lander's hull): the ring of ground stops the platform rests on, the four
        /// guide posts up toward the belly with their hazard collars, the winch posts and sheaves beside the shaft's
        /// top in the cabin floor and the belly lamp's housing.
        /// </summary>
        public static void Frame(LowPolyMeshBuilder b)
        {
            b.Torus(At(0f, 0.05f, 0f), PlatformRadius + 0.2f, 0.05f, 16, 3, PaletteSwatch.Charcoal);
            for (int i = 0; i < 8; i++)
            {
                Vector3 stop = Rotation(new Vector3(0f, 22.5f + i * 45f, 0f)) * Vector3.forward
                    * (PlatformRadius - 0.3f);
                b.Box(At(stop + Vector3.up * (DownY - PlatformThickness) * 0.5f, new Vector3(0f, 22.5f + i * 45f, 0f)),
                    new Vector3(0.22f, DownY - PlatformThickness, 0.22f), PaletteSwatch.Metal, 0.01f);
            }

            foreach (float yaw in GuideYaws)
            {
                Vector3 foot = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward * GuideRadius;
                Vector3 top = foot + Vector3.up * GuideTop;
                SiteKit.Bar(b, foot + Vector3.up * (GuideBar * 0.5f), top, GuideBar, PaletteSwatch.Metal);
                b.Box(At(top + Vector3.up * 0.03f, new Vector3(0f, yaw, 0f)), new Vector3(0.2f, 0.06f, 0.2f),
                    PaletteSwatch.Charcoal, 0.01f);
                b.Box(At(foot + Vector3.up * 0.55f, new Vector3(0f, yaw, 0f)),
                    new Vector3(GuideBar + 0.03f, 0.3f, GuideBar + 0.03f), PaletteSwatch.FadedAccent);
                b.Box(At(foot + Vector3.up * 0.04f, new Vector3(0f, yaw, 0f)), new Vector3(0.3f, 0.08f, 0.3f),
                    PaletteSwatch.Charcoal, 0.01f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * PostX, LanderMeshes.FloorY + (SheaveY - LanderMeshes.FloorY) * 0.5f, 0f),
                    new Vector3(0.14f, SheaveY - LanderMeshes.FloorY, 0.22f), PaletteSwatch.Charcoal, 0.02f);
                b.Box(At(side * (PostX - 0.06f), SheaveY + 0.04f, 0f), new Vector3(0.18f, 0.08f, 0.16f),
                    PaletteSwatch.Metal, 0.01f);
                b.Prism(At(new Vector3(side * CableX, SheaveY, 0f), AlongZ), 0.07f, 0.06f, 8, PaletteSwatch.Metal);
            }

            b.Box(At(BellyLampAt + Vector3.up * 0.09f), new Vector3(0.42f, 0.16f, 0.3f), PaletteSwatch.Charcoal, 0.03f);
            RecipeKit.Rod(b, BellyLampAt + Vector3.up * 0.16f, BellyLampAt + Vector3.up * 0.2f, 0.06f, 6,
                PaletteSwatch.Metal);
        }

        /// <summary>
        /// The platform in platform space (the origin at the middle of its top): a worn round deck with a hazard rim,
        /// tread plates either side of 07's lane, the turntable's centre boss, the lifting eyes for the two cables at
        /// its sides and the rim lamps' housings.
        /// </summary>
        public static LowPolyMeshBuilder Platform()
        {
            var b = new LowPolyMeshBuilder(600);
            b.Prism(At(0f, -PlatformThickness * 0.5f, 0f), PlatformRadius, PlatformThickness, 16,
                PaletteSwatch.WornPanel);
            b.Torus(At(0f, 0.004f, 0f), PlatformRadius - 0.1f, 0.05f, 16, 3, PaletteSwatch.FadedAccent);
            b.Prism(At(0f, 0.008f, 0f), 0.42f, 0.016f, 12, PaletteSwatch.Metal);
            b.Torus(At(0f, 0.01f, 0f), PadRingRadius + 0.06f, 0.025f, 20, 3, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.62f, 0.008f, 0f), new Vector3(0.36f, 0.016f, 2.4f), PaletteSwatch.Metal, 0.004f);
                b.Box(At(side * (CableX - 0.04f), -EyeDrop, 0f), new Vector3(0.12f, 0.12f, 0.2f),
                    PaletteSwatch.Charcoal, 0.01f);
                b.Torus(At(new Vector3(side * CableX, -EyeDrop + 0.08f, 0f), AlongZ), 0.045f, 0.015f, 8, 3,
                    PaletteSwatch.Metal);
            }

            for (int i = 0; i < RimLampCount; i++)
            {
                b.Prism(At(RimLamp(i) + Vector3.down * 0.03f), 0.07f, 0.04f, 8, PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>The rim lamps' glass (platform space), low on the rim where they light the dust round 07.</summary>
        public static LowPolyMeshBuilder RimLamps()
        {
            var b = new LowPolyMeshBuilder(160);
            for (int i = 0; i < RimLampCount; i++)
            {
                b.Icosphere(At(RimLamp(i)), 0.05f, 1, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>
        /// The pad's ring of glass set into the deck (platform space): lit when the lift can be stitched.
        /// </summary>
        public static LowPolyMeshBuilder PadGlow()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Torus(At(0f, 0.012f, 0f), PadRingRadius, 0.022f, 24, 3, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>The platform's dust drift: a low mound blown across its back half over the decades.</summary>
        public static LowPolyMeshBuilder PlatformDrift()
        {
            var b = new LowPolyMeshBuilder(400);
            SiteKit.Drift(b, new Vector3(0.4f, 0f, -0.7f), 1.6f, 0.9f, 0.1f, 70f, 91);
            return b;
        }

        /// <summary>The platform's rust: a bloom round each lifting eye and a streak from the right one.</summary>
        public static LowPolyMeshBuilder PlatformRust()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int side = -1; side <= 1; side += 2)
            {
                Matrix4x4 face = SiteKit.Face(new Vector3(side * (CableX + 0.03f + Weathering.RustLift), 0f, 0f),
                    Vector3.right * side, Vector3.up);
                SiteKit.RustStreak(b, face, 0f, -EyeDrop - 0.04f, side > 0 ? 0.1f : 0.06f);
            }

            return b;
        }

        /// <summary>
        /// Hatch leaf <paramref name="side"/> (-1 left, +1 right) in lander space, shut: half an octagon covering its
        /// half of the shaft's mouth just inside the stage's underside, its edge overlapping the other leaf's, with
        /// hazard stripes along the seam underneath.
        /// </summary>
        public static LowPolyMeshBuilder HatchLeaf(int side)
        {
            var b = new LowPolyMeshBuilder(80);
            float radius = LanderMeshes.ShaftRadius + LeafLip;
            var outline = new Vector2[6];
            outline[0] = new Vector2(-LeafOverlap, LanderMeshes.Corner(0, radius, 0f).z);
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = LanderMeshes.Corner(i, radius, 0f);
                outline[i + 1] = new Vector2(corner.x, corner.z);
            }

            outline[5] = new Vector2(-LeafOverlap, -outline[0].y);
            if (side < 0)
            {
                for (int i = 0; i < outline.Length; i++)
                {
                    outline[i].x = -outline[i].x;
                }
            }

            float bottom = LanderMeshes.StageBottom + LeafRecess;
            b.Extrude(At(new Vector3(0f, bottom + LeafThickness * 0.5f, 0f), new Vector3(90f, 0f, 0f)), outline,
                LeafThickness, PaletteSwatch.Metal);
            for (int stripe = -2; stripe <= 2; stripe++)
            {
                b.Box(At(side * 0.12f, bottom - 0.005f, stripe * 0.5f), new Vector3(0.16f, 0.01f, 0.2f),
                    PaletteSwatch.FadedAccent);
            }

            return b;
        }

        /// <summary>
        /// The hatch rim's lamp glasses round the shaft's mouth on the stage's underside (lander space).
        /// </summary>
        public static LowPolyMeshBuilder HatchRim()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int i = 0; i < HatchRimLamps; i++)
            {
                float yaw = (i + 0.5f) * 360f / HatchRimLamps;
                Vector3 at = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward * HatchRimRadius
                    + Vector3.up * (LanderMeshes.StageBottom - 0.03f);
                b.Prism(At(at), 0.06f, 0.04f, 8, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>The belly work lamp's lens, facing down and back at the platform (lander space).</summary>
        public static LowPolyMeshBuilder BellyLamp()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Box(At(BellyLampAt), new Vector3(0.34f, 0.03f, 0.22f), PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>A hoist cable hanging along -Y from its sheave (cable space), at the built length.</summary>
        public static LowPolyMeshBuilder Cable()
        {
            var b = new LowPolyMeshBuilder(40);
            RecipeKit.Rod(b, Vector3.zero, Vector3.down * BuiltCableLength, CableRadius, 5, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The jam (lander space): the right cable's frayed end hanging out of the shaft from its sheave, the rest of
        /// it coiled slack on the jammed platform's low side, and the crew's "OUT OF ORDER - K." tag wired to the
        /// right front guide post.
        /// </summary>
        public static LowPolyMeshBuilder Jam()
        {
            var b = new LowPolyMeshBuilder(900);
            Vector3 top = CableTop(1);
            Vector3 frayed = StitchPoint;
            SiteKit.Cable(b, new[] { top, top + new Vector3(0.02f, -0.9f, 0.01f), frayed + new Vector3(0f, 0.12f, 0f),
                frayed }, CableRadius, PaletteSwatch.Charcoal);
            for (int strand = -1; strand <= 1; strand++)
            {
                RecipeKit.Rod(b, frayed, frayed + new Vector3(strand * 0.04f, -0.09f, 0.03f * strand), 0.007f, 4,
                    PaletteSwatch.Metal);
            }

            Matrix4x4 deck = Matrix4x4.TRS(JammedCentre, JamTilt, Vector3.one);
            for (int i = 0; i < 4; i++)
            {
                b.Torus(deck * At(new Vector3(1.05f, 0.02f + i * 0.026f, 0.45f), new Vector3(0f, i * 31f, i * 3f)),
                    0.3f - i * 0.035f, CableRadius, 12, 3, PaletteSwatch.Charcoal);
            }

            Vector3 post = Rotation(new Vector3(0f, GuideYaws[0], 0f)) * Vector3.forward * GuideRadius;
            Matrix4x4 tag = At(post + new Vector3(0.12f, 1.05f, 0.1f), new Vector3(6f, 70f, -8f));
            b.Box(tag, new Vector3(0.44f, 0.24f, 0.015f), PaletteSwatch.Enamel, 0.004f);
            Glyphs.Write(b, tag * At(0f, 0.04f, 0.01f), "OUT OF", 0.055f, PaletteSwatch.WarmAccent,
                PaletteSwatch.Enamel);
            Glyphs.Write(b, tag * At(0f, -0.05f, 0.01f), "ORDER-K", 0.055f, PaletteSwatch.WarmAccent,
                PaletteSwatch.Enamel);
            RecipeKit.Rod(b, post + new Vector3(0f, 1.2f, 0f), tag.MultiplyPoint3x4(new Vector3(0f, 0.12f, 0f)),
                0.006f, 4, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The frayed end of the snapped cable below the belly: where 07's beam stitches it (lander space).
        /// </summary>
        public static Vector3 StitchPoint => new Vector3(CableX, LanderMeshes.StageBottom - 0.45f, 0f);

        /// <summary>Rim lamp <paramref name="index"/>'s place on the platform's rim (platform space).</summary>
        private static Vector3 RimLamp(int index)
        {
            float yaw = 22.5f + index * 360f / RimLampCount;
            return Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward * (PlatformRadius - 0.05f) + Vector3.up * 0.04f;
        }
    }
}
