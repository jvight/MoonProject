using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Registered <c>Art/R1Fauna</c> builder (order 108, after <c>Art/R1Models</c> at 106 and <c>Art/R1Ssr3</c> at 107).
    /// Builds the shared <see cref="FaunaMotionTuning"/> asset (<c>Assets/_Project/Generated/Art/R1/FaunaMotionTuning.asset</c>),
    /// the Tycho survey drone's cyan status lamp material (<c>R1_FaunaLampCyan.mat</c>), and wires
    /// <see cref="FaunaIdleMotion"/> onto the four 0.6 machine fauna prefabs under
    /// <c>Assets/_Project/Generated/Art/R1/Models/Fauna/</c>.
    /// </summary>
    public static class R1FaunaBuilder
    {
        public const string FaunaSourceFolder = R1MaterialRecipes.ModelsFolder + "/Fauna";
        public const string FaunaGeneratedFolder = ArtPaths.R1ModelsFolder + "/Fauna";

        public const string TuningAssetPath = ArtPaths.R1Root + "/FaunaMotionTuning.asset";

        public const string FaunaLampCyanMaterialName = "R1_FaunaLampCyan";
        public const string FaunaLampCyanMaterialPath = ArtPaths.R1MaterialsFolder + "/" + FaunaLampCyanMaterialName + ".mat";

        public const string PanelMiteFbxPath = FaunaSourceFolder + "/PanelMite.fbx";
        public const string SweeperFbxPath = FaunaSourceFolder + "/Sweeper.fbx";
        public const string SurveyDroneFbxPath = FaunaSourceFolder + "/SurveyDrone.fbx";
        public const string TrencherFbxPath = FaunaSourceFolder + "/Trencher.fbx";

        public const string PanelMitePrefabPath = FaunaGeneratedFolder + "/PanelMite.prefab";
        public const string SweeperPrefabPath = FaunaGeneratedFolder + "/Sweeper.prefab";
        public const string SurveyDronePrefabPath = FaunaGeneratedFolder + "/SurveyDrone.prefab";
        public const string TrencherPrefabPath = FaunaGeneratedFolder + "/Trencher.prefab";

        /// <summary>
        /// Builds <c>FaunaMotionTuning.asset</c>, <c>R1_FaunaLampCyan.mat</c>, and the four generated fauna prefabs
        /// (<c>PanelMite.prefab</c>, <c>Sweeper.prefab</c>, <c>SurveyDrone.prefab</c>, <c>Trencher.prefab</c>).
        /// </summary>
        [MoonBuilder("Art/R1Fauna", 108)]
        public static void Build()
        {
            BuildTuningAsset();
            BuildCyanLampMaterial();

            BuildFaunaPrefab(PanelMiteFbxPath, PanelMitePrefabPath);
            BuildFaunaPrefab(SweeperFbxPath, SweeperPrefabPath);
            BuildFaunaPrefab(SurveyDroneFbxPath, SurveyDronePrefabPath);
            BuildFaunaPrefab(TrencherFbxPath, TrencherPrefabPath);
        }

        /// <summary>
        /// Creates or replaces the shared <see cref="FaunaMotionTuning"/> asset at <see cref="TuningAssetPath"/>.
        /// </summary>
        public static FaunaMotionTuning BuildTuningAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<FaunaMotionTuning>(TuningAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var tuning = ScriptableObject.CreateInstance<FaunaMotionTuning>();
            tuning.name = "FaunaMotionTuning";
            return GeneratedAssets.CreateOrReplace(tuning, TuningAssetPath);
        }

        /// <summary>
        /// Loads the shared <see cref="FaunaMotionTuning"/> asset at <see cref="TuningAssetPath"/>, building it first if missing.
        /// </summary>
        public static FaunaMotionTuning LoadOrBuildTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<FaunaMotionTuning>(TuningAssetPath);
            return tuning != null ? tuning : BuildTuningAsset();
        }

        /// <summary>
        /// Creates or replaces <c>R1_FaunaLampCyan.mat</c> for the Tycho <c>SurveyDrone</c> status lamp.
        /// Uses <c>MoonProject/R1/Lit</c> with a bloom-safe cyan body (<c>_BloomSafeMax &lt; Palette.BloomThreshold</c>)
        /// and a hot core above threshold, modulated at runtime through <c>_EmissionStrength</c>.
        /// </summary>
        public static Material BuildCyanLampMaterial()
        {
            Shader r1Lit = R1MaterialLibrary.LoadShader();
            R1MaterialSpec lampSpec = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber);
            Material cyanMat = R1MaterialLibrary.CreateMaterial(r1Lit, lampSpec);
            cyanMat.name = FaunaLampCyanMaterialName;
            cyanMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            cyanMat.SetColor("_BaseColor", new Color(0.24f, 0.72f, 0.86f, 1f));
            cyanMat.SetColor("_EmissionColor", new Color(0.10f, 0.96f, 2.80f, 1f));
            cyanMat.SetFloat("_EmissionStrength", 1.00f);
            cyanMat.SetFloat("_BloomSafeMax", 0.96f);
            cyanMat.SetFloat("_HotCoreThreshold", 0.86f);
            cyanMat.SetFloat("_HotCorePeak", 0.78f);
            return GeneratedAssets.CreateOrReplace(cyanMat, FaunaLampCyanMaterialPath);
        }

        /// <summary>
        /// Loads <c>R1_FaunaLampCyan.mat</c>, building it first if missing.
        /// </summary>
        public static Material LoadOrBuildCyanLampMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FaunaLampCyanMaterialPath);
            return mat != null ? mat : BuildCyanLampMaterial();
        }

        /// <summary>
        /// Applies fauna-specific material overrides (cyan lamp on <c>SurveyDrone</c>) and wires
        /// <see cref="FaunaIdleMotion"/> on the instantiated fauna hierarchy before <see cref="R1ModelBuilder"/>
        /// saves the prefab.
        /// </summary>
        public static void ApplyFaunaOverrides(GameObject root, string modelPath)
        {
            if (root == null || string.IsNullOrEmpty(modelPath))
            {
                return;
            }

            if (!TryGetSpeciesFromModelPath(modelPath, out FaunaSpecies species))
            {
                return;
            }

            if (species == FaunaSpecies.SurveyDrone)
            {
                Transform lamp = FindChildRecursive(root.transform, "Lamp");
                if (lamp == null || !lamp.TryGetComponent(out Renderer lampRenderer))
                {
                    throw new InvalidOperationException(
                        $"R1FaunaBuilder: SurveyDrone at '{modelPath}' is missing required 'Lamp' Renderer.");
                }

                lampRenderer.sharedMaterial = LoadOrBuildCyanLampMaterial();
            }

            FaunaMotionTuning tuning = LoadOrBuildTuning();
            var motion = root.GetComponent<FaunaIdleMotion>();
            if (motion == null)
            {
                motion = root.AddComponent<FaunaIdleMotion>();
            }

            motion.ConfigureFromHierarchy(species, tuning);
        }

        /// <summary>
        /// Maps a fauna FBX asset path to its <see cref="FaunaSpecies"/>.
        /// </summary>
        public static bool TryGetSpeciesFromModelPath(string modelPath, out FaunaSpecies species)
        {
            if (!string.IsNullOrEmpty(modelPath))
            {
                if (modelPath.EndsWith("/PanelMite.fbx", StringComparison.Ordinal))
                {
                    species = FaunaSpecies.PanelMite;
                    return true;
                }

                if (modelPath.EndsWith("/Sweeper.fbx", StringComparison.Ordinal))
                {
                    species = FaunaSpecies.Sweeper;
                    return true;
                }

                if (modelPath.EndsWith("/SurveyDrone.fbx", StringComparison.Ordinal))
                {
                    species = FaunaSpecies.SurveyDrone;
                    return true;
                }

                if (modelPath.EndsWith("/Trencher.fbx", StringComparison.Ordinal))
                {
                    species = FaunaSpecies.Trencher;
                    return true;
                }
            }

            species = default;
            return false;
        }

        private static void BuildFaunaPrefab(string fbxPath, string prefabPath)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException(
                    $"R1FaunaBuilder: Failed to load fauna FBX model asset at '{fbxPath}'.");
            }

            var instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.name = modelAsset.name;
            try
            {
                R1ModelBuilder.AssignLibraryMaterials(instance, fbxPath);
                R1StoryPropsBuilder.ApplyStoryPropOverrides(instance, fbxPath);
                ApplyFaunaOverrides(instance, fbxPath);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }

            GeneratedAssets.SavePrefab(instance, prefabPath);
        }

        private static Transform FindChildRecursive(Transform parent, string targetName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, targetName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
