using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds the specialized R1 materials and applies per-child material/renderer overrides for the Opening v2
    /// story props under <c>Assets/_Project/Generated/Art/R1/Models/Props/</c> and <c>HabitatModules.prefab</c>.
    /// </summary>
    public static class R1StoryPropsBuilder
    {
        public const string SignalPillarShaderName = "MoonProject/R1/SignalPillar";
        public const string SignalPillarShaderPath = "Assets/_Project/Shaders/R1/R1SignalPillar.shader";

        public const string PropsSourceFolder = R1MaterialRecipes.ModelsFolder + "/Props";
        public const string PropsModelsFolder = ArtPaths.R1ModelsFolder + "/Props";

        public const string CableMarkerAmberMaterialName = "R1_CableMarkerAmber";
        public const string LanderBeaconAmberMaterialName = "R1_LanderBeaconAmber";
        public const string LanderReserveReadoutMaterialName = "R1_LanderReserveReadout";
        public const string BuildApronOutlineMaterialName = "R1_BuildApronOutline";
        public const string AirlockReadoutMaterialName = "R1_AirlockReadout";
        public const string SignalPillarMaterialName = "R1_SignalPillar";
        public const string PeakBeaconRedMaterialName = "R1_PeakBeaconRed";

        public const string CableMarkerAmberMaterialPath = ArtPaths.R1MaterialsFolder + "/" + CableMarkerAmberMaterialName + ".mat";
        public const string LanderBeaconAmberMaterialPath = ArtPaths.R1MaterialsFolder + "/" + LanderBeaconAmberMaterialName + ".mat";
        public const string LanderReserveReadoutMaterialPath = ArtPaths.R1MaterialsFolder + "/" + LanderReserveReadoutMaterialName + ".mat";
        public const string BuildApronOutlineMaterialPath = ArtPaths.R1MaterialsFolder + "/" + BuildApronOutlineMaterialName + ".mat";
        public const string AirlockReadoutMaterialPath = ArtPaths.R1MaterialsFolder + "/" + AirlockReadoutMaterialName + ".mat";
        public const string SignalPillarMaterialPath = ArtPaths.R1MaterialsFolder + "/" + SignalPillarMaterialName + ".mat";
        public const string PeakBeaconRedMaterialPath = ArtPaths.R1MaterialsFolder + "/" + PeakBeaconRedMaterialName + ".mat";

        public const string LanderReserveAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Albedo.png";
        public const string LanderReserveEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Emission.png";
        public const string AirlockReadoutAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Albedo.png";
        public const string AirlockReadoutEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Emission.png";
        public const string PeakBeaconRedAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Albedo.png";
        public const string PeakBeaconRedEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Emission.png";

        public const string DeadChargingPostPrefabPath = PropsModelsFolder + "/DeadChargingPost.prefab";
        public const string GroundCableSegmentPrefabPath = PropsModelsFolder + "/GroundCableSegment.prefab";
        public const string GroundCableEndPrefabPath = PropsModelsFolder + "/GroundCableEnd.prefab";
        public const string LanderServicePortPrefabPath = PropsModelsFolder + "/LanderServicePort.prefab";
        public const string LanderBeaconPrefabPath = PropsModelsFolder + "/LanderBeacon.prefab";
        public const string BuildApronMarkingsPrefabPath = PropsModelsFolder + "/BuildApronMarkings.prefab";
        public const string AirlockReadoutPanelPrefabPath = PropsModelsFolder + "/AirlockReadoutPanel.prefab";
        public const string DeadAntennaMastPrefabPath = PropsModelsFolder + "/DeadAntennaMast.prefab";
        public const string SignalPillarPrefabPath = PropsModelsFolder + "/SignalPillar.prefab";
        public const string PeakBeaconPrefabPath = PropsModelsFolder + "/PeakBeacon.prefab";
        public const string HabitatModulesPrefabPath = ArtPaths.R1ModelsFolder + "/Ruins/HabitatModules.prefab";

        /// <summary>Loads the <c>MoonProject/R1/SignalPillar</c> shader asset.</summary>
        public static Shader LoadSignalPillarShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SignalPillarShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"R1SignalPillar shader is missing at '{SignalPillarShaderPath}' ({SignalPillarShaderName}).");
            }

            return shader;
        }

        /// <summary>
        /// Builds all specialized story-prop materials under <c>Assets/_Project/Generated/Art/R1/Materials/</c>.
        /// Called automatically from <see cref="R1MaterialLibrary.BuildMaterials"/>.
        /// </summary>
        public static void BuildStoryPropMaterials(Shader litShader = null)
        {
            Shader r1Lit = litShader ?? R1MaterialLibrary.LoadShader();
            Shader pillarShader = LoadSignalPillarShader();

            R1MaterialSpec lampSpec = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber);
            R1MaterialSpec orangeSpec = R1MaterialRecipes.Get(R1MaterialSlot.OrangeStripe);
            R1MaterialSpec signageSpec = R1MaterialRecipes.Get(R1MaterialSlot.Signage);

            // 1. R1_CableMarkerAmber: faint, very dim "follow me" marker every 8 m (well below bloom threshold)
            Material cableMarkerMat = R1MaterialLibrary.CreateMaterial(r1Lit, lampSpec);
            cableMarkerMat.name = CableMarkerAmberMaterialName;
            cableMarkerMat.SetColor("_EmissionColor", new Color(0.95f, 0.58f, 0.20f, 1f));
            cableMarkerMat.SetFloat("_EmissionStrength", 0.28f);
            cableMarkerMat.SetFloat("_BloomSafeMax", 0.38f);
            cableMarkerMat.SetFloat("_HotCoreThreshold", 0.98f);
            cableMarkerMat.SetFloat("_HotCorePeak", 0.45f);
            GeneratedAssets.CreateOrReplace(cableMarkerMat, CableMarkerAmberMaterialPath);

            // 2. R1_LanderBeaconAmber: body strictly below bloom threshold (0.96 < 1.10) except tiny bullseye core (1.58)
            Material landerBeaconMat = R1MaterialLibrary.CreateMaterial(r1Lit, lampSpec);
            landerBeaconMat.name = LanderBeaconAmberMaterialName;
            landerBeaconMat.SetColor("_EmissionColor", new Color(1.00f, 0.74f, 0.28f, 1f));
            landerBeaconMat.SetFloat("_EmissionStrength", 1.00f);
            landerBeaconMat.SetFloat("_BloomSafeMax", 0.96f);
            landerBeaconMat.SetFloat("_HotCoreThreshold", 0.88f);
            landerBeaconMat.SetFloat("_HotCorePeak", 1.58f);
            GeneratedAssets.CreateOrReplace(landerBeaconMat, LanderBeaconAmberMaterialPath);

            // 3. R1_LanderReserveReadout: crisp 2-line amber readout (RESERVE · 18% / PORT A · READY)
            Material reserveReadoutMat = R1MaterialLibrary.CreateMaterial(r1Lit, signageSpec);
            reserveReadoutMat.name = LanderReserveReadoutMaterialName;
            reserveReadoutMat.SetTexture("_BaseMap", LoadRequiredTexture(LanderReserveAlbedoPath));
            reserveReadoutMat.SetTexture("_EmissionMap", LoadRequiredTexture(LanderReserveEmissionPath));
            reserveReadoutMat.SetColor("_EmissionColor", new Color(1.00f, 0.72f, 0.28f, 1f));
            reserveReadoutMat.SetFloat("_EmissionStrength", 0.88f);
            reserveReadoutMat.SetFloat("_BloomSafeMax", 0.95f);
            reserveReadoutMat.SetFloat("_HotCoreThreshold", 0.92f);
            reserveReadoutMat.SetFloat("_HotCorePeak", 1.02f);
            reserveReadoutMat.SetFloat("_DustAmount", 0.06f);
            reserveReadoutMat.SetFloat("_EdgeWearAmount", 0.15f);
            GeneratedAssets.CreateOrReplace(reserveReadoutMat, LanderReserveReadoutMaterialPath);

            // 4. R1_BuildApronOutline: worn orange bay outline with _BuildModeGlow property (0 = unlit, 1 = faint amber glow)
            Material apronOutlineMat = R1MaterialLibrary.CreateMaterial(r1Lit, orangeSpec);
            apronOutlineMat.name = BuildApronOutlineMaterialName;
            apronOutlineMat.SetTexture("_EmissionMap", LoadRequiredTexture(R1MaterialRecipes.GetEmissionTexturePath(lampSpec)));
            apronOutlineMat.SetColor("_EmissionColor", new Color(1.00f, 0.72f, 0.26f, 1f));
            apronOutlineMat.SetFloat("_EmissionStrength", 0.0f);
            apronOutlineMat.SetFloat("_BuildModeGlow", 0.0f);
            apronOutlineMat.SetFloat("_BloomSafeMax", 0.95f);
            apronOutlineMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            GeneratedAssets.CreateOrReplace(apronOutlineMat, BuildApronOutlineMaterialPath);

            // 5. R1_AirlockReadout: 2-line amber status readout (HABITAT SEAL · LOCKED / SUIT 01-04 · OFFLINE)
            Material airlockReadoutMat = R1MaterialLibrary.CreateMaterial(r1Lit, signageSpec);
            airlockReadoutMat.name = AirlockReadoutMaterialName;
            airlockReadoutMat.SetTexture("_BaseMap", LoadRequiredTexture(AirlockReadoutAlbedoPath));
            airlockReadoutMat.SetTexture("_EmissionMap", LoadRequiredTexture(AirlockReadoutEmissionPath));
            airlockReadoutMat.SetColor("_EmissionColor", new Color(1.00f, 0.74f, 0.28f, 1f));
            airlockReadoutMat.SetFloat("_EmissionStrength", 0.92f);
            airlockReadoutMat.SetFloat("_BloomSafeMax", 0.96f);
            airlockReadoutMat.SetFloat("_HotCoreThreshold", 0.92f);
            airlockReadoutMat.SetFloat("_HotCorePeak", 1.04f);
            airlockReadoutMat.SetFloat("_DustAmount", 0.05f);
            airlockReadoutMat.SetFloat("_EdgeWearAmount", 0.12f);
            GeneratedAssets.CreateOrReplace(airlockReadoutMat, AirlockReadoutMaterialPath);

            // 6. R1_SignalPillar: faint cool-white stylized vacuum beam with soft top fade (B11)
            var signalPillarMat = new Material(pillarShader) { name = SignalPillarMaterialName };
            signalPillarMat.SetColor("_BeamColor", new Color(0.76f, 0.88f, 1.00f, 1f));
            signalPillarMat.SetFloat("_Intensity", 0.62f);
            signalPillarMat.SetFloat("_BloomSafeMax", 0.92f);
            signalPillarMat.SetFloat("_PillarHeight", 60.0f);
            signalPillarMat.SetFloat("_BottomFadeMeters", 2.2f);
            signalPillarMat.SetFloat("_TopFadeStartNormalized", 0.30f);
            signalPillarMat.SetFloat("_EdgePower", 2.4f);
            signalPillarMat.enableInstancing = true;
            GeneratedAssets.CreateOrReplace(signalPillarMat, SignalPillarMaterialPath);

            // 7. R1_PeakBeaconRed: crimson-red AlertSoft beacon lens for PeakBeacon.prefab's Lamp node
            Material peakBeaconRedMat = R1MaterialLibrary.CreateMaterial(r1Lit, lampSpec);
            peakBeaconRedMat.name = PeakBeaconRedMaterialName;
            peakBeaconRedMat.SetTexture("_BaseMap", LoadRequiredTexture(PeakBeaconRedAlbedoPath));
            peakBeaconRedMat.SetTexture("_EmissionMap", LoadRequiredTexture(PeakBeaconRedEmissionPath));
            peakBeaconRedMat.SetColor("_EmissionColor", Color.white);
            peakBeaconRedMat.SetFloat("_EmissionStrength", 1.00f);
            peakBeaconRedMat.SetFloat("_BloomSafeMax", 0.98f);
            peakBeaconRedMat.SetFloat("_HotCoreThreshold", 0.88f);
            peakBeaconRedMat.SetFloat("_HotCorePeak", 1.48f);
            GeneratedAssets.CreateOrReplace(peakBeaconRedMat, PeakBeaconRedMaterialPath);
        }

        /// <summary>
        /// Applies per-child specialized material and renderer overrides to instantiated story-prop models before
        /// <see cref="R1ModelBuilder"/> saves them as prefabs.
        /// </summary>
        public static void ApplyStoryPropOverrides(GameObject root, string modelPath)
        {
            if (root == null || string.IsNullOrEmpty(modelPath))
            {
                return;
            }

            if (modelPath.EndsWith("/GroundCableSegment.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "MarkerLamp", R1MaterialRecipes.LampAmberSlot, LoadOrBuildMaterial(CableMarkerAmberMaterialPath));
            }
            else if (modelPath.EndsWith("/LanderServicePort.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "ReadoutScreen", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(LanderReserveReadoutMaterialPath));
                ReplaceRendererSlotMaterial(root, "ReservePilotLamp", R1MaterialRecipes.LampAmberSlot, LoadOrBuildMaterial(LanderBeaconAmberMaterialPath));
            }
            else if (modelPath.EndsWith("/LanderBeacon.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "BeaconLens", R1MaterialRecipes.LampAmberSlot, LoadOrBuildMaterial(LanderBeaconAmberMaterialPath));
            }
            else if (modelPath.EndsWith("/BuildApronMarkings.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "BayGlowOutline", R1MaterialRecipes.OrangeStripeSlot, LoadOrBuildMaterial(BuildApronOutlineMaterialPath));
                DisableShadowCastingOnHierarchy(root);
            }
            else if (modelPath.EndsWith("/AirlockReadoutPanel.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "ReadoutScreen", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(AirlockReadoutMaterialPath));
            }
            else if (modelPath.EndsWith("/HabitatModules.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "AirlockReadout", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(AirlockReadoutMaterialPath));
            }
            else if (modelPath.EndsWith("/SignalPillar.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "PillarBeam", R1MaterialRecipes.DarkGlassSlot, LoadOrBuildMaterial(SignalPillarMaterialPath));
                DisableShadowCastingOnHierarchy(root);
            }
            else if (modelPath.EndsWith("/PeakBeacon.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "Lamp", R1MaterialRecipes.LampAmberSlot, LoadOrBuildMaterial(PeakBeaconRedMaterialPath));
            }
        }

        /// <summary>Loads a generated story-prop material, building the story-prop materials first if missing.</summary>
        public static Material LoadOrBuildMaterial(string materialAssetPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
            if (mat == null)
            {
                BuildStoryPropMaterials();
                mat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
            }

            if (mat == null)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Required material asset is missing at '{materialAssetPath}'.");
            }

            return mat;
        }

        private static Texture2D LoadRequiredTexture(string path)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Required texture asset is missing at '{path}'.");
            }

            return tex;
        }

        private static void ReplaceRendererSlotMaterial(
            GameObject root,
            string childName,
            string targetSlotName,
            Material overrideMaterial)
        {
            Transform child = root.transform.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Model '{root.name}' is missing expected child node '{childName}'.");
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Child '{childName}' on '{root.name}' has no Renderer.");
            }

            Material[] mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Child '{childName}' on '{root.name}' has no materials.");
            }

            if (mats.Length == 1)
            {
                mats[0] = overrideMaterial;
                renderer.sharedMaterials = mats;
                return;
            }

            int matchedIndex = -1;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && R1MaterialRecipes.NormalizeSlotName(mats[i].name) == targetSlotName)
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex < 0)
            {
                throw new InvalidOperationException(
                    $"R1StoryPropsBuilder: Could not find material slot '{targetSlotName}' on '{root.name}/{childName}'.");
            }

            mats[matchedIndex] = overrideMaterial;
            renderer.sharedMaterials = mats;
        }

        private static void DisableShadowCastingOnHierarchy(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }
    }
}
