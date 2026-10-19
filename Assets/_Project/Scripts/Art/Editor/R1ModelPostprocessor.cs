using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Enforces R1 import rules on Blender FBX models under <c>Assets/_Project/Art/R1/Models/</c> and texture maps
    /// under <c>Assets/_Project/Art/R1/Textures/</c>. Validates source material slot names via
    /// <see cref="OnPreprocessMaterialDescription"/> and baked vertex colour attributes via
    /// <see cref="OnPostprocessModel"/>, logging <see cref="Debug.LogError(object)"/> on violations.
    /// </summary>
    public sealed class R1ModelPostprocessor : AssetPostprocessor
    {
        public const uint PostprocessorVersion = 4;
        public const string CollisionProxyPrefix = "UCX_";
        public const string ModelsPrefix = "Assets/_Project/Art/R1/Models/";
        public const string RocksPrefix = "Assets/_Project/Art/R1/Models/Rocks/";
        public const string TexturesPrefix = "Assets/_Project/Art/R1/Textures/";
        public const string MakerDecalsTexturePath = "Assets/_Project/Art/R1/Brands/T_R1_MakerDecals_Albedo.png";

        public override uint GetVersion()
        {
            return PostprocessorVersion;
        }

        /// <summary>Returns true when <paramref name="assetPath"/> is an FBX model inside the R1 models folder.</summary>
        public static bool IsR1ModelPath(string assetPath)
        {
            string normalized = NormalizePath(assetPath);
            return normalized.StartsWith(ModelsPrefix, StringComparison.Ordinal)
                && normalized.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true when <paramref name="assetPath"/> is a PNG texture inside the R1 textures folder or the maker decal atlas.</summary>
        public static bool IsR1TexturePath(string assetPath)
        {
            string normalized = NormalizePath(assetPath);
            if (normalized.Equals(MakerDecalsTexturePath, StringComparison.Ordinal))
            {
                return true;
            }

            return normalized.StartsWith(TexturesPrefix, StringComparison.Ordinal)
                && normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates that <paramref name="rawSlotName"/> matches one of the 12 fixed R1 material slot names in
        /// <see cref="R1MaterialRecipes"/>. Logs <see cref="Debug.LogError(object)"/> (including
        /// <paramref name="modelPath"/> and <paramref name="rawSlotName"/>) and returns false when unknown.
        /// </summary>
        public static bool ValidateSlotName(string rawSlotName, string modelPath)
        {
            string cleanSlot = R1MaterialRecipes.NormalizeSlotName(rawSlotName);
            if (!R1MaterialRecipes.TryGetBySlotName(cleanSlot, out _))
            {
                Debug.LogError(
                    $"R1ModelPostprocessor: Model '{modelPath}' uses unknown Blender material slot '{cleanSlot}'. " +
                    "All slots must match one of the 12 fixed R1_* names in R1MaterialRecipes.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that every mesh on <paramref name="root"/> has baked vertex colours (<c>mesh.colors32.Length &gt; 0</c>)
        /// for R1 AO/curvature/cavity shading. Logs <see cref="Debug.LogError(object)"/> with <paramref name="modelPath"/>
        /// when any mesh is missing vertex colours.
        /// </summary>
        public static bool ValidateVertexColors(GameObject root, string modelPath)
        {
            if (root == null)
            {
                return true;
            }

            bool allValid = true;
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i].sharedMesh;
                if (mesh != null && !HasVertexColors(mesh))
                {
                    Debug.LogError(
                        $"R1ModelPostprocessor: Mesh '{mesh.name}' in model '{modelPath}' has no vertex colours " +
                        "(mesh.colors32.Length == 0). Bake R=AO, G=convex curvature, B=cavity dust in Blender before exporting FBX.");
                    allValid = false;
                }
            }

            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                Mesh mesh = skinnedRenderers[i].sharedMesh;
                if (mesh != null && !HasVertexColors(mesh))
                {
                    Debug.LogError(
                        $"R1ModelPostprocessor: Skinned mesh '{mesh.name}' in model '{modelPath}' has no vertex colours " +
                        "(mesh.colors32.Length == 0). Bake R=AO, G=convex curvature, B=cavity dust in Blender before exporting FBX.");
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>Returns true when <paramref name="mesh"/> contains a non-empty vertex colour stream.</summary>
        public static bool HasVertexColors(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            Color32[] colors = mesh.colors32;
            return colors != null && colors.Length > 0;
        }

        private void OnPreprocessTexture()
        {
            if (!IsR1TexturePath(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            string normalized = NormalizePath(assetPath);
            bool isMakerDecal = normalized.Equals(MakerDecalsTexturePath, StringComparison.Ordinal);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool isNormal = name.EndsWith("_Normal", StringComparison.OrdinalIgnoreCase);
            bool isMask = name.EndsWith("_Mask", StringComparison.OrdinalIgnoreCase);

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormal && !isMask;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            if (isMakerDecal)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Repeat;
            }

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
        }

        private void OnPreprocessModel()
        {
            if (!IsR1ModelPath(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = true;
            importer.isReadable = NormalizePath(assetPath).StartsWith(RocksPrefix, StringComparison.Ordinal);
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
        }

        private void OnPreprocessMaterialDescription(
            MaterialDescription description,
            Material material,
            AnimationClip[] materialAnimation)
        {
            if (!IsR1ModelPath(assetPath))
            {
                return;
            }

            string slotName = description.materialName;
            if (material != null && !string.IsNullOrEmpty(slotName))
            {
                material.name = slotName;
            }

            ValidateSlotName(slotName, NormalizePath(assetPath));
        }

        /// <summary>
        /// Disables rendering and shadow casting on authored <c>UCX_*</c> collision proxy child meshes while keeping
        /// their <see cref="MeshFilter"/> intact for collider builders.
        /// </summary>
        public static void DisableCollisionProxyRenderers(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.name.StartsWith(CollisionProxyPrefix, StringComparison.Ordinal))
                {
                    renderer.enabled = false;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsR1ModelPath(assetPath))
            {
                return;
            }

            DisableCollisionProxyRenderers(root);
            ValidateVertexColors(root, NormalizePath(assetPath));
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
