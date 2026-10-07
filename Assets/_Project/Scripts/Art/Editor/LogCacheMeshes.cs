using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Ro's crew log cache: a battered sage tin box with rolled metal rims, a dented corner and scratches, its lid
    /// left ajar on the back hinge so her cream log pages show in the gap (one sheet slipping out of the front), a
    /// hand-lettered "RO" label on the front, a folded carry handle, an open hasp and dust settled on the lid. Origin
    /// on the ground at the centre of the box, +Z = the lid's front.
    /// </summary>
    internal static class LogCacheMeshes
    {
        public const float Width = 0.5f;
        public const float Depth = 0.32f;
        public const float BoxHeight = 0.19f;

        /// <summary>How far the lid stands open at the front, in degrees about its back hinge.</summary>
        public const float LidAjarDegrees = 17f;

        private const float LidHeight = 0.045f;

        public static LowPolyMeshBuilder Cache()
        {
            var b = new LowPolyMeshBuilder(900);
            MeshRange tin = b.Box(At(0f, BoxHeight * 0.5f, 0f), new Vector3(Width, BoxHeight, Depth),
                PaletteSwatch.Sage, 0.015f);
            var dent = new Vector3(-1f, 0.8f, 1f);
            var corner = new Vector3(-Width * 0.5f, BoxHeight, Depth * 0.5f);
            b.Shave(tin, dent, Vector3.Dot(dent.normalized, corner) - 0.035f);
            b.Box(At(0f, BoxHeight - 0.006f, 0f), new Vector3(Width + 0.012f, 0.012f, Depth + 0.012f),
                PaletteSwatch.Metal, 0.004f);
            b.Box(At(0f, 0.006f, 0f), new Vector3(Width + 0.008f, 0.012f, Depth + 0.008f), PaletteSwatch.Metal, 0.004f);
            b.Box(At(0f, BoxHeight - 0.04f, Depth * 0.5f + 0.004f), new Vector3(0.05f, 0.04f, 0.01f),
                PaletteSwatch.Metal, 0.003f);
            Matrix4x4 label = At(0.11f, 0.08f, Depth * 0.5f + 0.0015f);
            b.Box(label, new Vector3(0.17f, 0.095f, 0.003f), PaletteSwatch.Cream);
            Glyphs.Write(b, label * At(0f, 0f, 0.0015f), "RO", 0.065f, PaletteSwatch.WarmAccent, PaletteSwatch.Cream);
            Scratch(b, At(new Vector3(-0.12f, 0.09f, Depth * 0.5f + 0.001f), new Vector3(0f, 0f, 14f)), 0.09f);
            Scratch(b, At(new Vector3(-0.09f, 0.07f, Depth * 0.5f + 0.001f), new Vector3(0f, 0f, 9f)), 0.06f);

            b.Box(At(0f, BoxHeight + 0.004f, -0.01f), new Vector3(Width - 0.08f, 0.02f, Depth - 0.07f),
                PaletteSwatch.Cream);
            b.Box(At(new Vector3(0.07f, BoxHeight + 0.018f, 0.15f), new Vector3(14f, 12f, 0f)),
                new Vector3(0.17f, 0.004f, 0.15f), PaletteSwatch.Cream);
            b.Box(At(new Vector3(0.07f, BoxHeight + 0.0205f, 0.15f), new Vector3(14f, 12f, 0f)) *
                At(-0.02f, 0f, -0.02f), new Vector3(0.1f, 0.002f, 0.008f), PaletteSwatch.Charcoal);

            Lid(b, At(new Vector3(0f, BoxHeight, -Depth * 0.5f), new Vector3(-LidAjarDegrees, 0f, 0f)));
            return b;
        }

        /// <summary>The lid, built from its back hinge (origin) lying along +Z.</summary>
        private static void Lid(LowPolyMeshBuilder b, Matrix4x4 hinge)
        {
            float depth = Depth + 0.016f;
            b.Box(hinge * At(0f, LidHeight * 0.5f, depth * 0.5f), new Vector3(Width + 0.016f, LidHeight, depth),
                PaletteSwatch.Sage, 0.012f);
            b.Box(hinge * At(0f, 0.006f, depth * 0.5f), new Vector3(Width + 0.024f, 0.012f, depth + 0.008f),
                PaletteSwatch.Metal, 0.004f);
            b.Box(hinge * At(0f, -0.022f, depth + 0.004f), new Vector3(0.036f, 0.05f, 0.008f), PaletteSwatch.Metal,
                0.002f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(hinge * At(new Vector3(side * 0.16f, 0f, -0.004f), AlongX), 0.01f, 0.07f, 6,
                    PaletteSwatch.Charcoal);
            }

            Matrix4x4 top = hinge * At(0f, LidHeight, 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(top * At(side * 0.09f, 0.008f, 0.16f), new Vector3(0.024f, 0.016f, 0.02f), PaletteSwatch.Metal);
            }

            RecipeKit.Rod(b, top.MultiplyPoint3x4(new Vector3(-0.09f, 0.012f, 0.175f)),
                top.MultiplyPoint3x4(new Vector3(0.09f, 0.012f, 0.175f)), 0.008f, 5, PaletteSwatch.Metal);
            Scratch(b, top * At(new Vector3(-0.12f, 0.001f, 0.24f), new Vector3(-90f, 0f, 25f)), 0.08f);

            for (int i = 0; i < 3; i++)
            {
                var spot = new Vector3(-0.16f + i * 0.05f, 0.002f, 0.06f + i * 0.07f);
                b.Icosphere(top * At(spot, Vector3.zero, new Vector3(1f, 0.25f, 1f)), 0.05f - i * 0.01f, 1,
                    PaletteSwatch.DustLight);
            }
        }

        private static void Scratch(LowPolyMeshBuilder b, Matrix4x4 at, float length)
        {
            b.Box(at, new Vector3(length, 0.005f, 0.002f), PaletteSwatch.Metal);
        }
    }
}
