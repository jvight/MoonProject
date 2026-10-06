using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The six relics of the M2 content contract: Generated/Art/Relics/Relic_&lt;id&gt;.prefab, single-node models
    /// (meshes only) pivoted at the centre of mass, because gameplay makes them physics bodies on the tether.
    /// </summary>
    public static class RelicModelBuilder
    {
        /// <summary>Relic ids fixed by the content contract (saves and prefab names use them).</summary>
        public static IReadOnlyList<string> Ids => new[]
        {
            "cassette_player", "rubber_duck", "golden_record", "astronaut_boot", "teapot", "garden_gnome",
        };

        public static string PrefabName(string id)
        {
            return "Relic_" + id;
        }

        [MoonBuilder("Art/Relics", 150)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            foreach (string id in Ids)
            {
                ModelPrefabWriter.Write(CreateModel(id), ArtPaths.RelicFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateModel(string id)
        {
            LowPolyMeshBuilder geometry;
            switch (id)
            {
                case "cassette_player":
                    geometry = Scaled(RelicMeshes.CassettePlayer(), 1.15f);
                    break;
                case "rubber_duck":
                    geometry = RelicMeshes.RubberDuck();
                    break;
                case "golden_record":
                    geometry = RelicMeshes.GoldenRecord();
                    break;
                case "astronaut_boot":
                    geometry = Scaled(RelicMeshes.AstronautBoot(), 1.1f);
                    break;
                case "teapot":
                    geometry = Scaled(RelicMeshes.Teapot(), 1.25f);
                    break;
                case "garden_gnome":
                    geometry = RelicMeshes.GardenGnome();
                    break;
                default:
                    throw new ArgumentException($"Unknown relic id '{id}'.", nameof(id));
            }

            string name = PrefabName(id);
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        private static LowPolyMeshBuilder Scaled(LowPolyMeshBuilder geometry, float scale)
        {
            geometry.Transform(geometry.RangeFrom(0), Matrix4x4.Scale(Vector3.one * scale));
            return geometry;
        }
    }
}
