using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Registered <c>Art/R1Models</c> builder (order 106, after <c>Art/R1Materials</c> at 105). Wraps each FBX under
    /// <c>Assets/_Project/Art/R1/Models/</c> into <c>Assets/_Project/Generated/Art/R1/Models/&lt;same relative path&gt;.prefab</c>
    /// using <see cref="GeneratedAssets.SavePrefab"/>, replacing every renderer's material with the generated library
    /// material matching the source material slot name, and throwing <see cref="InvalidOperationException"/> on unknown
    /// slot names.
    /// </summary>
    public static class R1ModelBuilder
    {
        /// <summary>
        /// Scans <c>Assets/_Project/Art/R1/Models/</c> for FBX assets and writes a generated prefab for each one into
        /// <c>Assets/_Project/Generated/Art/R1/Models/</c>. With no FBX files in the folder, writes nothing and succeeds.
        /// </summary>
        [MoonBuilder("Art/R1Models", 106)]
        public static void Build()
        {
            if (!Directory.Exists(R1MaterialRecipes.ModelsFolder))
            {
                return;
            }

            string[] fbxFiles = Directory.GetFiles(
                R1MaterialRecipes.ModelsFolder,
                "*.fbx",
                SearchOption.AllDirectories);
            if (fbxFiles.Length == 0)
            {
                return;
            }

            Array.Sort(fbxFiles, StringComparer.Ordinal);
            for (int i = 0; i < fbxFiles.Length; i++)
            {
                string modelPath = NormalizePath(fbxFiles[i]);
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                {
                    throw new InvalidOperationException(
                        $"R1ModelBuilder: Failed to load FBX model asset at '{modelPath}'.");
                }

                string prefabPath = GetGeneratedPrefabPath(modelPath);
                var instance = UnityEngine.Object.Instantiate(modelAsset);
                instance.name = modelAsset.name;
                try
                {
                    AssignLibraryMaterials(instance, modelPath);
                    R1StoryPropsBuilder.ApplyStoryPropOverrides(instance, modelPath);
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    throw;
                }

                GeneratedAssets.SavePrefab(instance, prefabPath);
            }
        }

        /// <summary>
        /// Pure slot-mapping function that maps a source material slot name to its <see cref="R1MaterialSlot"/>, or
        /// throws <see cref="InvalidOperationException"/> (containing <paramref name="modelPath"/> and
        /// <paramref name="rawSlotName"/>) when the slot name is unknown.
        /// </summary>
        public static R1MaterialSlot MapSlotOrThrow(string rawSlotName, string modelPath)
        {
            if (R1MaterialRecipes.TryMapSlotName(rawSlotName, out R1MaterialSlot slot))
            {
                return slot;
            }

            string cleanSlot = R1MaterialRecipes.NormalizeSlotName(rawSlotName);
            throw new InvalidOperationException(
                $"R1ModelBuilder: Model '{modelPath}' uses unknown material slot '{cleanSlot}'. " +
                "All material slots must match one of the 12 fixed R1_* names in R1MaterialRecipes.");
        }

        /// <summary>
        /// Computes the generated prefab output path under <c>Assets/_Project/Generated/Art/R1/Models/</c> for a source
        /// FBX path under <c>Assets/_Project/Art/R1/Models/</c>.
        /// </summary>
        public static string GetGeneratedPrefabPath(string modelAssetPath)
        {
            string normalized = NormalizePath(modelAssetPath);
            string prefix = R1MaterialRecipes.ModelsFolder + "/";
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Model path '{modelAssetPath}' must be under '{prefix}'.",
                    nameof(modelAssetPath));
            }

            string relative = normalized.Substring(prefix.Length);
            string relativePrefab = Path.ChangeExtension(relative, ".prefab")?.Replace('\\', '/');
            return R1MaterialRecipes.GeneratedModelsFolder + "/" + relativePrefab;
        }

        /// <summary>
        /// Replaces every <see cref="Renderer"/>'s <c>sharedMaterials</c> under <paramref name="root"/> with the
        /// generated library <see cref="Material"/> matching each source material's slot name. Throws
        /// <see cref="InvalidOperationException"/> (including <paramref name="modelPath"/> and slot name) on any
        /// unknown slot name.
        /// </summary>
        public static void AssignLibraryMaterials(
            GameObject root,
            string modelPath,
            Func<R1MaterialSlot, Material> materialLoader = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Func<R1MaterialSlot, Material> loadMat = materialLoader ?? R1MaterialLibrary.LoadMaterial;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                Material[] sourceMats = renderer.sharedMaterials;
                if (sourceMats == null || sourceMats.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"R1ModelBuilder: Renderer '{renderer.name}' in model '{modelPath}' has no material slots assigned.");
                }

                var mappedMats = new Material[sourceMats.Length];
                for (int i = 0; i < sourceMats.Length; i++)
                {
                    string rawName = sourceMats[i] != null ? sourceMats[i].name : string.Empty;
                    R1MaterialSlot slot = MapSlotOrThrow(rawName, modelPath);
                    Material libraryMat = loadMat(slot);
                    if (libraryMat == null)
                    {
                        throw new InvalidOperationException(
                            $"R1ModelBuilder: Library material for slot '{slot}' in model '{modelPath}' is null.");
                    }

                    mappedMats[i] = libraryMat;
                }

                renderer.sharedMaterials = mappedMats;
            }

            R1ModelPostprocessor.DisableCollisionProxyRenderers(root);
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
