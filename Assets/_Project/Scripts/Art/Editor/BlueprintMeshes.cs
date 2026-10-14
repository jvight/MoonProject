using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's blueprints (M3-16 P7, M3-19 D7): an old pen plotter on brackets on the Rover Bay's back wall draws a
    /// kit's sheet when 07 scans its fragments, and the drawn sheets hang from wooden pegs on a cord along the bay's
    /// wall, one per kit (no hands pin anything, ruling 14). Each sheet is slate-blue paper with the kit drawn in
    /// chalky cream lines (no words). Sheets are in their own space (centre at the origin, facing +Z); the cord and
    /// slots in the cord's space (its pegs along X, the wall behind -Z); the plotter in its own space (+Z its front,
    /// the wall behind it).
    /// </summary>
    internal static class BlueprintMeshes
    {
        /// <summary>
        /// The kits with a sheet, in the order of their pegs from the left, named as Core's RoverAbility members (Art
        /// does not reference Core; Gameplay maps the names).
        /// </summary>
        public static readonly string[] Kits =
        {
            "HoverJump", "CargoCradle", "WarmHeadlamp", "BoostCoils", "CellPack", "FocusLens", "SideBins",
        };

        /// <summary>The plotter's bed: its centre and tilt (leaning back) in plotter space.</summary>
        public static readonly Vector3 BedCentre = new Vector3(0f, 1.15f, 0f);

        public static readonly Vector3 BedTilt = new Vector3(-30f, 0f, 0f);

        /// <summary>
        /// The drawing area on the bed (metres): the pen carriage travels its width, the pen its height.
        /// </summary>
        public const float DrawWidth = 0.86f;

        public const float DrawHeight = 0.58f;

        /// <summary>The plotter's ready lamp (plotter space).</summary>
        public static readonly Vector3 PlotterLamp = new Vector3(0.62f, 1.0f, 0.17f);

        private const float SheetWidth = 0.26f;
        private const float SheetHeight = 0.32f;
        private const float SheetThickness = 0.004f;
        private const float Line = 0.008f;
        private const float SlotSpacing = 0.3f;
        private const float CordHeight = 2.12f;
        private const float CordZ = -0.34f;
        private const float CordHalfSpan = 1.06f;

        private const float BedWidth = 1f;
        private const float BedHeight = 0.74f;
        private const float LegHalfSpan = 0.48f;

        /// <summary>The plotter's bed frame (local X across, Y up the bed, Z out of it), in plotter space.</summary>
        public static Matrix4x4 Bed => At(BedCentre, BedTilt);

        /// <summary>The pen carriage's home: the drawing area's bottom-left corner, in plotter space.</summary>
        public static Vector3 CarriageHome =>
            Bed.MultiplyPoint3x4(new Vector3(-DrawWidth * 0.5f, -DrawHeight * 0.5f, 0.03f));

        /// <summary>Where sheet <paramref name="index"/> hangs from its peg on the cord, in the cord's space.</summary>
        public static Vector3 Slot(int index)
        {
            return new Vector3((index - (Kits.Length - 1) * 0.5f) * SlotSpacing,
                CordHeight - SheetHeight * 0.5f - 0.02f, CordZ);
        }

        /// <summary>
        /// The cord along the wall, its hooks, and an empty peg waiting at every slot (the cord's space).
        /// </summary>
        public static LowPolyMeshBuilder Cord()
        {
            var b = new LowPolyMeshBuilder(300);
            var left = new Vector3(-CordHalfSpan, CordHeight, CordZ);
            var right = new Vector3(CordHalfSpan, CordHeight, CordZ);
            SiteKit.Cable(b, SiteKit.Sag(left, right, 0.03f, 6), 0.005f, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * CordHalfSpan, CordHeight, CordZ - 0.02f), AlongZ), 0.01f, 0.04f, 6,
                    PaletteSwatch.Metal);
            }

            for (int i = 0; i < Kits.Length; i++)
            {
                b.Box(At(new Vector3(Slot(i).x, CordHeight - 0.01f, CordZ + 0.01f)), new Vector3(0.016f, 0.07f, 0.02f),
                    PaletteSwatch.Wood, 0.003f);
            }

            return b;
        }

        /// <summary>
        /// One blueprint sheet: slate-blue paper with a chalky border and the kit drawn in cream lines, its top edge
        /// held by its peg on the cord.
        /// </summary>
        public static LowPolyMeshBuilder Sheet(string kit)
        {
            var b = new LowPolyMeshBuilder(300);
            b.Box(Matrix4x4.identity, new Vector3(SheetWidth, SheetHeight, SheetThickness), PaletteSwatch.DustShadow);
            float x = SheetWidth * 0.5f - 0.02f;
            float y = SheetHeight * 0.5f - 0.02f;
            Box(b, new Vector2(-x, -y), new Vector2(x, y));
            switch (kit)
            {
                case "HoverJump":
                    Body(b);
                    for (int i = 0; i < 4; i++)
                    {
                        Spring(b, -0.075f + i * 0.05f);
                    }

                    break;
                case "CargoCradle":
                    Body(b);
                    Box(b, new Vector2(-0.1f, -0.02f), new Vector2(-0.035f, 0.04f));
                    Draw(b, new Vector2(-0.1f, 0.01f), new Vector2(-0.035f, 0.01f));
                    break;
                case "WarmHeadlamp":
                    Draw(b, new Vector2(-0.1f, -0.02f), new Vector2(0.1f, -0.02f));
                    for (int i = -1; i <= 1; i++)
                    {
                        Ring(b, new Vector2(i * 0.075f, 0.025f), 0.03f);
                    }

                    break;
                case "FocusLens":
                    Ring(b, new Vector2(0f, 0.01f), 0.09f);
                    Ring(b, new Vector2(0f, 0.01f), 0.042f);
                    Ring(b, new Vector2(0f, 0.01f), 0.02f);
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = (90f + i * 120f) * Mathf.Deg2Rad;
                        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Draw(b, new Vector2(0f, 0.01f) + direction * 0.05f, new Vector2(0f, 0.01f) + direction * 0.085f);
                    }

                    Draw(b, new Vector2(-0.11f, 0.01f), new Vector2(-0.06f, 0.01f));
                    Draw(b, new Vector2(0.06f, 0.01f), new Vector2(0.11f, 0.01f));
                    break;
                case "SideBins":
                    Body(b);
                    Box(b, new Vector2(0.02f, 0.06f), new Vector2(0.075f, 0.1f));
                    Draw(b, new Vector2(0.03f, 0.06f), new Vector2(0.03f, 0.045f));
                    Draw(b, new Vector2(0.065f, 0.06f), new Vector2(0.065f, 0.045f));
                    Draw(b, new Vector2(0.015f, 0.1f), new Vector2(0.08f, 0.1f));
                    break;
                case "CellPack":
                    Draw(b, new Vector2(-0.11f, -0.06f), new Vector2(0.11f, -0.06f));
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Box(b, new Vector2(i * 0.055f - 0.04f, -0.05f), new Vector2(i * 0.055f + 0.04f, 0.06f));
                        Box(b, new Vector2(i * 0.055f - 0.012f, 0.06f), new Vector2(i * 0.055f + 0.012f, 0.08f));
                        Draw(b, new Vector2(i * 0.055f - 0.04f, 0.035f), new Vector2(i * 0.055f + 0.04f, 0.035f));
                    }

                    break;
                default:
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Box(b, new Vector2(-0.1f, i * 0.045f - 0.025f), new Vector2(0.1f, i * 0.045f + 0.025f));
                        Draw(b, new Vector2(0f, i * 0.045f - 0.025f), new Vector2(0f, i * 0.045f + 0.025f));
                    }

                    break;
            }

            return b;
        }

        /// <summary>
        /// The pen plotter's body in plotter space, the wall <paramref name="wallZ"/> behind: a sage bed leaning back
        /// on two brackets from the wall with a faded drawing surface, the carriage's two rails across it, the paper
        /// roll over its top, and a charcoal control box on its own bracket at its side holding the ready lamp, rust
        /// weeping from its bolt.
        /// </summary>
        public static LowPolyMeshBuilder Plotter(float wallZ)
        {
            var b = new LowPolyMeshBuilder(800);
            Matrix4x4 bed = Bed;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 low = bed.MultiplyPoint3x4(new Vector3(side * LegHalfSpan, -BedHeight * 0.35f, -0.05f));
                Vector3 high = bed.MultiplyPoint3x4(new Vector3(side * LegHalfSpan, BedHeight * 0.35f, -0.05f));
                SiteKit.Bar(b, low, new Vector3(low.x, low.y, wallZ), 0.04f, PaletteSwatch.Metal);
                SiteKit.Bar(b, high, new Vector3(high.x, high.y, wallZ), 0.04f, PaletteSwatch.Metal);
                b.Box(At(new Vector3(low.x, (low.y + high.y) * 0.5f, wallZ + 0.01f)), new Vector3(0.1f,
                    high.y - low.y + 0.12f, 0.02f), PaletteSwatch.Charcoal, 0.004f);
                b.Prism(At(new Vector3(low.x, low.y, wallZ + 0.03f), AlongZ), 0.02f, 0.02f, 6, PaletteSwatch.Rust);
            }

            b.Box(bed * At(0f, 0f, -0.03f), new Vector3(BedWidth, BedHeight, 0.04f), PaletteSwatch.Sage, 0.01f);
            b.Box(bed * At(0f, 0f, -0.006f), new Vector3(DrawWidth + 0.04f, DrawHeight + 0.04f, 0.008f),
                PaletteSwatch.FadedPaint);
            for (int k = -1; k <= 1; k += 2)
            {
                float y = k * (DrawHeight * 0.5f + 0.04f);
                RecipeKit.Rod(b, bed.MultiplyPoint3x4(new Vector3(-BedWidth * 0.5f, y, 0.02f)),
                    bed.MultiplyPoint3x4(new Vector3(BedWidth * 0.5f, y, 0.02f)), 0.012f, 6, PaletteSwatch.Metal);
            }

            Vector3 roll = bed.MultiplyPoint3x4(new Vector3(0f, BedHeight * 0.5f + 0.05f, 0.02f));
            b.Prism(At(roll, AlongX), 0.05f, BedWidth - 0.06f, 10, PaletteSwatch.FadedPaint);
            b.Prism(At(roll, AlongX), 0.02f, BedWidth + 0.04f, 6, PaletteSwatch.Metal);
            var box = new Vector3(0.62f, 0.9f, 0.04f);
            b.Box(At(box), new Vector3(0.18f, 0.32f, 0.22f), PaletteSwatch.Charcoal, 0.012f);
            SiteKit.Bar(b, box + new Vector3(0f, -0.1f, -0.11f), new Vector3(box.x, box.y - 0.1f, wallZ), 0.03f,
                PaletteSwatch.Metal);
            b.Prism(At(PlotterLamp + Vector3.back * 0.02f, AlongZ), 0.03f, 0.03f, 8, PaletteSwatch.Metal);
            SiteKit.RustStreak(b, SiteKit.Face(box + new Vector3(0f, 0f, 0.112f), Vector3.forward, Vector3.up), -0.04f,
                0.08f, 0.12f, 0.016f);
            return b;
        }

        /// <summary>
        /// The pen carriage in its own space (origin at the drawing area's bottom-left corner on the bed, X across the
        /// bed, Y up it): an upright bar spanning the drawing area, riding the two rails on charcoal blocks.
        /// </summary>
        public static LowPolyMeshBuilder Carriage()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(0f, DrawHeight * 0.5f, 0.045f), new Vector3(0.04f, DrawHeight + 0.12f, 0.025f),
                PaletteSwatch.Metal, 0.004f);
            for (int k = 0; k <= 1; k++)
            {
                b.Box(At(0f, k * DrawHeight + (k == 0 ? -0.04f : 0.04f), 0.03f), new Vector3(0.07f, 0.05f, 0.05f),
                    PaletteSwatch.Charcoal, 0.006f);
            }

            return b;
        }

        /// <summary>
        /// The pen head in its own space, on the carriage's bar: a small block holding a worn orange pen.
        /// </summary>
        public static LowPolyMeshBuilder PenHead()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Box(At(0f, 0f, 0.07f), new Vector3(0.06f, 0.05f, 0.04f), PaletteSwatch.Charcoal, 0.005f);
            b.Prism(At(new Vector3(0f, 0f, 0.035f), AlongZ), 0.01f, 0.05f, 6, PaletteSwatch.WarmAccent);
            b.Prism(At(new Vector3(0f, 0f, 0.008f), AlongZ), 0.004f, 0.012f, 4, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The sheet being drawn, lying on the bed (bed frame), the size of the drawing area.</summary>
        public static LowPolyMeshBuilder BedSheet()
        {
            var b = new LowPolyMeshBuilder(20);
            b.Box(Matrix4x4.identity, new Vector3(DrawWidth, DrawHeight, SheetThickness), PaletteSwatch.DustShadow);
            return b;
        }

        /// <summary>The plotter's ready lamp, a small glass dome facing out of the control box.</summary>
        public static LowPolyMeshBuilder LampGlass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), 0.022f, 0.012f, 8, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>07 seen from the side, small: a body and three wheels.</summary>
        private static void Body(LowPolyMeshBuilder b)
        {
            Box(b, new Vector2(-0.07f, 0f), new Vector2(0.07f, 0.06f));
            for (int i = -1; i <= 1; i++)
            {
                Ring(b, new Vector2(i * 0.05f, -0.03f), 0.022f);
            }
        }

        private static void Spring(LowPolyMeshBuilder b, float x)
        {
            for (int k = 0; k < 3; k++)
            {
                float top = -0.06f - k * 0.02f;
                Draw(b, new Vector2(x - 0.01f, top), new Vector2(x + 0.01f, top - 0.01f));
                Draw(b, new Vector2(x + 0.01f, top - 0.01f), new Vector2(x - 0.01f, top - 0.02f));
            }
        }

        private static void Box(LowPolyMeshBuilder b, Vector2 low, Vector2 high)
        {
            Draw(b, low, new Vector2(high.x, low.y));
            Draw(b, new Vector2(high.x, low.y), high);
            Draw(b, high, new Vector2(low.x, high.y));
            Draw(b, new Vector2(low.x, high.y), low);
        }

        private static void Ring(LowPolyMeshBuilder b, Vector2 centre, float radius)
        {
            b.Torus(At(new Vector3(centre.x, centre.y, SheetThickness * 0.5f), AlongZ), radius, Line * 0.5f, 10, 3,
                PaletteSwatch.Cream);
        }

        /// <summary>A chalky cream line on the sheet's face.</summary>
        private static void Draw(LowPolyMeshBuilder b, Vector2 from, Vector2 to)
        {
            var a = new Vector3(from.x, from.y, SheetThickness * 0.5f);
            var c = new Vector3(to.x, to.y, SheetThickness * 0.5f);
            b.Box(Along(a, c), new Vector3(Line, Line, Vector3.Distance(a, c) + Line), PaletteSwatch.Cream);
        }
    }
}
