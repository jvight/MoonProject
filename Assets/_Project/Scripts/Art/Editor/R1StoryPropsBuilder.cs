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
        public const string CrateRimGlowAmberMaterialName = "R1_CrateRimGlowAmber";
        public const string LanderBeaconAmberMaterialName = "R1_LanderBeaconAmber";
        public const string LanderReserveReadoutMaterialName = "R1_LanderReserveReadout";
        public const string BuildApronOutlineMaterialName = "R1_BuildApronOutline";
        public const string AirlockReadoutMaterialName = "R1_AirlockReadout";
        public const string SignalPillarMaterialName = "R1_SignalPillar";
        public const string PeakBeaconRedMaterialName = "R1_PeakBeaconRed";
        public const string IceCoreLabelMaterialName = "R1_IceCoreLabel";
        public const string IceCoreFrostMaterialName = "R1_IceCoreFrost";
        public const string IceCoreWindowMaterialName = "R1_IceCoreWindow";
        public const string CargoCrateLabelMaterialName = "R1_CargoCrateLabel";

        public const string CableMarkerAmberMaterialPath = ArtPaths.R1MaterialsFolder + "/" + CableMarkerAmberMaterialName + ".mat";
        public const string CrateRimGlowAmberMaterialPath = ArtPaths.R1MaterialsFolder + "/" + CrateRimGlowAmberMaterialName + ".mat";
        public const string LanderBeaconAmberMaterialPath = ArtPaths.R1MaterialsFolder + "/" + LanderBeaconAmberMaterialName + ".mat";
        public const string LanderReserveReadoutMaterialPath = ArtPaths.R1MaterialsFolder + "/" + LanderReserveReadoutMaterialName + ".mat";
        public const string BuildApronOutlineMaterialPath = ArtPaths.R1MaterialsFolder + "/" + BuildApronOutlineMaterialName + ".mat";
        public const string AirlockReadoutMaterialPath = ArtPaths.R1MaterialsFolder + "/" + AirlockReadoutMaterialName + ".mat";
        public const string SignalPillarMaterialPath = ArtPaths.R1MaterialsFolder + "/" + SignalPillarMaterialName + ".mat";
        public const string PeakBeaconRedMaterialPath = ArtPaths.R1MaterialsFolder + "/" + PeakBeaconRedMaterialName + ".mat";
        public const string IceCoreLabelMaterialPath = ArtPaths.R1MaterialsFolder + "/" + IceCoreLabelMaterialName + ".mat";
        public const string IceCoreFrostMaterialPath = ArtPaths.R1MaterialsFolder + "/" + IceCoreFrostMaterialName + ".mat";
        public const string IceCoreWindowMaterialPath = ArtPaths.R1MaterialsFolder + "/" + IceCoreWindowMaterialName + ".mat";
        public const string CargoCrateLabelMaterialPath = ArtPaths.R1MaterialsFolder + "/" + CargoCrateLabelMaterialName + ".mat";

        public const string LanderReserveAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Albedo.png";
        public const string LanderReserveEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Emission.png";
        public const string AirlockReadoutAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Albedo.png";
        public const string AirlockReadoutEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Emission.png";
        public const string PeakBeaconRedAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Albedo.png";
        public const string PeakBeaconRedEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Emission.png";
        public const string IceCoreLabelAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_IceCoreLabel_Albedo.png";
        public const string IceCoreFrostAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_IceCoreFrost_Albedo.png";
        public const string CargoCrateLabelAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_CargoCrateLabel_Albedo.png";

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
        public const string CrateMOpenPrefabPath = ArtPaths.R1ModelsFolder + "/Crates/Crate_M_Open.prefab";
        public const string CrateCargoOpenPrefabPath = ArtPaths.R1ModelsFolder + "/Crates/Crate_Cargo_Open.prefab";
        public const string PadFragmentCoilPlatePrefabPath = PropsModelsFolder + "/PadFragment_CoilPlate.prefab";
        public const string PadFragmentGuideRailPrefabPath = PropsModelsFolder + "/PadFragment_GuideRail.prefab";
        public const string IceCoreTubePrefabPath = PropsModelsFolder + "/IceCoreTube.prefab";
        public const string CableStraight2mPrefabPath = PropsModelsFolder + "/Cable_Straight_2m.prefab";
        public const string CableBend45PrefabPath = PropsModelsFolder + "/Cable_Bend_45.prefab";
        public const string CableBend90PrefabPath = PropsModelsFolder + "/Cable_Bend_90.prefab";
        public const string CableBrokenEndPrefabPath = PropsModelsFolder + "/Cable_BrokenEnd.prefab";
        public const string CablePlugPrefabPath = PropsModelsFolder + "/Cable_Plug.prefab";
        public const string CableSpliceBoxPrefabPath = PropsModelsFolder + "/Cable_Splice_Box.prefab";
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
            R1MaterialSpec creamSpec = R1MaterialRecipes.Get(R1MaterialSlot.CreamEnamel);
            R1MaterialSpec bareSteelSpec = R1MaterialRecipes.Get(R1MaterialSlot.BareSteel);
            R1MaterialSpec darkGlassSpec = R1MaterialRecipes.Get(R1MaterialSlot.DarkGlass);

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

            // 8. R1_CrateRimGlowAmber: soft, even, matte warm line on open crate rim (no speckles, well below bloom threshold)
            Material crateRimGlowMat = R1MaterialLibrary.CreateMaterial(r1Lit, lampSpec);
            crateRimGlowMat.name = CrateRimGlowAmberMaterialName;
            crateRimGlowMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            crateRimGlowMat.SetColor("_BaseColor", new Color(0.82f, 0.52f, 0.22f, 1f));
            crateRimGlowMat.SetTexture("_EmissionMap", Texture2D.whiteTexture);
            crateRimGlowMat.SetColor("_EmissionColor", new Color(0.98f, 0.64f, 0.24f, 1f));
            crateRimGlowMat.SetFloat("_EmissionStrength", 0.34f);
            crateRimGlowMat.SetFloat("_BloomSafeMax", 0.42f);
            crateRimGlowMat.SetFloat("_HotCoreThreshold", 0.99f);
            crateRimGlowMat.SetFloat("_HotCorePeak", 0.42f);
            crateRimGlowMat.SetFloat("_BumpScale", 0.0f);
            crateRimGlowMat.SetFloat("_MetallicScale", 0.0f);
            crateRimGlowMat.SetFloat("_SmoothnessScale", 0.20f);
            crateRimGlowMat.SetFloat("_SmoothnessMin", 0.08f);
            crateRimGlowMat.SetFloat("_SmoothnessMax", 0.16f);
            crateRimGlowMat.SetFloat("_ColorVarAmount", 0.0f);
            crateRimGlowMat.SetFloat("_EdgeWearAmount", 0.0f);
            crateRimGlowMat.SetFloat("_EdgeWearMetallic", 0.0f);
            crateRimGlowMat.SetFloat("_EdgeWearSmoothness", 0.14f);
            crateRimGlowMat.SetFloat("_DustAmount", 0.06f);
            crateRimGlowMat.SetFloat("_DustSmoothness", 0.10f);
            GeneratedAssets.CreateOrReplace(crateRimGlowMat, CrateRimGlowAmberMaterialPath);

            // 9. R1_IceCoreLabel: stencilled cryo plaque (CORE 41 · H2O · I.V. 10/88) with zero emission
            Material iceCoreLabelMat = R1MaterialLibrary.CreateMaterial(r1Lit, signageSpec);
            iceCoreLabelMat.name = IceCoreLabelMaterialName;
            iceCoreLabelMat.SetTexture("_BaseMap", LoadRequiredTexture(IceCoreLabelAlbedoPath));
            iceCoreLabelMat.SetTexture("_EmissionMap", null);
            iceCoreLabelMat.SetColor("_EmissionColor", Color.black);
            iceCoreLabelMat.SetFloat("_EmissionStrength", 0.0f);
            iceCoreLabelMat.SetFloat("_BloomSafeMax", 0.50f);
            iceCoreLabelMat.SetFloat("_HotCorePeak", 0.60f);
            iceCoreLabelMat.SetFloat("_DustAmount", 0.04f);
            iceCoreLabelMat.SetFloat("_EdgeWearAmount", 0.22f);
            iceCoreLabelMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(iceCoreLabelMat, IceCoreLabelMaterialPath);

            // 10. R1_IceCoreFrost: crystalline rime frost & frosted lunar ice core that catches 07's headlamps (no emission)
            Material iceCoreFrostMat = R1MaterialLibrary.CreateMaterial(r1Lit, creamSpec);
            iceCoreFrostMat.name = IceCoreFrostMaterialName;
            iceCoreFrostMat.SetTexture("_BaseMap", LoadRequiredTexture(IceCoreFrostAlbedoPath));
            iceCoreFrostMat.SetTexture("_BumpMap", LoadRequiredTexture(R1MaterialRecipes.GetNormalTexturePath(bareSteelSpec)));
            iceCoreFrostMat.SetFloat("_BumpScale", 1.30f);
            iceCoreFrostMat.SetFloat("_MetallicScale", 0.18f);
            iceCoreFrostMat.SetFloat("_SmoothnessMin", 0.78f);
            iceCoreFrostMat.SetFloat("_SmoothnessMax", 0.96f);
            iceCoreFrostMat.SetColor("_EdgeWearColor", new Color(0.95f, 0.99f, 1.00f, 1f));
            iceCoreFrostMat.SetFloat("_EdgeWearAmount", 0.68f);
            iceCoreFrostMat.SetFloat("_EdgeWearSmoothness", 0.96f);
            iceCoreFrostMat.SetColor("_DustColor", new Color(0.82f, 0.92f, 1.00f, 1f));
            iceCoreFrostMat.SetFloat("_DustAmount", 0.24f);
            iceCoreFrostMat.SetFloat("_DustSmoothness", 0.86f);
            iceCoreFrostMat.SetTexture("_EmissionMap", null);
            iceCoreFrostMat.SetColor("_EmissionColor", Color.black);
            iceCoreFrostMat.SetFloat("_EmissionStrength", 0.0f);
            iceCoreFrostMat.SetFloat("_BloomSafeMax", 0.35f);
            iceCoreFrostMat.SetFloat("_HotCorePeak", 0.45f);
            iceCoreFrostMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(iceCoreFrostMat, IceCoreFrostMaterialPath);

            // 11. R1_IceCoreWindow: high-glint cryo sight glass that catches 07's headlamps in eclipse dark (no emission)
            Material iceCoreWindowMat = R1MaterialLibrary.CreateMaterial(r1Lit, darkGlassSpec);
            iceCoreWindowMat.name = IceCoreWindowMaterialName;
            iceCoreWindowMat.SetColor("_BaseColor", new Color(0.75f, 0.88f, 0.98f, 1f));
            iceCoreWindowMat.SetFloat("_MetallicScale", 0.38f);
            iceCoreWindowMat.SetFloat("_SmoothnessMin", 0.88f);
            iceCoreWindowMat.SetFloat("_SmoothnessMax", 0.98f);
            iceCoreWindowMat.SetColor("_EdgeWearColor", new Color(0.90f, 0.97f, 1.00f, 1f));
            iceCoreWindowMat.SetFloat("_EdgeWearAmount", 0.62f);
            iceCoreWindowMat.SetFloat("_EdgeWearSmoothness", 0.98f);
            iceCoreWindowMat.SetFloat("_DustAmount", 0.03f);
            iceCoreWindowMat.SetTexture("_EmissionMap", null);
            iceCoreWindowMat.SetColor("_EmissionColor", Color.black);
            iceCoreWindowMat.SetFloat("_EmissionStrength", 0.0f);
            iceCoreWindowMat.SetFloat("_BloomSafeMax", 0.35f);
            iceCoreWindowMat.SetFloat("_HotCorePeak", 0.45f);
            iceCoreWindowMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(iceCoreWindowMat, IceCoreWindowMaterialPath);

            // 12. R1_CargoCrateLabel: stencilled lander cargo crate plaque (LUMEN CARGO / PART NO. LC-04 · PAD ASSY 01/02)
            Material cargoCrateLabelMat = R1MaterialLibrary.CreateMaterial(r1Lit, signageSpec);
            cargoCrateLabelMat.name = CargoCrateLabelMaterialName;
            cargoCrateLabelMat.SetTexture("_BaseMap", LoadRequiredTexture(CargoCrateLabelAlbedoPath));
            cargoCrateLabelMat.SetTexture("_EmissionMap", null);
            cargoCrateLabelMat.SetColor("_EmissionColor", Color.black);
            cargoCrateLabelMat.SetFloat("_EmissionStrength", 0.0f);
            cargoCrateLabelMat.SetFloat("_BloomSafeMax", 0.50f);
            cargoCrateLabelMat.SetFloat("_HotCorePeak", 0.60f);
            cargoCrateLabelMat.SetFloat("_DustAmount", 0.04f);
            cargoCrateLabelMat.SetFloat("_EdgeWearAmount", 0.20f);
            cargoCrateLabelMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(cargoCrateLabelMat, CargoCrateLabelMaterialPath);
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
            else if (modelPath.EndsWith("/Crate_M_Open.fbx", StringComparison.Ordinal))
            {
                Material rimMat = LoadOrBuildMaterial(CrateRimGlowAmberMaterialPath);
                ReplaceRendererSlotMaterial(root, "RimGlow", R1MaterialRecipes.LampAmberSlot, rimMat);
                ReplaceRendererSlotMaterial(root, "Lid/Seal", R1MaterialRecipes.LampAmberSlot, rimMat);
            }
            else if (modelPath.EndsWith("/Crate_Cargo_Open.fbx", StringComparison.Ordinal))
            {
                Material rimMat = LoadOrBuildMaterial(CrateRimGlowAmberMaterialPath);
                Material labelMat = LoadOrBuildMaterial(CargoCrateLabelMaterialPath);
                ReplaceRendererSlotMaterial(root, "RimGlow", R1MaterialRecipes.LampAmberSlot, rimMat);
                ReplaceRendererSlotMaterial(root, "Lid/Seal", R1MaterialRecipes.LampAmberSlot, rimMat);
                ReplaceRendererSlotMaterial(root, "LabelPlate", R1MaterialRecipes.SignageSlot, labelMat);
                ReplaceRendererSlotMaterial(root, "LabelPlate_Back", R1MaterialRecipes.SignageSlot, labelMat);
                ReplaceRendererSlotMaterial(root, "LabelPlate_Lid", R1MaterialRecipes.SignageSlot, labelMat);
                AddCargoCrateShellBoxColliders(root);
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
            else if (modelPath.EndsWith("/IceCoreTube.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "LabelPlate", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(IceCoreLabelMaterialPath));
                ReplaceRendererSlotMaterial(root, "FrostCap", R1MaterialRecipes.CreamEnamelSlot, LoadOrBuildMaterial(IceCoreFrostMaterialPath));
                ReplaceRendererSlotMaterial(root, "CoreSample", R1MaterialRecipes.CreamEnamelSlot, LoadOrBuildMaterial(IceCoreFrostMaterialPath));
                ReplaceRendererSlotMaterial(root, "SightWindow", R1MaterialRecipes.DarkGlassSlot, LoadOrBuildMaterial(IceCoreWindowMaterialPath));
            }
            else if (modelPath.EndsWith("/SSR3_Topper_03.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "FrostBlock", R1MaterialRecipes.CreamEnamelSlot, LoadOrBuildMaterial(IceCoreFrostMaterialPath));
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

        private static void AddCargoCrateShellBoxColliders(GameObject root)
        {
            FitBoxColliderToMesh(root);
            string[] shellNodes = { "Wall_Back", "Wall_Left", "Wall_Right", "SidePanel_Open", "Lid" };
            for (int i = 0; i < shellNodes.Length; i++)
            {
                Transform child = root.transform.Find(shellNodes[i]);
                if (child != null)
                {
                    FitBoxColliderToMesh(child.gameObject);
                }
            }
        }

        private static void FitBoxColliderToMesh(GameObject target)
        {
            var filter = target != null ? target.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            var box = target.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = target.AddComponent<BoxCollider>();
            }

            Bounds bounds = filter.sharedMesh.bounds;
            box.center = bounds.center;
            box.size = bounds.size;
        }
    }
}
