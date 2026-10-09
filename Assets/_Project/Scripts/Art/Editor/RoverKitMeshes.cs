using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Geometry of 07's crafted kit (docs/features/M3-11-visible-progression.md): sturdy, well-loved expedition gear
    /// Kenji's bay builds from salvage, in 07's own cream and metal, round-edged and never spiky, gone to rust at its
    /// bolts and seams (VISION ruling 12). Each piece is sized to change 07's outline from the chase camera and at
    /// 30 m (ruling 11): the lamp bar runs past both flanks with its outer lamps beside the head, the drums ride high
    /// on the shoulders, the rack has boarded sides. Each part is built in its socket's space (the prefab parents to
    /// the socket with identity); body-space layout is converted through the socket's pose.
    /// </summary>
    internal static class RoverKitMeshes
    {
        public const int LampCount = 3;

        // The lamp bar, body space: a crossbar just over the lid's front edge and past both flanks, on two uprights
        // from brackets on the radio face, with three caged lamps standing on it (the outer two beside the head).
        private const float BarHeight = 0.985f;
        private const float BarFront = 0.9f;
        private const float BarHalfSpan = 0.56f;
        private const float BarRadius = 0.03f;
        private const float UprightX = 0.38f;
        private const float UprightRadius = 0.028f;
        private const float ElbowHeight = 0.66f;
        private const float BracketX = 0.26f;
        private const float BracketHeight = 0.57f;
        private const float BracketDepth = 0.035f;
        private const float EndCapRadius = 0.046f;
        private const float LampSpacing = 0.42f;
        private const float LampRise = 0.085f;
        private const float LampAhead = 0.06f;
        private const float LampRadius = 0.062f;
        private const float CanRadius = 0.082f;
        private const float CanLength = 0.08f;
        private const float CanBackLength = 0.035f;
        private const float GuardAhead = 0.045f;

        // The guard's upright bars stand this far off the lamp's axis, so they sit outside the cone of the road light
        // play moves into the middle glass (90 degrees wide) and never catch it as a glare beside the eye.
        private const float GuardBarX = 0.054f;
        private const int LampSides = 12;

        // The capacitor drums, drum-socket space (+X out from the lid edge): a fat drum high on the shoulder, on two
        // saddles bolted to the lid's side, under the solar wing's edge and over the middle wheel's travel.
        private const float DrumRadius = 0.14f;
        private const float DrumLength = 0.4f;
        private const float DrumCapLength = 0.05f;
        private const float DomeLength = 0.02f;
        private const float BandWidth = 0.06f;
        private const float StrapOffset = 0.11f;
        private const float RibInset = 0.035f;
        private const float LugHeight = 0.055f;
        private const float TerminalLean = 35f;
        private const float TerminalOffset = 0.06f;
        private const int DrumSides = 14;

        // The cargo rack, body space: a crate-like basket behind the hatch, its boarded sides standing well over the
        // rear wheels, its back low so the chase camera's lines to the back "07" and the tail lamps pass over it (even
        // from 30 m), its straps slung across the top.
        private const float RackFloor = 0.5f;
        private const float RackTop = 0.84f;
        private const float RackBackTop = 0.68f;
        private const float StrapSag = 0.72f;
        private const float RackHalfWidth = 0.4f;
        private const float RackFront = -0.68f;
        private const float RackBack = -1.2f;
        private const float RackTubeRadius = 0.022f;
        private const float SlatLift = 0.02f;
        private const float SlatThickness = 0.018f;
        private const float BoardHeight = 0.08f;
        private const float BoardThickness = 0.02f;

        /// <summary>The drum's axis in drum space: out past the lid edge and up on the shoulder, along +Z.</summary>
        public static readonly Vector3 DrumCentre = new Vector3(0.205f, 0.14f, 0f);

        private static readonly float[] SideBoards = { 0.575f, 0.67f, 0.765f };

        private static readonly float[] BackBoards = { 0.575f };

        private static readonly float[] StrapRows = { -0.85f, -1.03f };

        /// <summary>Body space to HeadlampSocket space (the socket sits low on the front, pitched down).</summary>
        private static Matrix4x4 BodyToLampSocket =>
            Matrix4x4.Rotate(Rotation(new Vector3(-RoverModelBuilder.HeadlampPitchDegrees, 0f, 0f)))
            * Matrix4x4.Translate(-RoverModelBuilder.HeadlampPosition);

        /// <summary>Centre of lamp <paramref name="index"/>'s glass, in HeadlampSocket space.</summary>
        public static Vector3 LampGlassCentre(int index)
        {
            var body = new Vector3((index - 1) * LampSpacing, BarHeight + LampRise, BarFront + LampAhead);
            return BodyToLampSocket.MultiplyPoint3x4(body);
        }

        /// <summary>The relic's resting point on the rack's slats, in CargoSocket space.</summary>
        public static Vector3 RelicSeat => RackPoint(new Vector3(0f, RackFloor + SlatLift + SlatThickness * 0.5f,
            (RackFront + RackBack) * 0.5f));

        /// <summary>
        /// The lamp bar's frame: charcoal brackets bolted to the radio face (rust weeping from under them), two
        /// uprights rising in front of the lid to clamps on a crossbar that runs past both flanks to cream end caps,
        /// and three lamps standing on the bar in yokes, each a cream can with a metal back, a bright bezel round its
        /// glass and a round guard of two bars standing proud. The glasses are separate glow renderers.
        /// </summary>
        public static LowPolyMeshBuilder LampBar()
        {
            var b = new LowPolyMeshBuilder(3000);
            Matrix4x4 toSocket = BodyToLampSocket;
            var level = new Vector3(-RoverModelBuilder.HeadlampPitchDegrees, 0f, 0f);
            Matrix4x4 face = toSocket * SiteKit.Face(new Vector3(0f, 0f, RoverMeshes.BodyFront), Vector3.forward,
                Vector3.up);
            for (int side = -1; side <= 1; side += 2)
            {
                var bracket = new Vector3(side * BracketX, BracketHeight, RoverMeshes.BodyFront + BracketDepth * 0.5f);
                var elbow = new Vector3(side * UprightX, ElbowHeight, BarFront);
                var top = new Vector3(side * UprightX, BarHeight, BarFront);
                b.Box(At(toSocket.MultiplyPoint3x4(bracket), level), new Vector3(0.1f, 0.13f, BracketDepth),
                    PaletteSwatch.Charcoal, 0.008f);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 bolt = bracket + new Vector3(k * 0.032f, k * 0.042f, BracketDepth * 0.5f);
                    b.Icosphere(At(toSocket.MultiplyPoint3x4(bolt)), 0.011f, 0, PaletteSwatch.Rust);
                }

                SiteKit.RustStreak(b, face, side * (BracketX + 0.02f), BracketHeight - 0.065f, 0.06f, 0.014f);
                Tube(b, toSocket, bracket + Vector3.forward * (BracketDepth * 0.5f), elbow, UprightRadius);
                Tube(b, toSocket, elbow, top, UprightRadius);
                b.Icosphere(At(toSocket.MultiplyPoint3x4(elbow)), UprightRadius * 1.15f, 1, PaletteSwatch.Metal);
                b.Box(At(toSocket.MultiplyPoint3x4(top), level), new Vector3(0.075f, 0.075f, 0.075f),
                    PaletteSwatch.Metal, 0.012f);
                b.Prism(At(toSocket.MultiplyPoint3x4(top + Vector3.right * side * 0.048f), AlongX),
                    BarRadius + 0.005f, 0.014f, 10, PaletteSwatch.Rust);
                b.Icosphere(At(toSocket.MultiplyPoint3x4(new Vector3(side * BarHalfSpan, BarHeight, BarFront))),
                    EndCapRadius, 1, PaletteSwatch.Enamel);
            }

            Tube(b, toSocket, new Vector3(-BarHalfSpan, BarHeight, BarFront),
                new Vector3(BarHalfSpan, BarHeight, BarFront), BarRadius);
            for (int i = 0; i < LampCount; i++)
            {
                var bar = new Vector3((i - 1) * LampSpacing, BarHeight, BarFront);
                Lamp(b, LampGlassCentre(i), toSocket.MultiplyPoint3x4(bar));
            }

            return b;
        }

        /// <summary>One lamp glass, a dark disc facing the socket's +Z (it glows WarmLamp when lit).</summary>
        public static LowPolyMeshBuilder LampGlass()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), LampRadius, 0.012f, LampSides, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// The capacitor drum ("the big batteries"): a fat cream drum riding high on the shoulder on two saddles bolted
        /// to the lid's side, held by charcoal straps with metal buckles, ribbed near its domed metal end caps (light
        /// all over, so an end seen head-on reads as a tank, never a barrel's mouth; their seams rusted), two battery
        /// terminals leaning inboard on rusty collars, a lifting lug for the bay's arms and a fat lead into the lid.
        /// The cyan window band round its middle is the separate Glow renderer.
        /// </summary>
        public static LowPolyMeshBuilder CapacitorDrum()
        {
            var b = new LowPolyMeshBuilder(1600);
            float half = DrumLength * 0.5f;
            b.Prism(At(DrumCentre, AlongZ), DrumRadius, DrumLength, DrumSides, PaletteSwatch.Enamel);
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 along = Vector3.forward * end;
                Vector3 outward = end > 0 ? AlongZ : new Vector3(-90f, 0f, 0f);
                b.Frustum(At(DrumCentre + along * (half + DrumCapLength * 0.5f), outward), DrumRadius,
                    DrumRadius * 0.62f, DrumCapLength, DrumSides, PaletteSwatch.Metal);
                b.Frustum(At(DrumCentre + along * (half + DrumCapLength + DomeLength * 0.5f), outward),
                    DrumRadius * 0.62f, DrumRadius * 0.3f, DomeLength, DrumSides, PaletteSwatch.Metal);
                b.Prism(At(DrumCentre + along * half, AlongZ), DrumRadius + 0.004f, 0.014f, DrumSides,
                    PaletteSwatch.Rust, false);
                b.Prism(At(DrumCentre + along * (half - RibInset), AlongZ), DrumRadius + 0.01f, 0.02f, DrumSides,
                    PaletteSwatch.Metal);
                Vector3 strap = DrumCentre + along * StrapOffset;
                b.Prism(At(strap, AlongZ), DrumRadius + 0.007f, 0.036f, DrumSides, PaletteSwatch.Charcoal, false);
                b.Box(At(strap + Vector3.right * (DrumRadius + 0.014f)), new Vector3(0.022f, 0.056f, 0.046f),
                    PaletteSwatch.Metal, 0.005f);
                Saddle(b, strap.z);
                Terminal(b, DrumCentre + along * TerminalOffset);
            }

            LiftingLug(b);
            RecipeKit.Rod(b, DrumCentre + new Vector3(0.012f - DrumRadius, -0.03f, 0.06f),
                new Vector3(0.004f, -0.075f, 0.07f), 0.017f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The drum's window band (drum space, origin on the drum axis); glows cyan like the coils.</summary>
        public static LowPolyMeshBuilder DrumGlow()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), DrumRadius + 0.004f, BandWidth, DrumSides,
                PaletteSwatch.EyeGlass, false);
            return b;
        }

        /// <summary>
        /// The cargo cradle: a crate-like rear rack basket on two short arms from the hatch (their plates rusted on), a
        /// metal tube frame with rust collars at the post feet, three wooden boards along each side standing well over
        /// the rear wheels and one low across the back, a floor of wooden slats, and two worn orange straps slung slack
        /// across the top, one buckle hanging. Its back stays under the chase camera's lines to the back "07" and the
        /// tail lamps.
        /// </summary>
        public static LowPolyMeshBuilder CargoRack()
        {
            var b = new LowPolyMeshBuilder(2000);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * RackHalfWidth;
                foreach (float z in new[] { RackFront, RackBack })
                {
                    RackTube(b, new Vector3(x, RackFloor, z), new Vector3(x, RackTop, z));
                    b.Icosphere(At(RackPoint(new Vector3(x, RackTop, z))), RackTubeRadius * 1.6f, 1,
                        PaletteSwatch.Metal);
                    b.Prism(At(RackPoint(new Vector3(x, RackFloor + 0.035f, z))), RackTubeRadius + 0.004f, 0.02f, 8,
                        PaletteSwatch.Rust);
                }

                RackTube(b, new Vector3(x, RackFloor, RackFront), new Vector3(x, RackFloor, RackBack));
                RackTube(b, new Vector3(x, RackTop, RackFront), new Vector3(x, RackTop, RackBack));
                foreach (float y in SideBoards)
                {
                    b.Box(At(RackPoint(new Vector3(x, y, (RackFront + RackBack) * 0.5f))),
                        new Vector3(BoardThickness, BoardHeight, RackFront - RackBack - 2f * RackTubeRadius),
                        PaletteSwatch.Wood, 0.004f);
                }
            }

            RackTube(b, new Vector3(-RackHalfWidth, RackFloor, RackFront), new Vector3(RackHalfWidth, RackFloor,
                RackFront));
            RackTube(b, new Vector3(-RackHalfWidth, RackTop, RackFront),
                new Vector3(RackHalfWidth, RackTop, RackFront));
            RackTube(b, new Vector3(-RackHalfWidth, RackFloor, RackBack), new Vector3(RackHalfWidth, RackFloor,
                RackBack));
            RackTube(b, new Vector3(-RackHalfWidth, RackBackTop, RackBack), new Vector3(RackHalfWidth, RackBackTop,
                RackBack));
            foreach (float x in new[] { -0.13f, 0.13f })
            {
                RackTube(b, new Vector3(x, RackFloor, RackBack), new Vector3(x, RackBackTop, RackBack));
            }

            foreach (float y in BackBoards)
            {
                b.Box(At(RackPoint(new Vector3(0f, y, RackBack))),
                    new Vector3(2f * (RackHalfWidth - RackTubeRadius), BoardHeight, BoardThickness), PaletteSwatch.Wood,
                    0.004f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                RackTube(b, new Vector3(side * 0.2f, 0.46f, -0.625f), new Vector3(side * 0.2f, RackFloor, RackFront));
                b.Box(At(RackPoint(new Vector3(side * 0.2f, 0.46f, -0.628f))), new Vector3(0.07f, 0.07f, 0.012f),
                    PaletteSwatch.Rust);
            }

            for (int i = 0; i < 4; i++)
            {
                float x = -0.27f + i * 0.18f;
                b.Box(At(RackPoint(new Vector3(x, RackFloor + SlatLift, (RackFront + RackBack) * 0.5f))),
                    new Vector3(0.15f, SlatThickness, RackFront - RackBack - 0.02f), PaletteSwatch.Wood, 0.004f);
            }

            foreach (float z in StrapRows)
            {
                Strap(b, new Vector3(-RackHalfWidth, RackTop + 0.02f, z), new Vector3(0f, StrapSag, z + 0.01f));
                Strap(b, new Vector3(0f, StrapSag, z + 0.01f), new Vector3(RackHalfWidth, RackTop + 0.02f, z));
            }

            float hanging = StrapRows[1];
            Strap(b, new Vector3(RackHalfWidth + 0.02f, RackTop + 0.01f, hanging),
                new Vector3(RackHalfWidth + 0.03f, RackTop - 0.12f, hanging - 0.02f));
            b.Box(At(RackPoint(new Vector3(RackHalfWidth + 0.03f, RackTop - 0.14f, hanging - 0.02f))),
                new Vector3(0.016f, 0.05f, 0.06f), PaletteSwatch.Rust, 0.004f);
            return b;
        }

        /// <summary>
        /// One lamp in HeadlampSocket space, its glass at <paramref name="glass"/>, standing on the bar at
        /// <paramref name="bar"/>: can, back, bezel, guard and the yoke that holds it.
        /// </summary>
        private static void Lamp(LowPolyMeshBuilder b, Vector3 glass, Vector3 bar)
        {
            Vector3 can = glass - Vector3.forward * (0.004f + CanLength * 0.5f);
            b.Prism(At(can, AlongZ), CanRadius, CanLength, LampSides, PaletteSwatch.Enamel);
            b.Frustum(At(can - Vector3.forward * ((CanLength + CanBackLength) * 0.5f), new Vector3(-90f, 0f, 0f)),
                CanRadius, CanRadius * 0.6f, CanBackLength, LampSides, PaletteSwatch.Metal);
            b.Torus(At(glass + Vector3.forward * 0.004f, AlongZ), LampRadius + 0.009f, 0.011f, 14, 4,
                PaletteSwatch.Metal);
            Guard(b, glass);
            for (int side = -1; side <= 1; side += 2)
            {
                var plate = new Vector3(can.x + side * (CanRadius + 0.009f), (can.y + bar.y) * 0.5f, can.z);
                b.Box(At(plate), new Vector3(0.012f, can.y - bar.y + 0.03f, 0.05f), PaletteSwatch.Metal, 0.003f);
                b.Prism(At(new Vector3(can.x + side * (CanRadius + 0.024f), can.y, can.z), AlongX), 0.02f, 0.02f, 8,
                    PaletteSwatch.Charcoal);
            }

            b.Box(At(new Vector3(can.x, bar.y + BarRadius * 0.7f, can.z)),
                new Vector3(2f * CanRadius + 0.03f, 0.022f, 0.055f), PaletteSwatch.Metal, 0.004f);
        }

        /// <summary>
        /// A round guard over a lamp glass, standing proud of the bezel on four short spokes: a rim ring and two
        /// upright bars across it.
        /// </summary>
        private static void Guard(LowPolyMeshBuilder b, Vector3 glass)
        {
            Vector3 ring = glass + Vector3.forward * GuardAhead;
            float radius = CanRadius - 0.006f;
            b.Torus(At(ring, AlongZ), radius, 0.008f, 14, 4, PaletteSwatch.Metal);
            float barHalf = Mathf.Sqrt(radius * radius - GuardBarX * GuardBarX);
            for (int k = -1; k <= 1; k += 2)
            {
                b.Box(At(ring + Vector3.right * k * GuardBarX), new Vector3(0.009f, 2f * barHalf, 0.009f),
                    PaletteSwatch.Metal);
            }

            for (int k = 0; k < 4; k++)
            {
                float angle = (45f + 90f * k) * Mathf.Deg2Rad;
                Vector3 spoke = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                RecipeKit.Rod(b, glass + Vector3.forward * 0.006f + spoke, ring + spoke, 0.006f, 5,
                    PaletteSwatch.Metal);
            }
        }

        /// <summary>
        /// One saddle under the drum at <paramref name="z"/>: a charcoal block bolted to the lid's side (below the
        /// solar wing's edge), an upright block outboard of the wing's edge (its rusty bolts on its outer face), and a
        /// metal plate from its top tangent to the drum low on its inboard side (clear of the middle wheel at the top
        /// of its travel).
        /// </summary>
        private static void Saddle(LowPolyMeshBuilder b, float z)
        {
            b.Box(At(new Vector3(0.015f, -0.0575f, z)), new Vector3(0.04f, 0.085f, 0.05f), PaletteSwatch.Charcoal,
                0.006f);
            b.Box(At(new Vector3(0.0475f, 0.005f, z)), new Vector3(0.025f, 0.13f, 0.05f), PaletteSwatch.Charcoal,
                0.004f);
            var down = new Vector3(-Mathf.Sqrt(0.5f), -Mathf.Sqrt(0.5f), 0f);
            b.Box(At(DrumCentre + down * (DrumRadius + 0.009f) + Vector3.forward * z, new Vector3(0f, 0f, -45f)),
                new Vector3(0.12f, 0.018f, 0.05f), PaletteSwatch.Metal, 0.004f);
            for (int k = -1; k <= 1; k += 2)
            {
                b.Icosphere(At(new Vector3(0.056f, -0.035f, z + k * 0.014f)), 0.009f, 0, PaletteSwatch.Rust);
            }
        }

        /// <summary>
        /// A battery terminal on the drum's upper inboard shoulder: a rusty collar, a post, a rubber boot.
        /// </summary>
        private static void Terminal(LowPolyMeshBuilder b, Vector3 onAxis)
        {
            var lean = new Vector3(0f, 0f, TerminalLean);
            Vector3 up = Rotation(lean) * Vector3.up;
            Vector3 foot = onAxis + up * DrumRadius;
            b.Prism(At(foot + up * 0.004f, lean), 0.024f, 0.008f, 8, PaletteSwatch.Rust);
            b.Prism(At(foot + up * 0.02f, lean), 0.016f, 0.03f, 8, PaletteSwatch.Metal);
            b.Prism(At(foot + up * 0.04f, lean), 0.021f, 0.016f, 8, PaletteSwatch.Charcoal);
        }

        /// <summary>A lifting lug over the drum's top from strap to strap, for the bay's arms to carry it by.</summary>
        private static void LiftingLug(LowPolyMeshBuilder b)
        {
            float top = DrumCentre.y + DrumRadius;
            var lug = new[]
            {
                new Vector3(DrumCentre.x, top + 0.004f, -StrapOffset),
                new Vector3(DrumCentre.x, top + LugHeight, 0.035f - StrapOffset),
                new Vector3(DrumCentre.x, top + LugHeight, StrapOffset - 0.035f),
                new Vector3(DrumCentre.x, top + 0.004f, StrapOffset),
            };
            for (int i = 1; i < lug.Length; i++)
            {
                RecipeKit.Rod(b, lug[i - 1], lug[i], 0.012f, 6, PaletteSwatch.Metal);
                b.Icosphere(At(lug[i]), 0.0135f, 0, PaletteSwatch.Metal);
            }

            b.Icosphere(At(lug[0]), 0.0135f, 0, PaletteSwatch.Metal);
        }

        private static void Tube(LowPolyMeshBuilder b, Matrix4x4 toSocket, Vector3 from, Vector3 to, float radius)
        {
            RecipeKit.Rod(b, toSocket.MultiplyPoint3x4(from), toSocket.MultiplyPoint3x4(to), radius, 8,
                PaletteSwatch.Metal);
        }

        private static Vector3 RackPoint(Vector3 body)
        {
            return body - RoverModelBuilder.CargoPosition;
        }

        private static void RackTube(LowPolyMeshBuilder b, Vector3 from, Vector3 to)
        {
            RecipeKit.Rod(b, RackPoint(from), RackPoint(to), RackTubeRadius, 8, PaletteSwatch.Metal);
        }

        /// <summary>A flat strap segment between two body-space points, lying broad side up.</summary>
        private static void Strap(LowPolyMeshBuilder b, Vector3 from, Vector3 to)
        {
            b.Box(Along(RackPoint(from), RackPoint(to)), new Vector3(0.05f, 0.008f, Vector3.Distance(from, to) + 0.02f),
                PaletteSwatch.WarmAccent);
        }
    }
}
