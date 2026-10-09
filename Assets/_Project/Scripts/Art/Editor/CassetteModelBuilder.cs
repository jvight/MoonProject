using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Ro's cassettes (docs/features/M3-05-bell-radio-cassettes.md) as pickups:
    /// Generated/Art/Pickups/Cassette_&lt;id&gt;.prefab, one single-node model per <see cref="Styles"/> entry,
    /// pivoted at the centre of mass (about 0.057 m above the tape's bottom edge), standing upright with the label
    /// facing +Z. On Bell's shelf a tape stands on a slot with the slot's rotation, its bottom edge on the slot.
    /// </summary>
    public static class CassetteModelBuilder
    {
        public const string PrefabPrefix = "Cassette_";

        /// <summary>
        /// Every tape's look, in story order. Label colours stay distinct so the shelf reads at a glance: the night
        /// show in 07's coral, the airlock jam in honey, the orbit lullaby in sage.
        /// </summary>
        public static IReadOnlyList<CassetteStyle> Styles => new[]
        {
            new CassetteStyle("after_dark_1", PaletteSwatch.Charcoal, PaletteSwatch.WarmAccent, PaletteSwatch.Charcoal),
            new CassetteStyle("dust_and_honey", PaletteSwatch.Enamel, PaletteSwatch.Honey, PaletteSwatch.Wood),
            new CassetteStyle("slow_orbit", PaletteSwatch.SkyHorizon, PaletteSwatch.Sage, PaletteSwatch.SkyHorizon),
        };

        public static string PrefabName(string id)
        {
            return PrefabPrefix + id;
        }

        [MoonBuilder("Art/Cassettes", 170)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            foreach (CassetteStyle style in Styles)
            {
                ModelPrefabWriter.Write(CreateCassette(style.Id), ArtPaths.PickupFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateCassette(string id)
        {
            foreach (CassetteStyle style in Styles)
            {
                if (style.Id == id)
                {
                    string name = PrefabName(id);
                    LowPolyMeshBuilder geometry = RecipeKit.CentredOnMass(CassetteMeshes.Cassette(style));
                    return new ModelNode(name, Vector3.zero, new ModelMesh(name, geometry));
                }
            }

            throw new ArgumentException($"Unknown cassette id '{id}'.", nameof(id));
        }
    }
}
