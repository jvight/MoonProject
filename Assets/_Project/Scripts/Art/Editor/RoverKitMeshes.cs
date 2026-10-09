using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Geometry of 07's crafted kit (docs/features/M3-11-visible-progression.md): sturdy, well-loved expedition gear in
    /// 07's own cream, worn orange and metal, round-edged and never spiky. Each part is built in its socket's space
    /// (the prefab parents to the socket with identity); body-space layout is converted through the socket's pose.
    /// </summary>
    internal static class RoverKitMeshes
    {
        public const int LampCount = 3;

        private const float BarHeight = 0.99f;
        private const float BarFront = 0.79f;
        private const float BarHalfSpan = 0.4f;
        private const float BarRadius = 0.024f;
        private const float LampRise = 0.062f;
        private const float LampRadius = 0.058f;
        private const float LampDepth = 0.07f;
        private const float LampSpacing = 0.27f;

        private const float DrumRadius = 0.105f;
        private const float DrumLength = 0.32f;
        private const float DrumCapLength = 0.035f;
        private const float BandWidth = 0.05f;

        private const float RackFloor = 0.5f;
        private const float RackTop = 0.72f;
        private const float HoopTop = 0.84f;
        private const float RackHalfWidth = 0.36f;
        private const float RackFront = -0.68f;
        private const float RackBack = -1.12f;
        private const float RackTubeRadius = 0.018f;

        /// <summary>The drum's axis in drum space: out from the lid edge, a little up, along +Z.</summary>
        public static readonly Vector3 DrumCentre = new Vector3(0.17f, 0.05f, 0f);

        /// <summary>Body space to HeadlampSocket space (the socket sits low on the front, pitched down).</summary>
        private static Matrix4x4 BodyToLampSocket =>
            Matrix4x4.Rotate(Rotation(new Vector3(-RoverModelBuilder.HeadlampPitchDegrees, 0f, 0f)))
            * Matrix4x4.Translate(-RoverModelBuilder.HeadlampPosition);

        /// <summary>Centre of lamp <paramref name="index"/>'s glass, in HeadlampSocket space.</summary>
        public static Vector3 LampGlassCentre(int index)
        {
            var body = new Vector3((index - 1) * LampSpacing, BarHeight + LampRise, BarFront + LampDepth * 0.5f);
            return BodyToLampSocket.MultiplyPoint3x4(body);
        }

        /// <summary>The relic's resting point on the rack floor, in CargoSocket space.</summary>
        public static Vector3 RelicSeat => new Vector3(0f, RackFloor + 0.025f, (RackFront + RackBack) * 0.5f)
            - RoverModelBuilder.CargoPosition;

        /// <summary>
        /// The lamp bar's frame: two uprights from brackets on the radio face rising in front of the lid, a crossbar
        /// just over the lid's front edge (it shows above the body from the chase camera), cream end caps, and three
        /// cream lamp housings on top, each behind a round cage. The glasses are separate glow renderers.
        /// </summary>
        public static LowPolyMeshBuilder LampBar()
        {
            var b = new LowPolyMeshBuilder(900);
            Matrix4x4 toSocket = BodyToLampSocket;
            for (int side = -1; side <= 1; side += 2)
            {
                var bracket = new Vector3(side * 0.3f, 0.52f, 0.7f);
                var elbow = new Vector3(side * 0.37f, 0.64f, BarFront);
                var top = new Vector3(side * 0.37f, BarHeight, BarFront);
                b.Box(At(toSocket.MultiplyPoint3x4(bracket),
                    new Vector3(-RoverModelBuilder.HeadlampPitchDegrees, 0f, 0f)),
                    new Vector3(0.08f, 0.11f, 0.035f), PaletteSwatch.Charcoal, 0.008f);
                Tube(b, toSocket, bracket, elbow, BarRadius);
                Tube(b, toSocket, elbow, top, BarRadius);
                b.Icosphere(At(toSocket.MultiplyPoint3x4(elbow)), BarRadius * 1.1f, 1, PaletteSwatch.Metal);
                b.Icosphere(At(toSocket.MultiplyPoint3x4(new Vector3(side * BarHalfSpan, BarHeight, BarFront))),
                    0.036f, 1, PaletteSwatch.Enamel);
            }

            Tube(b, toSocket, new Vector3(-BarHalfSpan, BarHeight, BarFront),
                new Vector3(BarHalfSpan, BarHeight, BarFront), BarRadius);
            for (int i = 0; i < LampCount; i++)
            {
                Vector3 glass = LampGlassCentre(i);
                Vector3 housing = glass - Vector3.forward * LampDepth * 0.5f;
                b.Box(At(housing + new Vector3(0f, -LampRise * 0.6f, -0.005f)), new Vector3(0.05f, 0.04f, 0.04f),
                    PaletteSwatch.Metal, 0.006f);
                b.Prism(At(housing, AlongZ), LampRadius + 0.008f, LampDepth, 10, PaletteSwatch.Enamel);
                b.Frustum(At(housing - Vector3.forward * (LampDepth * 0.5f + 0.012f), AlongZ), LampRadius * 0.6f,
                    LampRadius + 0.006f, 0.024f, 10, PaletteSwatch.Metal);
                Cage(b, glass);
            }

            return b;
        }

        /// <summary>One lamp glass, a dark disc facing the socket's +Z (it glows WarmLamp when lit).</summary>
        public static LowPolyMeshBuilder LampGlass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), LampRadius, 0.012f, 10, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// The capacitor drum ("the big batteries"): a cream drum riding the lid edge on two metal saddles with
        /// charcoal straps, domed metal end caps, two battery terminals with worn orange caps and a fat lead into the
        /// body. The cyan window band around its middle is the separate Glow renderer.
        /// </summary>
        public static LowPolyMeshBuilder CapacitorDrum()
        {
            var b = new LowPolyMeshBuilder(500);
            Matrix4x4 axis = At(DrumCentre, AlongZ);
            b.Prism(axis, DrumRadius, DrumLength, 12, PaletteSwatch.Enamel);
            for (int end = -1; end <= 1; end += 2)
            {
                float z = end * (DrumLength * 0.5f + DrumCapLength * 0.5f);
                b.Frustum(At(DrumCentre + Vector3.forward * z, end > 0 ? AlongZ : new Vector3(-90f, 0f, 0f)),
                    DrumRadius, DrumRadius * 0.62f, DrumCapLength, 12, PaletteSwatch.Metal);
                float strap = end * DrumLength * 0.3f;
                b.Prism(At(DrumCentre + Vector3.forward * strap, AlongZ), DrumRadius + 0.006f, 0.03f, 12,
                    PaletteSwatch.Charcoal, false);
                b.Box(At(new Vector3(0.045f, 0.01f, strap)), new Vector3(0.09f, 0.07f, 0.035f), PaletteSwatch.Metal,
                    0.008f);
                Vector3 terminal = DrumCentre + new Vector3(0.015f, DrumRadius, end * 0.075f);
                b.Prism(At(terminal + Vector3.up * 0.012f), 0.016f, 0.03f, 8, PaletteSwatch.Metal);
                b.Prism(At(terminal + Vector3.up * 0.03f), 0.02f, 0.014f, 8, PaletteSwatch.WarmAccent);
            }

            RecipeKit.Rod(b, DrumCentre + new Vector3(-0.02f, DrumRadius + 0.03f, 0.075f),
                new Vector3(0.005f, 0.035f, 0.11f), 0.014f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The drum's window band (drum space, origin on the drum axis); glows cyan like the coils.</summary>
        public static LowPolyMeshBuilder DrumGlow()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(Matrix4x4.Rotate(Rotation(AlongZ)), DrumRadius + 0.004f, BandWidth, 12, PaletteSwatch.EyeGlass,
                false);
            return b;
        }

        /// <summary>
        /// The cargo cradle: a rear rack basket on two short arms from the hatch, a metal tube frame with rubber
        /// corner caps and a carry hoop with a rubber grip, a floor of wooden slats and two worn orange straps lying
        /// slack across it, one buckle hanging. It stays below the back stripe, so the chase camera still reads the
        /// "07".
        /// </summary>
        public static LowPolyMeshBuilder CargoRack()
        {
            var b = new LowPolyMeshBuilder(900);
            var corners = new[]
            {
                new Vector2(-RackHalfWidth, RackFront), new Vector2(RackHalfWidth, RackFront),
                new Vector2(RackHalfWidth, RackBack), new Vector2(-RackHalfWidth, RackBack),
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 from = corners[i];
                Vector2 to = corners[(i + 1) % corners.Length];
                RackTube(b, new Vector3(from.x, RackFloor, from.y), new Vector3(to.x, RackFloor, to.y));
                RackTube(b, new Vector3(from.x, RackTop, from.y), new Vector3(to.x, RackTop, to.y));
                RackTube(b, new Vector3(from.x, RackFloor, from.y), new Vector3(from.x, RackTop, from.y));
                b.Icosphere(At(RackPoint(new Vector3(from.x, RackTop, from.y))), RackTubeRadius * 1.9f, 1,
                    PaletteSwatch.Charcoal);
            }

            foreach (float x in new[] { -0.12f, 0.12f })
            {
                RackTube(b, new Vector3(x, RackFloor, RackBack), new Vector3(x, RackTop, RackBack));
            }

            // A carry hoop over the back rail: it lifts the cradle's outline above the rear wheels from afar while
            // staying under the line from the chase camera to the back "07".
            var hoop = new[]
            {
                new Vector3(-0.3f, RackTop, RackBack), new Vector3(-0.25f, HoopTop, RackBack),
                new Vector3(0.25f, HoopTop, RackBack), new Vector3(0.3f, RackTop, RackBack),
            };
            for (int i = 1; i < hoop.Length; i++)
            {
                RackTube(b, hoop[i - 1], hoop[i]);
                b.Icosphere(At(RackPoint(hoop[i - 1])), RackTubeRadius * 1.2f, 1, PaletteSwatch.Metal);
            }

            b.Prism(At(RackPoint(new Vector3(0f, HoopTop, RackBack)), AlongX), RackTubeRadius * 1.8f, 0.2f, 8,
                PaletteSwatch.Charcoal);

            for (int side = -1; side <= 1; side += 2)
            {
                RackTube(b, new Vector3(side * 0.2f, 0.46f, -0.625f), new Vector3(side * 0.2f, RackFloor, RackFront));
                b.Box(At(RackPoint(new Vector3(side * 0.2f, 0.46f, -0.628f))), new Vector3(0.07f, 0.07f, 0.012f),
                    PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < 4; i++)
            {
                float x = -0.24f + i * 0.16f;
                b.Box(At(RackPoint(new Vector3(x, RackFloor + 0.012f, (RackFront + RackBack) * 0.5f))),
                    new Vector3(0.13f, 0.018f, RackFront - RackBack - 0.02f), PaletteSwatch.Wood, 0.004f);
            }

            foreach (float z in new[] { -0.82f, -0.99f })
            {
                Strap(b, new Vector3(-RackHalfWidth, RackTop + 0.02f, z),
                    new Vector3(0f, RackFloor + 0.06f, z + 0.01f));
                Strap(b, new Vector3(0f, RackFloor + 0.06f, z + 0.01f), new Vector3(RackHalfWidth, RackTop + 0.02f, z));
            }

            Strap(b, new Vector3(RackHalfWidth + 0.02f, RackTop + 0.01f, -0.99f),
                new Vector3(RackHalfWidth + 0.03f, RackTop - 0.12f, -1.01f));
            b.Box(At(RackPoint(new Vector3(RackHalfWidth + 0.03f, RackTop - 0.14f, -1.01f))),
                new Vector3(0.016f, 0.05f, 0.06f), PaletteSwatch.Charcoal, 0.004f);
            return b;
        }

        private static void Tube(LowPolyMeshBuilder b, Matrix4x4 toSocket, Vector3 from, Vector3 to, float radius)
        {
            RecipeKit.Rod(b, toSocket.MultiplyPoint3x4(from), toSocket.MultiplyPoint3x4(to), radius, 8,
                PaletteSwatch.Metal);
        }

        /// <summary>A round cage over a lamp glass: a rim ring and a cross of bars standing a little proud.</summary>
        private static void Cage(LowPolyMeshBuilder b, Vector3 glass)
        {
            Vector3 front = glass + Vector3.forward * 0.03f;
            b.Torus(At(front, AlongZ), LampRadius + 0.004f, 0.007f, 12, 4, PaletteSwatch.Metal);
            b.Box(At(front), new Vector3(0.008f, 2f * LampRadius, 0.008f), PaletteSwatch.Metal);
            b.Box(At(front), new Vector3(2f * LampRadius, 0.008f, 0.008f), PaletteSwatch.Metal);
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
