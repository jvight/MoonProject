using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The lander's deck hatch (M3-07): a crew door (0.8 x 1.85 m, its porthole at a person's 1.5 m eye height) on a
    /// hinge, opened by its own little motor (ruling 14: 07 never touches it), and the shallow alcove behind it where
    /// Ines's light table stands. In lander space unless a method says otherwise; +Z is the hatch side.
    /// </summary>
    internal static class DeckHatchMeshes
    {
        /// <summary>A cabin wall triangle facing +Z at least this much is the hatch side's wall (left out).</summary>
        public const float HatchFaceDot = 0.99f;

        /// <summary>How far the alcove behind the doorway reaches into the cabin.</summary>
        public const float AlcoveDepth = 0.85f;

        /// <summary>How far the opened hatch swings out on its hinge (degrees, outward toward -X).</summary>
        public const float OpenSwing = 100f;

        private const float OpeningMargin = 0.03f;
        private const float FrameBar = 0.07f;
        private const float FrameDepth = 0.06f;
        private const float DoorThickness = 0.05f;
        private const float DoorStandOff = 0.07f;
        private const float PortholeRadius = 0.13f;

        /// <summary>Half the doorway's clear width.</summary>
        public static float OpeningHalf => LanderMeshes.DoorWidth * 0.5f + OpeningMargin;

        /// <summary>The doorway's clear top: under the cabin's orange band, which crosses it as a lintel.</summary>
        public static float OpeningTop => LanderMeshes.BandY - 0.08f;

        /// <summary>The hatch side's wall plane.</summary>
        public static float Face => LanderMeshes.CabinApothem;

        /// <summary>The DeckHatch node's pivot: its hinge, at the doorway's -X jamb, on the door's back face.</summary>
        public static Vector3 Hinge => new Vector3(-LanderMeshes.DoorWidth * 0.5f, LanderMeshes.DoorBottom,
            Face + DoorStandOff - DoorThickness * 0.5f);

        /// <summary>The hatch swung open by its motor (DeckHatch's local rotation; shut is identity).</summary>
        public static Quaternion Open => Rotation(new Vector3(0f, -OpenSwing, 0f));

        /// <summary>
        /// The doorway in the hatch side's wall: the wall rebuilt round the opening, a dark frame, the alcove's walls
        /// facing in, and the hatch motor on the frame above the hinge with its push rod.
        /// </summary>
        public static void Doorway(LowPolyMeshBuilder b)
        {
            float face = Face;
            float half = OpeningHalf;
            float top = OpeningTop;
            float bottom = LanderMeshes.DoorBottom;
            float wallHalf = LanderMeshes.CabinRadius * Mathf.Sin(Mathf.PI / 8f);
            float wallBottom = LanderMeshes.CabinBottom;
            float wallTop = LanderMeshes.CabinTop;
            Panel(b, -wallHalf, -half, wallBottom, wallTop, face, PaletteSwatch.Enamel);
            Panel(b, half, wallHalf, wallBottom, wallTop, face, PaletteSwatch.Enamel);
            Panel(b, -half, half, top, wallTop, face, PaletteSwatch.Enamel);
            Panel(b, -half, half, wallBottom, bottom, face, PaletteSwatch.Enamel);

            float frameTop = top + FrameBar;
            float z = face + FrameDepth * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (half + FrameBar * 0.5f), (LanderMeshes.DeckTop + frameTop) * 0.5f, z),
                    new Vector3(FrameBar, frameTop - LanderMeshes.DeckTop, FrameDepth), PaletteSwatch.Charcoal, 0.01f);
            }

            b.Box(At(0f, top + FrameBar * 0.5f, z), new Vector3(2f * (half + FrameBar), FrameBar, FrameDepth),
                PaletteSwatch.Charcoal, 0.01f);

            float back = face - AlcoveDepth;
            b.Polygon(Quad(new Vector3(-half, bottom, back), new Vector3(half, bottom, back),
                new Vector3(half, top, back), new Vector3(-half, top, back)), Vector3.forward, PaletteSwatch.Charcoal);
            b.Polygon(Quad(new Vector3(-half, bottom, back), new Vector3(-half, bottom, face),
                new Vector3(-half, top, face), new Vector3(-half, top, back)), Vector3.right, PaletteSwatch.WornPanel);
            b.Polygon(Quad(new Vector3(half, bottom, back), new Vector3(half, bottom, face),
                new Vector3(half, top, face), new Vector3(half, top, back)), Vector3.left, PaletteSwatch.WornPanel);
            b.Polygon(Quad(new Vector3(-half, top, back), new Vector3(half, top, back),
                new Vector3(half, top, face), new Vector3(-half, top, face)), Vector3.down, PaletteSwatch.WornPanel);
            b.Polygon(Quad(new Vector3(-half, bottom, back), new Vector3(half, bottom, back),
                new Vector3(half, bottom, face), new Vector3(-half, bottom, face)), Vector3.up,
                PaletteSwatch.WornPanel);

            var motor = new Vector3(-half - FrameBar - 0.1f, top - 0.12f, face + 0.08f);
            b.Box(At(motor), new Vector3(0.16f, 0.22f, 0.14f), PaletteSwatch.Metal, 0.015f);
            b.Prism(At(motor + new Vector3(0f, 0.13f, 0f)), 0.035f, 0.04f, 6, PaletteSwatch.Charcoal);
            Vector3 doorArm = Hinge + new Vector3(0.12f, LanderMeshes.DoorHeight - 0.3f, DoorThickness);
            RecipeKit.Rod(b, motor + new Vector3(0.06f, -0.05f, 0.05f), doorArm, 0.014f, 6, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// The hatch itself in DeckHatch space (the hinge at the origin, the door reaching along +X, +Z its outer
        /// face): a metal door with a hinge strip, the porthole's ring at a person's eye height and the handle.
        /// </summary>
        public static LowPolyMeshBuilder Hatch()
        {
            var b = new LowPolyMeshBuilder(200);
            float width = LanderMeshes.DoorWidth;
            float height = LanderMeshes.DoorHeight;
            b.Box(At(width * 0.5f, height * 0.5f, DoorThickness * 0.5f), new Vector3(width, height, DoorThickness),
                PaletteSwatch.Metal, 0.02f);
            b.Box(At(0.02f, height * 0.5f, DoorThickness * 0.5f), new Vector3(0.05f, height - 0.1f,
                DoorThickness + 0.02f), PaletteSwatch.Charcoal, 0.01f);
            b.Torus(At(new Vector3(width * 0.5f, LanderMeshes.EyeHeight, DoorThickness + 0.005f), AlongZ),
                PortholeRadius + 0.02f, 0.03f, 10, 4, PaletteSwatch.Charcoal);
            b.Box(At(width - 0.1f, LanderMeshes.HandHeight, DoorThickness + 0.02f), new Vector3(0.05f, 0.2f, 0.04f),
                PaletteSwatch.Charcoal, 0.01f);
            return b;
        }

        /// <summary>The porthole's glass in DeckHatch space: dark until a light inside shows through it.</summary>
        public static LowPolyMeshBuilder Porthole()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(At(new Vector3(LanderMeshes.DoorWidth * 0.5f, LanderMeshes.EyeHeight, DoorThickness * 0.5f),
                AlongZ), PortholeRadius, DoorThickness + 0.01f, 10, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>The hatch's own rust: a bloom under the handle, a streak down from the porthole's ring.</summary>
        public static LowPolyMeshBuilder HatchRust()
        {
            var b = new LowPolyMeshBuilder(60);
            Matrix4x4 outer = SiteKit.Face(new Vector3(0f, 0f, DoorThickness + 0.003f), Vector3.forward, Vector3.up);
            SiteKit.RustPatch(b, outer, LanderMeshes.DoorWidth - 0.1f, LanderMeshes.HandHeight - 0.15f, 0.08f);
            SiteKit.RustStreak(b, outer, LanderMeshes.DoorWidth * 0.5f + 0.06f, LanderMeshes.EyeHeight - 0.16f, 0.5f);
            return b;
        }

        private static void Panel(LowPolyMeshBuilder b, float left, float right, float bottom, float top, float z,
            PaletteSwatch swatch)
        {
            b.Polygon(Quad(new Vector3(left, bottom, z), new Vector3(right, bottom, z), new Vector3(right, top, z),
                new Vector3(left, top, z)), Vector3.forward, swatch);
        }

        private static Vector3[] Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            return new[] { a, b, c, d };
        }
    }
}
