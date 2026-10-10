using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's museum: a two-tier cabinet built from salvaged lander panels under a striped, scalloped market-stall
    /// awning, three bays per tier against a violet backdrop, each with a little round plinth and a honey name
    /// plaque, lit warm from under every shelf. Sized for the crew and for true-size relics (VISION ruling 13): waist
    /// and knee high tiers, its awning just over a person's head, each bay roomy for a relic of up to half a metre.
    /// Origin on the ground at the centre of its base, +Z = the open front.
    /// </summary>
    internal static class MuseumShelfMeshes
    {
        public const int SlotCount = 6;
        public const float Width = 2.7f;

        private const float Depth = 0.6f;
        private const float BaseHeight = 0.12f;
        private const float LowerShelfTop = 0.3f;
        private const float UpperShelfTop = 1f;
        private const float Top = 1.75f;
        private const float PlankThickness = 0.05f;
        private const float BayPitch = 0.86f;
        private const float PlinthHeight = 0.03f;
        private const float PlinthRadius = 0.18f;
        private const float SlotZ = 0.03f;

        // The awning stripe that sags.
        private const int SaggingStripe = 6;

        /// <summary>Where relic <paramref name="index"/> rests: its plinth top (0-2 lower tier, 3-5 upper).</summary>
        public static Vector3 SlotPosition(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Shelf slots are 0..5.");
            }

            float shelfTop = index < 3 ? LowerShelfTop : UpperShelfTop;
            return new Vector3((index % 3 - 1) * BayPitch, shelfTop + PlinthHeight, SlotZ);
        }

        public static LowPolyMeshBuilder Cabinet()
        {
            var b = new LowPolyMeshBuilder(1200);
            float halfWidth = Width * 0.5f;
            b.Box(At(0f, BaseHeight * 0.5f, 0f), new Vector3(Width + 0.1f, BaseHeight, Depth + 0.06f),
                PaletteSwatch.Metal, 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (halfWidth + 0.03f), (BaseHeight + Top) * 0.5f, 0f),
                    new Vector3(0.06f, Top - BaseHeight, Depth), PaletteSwatch.Enamel, 0.02f);
                b.Box(At(side * (halfWidth + 0.065f), UpperShelfTop - 0.25f, 0.06f), new Vector3(0.012f, 0.08f, 0.42f),
                    PaletteSwatch.WarmAccent);
            }

            b.Box(At(0f, (BaseHeight + Top) * 0.5f, -Depth * 0.5f + 0.02f), new Vector3(Width, Top - BaseHeight, 0.04f),
                PaletteSwatch.SkyHorizon);
            Plank(b, LowerShelfTop);
            Plank(b, UpperShelfTop);
            Plank(b, Top);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * BayPitch * 0.5f;
                b.Box(At(x, (LowerShelfTop + Top) * 0.5f, Depth * 0.5f - 0.05f),
                    new Vector3(0.05f, Top - LowerShelfTop, 0.05f), PaletteSwatch.Metal, 0.01f);
            }

            Awning(b);

            for (int i = 0; i < SlotCount; i++)
            {
                Vector3 slot = SlotPosition(i);
                b.Prism(At(slot.x, slot.y - PlinthHeight * 0.5f, slot.z), PlinthRadius, PlinthHeight, 10,
                    PaletteSwatch.Charcoal);
                b.Box(At(slot.x, slot.y - PlinthHeight - PlankThickness * 0.5f, Depth * 0.5f + 0.012f),
                    new Vector3(0.22f, 0.04f, 0.02f), PaletteSwatch.Honey);
            }

            return b;
        }

        /// <summary>
        /// The shelf's rust: streaks down the side panels from the shelf bolts, rust at the posts' feet.
        /// </summary>
        public static LowPolyMeshBuilder Rust()
        {
            var b = new LowPolyMeshBuilder(300);
            float halfWidth = Width * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 normal = Vector3.right * side;
                Matrix4x4 panel = SiteKit.Face(normal * (halfWidth + 0.06f + Weathering.RustLift), normal, Vector3.up);
                foreach (float shelfTop in new[] { UpperShelfTop, Top })
                {
                    SiteKit.RustSeam(b, panel, -0.3f, 0.3f, shelfTop - PlankThickness * 0.5f, 0.35f, 2f,
                        Mathf.RoundToInt(shelfTop * 100f) + side);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Weathering.Collar(b, At(side * BayPitch * 0.5f, LowerShelfTop + 0.04f, Depth * 0.5f - 0.05f), 0.042f,
                    0.06f);
            }

            return b;
        }

        /// <summary>Dust drifted against the shelf's base.</summary>
        public static LowPolyMeshBuilder Drifts()
        {
            var b = new LowPolyMeshBuilder(500);
            SiteKit.Drift(b, new Vector3(-Width * 0.3f, 0f, Depth * 0.5f + 0.12f), Width * 0.45f, 0.4f, 0.09f, 90f, 93);
            SiteKit.Drift(b, new Vector3(Width * 0.5f + 0.16f, 0f, 0f), Depth * 1.5f, 0.36f, 0.12f, 0f, 94);

            // A name plaque that fell off long ago, face down in the dust.
            b.Box(At(new Vector3(0.55f, 0.01f, Depth * 0.5f + 0.45f), new Vector3(0f, 22f, 0f)),
                new Vector3(0.22f, 0.02f, 0.05f), PaletteSwatch.Honey);
            return b;
        }

        /// <summary>Warm strips under the front edge of the upper shelf and the top, lighting the tier below.</summary>
        public static LowPolyMeshBuilder Lights()
        {
            var b = new LowPolyMeshBuilder(60);
            foreach (float shelfTop in new[] { UpperShelfTop, Top })
            {
                b.Box(At(0f, shelfTop - PlankThickness - 0.012f, Depth * 0.5f - 0.08f),
                    new Vector3(Width - 0.2f, 0.02f, 0.04f), PaletteSwatch.WarmLamp);
            }

            return b;
        }

        /// <summary>Honey and enamel stripes sloping to the front, finished with a row of scallops.</summary>
        private static void Awning(LowPolyMeshBuilder b)
        {
            const int stripes = 9;
            const float overhang = 0.25f;
            float width = Width + overhang;
            float stripeWidth = width / stripes;
            float depth = Depth + overhang;
            Matrix4x4 awning = At(new Vector3(0f, Top + 0.1f, 0.08f), new Vector3(10f, 0f, 0f));
            for (int i = 0; i < stripes; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * stripeWidth;
                PaletteSwatch swatch = i % 2 == 0 ? PaletteSwatch.Honey : PaletteSwatch.Enamel;
                // One stripe's fixings rusted through at the front: it sags over the shelf's right bay.
                Matrix4x4 stripe = i == SaggingStripe
                    ? awning * At(x, 0f, -depth * 0.5f) * Matrix4x4.Rotate(Rotation(new Vector3(24f, 0f, 3f)))
                        * At(0f, 0f, depth * 0.5f)
                    : awning * At(x, 0f, 0f);
                Vector3 stripeUp = stripe.MultiplyVector(Vector3.up).normalized;
                b.Box(stripe, new Vector3(stripeWidth, 0.05f, depth), swatch);
                Vector3 hem = stripe.MultiplyPoint3x4(new Vector3(0f, -0.02f, depth * 0.5f));
                MeshRange scallop = b.Prism(stripe * At(new Vector3(0f, -0.02f, depth * 0.5f), AlongZ),
                    stripeWidth * 0.46f, 0.025f, 10, swatch);
                b.Shave(scallop, stripeUp, Vector3.Dot(stripeUp, hem));
            }

            b.Box(awning * At(0f, -0.005f, depth * 0.5f), new Vector3(width, 0.045f, 0.03f), PaletteSwatch.WarmAccent);
        }

        private static void Plank(LowPolyMeshBuilder b, float top)
        {
            b.Box(At(0f, top - PlankThickness * 0.5f, 0f), new Vector3(Width, PlankThickness, Depth),
                PaletteSwatch.Enamel, 0.02f);
        }
    }
}
