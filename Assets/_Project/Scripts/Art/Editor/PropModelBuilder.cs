using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Story props in Generated/Art/Props: <c>LogCache</c>, Ro's battered tin box at the canyon terminus (M3-05),
    /// a single-node model with its root on the ground at the box centre, +Z = the lid's front, lid ajar; and
    /// <c>OrangeBeadPatch_0..2</c>, the basin's orange glass bead drifts (M3-27 #4) for World to scatter on the old
    /// vent field, each a single node with its root on the ground at the patch centre.
    /// </summary>
    public static class PropModelBuilder
    {
        public const string LogCacheName = "LogCache";
        public const string OrangeBeadPatchPrefix = "OrangeBeadPatch_";
        public const int OrangeBeadPatchVariants = OrangeBeadMeshes.PatchVariants;

        /// <summary>
        /// A small rockfall slumped against a canyon alcove wall (0.5.5 Bell parts): its root on the ground, -Z against
        /// the wall, +Z its front with a dark hollow gap holding an empty <c>DigSpot</c> (+Y up) where a buried part lies.
        /// </summary>
        public const string RockfallPocketName = "RockfallPocket";

        public const string DigSpotName = SiteModelBuilder.DigSpotName;


        [MoonBuilder("Art/Props", 175)]
        public static void Build()
        {
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateLogCache()), ArtPaths.PropFolder,
                PaletteAssetBuilder.LoadMaterial());
            Material material = PaletteAssetBuilder.LoadMaterial();
            for (int i = 0; i < OrangeBeadPatchVariants; i++)
            {
                ModelPrefabWriter.Write(CreateOrangeBeadPatch(i), ArtPaths.PropFolder, material);
            }

            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateRockfallPocket()), ArtPaths.PropFolder, material);

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLogCache()
        {
            return new ModelNode(LogCacheName, Vector3.zero, new ModelMesh(LogCacheName, LogCacheMeshes.Cache()));
        }

        public static ModelNode CreateRockfallPocket()
        {
            var root = new ModelNode(RockfallPocketName, Vector3.zero,
                new ModelMesh(RockfallPocketName, LootContainerMeshes.RockfallPocket()));
            root.Add(new ModelNode(DigSpotName, LootContainerMeshes.RockfallDigSpot, Quaternion.identity));
            return root;
        }

        public static ModelNode CreateOrangeBeadPatch(int variant)
        {
            string name = OrangeBeadPatchPrefix + variant.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, OrangeBeadMeshes.Patch(variant)));
        }
    }
}

