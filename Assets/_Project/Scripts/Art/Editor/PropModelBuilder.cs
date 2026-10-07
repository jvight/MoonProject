using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Story props in Generated/Art/Props: <c>LogCache</c>, Ro's battered tin box at the canyon terminus (M3-05),
    /// a single-node model with its root on the ground at the box centre, +Z = the lid's front, lid ajar.
    /// </summary>
    public static class PropModelBuilder
    {
        public const string LogCacheName = "LogCache";

        [MoonBuilder("Art/Props", 175)]
        public static void Build()
        {
            ModelPrefabWriter.Write(CreateLogCache(), ArtPaths.PropFolder, PaletteAssetBuilder.LoadMaterial());
            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLogCache()
        {
            return new ModelNode(LogCacheName, Vector3.zero, new ModelMesh(LogCacheName, LogCacheMeshes.Cache()));
        }
    }
}
