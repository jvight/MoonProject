using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Home's open supply shelves and the crates on them (M3-25 §3.1, T2, T4), all built for wheels (ruling 12):
    /// rover-height racks that stand on the belly ring's sockets, slots for stacks of up to three crates, a crate per
    /// material in its colour so home's stash reads from the dock, a warm lamp over each rack; and the crew's sealed
    /// supply crate, cut once to open.
    /// Rack space: the origin on the ground at the rack's middle, +Z out of its open front, +Y up.
    /// </summary>
    internal static class SupplyShelfMeshes
    {
        /// <summary>Slots per rack: <see cref="Columns"/> across, <see cref="Tiers"/> boards high.</summary>
        public const int Columns = 4;

        public const int Tiers = 3;

        public const int Slots = Columns * Tiers;

        /// <summary>How many crates a slot stacks.</summary>
        public const int StackHeight = 3;

        /// <summary>The materials a crate comes in (bundle and crate colours of M3-27); the rest come later.</summary>
        public static readonly string[] CrateMaterials = { "Metal", "Wiring", "Optics", "OrangeBeads" };

        public static readonly Vector3 CrateSize = new Vector3(0.3f, 0.112f, 0.34f);

        // The lid ridge stands this proud; the next crate up sits on the ridge's height.
        private const float Ridge = 0.012f;

        /// <summary>From one stacked crate's bottom to the next's.</summary>
        public static float StackPitch => CrateSize.y + Ridge + 0.004f;

        private const float RackWidth = 1.5f;
        private const float RackDepth = 0.46f;
        private const float RackHeight = 1.45f;
        private const float FootHeight = 0.12f;
        private const float BoardThickness = 0.04f;
        private const float Post = 0.06f;
        private const float LampHeight = RackHeight + 0.12f;

        // The crew crate (T4): crew-sized, its lid one cut away.
        private static readonly Vector3 CrewCrateSize = new Vector3(0.78f, 0.44f, 0.52f);
        private const float LidHeight = 0.1f;

        /// <summary>Where slot <paramref name="index"/>'s stack stands (rack space, its bottom centre).</summary>
        public static Vector3 Slot(int index)
        {
            int column = index % Columns;
            int tier = index / Columns;
            float pitch = (RackWidth - Post) / Columns;
            float x = -RackWidth * 0.5f + Post * 0.5f + pitch * (column + 0.5f);
            return new Vector3(x, Board(tier) + BoardThickness * 0.5f, 0.02f);
        }

        /// <summary>The rack's lamp, over its open front (rack space).</summary>
        public static Vector3 Lamp => new Vector3(0f, LampHeight - 0.05f, RackDepth * 0.5f + 0.06f);

        /// <summary>The rack: four posts on feet, three boards and a lamp arm over the front, in worn crew paint.</summary>
        public static LowPolyMeshBuilder Rack()
        {
            var b = new LowPolyMeshBuilder(400);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var foot = new Vector3(x * (RackWidth - Post) * 0.5f, 0f, z * (RackDepth - Post) * 0.5f);
                    b.Box(At(foot + Vector3.up * RackHeight * 0.5f), new Vector3(Post, RackHeight, Post),
                        PaletteSwatch.Metal, 0.008f);
                    b.Box(At(foot + Vector3.up * 0.02f), new Vector3(Post + 0.06f, 0.04f, Post + 0.06f),
                        PaletteSwatch.Charcoal);
                }
            }

            for (int tier = 0; tier < Tiers; tier++)
            {
                b.Box(At(0f, Board(tier), 0f), new Vector3(RackWidth, BoardThickness, RackDepth),
                    PaletteSwatch.WornPanel, 0.006f);
            }

            b.Box(At(0f, RackHeight, 0f), new Vector3(RackWidth, BoardThickness, RackDepth), PaletteSwatch.WornPanel,
                0.006f);
            SiteKit.Bar(b, new Vector3(0f, RackHeight, 0f), new Vector3(0f, LampHeight, Lamp.z), 0.03f,
                PaletteSwatch.Metal);
            b.Frustum(At(Lamp + Vector3.up * 0.04f), 0.07f, 0.03f, 0.05f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The rack lamp's glass (lamp space): warm when home has power.</summary>
        public static LowPolyMeshBuilder LampGlass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Icosphere(Matrix4x4.identity, 0.035f, 1, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// A stack's crate in <paramref name="material"/>'s colour (crate space: its bottom centre on the board): a
        /// low lidded box with dark corner straps, a little grab slot in front, and a low lid ridge whose shape differs by
        /// material so the stacks read apart without their colour too; three stack on a board.
        /// </summary>
        public static LowPolyMeshBuilder Crate(string material)
        {
            var b = new LowPolyMeshBuilder(160);
            PaletteSwatch colour = CrateColour(material);
            b.Box(At(0f, CrateSize.y * 0.5f, 0f), CrateSize, colour, 0.012f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (CrateSize.x * 0.5f - 0.03f), CrateSize.y * 0.5f, 0f),
                    new Vector3(0.025f, CrateSize.y, CrateSize.z + 0.008f), PaletteSwatch.Charcoal);
            }

            b.Box(At(0f, CrateSize.y * 0.6f, CrateSize.z * 0.5f + 0.002f), new Vector3(0.1f, 0.024f, 0.006f),
                PaletteSwatch.Charcoal);
            float top = CrateSize.y + Ridge * 0.5f;
            switch (material)
            {
                case "Metal":
                    b.Box(At(0f, top, 0f), new Vector3(CrateSize.x * 0.6f, Ridge, CrateSize.z * 0.7f), colour, 0.004f);
                    break;
                case "Wiring":
                    b.Torus(At(0f, top, 0f), 0.07f, Ridge * 0.5f, 10, 3, colour);
                    break;
                case "Optics":
                    b.Prism(At(0f, top, 0f), 0.08f, Ridge, 8, colour);
                    break;
                default:
                    for (int i = 0; i < 3; i++)
                    {
                        b.Icosphere(At(-0.06f + i * 0.06f, top, 0.02f * (i - 1)), Ridge * 0.6f, 0, colour);
                    }

                    break;
            }

            return b;
        }

        /// <summary>The crate's colour by material (M3-25 T2, M3-27): Metal grey, Wiring copper, Optics violet, as
        /// their bundles; orange beads the soil's rust orange.</summary>
        public static PaletteSwatch CrateColour(string material)
        {
            switch (material)
            {
                case "Metal":
                    return PaletteSwatch.Metal;
                case "Wiring":
                    return PaletteSwatch.WarmAccent;
                case "Optics":
                    return PaletteSwatch.SkyHorizon;
                case "OrangeBeads":
                    return PaletteSwatch.Rust;
                default:
                    throw new System.ArgumentException($"No crate colour for '{material}'.", nameof(material));
            }
        }

        /// <summary>The crew crate's body and frame (crate space: its bottom centre on the ground), sealed and dusty.</summary>
        public static LowPolyMeshBuilder CrewCrateFrame()
        {
            var b = new LowPolyMeshBuilder(200);
            float body = CrewCrateSize.y - LidHeight;
            MeshRange box = b.Box(At(0f, body * 0.5f, 0f), new Vector3(CrewCrateSize.x, body, CrewCrateSize.z),
                PaletteSwatch.WornPanel, 0.015f);
            SiteKit.DustCap(b, box);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * CrewCrateSize.x * 0.5f, body * 0.5f, 0f), new Vector3(0.04f, body + 0.02f,
                    CrewCrateSize.z + 0.02f), PaletteSwatch.FadedAccent);
                b.Box(At(side * (CrewCrateSize.x * 0.5f + 0.02f), body * 0.55f, 0f), new Vector3(0.03f, 0.05f, 0.16f),
                    PaletteSwatch.Charcoal);
            }

            Matrix4x4 front = SiteKit.Face(new Vector3(0f, body * 0.5f, CrewCrateSize.z * 0.5f + 0.002f),
                Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, front, 0.25f, body * 0.3f, 0.2f);
            return b;
        }

        /// <summary>The crew crate's lid (lid space: its bottom centre on the body), the cut opens it.</summary>
        public static LowPolyMeshBuilder CrewCrateLid()
        {
            var b = new LowPolyMeshBuilder(80);
            MeshRange lid = b.Box(At(0f, LidHeight * 0.5f, 0f),
                new Vector3(CrewCrateSize.x + 0.02f, LidHeight, CrewCrateSize.z + 0.02f), PaletteSwatch.WornPanel,
                0.012f);
            SiteKit.DustCap(b, lid);
            b.Box(At(0f, LidHeight * 0.5f, CrewCrateSize.z * 0.5f + 0.014f), new Vector3(0.16f, 0.05f, 0.012f),
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The lid's seal (lid space): a thin glass bead all round the seam, so the crate shows it can be cut and reads
        /// from afar at night.
        /// </summary>
        public static LowPolyMeshBuilder CrewCrateSeal()
        {
            var b = new LowPolyMeshBuilder(60);
            const float bead = 0.014f;
            float width = CrewCrateSize.x + 0.024f;
            float depth = CrewCrateSize.z + 0.024f;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(0f, bead * 0.5f, side * depth * 0.5f), new Vector3(width, bead, bead), PaletteSwatch.LampGlass);
                b.Box(At(side * width * 0.5f, bead * 0.5f, 0f), new Vector3(bead, bead, depth - bead),
                    PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>Where the lid sits on the crate (crate space).</summary>
        public static Vector3 LidSeat => new Vector3(0f, CrewCrateSize.y - LidHeight, 0f);

        /// <summary>
        /// The lid's seal, where the beam cuts once to open it: the CutPoint (lid space, +Z out of the front seam) and
        /// its cut line along the seam (CutPoint space).
        /// </summary>
        public static Vector3 SealPoint => new Vector3(0f, 0.01f, CrewCrateSize.z * 0.5f + 0.012f);

        public static Vector3[] SealLine => new[]
        {
            new Vector3(-CrewCrateSize.x * 0.42f, 0f, 0f), new Vector3(CrewCrateSize.x * 0.42f, 0f, 0f),
        };

        private static float Board(int tier)
        {
            return FootHeight + tier * (RackHeight - FootHeight) / Tiers;
        }
    }
}
