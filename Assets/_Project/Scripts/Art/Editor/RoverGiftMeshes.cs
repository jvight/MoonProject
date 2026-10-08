using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The friends' gifts on 07 (docs/features/M3-11-visible-progression.md): signs of solitude healed, each a node of
    /// RoverModel hidden until earned. Tilly's replacement cell for the solar wing's gap, the fresh "07" Ro stencils
    /// over the faded one, and the little radio pennant Bell clips to the antenna.
    /// </summary>
    internal static class RoverGiftMeshes
    {
        // The fresh serial: a newly painted stripe patch laid on the old paint, thick enough to bury the faded
        // numerals, with crisp cream stencil numerals over a charcoal drop shadow.
        private const float PatchThickness = 0.016f;
        private const float PatchLength = 0.21f;
        private const float NumeralHeight = 0.068f;
        private const float ShadowOffset = 0.005f;
        private const float ShadowDepth = 0.003f;

        /// <summary>
        /// Where the pennant clips onto the whip, in antenna space (on the upper segment, past the kink).
        /// </summary>
        public static Vector3 PennantClip =>
            Vector3.Lerp(RoverMeshes.AntennaKink, RoverMeshes.AntennaTipPosition, 0.3f);

        /// <summary>The cell the years took, in solar wing space (the CellFilled pivot).</summary>
        public static Vector3 CellFilledCentre =>
            RoverMeshes.WingCellCentre(RoverMeshes.MissingRow, RoverMeshes.MissingColumn);

        /// <summary>
        /// Ro's fresh "07" in body space (Body/Decal07Fresh): on both flank stripes and the back stripe, each a
        /// fresh patch of the worn orange that hides the faded serial under it, with bold cream stencil numerals.
        /// </summary>
        public static LowPolyMeshBuilder FreshSerial()
        {
            var b = new LowPolyMeshBuilder(900);
            const float lift = RoverMeshes.PaintProud + RoverMeshes.PaintThickness * 0.5f + PatchThickness * 0.5f;
            const float flank = RoverMeshes.BodyHalfWidth + lift;
            const float serialZ = 0.17f;
            Serial(b, SiteKit.Face(new Vector3(flank, RoverMeshes.StripeY, serialZ), Vector3.right, Vector3.up),
                PatchLength);
            Serial(b, SiteKit.Face(new Vector3(-flank, RoverMeshes.StripeY, serialZ), Vector3.left, Vector3.up),
                PatchLength);
            const float back = RoverMeshes.BodyBack - lift;
            Serial(b, SiteKit.Face(new Vector3(0f, RoverMeshes.StripeY, back), Vector3.back, Vector3.up), PatchLength);
            return b;
        }

        /// <summary>
        /// Tilly's replacement cell (SolarWing/CellFilled, origin at the cell centre): a violet cell a hair proud of
        /// the old ones, held by two strips of her honey tape.
        /// </summary>
        public static LowPolyMeshBuilder ReplacementCell()
        {
            var b = new LowPolyMeshBuilder(60);
            Vector2 cell = RoverMeshes.WingCell;
            b.Box(At(0f, 0.002f, 0f), new Vector3(cell.x - 0.004f, RoverMeshes.CellThickness, cell.y - 0.004f),
                PaletteSwatch.SkyHorizon);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(new Vector3(side * cell.x * 0.32f, 0.009f, side * cell.y * 0.38f), new Vector3(0f, 35f, 0f)),
                    new Vector3(0.022f, 0.004f, 0.07f), PaletteSwatch.Honey);
            }

            return b;
        }

        /// <summary>
        /// Bell's radio pennant (Antenna/Pennant, origin at its clip on the whip): a small stiff rod reaching back
        /// and out like the old flag poles, a honey pennant hanging from it with a worn orange band.
        /// </summary>
        public static LowPolyMeshBuilder Pennant()
        {
            var b = new LowPolyMeshBuilder(80);
            Vector3 reach = new Vector3(0.35f, 0f, -1f).normalized * 0.18f;
            b.Prism(At(new Vector3(0f, 0.002f, 0f)), 0.016f, 0.03f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, Vector3.zero, reach, 0.006f, 5, PaletteSwatch.Metal);
            Matrix4x4 cloth = SiteKit.Face(new Vector3(0f, -0.006f, 0f), Vector3.Cross(reach, Vector3.up), Vector3.up);
            Vector2[] flag =
            {
                new Vector2(0f, 0f), new Vector2(0.175f, 0f), new Vector2(0f, -0.11f),
            };
            b.Extrude(cloth, flag, 0.004f, PaletteSwatch.Honey);
            Vector2[] band =
            {
                new Vector2(0.025f, -0.004f), new Vector2(0.06f, -0.004f), new Vector2(0.06f, -0.073f),
                new Vector2(0.025f, -0.089f),
            };
            b.Extrude(cloth, band, 0.007f, PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>
        /// One fresh serial on a face frame centred on the stripe: the patch, the shadow, the numerals.
        /// </summary>
        private static void Serial(LowPolyMeshBuilder b, Matrix4x4 face, float length)
        {
            b.Box(face, new Vector3(length, RoverMeshes.StripeHeight + 0.004f, PatchThickness),
                PaletteSwatch.WarmAccent, 0.002f);
            float surface = PatchThickness * 0.5f;
            Glyphs.Write(b, face * At(ShadowOffset, -ShadowOffset, surface - ShadowDepth), "07", NumeralHeight,
                PaletteSwatch.Charcoal, PaletteSwatch.WarmAccent);
            Glyphs.Write(b, face * At(0f, 0f, surface), "07", NumeralHeight, PaletteSwatch.Cream,
                PaletteSwatch.WarmAccent);
        }
    }
}
