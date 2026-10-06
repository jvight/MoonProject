using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The four scrap pickups of the M2 content contract (docs/ARCHITECTURE.md): Scrap_Bolt, Scrap_Gear,
    /// Scrap_Panel and Scrap_Coil, 0.3-0.5 m, each with a TechGlow accent so they glint across the dark dust.
    /// Single-node models, pivot at the centre of mass, meshes only (gameplay adds physics and magnetism).
    /// </summary>
    public static class ScrapModelBuilder
    {
        /// <summary>Prefab names fixed by the content contract (gameplay loads them by path).</summary>
        public static IReadOnlyList<string> Names => new[] { "Scrap_Bolt", "Scrap_Gear", "Scrap_Panel", "Scrap_Coil" };

        private static readonly Vector2[] PanelShard =
        {
            new Vector2(-0.2f, -0.15f), new Vector2(0.17f, -0.17f), new Vector2(0.21f, 0.02f),
            new Vector2(0.12f, 0.06f), new Vector2(0.15f, 0.16f), new Vector2(-0.03f, 0.13f),
            new Vector2(-0.09f, 0.18f), new Vector2(-0.19f, 0.08f),
        };

        [MoonBuilder("Art/Scrap", 130)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            foreach (string name in Names)
            {
                ModelPrefabWriter.Write(CreateModel(name), ArtPaths.ScrapFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateModel(string name)
        {
            LowPolyMeshBuilder geometry;
            switch (name)
            {
                case "Scrap_Bolt":
                    geometry = Bolt();
                    break;
                case "Scrap_Gear":
                    geometry = Gear();
                    break;
                case "Scrap_Panel":
                    geometry = Panel();
                    break;
                case "Scrap_Coil":
                    geometry = Coil();
                    break;
                default:
                    throw new System.ArgumentException($"Unknown scrap '{name}'.", nameof(name));
            }

            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        /// <summary>A chunky hex bolt lying on its side, a glowing washer under the head and a hex nut.</summary>
        private static LowPolyMeshBuilder Bolt()
        {
            var b = new LowPolyMeshBuilder(260);
            b.Prism(At(new Vector3(-0.17f, 0f, 0f), AlongX), 0.085f, 0.07f, 6, PaletteSwatch.Metal);
            b.Prism(At(new Vector3(-0.125f, 0f, 0f), AlongX), 0.066f, 0.014f, 12, PaletteSwatch.TechGlow);
            b.Prism(At(new Vector3(0.03f, 0f, 0f), AlongX), 0.04f, 0.3f, 8, PaletteSwatch.Metal);
            for (int i = 0; i < 5; i++)
            {
                b.Torus(At(new Vector3(0.06f + i * 0.026f, 0f, 0f), AlongX), 0.042f, 0.007f, 8, 3,
                    PaletteSwatch.Charcoal);
            }

            b.Torus(At(new Vector3(-0.02f, 0f, 0f), new Vector3(0f, 0f, -90f), new Vector3(1f, 1.5f, 1f)), 0.06f,
                0.022f, 6, 4, PaletteSwatch.Charcoal);
            b.Prism(At(new Vector3(0.185f, 0f, 0f), AlongX), 0.025f, 0.012f, 8, PaletteSwatch.TechGlow);
            return b;
        }

        /// <summary>A twelve-tooth gear lying flat, a dark hub with a glowing axle core.</summary>
        private static LowPolyMeshBuilder Gear()
        {
            const int teeth = 12;
            const float tip = 0.2f;
            const float root = 0.16f;
            var outline = new Vector2[teeth * 4];
            for (int i = 0; i < teeth; i++)
            {
                float start = i * 2f * Mathf.PI / teeth;
                float step = 2f * Mathf.PI / teeth;
                outline[4 * i] = OnCircle(root, start);
                outline[4 * i + 1] = OnCircle(tip, start + step * 0.15f);
                outline[4 * i + 2] = OnCircle(tip, start + step * 0.45f);
                outline[4 * i + 3] = OnCircle(root, start + step * 0.6f);
            }

            var b = new LowPolyMeshBuilder(260);
            b.Extrude(At(Vector3.zero, new Vector3(90f, 0f, 0f)), outline, 0.05f, PaletteSwatch.Metal);
            b.Prism(At(Vector3.zero), 0.075f, 0.08f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(Vector3.zero), 0.035f, 0.09f, 8, PaletteSwatch.TechGlow);
            b.Torus(At(new Vector3(0f, 0.027f, 0f)), 0.12f, 0.008f, 16, 3, PaletteSwatch.TechGlow);
            return b;
        }

        /// <summary>A broken shard of a solar panel: violet cells, a glowing wire torn at the break.</summary>
        private static LowPolyMeshBuilder Panel()
        {
            var b = new LowPolyMeshBuilder(200);
            Matrix4x4 flat = At(Vector3.zero, new Vector3(90f, 0f, 0f));
            b.Extrude(flat, PanelShard, 0.022f, PaletteSwatch.Metal);
            b.Box(flat * At(-0.09f, -0.06f, -0.013f), new Vector3(0.15f, 0.13f, 0.008f), PaletteSwatch.SkyHorizon);
            b.Box(flat * At(0.08f, -0.08f, -0.013f), new Vector3(0.14f, 0.1f, 0.008f), PaletteSwatch.SkyHorizon);
            b.Box(flat * At(-0.1f, 0.09f, -0.013f), new Vector3(0.12f, 0.06f, 0.008f), PaletteSwatch.SkyHorizon);
            Vector3 wireFrom = new Vector3(0.12f, 0.02f, -0.07f);
            Vector3 wireBend = new Vector3(0.2f, 0.035f, -0.02f);
            Vector3 wireEnd = new Vector3(0.26f, 0.03f, 0.05f);
            RecipeKit.Rod(b, wireFrom, wireBend, 0.008f, 5, PaletteSwatch.TechGlow);
            RecipeKit.Rod(b, wireBend, wireEnd, 0.008f, 5, PaletteSwatch.TechGlow);
            b.Icosphere(At(wireEnd), 0.016f, 1, PaletteSwatch.TechGlow);
            b.Box(At(new Vector3(0.02f, 0.016f, 0.07f), new Vector3(0f, 20f, 0f)), new Vector3(0.06f, 0.012f, 0.04f),
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>A spool of copper wire with metal flanges, a glowing core and one loose glowing end.</summary>
        private static LowPolyMeshBuilder Coil()
        {
            var b = new LowPolyMeshBuilder(320);
            b.Prism(At(Vector3.zero), 0.07f, 0.2f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(0f, -0.1f, 0f), 0.15f, 0.025f, 10, PaletteSwatch.Metal);
            b.Prism(At(0f, 0.1f, 0f), 0.15f, 0.025f, 10, PaletteSwatch.Metal);
            for (int i = 0; i < 4; i++)
            {
                b.Torus(At(0f, -0.06f + i * 0.04f, 0f), 0.1f, 0.022f, 10, 4, PaletteSwatch.WarmAccent);
            }

            b.Prism(At(Vector3.zero), 0.03f, 0.23f, 8, PaletteSwatch.TechGlow);
            Vector3 looseStart = new Vector3(0.11f, 0.06f, 0.03f);
            Vector3 looseBend = new Vector3(0.19f, 0.1f, 0.08f);
            Vector3 looseEnd = new Vector3(0.23f, 0.04f, 0.16f);
            RecipeKit.Rod(b, looseStart, looseBend, 0.011f, 5, PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, looseBend, looseEnd, 0.011f, 5, PaletteSwatch.WarmAccent);
            b.Icosphere(At(looseEnd), 0.02f, 1, PaletteSwatch.TechGlow);
            return b;
        }

        private static Vector2 OnCircle(float radius, float radians)
        {
            return new Vector2(radius * Mathf.Cos(radians), radius * Mathf.Sin(radians));
        }
    }
}
