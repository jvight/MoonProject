using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// A chunky pickup-sized cassette tape, labelled alike on both faces so a spinning pickup always shows its colour:
    /// a bevelled shell, a coloured label band with a cream strip of Ro's handwriting, two cream reel hubs either side
    /// of a tape window that glows warm amber, the trapezoid head ridge with its capstan holes and a label-colour
    /// stripe along the top edge (what you see of a tape on a shelf from above). Built standing upright, centred on
    /// the origin: +X across, +Y up, the label faces +Z. Drawn at a large working size with chunky details, then
    /// scaled whole to its true size (VISION ruling 13: about twice a real cassette).
    /// </summary>
    internal static class CassetteMeshes
    {
        public const float Width = 0.18f;
        public const float Scale = Width / DrawnWidth;
        public const float Height = DrawnHeight * Scale;
        public const float Thickness = DrawnThickness * Scale;

        private const float DrawnWidth = 0.35f;
        private const float DrawnHeight = 0.22f;
        private const float DrawnThickness = 0.07f;

        public static LowPolyMeshBuilder Cassette(CassetteStyle style)
        {
            var b = new LowPolyMeshBuilder(700);
            b.Box(Matrix4x4.identity, new Vector3(DrawnWidth, DrawnHeight, DrawnThickness), style.Shell, 0.014f);
            b.Box(At(0f, DrawnHeight * 0.5f + 0.001f, 0f),
                new Vector3(DrawnWidth - 0.06f, 0.004f, DrawnThickness - 0.03f), style.Label);
            Face(b, At(0f, 0f, DrawnThickness * 0.5f), style);
            Face(b, At(Vector3.back * (DrawnThickness * 0.5f), new Vector3(0f, 180f, 0f)), style);
            b.Transform(b.RangeFrom(0), Matrix4x4.Scale(Vector3.one * Scale));
            return b;
        }

        /// <summary>One face's details on the plane of <paramref name="face"/> (+Z out of the shell).</summary>
        private static void Face(LowPolyMeshBuilder b, Matrix4x4 face, CassetteStyle style)
        {
            b.Box(face * At(0f, 0.025f, 0.002f), new Vector3(0.31f, 0.14f, 0.004f), style.Label);
            b.Box(face * At(0f, 0.072f, 0.0045f), new Vector3(0.27f, 0.034f, 0.002f), PaletteSwatch.Cream);
            float[] strokes = { 0.09f, 0.05f, 0.07f };
            float x = 0.11f;
            for (int i = 0; i < strokes.Length; i++)
            {
                b.Box(face * At(new Vector3(x - strokes[i] * 0.5f, 0.072f + (i % 2) * 0.004f, 0.006f),
                    new Vector3(0f, 0f, 4f - i * 3f)), new Vector3(strokes[i], 0.014f, 0.002f), style.Ink);
                x -= strokes[i] + 0.016f;
            }

            b.Box(face * At(0f, 0.012f, 0.0045f), new Vector3(0.21f, 0.058f, 0.002f), PaletteSwatch.Charcoal, 0.0008f);
            b.Box(face * At(0f, 0.012f, 0.006f), new Vector3(0.07f, 0.03f, 0.002f), PaletteSwatch.LampGlass);
            for (int side = -1; side <= 1; side += 2)
            {
                Matrix4x4 reel = face * At(new Vector3(side * 0.072f, 0.012f, 0.0065f), AlongZ);
                b.Prism(reel, 0.022f, 0.004f, 6, PaletteSwatch.Cream);
                b.Prism(reel * At(0f, 0.0025f, 0f), 0.011f, 0.002f, 6, PaletteSwatch.Charcoal);
            }

            Vector2[] ridge =
            {
                new Vector2(-0.115f, -0.095f), new Vector2(0.115f, -0.095f), new Vector2(0.088f, -0.055f),
                new Vector2(-0.088f, -0.055f),
            };
            b.Extrude(face * At(0f, 0f, 0.003f), ridge, 0.006f, style.Shell);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(face * At(new Vector3(side * 0.05f, -0.076f, 0.0065f), AlongZ), 0.01f, 0.002f, 6,
                    PaletteSwatch.Charcoal);
                b.Prism(face * At(new Vector3(side * 0.148f, -0.086f, 0.001f), AlongZ), 0.011f, 0.003f, 6,
                    PaletteSwatch.Metal);
            }
        }
    }
}
