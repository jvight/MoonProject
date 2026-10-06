using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's museum: a two-tier cabinet built from salvaged lander panels under a striped, scalloped market-stall
    /// awning, three bays per tier against a violet backdrop, each with a little round plinth and a honey name
    /// plaque, lit warm from under every shelf. Origin on the
    /// ground at the centre of its base, +Z = the open front.
    /// </summary>
    internal static class MuseumShelfMeshes
    {
        public const int SlotCount = 6;
        public const float Width = 4.2f;

        private const float Depth = 1.0f;
        private const float LowerShelfTop = 0.34f;
        private const float UpperShelfTop = 1.74f;
        private const float Top = 3.1f;
        private const float PlankThickness = 0.08f;
        private const float BayPitch = 1.37f;
        private const float PlinthHeight = 0.04f;
        private const float SlotZ = 0.04f;

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
            b.Box(At(0f, 0.07f, 0f), new Vector3(Width + 0.1f, 0.14f, Depth + 0.06f), PaletteSwatch.Metal, 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (halfWidth + 0.03f), (0.14f + Top) * 0.5f, 0f), new Vector3(0.06f, Top - 0.14f, Depth),
                    PaletteSwatch.Enamel, 0.02f);
                b.Box(At(side * (halfWidth + 0.065f), 1.2f, 0.1f), new Vector3(0.012f, 0.1f, 0.7f),
                    PaletteSwatch.WarmAccent);
            }

            b.Box(At(0f, (0.14f + Top) * 0.5f, -Depth * 0.5f + 0.02f), new Vector3(Width, Top - 0.14f, 0.04f),
                PaletteSwatch.SkyHorizon);
            Plank(b, LowerShelfTop);
            Plank(b, UpperShelfTop);
            Plank(b, Top);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * BayPitch * 0.5f;
                b.Box(At(x, (LowerShelfTop + Top) * 0.5f, Depth * 0.5f - 0.06f),
                    new Vector3(0.07f, Top - LowerShelfTop, 0.07f), PaletteSwatch.Metal, 0.01f);
            }

            Awning(b);

            for (int i = 0; i < SlotCount; i++)
            {
                Vector3 slot = SlotPosition(i);
                b.Prism(At(slot.x, slot.y - PlinthHeight * 0.5f, slot.z), 0.26f, PlinthHeight, 10,
                    PaletteSwatch.Charcoal);
                b.Box(At(slot.x, slot.y - PlinthHeight - PlankThickness * 0.5f, Depth * 0.5f + 0.012f),
                    new Vector3(0.3f, 0.055f, 0.02f), PaletteSwatch.Honey);
            }

            return b;
        }

        /// <summary>Warm strips under the front edge of the upper shelf and the top, lighting the tier below.</summary>
        public static LowPolyMeshBuilder Lights()
        {
            var b = new LowPolyMeshBuilder(60);
            foreach (float shelfTop in new[] { UpperShelfTop, Top })
            {
                b.Box(At(0f, shelfTop - PlankThickness - 0.015f, Depth * 0.5f - 0.12f),
                    new Vector3(Width - 0.3f, 0.025f, 0.05f), PaletteSwatch.WarmLamp);
            }

            return b;
        }

        /// <summary>Honey and enamel stripes sloping to the front, finished with a row of scallops.</summary>
        private static void Awning(LowPolyMeshBuilder b)
        {
            const int stripes = 9;
            const float overhang = 0.4f;
            float width = Width + overhang;
            float stripeWidth = width / stripes;
            float depth = Depth + overhang;
            Matrix4x4 awning = At(new Vector3(0f, Top + 0.13f, 0.12f), new Vector3(10f, 0f, 0f));
            Vector3 awningUp = awning.MultiplyVector(Vector3.up);
            for (int i = 0; i < stripes; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * stripeWidth;
                PaletteSwatch swatch = i % 2 == 0 ? PaletteSwatch.Honey : PaletteSwatch.Enamel;
                b.Box(awning * At(x, 0f, 0f), new Vector3(stripeWidth, 0.07f, depth), swatch);
                Vector3 hem = awning.MultiplyPoint3x4(new Vector3(x, -0.03f, depth * 0.5f));
                MeshRange scallop = b.Prism(awning * At(new Vector3(x, -0.03f, depth * 0.5f), AlongZ),
                    stripeWidth * 0.46f, 0.03f, 10, swatch);
                b.Shave(scallop, awningUp, Vector3.Dot(awningUp, hem));
            }

            b.Box(awning * At(0f, -0.005f, depth * 0.5f), new Vector3(width, 0.06f, 0.035f), PaletteSwatch.WarmAccent);
        }

        private static void Plank(LowPolyMeshBuilder b, float top)
        {
            b.Box(At(0f, top - PlankThickness * 0.5f, 0f), new Vector3(Width, PlankThickness, Depth),
                PaletteSwatch.Enamel, 0.02f);
        }
    }
}
