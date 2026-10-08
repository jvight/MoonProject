using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The small salvage models: the three material bundles 07 carries home (one silhouette and colour each: a
    /// strapped stack of grey plates, a round copper coil, a fan of violet cells) and the loose wreck bits strewn along
    /// Kestrel-3's debris trail. Built around the origin; the builder pivots them at the centre of mass.
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

        /// <summary>A crumpled gold-foil hull panel, bent double.</summary>
        public static LowPolyMeshBuilder FoilShard()
        {
            var b = new LowPolyMeshBuilder(120);
            Paint foil = Paint.Facing(PaletteSwatch.Honey, PaletteSwatch.Metal, Vector3.up, 0.2f);
            b.Box(At(new Vector3(0f, 0.05f, -0.2f), new Vector3(-8f, 15f, 6f)), new Vector3(0.7f, 0.04f, 0.45f), foil);
            b.Box(At(new Vector3(0.04f, 0.2f, 0.17f), new Vector3(-48f, 15f, 6f)), new Vector3(0.7f, 0.04f, 0.4f),
                foil);
            b.Prism(At(new Vector3(0.02f, 0.06f, 0f), new Vector3(0f, 15f, 90f)), 0.035f, 0.72f, 6,
                PaletteSwatch.Metal);
            return b;
        }

        /// <summary>A strut torn off the bus: a bar with its bracket and a rusted snapped end.</summary>
        public static LowPolyMeshBuilder StrutChunk()
        {
            var b = new LowPolyMeshBuilder(120);
            SiteKit.Bar(b, new Vector3(-0.45f, 0.07f, 0f), new Vector3(0.35f, 0.09f, 0.05f), 0.11f,
                PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(0.35f, 0.09f, 0.05f), new Vector3(0.5f, 0.2f, 0.12f), 0.1f, PaletteSwatch.Rust);
            b.Box(At(new Vector3(-0.45f, 0.1f, 0f), new Vector3(0f, 0f, 8f)), new Vector3(0.06f, 0.2f, 0.22f),
                PaletteSwatch.Metal, 0.01f);
            b.Prism(At(new Vector3(-0.49f, 0.14f, 0.05f), AlongX), 0.025f, 0.04f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>A clump of torn harness: copper and dark leads out of a sage connector.</summary>
        public static LowPolyMeshBuilder HarnessClump()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Box(At(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 20f, 6f)), new Vector3(0.22f, 0.14f, 0.16f),
                PaletteSwatch.Sage, 0.02f);
            Vector3[][] leads =
            {
                new[]
                {
                    new Vector3(0.1f, 0.08f, 0f), new Vector3(0.3f, 0.12f, 0.08f), new Vector3(0.42f, 0.04f, -0.05f),
                },
                new[]
                {
                    new Vector3(0.08f, 0.1f, 0.05f), new Vector3(0.22f, 0.04f, 0.25f), new Vector3(0.05f, 0.04f, 0.35f),
                },
                new[]
                {
                    new Vector3(-0.1f, 0.06f, 0f), new Vector3(-0.28f, 0.04f, -0.1f), new Vector3(-0.3f, 0.1f, -0.25f),
                },
            };
            for (int i = 0; i < leads.Length; i++)
            {
                SiteKit.Cable(b, leads[i], 0.032f, i == 2 ? PaletteSwatch.Charcoal : PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>A solar tile broken off a wing: a frame chunk with violet cells, one lost.</summary>
        public static LowPolyMeshBuilder CellTile()
        {
            var b = new LowPolyMeshBuilder(160);
            Matrix4x4 face = SiteKit.Face(new Vector3(0f, 0.12f, 0f), new Vector3(0.15f, 1f, -0.3f), Vector3.forward);
            SiteKit.SolarPanel(b, face, 0.75f, 0.5f, true);
            return b;
        }

        /// <summary>A sensor lens housing: a metal barrel, a dark rim, a violet lens.</summary>
        public static LowPolyMeshBuilder LensHousing()
        {
            var b = new LowPolyMeshBuilder(160);
            Matrix4x4 barrel = At(new Vector3(0f, 0.17f, 0f), new Vector3(80f, 25f, 0f));
            b.Prism(barrel, 0.16f, 0.36f, 10, PaletteSwatch.Metal);
            b.Torus(barrel * At(0f, 0.2f, 0f), 0.165f, 0.035f, 10, 3, PaletteSwatch.Charcoal);
            b.Prism(barrel * At(0f, 0.185f, 0f), 0.12f, 0.02f, 10, PaletteSwatch.SkyHorizon);
            b.Box(barrel * At(0f, -0.1f, 0.16f), new Vector3(0.12f, 0.1f, 0.06f), PaletteSwatch.FadedPaint);
            return b;
        }
    }
}
