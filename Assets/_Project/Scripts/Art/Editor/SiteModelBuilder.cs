using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The salvage sites of M3-13 (docs/ARCHITECTURE.md, "Contract: salvage sites"): <c>Site_&lt;id&gt;</c> in
    /// Generated/Art/Sites, its root on the ground at the site anchor (+Z = the anchor's Forward), holding the
    /// weathered <c>Skeleton</c> that stays forever, 4-8 detachable <c>Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]</c>
    /// pieces pivoted at their attach points (each with a <c>CutPoint</c> whose +Z leaves the cut face) and the
    /// <c>Heart</c> where the site's relic rests; the Kestrel trail's loose
    /// <c>Debris_&lt;Material&gt;_&lt;n&gt;</c> bits beside them; and the three material bundles
    /// <c>Material_&lt;Material&gt;</c> in Generated/Art/Pickups. Meshes only, M_LowPoly, nothing glows.
    /// </summary>
    public static class SiteModelBuilder
    {
        public const string SkeletonName = "Skeleton";
        public const string CutPointName = "CutPoint";
        public const string HeartName = "Heart";
        public const string DragSuffix = "_Drag";

        /// <summary>The site ids of the contract (site.&lt;id&gt; anchors, Site_&lt;id&gt; prefabs).</summary>
        public static IReadOnlyList<string> Ids => new[] { "depot", "kestrel", "drill", "garage", "lander" };

        /// <summary>The Kestrel trail's loose bits; each is worth one bundle of its material.</summary>
        public static IReadOnlyList<string> DebrisNames => new[]
        {
            "Debris_Metal_0", "Debris_Metal_1", "Debris_Wiring_0", "Debris_Optics_0", "Debris_Optics_1",
        };

        [MoonBuilder("Art/Sites", 190)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            foreach (string id in Ids)
            {
                ModelPrefabWriter.Write(CreateSite(id), ArtPaths.SitesFolder, material);
            }

            foreach (string name in DebrisNames)
            {
                ModelPrefabWriter.Write(CreateDebris(name), ArtPaths.SitesFolder, material);
            }

            foreach (SalvageMaterial bundle in Enum.GetValues(typeof(SalvageMaterial)))
            {
                ModelPrefabWriter.Write(CreateBundle(bundle), ArtPaths.PickupFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static string SiteName(string id)
        {
            return "Site_" + id;
        }

        public static string BundleName(SalvageMaterial material)
        {
            return "Material_" + material;
        }

        public static ModelNode CreateSite(string id)
        {
            SiteRecipe recipe = Recipe(id);
            string name = SiteName(id);
            var root = new ModelNode(name, Vector3.zero);
            root.Add(new ModelNode(SkeletonName, Vector3.zero, new ModelMesh(name + "_" + SkeletonName,
                recipe.Skeleton)));
            for (int i = 0; i < recipe.Pieces.Count; i++)
            {
                SalvagePieceRecipe piece = recipe.Pieces[i];
                piece.Geometry.Transform(piece.Geometry.RangeFrom(0), Matrix4x4.Translate(-piece.Pivot));
                string pieceName = $"Salvage_{i}_{piece.Material}{(piece.Drag ? DragSuffix : string.Empty)}";
                ModelNode node = root.Add(new ModelNode(pieceName, piece.Pivot,
                    new ModelMesh($"{name}_Salvage_{i}", piece.Geometry)));
                node.Add(new ModelNode(CutPointName, piece.CutPoint - piece.Pivot, Facing(piece.CutNormal)));
            }

            root.Add(new ModelNode(HeartName, recipe.Heart));
            return root;
        }

        public static ModelNode CreateBundle(SalvageMaterial material)
        {
            LowPolyMeshBuilder geometry;
            switch (material)
            {
                case SalvageMaterial.Metal:
                    geometry = SalvageBitMeshes.MetalBundle();
                    break;
                case SalvageMaterial.Wiring:
                    geometry = SalvageBitMeshes.WiringBundle();
                    break;
                case SalvageMaterial.Optics:
                    geometry = SalvageBitMeshes.OpticsBundle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }

            string name = BundleName(material);
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        public static ModelNode CreateDebris(string name)
        {
            LowPolyMeshBuilder geometry;
            switch (name)
            {
                case "Debris_Metal_0":
                    geometry = SalvageBitMeshes.FoilShard();
                    break;
                case "Debris_Metal_1":
                    geometry = SalvageBitMeshes.StrutChunk();
                    break;
                case "Debris_Wiring_0":
                    geometry = SalvageBitMeshes.HarnessClump();
                    break;
                case "Debris_Optics_0":
                    geometry = SalvageBitMeshes.CellTile();
                    break;
                case "Debris_Optics_1":
                    geometry = SalvageBitMeshes.LensHousing();
                    break;
                default:
                    throw new ArgumentException($"Unknown debris '{name}'.", nameof(name));
            }

            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        private static SiteRecipe Recipe(string id)
        {
            switch (id)
            {
                case "depot":
                    return DepotSite.Create();
                case "kestrel":
                    return KestrelSite.Create();
                case "drill":
                    return DrillSite.Create();
                case "garage":
                    return GarageSite.Create();
                case "lander":
                    return LanderSite.Create();
                default:
                    throw new ArgumentException($"Unknown salvage site '{id}'.", nameof(id));
            }
        }

        /// <summary>The rotation turning +Z onto <paramref name="direction"/> with +Y kept up (managed math).</summary>
        private static Quaternion Facing(Vector3 direction)
        {
            float pitch = Mathf.Asin(Mathf.Clamp(-direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return Place.Rotation(new Vector3(pitch, yaw, 0f));
        }
    }
}
