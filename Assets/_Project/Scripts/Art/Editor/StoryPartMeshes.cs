using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The tower's three story parts (M3-16 P1): found in the wrecks, carried home on 07's back (its TubeSocket) and
    /// fed to the tower's port. True scale, each under half a metre, chunky enough to read from the chase camera, and
    /// left alone for decades like everything else (ruling 12): faded paint, rust at seams and bolts, dust on top.
    /// Each is centred on its mass, like the other pickups.
    /// </summary>
    internal static class StoryPartMeshes
    {
        /// <summary>A copper coil drum on its spool, its casing cracked open down one side.</summary>
        public static LowPolyMeshBuilder TowerCoil()
        {
            var b = new LowPolyMeshBuilder(500);
            b.Prism(At(0f, 0.18f, 0f), 0.07f, 0.34f, 8, PaletteSwatch.Metal);
            foreach (float y in new[] { 0.02f, 0.34f })
            {
                b.Prism(At(0f, y, 0f), 0.17f, 0.03f, 10, PaletteSwatch.Metal);
                b.Prism(At(0f, y + 0.017f, 0f), 0.172f, 0.006f, 10, PaletteSwatch.Rust, false);
            }

            for (int i = 0; i < 6; i++)
            {
                b.Torus(At(0f, 0.07f + i * 0.045f, 0f), 0.13f, 0.024f, 12, 4, PaletteSwatch.WarmAccent);
            }

            MeshRange casing = b.Box(At(new Vector3(-0.1f, 0.18f, 0f), new Vector3(0f, 0f, 6f)),
                new Vector3(0.04f, 0.26f, 0.24f), PaletteSwatch.FadedPaint, 0.006f);
            b.Box(At(new Vector3(-0.122f, 0.2f, 0.02f), new Vector3(0f, 0f, 25f)), new Vector3(0.006f, 0.16f, 0.02f),
                PaletteSwatch.Rust);
            SiteKit.DustCap(b, casing);
            return RecipeKit.CentredOnMass(b);
        }

        /// <summary>
        /// A boxy transformer: a sage case with cooling fins, two insulated terminals and a frayed lead.
        /// </summary>
        public static LowPolyMeshBuilder Transformer()
        {
            var b = new LowPolyMeshBuilder(500);
            MeshRange body = b.Box(At(0f, 0.13f, 0f), new Vector3(0.3f, 0.26f, 0.22f), PaletteSwatch.Sage, 0.015f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 0; k < 4; k++)
                {
                    b.Box(At(side * 0.165f, 0.13f, -0.075f + k * 0.05f), new Vector3(0.03f, 0.22f, 0.012f),
                        PaletteSwatch.Metal);
                }
            }

            for (int i = -1; i <= 1; i += 2)
            {
                b.Prism(At(i * 0.07f, 0.29f, 0f), 0.025f, 0.05f, 8, PaletteSwatch.Charcoal);
                b.Prism(At(i * 0.07f, 0.325f, 0f), 0.01f, 0.03f, 6, PaletteSwatch.Metal);
                b.Prism(At(i * 0.07f, 0.27f, 0f), 0.03f, 0.008f, 8, PaletteSwatch.Rust, false);
            }

            var lead = new Vector3(0.07f, 0.34f, 0f);
            var end = new Vector3(0.22f, 0.3f, 0.12f);
            RecipeKit.Rod(b, lead, end, 0.012f, 6, PaletteSwatch.Charcoal);
            for (int k = 0; k < 3; k++)
            {
                RecipeKit.Rod(b, end, end + new Vector3(0.03f, -0.01f - k * 0.015f, -0.02f + k * 0.02f), 0.003f, 4,
                    PaletteSwatch.WarmAccent);
            }

            SiteKit.RustStreak(b, SiteKit.Face(new Vector3(0f, 0f, 0.112f), Vector3.forward, Vector3.up), 0.08f,
                0.22f, 0.12f, 0.02f);
            SiteKit.DustCap(b, body);
            return RecipeKit.CentredOnMass(b);
        }

        /// <summary>A small relay module with its grille, a stub arm and a fragment of its broken dish.</summary>
        public static LowPolyMeshBuilder RelaySpare()
        {
            var b = new LowPolyMeshBuilder(500);
            MeshRange body = b.Box(At(0f, 0.08f, 0f), new Vector3(0.22f, 0.16f, 0.18f), PaletteSwatch.FadedPaint,
                0.012f);
            for (int k = 0; k < 4; k++)
            {
                b.Box(At(0f, 0.04f + k * 0.03f, 0.092f), new Vector3(0.15f, 0.012f, 0.008f), PaletteSwatch.Charcoal);
            }

            RecipeKit.Rod(b, new Vector3(0.05f, 0.16f, -0.03f), new Vector3(0.09f, 0.28f, -0.06f), 0.014f, 6,
                PaletteSwatch.Metal);
            Matrix4x4 dish = At(new Vector3(0.1f, 0.31f, -0.07f), new Vector3(-35f, 20f, 15f));
            b.Frustum(dish, 0.04f, 0.17f, 0.06f, 7, PaletteSwatch.Metal, false);
            b.Box(dish * At(0.13f, 0.03f, 0f), new Vector3(0.05f, 0.012f, 0.1f), PaletteSwatch.Rust);
            SiteKit.RustStreak(b, SiteKit.Face(new Vector3(0f, 0f, 0.092f), Vector3.forward, Vector3.up), -0.07f,
                0.14f, 0.1f, 0.016f);
            SiteKit.DustCap(b, body);
            return RecipeKit.CentredOnMass(b);
        }
    }
}
