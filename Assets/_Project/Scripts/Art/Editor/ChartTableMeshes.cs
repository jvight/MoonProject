using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Ines's light table in the alcove behind the deck hatch (M3-07), framed by the open doorway at 07's lens height
    /// on the deck. Table space: the origin at the middle of the table's top, +X across it, +Y up its slope, +Z out of
    /// it toward the doorway. The paper lies on a lit panel; the panel's warm rim and glow show the station's reach.
    /// </summary>
    internal static class ChartTableMeshes
    {
        /// <summary>How far the table tips back from upright, so 07's lens on the deck looks onto it.</summary>
        public const float Tilt = 40f;

        private const float TopWidth = 0.7f;
        private const float TopHeight = 0.52f;
        private const float TopThickness = 0.04f;
        private const float PanelWidth = 0.6f;
        private const float PanelHeight = 0.42f;
        private const float PanelThickness = 0.006f;
        private const float SheetWidth = 0.56f;
        private const float SheetHeight = 0.38f;
        private const float SheetThickness = 0.003f;
        private const float LegBar = 0.035f;
        private const float RailGap = 0.06f;
        private const float RailBar = 0.018f;
        private const float RailLift = 0.03f;
        private const float BridgeLift = 0.045f;

        /// <summary>How far the chart pen's carriage travels across the paper (table space X).</summary>
        public const float DrawWidth = 0.5f;

        /// <summary>How far the pen head travels along the carriage (table space Y).</summary>
        public const float DrawHeight = 0.34f;

        /// <summary>
        /// ChartPenCarriage's place at the paper's lower left corner, where the plotter's pen head (reused as is)
        /// puts its tip on the paper.
        /// </summary>
        public static Vector3 CarriageHome => new Vector3(-DrawWidth * 0.5f, -DrawHeight * 0.5f, SheetTop - 0.002f);

        /// <summary>
        /// Where the table's top sits in lander space: in the alcove, its middle a little under 07's lens on the deck
        /// so the doorway frames it (the opening runs from the deck to the band).
        /// </summary>
        public static Vector3 Centre => new Vector3(0f, LanderMeshes.DeckTop + 1.05f,
            DeckHatchMeshes.Face - DeckHatchMeshes.AlcoveDepth + 0.32f);

        public static Quaternion Rotation => Place.Rotation(new Vector3(-Tilt, 0f, 0f));

        /// <summary>The top of the lit panel's glass, where the paper lies (table space z).</summary>
        public static float PanelTop => PanelThickness;

        /// <summary>The paper's face (table space z): the chart pen's tip touches it.</summary>
        public static float SheetTop => PanelTop + SheetThickness;

        /// <summary>
        /// The table in table space: a worn top with a raised rim round the lit panel, on four legs down to the
        /// alcove's floor.
        /// </summary>
        public static LowPolyMeshBuilder Table()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Box(At(0f, 0f, -TopThickness * 0.5f), new Vector3(TopWidth, TopHeight, TopThickness),
                PaletteSwatch.WornPanel, 0.01f);
            float rimX = (TopWidth + PanelWidth) * 0.25f;
            float rimY = (TopHeight + PanelHeight) * 0.25f;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * rimX, 0f, 0.006f), new Vector3((TopWidth - PanelWidth) * 0.5f, TopHeight, 0.012f),
                    PaletteSwatch.Charcoal);
                b.Box(At(0f, side * rimY, 0.006f), new Vector3(PanelWidth, (TopHeight - PanelHeight) * 0.5f, 0.012f),
                    PaletteSwatch.Charcoal);
            }

            Matrix4x4 table = Matrix4x4.TRS(Centre, Rotation, Vector3.one);
            Matrix4x4 toTable = table.inverse;
            float floor = LanderMeshes.DoorBottom;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    var corner = new Vector3(x * (TopWidth * 0.5f - 0.05f), y * (TopHeight * 0.5f - 0.05f),
                        -TopThickness);
                    Vector3 cornerInLander = table.MultiplyPoint3x4(corner);
                    Vector3 foot = toTable.MultiplyPoint3x4(new Vector3(cornerInLander.x, floor, cornerInLander.z));
                    SiteKit.Bar(b, corner, foot, LegBar, PaletteSwatch.WornPanel);
                }
            }

            return b;
        }

        /// <summary>
        /// The chart pen's frame (table space): Kenji's smaller, brass sibling of the plotter's gantry, two rails
        /// across the table above and below the paper on little posts at the rim.
        /// </summary>
        public static LowPolyMeshBuilder Gantry()
        {
            var b = new LowPolyMeshBuilder(120);
            float halfRun = DrawWidth * 0.5f + 0.06f;
            for (int side = -1; side <= 1; side += 2)
            {
                float y = side * (DrawHeight * 0.5f + RailGap);
                SiteKit.Bar(b, new Vector3(-halfRun, y, RailLift), new Vector3(halfRun, y, RailLift), RailBar,
                    PaletteSwatch.Honey);
                for (int end = -1; end <= 1; end += 2)
                {
                    b.Box(At(end * halfRun, y, RailLift * 0.5f), new Vector3(0.03f, 0.03f, RailLift + 0.01f),
                        PaletteSwatch.Charcoal, 0.004f);
                }
            }

            return b;
        }

        /// <summary>
        /// The chart pen's carriage (carriage space: its origin under the pen's tip at the bottom of its travel):
        /// a brass bridge spanning the paper from rail to rail with a roller block on each.
        /// </summary>
        public static LowPolyMeshBuilder Carriage()
        {
            var b = new LowPolyMeshBuilder(60);
            float low = -RailGap;
            float high = DrawHeight + RailGap;
            SiteKit.Bar(b, new Vector3(0f, low, BridgeLift), new Vector3(0f, high, BridgeLift), 0.02f,
                PaletteSwatch.Honey);
            foreach (float y in new[] { low, high })
            {
                b.Box(At(0f, y, RailLift + 0.01f), new Vector3(0.045f, 0.035f, 0.035f), PaletteSwatch.Charcoal, 0.005f);
            }

            return b;
        }

        /// <summary>The lit panel under the paper (table space): dark glass, warm when the table is lit.</summary>
        public static LowPolyMeshBuilder Glow()
        {
            var b = new LowPolyMeshBuilder(12);
            b.Box(At(0f, 0f, PanelThickness * 0.5f), new Vector3(PanelWidth, PanelHeight, PanelThickness),
                PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>Ines's chart paper (table space) on the lit panel, its rim of light showing round it.</summary>
        public static LowPolyMeshBuilder Sheet()
        {
            var b = new LowPolyMeshBuilder(12);
            b.Box(At(0f, 0f, PanelTop + SheetThickness * 0.5f), new Vector3(SheetWidth, SheetHeight, SheetThickness),
                PaletteSwatch.Cream);
            return b;
        }
    }
}
