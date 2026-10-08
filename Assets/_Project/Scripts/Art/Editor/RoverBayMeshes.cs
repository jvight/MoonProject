using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's Rover Bay (docs/features/M3-14-built-for-07.md, VISION ruling 14: 07 has no hands; the art contract in
    /// docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): a drive-in service bay the crew built for a rover. A
    /// sheltered frame with an arched front, a low ramp up to a turning turntable sized for 07 with a pit under its
    /// central hole, three gantry arms on the crane rail (a yaw turret, two long links and a fitting tip each, folded
    /// along the rail under the roof at rest) and a floor arm that rises through the hole to fit kit under 07's belly,
    /// a material hopper inside the bay at rover height that 07's beam feeds, two work lamps, cable reels, a lit "07"
    /// sign and Kenji's old human bench beside it as a remnant. Built in its own space: origin on the ground at
    /// WorkshopAnchor, +Z = the open front 07 drives in from. Long left alone (ruling 12): faded paint, a roof panel
    /// gone, rust and dust in its weather skins.
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

        /// <summary>
        /// The turntable: the centre of its top (a plate standing a little proud of the floor, bevelled so 07 rolls
        /// on), its radius, the radius of the hole in its middle the floor arm rises through (wide enough for the
        /// Hover-Jump coils' plate to pass), and the floor's height.
        /// </summary>
        public static readonly Vector3 TurntableCentre = new Vector3(0f, PadTop + TurntableRaise, -0.35f);

        public const float TurntableRadius = 1.75f;
        public const float TurntableHole = 0.5f;
        public const float PadTop = 0.12f;

        /// <summary>
        /// The arms' shoulders under the crane rail, and their link lengths: long enough that a tip reaches every kit
        /// socket on a parked 07 with room to spare (rover's BayReach test proves it).
        /// </summary>
        public const float RailHeight = 3.08f;

        public const float UpperLength = 1.3f;
        public const float LowerLength = 1.2f;
        public const float TipLength = 0.26f;

        /// <summary>
        /// The arms' folded rest pose (Upper, Lower, Tip pitch about local X, deg): the upper link along the rail, the
        /// lower folded back under it, the tip hanging down; each arm's Yaw turns it along its stretch of rail.
        /// </summary>
        public static readonly Vector3 ArmRest = new Vector3(-90f, 170f, -80f);

        /// <summary>
        /// The floor arm: its base under the turntable's centre (on the ground, its frame level), and where its tip
        /// (the top of the cradle the coils ride) rests, just under the turntable's top in the hole.
        /// </summary>
        public static readonly Vector3 FloorArmBase = new Vector3(TurntableCentre.x, 0f, TurntableCentre.z);

        public const float FloorTipRest = PadTop + 0.015f;
        public const float FloorTipOffset = 0.03f;

        /// <summary>
        /// The hopper's mouth (its centre, at rover height) inside the bay's back right corner: ahead and to the side
        /// of a parked 07's eye, so its beam reaches the mouth with nothing in the way, not even 07 itself.
        /// </summary>
        public static readonly Vector3 HopperMouth = new Vector3(1.55f, 1.15f, -1.85f);

        /// <summary>Half the width of the bay hopper's mouth.</summary>
        public const float HopperSize = 0.38f;

        /// <summary>The arch 07 drives in under: half its span and the height its arch springs from.</summary>
        public const float ArchHalfSpan = 1.45f;

        public const float ArchSpring = 1.7f;

        /// <summary>The lit "07" sign on the roof over the arch: its centre and the numerals' height.</summary>
        public static readonly Vector3 SignCentre = new Vector3(0f, EaveHeight + 0.62f, FrontZ + 0.02f);

        private const float SignHeight = 0.42f;

        private const float Post = 0.16f;
        private const float Beam = 0.14f;
        private const int ArchSegments = 12;
        private const float FacadeDepth = 0.06f;
        private const int HazardSegments = 20;
        private const float TurntableRaise = 0.03f;
        private const float TurntableDepth = 0.09f;
        private const float TurntableBevel = 0.04f;
        private const float HazardBand = 0.1f;

        // The crane rail rides this far over the shoulders, clear of the folded elbows.
        private const float RingRise = 0.3f;

        private const float FloorColumn = 0.6f;

        // Kenji's remnant bench, left of the bay.
        private static Matrix4x4 BenchPlacement =>
            At(new Vector3(-HalfWidth - 1.95f, 0f, 0.9f), new Vector3(0f, 12f, 0f));

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
        /// The rest turn of arm <paramref name="index"/>'s Yaw joint (deg, about local Y): the side arms fold forward
        /// along their side of the rail, the back arm to the left along the back.
        /// </summary>
        public static float FoldYaw(int index)
        {
            switch (index)
            {
                case 0:
                    return -90f;
                case 1:
                    return -90f;
                case 2:
                    return 90f;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Bay arms are 0..2.");
            }
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
            var b = new LowPolyMeshBuilder(7000);
            Structure(b);
            Roof(b);
            Walls(b);
            Facade(b);
            Floor(b);
            Hopper(b);
            SiteKit.Drum(b, At(new Vector3(-HalfWidth - 0.45f, 0.56f, -1.4f), new Vector3(0f, 90f, 0f)), 0.5f, 0.6f,
                PaletteSwatch.WarmAccent);
            SiteKit.Drum(b, At(new Vector3(1.75f, PadTop + 0.42f, 1.25f)), 0.42f, 0.45f, PaletteSwatch.Charcoal);
            SiteKit.Drum(b, At(new Vector3(-1.75f, PadTop + 0.3f, 1.25f), new Vector3(0f, 90f, 0f)), 0.3f, 0.35f,
                PaletteSwatch.FadedAccent);
            Board(b);
            Sign(b);
            for (int i = 0; i < LampCount; i++)
            {
                Matrix4x4 lamp = At(LampGlass(i), LampEuler);
                b.Prism(lamp * At(0f, 0f, -0.08f) * Matrix4x4.Rotate(Rotation(AlongZ)), 0.15f, 0.16f, 10,
                    PaletteSwatch.FadedPaint);
                b.Torus(lamp * At(new Vector3(0f, 0f, 0.01f), AlongZ), 0.14f, 0.02f, 10, 3, PaletteSwatch.Metal);
                RecipeKit.Rod(b, LampGlass(i) + new Vector3(0f, 0.1f, -0.1f), new Vector3(LampGlass(i).x, EaveHeight,
                    FrontZ), 0.025f, 5, PaletteSwatch.Metal);
            }

            b.Append(WorkbenchMeshes.Bench(), BenchPlacement);
            return b;
        }

        /// <summary>
        /// The turntable (origin at its top centre, the node turns about local Y with 07 on it): a ribbed ring plate
        /// with a bevelled edge, hazard bands round its rim and round the hole in its middle, 07's tyre tracks worn in.
        /// </summary>
        public static LowPolyMeshBuilder Turntable()
        {
            var b = new LowPolyMeshBuilder(900);
            Vector2[] ring =
            {
                new Vector2(TurntableRadius + TurntableBevel, -TurntableDepth), new Vector2(TurntableRadius, 0f),
                new Vector2(TurntableHole, 0f), new Vector2(TurntableHole, -TurntableDepth),
                new Vector2(TurntableRadius + TurntableBevel, -TurntableDepth),
            };
            b.Lathe(Matrix4x4.identity, ring, HazardSegments, PaletteSwatch.Metal);
            HazardRing(b, TurntableRadius - HazardBand - 0.02f, TurntableRadius - 0.02f);
            HazardRing(b, TurntableHole + 0.01f, TurntableHole + HazardBand);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.66f, 0.004f, 0f), new Vector3(0.24f, 0.008f, TurntableRadius * 1.7f),
                    PaletteSwatch.Charcoal);
            }

            float inner = TurntableHole + HazardBand + 0.05f;
            float outer = TurntableRadius - HazardBand - 0.07f;
            for (int i = 0; i < 12; i++)
            {
                Matrix4x4 rib = At(Vector3.zero, new Vector3(0f, i * 30f + 15f, 0f));
                b.Box(rib * At(0f, 0.003f, (inner + outer) * 0.5f), new Vector3(0.05f, 0.006f, outer - inner),
                    PaletteSwatch.FadedPaint);
            }

            return b;
        }

        /// <summary>A flat band of alternating crane-yellow and black segments on the turntable's top.</summary>
        private static void HazardRing(LowPolyMeshBuilder b, float inner, float outer)
        {
            const int segments = 24;
            float step = 2f * Mathf.PI / segments;
            var points = new Vector3[4];
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = a0 + step;
                points[0] = new Vector3(Mathf.Sin(a0) * inner, 0.003f, Mathf.Cos(a0) * inner);
                points[1] = new Vector3(Mathf.Sin(a0) * outer, 0.003f, Mathf.Cos(a0) * outer);
                points[2] = new Vector3(Mathf.Sin(a1) * outer, 0.003f, Mathf.Cos(a1) * outer);
                points[3] = new Vector3(Mathf.Sin(a1) * inner, 0.003f, Mathf.Cos(a1) * inner);
                b.Polygon(points, Vector3.up, i % 2 == 0 ? PaletteSwatch.Honey : PaletteSwatch.Charcoal);
            }
        }

        /// <summary>
        /// An arm's Yaw joint (its own mesh, origin at the shoulder): a slewing ring under the trolley, the shoulder's
        /// pitch drum the upper link hangs from and the drum's motor.
        /// </summary>
        public static LowPolyMeshBuilder YawJoint()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Prism(At(0f, 0.04f, 0f), 0.16f, 0.06f, 12, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.17f, -0.01f, 0f), new Vector3(0.04f, 0.16f, 0.2f), PaletteSwatch.FadedPaint, 0.01f);
            }

            b.Prism(At(Vector3.zero, AlongX), 0.11f, 0.32f, 10, PaletteSwatch.Metal);
            b.Prism(At(new Vector3(0.24f, 0f, 0f), AlongX), 0.08f, 0.1f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The floor arm's lift (its own mesh, origin at the top of its column): a hydraulic column reaching down into
        /// the pit and a guide collar; it rises along local Y.
        /// </summary>
        public static LowPolyMeshBuilder FloorLiftMesh()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Prism(At(0f, -FloorColumn * 0.5f, 0f), 0.09f, FloorColumn, 10, PaletteSwatch.Metal);
            b.Prism(At(0f, -0.04f, 0f), 0.13f, 0.05f, 10, PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>
        /// The floor arm's tip (its own mesh, origin on top of its cradle, where a belly piece rides up): a crane-
        /// yellow cradle plate with four rubber pads, small enough to rise through the turntable's hole.
        /// </summary>
        public static LowPolyMeshBuilder FloorTipMesh()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(0f, -0.02f, 0f), new Vector3(0.44f, 0.04f, 0.62f), PaletteSwatch.Honey, 0.01f);
            b.Prism(At(0f, -FloorTipOffset - 0.02f, 0f), 0.12f, 0.04f, 10, PaletteSwatch.Charcoal);
            for (int i = 0; i < 4; i++)
            {
                b.Box(At((i % 2 == 0 ? -1f : 1f) * 0.16f, -0.004f, (i < 2 ? -1f : 1f) * 0.25f),
                    new Vector3(0.08f, 0.008f, 0.08f), PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>
        /// An arm's trolley on the crane rail (Arm_n's own mesh, origin at the shoulder): a box riding the rail on
        /// four wheels.
        /// </summary>
        public static LowPolyMeshBuilder ShoulderMount()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(0f, 0.13f, 0f), new Vector3(0.32f, 0.12f, 0.4f), PaletteSwatch.FadedPaint, 0.03f);
            for (int i = 0; i < 4; i++)
            {
                b.Prism(At(new Vector3((i % 2 == 0 ? -1f : 1f) * 0.12f, 0.2f, (i < 2 ? -1f : 1f) * 0.13f), AlongX),
                    0.04f, 0.04f, 8, PaletteSwatch.Charcoal);
            }

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

        /// <summary>
        /// A link's rust (in the link's space): a band round it just above the joint at its end, where grit and
        /// grease gathered.
        /// </summary>
        public static LowPolyMeshBuilder LinkRust(float length, float thickness)
        {
            var b = new LowPolyMeshBuilder(40);
            Weathering.Collar(b, At(0f, -length + thickness * 1.6f, 0f), thickness * 0.78f, thickness * 0.8f);
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
            b.Append(WorkbenchMeshes.Rust(), BenchPlacement);
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

            // The arms' crane rail: a square ring of crane-yellow I-beams just under the roof round the turntable, tied
            // to the side beams by cross girders; the arms' trolleys ride its underside.
            float ring = 1.3f;
            Vector3 centre = new Vector3(TurntableCentre.x, RailHeight + RingRise, TurntableCentre.z);
            Vector3[] corners =
            {
                centre + new Vector3(-ring, 0f, -ring), centre + new Vector3(ring, 0f, -ring),
                centre + new Vector3(ring, 0f, ring), centre + new Vector3(-ring, 0f, ring),
            };
            for (int i = 0; i < corners.Length; i++)
            {
                IBeam(b, corners[i], corners[(i + 1) % corners.Length]);
            }

            foreach (float z in new[] { corners[0].z, corners[2].z })
            {
                float y = centre.y + 0.06f;
                SiteKit.Bar(b, new Vector3(-HalfWidth, y, z), new Vector3(HalfWidth, y, z), 0.08f, PaletteSwatch.Metal);
            }
        }

        /// <summary>
        /// A crane-rail I-beam from <paramref name="from"/> to <paramref name="to"/> (level): a web and two flanges,
        /// running past the corners so the ring's ends overlap.
        /// </summary>
        private static void IBeam(LowPolyMeshBuilder b, Vector3 from, Vector3 to)
        {
            Matrix4x4 run = Along(from, to);
            float length = Vector3.Distance(from, to) + 0.18f;
            b.Box(run, new Vector3(0.05f, 0.2f, length), PaletteSwatch.Honey);
            for (int flange = -1; flange <= 1; flange += 2)
            {
                b.Box(run * At(0f, flange * 0.1f, 0f), new Vector3(0.18f, 0.03f, length), PaletteSwatch.Honey);
            }
        }

        /// <summary>
        /// The front: a facade between the front posts with a rover-sized arch cut in it (07 drives in under it with
        /// a metre to spare), the arch trimmed in 07's orange with a keystone.
        /// </summary>
        private static void Facade(LowPolyMeshBuilder b)
        {
            float z = FrontZ;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (ArchHalfSpan + HalfWidth) * 0.5f, EaveHeight * 0.5f, z),
                    new Vector3(HalfWidth - ArchHalfSpan, EaveHeight, FacadeDepth), PaletteSwatch.FadedPaint);
            }

            float step = 2f * ArchHalfSpan / ArchSegments;
            var strip = new Vector2[4];
            for (int i = 0; i < ArchSegments; i++)
            {
                float x0 = -ArchHalfSpan + i * step;
                float x1 = x0 + step;
                strip[0] = new Vector2(x0, ArchY(x0));
                strip[1] = new Vector2(x1, ArchY(x1));
                strip[2] = new Vector2(x1, EaveHeight);
                strip[3] = new Vector2(x0, EaveHeight);
                b.Extrude(At(0f, 0f, z), strip, FacadeDepth, PaletteSwatch.FadedPaint);
                float trim = z + FacadeDepth * 0.5f + 0.03f;
                SiteKit.Bar(b, new Vector3(x0, ArchY(x0), trim), new Vector3(x1, ArchY(x1), trim), 0.09f,
                    PaletteSwatch.WarmAccent);
            }

            b.Box(At(0f, ArchSpring + ArchHalfSpan - 0.02f, z + FacadeDepth * 0.5f + 0.05f),
                new Vector3(0.24f, 0.3f, 0.1f), PaletteSwatch.WarmAccent, 0.02f);
        }

        /// <summary>The arch's height over <paramref name="x"/> (its springing height outside the span).</summary>
        private static float ArchY(float x)
        {
            return ArchSpring + Mathf.Sqrt(Mathf.Max(0f, ArchHalfSpan * ArchHalfSpan - x * x));
        }

        /// <summary>
        /// The "07" sign's box on the roof over the arch, on two legs: its numerals are their own glow renderer
        /// (<see cref="SignGlow"/>), dark until the bay has power.
        /// </summary>
        private static void Sign(LowPolyMeshBuilder b)
        {
            b.Box(At(SignCentre), new Vector3(1.05f, 0.62f, 0.12f), PaletteSwatch.Charcoal, 0.02f);
            b.Box(At(SignCentre + new Vector3(0f, 0f, 0.061f)), new Vector3(0.95f, 0.52f, 0.004f),
                PaletteSwatch.FadedPaint);
            for (int side = -1; side <= 1; side += 2)
            {
                SiteKit.Bar(b, new Vector3(side * 0.35f, EaveHeight, SignCentre.z),
                    new Vector3(side * 0.35f, SignCentre.y - 0.31f, SignCentre.z), 0.05f, PaletteSwatch.Metal);
            }
        }

        /// <summary>The sign's warm "07" (a glow renderer on the glow-off material, in bay space).</summary>
        public static LowPolyMeshBuilder SignGlow()
        {
            var b = new LowPolyMeshBuilder(200);
            Glyphs.Write(b, At(SignCentre + new Vector3(0f, 0f, 0.063f)), "07", SignHeight, PaletteSwatch.LampGlass,
                PaletteSwatch.Charcoal);
            return b;
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

        /// <summary>The slab, the ring round the turntable, the dark pit under its hole and the ramp in.</summary>
        private static void Floor(LowPolyMeshBuilder b)
        {
            float depth = FrontZ - BackZ;
            b.Box(At(0f, PadTop * 0.5f, (FrontZ + BackZ) * 0.5f), new Vector3(2f * HalfWidth, PadTop, depth),
                PaletteSwatch.Metal, 0.03f);
            b.Torus(At(new Vector3(TurntableCentre.x, PadTop, TurntableCentre.z)),
                TurntableRadius + TurntableBevel + 0.06f, 0.04f, 20, 3, PaletteSwatch.Charcoal);

            // The pit under the turntable's hole: dark, ringed, the floor arm's column standing in it.
            b.Prism(At(TurntableCentre.x, PadTop + 0.002f, TurntableCentre.z), TurntableHole - 0.01f, 0.004f, 20,
                PaletteSwatch.Charcoal);
            b.Wedge(At(0f, PadTop * 0.5f, FrontZ + 0.55f), new Vector3(2.6f, PadTop, 1.1f), PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 1.38f, PadTop + 0.03f, FrontZ + 0.4f), new Vector3(0.08f, 0.06f, 1.2f),
                    PaletteSwatch.FadedAccent, 0.01f);
            }
        }

        /// <summary>
        /// The material hopper inside the bay's back right corner: a funnel on four legs, its mouth at rover height
        /// turned to the turntable so a parked 07's beam reaches it with nothing in the way, a chute out through the
        /// back wall to the store behind.
        /// </summary>
        private static void Hopper(LowPolyMeshBuilder b)
        {
            ServiceKit.Hopper(b, HopperMouth, HopperFacing, HopperSize);
            RecipeKit.Rod(b, new Vector3(HopperMouth.x + 0.2f, 0.66f, HopperMouth.z - 0.3f),
                new Vector3(HopperMouth.x + 0.35f, PadTop + 0.35f, BackZ - 0.12f), 0.07f, 8, PaletteSwatch.Metal);
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

        /// <summary>The old hand-painted "PIT" board on the left pier, hung askew like the bench sticker.</summary>
        private static void Board(LowPolyMeshBuilder b)
        {
            Matrix4x4 board = At(new Vector3(-(ArchHalfSpan + HalfWidth) * 0.5f, 2.3f, FrontZ + FacadeDepth * 0.5f
                + 0.03f), new Vector3(0f, 0f, -4f));
            b.Box(board, new Vector3(0.62f, 0.3f, 0.04f), PaletteSwatch.Enamel, 0.01f);
            Glyphs.Write(b, board * At(0f, 0f, 0.021f), "PIT", 0.18f, PaletteSwatch.Charcoal, PaletteSwatch.Enamel);
        }
    }
}
