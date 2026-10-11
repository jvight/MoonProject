using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The crew's habitat lander that 07 calls home (docs/features/M3-19): a gold-foil descent stage on four splayed
    /// legs, high enough for 07 to drive in under its belly, carrying an octagonal cabin of riveted, sun-bleached
    /// enamel panels with 07's worn orange stripe, under a tapered roof with the dish and the crew's roof hatch. The
    /// belly lift (<see cref="BellyLiftMeshes"/>) rises through a shaft in the stage into the cabin, the crew's lab
    /// (<see cref="LabMeshes"/>). Round portholes, the big one facing the Peak with its ring of lamp sockets, porch
    /// lamps and the crew's ladder up the front-left leg and the cabin wall to the roof. Decades alone show in its
    /// shape (a dented stage panel, one torn away and hanging, the dish knocked askew, the whip's snapped feed, the
    /// HOME sign leaning) and in the removable weather layers (see <see cref="Weathering"/>). Built in base space: the
    /// origin on the ground under the cabin's centre, +Z = the front, where 07 drives in under the belly.
    /// </summary>
    internal static class LanderMeshes
    {
        private const int Sides = 8;

        // Rust causes per metre along the stage's and portholes' seams (few, at random gaps; M3-15 §3).
        private const float SeamRunsPerMetre = 1.5f;

        /// <summary>The belly's clearance: ground to the stage's bottom, 07 plus headroom.</summary>
        public const float StageBottom = 2.6f;

        /// <summary>The stage's top and the cabin floor's surface (the lift's top stop).</summary>
        public const float FloorY = StageBottom + 1f;

        public const float StageRadius = 4.05f;

        /// <summary>
        /// The cabin's inside, across its flats and floor to ceiling: the turntable plus a ring of stations.
        /// </summary>
        public const float CabinInnerApothem = 2.8f;

        public const float CabinInnerHeight = 2.5f;

        public const float CeilingY = FloorY + CabinInnerHeight;

        public const float WallThickness = 0.16f;

        /// <summary>The top of the cabin's walls, where the tapered roof starts.</summary>
        public const float CabinTop = CeilingY + 0.22f;

        public const float RoofRise = 0.7f;

        /// <summary>
        /// The lander's height to its roof (the tower must stand 1.25 times taller, M3-19 refinement 4).
        /// </summary>
        public const float RoofTop = CabinTop + RoofRise;

        /// <summary>
        /// The shaft the platform rises through, across its corners (an octagon round the 1.7 m platform).
        /// </summary>
        public const float ShaftRadius = 1.95f;

        public const float FootRadius = 6f;

        /// <summary>
        /// Yaw of the leg the crew's ladder climbs (the front-left), and of the wall it carries on up.
        /// </summary>
        public const float LadderYaw = -45f;

        // The ladder leans on the stage beside the leg, not through its strut and braces.
        private const float LadderBeside = 0.6f;

        /// <summary>
        /// Yaw of the cabin face with the big porthole, over the survey chart: toward the Peak from home.
        /// </summary>
        public const float BigPortholeYaw = -135f;

        public const float BigPortholeRadius = 0.6f;

        public const float BigPortholeHeight = FloorY + 1.6f;

        /// <summary>The lamp sockets round the big porthole: one per known tech node, up to 0.8's count.</summary>
        public const int PortholeLampCount = 32;

        public const float PortholeLampRing = 0.9f;

        private const float PortholeLampRadius = 0.05f;

        public const int LampCount = 4;

        /// <summary>The window panes that light one by one as home wakes (M3-16): front, right, back.</summary>
        public const int PaneCount = 3;

        // The crew's eye height inside the cabin (VISION ruling 13: a person is 1.75 m).
        private const float WindowHeight = FloorY + 1.5f;
        private const float PaneRadius = 0.3f;

        // The cabin's worn orange band, near the top of its walls, and the ledge under it where dust settles.
        internal const float BandY = CabinTop - 0.32f;

        /// <summary>
        /// Top of the cushion on Tilly's perch (the lander's FriendSocket_tilly): a little landing pad on a post on
        /// the stage's front-right ledge, beside the cabin wall.
        /// </summary>
        public static readonly Vector3 TillyPerch = new Vector3(1.62f, FloorY + 0.52f, 2.81f);

        private const float HipRadius = 3.3f;
        private const float HipHeight = StageBottom + 0.35f;

        // Each cabin face breaks into this many panel columns, below and above its middle seam.
        private const int PanelColumns = 2;
        private const float PanelGap = 0.03f;
        private const float SeamHeight = FloorY + 1.25f;
        private const float RivetSize = 0.035f;
        private const int RivetsPerSeam = 6;

        // Which stage panels (by quarter turn from the front) took the years worst.
        private const int DentedPanel = 1;
        private const int TornPanel = 2;

        private const int DishDentSeed = 84;

        public static readonly float StageApothem = StageRadius * Mathf.Cos(Mathf.PI / Sides);

        public static readonly float CabinApothem = CabinInnerApothem + WallThickness;

        public static readonly float CabinRadius = CabinApothem / Mathf.Cos(Mathf.PI / Sides);

        private static readonly Vector2[] FootpadProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.62f, 0.02f), new Vector2(0.64f, 0.12f), new Vector2(0.4f, 0.22f),
            new Vector2(0f, 0.24f),
        };

        private static readonly Vector2[] DishProfile =
        {
            new Vector2(0f, -0.14f), new Vector2(0.32f, -0.1f), new Vector2(0.62f, 0.07f), new Vector2(0.56f, 0.09f),
            new Vector2(0.3f, -0.04f), new Vector2(0f, -0.07f),
        };

        public static LowPolyMeshBuilder Hull()
        {
            var b = new LowPolyMeshBuilder(7000);
            DescentStage(b);
            Legs(b);
            Cabin(b);
            Roof(b);
            Ladder(b);
            ChargingDockMeshes.Dock(b);
            for (int i = 0; i < LampCount; i++)
            {
                LampFixture(b, LampPosition(i));
            }

            MissionPatch(b);
            Perch(b);
            return b;
        }

        /// <summary>
        /// Window pane <paramref name="index"/> in lander space (0 front, 1 right, 2 back), dark glass at the crew's
        /// eye height that glows warm when lit.
        /// </summary>
        public static LowPolyMeshBuilder Pane(int index)
        {
            var b = new LowPolyMeshBuilder(60);
            float yaw = index * 90f;
            b.Prism(OnFace(yaw, CabinApothem + 0.03f, 0f, WindowHeight) * Matrix4x4.Rotate(Rotation(AlongZ)),
                PaneRadius, 0.03f, 12, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// The big porthole's glass over the survey chart: dark until the lab's lamps light it from inside.
        /// </summary>
        public static LowPolyMeshBuilder BigPorthole()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(OnFace(BigPortholeYaw, CabinApothem + 0.03f, 0f, BigPortholeHeight)
                * Matrix4x4.Rotate(Rotation(AlongZ)), BigPortholeRadius, 0.03f, 16, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// Where porthole lamp <paramref name="index"/> sits, in lander space: round the big porthole clockwise from
        /// its top as seen from outside, in its own little socket on the ring.
        /// </summary>
        public static Vector3 PortholeLamp(int index)
        {
            float angle = index * Mathf.PI * 2f / PortholeLampCount;
            Matrix4x4 face = OnFace(BigPortholeYaw, CabinApothem + 0.05f, 0f, BigPortholeHeight);
            return face.MultiplyPoint3x4(new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f) * PortholeLampRing);
        }

        /// <summary>
        /// One porthole lamp's glass, centred on its socket and facing out of the big porthole's face.
        /// </summary>
        public static LowPolyMeshBuilder PortholeLampGlass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.Rotate(Rotation(new Vector3(0f, BigPortholeYaw, 0f)) * Rotation(AlongZ)),
                PortholeLampRadius, 0.035f, 6, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>A porch lamp's bulb, centred on its LampSocket.</summary>
        public static LowPolyMeshBuilder Bulb()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, 0.08f, 1, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// Lamp socket <paramref name="index"/> (0..<see cref="LampCount"/> - 1): hanging under the belly's front lip
        /// over the way in, on the lamp post at the ladder's foot, and on the cabin walls facing the bay and the
        /// tower. Each gets a fixture so the light has a source.
        /// </summary>
        public static Vector3 LampPosition(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(0f, StageBottom - 0.22f, StageApothem - 0.35f);
                case 1:
                    return new Vector3(-3.55f, 1.1f, 4.95f);
                case 2:
                    return new Vector3(CabinApothem + 0.26f, CabinTop - 0.75f, 0f);
                case 3:
                    return new Vector3(-CabinApothem - 0.26f, CabinTop - 0.75f, 0f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Lander lamps are 0..3.");
            }
        }

        /// <summary>
        /// The lander's rust: blooms at the cabin's rivets and streaks down from the portholes and the stripe, runs
        /// down the gold stage from its top seam, collars at the legs' knees and feet, the dock's own and the sign's.
        /// </summary>
        public static LowPolyMeshBuilder Rust()
        {
            var b = new LowPolyMeshBuilder(1400);
            float face = CabinApothem + Weathering.RustLift;
            for (int side = 0; side < Sides; side++)
            {
                float yaw = side * 45f;
                Matrix4x4 wall = Face(yaw, face);
                float half = HalfSide(CabinApothem) - 0.1f;
                SiteKit.RustSeam(b, wall, -half, half, SeamHeight, 0.55f, SeamRunsPerMetre, 300 + side);
                SiteKit.RustStreak(b, wall, (side % 3 - 1) * 0.42f, BandY - 0.11f, 0.35f + side % 2 * 0.25f);
            }

            for (int i = 0; i < PaneCount; i++)
            {
                SiteKit.RustStreak(b, Face(i * 90f, face + 0.02f), 0.16f, WindowHeight - PaneRadius - 0.06f, 0.45f);
            }

            SiteKit.RustStreak(b, Face(BigPortholeYaw, face + 0.02f), -0.3f,
                BigPortholeHeight - PortholeLampRing - 0.08f, 0.6f);
            float stage = StageApothem + Weathering.RustLift;
            for (int i = 0; i < Sides; i++)
            {
                float half = HalfSide(StageApothem) - 0.15f;
                SiteKit.RustSeam(b, Face(i * 45f, stage), -half, half, FloorY - 0.12f, 0.8f, SeamRunsPerMetre,
                    310 + i);
            }

            SiteKit.RustPatch(b, Face(DentedPanel * 90f, StageApothem + 0.04f), 0.2f, StageBottom + 0.5f, 0.24f);
            for (int i = 0; i < 4; i++)
            {
                LegPoints(i, out Vector3 hip, out Vector3 foot, out Vector3 knee);
                Vector3 along = (foot - hip).normalized;
                Weathering.Collar(b, Along(knee - along * 0.14f, knee + along * 0.14f)
                    * Matrix4x4.Rotate(Rotation(AlongZ)), 0.14f, 0.2f);
                Weathering.Collar(b, At(foot + Vector3.up * 0.12f), 0.16f, 0.14f);
            }

            ChargingDockMeshes.Rust(b);
            return b;
        }

        /// <summary>
        /// Dust drifted against the lander over the decades: round each footpad and a long low mound blown in under
        /// the belly's back, clear of the platform's ring, plus what fell and was never picked up.
        /// </summary>
        public static LowPolyMeshBuilder Drifts()
        {
            var b = new LowPolyMeshBuilder(1400);
            for (int i = 0; i < 4; i++)
            {
                float yaw = 45f + i * 90f;
                Vector3 foot = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward * (FootRadius + 0.2f);
                SiteKit.Drift(b, foot, 1.9f, 1.1f, 0.22f, yaw + 15f, 81 + i);
            }

            SiteKit.Drift(b, new Vector3(0.6f, 0f, -3.1f), 3.6f, 1.6f, 0.24f, 80f, 85);
            SiteKit.Drift(b, new Vector3(-3.4f, 0f, -1.2f), 1.6f, 0.8f, 0.2f, 20f, 86);
            Debris(b);
            return b;
        }

        /// <summary>
        /// What fell and was never picked up, half sunk in the dust behind the back legs: the torn foil sheet's twin
        /// that blew off, a snapped brace and a coil of cable. It sweeps away with the dust.
        /// </summary>
        private static void Debris(LowPolyMeshBuilder b)
        {
            b.Box(At(new Vector3(-4.6f, 0.02f, -2.2f), new Vector3(4f, 25f, -3f)), new Vector3(0.9f, 0.025f, 0.55f),
                PaletteSwatch.Honey);
            RecipeKit.Strut(b, new Vector3(3.2f, 0.05f, -4.9f), new Vector3(4.1f, 0.04f, -4.5f),
                new Vector2(0.08f, 0.08f), PaletteSwatch.Metal);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(At(new Vector3(-2.4f, 0.03f + i * 0.022f, -4.6f), new Vector3(0f, i * 23f, i * 4f)),
                    0.26f - i * 0.03f, 0.017f, 10, 3, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>
        /// The gold-foil descent stage: an octagonal ring round the lift's shaft, banded, with dark access panels (one
        /// dented, one torn away and hanging) and the winch housings at the shaft's mouth underneath.
        /// </summary>
        private static void DescentStage(LowPolyMeshBuilder b)
        {
            OctagonRing(b, StageRadius, ShaftRadius, StageBottom, FloorY - LabMeshes.FloorThickness,
                PaletteSwatch.Honey, PaletteSwatch.Charcoal);
            Band(b, StageRadius + 0.03f, StageBottom + 0.12f, 0.09f, PaletteSwatch.Charcoal);
            Band(b, StageRadius + 0.03f, FloorY - 0.08f, 0.1f, PaletteSwatch.Metal);
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 90f;
                if (i == TornPanel)
                {
                    HangingPanel(b, yaw);
                    continue;
                }

                // The panel by the ladder took a knock: pushed in and twisted.
                Matrix4x4 face = Face(yaw, StageApothem + 0.02f);
                Matrix4x4 seat = i == DentedPanel
                    ? face * At(new Vector3(0.1f, StageBottom + 0.5f, -0.02f), new Vector3(-6f, 5f, 4f))
                    : face * At(0f, StageBottom + 0.5f, 0f);
                b.Box(seat, new Vector3(0.9f, 0.42f, 0.05f), PaletteSwatch.Metal, 0.015f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (ShaftRadius + 0.32f), StageBottom - 0.16f, 0f), new Vector3(0.5f, 0.32f, 0.7f),
                    PaletteSwatch.Metal, 0.03f);
                b.Prism(At(new Vector3(side * (ShaftRadius + 0.32f), StageBottom - 0.16f, 0f), AlongZ), 0.13f, 0.76f,
                    10, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>
        /// The foil sheet that worked loose on the back, facing away from home's lamps: a dark hole where it was and
        /// the sheet hanging from its last bolt at one top corner, swung down past the stage's bottom edge.
        /// </summary>
        private static void HangingPanel(LowPolyMeshBuilder b, float yaw)
        {
            Matrix4x4 face = Face(yaw, StageApothem + 0.01f);
            const float width = 1.1f;
            const float height = 0.66f;
            var hole = new Vector3(0.2f, StageBottom + 0.16f + height * 0.5f, 0f);
            b.Box(face * At(hole), new Vector3(width, height, 0.03f), PaletteSwatch.Charcoal);
            var bolt = new Vector3(hole.x - width * 0.5f + 0.04f, hole.y + height * 0.5f - 0.04f, 0.03f);
            b.Icosphere(face * At(bolt), 0.03f, 0, PaletteSwatch.Metal);
            Matrix4x4 sheet = face * At(bolt, new Vector3(-10f, 6f, 58f));
            b.Box(sheet * At(width * 0.5f - 0.04f, -height * 0.5f + 0.04f, 0.02f), new Vector3(width, height, 0.025f),
                PaletteSwatch.Honey);
            b.Box(sheet * At(width * 0.5f - 0.04f, -height * 0.5f + 0.04f, -0.002f),
                new Vector3(width - 0.06f, height - 0.06f, 0.012f), PaletteSwatch.Metal);
        }

        private static void Legs(LowPolyMeshBuilder b)
        {
            for (int i = 0; i < 4; i++)
            {
                LegPoints(i, out Vector3 hip, out Vector3 foot, out Vector3 knee);
                Vector3 direction = new Vector3(foot.x, 0f, foot.z).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                RecipeKit.Strut(b, hip, foot, new Vector2(0.2f, 0.2f), PaletteSwatch.Metal);
                RecipeKit.Strut(b, direction * (StageApothem - 0.3f) + side * 1.1f + Vector3.up * StageBottom, knee,
                    new Vector2(0.11f, 0.11f), PaletteSwatch.Metal);
                RecipeKit.Strut(b, direction * (StageApothem - 0.3f) - side * 1.1f + Vector3.up * StageBottom, knee,
                    new Vector2(0.11f, 0.11f), PaletteSwatch.Metal);
                b.Box(At(hip, new Vector3(0f, 45f + i * 90f, 0f)), new Vector3(0.34f, 0.3f, 0.34f),
                    PaletteSwatch.Charcoal, 0.03f);
                b.Lathe(At(new Vector3(foot.x, 0f, foot.z)), FootpadProfile, 10, PaletteSwatch.Metal);
                b.Prism(At(foot + Vector3.up * 0.12f), 0.13f, 0.24f, 8, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>
        /// The cabin: an octagon of riveted enamel panels (two columns either side of a middle seam on every face)
        /// with metal posts on its corners, the orange band and the dusty ledge under it, porthole frames and a
        /// reaction-control quad on each flank.
        /// </summary>
        private static void Cabin(LowPolyMeshBuilder b)
        {
            float half = HalfSide(CabinApothem);
            float column = (half * 2f - PanelGap * (PanelColumns + 1)) / PanelColumns;
            for (int side = 0; side < Sides; side++)
            {
                float yaw = side * 45f;
                Matrix4x4 face = Face(yaw, CabinApothem - WallThickness * 0.5f);
                for (int c = 0; c < PanelColumns; c++)
                {
                    float x = -half + PanelGap + column * (c + 0.5f) + PanelGap * c;
                    PanelSlab(b, face, x, column, FloorY, SeamHeight, side * 4 + c);
                    PanelSlab(b, face, x, column, SeamHeight, CabinTop, side * 4 + c + 2);
                }

                b.Box(face * At(0f, (FloorY + CabinTop) * 0.5f, -0.01f),
                    new Vector3(half * 2f, CabinTop - FloorY, WallThickness - 0.04f), PaletteSwatch.Charcoal);
                Rivets(b, face, half, SeamHeight, WallThickness * 0.5f);
                Rivets(b, face, half, CabinTop - 0.08f, WallThickness * 0.5f);
                Vector3 corner = Corner(side, CabinRadius + 0.01f, 0f);
                b.Box(At(new Vector3(corner.x, (FloorY + CabinTop) * 0.5f, corner.z), new Vector3(0f, yaw + 22.5f, 0f)),
                    new Vector3(0.12f, CabinTop - FloorY, 0.12f), PaletteSwatch.Metal, 0.02f);
            }

            Band(b, CabinRadius + 0.02f, BandY, 0.18f, PaletteSwatch.WarmAccent);
            Band(b, CabinRadius + 0.05f, BandY - 0.12f, 0.04f, PaletteSwatch.Metal);
            Band(b, CabinRadius + 0.04f, FloorY + 0.06f, 0.12f, PaletteSwatch.Charcoal);
            for (int i = 0; i < PaneCount; i++)
            {
                b.Torus(OnFace(i * 90f, CabinApothem + 0.03f, 0f, WindowHeight) * Matrix4x4.Rotate(Rotation(AlongZ)),
                    PaneRadius + 0.03f, 0.05f, 12, 4, PaletteSwatch.Charcoal);
            }

            Matrix4x4 big = OnFace(BigPortholeYaw, CabinApothem + 0.03f, 0f, BigPortholeHeight);
            b.Torus(big * Matrix4x4.Rotate(Rotation(AlongZ)), BigPortholeRadius + 0.05f, 0.08f, 16, 4,
                PaletteSwatch.Charcoal);
            b.Torus(big * Matrix4x4.Rotate(Rotation(AlongZ)), PortholeLampRing, 0.04f, 32, 3, PaletteSwatch.Metal);
            for (int i = 0; i < PortholeLampCount; i++)
            {
                b.Prism(At(PortholeLamp(i) - Rotation(new Vector3(0f, BigPortholeYaw, 0f)) * Vector3.forward * 0.02f,
                    Rotation(new Vector3(0f, BigPortholeYaw, 0f)) * Rotation(AlongZ)), PortholeLampRadius + 0.02f,
                    0.03f, 6, PaletteSwatch.Charcoal);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 thruster = Rotation(new Vector3(0f, side * 90f, 0f)) * Vector3.forward * (CabinApothem + 0.12f);
                RcsQuad(b, thruster + Vector3.up * (BandY - 0.45f) + Vector3.back * 0.75f, side * 90f);
            }

            OctagonRing(b, StageRadius - 0.02f, CabinRadius, FloorY - 0.01f, FloorY + 0.02f, PaletteSwatch.Metal,
                PaletteSwatch.Metal);
        }

        /// <summary>
        /// One cabin panel between <paramref name="bottom"/> and <paramref name="top"/>: sun-bleached enamel, a few of
        /// them sitting a hair proud or twisted where the years worked their rivets loose.
        /// </summary>
        private static void PanelSlab(LowPolyMeshBuilder b, Matrix4x4 face, float x, float width, float bottom,
            float top, int index)
        {
            float proud = index % 5 == 0 ? 0.012f : 0f;
            float twist = index % 7 == 3 ? 1.2f : 0f;
            b.Box(face * At(new Vector3(x, (bottom + top) * 0.5f, WallThickness * 0.5f - 0.01f + proud),
                new Vector3(0f, 0f, twist)), new Vector3(width, top - bottom - PanelGap, 0.03f),
                PaletteSwatch.Enamel);
        }

        /// <summary>A row of rivet heads across a face at <paramref name="y"/>.</summary>
        private static void Rivets(LowPolyMeshBuilder b, Matrix4x4 face, float half, float y, float depth)
        {
            for (int i = 0; i < RivetsPerSeam; i++)
            {
                float x = Mathf.Lerp(-half + 0.12f, half - 0.12f, (float)i / (RivetsPerSeam - 1));
                b.Box(face * At(x, y, depth + 0.012f), Vector3.one * RivetSize, PaletteSwatch.Metal);
            }
        }

        /// <summary>Four small reaction-control nozzles on a block, facing out of the cabin along a yaw.</summary>
        private static void RcsQuad(LowPolyMeshBuilder b, Vector3 centre, float yaw)
        {
            Matrix4x4 frame = At(centre, new Vector3(0f, yaw, 0f));
            b.Box(frame, new Vector3(0.3f, 0.3f, 0.24f), PaletteSwatch.Metal, 0.02f);
            b.Cone(frame * At(new Vector3(0f, 0.24f, 0f)), 0.07f, 0.18f, 6, PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(0f, -0.24f, 0f), new Vector3(180f, 0f, 0f)), 0.07f, 0.18f, 6,
                PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(0.24f, 0f, 0f), new Vector3(0f, 0f, -90f)), 0.07f, 0.18f, 6,
                PaletteSwatch.Charcoal);
            b.Cone(frame * At(new Vector3(-0.24f, 0f, 0f), new Vector3(0f, 0f, 90f)), 0.07f, 0.18f, 6,
                PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// The tapered roof: panelled like the walls, a flat top with the crew's round roof hatch, the dish knocked
        /// askew on its mast and the whip aerial whose feed snapped and hangs down the back.
        /// </summary>
        private static void Roof(LowPolyMeshBuilder b)
        {
            float topRadius = CabinRadius * 0.48f;
            b.Frustum(At(0f, CabinTop + RoofRise * 0.5f, 0f), CabinRadius + 0.04f, topRadius, RoofRise, Sides,
                PaletteSwatch.WornPanel);
            Band(b, CabinRadius + 0.06f, CabinTop + 0.03f, 0.06f, PaletteSwatch.Metal);
            for (int side = 0; side < Sides; side++)
            {
                Vector3 low = Corner(side, CabinRadius + 0.05f, CabinTop + 0.02f);
                Vector3 high = Corner(side, topRadius + 0.02f, RoofTop - 0.01f);
                SiteKit.Bar(b, low, high, 0.06f, PaletteSwatch.Metal);
            }

            var hatch = new Vector3(0.55f, RoofTop, 0.35f);
            b.Prism(At(hatch + Vector3.up * 0.06f), 0.42f, 0.12f, 12, PaletteSwatch.Metal);
            b.Prism(At(hatch + Vector3.up * 0.14f), 0.36f, 0.05f, 12, PaletteSwatch.Enamel);
            b.Box(At(hatch + new Vector3(0f, 0.19f, 0f)), new Vector3(0.3f, 0.04f, 0.05f), PaletteSwatch.Charcoal);

            var mastBase = new Vector3(-0.6f, RoofTop, -0.5f);
            Vector3 mastTop = mastBase + new Vector3(0.06f, 1.1f, 0.03f);
            RecipeKit.Rod(b, mastBase, mastTop, 0.05f, 6, PaletteSwatch.Metal);
            b.Lathe(At(mastTop + new Vector3(0f, 0.06f, 0.06f), new Vector3(78f, 48f, 15f)), DishProfile, 12,
                PaletteSwatch.Enamel, new Displacement(DishDentSeed, 0.045f, 4f));

            var whipBase = new Vector3(0.9f, RoofTop - 0.1f, -0.85f);
            RecipeKit.Rod(b, whipBase, whipBase + new Vector3(0.1f, 1.9f, -0.06f), 0.018f, 5, PaletteSwatch.Metal);
            SiteKit.Cable(b, new[]
            {
                whipBase + new Vector3(0.02f, 0.05f, 0f), whipBase + new Vector3(0.3f, -0.35f, -0.55f),
                new Vector3(1.2f, CabinTop - 0.6f, -CabinApothem - 0.04f),
                new Vector3(1.28f, CabinTop - 1.5f, -CabinApothem - 0.06f),
            }, 0.02f, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// The crew's ladder: up beside the front-left leg from its footpad to the stage's ledge, then up the cabin
        /// wall and over the roof's edge to the hatch. A remnant: 07 rides the lift.
        /// </summary>
        private static void Ladder(LowPolyMeshBuilder b)
        {
            Vector3 out1 = Rotation(new Vector3(0f, LadderYaw, 0f)) * Vector3.forward;
            Vector3 across = Vector3.Cross(Vector3.up, out1).normalized;
            Vector3 beside = across * LadderBeside;
            Vector3 low = out1 * (FootRadius - 0.7f) + beside + Vector3.up * 0.05f;
            Vector3 high = out1 * (StageApothem + 0.12f) + beside + Vector3.up * FloorY;
            Rungs(b, low, high, across, 0.24f);
            Vector3 wallFoot = out1 * (CabinApothem + 0.14f) + beside + Vector3.up * FloorY;
            Rungs(b, wallFoot, wallFoot + Vector3.up * (CabinTop - FloorY + 0.25f), across, 0.22f);
        }

        private static void Rungs(LowPolyMeshBuilder b, Vector3 from, Vector3 to, Vector3 across, float halfWidth)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                RecipeKit.Rod(b, from + across * (s * halfWidth), to + across * (s * halfWidth), 0.03f, 6,
                    PaletteSwatch.Metal);
            }

            int rungs = Mathf.RoundToInt(Vector3.Distance(from, to) / 0.3f);
            for (int r = 1; r < rungs; r++)
            {
                Vector3 at = Vector3.Lerp(from, to, (float)r / rungs);
                RecipeKit.Rod(b, at - across * halfWidth, at + across * halfWidth, 0.02f, 5, PaletteSwatch.Metal);
            }
        }

        /// <summary>
        /// A shade over a lamp position: on a post from the ground when the lamp is low, hanging from the stage's
        /// underside under the belly, otherwise on a bracket from the cabin wall.
        /// </summary>
        private static void LampFixture(LowPolyMeshBuilder b, Vector3 lamp)
        {
            b.Frustum(At(lamp + Vector3.up * 0.1f), 0.14f, 0.05f, 0.09f, 8, PaletteSwatch.Charcoal);
            if (lamp.y < FloorY * 0.5f)
            {
                RecipeKit.Rod(b, new Vector3(lamp.x, 0f, lamp.z), lamp + Vector3.up * 0.06f, 0.035f, 6,
                    PaletteSwatch.Metal);
                b.Prism(At(lamp.x, 0.03f, lamp.z), 0.13f, 0.06f, 8, PaletteSwatch.Charcoal);
                return;
            }

            if (lamp.y < StageBottom)
            {
                RecipeKit.Rod(b, lamp + Vector3.up * 0.14f, new Vector3(lamp.x, StageBottom, lamp.z), 0.02f, 5,
                    PaletteSwatch.Charcoal);
                return;
            }

            var flat = new Vector3(lamp.x, 0f, lamp.z);
            Vector3 wall = flat.normalized * (CabinApothem - 0.02f) + Vector3.up * (lamp.y + 0.15f);
            RecipeKit.Rod(b, wall, lamp + Vector3.up * 0.15f, 0.025f, 5, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// Tilly's perch on the stage's ledge: a padded landing disc on a post, its rim in 07's orange.
        /// </summary>
        private static void Perch(LowPolyMeshBuilder b)
        {
            var foot = new Vector3(TillyPerch.x, FloorY + 0.02f, TillyPerch.z);
            RecipeKit.Rod(b, foot, foot + Vector3.up * 0.44f, 0.035f, 8, PaletteSwatch.Metal);
            b.Prism(At(foot + Vector3.up * 0.02f), 0.08f, 0.04f, 8, PaletteSwatch.Charcoal);
            Vector3 pad = TillyPerch - Vector3.up * 0.06f;
            b.Prism(At(pad - Vector3.up * 0.02f), 0.3f, 0.04f, 12, PaletteSwatch.Metal);
            b.Torus(At(pad), 0.3f, 0.02f, 12, 4, PaletteSwatch.WarmAccent);
            Vector2[] cushion =
            {
                new Vector2(0f, 0f), new Vector2(0.21f, 0.004f), new Vector2(0.235f, 0.03f), new Vector2(0.2f, 0.056f),
                new Vector2(0f, 0.06f),
            };
            b.Lathe(At(pad), cushion, 12, PaletteSwatch.Honey);
        }

        /// <summary>
        /// Where the HOME sign's post stands in the dust beside 07's dock (the HomeSign node's pivot).
        /// </summary>
        public static readonly Vector3 HomeSignFoot = new Vector3(0.95f, 0f, 8.1f);

        /// <summary>The HOME sign leaning with the years: back and to the side (the node's built rotation).</summary>
        public static readonly Vector3 HomeSignLean = new Vector3(-9f, 17f, 13f);

        /// <summary>The HOME sign set straight again once home wakes (M3-16), still facing the way in.</summary>
        public static readonly Vector3 HomeSignUpright = new Vector3(0f, 17f, 0f);

        /// <summary>
        /// A little hand-painted "HOME" sign on a post, in its own space (the post's foot at the origin), rust weeping
        /// from the bolts that hold its board.
        /// </summary>
        public static LowPolyMeshBuilder HomeSign()
        {
            var b = new LowPolyMeshBuilder(400);
            b.Box(At(0f, 0.46f, 0f), new Vector3(0.06f, 0.9f, 0.05f), PaletteSwatch.Metal);
            b.Box(At(0f, 0.95f, 0.035f), new Vector3(0.92f, 0.36f, 0.04f), PaletteSwatch.Enamel, 0.012f);
            Glyphs.Write(b, At(0f, 0.95f, 0.055f), "HOME", 0.2f, PaletteSwatch.WarmAccent, PaletteSwatch.Enamel);
            Matrix4x4 board = At(0f, 0.95f, 0.035f + 0.02f + Weathering.RustLift);
            SiteKit.RustStreak(b, board, -0.36f, 0.12f, 0.16f);
            SiteKit.RustStreak(b, board, 0.36f, 0.12f, 0.22f);
            return b;
        }

        /// <summary>
        /// Leg <paramref name="index"/>'s hip on the stage, its foot on the ground and the knee its braces meet.
        /// </summary>
        public static void LegPoints(int index, out Vector3 hip, out Vector3 foot, out Vector3 knee)
        {
            Vector3 direction = Rotation(new Vector3(0f, 45f + index * 90f, 0f)) * Vector3.forward;
            hip = direction * HipRadius + Vector3.up * HipHeight;
            foot = direction * FootRadius + Vector3.up * 0.24f;
            knee = Vector3.Lerp(hip, foot, 0.45f);
        }

        /// <summary>Half the width of one face of an octagon with this <paramref name="apothem"/>.</summary>
        public static float HalfSide(float apothem)
        {
            return apothem * Mathf.Tan(Mathf.PI / Sides);
        }

        /// <summary>Corner <paramref name="index"/> of an octagon (flat faces at multiples of 45°, +Z first).</summary>
        public static Vector3 Corner(int index, float radius, float y)
        {
            float angle = (index + 0.5f) * Mathf.PI * 2f / Sides;
            return new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);
        }

        /// <summary>
        /// An octagonal washer from <paramref name="bottom"/> to <paramref name="top"/>: its outer wall, its top and
        /// bottom faces and the inner wall round its hole (faces inward), so the lift's shaft is open all the way.
        /// </summary>
        public static void OctagonRing(LowPolyMeshBuilder b, float outer, float inner, float bottom, float top,
            PaletteSwatch skin, PaletteSwatch shaft)
        {
            for (int i = 0; i < Sides; i++)
            {
                int j = (i + Sides - 1) % Sides;
                Vector3 normal = Rotation(new Vector3(0f, i * 45f, 0f)) * Vector3.forward;
                b.Polygon(new[] { Corner(j, outer, bottom), Corner(i, outer, bottom), Corner(i, outer, top),
                    Corner(j, outer, top) }, normal, skin);
                b.Polygon(new[] { Corner(j, inner, top), Corner(i, inner, top), Corner(i, inner, bottom),
                    Corner(j, inner, bottom) }, -normal, shaft);
                b.Polygon(new[] { Corner(j, inner, top), Corner(j, outer, top), Corner(i, outer, top),
                    Corner(i, inner, top) }, Vector3.up, skin);
                b.Polygon(new[] { Corner(j, inner, bottom), Corner(i, inner, bottom), Corner(i, outer, bottom),
                    Corner(j, outer, bottom) }, Vector3.down, skin);
            }
        }

        /// <summary>A thin octagonal band round the hull at <paramref name="y"/>.</summary>
        private static void Band(LowPolyMeshBuilder b, float radius, float y, float height, PaletteSwatch swatch)
        {
            b.Prism(At(0f, y, 0f), radius, height, Sides, swatch, false);
        }

        /// <summary>
        /// A frame on the octagon face facing <paramref name="yaw"/>, <paramref name="distance"/> out.
        /// </summary>
        private static Matrix4x4 Face(float yaw, float distance)
        {
            Vector3 normal = Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward;
            return SiteKit.Face(normal * distance, normal, Vector3.up);
        }

        /// <summary>
        /// A point frame on that face, <paramref name="x"/> along it and at height <paramref name="y"/>.
        /// </summary>
        private static Matrix4x4 OnFace(float yaw, float distance, float x, float y)
        {
            return Face(yaw, distance) * At(x, y, 0f);
        }

        /// <summary>A faded round mission patch on the right wall: a sage field, a cream moon, a star.</summary>
        private static void MissionPatch(LowPolyMeshBuilder b)
        {
            Matrix4x4 patch = OnFace(90f, CabinApothem + 0.03f, 0.8f, FloorY + 1.1f);
            b.Prism(patch * Matrix4x4.Rotate(Rotation(AlongZ)), 0.34f, 0.025f, 14, PaletteSwatch.Sage);
            b.Torus(patch * At(new Vector3(0f, 0f, 0.012f), AlongZ), 0.34f, 0.025f, 14, 3, PaletteSwatch.Honey);
            b.Prism(patch * At(new Vector3(-0.09f, 0.08f, 0.02f), AlongZ), 0.12f, 0.02f, 10, PaletteSwatch.Cream);
            b.Extrude(patch * At(0.12f, -0.1f, 0.022f), Glyphs.Star(0.08f, 0.035f), 0.01f, PaletteSwatch.Honey);
        }
    }
}
