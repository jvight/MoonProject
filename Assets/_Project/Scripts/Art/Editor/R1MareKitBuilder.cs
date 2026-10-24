using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Registered <c>Art/R1MareKit</c> builder (order 109, after <c>Art/R1StoryProps</c> at 108) that builds
    /// The Long Mare (<c>0.6</c>) specialized materials (<c>R1_MareBasalt</c>, <c>R1_MareSwirl</c>,
    /// <c>R1_MareEjectaRay</c>, <c>R1_Ilmenite</c>, <c>R1_TardigradeWindow</c>) and applies material and
    /// collider overrides to all Mare kit prefabs under <c>Assets/_Project/Generated/Art/R1/Models/Mare/</c>.
    /// </summary>
    public static class R1MareKitBuilder
    {
        public const string MareModelsSubfolder = R1MaterialRecipes.ModelsFolder + "/Mare";
        public const string GeneratedMareModelsSubfolder = R1MaterialRecipes.GeneratedModelsFolder + "/Mare";

        // Mare Ground & Decal Models
        public const string MareGroundPatchFbxPath = MareModelsSubfolder + "/MareGround_Patch.fbx";
        public const string DecalMareSwirlFbxPath = MareModelsSubfolder + "/Decal_MareSwirl.fbx";
        public const string DecalMareEjectaRayFbxPath = MareModelsSubfolder + "/Decal_MareEjectaRay.fbx";

        public const string MareGroundPatchPrefabPath = GeneratedMareModelsSubfolder + "/MareGround_Patch.prefab";
        public const string DecalMareSwirlPrefabPath = GeneratedMareModelsSubfolder + "/Decal_MareSwirl.prefab";
        public const string DecalMareEjectaRayPrefabPath = GeneratedMareModelsSubfolder + "/Decal_MareEjectaRay.prefab";

        // Mare Rock Kit (6 basalt boulders/outcrops + 2 Ilmenite outcrops)
        public static readonly string[] MareBasaltRockFbxPaths =
        {
            MareModelsSubfolder + "/MareRock_00.fbx",
            MareModelsSubfolder + "/MareRock_01.fbx",
            MareModelsSubfolder + "/MareRock_02.fbx",
            MareModelsSubfolder + "/MareRock_03.fbx",
            MareModelsSubfolder + "/MareRock_04.fbx",
            MareModelsSubfolder + "/MareRock_05.fbx",
        };

        public static readonly string[] MareBasaltRockPrefabPaths =
        {
            GeneratedMareModelsSubfolder + "/MareRock_00.prefab",
            GeneratedMareModelsSubfolder + "/MareRock_01.prefab",
            GeneratedMareModelsSubfolder + "/MareRock_02.prefab",
            GeneratedMareModelsSubfolder + "/MareRock_03.prefab",
            GeneratedMareModelsSubfolder + "/MareRock_04.prefab",
            GeneratedMareModelsSubfolder + "/MareRock_05.prefab",
        };

        public const string IlmeniteOutcrop00FbxPath = MareModelsSubfolder + "/MareOutcrop_Ilmenite_00.fbx";
        public const string IlmeniteOutcrop01FbxPath = MareModelsSubfolder + "/MareOutcrop_Ilmenite_01.fbx";
        public const string IlmeniteOutcrop00PrefabPath = GeneratedMareModelsSubfolder + "/MareOutcrop_Ilmenite_00.prefab";
        public const string IlmeniteOutcrop01PrefabPath = GeneratedMareModelsSubfolder + "/MareOutcrop_Ilmenite_01.prefab";

        // Rille Pass Modular Pieces (4 chunks)
        public const string RilleWallStraightFbxPath = MareModelsSubfolder + "/Rille_Wall_Straight.fbx";
        public const string RilleWallCurveFbxPath = MareModelsSubfolder + "/Rille_Wall_Curve.fbx";
        public const string RilleSlopeRampFbxPath = MareModelsSubfolder + "/Rille_Slope_Ramp.fbx";
        public const string RilleMouthCornerFbxPath = MareModelsSubfolder + "/Rille_Mouth_Corner.fbx";

        public const string RilleWallStraightPrefabPath = GeneratedMareModelsSubfolder + "/Rille_Wall_Straight.prefab";
        public const string RilleWallCurvePrefabPath = GeneratedMareModelsSubfolder + "/Rille_Wall_Curve.prefab";
        public const string RilleSlopeRampPrefabPath = GeneratedMareModelsSubfolder + "/Rille_Slope_Ramp.prefab";
        public const string RilleMouthCornerPrefabPath = GeneratedMareModelsSubfolder + "/Rille_Mouth_Corner.prefab";

        // Mare Derelicts (3 models)
        public const string DerelictMareConvoyFbxPath = MareModelsSubfolder + "/Derelict_MareConvoy.fbx";
        public const string DerelictMareSurveyDroneFbxPath = MareModelsSubfolder + "/Derelict_MareSurveyDrone.fbx";
        public const string DerelictTardigradeCapsuleFbxPath = MareModelsSubfolder + "/Derelict_TardigradeCapsule.fbx";

        public const string DerelictMareConvoyPrefabPath = GeneratedMareModelsSubfolder + "/Derelict_MareConvoy.prefab";
        public const string DerelictMareSurveyDronePrefabPath = GeneratedMareModelsSubfolder + "/Derelict_MareSurveyDrone.prefab";
        public const string DerelictTardigradeCapsulePrefabPath = GeneratedMareModelsSubfolder + "/Derelict_TardigradeCapsule.prefab";

        // Mare Shader & Material Asset Paths
        public const string MareDecalShaderName = "MoonProject/R1/MareDecal";
        public const string MareDecalShaderPath = "Assets/_Project/Shaders/R1/R1MareDecal.shader";

        public const string MareBasaltMatPath = R1MaterialRecipes.GeneratedMaterialsFolder + "/R1_MareBasalt.mat";
        public const string MareSwirlMatPath = R1MaterialRecipes.GeneratedMaterialsFolder + "/R1_MareSwirl.mat";
        public const string MareEjectaRayMatPath = R1MaterialRecipes.GeneratedMaterialsFolder + "/R1_MareEjectaRay.mat";
        public const string IlmeniteMatPath = R1MaterialRecipes.GeneratedMaterialsFolder + "/R1_Ilmenite.mat";
        public const string TardigradeWindowMatPath = R1MaterialRecipes.GeneratedMaterialsFolder + "/R1_TardigradeWindow.mat";

        // Mare Texture Asset Paths
        public const string MareBasaltAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareBasalt_Albedo.png";
        public const string MareBasaltNormalPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareBasalt_Normal.png";
        public const string MareBasaltMaskPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareBasalt_Mask.png";

        public const string MareSwirlAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareSwirl_Albedo.png";
        public const string MareSwirlNormalPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareSwirl_Normal.png";
        public const string MareSwirlMaskPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareSwirl_Mask.png";

        public const string MareEjectaRayAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareEjectaRay_Albedo.png";
        public const string MareEjectaRayNormalPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareEjectaRay_Normal.png";
        public const string MareEjectaRayMaskPath = R1MaterialRecipes.TexturesFolder + "/T_R1_MareEjectaRay_Mask.png";

        public const string IlmeniteAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_Ilmenite_Albedo.png";
        public const string IlmeniteNormalPath = R1MaterialRecipes.TexturesFolder + "/T_R1_Ilmenite_Normal.png";
        public const string IlmeniteMaskPath = R1MaterialRecipes.TexturesFolder + "/T_R1_Ilmenite_Mask.png";

        [MoonBuilder("Art/R1MareKit", 109)]
        public static void Build()
        {
            Shader litShader = R1MaterialLibrary.LoadShader();
            BuildMareMaterials(litShader);
            R1ModelBuilder.Build();
            AssetDatabase.SaveAssets();
        }

        public static Shader LoadMareDecalShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(MareDecalShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"R1MareDecal shader is missing at '{MareDecalShaderPath}' ({MareDecalShaderName}).");
            }

            return shader;
        }

        /// <summary>
        /// Builds all 5 Mare kit specialized materials (<c>R1_MareBasalt</c>, <c>R1_MareSwirl</c>,
        /// <c>R1_MareEjectaRay</c>, <c>R1_Ilmenite</c>, <c>R1_TardigradeWindow</c>) into
        /// <c>Assets/_Project/Generated/Art/R1/Materials/</c>.
        /// </summary>
        public static void BuildMareMaterials(Shader litShader)
        {
            if (litShader == null)
            {
                litShader = R1MaterialLibrary.LoadShader();
            }

            Shader decalShader = LoadMareDecalShader();

            // 1. R1_MareBasalt: dark blue-black flood-basalt (#1D2332..#2F384E) with subtle fine weathered texture (no checker grid)
            Material mareBasalt = CreateSurfaceMaterial(
                litShader,
                "R1_MareBasalt",
                MareBasaltAlbedoPath,
                MareBasaltNormalPath,
                MareBasaltMaskPath,
                bumpScale: 0.58f,
                metallicScale: 1.0f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.14f,
                smoothnessMax: 0.28f,
                edgeWearColor: new Color32(0x7E, 0x8C, 0xAC, 0xFF),
                edgeWearAmount: 0.50f,
                edgeWearMetallic: 0.08f,
                edgeWearSmoothness: 0.26f,
                dustColor: new Color32(0x5E, 0x6A, 0x86, 0xFF),
                dustAmount: 0.54f,
                dustSmoothness: 0.14f,
                colorVarScale: 0.55f,
                colorVarAmount: 0.065f);
            mareBasalt.SetColor("_BaseColor", new Color(1.78f, 1.84f, 1.98f, 1.0f));
            GeneratedAssets.CreateOrReplace(mareBasalt, MareBasaltMatPath);

            // 2. R1_MareSwirl: soft, sinuous, feathered pale swirl decal on dark basalt (alpha-blended, zero hard polygon edges)
            Material mareSwirl = CreateDecalMaterial(
                decalShader,
                "R1_MareSwirl",
                MareSwirlAlbedoPath,
                MareSwirlNormalPath,
                MareSwirlMaskPath,
                baseColor: new Color(0.95f, 0.97f, 1.00f, 1.00f),
                bumpScale: 0.35f,
                smoothness: 0.06f,
                dustColor: new Color32(0xA4, 0xAE, 0xC4, 0xFF),
                dustAmount: 0.16f,
                wearColor: new Color32(0xE8, 0xEE, 0xFA, 0xFF),
                wearAmount: 0.14f,
                opacity: 0.94f);
            GeneratedAssets.CreateOrReplace(mareSwirl, MareSwirlMatPath);

            // 3. R1_MareEjectaRay: soft, streaky radial ejecta dust rays (alpha-blended, zero hard polygon edges)
            Material mareEjectaRay = CreateDecalMaterial(
                decalShader,
                "R1_MareEjectaRay",
                MareEjectaRayAlbedoPath,
                MareEjectaRayNormalPath,
                MareEjectaRayMaskPath,
                baseColor: new Color(0.88f, 0.92f, 0.98f, 1.00f),
                bumpScale: 0.30f,
                smoothness: 0.06f,
                dustColor: new Color32(0x58, 0x62, 0x78, 0xFF),
                dustAmount: 0.18f,
                wearColor: new Color32(0xA0, 0xAA, 0xC0, 0xFF),
                wearAmount: 0.12f,
                opacity: 0.85f);
            GeneratedAssets.CreateOrReplace(mareEjectaRay, MareEjectaRayMatPath);

            // 4. R1_Ilmenite: natural FeTiO3 drill-target ore (#232834 base, calm bronze-gunmetal metallic sheen, non-emissive)
            Material ilmenite = CreateSurfaceMaterial(
                litShader,
                "R1_Ilmenite",
                IlmeniteAlbedoPath,
                IlmeniteNormalPath,
                IlmeniteMaskPath,
                bumpScale: 0.65f,
                metallicScale: 1.0f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.35f,
                smoothnessMax: 0.65f,
                edgeWearColor: new Color32(0xAC, 0x92, 0x74, 0xFF),
                edgeWearAmount: 0.54f,
                edgeWearMetallic: 0.90f,
                edgeWearSmoothness: 0.62f,
                dustColor: new Color32(0x46, 0x50, 0x66, 0xFF),
                dustAmount: 0.24f,
                dustSmoothness: 0.12f,
                colorVarScale: 0.85f,
                colorVarAmount: 0.035f);
            ilmenite.SetColor("_BaseColor", new Color(1.58f, 1.52f, 1.46f, 1.0f));
            GeneratedAssets.CreateOrReplace(ilmenite, IlmeniteMatPath);

            // 5. R1_TardigradeWindow: tiny warm amber dormant life-support viewport (bloom-safe < Palette.BloomThreshold)
            R1MaterialSpec lampSpec = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber);
            Material windowMat = R1MaterialLibrary.CreateMaterial(litShader, lampSpec);
            windowMat.name = "R1_TardigradeWindow";
            windowMat.SetColor("_BaseColor", new Color32(0xFF, 0xC4, 0x6B, 0xFF));
            windowMat.SetColor("_EmissionColor", new Color32(0xFF, 0xB8, 0x4D, 0xFF));
            windowMat.SetFloat("_EmissionStrength", 0.72f);
            windowMat.SetFloat("_BloomSafeMax", 1.02f);
            windowMat.SetFloat("_HotCoreThreshold", 0.88f);
            windowMat.SetFloat("_HotCorePeak", 1.12f);
            windowMat.SetFloat("_DustAmount", 0.06f);
            GeneratedAssets.CreateOrReplace(windowMat, TardigradeWindowMatPath);
        }

        /// <summary>
        /// Applies Mare-specific material overrides, shadow-casting flags, and colliders to Mare kit prefabs
        /// during <see cref="R1ModelBuilder.Build"/>.
        /// </summary>
        public static void ApplyMareKitOverrides(GameObject instance, string modelPath)
        {
            if (instance == null || string.IsNullOrEmpty(modelPath))
            {
                return;
            }

            string normalized = modelPath.Replace('\\', '/');
            if (!normalized.StartsWith(MareModelsSubfolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = Path.GetFileName(normalized);

            if (string.Equals(fileName, "MareGround_Patch.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                ReplaceAllRenderersMaterial(instance, mareBasalt);
                AddBoxColliderToBounds(instance);
                return;
            }

            if (string.Equals(fileName, "Decal_MareSwirl.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material swirlMat = LoadRequiredMaterial(MareSwirlMatPath);
                ConfigureGroundDecal(instance, swirlMat);
                return;
            }

            if (string.Equals(fileName, "Decal_MareEjectaRay.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material rayMat = LoadRequiredMaterial(MareEjectaRayMatPath);
                ConfigureGroundDecal(instance, rayMat);
                return;
            }

            if (fileName.StartsWith("MareRock_", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                ReplaceAllRenderersMaterial(instance, mareBasalt);
                AddBoxColliderToBounds(instance);
                return;
            }

            if (fileName.StartsWith("MareOutcrop_Ilmenite_", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                Material ilmenite = LoadRequiredMaterial(IlmeniteMatPath);
                ReplaceChildMaterial(instance, "HostRock", mareBasalt);
                ReplaceChildMaterial(instance, "IlmeniteVein", ilmenite);
                AddBoxColliderToBounds(instance);
                return;
            }

            if (fileName.StartsWith("Rille_", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                ReplaceAllRenderersMaterial(instance, mareBasalt);
                AddMeshCollidersToChildren(instance);
                return;
            }

            if (string.Equals(fileName, "Derelict_MareConvoy.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                ReplaceChildMaterial(instance, "WheelRuts", mareBasalt);
                AddBoxColliderToBounds(instance);
                return;
            }

            if (string.Equals(fileName, "Derelict_MareSurveyDrone.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                ReplaceChildMaterial(instance, "ImpactFurrow", mareBasalt);
                AddBoxColliderToBounds(instance);
                return;
            }

            if (string.Equals(fileName, "Derelict_TardigradeCapsule.fbx", StringComparison.OrdinalIgnoreCase))
            {
                Material mareBasalt = LoadRequiredMaterial(MareBasaltMatPath);
                Material windowMat = LoadRequiredMaterial(TardigradeWindowMatPath);
                ReplaceChildMaterial(instance, "BurialMound", mareBasalt);
                ReplaceChildMaterial(instance, "TinyWindow", windowMat);
                AddBoxColliderToBounds(instance);
            }
        }

        private static Material CreateSurfaceMaterial(
            Shader litShader,
            string name,
            string albedoPath,
            string normalPath,
            string maskPath,
            float bumpScale,
            float metallicScale,
            float smoothnessScale,
            float smoothnessMin,
            float smoothnessMax,
            Color32 edgeWearColor,
            float edgeWearAmount,
            float edgeWearMetallic,
            float edgeWearSmoothness,
            Color32 dustColor,
            float dustAmount,
            float dustSmoothness,
            float colorVarScale,
            float colorVarAmount)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if (albedo == null || normal == null || mask == null)
            {
                throw new InvalidOperationException(
                    $"R1MareKitBuilder: Missing required texture(s) for '{name}' at '{albedoPath}', '{normalPath}', or '{maskPath}'.");
            }

            var mat = new Material(litShader)
            {
                name = name,
                enableInstancing = true,
            };

            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", bumpScale);
            mat.SetTexture("_MaskMap", mask);
            mat.SetFloat("_MetallicScale", metallicScale);
            mat.SetFloat("_SmoothnessScale", smoothnessScale);
            mat.SetFloat("_SmoothnessMin", smoothnessMin);
            mat.SetFloat("_SmoothnessMax", smoothnessMax);
            mat.SetFloat("_OcclusionStrength", 1.0f);

            mat.SetColor("_EdgeWearColor", edgeWearColor);
            mat.SetFloat("_EdgeWearAmount", edgeWearAmount);
            mat.SetFloat("_EdgeWearThreshold", 0.22f);
            mat.SetFloat("_EdgeWearSharpness", 2.0f);
            mat.SetFloat("_EdgeWearMetallic", edgeWearMetallic);
            mat.SetFloat("_EdgeWearSmoothness", edgeWearSmoothness);

            mat.SetColor("_DustColor", dustColor);
            mat.SetFloat("_DustAmount", dustAmount);
            mat.SetFloat("_DustSharpness", 2.2f);
            mat.SetFloat("_DustUpThreshold", 0.15f);
            mat.SetFloat("_DustOcclusionWeight", 0.45f);
            mat.SetFloat("_DustSmoothness", dustSmoothness);

            mat.SetFloat("_ColorVarScale", colorVarScale);
            mat.SetFloat("_ColorVarAmount", colorVarAmount);

            mat.SetColor("_EmissionColor", Color.black);
            mat.SetFloat("_EmissionStrength", 0.0f);
            return mat;
        }

        private static Material CreateDecalMaterial(
            Shader decalShader,
            string name,
            string albedoPath,
            string normalPath,
            string maskPath,
            Color baseColor,
            float bumpScale,
            float smoothness,
            Color32 dustColor,
            float dustAmount,
            Color32 wearColor,
            float wearAmount,
            float opacity)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if (albedo == null || normal == null || mask == null)
            {
                throw new InvalidOperationException(
                    $"R1MareKitBuilder: Missing required texture(s) for '{name}' at '{albedoPath}', '{normalPath}', or '{maskPath}'.");
            }

            var mat = new Material(decalShader)
            {
                name = name,
                enableInstancing = true,
            };

            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", bumpScale);
            mat.SetTexture("_MaskMap", mask);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetColor("_DustColor", dustColor);
            mat.SetFloat("_DustAmount", dustAmount);
            mat.SetColor("_WearColor", wearColor);
            mat.SetFloat("_WearAmount", wearAmount);
            mat.SetFloat("_AOStrength", 0.30f);
            mat.SetFloat("_MicroVariation", 0.08f);
            mat.SetFloat("_Opacity", opacity);
            mat.SetColor("_EmissionColor", Color.black);
            mat.SetFloat("_EmissionStrength", 0.0f);
            return mat;
        }

        private static Material LoadRequiredMaterial(string matPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                BuildMareMaterials(R1MaterialLibrary.LoadShader());
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            if (mat == null)
            {
                throw new InvalidOperationException(
                    $"R1MareKitBuilder: Required material missing at '{matPath}'. Run Art/R1Materials first.");
            }

            return mat;
        }

        private static void ConfigureGroundDecal(GameObject instance, Material decalMat)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                var mats = new Material[r.sharedMaterials.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = decalMat;
                }

                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(colliders[i]);
            }
        }

        private static void ReplaceAllRenderersMaterial(GameObject instance, Material targetMat)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r.name.StartsWith("Decal_", StringComparison.Ordinal))
                {
                    continue;
                }

                var mats = new Material[r.sharedMaterials.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = targetMat;
                }

                r.sharedMaterials = mats;
            }
        }

        private static void ReplaceChildMaterial(GameObject root, string childName, Material targetMat)
        {
            Transform child = FindChildRecursive(root.transform, childName);
            if (child == null)
            {
                return;
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = targetMat;
            }

            renderer.sharedMaterials = mats;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (string.Equals(c.name, name, StringComparison.Ordinal))
                {
                    return c;
                }

                Transform found = FindChildRecursive(c, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void AddBoxColliderToBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = root.AddComponent<BoxCollider>();
            }

            box.center = root.transform.InverseTransformPoint(b.center);
            box.size = b.size;
        }

        private static void AddMeshCollidersToChildren(GameObject root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf.sharedMesh == null || mf.name.StartsWith("Decal_", StringComparison.Ordinal))
                {
                    continue;
                }

                var mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = mf.gameObject.AddComponent<MeshCollider>();
                }

                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }
        }
    }
}
