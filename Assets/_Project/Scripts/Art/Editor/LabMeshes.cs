using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The crew's lab inside the habitat lander's cabin (docs/features/M3-19 §5, §6): the floor round the shaft the
    /// turntable rises through, the octagon's lining (warm wood wainscot under worn panels, ribs on the corners), the
    /// ceiling, the inside of the portholes looking out at the night, the camera's ring rail just under the ceiling
    /// and four warm wall lamps between the stations. Lab space: the origin at the middle of the cabin floor (the
    /// platform's top when it is up), +Z the lander's front, +Y up. The stations themselves are
    /// <see cref="LabStationMeshes"/>.
    /// </summary>
    internal static class LabMeshes
    {
        private const int Sides = 8;

        /// <summary>The floor plate's thickness under the cabin floor's surface.</summary>
        public const float FloorThickness = 0.04f;

        public const float Height = LanderMeshes.CabinInnerHeight;

        /// <summary>The lining's distance from the axis across its flats, a hair inside the cabin's walls.</summary>
        public const float LiningApothem = LanderMeshes.CabinInnerApothem - 0.02f;

        /// <summary>The camera's ring rail: 0.4 m in from the walls, just under the ceiling.</summary>
        public const float RingRadius = LanderMeshes.CabinInnerApothem - 0.4f;

        public const float RingHeight = Height - 0.2f;

        /// <summary>07's lens height on the turntable: every station's frame sits at it.</summary>
        public const float LensHeight = 1.25f;

        /// <summary>How far each station's frame stands from the axis: the front of the ring of stations.</summary>
        public const float StationRadius = 2.2f;

        public const int CabinLampCount = 4;

        private const float WainscotHeight = 0.9f;
        private const float RibWidth = 0.1f;
        private const float RailTube = 0.035f;
        private const float WindowInset = 0.05f;

        private static readonly float WindowHeight = 1.5f;
        private static readonly float PortholeHeight = LanderMeshes.BigPortholeHeight - LanderMeshes.FloorY;

        /// <summary>The stations' yaws round the turntable, clockwise from above in <c>LabStation</c> order.</summary>
        public static readonly float[] StationYaws = { LanderMeshes.BigPortholeYaw, -45f, 45f, 135f };

        /// <summary>The station names, in the same order (the <c>LabStation</c> enum's).</summary>
        public static readonly string[] StationNames = { "Chart", "Reader", "Board", "Keepsakes" };

        /// <summary>
        /// Station <paramref name="index"/>'s frame in lab space: at its face, at 07's lens height, +Z out of the
        /// station toward the axis.
        /// </summary>
        public static Vector3 StationPosition(int index)
        {
            return Rotation(new Vector3(0f, StationYaws[index], 0f)) * Vector3.forward * StationRadius
                + Vector3.up * LensHeight;
        }

        public static Quaternion StationRotation(int index)
        {
            return Rotation(new Vector3(0f, StationYaws[index] + 180f, 0f));
        }

        /// <summary>
        /// The room: the floor plate round the shaft with the sheaves' notches, the lining and its corner ribs, the
        /// ceiling with a rib on each corner meeting a round boss, and the insides of the portholes: frames and night
        /// glass, never lit from outside.
        /// </summary>
        public static LowPolyMeshBuilder Shell()
        {
            var b = new LowPolyMeshBuilder(2400);
            float liningRadius = LiningApothem / Mathf.Cos(Mathf.PI / Sides);
            LanderMeshes.OctagonRing(b, liningRadius, LanderMeshes.ShaftRadius, -FloorThickness, 0f,
                PaletteSwatch.WornPanel, PaletteSwatch.Charcoal);
            b.Torus(At(0f, 0.004f, 0f), LanderMeshes.ShaftRadius * Mathf.Cos(Mathf.PI / Sides) + 0.06f, 0.03f, 16, 3,
                PaletteSwatch.FadedAccent);
            float half = LanderMeshes.HalfSide(LiningApothem);
            var ceiling = new Vector3[Sides];
            for (int side = 0; side < Sides; side++)
            {
                float yaw = side * 45f;
                Matrix4x4 wall = Wall(yaw, 0f);
                b.Box(wall * At(0f, WainscotHeight * 0.5f, -0.01f), new Vector3(half * 2f, WainscotHeight, 0.02f),
                    PaletteSwatch.Wood);
                b.Box(wall * At(0f, (WainscotHeight + Height) * 0.5f, -0.01f),
                    new Vector3(half * 2f, Height - WainscotHeight, 0.02f), PaletteSwatch.WornPanel);
                b.Box(wall * At(0f, WainscotHeight, 0.01f), new Vector3(half * 2f, 0.05f, 0.04f),
                    PaletteSwatch.Charcoal);
                Vector3 corner = LanderMeshes.Corner(side, liningRadius - 0.03f, Height * 0.5f);
                b.Box(At(corner, new Vector3(0f, yaw + 22.5f, 0f)), new Vector3(RibWidth, Height, RibWidth),
                    PaletteSwatch.Metal, 0.01f);
                ceiling[side] = LanderMeshes.Corner(side, liningRadius, Height);
                SiteKit.Bar(b, LanderMeshes.Corner(side, liningRadius - 0.05f, Height - 0.04f),
                    new Vector3(0f, Height - 0.04f, 0f), 0.06f, PaletteSwatch.Metal);
            }

            b.Polygon(ceiling, Vector3.down, PaletteSwatch.WornPanel);
            b.Prism(At(0f, Height - 0.05f, 0f), 0.24f, 0.08f, 8, PaletteSwatch.Charcoal);
            for (int i = 0; i < LanderMeshes.PaneCount; i++)
            {
                Porthole(b, i * 90f, WindowHeight, 0.3f);
            }

            Porthole(b, LanderMeshes.BigPortholeYaw, PortholeHeight, LanderMeshes.BigPortholeRadius);
            for (int i = 0; i < CabinLampCount; i++)
            {
                LampShade(b, CabinLamp(i));
            }

            TapeRun(b);
            return b;
        }

        /// <summary>
        /// The camera's ring rail in rail space (the origin on the axis at the rail's height): a brass-dull tube with
        /// hangers up to the ceiling on every corner.
        /// </summary>
        public static LowPolyMeshBuilder RingRail()
        {
            var b = new LowPolyMeshBuilder(400);
            b.Torus(Matrix4x4.identity, RingRadius, RailTube, 32, 4, PaletteSwatch.Metal);
            for (int i = 0; i < Sides; i++)
            {
                Vector3 at = Rotation(new Vector3(0f, 22.5f + i * 45f, 0f)) * Vector3.forward * RingRadius;
                RecipeKit.Rod(b, at, at + Vector3.up * (Height - RingHeight), 0.015f, 5, PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>The four wall lamps' bulbs (lab space): the lab's practical warm light.</summary>
        public static LowPolyMeshBuilder CabinLamps()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int i = 0; i < CabinLampCount; i++)
            {
                b.Icosphere(At(CabinLamp(i)), 0.06f, 1, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>
        /// Wall lamp <paramref name="index"/>'s bulb (lab space): over the portholes on the faces between the stations
        /// (front, right, back, left), below the rail so the camera never meets one.
        /// </summary>
        public static Vector3 CabinLamp(int index)
        {
            return Rotation(new Vector3(0f, index * 90f, 0f)) * Vector3.forward * (LiningApothem - 0.16f)
                + Vector3.up * 2.02f;
        }

        /// <summary>A shade on a short bracket from the wall over a lamp's bulb.</summary>
        private static void LampShade(LowPolyMeshBuilder b, Vector3 bulb)
        {
            Vector3 flat = new Vector3(bulb.x, 0f, bulb.z).normalized;
            b.Frustum(At(bulb + Vector3.up * 0.08f), 0.13f, 0.05f, 0.1f, 8, PaletteSwatch.Honey);
            RecipeKit.Rod(b, bulb + Vector3.up * 0.13f, flat * LiningApothem + Vector3.up * (bulb.y + 0.13f), 0.015f,
                5, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// A porthole from inside: a deep dark frame and the night beyond (cool dark glass, so the earthlight through
        /// it reads cool against the lamps' warm).
        /// </summary>
        private static void Porthole(LowPolyMeshBuilder b, float yaw, float height, float radius)
        {
            Matrix4x4 at = Wall(yaw, WindowInset) * At(0f, height, 0f);
            b.Torus(at * Matrix4x4.Rotate(Rotation(AlongZ)), radius + 0.04f, 0.06f, 16, 4, PaletteSwatch.Charcoal);
            b.Prism(Wall(yaw, 0.005f) * At(0f, height, 0f) * Matrix4x4.Rotate(Rotation(AlongZ)), radius, 0.01f, 16,
                PaletteSwatch.SkyHorizon);
        }

        /// <summary>
        /// The reader's paper tape carried on along the front wall to the board: a strip held by little clips, so
        /// what the reader decodes visibly runs on to Kenji's plans.
        /// </summary>
        private static void TapeRun(LowPolyMeshBuilder b)
        {
            float half = LanderMeshes.HalfSide(LiningApothem);
            Matrix4x4 front = Wall(0f, 0.03f);
            b.Box(front * At(0f, LabStationMeshes.TapeHeight, 0f), new Vector3(half * 2f + 0.1f, 0.05f, 0.004f),
                PaletteSwatch.Cream);
            for (int i = -2; i <= 2; i++)
            {
                b.Box(front * At(i * half * 0.45f, LabStationMeshes.TapeHeight, 0.006f), new Vector3(0.03f, 0.07f, 0.01f),
                    PaletteSwatch.Metal);
            }
        }

        /// <summary>
        /// A frame on the lining of the face at <paramref name="yaw"/>: +X along it, +Y up from the floor, +Z out of
        /// the wall into the room, <paramref name="depth"/> in front of the lining.
        /// </summary>
        public static Matrix4x4 Wall(float yaw, float depth)
        {
            Vector3 normal = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
            return SiteKit.Face(normal * (LiningApothem - depth), -normal, Vector3.up);
        }
    }
}
