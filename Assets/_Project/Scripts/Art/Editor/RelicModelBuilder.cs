using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The six relics of the M2 content contract: Generated/Art/Relics/Relic_&lt;id&gt;.prefab, single-node models
    /// (meshes only) pivoted at the centre of mass, because gameplay makes them physics bodies on the tether. Each is
    /// at its true size (VISION ruling 13): at most about twice the real thing and never over half a metre;
    /// readability comes from glow, pillars and sound, never from giant props.
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
                    geometry = RelicMeshes.CassettePlayer();
                    break;
                case "rubber_duck":
                    geometry = RelicMeshes.RubberDuck();
                    break;
                case "golden_record":
                    geometry = RelicMeshes.GoldenRecord();
                    break;
                case "astronaut_boot":
                    geometry = RelicMeshes.AstronautBoot();
                    break;
                case "teapot":
                    geometry = RelicMeshes.Teapot();
                    break;
                case "garden_gnome":
                    geometry = RelicMeshes.GardenGnome();
                    break;
                default:
                    throw new ArgumentException($"Unknown relic id '{id}'.", nameof(id));
            }

            string name = PrefabName(id);
            Sized(geometry, TrueSize(id));
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        /// <summary>
        /// The relic's largest dimension in metres: a cassette player 0.25, a bath duck 0.2, the golden record 0.4,
        /// a suit boot 0.4, a teapot 0.35, a garden gnome 0.5.
        /// </summary>
        public static float TrueSize(string id)
        {
            switch (id)
            {
                case "cassette_player":
                    return 0.25f;
                case "rubber_duck":
                    return 0.2f;
                case "golden_record":
                case "astronaut_boot":
                    return 0.4f;
                case "teapot":
                    return 0.35f;
                case "garden_gnome":
                    return 0.5f;
                default:
                    throw new ArgumentException($"Unknown relic id '{id}'.", nameof(id));
            }
        }

        /// <summary>
        /// Scales <paramref name="geometry"/> uniformly so its largest dimension is <paramref name="size"/>.
        /// </summary>
        private static void Sized(LowPolyMeshBuilder geometry, float size)
        {
            Vector3 extent = geometry.Bounds.size;
            float largest = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
            geometry.Transform(geometry.RangeFrom(0), Matrix4x4.Scale(Vector3.one * (size / largest)));
        }
    }
}
