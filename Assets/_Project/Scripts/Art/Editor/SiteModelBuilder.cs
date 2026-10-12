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
    /// heart is found); and the three material bundles <c>Material_&lt;Material&gt;</c> in Generated/Art/Pickups.
    /// Meshes only, M_LowPoly.
    /// </summary>
    public static class SiteModelBuilder
    {
        public const string SkeletonName = "Skeleton";
        public const string CutPointName = "CutPoint";
        public const string HeartName = "Heart";
        public const string DragSuffix = "_Drag";
        public const string LandmarkName = "Landmark";
        public const string LandmarkPilotName = "LandmarkPilot";

        /// <summary>
        /// The crew's supply crate (M3-25 T4), one per site beside its heart: <c>CrewCrate</c> (the sealed body, which stays
        /// as an empty prop) and its <c>Lid</c>, whose <c>CutPoint</c> sits on the front seal; one cut lifts the lid off.
        /// </summary>
        public const string CrewCrateName = "CrewCrate";

        public const string CrewCrateLidName = "Lid";

        // Off to the heart's right and out in front of the wreck, so the crate stands in the open where 07 arrives.
        private const float CrewCrateBesideHeart = 0.9f;
        private const float CrewCrateClearOfWreck = 0.8f;


        /// <summary>The site ids of the contract (site.&lt;id&gt; anchors, Site_&lt;id&gt; prefabs).</summary>
        public static IReadOnlyList<string> Ids => new[] { "depot", "kestrel", "drill", "garage", "lander" };

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

            foreach (SalvagePieceMaterial bundle in Enum.GetValues(typeof(SalvagePieceMaterial)))
            {
                ModelPrefabWriter.Write(CreateBundle(bundle), ArtPaths.PickupFolder, material);
            }

            ModelPrefabWriter.Write(CreateOrangeBeadsBundle(), ArtPaths.PickupFolder, material);
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
            AddCrewCrate(root, name, new Vector3(recipe.Heart.x + CrewCrateBesideHeart, 0f,
                Footprint(root).max.z + CrewCrateClearOfWreck));
            if (SiteLandmarkMeshes.HasLandmark(id))
            {
                (Vector3 foot, float yaw) = SiteLandmarkMeshes.Placement(id);
                ModelNode landmark = root.Add(new ModelNode(LandmarkName, foot,
                    Place.Rotation(new Vector3(0f, yaw, 0f)),
                    new ModelMesh($"{name}_{LandmarkName}", SiteLandmarkMeshes.Landmark(id))));
                landmark.Add(new ModelNode(LandmarkPilotName, SiteLandmarkMeshes.PilotTop(id), Quaternion.identity,
                    new ModelMesh($"{name}_{LandmarkPilotName}", SiteLandmarkMeshes.Pilot()),
                    ModelMaterial.PaletteGlowOff));
            }

            return root;
        }

        /// <summary>The ground the site's wreck covers: its skeleton and every piece where it hangs.</summary>
        public static Bounds Footprint(ModelNode site)
        {
            Bounds footprint = site.Children[0].Mesh.Geometry.Bounds;
            foreach (ModelNode node in site.Children)
            {
                if (node.Mesh != null && node.Name.StartsWith("Salvage_", StringComparison.Ordinal))
                {
                    Bounds piece = node.Mesh.Geometry.Bounds;
                    piece.center += node.LocalPosition;
                    footprint.Encapsulate(piece);
                }
            }

            return footprint;
        }

        private static void AddCrewCrate(ModelNode root, string name, Vector3 foot)
        {
            ModelNode crate = root.Add(new ModelNode(CrewCrateName, foot,
                new ModelMesh(name + "_" + CrewCrateName, SupplyShelfMeshes.CrewCrateFrame())));
            ModelNode lid = crate.Add(new ModelNode(CrewCrateLidName, SupplyShelfMeshes.LidSeat,
                new ModelMesh(name + "_" + CrewCrateName + CrewCrateLidName, SupplyShelfMeshes.CrewCrateLid())));
            lid.Add(new ModelNode(CutPointName, SupplyShelfMeshes.SealPoint, Facing(Vector3.forward)));
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
        /// The first lunar material's bundle (M3-27 #4), <c>Material_OrangeBeads</c> beside the salvage ones: orange
        /// beads heaped in a round woven sack, round and orange where Wiring's coil is round and copper.
        /// </summary>
        public const string OrangeBeadsBundleName = "Material_OrangeBeads";

        public static ModelNode CreateOrangeBeadsBundle()
        {
            return new ModelNode(OrangeBeadsBundleName, Vector3.zero,
                new ModelMesh(OrangeBeadsBundleName, RecipeKit.CentredOnMass(OrangeBeadMeshes.Bundle())));
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
