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

        // Mare Material Asset Paths
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

            // 1. R1_MareBasalt: dark black-blue flood-basalt regolith (#101420..#222B40) with tiling-break detail
            Material mareBasalt = CreateSurfaceMaterial(
                litShader,
                "R1_MareBasalt",
                MareBasaltAlbedoPath,
                MareBasaltNormalPath,
                MareBasaltMaskPath,
                bumpScale: 1.15f,
                metallicScale: 1.0f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.08f,
                smoothnessMax: 0.24f,
                edgeWearColor: new Color32(0x46, 0x52, 0x6C, 0xFF),
                edgeWearAmount: 0.34f,
                edgeWearMetallic: 0.08f,
                edgeWearSmoothness: 0.22f,
                dustColor: new Color32(0x36, 0x40, 0x58, 0xFF),
                dustAmount: 0.28f,
                dustSmoothness: 0.10f,
                colorVarScale: 0.62f,
                colorVarAmount: 0.085f);
            GeneratedAssets.CreateOrReplace(mareBasalt, MareBasaltMatPath);

            // 2. R1_MareSwirl: high-albedo magnetically shielded lunar swirl ribbon (#D0D6E4), matte, zero emission
            Material mareSwirl = CreateSurfaceMaterial(
                litShader,
                "R1_MareSwirl",
                MareSwirlAlbedoPath,
                MareSwirlNormalPath,
                MareSwirlMaskPath,
                bumpScale: 0.85f,
                metallicScale: 0.0f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.04f,
                smoothnessMax: 0.12f,
                edgeWearColor: new Color32(0xD8, 0xDF, 0xEC, 0xFF),
                edgeWearAmount: 0.18f,
                edgeWearMetallic: 0.0f,
                edgeWearSmoothness: 0.08f,
                dustColor: new Color32(0xC8, 0xCE, 0xDC, 0xFF),
                dustAmount: 0.32f,
                dustSmoothness: 0.07f,
                colorVarScale: 0.85f,
                colorVarAmount: 0.045f);
            GeneratedAssets.CreateOrReplace(mareSwirl, MareSwirlMatPath);

            // 3. R1_MareEjectaRay: directional pale ejecta-ray wayfinding dust streaks (#4A5264..#686E82), zero emission
            Material mareEjectaRay = CreateSurfaceMaterial(
                litShader,
                "R1_MareEjectaRay",
                MareEjectaRayAlbedoPath,
                MareEjectaRayNormalPath,
                MareEjectaRayMaskPath,
                bumpScale: 0.95f,
                metallicScale: 0.25f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.06f,
                smoothnessMax: 0.15f,
                edgeWearColor: new Color32(0x68, 0x6E, 0x82, 0xFF),
                edgeWearAmount: 0.20f,
                edgeWearMetallic: 0.02f,
                edgeWearSmoothness: 0.10f,
                dustColor: new Color32(0x4A, 0x52, 0x64, 0xFF),
                dustAmount: 0.28f,
                dustSmoothness: 0.09f,
                colorVarScale: 0.72f,
                colorVarAmount: 0.055f);
            GeneratedAssets.CreateOrReplace(mareEjectaRay, MareEjectaRayMatPath);

            // 4. R1_Ilmenite: stepped FeTiO3 drill-target ore (#181A22 base, warm bronze-black #6E5849 & gunmetal glint, non-emissive)
            Material ilmenite = CreateSurfaceMaterial(
                litShader,
                "R1_Ilmenite",
                IlmeniteAlbedoPath,
                IlmeniteNormalPath,
                IlmeniteMaskPath,
                bumpScale: 1.25f,
                metallicScale: 1.0f,
                smoothnessScale: 1.0f,
                smoothnessMin: 0.28f,
                smoothnessMax: 0.64f,
                edgeWearColor: new Color32(0x8E, 0x74, 0x58, 0xFF),
                edgeWearAmount: 0.64f,
                edgeWearMetallic: 0.88f,
                edgeWearSmoothness: 0.62f,
                dustColor: new Color32(0x2E, 0x36, 0x4A, 0xFF),
                dustAmount: 0.14f,
                dustSmoothness: 0.10f,
                colorVarScale: 1.40f,
                colorVarAmount: 0.035f);
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
