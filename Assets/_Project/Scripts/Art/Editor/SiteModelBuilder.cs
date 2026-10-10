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
    /// <c>Heart</c> where the site's relic rests, and (M3-18 L1) the depot's, Kestrel's, drill's and garage's tall
    /// <c>Landmark</c> with its <c>LandmarkPilot</c> lamp (M_LowPolyGlowOff: dark until gameplay lights it once the
    /// heart is found); the Kestrel trail's loose <c>Debris_&lt;Material&gt;_&lt;n&gt;</c> bits beside them; and the
    /// three material bundles <c>Material_&lt;Material&gt;</c> in Generated/Art/Pickups. Meshes only, M_LowPoly.
    /// </summary>
    public static class SiteModelBuilder
    {
        public const string SkeletonName = "Skeleton";
        public const string CutPointName = "CutPoint";
        public const string HeartName = "Heart";
        public const string DragSuffix = "_Drag";
        public const string LandmarkName = "Landmark";
        public const string LandmarkPilotName = "LandmarkPilot";

        /// <summary>How far outside the skeleton's back-left corner a landmark stands (metres).</summary>
        private const float LandmarkClearance = 2f;

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
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            foreach (string id in Ids)
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateSite(id)), ArtPaths.SitesFolder, material,
                    glowOff);
            }

            foreach (string name in DebrisNames)
            {
                ModelPrefabWriter.Write(CreateDebris(name), ArtPaths.SitesFolder, material);
            }

            foreach (SalvagePieceMaterial bundle in Enum.GetValues(typeof(SalvagePieceMaterial)))
            {
                ModelPrefabWriter.Write(CreateBundle(bundle), ArtPaths.PickupFolder, material);
            }

            ModelPrefabWriter.Write(CreateBlueprintTube(), ArtPaths.PickupFolder, material);
            foreach (string part in StoryPartNames)
            {
                ModelPrefabWriter.Write(CreateStoryPart(part), ArtPaths.PickupFolder, material);
            }
            AssetDatabase.SaveAssets();
        }

        public const string BlueprintTubeName = "BlueprintTube";

        /// <summary>
        /// The tower's story parts (M3-16 P1), pickups 07 carries home on its TubeSocket and feeds to the tower's port.
        /// </summary>
        public static readonly string[] StoryPartNames = { "Part_TowerCoil", "Part_Transformer", "Part_RelaySpare" };

        public static ModelNode CreateStoryPart(string name)
        {
            LowPolyMeshBuilder geometry;
            switch (name)
            {
                case "Part_TowerCoil":
                    geometry = StoryPartMeshes.TowerCoil();
                    break;
                case "Part_Transformer":
                    geometry = StoryPartMeshes.Transformer();
                    break;
                case "Part_RelaySpare":
                    geometry = StoryPartMeshes.RelaySpare();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown story part.");
            }

            return new ModelNode(name, Vector3.zero, new ModelMesh(name, geometry));
        }

        public static string SiteName(string id)
        {
            return "Site_" + id;
        }

        public static string BundleName(SalvagePieceMaterial material)
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
            if (SiteLandmarkMeshes.HasLandmark(id))
            {
                Bounds skeleton = recipe.Skeleton.Bounds;
                var foot = new Vector3(skeleton.min.x - LandmarkClearance, 0f, skeleton.min.z - LandmarkClearance);
                ModelNode landmark = root.Add(new ModelNode(LandmarkName, foot,
                    new ModelMesh($"{name}_{LandmarkName}", SiteLandmarkMeshes.Landmark(id))));
                landmark.Add(new ModelNode(LandmarkPilotName, SiteLandmarkMeshes.PilotTop(id), Quaternion.identity,
                    new ModelMesh($"{name}_{LandmarkPilotName}", SiteLandmarkMeshes.Pilot()),
                    ModelMaterial.PaletteGlowOff));
            }

            return root;
        }

        public static ModelNode CreateBundle(SalvagePieceMaterial material)
        {
            LowPolyMeshBuilder geometry;
            switch (material)
            {
                case SalvagePieceMaterial.Metal:
                    geometry = SalvageBitMeshes.MetalBundle();
                    break;
                case SalvagePieceMaterial.Wiring:
                    geometry = SalvageBitMeshes.WiringBundle();
                    break;
                case SalvagePieceMaterial.Optics:
                    geometry = SalvageBitMeshes.OpticsBundle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }

            string name = BundleName(material);
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        /// <summary>
        /// The blueprint tube a salvage site's heart holds (M3-16): Gameplay places it and pins its kit's sheet on
        /// Kenji's board once 07 scans it.
        /// </summary>
        public static ModelNode CreateBlueprintTube()
        {
            return new ModelNode(BlueprintTubeName, Vector3.zero,
                new ModelMesh(BlueprintTubeName, BlueprintMeshes.Tube()));
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
