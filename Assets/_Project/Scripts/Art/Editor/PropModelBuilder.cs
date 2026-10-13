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
        /// Kestrel-3's hull fragments for World's scrap field (0.5.5 §2): <c>HullFragment_0..3</c>, a curved skin on its
        /// ribs, a torn plate, a panel with its hatch and a bare rib cage, each about 2 m across with its root where the
        /// dust line crosses its middle (place it on the ground; what's below y 0 is buried), +Z its exposed side,
        /// and a <c>CutPoint</c> (+Z out of its cut face) on the edge 07 cuts.
        /// </summary>
        public const string HullFragmentPrefix = "HullFragment_";

        public const int HullFragmentVariants = HullFragmentMeshes.Variants;

        /// <summary>
        /// Each hull fragment's <c>FragmentRest</c> beside its exposed side holds a <c>FragmentSocket</c>, where World
        /// lays the Warm Headlamp's fragments (§1.3) among the scrap field's plates.
        /// </summary>
        public const string FragmentRestName = "FragmentRest";

        public const string FragmentSocketName = "FragmentSocket";

        /// <summary>
        /// Kenji's crashed test jumper at the chasm's near lip (0.5.5 §1.3), <c>JumperWreck</c>: its root on the ground,
        /// +Z its nose toward the chasm, holding <c>FragmentSocket_HoverJump_0..2</c> on its frame and the rubble.
        /// </summary>
        public const string JumperWreckName = "JumperWreck";

        // Where a hull fragment's rest sits: out from its exposed side.
        private static readonly Vector3 HullRest = new Vector3(0.2f, 0f, 1.25f);

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

            for (int i = 0; i < HullFragmentVariants; i++)
            {
                ModelPrefabWriter.Write(CreateHullFragment(i), ArtPaths.PropFolder, material);
            }

            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateJumperWreck()), ArtPaths.PropFolder, material);

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLogCache()
        {
            return new ModelNode(LogCacheName, Vector3.zero, new ModelMesh(LogCacheName, LogCacheMeshes.Cache()));
        }

        public static ModelNode CreateHullFragment(int variant)
        {
            string name = HullFragmentPrefix + variant.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var root = new ModelNode(name, Vector3.zero, new ModelMesh(name, HullFragmentMeshes.Fragment(variant)));
            root.Add(new ModelNode(SiteModelBuilder.CutPointName, HullFragmentMeshes.CutPoint(variant)));
            ModelNode rest = root.Add(new ModelNode(FragmentRestName, HullRest, new ModelMesh(name + "_" + FragmentRestName,
                ScanSiteMeshes.Rest(new Vector2(0.5f, 0.5f), 70 + variant))));
            rest.Add(new ModelNode(FragmentSocketName, Vector3.up * ScanSiteMeshes.RestHeight));
            return root;
        }

        public static ModelNode CreateJumperWreck()
        {
            var root = new ModelNode(JumperWreckName, Vector3.zero,
                new ModelMesh(JumperWreckName, ScanSiteMeshes.JumperWreck()));
            for (int i = 0; i < ScanSiteMeshes.JumperSockets.Length; i++)
            {
                root.Add(new ModelNode(SiteModelBuilder.FragmentSocketPrefix + "HoverJump_" + i,
                    ScanSiteMeshes.JumperSockets[i]));
            }

            return root;
        }

        public static ModelNode CreateOrangeBeadPatch(int variant)
        {
            string name = OrangeBeadPatchPrefix + variant.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, OrangeBeadMeshes.Patch(variant)));
        }
    }
}
