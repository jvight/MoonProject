using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The small salvage models: the three material bundles 07 carries home (one silhouette and colour each: a
    /// strapped stack of grey plates, of copper coils and of violet cells). Built around the origin; the builder pivots
    /// them at the centre of mass.
    /// </summary>
    internal static class SalvageBitMeshes
    {
        // Every bundle is a stack strapped twice (docs/design/feel-pass.md §3.1, "a strapped stack in the material's
        // colour"): the straps' half-spacing, their width and how far they stand proud of the stack.
        private const float StrapX = 0.09f;
        private const float StrapWidth = 0.035f;
        private const float StrapProud = 0.015f;
        private const float StrapThickness = 0.008f;

        /// <summary>Metal: three bent plates stacked and strapped, flat and rectangular.</summary>
        public static LowPolyMeshBuilder MetalBundle()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Box(At(new Vector3(0f, 0.02f, 0f), new Vector3(0f, 4f, 2f)), new Vector3(0.34f, 0.035f, 0.22f),
                PaletteSwatch.Metal, 0.008f);
            b.Box(At(new Vector3(0.01f, 0.06f, 0.005f), new Vector3(3f, -6f, -2f)), new Vector3(0.32f, 0.035f, 0.2f),
                PaletteSwatch.Rust, 0.008f);
            b.Box(At(new Vector3(-0.005f, 0.1f, -0.005f), new Vector3(-2f, 9f, 3f)), new Vector3(0.3f, 0.035f, 0.21f),
                PaletteSwatch.WornPanel, 0.008f);
            Straps(b, 0.12f, 0.22f);
            return b;
        }

        /// <summary>Wiring: three flat copper coils stacked and strapped, a sage tag on the top strap.</summary>
        public static LowPolyMeshBuilder WiringBundle()
        {
            var b = new LowPolyMeshBuilder(400);
            for (int i = 0; i < 3; i++)
            {
                b.Torus(At(new Vector3(0.004f * i, 0.025f + i * 0.042f, 0f), new Vector3(0f, i * 25f, 0f)), 0.13f,
                    0.022f, 12, 4, PaletteSwatch.WarmAccent);
            }

            Straps(b, 0.15f, 0.31f);
            b.Box(At(new Vector3(StrapX, 0.16f, 0.05f), new Vector3(0f, 20f, 0f)), new Vector3(0.05f, 0.012f, 0.04f),
                PaletteSwatch.Sage);
            return b;
        }

        /// <summary>Optics: four violet cells on thin metal rails, stacked and strapped, one cracked.</summary>
        public static LowPolyMeshBuilder OpticsBundle()
        {
            var b = new LowPolyMeshBuilder(300);
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3(0.004f * (i % 2), 0.012f + i * 0.026f, 0f);
                Matrix4x4 tile = At(at, new Vector3(0f, i * 3f, 0f));
                b.Box(tile, new Vector3(0.28f, 0.016f, 0.2f), PaletteSwatch.SkyHorizon);
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Box(tile * At(side * 0.146f, 0f, 0f), new Vector3(0.012f, 0.02f, 0.21f), PaletteSwatch.Metal);
                }
            }

            b.Box(At(new Vector3(0.03f, 0.098f, 0.02f), new Vector3(0f, 35f, 0f)), new Vector3(0.008f, 0.004f, 0.14f),
                PaletteSwatch.Charcoal);
            Straps(b, 0.12f, 0.23f);
            return b;
        }

        /// <summary>
        /// Two charcoal straps round a stack <paramref name="height"/> tall, <paramref name="depth"/> deep.
        /// </summary>
        private static void Straps(LowPolyMeshBuilder b, float height, float depth)
        {
            float top = height + StrapProud * 0.5f;
            float half = (depth + StrapProud) * 0.5f;
            foreach (float x in new[] { -StrapX, StrapX })
            {
                foreach (float y in new[] { -StrapProud * 0.5f, top })
                {
                    b.Box(At(x, y, 0f), new Vector3(StrapWidth, StrapThickness, depth + StrapProud),
                        PaletteSwatch.Charcoal);
                }

                foreach (float z in new[] { -half, half })
                {
                    b.Box(At(x, height * 0.5f, z), new Vector3(StrapWidth, height + StrapProud, StrapThickness),
                        PaletteSwatch.Charcoal);
                }
            }
        }
    }
}
