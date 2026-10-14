using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The lab's three stations (docs/features/M3-19 §5), each in its station space: the origin at the station's face
    /// at 07's lens height, +Z out of the station toward the turntable's axis, +Y up, +X to the left as 07 looks at
    /// it. The wall is <see cref="WallZ"/> behind and the floor <see cref="FloorY"/> below. Ines's survey chart (its
    /// light table is <see cref="ChartTableMeshes"/>, with her notes pinned round the big porthole), Kenji's tech board
    /// (a pin, a sheet and a lamp per 0.5 node under the recovery panel's four rows) and the keepsake shelf (deep open
    /// cubbies in four crew sections, brass name plates, dust outlines, a reading lamp each).
    /// </summary>
    internal static class LabStationMeshes
    {
        public static readonly float WallZ = -(LabMeshes.LiningApothem - LabMeshes.StationRadius);

        public const float FloorY = -LabMeshes.LensHeight;

        /// <summary>
        /// Ines's light table's place at the chart (station space): its top's middle 1.05 m above the floor, a little
        /// back from the station's face so the big porthole frames it from behind.
        /// </summary>
        public static readonly Vector3 TableCentre = new Vector3(0f, 1.05f + FloorY, -0.2f);

        // The board: cork on a wooden frame, the recovery panel across its head.
        private const float BoardWidth = 1.9f;
        private const float BoardBottom = 0.6f;
        private const float BoardTop = 1.76f;
        private const float PanelBottom = 1.8f;
        private const float PanelTop = 2.2f;
        private const int BoardColumns = 3;

        /// <summary>
        /// The recovery panel's rows, in <c>RecoveryTarget</c> order, with their names and lamp sockets.
        /// </summary>
        public static readonly string[] RowNames = { "Power", "Link", "Dish", "Answer" };

        public static readonly int[] RowLamps = { 3, 8, 4, 1 };

        private const int LinkRow = 1;

        private static readonly string[] RowLabels = { "POWER", "LINK", "DISH", "ANSWER" };

        /// <summary>
        /// Tech nodes on the board in 0.5 (one pin, sheet and lamp each, in <c>TechNodeId</c> order).
        /// </summary>
        public const int NodeCount = 9;

        // The keepsake shelf: one shelf of deep cubbies at lens height.
        private const float CubbyBottom = 0.9f;
        private const float CubbyTop = 1.6f;
        private const float CubbyDepth = 0.45f;
        private const float CubbyWidth = 0.34f;
        private const float Divider = 0.025f;

        /// <summary>The crew's sections left to right as 07 looks, and how many cubbies each keeps.</summary>
        public static readonly string[] Crew = { "INES", "RO", "THEO", "KENJI" };

        public static readonly int[] CrewCubbies = { 1, 2, 2, 1 };

        public const int KeepsakeSlotCount = 6;

        /// <summary>Ines's notes pinned on the wall either side of the big porthole.</summary>
        public static LowPolyMeshBuilder ChartStation()
        {
            var b = new LowPolyMeshBuilder(120);
            float[] notes = { 0.88f, -0.92f, 1f, -0.82f };
            for (int i = 0; i < notes.Length; i++)
            {
                float y = (i < 2 ? 0.55f : 0.1f) + (i % 2) * 0.06f;
                b.Box(At(new Vector3(notes[i], y, WallZ + 0.005f), new Vector3(0f, 0f, (i % 3 - 1) * 6f)),
                    new Vector3(0.18f, 0.13f, 0.004f), PaletteSwatch.Cream);
                b.Icosphere(At(notes[i], y + 0.05f, WallZ + 0.012f), 0.012f, 0, PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>
        /// Kenji's tech board: cork in a wooden frame on the wall, red thread from pin to pin along the tree's lines,
        /// and the recovery panel's metal header with the protocol's name and each row's label and empty sockets.
        /// </summary>
        public static LowPolyMeshBuilder Board()
        {
            var b = new LowPolyMeshBuilder(1600);
            float midY = FloorY + (BoardBottom + BoardTop) * 0.5f;
            b.Box(At(0f, midY, WallZ + 0.03f), new Vector3(BoardWidth + 0.1f, BoardTop - BoardBottom + 0.1f, 0.05f),
                PaletteSwatch.Wood, 0.01f);
            b.Box(At(0f, midY, WallZ + 0.058f), new Vector3(BoardWidth, BoardTop - BoardBottom, 0.01f),
                PaletteSwatch.Honey);
            for (int n = 0; n < NodeCount; n++)
            {
                int right = n % BoardColumns < BoardColumns - 1 ? n + 1 : -1;
                int below = n + BoardColumns < NodeCount ? n + BoardColumns : -1;
                foreach (int other in new[] { right, below })
                {
                    if (other >= 0)
                    {
                        RecipeKit.Rod(b, PinPosition(n) + Vector3.back * 0.005f,
                            PinPosition(other) + Vector3.back * 0.005f, 0.004f, 4, PaletteSwatch.WarmAccent);
                    }
                }

                b.Prism(At(LampPosition(n) - Vector3.forward * 0.01f, AlongZ), NodeLampRadius + 0.01f, 0.02f, 8,
                    PaletteSwatch.Charcoal);
            }

            float panelMid = FloorY + (PanelBottom + PanelTop) * 0.5f;
            b.Box(At(0f, panelMid, WallZ + 0.04f), new Vector3(BoardWidth + 0.1f, PanelTop - PanelBottom, 0.06f),
                PaletteSwatch.Metal, 0.01f);
            Glyphs.Write(b, At(0f, FloorY + PanelTop - 0.045f, WallZ + 0.071f), "RECOVERY PROTOCOL", 0.04f,
                PaletteSwatch.Cream, PaletteSwatch.Metal);
            for (int row = 0; row < RowNames.Length; row++)
            {
                Vector3 label = RowOrigin(row) + new Vector3(0.62f, 0f, 0.001f);
                Glyphs.Write(b, At(label), RowLabels[row], 0.035f, PaletteSwatch.Cream, PaletteSwatch.Metal);
                for (int lamp = 0; lamp < RowLamps[row]; lamp++)
                {
                    float radius = IsPeakLamp(row, lamp) ? PeakLampRadius : RowLampRadius;
                    b.Prism(At(RowLampPosition(row, lamp) - Vector3.forward * 0.008f, AlongZ), radius + 0.012f,
                        0.015f, 8, PaletteSwatch.Charcoal);
                }
            }

            return b;
        }

        /// <summary>Pin <paramref name="node"/> (station space): a brass map pin on the cork.</summary>
        public static LowPolyMeshBuilder Pin()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Icosphere(At(0f, 0f, 0.025f), 0.022f, 1, PaletteSwatch.Honey);
            RecipeKit.Rod(b, Vector3.zero, new Vector3(0f, 0f, 0.025f), 0.004f, 4, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The sheet a decoded node pins under its pin (sheet space: the pin at the origin, the paper hanging below
        /// it): cream paper with the kit's ink lines, a little skewed.
        /// </summary>
        public static LowPolyMeshBuilder Sheet(int node)
        {
            var b = new LowPolyMeshBuilder(80);
            float skew = (node % 3 - 1) * 3f;
            Matrix4x4 paper = At(new Vector3(0f, -0.12f, 0.004f), new Vector3(0f, 0f, skew));
            b.Box(paper, new Vector3(0.34f, 0.24f, 0.003f), PaletteSwatch.Cream);
            for (int line = 0; line < 3; line++)
            {
                float width = 0.24f - ((node + line) % 3) * 0.05f;
                b.Box(paper * At(0f, 0.06f - line * 0.05f, 0.002f), new Vector3(width, 0.008f, 0.002f),
                    PaletteSwatch.Charcoal);
            }

            b.Box(paper * At(0.09f - (node % 2) * 0.18f, -0.08f, 0.002f), new Vector3(0.05f, 0.04f, 0.002f),
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>A small round lamp glass (station space for the board's node lamps, centred on its node).</summary>
        public static LowPolyMeshBuilder LampGlass(float radius)
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), radius, 0.02f, 8, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// Where node <paramref name="node"/>'s pin is on the board (three columns, read left to right as 07 looks).
        /// </summary>
        public static Vector3 PinPosition(int node)
        {
            int column = node % BoardColumns;
            int row = node / BoardColumns;
            float x = (1 - column) * 0.6f;
            float y = FloorY + BoardTop - 0.14f - row * 0.38f;
            return new Vector3(x, y, WallZ + 0.064f);
        }

        /// <summary>Node <paramref name="node"/>'s lamp: beside its pin, toward the board's right edge.</summary>
        public static Vector3 LampPosition(int node)
        {
            return PinPosition(node) + new Vector3(-0.21f, 0f, 0.006f);
        }

        /// <summary>The recovery panel's centre (station space): the RecoveryPanel node.</summary>
        public static Vector3 PanelCentre => new Vector3(0f, FloorY + (PanelBottom + PanelTop) * 0.5f, WallZ + 0.07f);

        /// <summary>Row <paramref name="row"/>'s origin on the panel (station space), its label to the left.</summary>
        public static Vector3 RowOrigin(int row)
        {
            return new Vector3(0f, FloorY + PanelTop - 0.12f - row * 0.075f, WallZ + 0.071f);
        }

        /// <summary>
        /// Lamp <paramref name="lamp"/> of row <paramref name="row"/> (station space), in reading order.
        /// </summary>
        public static Vector3 RowLampPosition(int row, int lamp)
        {
            return RowOrigin(row) + new Vector3(0.32f - lamp * 0.11f, 0f, 0.002f);
        }

        /// <summary>
        /// The board's node lamps, the panel's row lamps and the Link row's larger last lamp: the Peak's foot.
        /// </summary>
        public const float NodeLampRadius = 0.025f;

        public const float RowLampRadius = 0.022f;

        public const float PeakLampRadius = 0.034f;

        /// <summary>
        /// Whether lamp <paramref name="lamp"/> of row <paramref name="row"/> is the Link row's last, the Peak's foot.
        /// </summary>
        public static bool IsPeakLamp(int row, int lamp)
        {
            return row == LinkRow && lamp == RowLamps[row] - 1;
        }

        /// <summary>
        /// The keepsake shelf: one long shelf of deep open cubbies at lens height on a cabinet, thicker dividers
        /// between the crew's sections, a brass name plate under each, a dust outline on each cubby's floor where its
        /// thing stood, and an arm lamp's shade over each section.
        /// </summary>
        public static LowPolyMeshBuilder Keepsakes()
        {
            var b = new LowPolyMeshBuilder(1400);
            float width = ShelfWidth;
            float back = WallZ;
            float frontZ = back + CubbyDepth;
            float bottom = FloorY + CubbyBottom;
            float top = FloorY + CubbyTop;
            b.Box(At(0f, FloorY + CubbyBottom * 0.5f, back + CubbyDepth * 0.5f), new Vector3(width + 0.06f,
                CubbyBottom, CubbyDepth), PaletteSwatch.Wood, 0.02f);
            b.Box(At(0f, (bottom + top) * 0.5f, back + 0.01f), new Vector3(width, top - bottom, 0.02f),
                PaletteSwatch.WornPanel);
            b.Box(At(0f, top + 0.02f, back + CubbyDepth * 0.5f), new Vector3(width + 0.06f, 0.04f, CubbyDepth + 0.02f),
                PaletteSwatch.Wood, 0.01f);
            float x = width * 0.5f;
            int slot = 0;
            for (int section = 0; section < Crew.Length; section++)
            {
                float sectionWidth = CrewCubbies[section] * (CubbyWidth + Divider);
                float thick = section == 0 ? 0.05f : 0.045f;
                b.Box(At(x, (bottom + top) * 0.5f, back + CubbyDepth * 0.5f), new Vector3(thick, top - bottom,
                    CubbyDepth), PaletteSwatch.Wood);
                for (int c = 0; c < CrewCubbies[section]; c++)
                {
                    float cubbyX = KeepsakeSlot(slot).x;
                    if (c > 0)
                    {
                        float between = cubbyX + (CubbyWidth + Divider) * 0.5f;
                        b.Box(At(between, (bottom + top) * 0.5f, back + CubbyDepth * 0.5f),
                            new Vector3(Divider, top - bottom, CubbyDepth), PaletteSwatch.Wood);
                    }

                    b.Torus(At(cubbyX, bottom + 0.004f, back + CubbyDepth * 0.5f), 0.1f + (slot % 2) * 0.02f, 0.012f,
                        10, 3, PaletteSwatch.DustLight);
                    slot++;
                }

                float plateX = x - sectionWidth * 0.5f;
                b.Box(At(plateX, bottom - 0.06f, frontZ + 0.006f), new Vector3(Mathf.Min(0.3f, sectionWidth - 0.06f),
                    0.07f, 0.012f), PaletteSwatch.Honey, 0.004f);
                Glyphs.Write(b, At(plateX, bottom - 0.06f, frontZ + 0.013f), Crew[section], 0.035f,
                    PaletteSwatch.Charcoal, PaletteSwatch.Honey);
                LampArm(b, ReadingLamp(section));
                x -= sectionWidth;
            }

            b.Box(At(x, (bottom + top) * 0.5f, back + CubbyDepth * 0.5f), new Vector3(0.05f, top - bottom, CubbyDepth),
                PaletteSwatch.Wood);
            return b;
        }

        /// <summary>The shelf's width across its cubbies and dividers.</summary>
        private static float ShelfWidth => KeepsakeSlotCount * (CubbyWidth + Divider);

        /// <summary>
        /// Keepsake slot <paramref name="slot"/> (station space): the middle of its cubby's floor, sections left to
        /// right as 07 looks (Ines, Ro, Ro, Theo, Theo, Kenji).
        /// </summary>
        public static Vector3 KeepsakeSlot(int slot)
        {
            float x = ShelfWidth * 0.5f - (slot + 0.5f) * (CubbyWidth + Divider);
            return new Vector3(x, FloorY + CubbyBottom + 0.004f, WallZ + CubbyDepth * 0.5f);
        }

        /// <summary>Section <paramref name="section"/>'s reading lamp bulb (station space), over its cubbies.</summary>
        public static Vector3 ReadingLamp(int section)
        {
            float x = ShelfWidth * 0.5f;
            for (int i = 0; i < section; i++)
            {
                x -= CrewCubbies[i] * (CubbyWidth + Divider);
            }

            x -= CrewCubbies[section] * (CubbyWidth + Divider) * 0.5f;
            return new Vector3(x, FloorY + CubbyTop + 0.22f, WallZ + 0.2f);
        }

        /// <summary>A reading lamp's bulb under its shade, centred on its node.</summary>
        public static LowPolyMeshBuilder ReadingBulb()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Icosphere(Matrix4x4.identity, 0.035f, 1, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>An arm lamp from the wall: a bracket and a brass shade over the bulb.</summary>
        private static void LampArm(LowPolyMeshBuilder b, Vector3 bulb)
        {
            b.Frustum(At(bulb + Vector3.up * 0.06f), 0.09f, 0.035f, 0.08f, 8, PaletteSwatch.Honey);
            RecipeKit.Rod(b, bulb + Vector3.up * 0.1f, new Vector3(bulb.x, bulb.y + 0.16f, WallZ), 0.01f, 4,
                PaletteSwatch.Charcoal);
        }
    }
}
