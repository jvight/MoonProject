using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The small salvage models: the three material bundles 07 carries home (one silhouette and colour each: a
    /// strapped stack of grey plates, a round copper coil, a fan of violet cells). Built around the origin; the builder
    /// pivots them at the centre of mass.
    /// </summary>
    internal static class SalvageBitMeshes
    {
        /// <summary>Metal: three bent plates stacked and strapped, flat and rectangular.</summary>
        public static LowPolyMeshBuilder MetalBundle()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Box(At(new Vector3(0f, 0.02f, 0f), new Vector3(0f, 4f, 2f)), new Vector3(0.34f, 0.035f, 0.22f),
                PaletteSwatch.Metal, 0.008f);
            b.Box(At(new Vector3(0.01f, 0.06f, 0.005f), new Vector3(3f, -6f, -2f)), new Vector3(0.32f, 0.035f, 0.2f),
                PaletteSwatch.Rust, 0.008f);
            b.Box(At(new Vector3(-0.005f, 0.1f, -0.005f), new Vector3(-2f, 9f, 3f)), new Vector3(0.3f, 0.035f, 0.21f),
                PaletteSwatch.FadedPaint, 0.008f);
            foreach (float x in new[] { -0.09f, 0.09f })
            {
                b.Box(At(x, 0.06f, 0f), new Vector3(0.035f, 0.14f, 0.235f), PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>Wiring: a coil of copper cable on a dark hub, its end tucked, a sage tag.</summary>
        public static LowPolyMeshBuilder WiringBundle()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Torus(At(0f, 0.04f, 0f), 0.12f, 0.042f, 12, 5, PaletteSwatch.WarmAccent);
            b.Torus(At(new Vector3(0.005f, 0.1f, 0f), new Vector3(4f, 0f, 3f)), 0.115f, 0.04f, 12, 5,
                PaletteSwatch.WarmAccent);
            b.Prism(At(0f, 0.07f, 0f), 0.07f, 0.16f, 8, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, new Vector3(0.15f, 0.12f, 0.02f), new Vector3(0.17f, 0.05f, 0.1f), 0.022f, 5,
                PaletteSwatch.WarmAccent);
            b.Box(At(new Vector3(0.17f, 0.04f, 0.11f), new Vector3(0f, 30f, 0f)), new Vector3(0.05f, 0.03f, 0.04f),
                PaletteSwatch.Sage);
            return b;
        }

        /// <summary>Optics: three violet solar tiles fanned on a pin, one cracked, a lens disc at the hinge.</summary>
        public static LowPolyMeshBuilder OpticsBundle()
        {
            var b = new LowPolyMeshBuilder(300);
            for (int i = 0; i < 3; i++)
            {
                Matrix4x4 tile = At(new Vector3(0f, 0.012f + i * 0.02f, 0f), new Vector3(0f, -40f + i * 40f, 0f))
                    * At(0f, 0f, 0.1f);
                b.Box(tile, new Vector3(0.13f, 0.016f, 0.2f), PaletteSwatch.SkyHorizon);
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Box(tile * At(side * 0.068f, 0f, 0f), new Vector3(0.012f, 0.02f, 0.2f), PaletteSwatch.Metal);
                }
            }

            b.Box(At(new Vector3(0.064f, 0.061f, 0.077f), new Vector3(0f, 55f, 0f)), new Vector3(0.008f, 0.004f, 0.14f),
                PaletteSwatch.Charcoal);

            b.Prism(At(0f, 0.04f, 0f), 0.07f, 0.09f, 10, PaletteSwatch.Metal);
            b.Prism(At(0f, 0.09f, 0f), 0.055f, 0.012f, 10, PaletteSwatch.SkyHorizon);
            return b;
        }
    }
}
