using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Geometry of the rover "07": a small, old survey rover, gently melancholic machinery (docs/VISION.md). A tall
    /// rounded radio-cabinet body in warm enamel with a broad worn stripe, a big hooded one-eyed head on a thin
    /// neck, gentle wheels. Each method returns one part in its node's local space (pivots: see
    /// <see cref="RoverModelBuilder"/>). Right-side parts (bogie, wheels) are built for +X and mirrored for the left.
    /// </summary>
    internal static class RoverMeshes
    {
        public const float LidTop = 0.95f;
        public const float LampHeight = 0.47f;

        /// <summary>Front of the lens dome along the eye's +Z (where beams leave).</summary>
        public const float LensFront = 0.042f;

        /// <summary>Head hinge in neck space: the top of a thin neck leaning forward like a tired desk lamp.</summary>
        public static readonly Vector3 HeadHinge = new Vector3(0f, 0.27f, 0.07f);

        /// <summary>Eye (and eyelid) pivot in head space: the lens centre.</summary>
        public static readonly Vector3 EyeCentre = new Vector3(0f, 0.16f, 0.21f);

        /// <summary>Antenna tip in antenna space: the end of the bent whip.</summary>
        public static readonly Vector3 AntennaTipPosition = new Vector3(0.035f, 0.56f, -0.045f);

        /// <summary>The whip's tired kink in antenna space.</summary>
        public static readonly Vector3 AntennaKink = new Vector3(0f, 0.33f, 0.01f);

        /// <summary>
        /// The folded solar wing's 2 x 3 cell grid (wing space): cell size (x across, y along the wing).
        /// </summary>
        public static readonly Vector2 WingCell =
            new Vector2(WingHalfWidth - 1.5f * WingBar, (WingLength - 4f * WingBar) / 3f);

        public const float CellThickness = 0.012f;

        /// <summary>The cell lost to the years (rear right), the gap Tilly's gift fills.</summary>
        public const int MissingRow = 2;

        public const int MissingColumn = 1;

        private const float BodyCentreZ = 0.03f;
        internal const float BodyHalfWidth = 0.44f;
        private const float BodyFront = 0.68f;
        internal const float BodyBack = -0.62f;
        private const float TubBottom = 0.34f;
        private const float TubTop = 0.84f;
        private const float BodyCorner = 0.16f;
        internal const float StripeY = 0.75f;
        internal const float StripeHeight = 0.1f;
        internal const float PaintProud = 0.0035f;
        internal const float PaintThickness = 0.012f;

        /// <summary>Half-length of the straight (non-rounded) part of the front and back faces.</summary>
        private const float FaceHalfSpan = BodyHalfWidth - BodyCorner;

        private const float WheelHalfWidth = 0.11f;
        private const float TyreRadius = 0.335f;
        private const float LugRadius = 0.032f;
        private const float LidInner = 0.183f;
        private const float LidOuter = 0.196f;
        private const float LidWidth = 0.38f;
        private const float BrowInner = 0.205f;
        private const float BrowOuter = 0.255f;
        private const float WingBar = 0.02f;
        private const float WingHalfWidth = 0.31f;
        private const float WingLength = 0.62f;

        private static readonly Vector2[] Seven =
        {
            new Vector2(-0.036f, 0.055f), new Vector2(0.038f, 0.055f), new Vector2(0.038f, 0.037f),
            new Vector2(-0.006f, -0.055f), new Vector2(-0.027f, -0.055f), new Vector2(0.015f, 0.037f),
            new Vector2(-0.036f, 0.037f),
        };

        private static readonly Vector2[] LensProfile =
        {
            new Vector2(0.14f, -0.009f), new Vector2(0.118f, 0.018f), new Vector2(0.067f, 0.036f),
            new Vector2(0f, LensFront),
        };

        public static LowPolyMeshBuilder Body()
        {
            var b = new LowPolyMeshBuilder(1400);
            float length = BodyFront - BodyBack;

            b.Box(At(0f, 0.31f, BodyCentreZ), new Vector3(0.72f, 0.06f, 1.12f), PaletteSwatch.Charcoal, 0.02f);
            MeshRange tub = RoundedSlab(b, (TubBottom + TubTop) * 0.5f, 2f * BodyHalfWidth, length,
                TubTop - TubBottom, BodyCorner, PaletteSwatch.Enamel);
            Bevel(b, tub, 0.05f, false, true);
            MeshRange lid = RoundedSlab(b, 0.87f, 2f * BodyHalfWidth + 0.06f, length + 0.06f, 0.06f,
                BodyCorner + 0.03f, PaletteSwatch.Enamel);
            Bevel(b, lid, 0.015f, true, true);
            MeshRange cushion = RoundedSlab(b, 0.925f, 2f * BodyHalfWidth - 0.04f, length - 0.04f, 0.05f,
                BodyCorner - 0.01f, PaletteSwatch.Enamel);
            Bevel(b, cushion, 0.035f, true, false);

            Stripes(b);
            SerialOnFlank(b, 1f, 0.13f, 0.21f);
            SerialOnFlank(b, -1f, 0.21f, 0.13f);
            SerialOnBack(b);
            Patch(b);
            RadioFace(b);
            Rear(b);
            LidRivets(b);
            return b;
        }

        public static LowPolyMeshBuilder Bogie()
        {
            var b = new LowPolyMeshBuilder(300);
            Vector3 hinge = RoverModelBuilder.BogieHinge;
            Vector3 front = AxleLocal(RoverModelBuilder.WheelBase);
            Vector3 middle = AxleLocal(0f);
            Vector3 rear = AxleLocal(-RoverModelBuilder.WheelBase);
            var pivot = new Vector3(0f, 0.6f - hinge.y, -0.4f - hinge.z);
            var armSize = new Vector2(0.04f, 0.055f);

            RecipeKit.Strut(b, Vector3.zero, front, armSize, PaletteSwatch.Charcoal);
            RecipeKit.Strut(b, Vector3.zero, pivot, armSize, PaletteSwatch.Charcoal);
            RecipeKit.Strut(b, pivot, middle, armSize, PaletteSwatch.Charcoal);
            RecipeKit.Strut(b, pivot, rear, armSize, PaletteSwatch.Charcoal);

            b.Prism(At(new Vector3(-0.005f, 0f, 0f), AlongX), 0.05f, 0.07f, 10, PaletteSwatch.Metal);
            b.Prism(At(pivot, AlongX), 0.04f, 0.06f, 8, PaletteSwatch.Metal);
            float inner = RoverModelBuilder.WheelTrack - hinge.x;
            foreach (Vector3 axle in new[] { front, middle, rear })
            {
                b.Prism(At(new Vector3(inner * 0.5f, axle.y, axle.z), AlongX), 0.03f, inner + 0.03f, 8,
                    PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>
        /// Right-side wheel (hub on +X): a charcoal tyre with six soft rounded lugs, a big cream hub disc with a
        /// metal cap and one warm valve dot so the spin reads.
        /// </summary>
        public static LowPolyMeshBuilder Wheel()
        {
            var b = new LowPolyMeshBuilder(300);
            Tyre(b, 12, 6, PaletteSwatch.Charcoal, PaletteSwatch.Charcoal);
            b.Prism(At(new Vector3(WheelHalfWidth + 0.012f, 0f, 0f), AlongX), 0.22f, 0.03f, 12, PaletteSwatch.Cream);
            b.Frustum(At(new Vector3(WheelHalfWidth + 0.045f, 0f, 0f), AlongX), 0.09f, 0.06f, 0.036f, 8,
                PaletteSwatch.Metal);
            b.Icosphere(At(WheelHalfWidth + 0.03f, 0.16f, 0f), 0.02f, 0, PaletteSwatch.WarmAccent);
            b.Prism(At(new Vector3(-WheelHalfWidth - 0.008f, 0f, 0f), AlongX), 0.16f, 0.02f, 10, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The mismatched spare from another machine, clearly patched on: dark rubber like the others but with a
        /// busier tread of eight small lugs, two wraps of faded tape round the tyre with a loose end, and a sage hub
        /// held by four bolts (VISION ruling 13: wear must read as intentional at first glance).
        /// </summary>
        public static LowPolyMeshBuilder SpareWheel()
        {
            var b = new LowPolyMeshBuilder(400);
            Tyre(b, 10, 8, PaletteSwatch.Charcoal, PaletteSwatch.Charcoal);
            for (int wrap = -1; wrap <= 1; wrap += 2)
            {
                b.Prism(At(new Vector3(wrap * 0.045f, 0f, 0f), new Vector3(0f, wrap * 7f, -90f)), TyreRadius + 0.006f,
                    0.032f, 10, PaletteSwatch.FadedPaint, false);
            }

            b.Box(At(new Vector3(WheelHalfWidth + 0.003f, -0.2f, -0.21f), new Vector3(-35f, 0f, 0f)),
                new Vector3(0.006f, 0.034f, 0.09f), PaletteSwatch.FadedPaint);
            b.Prism(At(new Vector3(WheelHalfWidth + 0.012f, 0f, 0f), AlongX), 0.24f, 0.03f, 10, PaletteSwatch.Sage);
            for (int i = 0; i < 4; i++)
            {
                float radians = (i * 90f + 45f) * Mathf.Deg2Rad;
                var at = new Vector3(WheelHalfWidth + 0.028f, Mathf.Cos(radians) * 0.15f, Mathf.Sin(radians) * 0.15f);
                b.Icosphere(At(at), 0.016f, 0, PaletteSwatch.Charcoal);
            }

            b.Frustum(At(new Vector3(WheelHalfWidth + 0.042f, 0f, 0f), AlongX), 0.075f, 0.05f, 0.03f, 6,
                PaletteSwatch.Charcoal);
            b.Prism(At(new Vector3(-WheelHalfWidth - 0.008f, 0f, 0f), AlongX), 0.16f, 0.02f, 10, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// Collar, a ribbed bellows boot, a slim forward-leaning pole with a two-cable bundle clamped along its back
        /// over a thin cable-guide fin, and the head yoke. The fin is edge-on (slim) from behind but casts a solid
        /// band under a low side light, so the neck's shadow still ties the head's shadow to the body's while the
        /// silhouette keeps its tired desk-lamp posture.
        /// </summary>
        public static LowPolyMeshBuilder Neck()
        {
            var b = new LowPolyMeshBuilder(400);
            b.Prism(At(0f, 0.02f, 0f), 0.085f, 0.04f, 8, PaletteSwatch.Charcoal);
            for (int i = 0; i < 5; i++)
            {
                float radius = i % 2 == 0 ? 0.062f : 0.05f;
                b.Prism(At(0f, 0.052f + i * 0.024f, 0f), radius - i * 0.002f, 0.024f, 8, PaletteSwatch.Charcoal);
            }

            var poleBase = new Vector3(0f, 0.03f, 0f);
            Vector3 poleTop = HeadHinge - new Vector3(0f, 0.03f, 0f);
            RecipeKit.Rod(b, poleBase, poleTop, 0.038f, 8, PaletteSwatch.Metal);
            Vector3 lean = poleTop - poleBase;
            float leanDegrees = Mathf.Atan2(lean.z, lean.y) * Mathf.Rad2Deg;
            Matrix4x4 fin = At((poleBase + poleTop) * 0.5f, new Vector3(leanDegrees, 0f, 0f)) * At(0f, 0.01f, -0.05f);
            b.Box(fin, new Vector3(0.014f, lean.magnitude - 0.06f, 0.08f), PaletteSwatch.Charcoal);

            Vector3 into = HeadHinge + new Vector3(0f, 0.04f, -0.12f);
            Cable(b, 0.026f, -0.06f, -0.095f, into + new Vector3(0.026f, 0f, 0f), PaletteSwatch.WarmAccent);
            Cable(b, -0.024f, -0.055f, -0.085f, into + new Vector3(-0.024f, -0.01f, 0.01f), PaletteSwatch.Charcoal);
            foreach (float t in new[] { 0.38f, 0.7f })
            {
                Vector3 onPole = Vector3.Lerp(poleBase, poleTop, t);
                b.Box(At(onPole + new Vector3(0f, 0f, -0.038f)), new Vector3(0.11f, 0.022f, 0.1f),
                    PaletteSwatch.Metal, 0.008f);
            }

            b.Box(At(HeadHinge - new Vector3(0f, 0.025f, 0f)), new Vector3(0.14f, 0.04f, 0.06f),
                PaletteSwatch.Charcoal, 0.01f);
            b.Prism(At(HeadHinge, AlongX), 0.032f, 0.18f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// One cable of the neck bundle: up the back of the pole at <paramref name="x"/>, sagging back to
        /// <paramref name="sagZ"/> halfway, then over into the back of the head at <paramref name="end"/>.
        /// </summary>
        private static void Cable(LowPolyMeshBuilder b, float x, float startZ, float sagZ, Vector3 end,
            PaletteSwatch swatch)
        {
            var start = new Vector3(x, 0.05f, startZ);
            var low = new Vector3(x, 0.13f, sagZ);
            var high = new Vector3(x, 0.22f, sagZ + 0.02f);
            RecipeKit.Rod(b, start, low, 0.017f, 6, swatch);
            RecipeKit.Rod(b, low, high, 0.017f, 6, swatch);
            RecipeKit.Rod(b, high, end, 0.017f, 6, swatch);
            b.Icosphere(At(low), 0.018f, 0, swatch);
            b.Icosphere(At(high), 0.018f, 0, swatch);
        }

        /// <summary>
        /// Big rounded sensor head wearing a hood: a shell a little wider than the head over its upper half (the
        /// hooded silhouette reads from behind) that rolls forward into a heavy brow over the lens on the eye's own
        /// axis (it shades the top of the eye even when open; the eyelid slides out from under it). The back is a
        /// vented camera-back plate with a little warm tag.
        /// </summary>
        public static LowPolyMeshBuilder Head()
        {
            var b = new LowPolyMeshBuilder(500);
            b.Box(At(0f, 0.2f, 0f), new Vector3(0.54f, 0.4f, 0.42f), PaletteSwatch.Enamel, 0.13f);
            MeshRange hood = b.Box(At(0f, 0.3f, -0.01f), new Vector3(0.6f, 0.22f, 0.46f), PaletteSwatch.Enamel, 0.09f);
            b.RepaintFacing(hood, Vector3.down, 0.9f, PaletteSwatch.Charcoal);
            MeshRange brow = Brow(b);
            b.RepaintFacing(brow, Vector3.down, 0.25f, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * 0.275f, 0.19f, -0.03f), AlongX), 0.055f, 0.035f, 8,
                    PaletteSwatch.Charcoal);
            }

            b.Box(At(0f, 0.19f, -0.215f), new Vector3(0.3f, 0.2f, 0.04f), PaletteSwatch.Metal, 0.012f);
            for (int i = 0; i < 3; i++)
            {
                b.Box(At(0f, 0.15f + i * 0.04f, -0.238f), new Vector3(0.2f, 0.014f, 0.012f), PaletteSwatch.Charcoal);
            }

            b.Box(At(-0.16f, 0.335f, -0.205f), new Vector3(0.09f, 0.04f, 0.02f), PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>The big round lens: dark barrel rim, metal bezel, glowing amber dome.</summary>
        public static LowPolyMeshBuilder Eye()
        {
            var b = new LowPolyMeshBuilder(260);
            b.Prism(At(new Vector3(0f, 0f, -0.035f), AlongZ), 0.168f, 0.09f, 16, PaletteSwatch.Charcoal);
            b.Torus(At(new Vector3(0f, 0f, 0.013f), AlongZ), 0.162f, 0.013f, 16, 4, PaletteSwatch.Metal);
            b.Lathe(At(Vector3.zero, AlongZ), LensProfile, 16, PaletteSwatch.WarmLamp);
            return b;
        }

        /// <summary>
        /// Curved shutter around the eye's X axis. At rotation 0 it rests above the lens, tucked under the hood;
        /// turning it about +X swings it down over the lens, its charcoal lash edge leading.
        /// </summary>
        public static LowPolyMeshBuilder Eyelid()
        {
            var b = new LowPolyMeshBuilder(120);
            ShellArc(b, Matrix4x4.identity, 48f, 58f, LidInner, LidOuter, LidWidth, PaletteSwatch.Charcoal);
            ShellArc(b, Matrix4x4.identity, 58f, 165f, LidInner, LidOuter, LidWidth, PaletteSwatch.Enamel);
            return b;
        }

        /// <summary>
        /// Single folded solar panel hanging a little over the left flank: a metal grid over a charcoal backing
        /// holding 2 x 3 recessed violet cells; the rear-right cell is missing, so the dark backing shows through.
        /// </summary>
        public static LowPolyMeshBuilder SolarWing()
        {
            const float bar = WingBar;
            const float height = 0.022f;
            const float halfWidth = WingHalfWidth;
            const float length = WingLength;
            var b = new LowPolyMeshBuilder(280);
            b.Prism(At(Vector3.zero, AlongX), 0.016f, 2f * halfWidth + 0.02f, 8, PaletteSwatch.Charcoal);
            b.Box(At(0f, -0.007f, -length * 0.5f), new Vector3(2f * halfWidth - 0.02f, 0.008f, length - 0.02f),
                PaletteSwatch.Charcoal);
            b.Box(At(-halfWidth + bar * 0.5f, 0f, -length * 0.5f), new Vector3(bar, height, length),
                PaletteSwatch.Metal, 0.004f);
            b.Box(At(halfWidth - bar * 0.5f, 0f, -length * 0.5f), new Vector3(bar, height, length),
                PaletteSwatch.Metal, 0.004f);
            b.Box(At(0f, 0f, -length * 0.5f), new Vector3(bar, height, length - 0.02f), PaletteSwatch.Metal, 0.004f);
            float cellLength = WingCell.y;
            for (int i = 0; i <= 3; i++)
            {
                float z = -bar * 0.5f - i * (cellLength + bar);
                b.Box(At(0f, 0f, z), new Vector3(2f * halfWidth, height, bar), PaletteSwatch.Metal, 0.004f);
            }

            float cellWidth = WingCell.x;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 2; column++)
                {
                    if (row == MissingRow && column == MissingColumn)
                    {
                        continue;
                    }

                    b.Box(At(WingCellCentre(row, column)), new Vector3(cellWidth, CellThickness, cellLength),
                        PaletteSwatch.SkyHorizon);
                }
            }

            return b;
        }

        /// <summary>Base mount and a whip with a tired kink, ending under <see cref="AntennaTipPosition"/>.</summary>
        public static LowPolyMeshBuilder Antenna()
        {
            var b = new LowPolyMeshBuilder(100);
            Vector3 kink = AntennaKink;
            b.Prism(At(0f, 0.02f, 0f), 0.032f, 0.04f, 8, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, new Vector3(0f, 0.04f, 0f), kink, 0.0085f, 5, PaletteSwatch.Metal);
            RecipeKit.Rod(b, kink, AntennaTipPosition, 0.0075f, 5, PaletteSwatch.Metal);
            b.Icosphere(At(kink), 0.012f, 0, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>Small soft glowing bulb; origin = the tip of the whip.</summary>
        public static LowPolyMeshBuilder AntennaTip()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, 0.024f, 1, PaletteSwatch.PilotLight);
            return b;
        }

        /// <summary>
        /// The brow, in head space: a solid forehead around the eye's axis whose outer surface rolls forward from
        /// inside the head top down to just above the lens (tapering so it sinks into the head behind), extruded
        /// across the head. The resting eyelid hides inside it, like a shutter in its slot.
        /// </summary>
        private static MeshRange Brow(LowPolyMeshBuilder b)
        {
            const int steps = 7;
            const float lowDegrees = 52f;
            const float highDegrees = 160f;
            const float backOuter = 0.2f;
            var polygon = new Vector2[steps + 3];
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float angle = Mathf.Lerp(lowDegrees, highDegrees, t) * Mathf.Deg2Rad;
                float radius = Mathf.Lerp(BrowOuter, backOuter, t);
                polygon[i] = new Vector2(-radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
            }

            polygon[steps + 1] = RecipeKit.Polar(highDegrees, 0.12f);
            polygon[steps + 2] = RecipeKit.Polar(lowDegrees + 8f, BrowInner);
            return b.Extrude(At(EyeCentre) * Matrix4x4.Rotate(Rotation(new Vector3(0f, 90f, 0f))), polygon, 0.62f,
                PaletteSwatch.Enamel);
        }

        /// <summary>
        /// Plan-rounded slab (a rounded rectangle extruded vertically) centred at (0, <paramref name="centreY"/>,
        /// body centre).
        /// </summary>
        private static MeshRange RoundedSlab(LowPolyMeshBuilder b, float centreY, float width, float length,
            float height, float corner, PaletteSwatch swatch)
        {
            const int arcSteps = 3;
            var outline = new Vector2[4 * (arcSteps + 1)];
            float hx = width * 0.5f - corner;
            float hz = length * 0.5f - corner;
            int index = 0;
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                float cx = quadrant == 0 || quadrant == 3 ? hx : -hx;
                float cz = quadrant < 2 ? hz : -hz;
                for (int step = 0; step <= arcSteps; step++)
                {
                    float angle = (quadrant * 90f + step * 90f / arcSteps) * Mathf.Deg2Rad;
                    outline[index++] = new Vector2(cx + corner * Mathf.Cos(angle), -(cz + corner * Mathf.Sin(angle)));
                }
            }

            return b.Extrude(At(new Vector3(0f, centreY, BodyCentreZ), new Vector3(-90f, 0f, 0f)), outline, height,
                swatch);
        }

        /// <summary>
        /// Softens a slab's top and/or bottom rim by shaving it with planes along the eight horizontal directions
        /// tilted 45 degrees, <paramref name="size"/> deep. The range must be the builder's last.
        /// </summary>
        private static MeshRange Bevel(LowPolyMeshBuilder b, MeshRange range, float size, bool top, bool bottom)
        {
            for (int vertical = -1; vertical <= 1; vertical += 2)
            {
                if ((vertical > 0 && !top) || (vertical < 0 && !bottom))
                {
                    continue;
                }

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 normal = new Vector3(Mathf.Sin(angle), vertical, Mathf.Cos(angle)).normalized;
                    range = b.Shave(range, normal, b.Support(range, normal) - size);
                }
            }

            return range;
        }

        /// <summary>
        /// Faceted tyre with soft hexagonal lugs whose tips reach exactly the contract wheel radius; the first lug
        /// points straight down, so the resting wheel touches the ground at y = 0.
        /// </summary>
        private static void Tyre(LowPolyMeshBuilder b, int sides, int lugs, PaletteSwatch tyre, PaletteSwatch lug)
        {
            b.Prism(At(Vector3.zero, AlongX), TyreRadius, 2f * WheelHalfWidth, sides, tyre);
            for (int i = 0; i < lugs; i++)
            {
                float radians = Mathf.PI * 0.5f + i * 2f * Mathf.PI / lugs;
                var direction = new Vector3(0f, -Mathf.Sin(radians), Mathf.Cos(radians));
                b.Prism(At(direction * (RoverModelBuilder.WheelRadius - LugRadius), new Vector3(0f, 0f, -90f)),
                    LugRadius, 2f * WheelHalfWidth - 0.02f, 6, lug);
            }
        }

        private static void Stripes(LowPolyMeshBuilder b)
        {
            float flankX = BodyHalfWidth + PaintProud;
            FlankStripe(b, flankX, -0.46f, -0.1f);
            FlankStripe(b, flankX, -0.05f, 0.3f);
            FlankStripe(b, flankX, 0.35f, 0.52f);
            FlankStripe(b, -flankX, -0.46f, 0.04f);
            FlankStripe(b, -flankX, 0.09f, 0.52f);
            FaceStripe(b, BodyFront + PaintProud, -FaceHalfSpan, 0.06f);
            FaceStripe(b, BodyFront + PaintProud, 0.11f, FaceHalfSpan);
            FaceStripe(b, BodyBack - PaintProud, -FaceHalfSpan, FaceHalfSpan);
        }

        private static void FlankStripe(LowPolyMeshBuilder b, float x, float fromZ, float toZ)
        {
            b.Box(At(x, StripeY, (fromZ + toZ) * 0.5f), new Vector3(PaintThickness, StripeHeight, toZ - fromZ),
                PaletteSwatch.WarmAccent);
        }

        private static void FaceStripe(LowPolyMeshBuilder b, float z, float fromX, float toX)
        {
            b.Box(At((fromX + toX) * 0.5f, StripeY, z), new Vector3(toX - fromX, StripeHeight, PaintThickness),
                PaletteSwatch.WarmAccent);
        }

        /// <summary>
        /// The hand-painted "07" on a flank stripe, chalky with age, reading front-to-back from outside.
        /// </summary>
        private static void SerialOnFlank(LowPolyMeshBuilder b, float side, float zeroZ, float sevenZ)
        {
            const float scale = 0.7f;
            float x = side * (BodyHalfWidth + PaintProud + PaintThickness * 0.5f + 0.002f);
            b.Torus(At(new Vector3(x, StripeY - 0.002f, zeroZ), new Vector3(0f, 0f, side * -86f),
                new Vector3(1.35f * scale, 0.25f, scale)), 0.032f, 0.013f, 10, 4, PaletteSwatch.FadedPaint);
            b.Extrude(At(new Vector3(x, StripeY + 0.002f, sevenZ), new Vector3(0f, side > 0f ? -90f : 90f, -3f),
                new Vector3(scale, scale, 1f)), Seven, 0.006f, PaletteSwatch.FadedPaint);
        }

        /// <summary>The faded "07" on the back stripe, so the chase camera always sees who this is.</summary>
        private static void SerialOnBack(LowPolyMeshBuilder b)
        {
            const float scale = 0.8f;
            float z = BodyBack - PaintProud - PaintThickness * 0.5f - 0.002f;
            b.Torus(At(new Vector3(-0.045f, StripeY - 0.002f, z), new Vector3(90f, 0f, 3f),
                new Vector3(scale, 0.25f, 1.35f * scale)), 0.032f, 0.013f, 10, 4, PaletteSwatch.FadedPaint);
            b.Extrude(At(new Vector3(0.04f, StripeY + 0.002f, z), new Vector3(0f, 0f, -3f),
                new Vector3(scale, scale, 1f)), Seven, 0.006f, PaletteSwatch.FadedPaint);
        }

        /// <summary>Sage plate riveted over the left rear flank, askew, covering a stretch of the stripe.</summary>
        private static void Patch(LowPolyMeshBuilder b)
        {
            const float x = -(BodyHalfWidth + 0.009f);
            Matrix4x4 plate = At(new Vector3(x, 0.74f, -0.3f), new Vector3(-5f, 0f, 0f));
            b.Box(plate, new Vector3(0.012f, 0.16f, 0.22f), PaletteSwatch.Sage);
            for (int corner = 0; corner < 4; corner++)
            {
                float y = corner < 2 ? 0.06f : -0.06f;
                float z = corner % 2 == 0 ? 0.09f : -0.09f;
                b.Icosphere(plate * At(-0.006f, y, z), 0.01f, 0, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>Front like an old radio: speaker slats, two knobs, a small road lamp low in the middle.</summary>
        private static void RadioFace(LowPolyMeshBuilder b)
        {
            const float face = BodyFront;
            for (int i = 0; i < 4; i++)
            {
                b.Box(At(0f, 0.585f + i * 0.026f, face + 0.002f), new Vector3(0.34f, 0.013f, 0.012f),
                    PaletteSwatch.Charcoal);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * 0.2f, LampHeight, face + 0.012f), AlongZ), 0.03f, 0.03f, 10,
                    PaletteSwatch.WarmAccent);
            }

            b.Prism(At(new Vector3(0f, LampHeight, face + 0.015f), AlongZ), 0.062f, 0.05f, 12, PaletteSwatch.Charcoal);
            b.Frustum(At(new Vector3(0f, LampHeight, face + 0.045f), AlongZ), 0.05f, 0.036f, 0.02f, 12,
                PaletteSwatch.WarmLamp);
        }

        /// <summary>Back: two soft tail lamps and a little hatch with a handle (where cargo attaches).</summary>
        private static void Rear(LowPolyMeshBuilder b)
        {
            const float face = BodyBack;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * 0.2f, 0.6f, face - 0.012f), AlongZ), 0.05f, 0.03f, 10,
                    PaletteSwatch.Charcoal);
                b.Frustum(At(new Vector3(side * 0.2f, 0.6f, face - 0.034f), AlongZ), 0.038f, 0.028f, 0.016f, 10,
                    PaletteSwatch.PilotLight);
            }

            const float hatchY = 0.48f;
            b.Box(At(0f, hatchY + 0.07f, face - 0.002f), new Vector3(0.26f, 0.012f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(0f, hatchY - 0.07f, face - 0.002f), new Vector3(0.26f, 0.012f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(-0.124f, hatchY, face - 0.002f), new Vector3(0.012f, 0.14f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(0.124f, hatchY, face - 0.002f), new Vector3(0.012f, 0.14f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(0f, hatchY - 0.02f, face - 0.012f), new Vector3(0.08f, 0.016f, 0.02f), PaletteSwatch.Charcoal,
                0.004f);
        }

        private static void LidRivets(LowPolyMeshBuilder b)
        {
            const float y = 0.87f;
            float x = BodyHalfWidth + 0.03f;
            for (int i = 0; i < 8; i++)
            {
                float z = -0.4f + i * 0.12f;
                b.Icosphere(At(x, y, z), 0.01f, 0, PaletteSwatch.Enamel);
                b.Icosphere(At(-x, y, z), 0.01f, 0, PaletteSwatch.Enamel);
            }
        }

        /// <summary>
        /// Centre of the wing cell at <paramref name="row"/> (0 = by the hinge) and column (0 = left).
        /// </summary>
        public static Vector3 WingCellCentre(int row, int column)
        {
            float x = (column == 0 ? -1f : 1f) * (WingBar * 0.5f + WingCell.x * 0.5f);
            float z = -WingBar - WingCell.y * 0.5f - row * (WingCell.y + WingBar);
            return new Vector3(x, 0f, z);
        }

        private static Vector3 AxleLocal(float wheelZ)
        {
            Vector3 hinge = RoverModelBuilder.BogieHinge;
            return new Vector3(0f, RoverModelBuilder.WheelRadius - hinge.y, wheelZ - hinge.z);
        }

        /// <summary>
        /// Curved plate: an annular sector around the local X axis between two angles measured from +Z towards +Y,
        /// extruded along X to <paramref name="width"/>.
        /// </summary>
        private static MeshRange ShellArc(LowPolyMeshBuilder b, Matrix4x4 placement, float fromDegrees,
            float toDegrees, float inner, float outer, float width, PaletteSwatch swatch)
        {
            const int steps = 7;
            var polygon = new Vector2[2 * (steps + 1)];
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, (float)i / steps) * Mathf.Deg2Rad;
                polygon[i] = new Vector2(-outer * Mathf.Cos(angle), outer * Mathf.Sin(angle));
                polygon[2 * steps + 1 - i] = new Vector2(-inner * Mathf.Cos(angle), inner * Mathf.Sin(angle));
            }

            return b.Extrude(placement * Matrix4x4.Rotate(Rotation(new Vector3(0f, 90f, 0f))), polygon, width, swatch);
        }
    }
}
