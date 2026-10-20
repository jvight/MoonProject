using System;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
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
        public const string GroundImprintShaderName = "MoonProject/R1/GroundImprint";
        public const string GroundImprintShaderPath = "Assets/_Project/Shaders/R1/R1GroundImprint.shader";

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
        public const string OldLanderTanEnamelMaterialName = "R1_OldLanderTanEnamel";
        public const string OldLanderStrutMaterialName = "R1_OldLanderStrut";
        public const string OldLanderBlastDecalMaterialName = "R1_OldLanderBlastDecal";
        public const string CargoCrateLabelMaterialName = "R1_CargoCrateLabel";
        public const string MissionFlagStripeMaterialName = "R1_MissionFlagStripe";
        public const string BootprintTrailMaterialName = "R1_BootprintTrail";
        public const string MakerDecalMaterialName = "M_R1_MakerDecal";

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
        public const string OldLanderTanEnamelMaterialPath = ArtPaths.R1MaterialsFolder + "/" + OldLanderTanEnamelMaterialName + ".mat";
        public const string OldLanderStrutMaterialPath = ArtPaths.R1MaterialsFolder + "/" + OldLanderStrutMaterialName + ".mat";
        public const string OldLanderBlastDecalMaterialPath = ArtPaths.R1MaterialsFolder + "/" + OldLanderBlastDecalMaterialName + ".mat";
        public const string CargoCrateLabelMaterialPath = ArtPaths.R1MaterialsFolder + "/" + CargoCrateLabelMaterialName + ".mat";
        public const string MissionFlagStripeMaterialPath = ArtPaths.R1MaterialsFolder + "/" + MissionFlagStripeMaterialName + ".mat";
        public const string BootprintTrailMaterialPath = ArtPaths.R1MaterialsFolder + "/" + BootprintTrailMaterialName + ".mat";
        public const string MakerDecalMaterialPath = ArtPaths.R1MaterialsFolder + "/" + MakerDecalMaterialName + ".mat";

        public const string LanderReserveAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Albedo.png";
        public const string LanderReserveEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_LanderReserve_Emission.png";
        public const string AirlockReadoutAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Albedo.png";
        public const string AirlockReadoutEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_AirlockReadout_Emission.png";
        public const string PeakBeaconRedAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Albedo.png";
        public const string PeakBeaconRedEmissionPath = R1MaterialRecipes.TexturesFolder + "/T_R1_PeakBeaconRed_Emission.png";
        public const string IceCoreLabelAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_IceCoreLabel_Albedo.png";
        public const string IceCoreFrostAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_IceCoreFrost_Albedo.png";
        public const string CargoCrateLabelAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_CargoCrateLabel_Albedo.png";
        public const string BootprintTrailAlbedoPath = R1MaterialRecipes.TexturesFolder + "/T_R1_BootprintTrail_Albedo.png";
        public const string BootprintTrailNormalPath = R1MaterialRecipes.TexturesFolder + "/T_R1_BootprintTrail_Normal.png";
        public const string MakerDecalsAlbedoPath = R1ModelPostprocessor.MakerDecalsTexturePath;

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
        public const string PowerModulePrefabPath = ArtPaths.R1ModelsFolder + "/Ruins/PowerModule.prefab";
        public const string PropOldLanderPrefabPath = PropsModelsFolder + "/Prop_OldLander.prefab";
        public const string DecalOldLanderBlastPrefabPath = PropsModelsFolder + "/Decal_OldLanderBlast.prefab";
        public const string PropMissionFlagPrefabPath = PropsModelsFolder + "/Prop_MissionFlag.prefab";
        public const string PropSeismicStationPrefabPath = PropsModelsFolder + "/Prop_SeismicStation.prefab";
        public const string PropRetroreflectorPrefabPath = PropsModelsFolder + "/Prop_Retroreflector.prefab";
        public const string PropSurveyTripodPrefabPath = PropsModelsFolder + "/Prop_SurveyTripod.prefab";
        public const string PropSampleRackPrefabPath = PropsModelsFolder + "/Prop_SampleRack.prefab";
        public const string DecalBootprintTrailPrefabPath = PropsModelsFolder + "/Decal_BootprintTrail.prefab";
        public const string GeophoneSensorPrefabPath = PropsModelsFolder + "/Geophone_Sensor.prefab";
        public const string GeophoneRibbon10mPrefabPath = PropsModelsFolder + "/Geophone_Ribbon_10m.prefab";
        public const string GeophonePlugPrefabPath = PropsModelsFolder + "/Geophone_Plug.prefab";
        public const string PropGeophoneLinePrefabPath = PropsModelsFolder + "/Prop_GeophoneLine.prefab";

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

        /// <summary>Loads the <c>MoonProject/R1/GroundImprint</c> shader asset.</summary>
        public static Shader LoadGroundImprintShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GroundImprintShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"R1GroundImprint shader is missing at '{GroundImprintShaderPath}' ({GroundImprintShaderName}).");
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
            Shader groundImprintShader = LoadGroundImprintShader();

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

            // 11. R1_IceCoreWindow: high-glint optical/cryo glass with visible facet contrast and dust/wear support (no emission)
            Material iceCoreWindowMat = R1MaterialLibrary.CreateMaterial(r1Lit, darkGlassSpec);
            iceCoreWindowMat.name = IceCoreWindowMaterialName;
            iceCoreWindowMat.SetColor("_BaseColor", new Color(1.65f, 1.95f, 2.35f, 1f));
            iceCoreWindowMat.SetFloat("_MetallicScale", 0.25f);
            iceCoreWindowMat.SetFloat("_SmoothnessMin", 0.84f);
            iceCoreWindowMat.SetFloat("_SmoothnessMax", 0.96f);
            iceCoreWindowMat.SetColor("_EdgeWearColor", new Color(0.58f, 0.72f, 0.88f, 1f));
            iceCoreWindowMat.SetFloat("_EdgeWearAmount", 0.82f);
            iceCoreWindowMat.SetFloat("_EdgeWearThreshold", 0.42f);
            iceCoreWindowMat.SetFloat("_EdgeWearSharpness", 1.20f);
            iceCoreWindowMat.SetFloat("_EdgeWearMetallic", 0.14f);
            iceCoreWindowMat.SetFloat("_EdgeWearSmoothness", 0.94f);
            iceCoreWindowMat.SetColor("_DustColor", new Color(0.58f, 0.56f, 0.52f, 1f));
            iceCoreWindowMat.SetFloat("_DustAmount", 0.46f);
            iceCoreWindowMat.SetFloat("_DustOcclusionWeight", 0.80f);
            iceCoreWindowMat.SetFloat("_DustSharpness", 1.15f);
            iceCoreWindowMat.SetTexture("_EmissionMap", null);
            iceCoreWindowMat.SetColor("_EmissionColor", Color.black);
            iceCoreWindowMat.SetFloat("_EmissionStrength", 0.0f);
            iceCoreWindowMat.SetFloat("_BloomSafeMax", 0.35f);
            iceCoreWindowMat.SetFloat("_HotCorePeak", 0.45f);
            iceCoreWindowMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(iceCoreWindowMat, IceCoreWindowMaterialPath);

            // 12. R1_OldLanderTanEnamel: sun-faded white thermal paint turned warm tan/brown from decades of lunar UV (no emission)
            Material oldLanderTanMat = R1MaterialLibrary.CreateMaterial(r1Lit, creamSpec);
            oldLanderTanMat.name = OldLanderTanEnamelMaterialName;
            oldLanderTanMat.SetColor("_BaseColor", new Color(0.74f, 0.58f, 0.40f, 1f));
            oldLanderTanMat.SetFloat("_MetallicScale", 0.06f);
            oldLanderTanMat.SetFloat("_SmoothnessMin", 0.12f);
            oldLanderTanMat.SetFloat("_SmoothnessMax", 0.28f);
            oldLanderTanMat.SetColor("_EdgeWearColor", new Color(0.46f, 0.35f, 0.24f, 1f));
            oldLanderTanMat.SetFloat("_EdgeWearAmount", 0.56f);
            oldLanderTanMat.SetFloat("_EdgeWearSmoothness", 0.22f);
            oldLanderTanMat.SetColor("_DustColor", new Color(0.58f, 0.54f, 0.48f, 1f));
            oldLanderTanMat.SetFloat("_DustAmount", 0.38f);
            oldLanderTanMat.SetTexture("_EmissionMap", null);
            oldLanderTanMat.SetColor("_EmissionColor", Color.black);
            oldLanderTanMat.SetFloat("_EmissionStrength", 0.0f);
            oldLanderTanMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(oldLanderTanMat, OldLanderTanEnamelMaterialPath);

            // 12b. R1_OldLanderStrut: weathered matte aluminium/painted strut material with lowered smoothness
            //      so thin spaceframe/leg/mast struts do not sparkle 1-2 px at 30-80 m in grazing sun or earthshine
            Material oldLanderStrutMat = R1MaterialLibrary.CreateMaterial(r1Lit, bareSteelSpec);
            oldLanderStrutMat.name = OldLanderStrutMaterialName;
            oldLanderStrutMat.SetColor("_BaseColor", new Color(0.66f, 0.67f, 0.68f, 1f));
            oldLanderStrutMat.SetFloat("_MetallicScale", 0.16f);
            oldLanderStrutMat.SetFloat("_SmoothnessScale", 0.22f);
            oldLanderStrutMat.SetFloat("_SmoothnessMin", 0.06f);
            oldLanderStrutMat.SetFloat("_SmoothnessMax", 0.18f);
            oldLanderStrutMat.SetFloat("_BumpScale", 0.14f);
            oldLanderStrutMat.SetColor("_EdgeWearColor", new Color(0.72f, 0.73f, 0.74f, 1f));
            oldLanderStrutMat.SetFloat("_EdgeWearAmount", 0.24f);
            oldLanderStrutMat.SetFloat("_EdgeWearMetallic", 0.18f);
            oldLanderStrutMat.SetFloat("_EdgeWearSmoothness", 0.16f);
            oldLanderStrutMat.SetColor("_DustColor", new Color(0.56f, 0.55f, 0.52f, 1f));
            oldLanderStrutMat.SetFloat("_DustAmount", 0.40f);
            oldLanderStrutMat.SetFloat("_DustSmoothness", 0.08f);
            oldLanderStrutMat.SetTexture("_EmissionMap", null);
            oldLanderStrutMat.SetColor("_EmissionColor", Color.black);
            oldLanderStrutMat.SetFloat("_EmissionStrength", 0.0f);
            oldLanderStrutMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(oldLanderStrutMat, OldLanderStrutMaterialPath);

            // 13. R1_OldLanderBlastDecal: soft, slightly brighter disturbed-regolith patch under Prop_OldLander (no emission)
            R1MaterialSpec basaltSpec = R1MaterialRecipes.Get(R1MaterialSlot.Basalt);
            Material oldLanderBlastMat = R1MaterialLibrary.CreateMaterial(r1Lit, basaltSpec);
            oldLanderBlastMat.name = OldLanderBlastDecalMaterialName;
            oldLanderBlastMat.SetColor("_BaseColor", new Color(1.22f, 1.20f, 1.16f, 1f));
            oldLanderBlastMat.SetFloat("_MetallicScale", 0.02f);
            oldLanderBlastMat.SetFloat("_SmoothnessMin", 0.04f);
            oldLanderBlastMat.SetFloat("_SmoothnessMax", 0.12f);
            oldLanderBlastMat.SetColor("_EdgeWearColor", new Color(0.72f, 0.71f, 0.68f, 1f));
            oldLanderBlastMat.SetFloat("_EdgeWearAmount", 0.35f);
            oldLanderBlastMat.SetColor("_DustColor", new Color(0.68f, 0.67f, 0.64f, 1f));
            oldLanderBlastMat.SetFloat("_DustAmount", 0.62f);
            oldLanderBlastMat.SetTexture("_EmissionMap", null);
            oldLanderBlastMat.SetColor("_EmissionColor", Color.black);
            oldLanderBlastMat.SetFloat("_EmissionStrength", 0.0f);
            oldLanderBlastMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(oldLanderBlastMat, OldLanderBlastDecalMaterialPath);
            // 14. R1_CargoCrateLabel: stencilled lander cargo crate plaque (LUMEN CARGO / PART NO. LC-04 · PAD ASSY 01/02)
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

            // 15. R1_MissionFlagStripe: sun-bleached faint stripe & canton trim (almost white warm grey-cream, zero emission)
            Material flagStripeMat = R1MaterialLibrary.CreateMaterial(r1Lit, creamSpec);
            flagStripeMat.name = MissionFlagStripeMaterialName;
            flagStripeMat.SetColor("_BaseColor", new Color(0.84f, 0.80f, 0.74f, 1f));
            flagStripeMat.SetColor("_EdgeWearColor", new Color(0.95f, 0.93f, 0.89f, 1f));
            flagStripeMat.SetFloat("_EdgeWearAmount", 0.55f);
            flagStripeMat.SetFloat("_DustAmount", 0.34f);
            flagStripeMat.SetFloat("_SmoothnessMin", 0.08f);
            flagStripeMat.SetFloat("_SmoothnessMax", 0.22f);
            flagStripeMat.SetTexture("_EmissionMap", null);
            flagStripeMat.SetColor("_EmissionColor", Color.black);
            flagStripeMat.SetFloat("_EmissionStrength", 0.0f);
            flagStripeMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            GeneratedAssets.CreateOrReplace(flagStripeMat, MissionFlagStripeMaterialPath);

            // 16. R1_BootprintTrail: two crossing EVA bootprint trails, re-shading only the ground's own sunlight so the
            //     prints keep the regolith's colour, show no edge, and read in grazing sun but fade out at high sun
            var bootprintMat = new Material(groundImprintShader) { name = BootprintTrailMaterialName };
            bootprintMat.SetTexture("_BaseMap", LoadRequiredTexture(BootprintTrailAlbedoPath));
            bootprintMat.SetTexture("_BumpMap", LoadRequiredTexture(BootprintTrailNormalPath));
            bootprintMat.SetVector("_ImprintSize", new Vector4(0.95f, 3.00f, 0f, 0f));
            bootprintMat.SetFloat("_BandBelow", 0.10f);
            bootprintMat.SetFloat("_BandAbove", 0.02f);
            bootprintMat.SetFloat("_ReliefStrength", 0.45f);
            bootprintMat.SetFloat("_MaxShade", 0.25f);
            bootprintMat.SetFloat("_GrazeStart", 0.35f);
            bootprintMat.SetFloat("_GrazeEnd", 0.75f);
            bootprintMat.SetFloat("_HighSunRelief", 0.15f);
            GeneratedAssets.CreateOrReplace(bootprintMat, BootprintTrailMaterialPath);

            // 17. M_R1_MakerDecal: URP Lit with alpha clip (_AlphaClip = 1, _Cutoff = 0.35), no shadow casting,
            //     slight smoothness (0.22) so painted maker stencils catch low-angle sun like real vehicle paint.
            //     URP's own Lit validation then derives the queue, keywords, tags and passes, so the saved asset is
            //     already what URP would rewrite it to on a fresh import (a clean build leaves it unchanged).
            Shader urpLit = R1MaterialLibrary.LoadUrpLitShader();
            var makerDecalMat = new Material(urpLit) { name = MakerDecalMaterialName };
            makerDecalMat.SetTexture("_BaseMap", LoadRequiredTexture(MakerDecalsAlbedoPath));
            makerDecalMat.SetColor("_BaseColor", Color.white);
            makerDecalMat.SetFloat("_AlphaClip", 1.0f);
            makerDecalMat.SetFloat("_Cutoff", 0.35f);
            makerDecalMat.SetFloat("_Surface", 0.0f);
            makerDecalMat.SetFloat("_Metallic", 0.0f);
            makerDecalMat.SetFloat("_Smoothness", 0.22f);
            makerDecalMat.SetFloat("_SpecularHighlights", 1.0f);
            makerDecalMat.SetFloat("_EnvironmentReflections", 1.0f);
            makerDecalMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            makerDecalMat.enableInstancing = true;
            BaseShaderGUI.SetMaterialKeywords(makerDecalMat, LitGUI.SetMaterialKeywords);
            GeneratedAssets.CreateOrReplace(makerDecalMat, MakerDecalMaterialPath);
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
                ReplaceRendererSlotMaterial(root, "FrostBlock", R1MaterialRecipes.WhitePaintSlot, LoadOrBuildMaterial(IceCoreFrostMaterialPath));
            }
            else if (modelPath.EndsWith("/Prop_MissionFlag.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "FlagSheet", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(MissionFlagStripeMaterialPath));
                EnsureSimpleBoxCollider(root);
            }
            else if (modelPath.EndsWith("/Prop_SeismicStation.fbx", StringComparison.Ordinal))
            {
                EnsureSimpleBoxCollider(root);
            }
            else if (modelPath.EndsWith("/Prop_Retroreflector.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "PrismArray", R1MaterialRecipes.DarkGlassSlot, LoadOrBuildMaterial(IceCoreWindowMaterialPath));
                EnsureSimpleBoxCollider(root);
            }
            else if (modelPath.EndsWith("/Prop_SurveyTripod.fbx", StringComparison.Ordinal))
            {
                EnsureSimpleBoxCollider(root);
            }
            else if (modelPath.EndsWith("/Prop_SampleRack.fbx", StringComparison.Ordinal))
            {
                EnsureSimpleBoxCollider(root);
            }
            else if (modelPath.EndsWith("/Decal_BootprintTrail.fbx", StringComparison.Ordinal))
            {
                ReplaceRendererSlotMaterial(root, "TrailSheet", R1MaterialRecipes.SignageSlot, LoadOrBuildMaterial(BootprintTrailMaterialPath));
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                    renderers[i].receiveShadows = true;
                }
            }
            else if (modelPath.EndsWith("/Prop_OldLander.fbx", StringComparison.Ordinal))
            {
                AddOldLanderBoxColliders(root);
            }
            else if (modelPath.EndsWith("/Decal_OldLanderBlast.fbx", StringComparison.Ordinal))
            {
                Material blastMat = LoadOrBuildMaterial(OldLanderBlastDecalMaterialPath);
                ReplaceRendererSlotMaterial(root, "BlastPatch", R1MaterialRecipes.CreamEnamelSlot, blastMat);
                ReplaceRendererSlotMaterial(root, "FootpadScuffs", R1MaterialRecipes.CreamEnamelSlot, blastMat);
                DisableShadowCastingOnHierarchy(root);
            }

            else if (modelPath.EndsWith("/PowerModule.fbx", StringComparison.Ordinal))
            {
                EnsureSimpleBoxCollider(root);
            }

            ApplyMakerDecalOverrides(root);
        }

        /// <summary>Returns true when <paramref name="nodeName"/> is a maker decal child node (<c>Decal_&lt;Maker&gt;_&lt;n&gt;</c>).</summary>
        public static bool IsMakerDecalNodeName(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return false;
            }

            return nodeName.StartsWith("Decal_VIS_", StringComparison.Ordinal)
                || nodeName.StartsWith("Decal_ILC_", StringComparison.Ordinal)
                || nodeName.StartsWith("Decal_Tycho_", StringComparison.Ordinal)
                || nodeName.StartsWith("Decal_KA_", StringComparison.Ordinal);
        }

        private static void ApplyMakerDecalOverrides(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Material decalMat = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !IsMakerDecalNodeName(r.gameObject.name))
                {
                    continue;
                }

                if (decalMat == null)
                {
                    decalMat = LoadOrBuildMaterial(MakerDecalMaterialPath);
                }

                Material[] mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = decalMat;
                }

                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        }

        private static void AddOldLanderBoxColliders(GameObject root)
        {
            // Central triangular spaceframe & thermal compartments box collider
            var bodyCol = root.AddComponent<BoxCollider>();
            bodyCol.center = new Vector3(0.0f, 0.86f, -0.02f);
            bodyCol.size = new Vector3(1.36f, 0.96f, 1.24f);

            // Tall central mast, flat solar panel & planar antenna box collider
            var mastCol = root.AddComponent<BoxCollider>();
            mastCol.center = new Vector3(0.02f, 2.16f, -0.18f);
            mastCol.size = new Vector3(1.12f, 1.48f, 0.78f);

            // Three extended landing leg & footpad box colliders
            var leg1Col = root.AddComponent<BoxCollider>();
            leg1Col.center = new Vector3(0.76f, 0.32f, 0.44f);
            leg1Col.size = new Vector3(0.92f, 0.64f, 0.72f);

            var leg2Col = root.AddComponent<BoxCollider>();
            leg2Col.center = new Vector3(-0.76f, 0.32f, 0.44f);
            leg2Col.size = new Vector3(0.92f, 0.64f, 0.72f);

            var leg3Col = root.AddComponent<BoxCollider>();
            leg3Col.center = new Vector3(0.0f, 0.24f, -0.86f);
            leg3Col.size = new Vector3(0.66f, 0.66f, 0.96f);
        }

        /// <summary>
        /// Ensures a simple <see cref="BoxCollider"/> exists on <paramref name="root"/> fitted to the combined local
        /// mesh bounds of its hierarchy.
        /// </summary>
        public static BoxCollider EnsureSimpleBoxCollider(GameObject root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;
            Bounds bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot = worldToRoot * filters[i].transform.localToWorldMatrix;
                Bounds mb = mesh.bounds;
                Vector3 c = mb.center;
                Vector3 e = mb.extents;
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            Vector3 pt = toRoot.MultiplyPoint3x4(c + new Vector3(e.x * sx, e.y * sy, e.z * sz));
                            if (!hasBounds)
                            {
                                bounds = new Bounds(pt, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(pt);
                            }
                        }
                    }
                }
            }

            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider>();
            }

            if (hasBounds)
            {
                collider.center = bounds.center;
                Vector3 s = bounds.size;
                collider.size = new Vector3(Mathf.Max(0.02f, s.x), Mathf.Max(0.02f, s.y), Mathf.Max(0.02f, s.z));
            }

            return collider;
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
