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
        /// as an empty prop) and its <c>Lid</c>, whose <c>CutPoint</c> sits on the front seal and whose <c>Seal</c>
        /// (M_LowPolyGlowOff) glows round the seam until the one cut lifts the lid off.
        /// </summary>
        public const string CrewCrateName = "CrewCrate";

        public const string CrewCrateLidName = "Lid";
        public const string CrewCrateSealName = "Seal";

        /// <summary>
        /// The relics' debris pockets (0.5.5 §2, D6): <c>RelicPocket_&lt;relic id&gt;</c> at the wreck's edge, a dark gap
        /// under a fallen panel, +Z its open side facing out from the wreck, holding <c>DigSpot</c> where the relic
        /// and its glint sit in the gap.
        /// </summary>
        public const string RelicPocketPrefix = "RelicPocket_";

        public const string DigSpotName = "DigSpot";

        /// <summary>The relics buried at each site (the content contract's ids, matching gameplay's relic recipes).</summary>
        public static IReadOnlyList<string> RelicsAt(string id)
        {
            switch (id)
            {
                case "garage":
                    return new[] { "cassette_player", "rubber_duck", "garden_gnome" };
                case "kestrel":
                    return new[] { "golden_record" };
                case "drill":
                    return new[] { "astronaut_boot" };
                case "lander":
                    return new[] { "teapot" };
                default:
                    return Array.Empty<string>();
            }
        }

        /// <summary>
        /// The scannable fragments' places at a site (0.5.5 §1.3, §1.4): <c>FragmentRest_&lt;Blueprint&gt;_&lt;n&gt;</c>,
        /// a fallen panel on rubble inside the wreck's front edge sized to its fragment, holding
        /// <c>FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt;</c> on its top (+Y up, +Z the fragment's front) where gameplay
        /// sets the fragment.
        /// </summary>
        public const string FragmentRestPrefix = "FragmentRest_";

        public const string FragmentSocketPrefix = "FragmentSocket_";

        /// <summary>The blueprint whose fragments lie at each site, and how many.</summary>
        public static (string Blueprint, int Count) FragmentsAt(string id)
        {
            switch (id)
            {
                case "garage":
                    return ("CargoCradle", 2);
                case "drill":
                    return ("BoostCoils", 3);
                case "kestrel":
                    return ("FocusLens", 3);
                case "lander":
                    return ("SideBins", 2);
                default:
                    return (string.Empty, 0);
            }
        }

        // How far inside the wreck's front edge the fragments' rests sit, and how far apart.
        private const float RestInset = 0.45f;
        private const float RestSpacing = 1.1f;

        // How far a pocket's middle sits out from the wreck's edge: close enough that its panel leans on the wreck.
        private const float PocketOutset = 0.3f;

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
            Bounds footprint = Footprint(root);
            AddCrewCrate(root, name, new Vector3(recipe.Heart.x + CrewCrateBesideHeart, 0f,
                footprint.max.z + CrewCrateClearOfWreck));
            AddRelicPockets(root, id, footprint);
            AddFragmentSockets(root, id, footprint);
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

        private static void AddFragmentSockets(ModelNode root, string id, Bounds footprint)
        {
            (string blueprint, int count) = FragmentsAt(id);
            for (int i = 0; i < count; i++)
            {
                Bounds fragment = FragmentBuilder.CreateFragment(blueprint, i).Children[0].Mesh.Geometry.Bounds;
                var at = new Vector3(footprint.center.x + (i - (count - 1) * 0.5f) * RestSpacing * (1f + fragment.size.x),
                    0f, footprint.max.z - RestInset - fragment.extents.z);
                string tag = blueprint + "_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                ModelNode rest = root.Add(new ModelNode(FragmentRestPrefix + tag, at, new ModelMesh(
                    $"Site_{id}_{FragmentRestPrefix}{tag}", ScanSiteMeshes.Rest(new Vector2(fragment.size.x,
                        fragment.size.z), 40 + i))));
                rest.Add(new ModelNode(FragmentSocketPrefix + tag, Vector3.up * ScanSiteMeshes.RestHeight));
            }
        }

        private static void AddRelicPockets(ModelNode root, string id, Bounds footprint)
        {
            IReadOnlyList<string> relics = RelicsAt(id);
            var edges = new[]
            {
                new Vector3(footprint.min.x - PocketOutset, 0f, footprint.center.z),
                new Vector3(footprint.center.x, 0f, footprint.min.z - PocketOutset),
                new Vector3(footprint.max.x + PocketOutset, 0f, footprint.center.z - footprint.extents.z * 0.4f),
            };
            for (int i = 0; i < relics.Count; i++)
            {
                Vector3 at = edges[i];
                Vector3 outward = at - new Vector3(footprint.center.x, 0f, footprint.center.z);
                ModelNode pocket = root.Add(new ModelNode(RelicPocketPrefix + relics[i], at,
                    Quaternion.LookRotation(outward.normalized, Vector3.up),
                    new ModelMesh($"Site_{id}_{RelicPocketPrefix}{i}", LootContainerMeshes.Pocket(1000 + i))));
                pocket.Add(new ModelNode(DigSpotName, LootContainerMeshes.DigSpot));
            }
        }

        private static void AddCrewCrate(ModelNode root, string name, Vector3 foot)
        {
            ModelNode crate = root.Add(new ModelNode(CrewCrateName, foot,
                new ModelMesh(name + "_" + CrewCrateName, SupplyShelfMeshes.CrewCrateFrame())));
            ModelNode lid = crate.Add(new ModelNode(CrewCrateLidName, SupplyShelfMeshes.LidSeat,
                new ModelMesh(name + "_" + CrewCrateName + CrewCrateLidName, SupplyShelfMeshes.CrewCrateLid())));
            lid.Add(new ModelNode(CutPointName, SupplyShelfMeshes.SealPoint, Facing(Vector3.forward)));
            lid.Add(new ModelNode(CrewCrateSealName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_" + CrewCrateName + CrewCrateSealName, SupplyShelfMeshes.CrewCrateSeal()),
                ModelMaterial.PaletteGlowOff));
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
