using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the Opening v2 R1 story props: scale contracts (<c>tools/art/qa/contracts/*.json</c>), anchor hierarchy,
    /// specialized material assignments, <c>_BuildModeGlow</c> property on <c>BuildApronMarkings</c>,
    /// <c>MoonProject/R1/SignalPillar</c> shader compilation, and <c>PeakBeacon</c> <c>Lamp</c> node contract.
    /// </summary>
    public sealed class R1StoryPropsTests
    {
        private const string ContractsDir = "tools/art/qa/contracts";

        private static readonly (string ContractFile, string FbxRelativePath)[] StoryPropContracts =
        {
            ("dead_charging_post.json", "Props/DeadChargingPost.fbx"),
            ("ground_cable_segment.json", "Props/GroundCableSegment.fbx"),
            ("ground_cable_end.json", "Props/GroundCableEnd.fbx"),
            ("lander_service_port.json", "Props/LanderServicePort.fbx"),
            ("lander_beacon.json", "Props/LanderBeacon.fbx"),
            ("build_apron_markings.json", "Props/BuildApronMarkings.fbx"),
            ("airlock_readout_panel.json", "Props/AirlockReadoutPanel.fbx"),
            ("dead_antenna_mast.json", "Props/DeadAntennaMast.fbx"),
            ("signal_pillar.json", "Props/SignalPillar.fbx"),
            ("peak_beacon.json", "Props/PeakBeacon.fbx"),
            ("crate_m_open.json", "Crates/Crate_M_Open.fbx"),
            ("crate_cargo_open.json", "Crates/Crate_Cargo_Open.fbx"),
            ("pad_fragment_coil_plate.json", "Props/PadFragment_CoilPlate.fbx"),
            ("pad_fragment_guide_rail.json", "Props/PadFragment_GuideRail.fbx"),
            ("ice_core_tube.json", "Props/IceCoreTube.fbx"),
            ("cable_straight_2m.json", "Props/Cable_Straight_2m.fbx"),
            ("cable_bend_45.json", "Props/Cable_Bend_45.fbx"),
            ("cable_bend_90.json", "Props/Cable_Bend_90.fbx"),
            ("cable_broken_end.json", "Props/Cable_BrokenEnd.fbx"),
            ("cable_plug.json", "Props/Cable_Plug.fbx"),
            ("cable_splice_box.json", "Props/Cable_Splice_Box.fbx"),
            ("prop_old_lander.json", "Props/Prop_OldLander.fbx"),
            ("decal_old_lander_blast.json", "Props/Decal_OldLanderBlast.fbx"),
            ("prop_mission_flag.json", "Props/Prop_MissionFlag.fbx"),
            ("prop_seismic_station.json", "Props/Prop_SeismicStation.fbx"),
            ("prop_retroreflector.json", "Props/Prop_Retroreflector.fbx"),
            ("prop_survey_tripod.json", "Props/Prop_SurveyTripod.fbx"),
            ("prop_sample_rack.json", "Props/Prop_SampleRack.fbx"),
            ("decal_bootprint_trail.json", "Props/Decal_BootprintTrail.fbx"),
            ("geophone_sensor.json", "Props/Geophone_Sensor.fbx"),
            ("geophone_ribbon_10m.json", "Props/Geophone_Ribbon_10m.fbx"),
            ("geophone_plug.json", "Props/Geophone_Plug.fbx"),
            ("prop_geophone_line.json", "Props/Prop_GeophoneLine.fbx"),
        };

        [Serializable]
        private struct ContractSpec
        {
            public string model;
            public BoundsSpec bounds;
        }

        [Serializable]
        private struct BoundsSpec
        {
            public float[] size;
            public float tolerance;
            public float[] min_size;
            public float[] max_size;
        }

        [OneTimeSetUp]
        public void EnsureBuilt()
        {
            R1MaterialLibrary.BuildMaterials();
            R1ModelBuilder.Build();
        }

        private static IEnumerable<TestCaseData> ContractCases()
        {
            for (int i = 0; i < StoryPropContracts.Length; i++)
            {
                (string contractFile, string fbxRelativePath) = StoryPropContracts[i];
                yield return new TestCaseData(contractFile, fbxRelativePath)
                    .SetName($"StoryProp_{Path.GetFileNameWithoutExtension(contractFile)}_MatchesContractBounds");
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void StoryPropPrefab_RendererAndMeshBounds_MatchContract(string contractFile, string fbxRelativePath)
        {
            string contractPath = Path.Combine(ContractsDir, contractFile);
            Assert.IsTrue(File.Exists(contractPath), $"Missing contract JSON at '{contractPath}'.");
            ContractSpec contract = JsonUtility.FromJson<ContractSpec>(File.ReadAllText(contractPath));
            Assert.IsNotNull(contract.bounds.size, $"Contract '{contractFile}' is missing bounds.size.");
            Assert.AreEqual(3, contract.bounds.size.Length, $"Contract '{contractFile}' bounds.size must have 3 axes.");

            string fbxPath = R1MaterialRecipes.ModelsFolder + "/" + fbxRelativePath;
            string prefabPath = R1ModelBuilder.GetGeneratedPrefabPath(fbxPath);
            GameObject prefab = MeshChecks.LoadGeneratedPrefab(prefabPath);

            Bounds meshBounds = MeshChecks.PrefabBounds(prefab, prefab.transform.localToWorldMatrix);
            Vector3 actualMeshSize = new Vector3(meshBounds.size.x, meshBounds.size.z, meshBounds.size.y);
            AssertAxisBounds(contractFile, "mesh", actualMeshSize, contract.bounds);

            Bounds rendererBounds = MeasureInstantiatedRendererBounds(prefab);
            Vector3 actualRendererSize = new Vector3(rendererBounds.size.x, rendererBounds.size.z, rendererBounds.size.y);
            AssertAxisBounds(contractFile, "renderer", actualRendererSize, contract.bounds);
        }

        [Test]
        public void DeadChargingPostAndGroundCable_ExposeAnchors_AndDimFollowMeMarker()
        {
            GameObject postPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.DeadChargingPostPrefabPath);
            Assert.IsNotNull(postPrefab.transform.Find("Socket_Umbilical"), "DeadChargingPost missing Socket_Umbilical.");
            Assert.IsNotNull(postPrefab.transform.Find("StatusWindow_Dead"), "DeadChargingPost missing StatusWindow_Dead.");
            Assert.IsNotNull(postPrefab.transform.Find("CableOrigin_Anchor"), "DeadChargingPost missing CableOrigin_Anchor.");

            Renderer[] postRenderers = postPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in postRenderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        "DeadChargingPost must have no lit R1_LampAmber material slots.");
                }
            }

            GameObject segPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.GroundCableSegmentPrefabPath);
            Transform startAnchor = segPrefab.transform.Find("Start_Anchor");
            Transform endAnchor = segPrefab.transform.Find("End_Anchor");
            Assert.IsNotNull(startAnchor, "GroundCableSegment missing Start_Anchor.");
            Assert.IsNotNull(endAnchor, "GroundCableSegment missing End_Anchor.");
            Assert.AreEqual(8.0f, Vector3.Distance(startAnchor.localPosition, endAnchor.localPosition), 0.01f,
                "GroundCableSegment Start_Anchor to End_Anchor span must be 8.0 m for seamless tiling.");

            Transform markerLamp = segPrefab.transform.Find("MarkerLamp");
            Assert.IsNotNull(markerLamp, "GroundCableSegment missing MarkerLamp child.");
            Material markerMat = markerLamp.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.CableMarkerAmberMaterialName, markerMat.name);
            Assert.Less(markerMat.GetFloat("_BloomSafeMax"), 0.50f,
                "Cable marker must stay very dim and well below bloom threshold.");
            Assert.Less(markerMat.GetFloat("_HotCorePeak"), Palette.BloomThreshold,
                "Cable marker must not bloom like glowing loot.");

            GameObject endPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.GroundCableEndPrefabPath);
            Assert.IsNotNull(endPrefab.transform.Find("Start_Anchor"), "GroundCableEnd missing Start_Anchor.");
            Assert.IsNotNull(endPrefab.transform.Find("Plug_Anchor"), "GroundCableEnd missing Plug_Anchor.");
        }

        [Test]
        public void LanderServicePortAndBeacon_ExposeAnchors_AndBloomSafeEmissiveExceptTinyCore()
        {
            GameObject portPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.LanderServicePortPrefabPath);
            Assert.IsNotNull(portPrefab.transform.Find("HullMount_Anchor"));
            Assert.IsNotNull(portPrefab.transform.Find("Socket_Cable"));
            Assert.IsNotNull(portPrefab.transform.Find("Socket_Trickle"));
            Assert.IsNotNull(portPrefab.transform.Find("Readout_Anchor"));

            Transform readout = portPrefab.transform.Find("ReadoutScreen");
            Assert.IsNotNull(readout);
            Material readoutMat = readout.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.LanderReserveReadoutMaterialName, readoutMat.name);

            GameObject beaconPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.LanderBeaconPrefabPath);
            Assert.IsNotNull(beaconPrefab.transform.Find("Mount_Anchor"));
            Assert.IsNotNull(beaconPrefab.transform.Find("Light_Anchor"));

            Transform lens = beaconPrefab.transform.Find("BeaconLens");
            Assert.IsNotNull(lens);
            Material lensMat = lens.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.LanderBeaconAmberMaterialName, lensMat.name);
            Assert.Less(lensMat.GetFloat("_BloomSafeMax"), Palette.BloomThreshold,
                "LanderBeacon body emission must stay below Palette.BloomThreshold.");
            Assert.Greater(lensMat.GetFloat("_HotCorePeak"), Palette.BloomThreshold,
                "LanderBeacon tiny core must exceed Palette.BloomThreshold for 165 m visibility.");
        }

        [Test]
        public void BuildApronMarkings_ExposesBuildModeGlowMaterialProperty()
        {
            Shader r1Lit = R1MaterialLibrary.LoadShader();
            var testMat = new Material(r1Lit);
            try
            {
                Assert.IsTrue(testMat.HasProperty("_BuildModeGlow"),
                    "R1Lit shader must expose _BuildModeGlow property for build-apron outline toggle.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testMat);
            }

            GameObject apronPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.BuildApronMarkingsPrefabPath);
            Assert.IsNotNull(apronPrefab.transform.Find("PadSnap_Anchor"));
            Assert.IsNotNull(apronPrefab.transform.Find("RoverApproach_Anchor"));
            Assert.IsNotNull(apronPrefab.transform.Find("CableChannel_Anchor"));

            Transform outline = apronPrefab.transform.Find("BayGlowOutline");
            Assert.IsNotNull(outline, "BuildApronMarkings must contain BayGlowOutline child.");
            Material outlineMat = outline.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.BuildApronOutlineMaterialName, outlineMat.name);
            Assert.AreEqual(0f, outlineMat.GetFloat("_BuildModeGlow"), 1e-4f,
                "BayGlowOutline _BuildModeGlow must default to 0 (unlit when build mode is closed).");
        }

        [Test]
        public void AirlockReadoutPanelAndHabitatModules_UseEmissiveReadoutMaterial_AndPreserveAirlockPanel()
        {
            GameObject panelPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.AirlockReadoutPanelPrefabPath);
            Assert.IsNotNull(panelPrefab.transform.Find("WallMount_Anchor"));
            Assert.IsNotNull(panelPrefab.transform.Find("Readout_Anchor"));

            Transform screen = panelPrefab.transform.Find("ReadoutScreen");
            Assert.IsNotNull(screen);
            Material screenMat = screen.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.AirlockReadoutMaterialName, screenMat.name);
            Assert.AreNotEqual(R1MaterialRecipes.CrtFaceShaderName, screenMat.shader.name);

            GameObject habitatPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.HabitatModulesPrefabPath);
            Assert.IsNotNull(habitatPrefab.transform.Find("AirlockPanel"),
                "HabitatModules must preserve existing AirlockPanel child.");
            Transform habitatReadout = habitatPrefab.transform.Find("AirlockReadout");
            Assert.IsNotNull(habitatReadout,
                "HabitatModules must contain AirlockReadout child at Rover 07 eye level beside the airlock door.");
            Material[] readoutMats = habitatReadout.GetComponent<Renderer>().sharedMaterials;
            Assert.AreEqual(2, readoutMats.Length);
            Assert.AreEqual(R1StoryPropsBuilder.AirlockReadoutMaterialName, readoutMats[1].name);
        }

        [Test]
        public void DeadAntennaMastAndSignalPillar_CompileShader_AndHaveExpectedStructure()
        {
            Shader pillarShader = R1StoryPropsBuilder.LoadSignalPillarShader();
            Assert.IsNotNull(pillarShader);
            Assert.AreEqual(R1StoryPropsBuilder.SignalPillarShaderName, pillarShader.name);
            Assert.IsTrue(pillarShader.isSupported);
            Assert.IsFalse(ShaderUtil.ShaderHasError(pillarShader),
                "MoonProject/R1/SignalPillar shader must compile without errors.");

            GameObject deadMastPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.DeadAntennaMastPrefabPath);
            Assert.IsNotNull(deadMastPrefab.transform.Find("Stage_4/Mast/DishAzimuth/Dish"));
            Renderer[] mastRenderers = deadMastPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in mastRenderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        "DeadAntennaMast must have no lit R1_LampAmber material slots.");
                }
            }

            GameObject pillarPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.SignalPillarPrefabPath);
            Assert.IsNotNull(pillarPrefab.transform.Find("Base_Anchor"));
            Assert.IsNotNull(pillarPrefab.transform.Find("Top_Anchor"));
            Transform beam = pillarPrefab.transform.Find("PillarBeam");
            Assert.IsNotNull(beam);
            Material beamMat = beam.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.SignalPillarMaterialName, beamMat.name);
            Assert.AreEqual(R1StoryPropsBuilder.SignalPillarShaderName, beamMat.shader.name);
        }

        [Test]
        public void PeakBeacon_PreservesLampNodeAt672m_WithRedEmissiveMaterial()
        {
            GameObject peakPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PeakBeaconPrefabPath);
            Assert.IsNotNull(peakPrefab.transform.Find("Mast"));
            Assert.IsNotNull(peakPrefab.transform.Find("Dish_Anchor"));
            Assert.IsNotNull(peakPrefab.transform.Find("SummitBase_Anchor"));

            Transform lamp = peakPrefab.transform.Find("Lamp");
            Assert.IsNotNull(lamp, "PeakBeacon must preserve child node 'Lamp' for PeakBeacon.cs.");
            Vector3 lampWorldPos = peakPrefab.transform.TransformPoint(lamp.localPosition);
            Assert.AreEqual(6.72f, lampWorldPos.y, 0.02f,
                "PeakBeacon Lamp child must sit at Y = 6.72 m matching PeakBeaconBuilder.LampHeight.");

            Material lampMat = lamp.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.PeakBeaconRedMaterialName, lampMat.name);
        }

        [Test]
        public void CrateMOpenAndPadFragments_ExposeCollectibleChildren_BloomSafeRimGlow_AndFragmentSilhouettes()
        {
            GameObject openCratePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CrateMOpenPrefabPath);
            Assert.IsNotNull(openCratePrefab.transform.Find("Lid"), "Crate_M_Open missing Lid.");
            Assert.IsNotNull(openCratePrefab.transform.Find("Lid/Seal"), "Crate_M_Open missing Lid/Seal.");
            Assert.IsNotNull(openCratePrefab.transform.Find("Lid/CutPoint"), "Crate_M_Open missing Lid/CutPoint.");
            Assert.IsNotNull(openCratePrefab.transform.Find("ItemSocket"), "Crate_M_Open missing ItemSocket.");

            string[] contentItems = { "Igniter", "MetalBundle", "WiringSpool" };
            for (int i = 0; i < contentItems.Length; i++)
            {
                Transform item = openCratePrefab.transform.Find(contentItems[i]);
                Assert.IsNotNull(item, $"Crate_M_Open missing separate child '{contentItems[i]}'.");
                Assert.IsNotNull(item.GetComponent<Renderer>(), $"Crate_M_Open/{contentItems[i]} must have a Renderer.");
                Assert.IsNotNull(item.GetComponent<MeshFilter>(), $"Crate_M_Open/{contentItems[i]} must have a MeshFilter.");
            }

            Transform rimGlow = openCratePrefab.transform.Find("RimGlow");
            Assert.IsNotNull(rimGlow, "Crate_M_Open missing RimGlow child.");
            Material rimMat = rimGlow.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.CrateRimGlowAmberMaterialName, rimMat.name);
            Assert.Less(rimMat.GetFloat("_BloomSafeMax"), Palette.BloomThreshold,
                "Crate_M_Open RimGlow emission must stay well below bloom threshold.");
            Assert.Less(rimMat.GetFloat("_HotCorePeak"), Palette.BloomThreshold,
                "Crate_M_Open RimGlow hot core must stay below bloom threshold.");

            GameObject coilPlatePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PadFragmentCoilPlatePrefabPath);
            Assert.IsNotNull(coilPlatePrefab.transform.Find("Scan_1"), "PadFragment_CoilPlate missing Scan_1.");
            Assert.IsNotNull(coilPlatePrefab.GetComponent<Renderer>(), "PadFragment_CoilPlate root must have a Renderer.");

            GameObject guideRailPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PadFragmentGuideRailPrefabPath);
            Assert.IsNotNull(guideRailPrefab.transform.Find("Scan_1"), "PadFragment_GuideRail missing Scan_1.");
            Assert.IsNotNull(guideRailPrefab.GetComponent<Renderer>(), "PadFragment_GuideRail root must have a Renderer.");
        }

        [Test]
        public void IceCoreTube_HasCenteredPivotStencilledLabelFrostEndAndWindowCoreWithoutBloom()
        {
            GameObject tubePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.IceCoreTubePrefabPath);
            Assert.IsNotNull(tubePrefab.transform.Find("CryoSleeve"), "IceCoreTube missing CryoSleeve.");
            Assert.IsNotNull(tubePrefab.transform.Find("CoreSample"), "IceCoreTube missing CoreSample.");
            Assert.IsNotNull(tubePrefab.transform.Find("SightWindow"), "IceCoreTube missing SightWindow.");
            Assert.IsNotNull(tubePrefab.transform.Find("LabelPlate"), "IceCoreTube missing LabelPlate.");
            Assert.IsNotNull(tubePrefab.transform.Find("CarryHandle"), "IceCoreTube missing CarryHandle.");
            Assert.IsNotNull(tubePrefab.transform.Find("FrostCap"), "IceCoreTube missing FrostCap.");
            Assert.IsNotNull(tubePrefab.transform.Find("Center_Anchor"), "IceCoreTube missing Center_Anchor.");
            Assert.IsNotNull(tubePrefab.transform.Find("Window_Anchor"), "IceCoreTube missing Window_Anchor.");
            Assert.IsNotNull(tubePrefab.transform.Find("Handle_Anchor"), "IceCoreTube missing Handle_Anchor.");
            Assert.IsNotNull(tubePrefab.transform.Find("Frost_Anchor"), "IceCoreTube missing Frost_Anchor.");

            Assert.AreEqual(0, tubePrefab.GetComponentsInChildren<ParticleSystem>(true).Length,
                "IceCoreTube frost must be a material, not particles.");

            Bounds meshBounds = MeshChecks.PrefabBounds(tubePrefab, tubePrefab.transform.localToWorldMatrix);
            Assert.Less(meshBounds.center.magnitude, 0.01f,
                "IceCoreTube pivot must be at its centre (0, 0, 0), lying on its side.");
            Assert.AreEqual(0.45f, meshBounds.size.x, 0.015f, "IceCoreTube length must be ~0.45 m.");
            Assert.AreEqual(0.082f, meshBounds.size.y, 0.015f, "IceCoreTube height must be ~0.08 m.");
            Assert.AreEqual(0.082f, meshBounds.size.z, 0.015f, "IceCoreTube depth must be ~0.08 m.");

            Material labelMat = tubePrefab.transform.Find("LabelPlate").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreLabelMaterialName, labelMat.name);

            Material frostMat = tubePrefab.transform.Find("FrostCap").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreFrostMaterialName, frostMat.name);

            Material coreMat = tubePrefab.transform.Find("CoreSample").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreFrostMaterialName, coreMat.name);

            Material windowMat = tubePrefab.transform.Find("SightWindow").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreWindowMaterialName, windowMat.name);

            Material[] specializedMats = { labelMat, frostMat, windowMat };
            foreach (Material sm in specializedMats)
            {
                Assert.Less(sm.GetFloat("_BloomSafeMax"), Palette.BloomThreshold,
                    $"Specialized IceCoreTube material '{sm.name}' _BloomSafeMax must stay below Palette.BloomThreshold.");
                Assert.Less(sm.GetFloat("_HotCorePeak"), Palette.BloomThreshold,
                    $"Specialized IceCoreTube material '{sm.name}' _HotCorePeak must stay below Palette.BloomThreshold.");
            }

            Renderer[] renderers = tubePrefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m);
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        "IceCoreTube must have no lit R1_LampAmber material slots.");
                    Assert.AreEqual(0f, m.GetFloat("_EmissionStrength"), 1e-4f,
                        $"IceCoreTube material '{m.name}' must have zero emission (relying on headlamp specular/frost catch).");
                }
            }
        }

        [Test]
        public void CableKit_ExposesAnchors_SunkInRegolith_FrayedCopperPlugAndDeadSpliceLamp()
        {
            // 1. Cable_Straight_2m, Cable_Bend_45, Cable_Bend_90
            GameObject straightPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CableStraight2mPrefabPath);
            Transform sStart = straightPrefab.transform.Find("Start_Anchor");
            Transform sEnd = straightPrefab.transform.Find("End_Anchor");
            Assert.IsNotNull(sStart, "Cable_Straight_2m missing Start_Anchor.");
            Assert.IsNotNull(sEnd, "Cable_Straight_2m missing End_Anchor.");
            Assert.IsNotNull(straightPrefab.transform.Find("Clip_Anchor"), "Cable_Straight_2m missing Clip_Anchor.");
            Assert.AreEqual(2.0f, Vector3.Distance(sStart.localPosition, sEnd.localPosition), 0.01f,
                "Cable_Straight_2m Start_Anchor to End_Anchor span must be 2.0 m.");

            Transform straightJacket = straightPrefab.transform.Find("CableRun");
            Assert.IsNotNull(straightJacket, "Cable_Straight_2m missing CableRun.");
            Assert.IsNotNull(straightPrefab.transform.Find("StakeClip"), "Cable_Straight_2m missing StakeClip.");
            Mesh jacketMesh = straightJacket.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(jacketMesh);
            Assert.AreEqual(jacketMesh.vertexCount, jacketMesh.uv.Length, "CableJacket must have UV0 for top dust readability.");
            Color[] jacketColors = jacketMesh.colors;
            Assert.AreEqual(jacketMesh.vertexCount, jacketColors.Length, "CableJacket must have vertex colors for top dust readability.");
            float maxDustB = 0f;
            for (int i = 0; i < jacketColors.Length; i++)
            {
                if (jacketColors[i].b > maxDustB)
                {
                    maxDustB = jacketColors[i].b;
                }
            }
            Assert.Greater(maxDustB, 0.70f, "CableJacket top crown must have elevated vertex-color B channel so R1Lit dust layer reads on top.");

            GameObject bend45Prefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CableBend45PrefabPath);
            Assert.IsNotNull(bend45Prefab.transform.Find("Start_Anchor"), "Cable_Bend_45 missing Start_Anchor.");
            Assert.IsNotNull(bend45Prefab.transform.Find("End_Anchor"), "Cable_Bend_45 missing End_Anchor.");
            Assert.IsNotNull(bend45Prefab.transform.Find("Clip_Anchor"), "Cable_Bend_45 missing Clip_Anchor.");
            Assert.IsNotNull(bend45Prefab.transform.Find("CableRun"), "Cable_Bend_45 missing CableRun.");
            Assert.IsNotNull(bend45Prefab.transform.Find("StakeClip"), "Cable_Bend_45 missing StakeClip.");

            GameObject bend90Prefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CableBend90PrefabPath);
            Assert.IsNotNull(bend90Prefab.transform.Find("Start_Anchor"), "Cable_Bend_90 missing Start_Anchor.");
            Assert.IsNotNull(bend90Prefab.transform.Find("End_Anchor"), "Cable_Bend_90 missing End_Anchor.");
            Assert.IsNotNull(bend90Prefab.transform.Find("Clip_Anchor"), "Cable_Bend_90 missing Clip_Anchor.");
            Assert.IsNotNull(bend90Prefab.transform.Find("CableRun"), "Cable_Bend_90 missing CableRun.");
            Assert.IsNotNull(bend90Prefab.transform.Find("StakeClip"), "Cable_Bend_90 missing StakeClip.");

            // 2. Cable_BrokenEnd
            GameObject brokenPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CableBrokenEndPrefabPath);
            Assert.IsNotNull(brokenPrefab.transform.Find("Start_Anchor"), "Cable_BrokenEnd missing Start_Anchor.");
            Assert.IsNotNull(brokenPrefab.transform.Find("Break_Anchor"), "Cable_BrokenEnd missing Break_Anchor.");
            Assert.IsNotNull(brokenPrefab.transform.Find("CableStub"), "Cable_BrokenEnd missing CableStub.");
            Assert.IsNotNull(brokenPrefab.transform.Find("CrackedSleeve"), "Cable_BrokenEnd missing CrackedSleeve.");
            Transform frayedStrands = brokenPrefab.transform.Find("FrayedStrands");
            Assert.IsNotNull(frayedStrands, "Cable_BrokenEnd missing FrayedStrands.");
            Assert.AreEqual(R1MaterialRecipes.CopperSlot, frayedStrands.GetComponent<Renderer>().sharedMaterial.name,
                "Cable_BrokenEnd FrayedStrands must use R1_Copper.");

            // 3. Cable_Plug
            GameObject plugPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CablePlugPrefabPath);
            Assert.IsNotNull(plugPrefab.transform.Find("Start_Anchor"), "Cable_Plug missing Start_Anchor.");
            Transform plugAnchor = plugPrefab.transform.Find("Plug_Anchor");
            Assert.IsNotNull(plugAnchor, "Cable_Plug missing Plug_Anchor.");
            Vector3 plugWorldPos = plugPrefab.transform.TransformPoint(plugAnchor.localPosition);
            Assert.AreEqual(0.20f, plugWorldPos.y, 0.015f,
                "Cable_Plug Plug_Anchor must sit at Y = 0.20 m to seat into LanderServicePort and ChargingPad sockets.");
            Assert.IsNotNull(plugPrefab.transform.Find("CableLead"), "Cable_Plug missing CableLead.");
            Assert.IsNotNull(plugPrefab.transform.Find("PlugHead"), "Cable_Plug missing PlugHead.");

            // 4. Cable_Splice_Box
            GameObject splicePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CableSpliceBoxPrefabPath);
            Assert.IsNotNull(splicePrefab.transform.Find("Input_Anchor"), "Cable_Splice_Box missing Input_Anchor.");
            Assert.IsNotNull(splicePrefab.transform.Find("Output_Anchor"), "Cable_Splice_Box missing Output_Anchor.");
            Assert.IsNotNull(splicePrefab.transform.Find("StatusLamp_Dead"), "Cable_Splice_Box missing StatusLamp_Dead.");
            Assert.IsNotNull(splicePrefab.transform.Find("SpliceHousing"), "Cable_Splice_Box missing SpliceHousing.");
            Transform deadLamp = splicePrefab.transform.Find("DeadStatusLamp");
            Assert.IsNotNull(deadLamp, "Cable_Splice_Box missing DeadStatusLamp.");

            GameObject[] allKitPrefabs = { straightPrefab, bend45Prefab, bend90Prefab, brokenPrefab, plugPrefab, splicePrefab };
            for (int p = 0; p < allKitPrefabs.Length; p++)
            {
                Renderer[] kitRenderers = allKitPrefabs[p].GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in kitRenderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m);
                        Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                            $"Cable kit prefab '{allKitPrefabs[p].name}' must have no lit R1_LampAmber material slots.");
                    }
                }
            }
        }

        [Test]
        public void PropOldLanderAndDecalOldLanderBlast_HaveExpectedStructureCollidersMaterialsAndZeroEmission()
        {
            GameObject landerPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropOldLanderPrefabPath);
            string[] expectedChildren =
            {
                "Spaceframe",
                "LandingLegs",
                "MastAndSolar",
                "Array_Hinge",
                "TvCamera",
                "ScoopArm",
                "ThermalBoxes",
                "SerialStencil",
                "RemovedPartSpots",
                "Base_Anchor",
                "MastTop_Anchor",
                "Camera_Anchor",
                "Scoop_Anchor",
                "RemovedSpot_1_Anchor",
                "RemovedSpot_2_Anchor",
            };
            for (int i = 0; i < expectedChildren.Length; i++)
            {
                Assert.IsNotNull(landerPrefab.transform.Find(expectedChildren[i]),
                    $"Prop_OldLander missing expected child '{expectedChildren[i]}'.");
            }

            // Planar array exposed on its own hinge node (Array_Hinge) tilting about the mast arm axis
            Transform arrayHinge = landerPrefab.transform.Find("Array_Hinge");
            Assert.IsNotNull(arrayHinge.GetComponent<Renderer>(), "Prop_OldLander Array_Hinge must have a MeshRenderer.");
            // Children sit in the FBX root's local (Blender Z-up) axes; the root's +90 deg X conversion turns them Y-up.
            Vector3 hingeInPrefab = landerPrefab.transform.localRotation * arrayHinge.localPosition;
            Assert.AreEqual(2.46f, hingeInPrefab.y, 0.05f, "Prop_OldLander Array_Hinge pivot must sit on the mast arm axis.");

            // About 3 m tall, with one footpad half-buried below Y = 0
            Bounds landerBounds = MeshChecks.PrefabBounds(landerPrefab, landerPrefab.transform.localToWorldMatrix);
            Assert.AreEqual(3.0f, landerBounds.size.y, 0.10f, "Prop_OldLander height must be ~3.0 m.");
            Assert.Less(landerBounds.min.y, -0.08f, "Prop_OldLander must have one footpad half-buried below Y = 0.");

            // Simple BoxColliders on the prefab
            BoxCollider[] boxColliders = landerPrefab.GetComponentsInChildren<BoxCollider>(true);
            Assert.GreaterOrEqual(boxColliders.Length, 4, "Prop_OldLander must have simple BoxColliders.");

            // ThermalBoxes uses Era-P R1_WhitePaint body and R1_SilverFoil top mirror patches (no cream or gold foil)
            Renderer thermalRenderer = landerPrefab.transform.Find("ThermalBoxes").GetComponent<Renderer>();
            Assert.IsNotNull(thermalRenderer);
            bool hasWhitePaint = false;
            bool hasSilverFoil = false;
            foreach (Material m in thermalRenderer.sharedMaterials)
            {
                if (m != null && m.name == R1MaterialRecipes.WhitePaintSlot)
                {
                    hasWhitePaint = true;
                }
                if (m != null && m.name == R1MaterialRecipes.SilverFoilSlot)
                {
                    hasSilverFoil = true;
                }
                Assert.AreNotEqual(R1MaterialRecipes.CreamEnamelSlot, m?.name, "Prop_OldLander ThermalBoxes must not use R1_CreamEnamel.");
                Assert.AreNotEqual(R1MaterialRecipes.GoldFoilSlot, m?.name, "Prop_OldLander ThermalBoxes must not use R1_GoldFoil.");
            }
            Assert.IsTrue(hasWhitePaint, "Prop_OldLander ThermalBoxes must use R1_WhitePaint.");
            Assert.IsTrue(hasSilverFoil, "Prop_OldLander ThermalBoxes must use R1_SilverFoil mirror patches.");

            // Spaceframe and LandingLegs use bare aluminium (R1_BareSteel) with no painted R1_GreyMetal or R1_GoldFoil
            Renderer spaceframeRenderer = landerPrefab.transform.Find("Spaceframe").GetComponent<Renderer>();
            Renderer legsRenderer = landerPrefab.transform.Find("LandingLegs").GetComponent<Renderer>();
            Assert.IsNotNull(spaceframeRenderer);
            Assert.IsNotNull(legsRenderer);
            bool hasBareSteel = false;
            foreach (Material m in spaceframeRenderer.sharedMaterials)
            {
                if (m != null && m.name == R1MaterialRecipes.BareSteelSlot)
                {
                    hasBareSteel = true;
                }
                Assert.AreNotEqual(R1MaterialRecipes.GreyMetalSlot, m?.name, "Prop_OldLander Spaceframe must not use painted R1_GreyMetal.");
                Assert.AreNotEqual(R1MaterialRecipes.GoldFoilSlot, m?.name, "Prop_OldLander Spaceframe must not use R1_GoldFoil.");
            }
            Assert.IsTrue(hasBareSteel, "Prop_OldLander Spaceframe must use R1_BareSteel.");
            foreach (Material m in legsRenderer.sharedMaterials)
            {
                Assert.AreNotEqual(R1MaterialRecipes.GreyMetalSlot, m?.name, "Prop_OldLander LandingLegs must not use painted R1_GreyMetal.");
                Assert.AreNotEqual(R1MaterialRecipes.GoldFoilSlot, m?.name, "Prop_OldLander LandingLegs must not use R1_GoldFoil.");
            }

            // Long-dead lander: no lit R1_LampAmber or active emission
            AssertNoEmissiveMaterials(landerPrefab);

            // Decal_OldLanderBlast: BlastPatch + FootpadScuffs + Center_Anchor, shadow casting disabled
            GameObject decalPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.DecalOldLanderBlastPrefabPath);
            Assert.IsNotNull(decalPrefab.transform.Find("BlastPatch"), "Decal_OldLanderBlast missing BlastPatch.");
            Assert.IsNotNull(decalPrefab.transform.Find("FootpadScuffs"), "Decal_OldLanderBlast missing FootpadScuffs.");
            Assert.IsNotNull(decalPrefab.transform.Find("Center_Anchor"), "Decal_OldLanderBlast missing Center_Anchor.");

            Material patchMat = decalPrefab.transform.Find("BlastPatch").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(R1StoryPropsBuilder.OldLanderBlastDecalMaterialName, patchMat.name);

            Renderer[] decalRenderers = decalPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in decalRenderers)
            {
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode,
                    "Decal_OldLanderBlast renderers must have shadow casting disabled.");
            }
        }

        [Test]
        public void CrateCargoOpen_HasLanderFamilyMaterials_OpenSideAndLid_RaisedPartSocketsVisibleFrom4mBehind2mUp_AndSimpleShellColliders()
        {
            GameObject cratePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.CrateCargoOpenPrefabPath);
            Assert.AreEqual(Vector3.one, cratePrefab.transform.localScale, "Crate_Cargo_Open must be true size with no code scaling.");

            GameObject instance = UnityEngine.Object.Instantiate(cratePrefab);
            try
            {
                // 1. Verify required nodes
                Transform wallBack = instance.transform.Find("Wall_Back");
                Transform wallLeft = instance.transform.Find("Wall_Left");
                Transform wallRight = instance.transform.Find("Wall_Right");
                Transform sidePanelOpen = instance.transform.Find("SidePanel_Open");
                Transform lid = instance.transform.Find("Lid");
                Transform seal = instance.transform.Find("Lid/Seal");
                Transform cutPoint = instance.transform.Find("Lid/CutPoint");
                Transform rimGlow = instance.transform.Find("RimGlow");
                Transform labelPlate = instance.transform.Find("LabelPlate");
                Transform labelBack = instance.transform.Find("LabelPlate_Back");
                Transform labelLid = instance.transform.Find("LabelPlate_Lid");
                Transform socket0 = instance.transform.Find("PartSocket_0");
                Transform socket1 = instance.transform.Find("PartSocket_1");
                Transform itemSocket = instance.transform.Find("ItemSocket");

                Assert.IsNotNull(wallBack, "Crate_Cargo_Open missing Wall_Back.");
                Assert.IsNotNull(wallLeft, "Crate_Cargo_Open missing Wall_Left.");
                Assert.IsNotNull(wallRight, "Crate_Cargo_Open missing Wall_Right.");
                Assert.IsNotNull(sidePanelOpen, "Crate_Cargo_Open missing SidePanel_Open.");
                Assert.IsNotNull(lid, "Crate_Cargo_Open missing Lid.");
                Assert.IsNotNull(seal, "Crate_Cargo_Open missing Lid/Seal.");
                Assert.IsNotNull(cutPoint, "Crate_Cargo_Open missing Lid/CutPoint.");
                Assert.IsNotNull(rimGlow, "Crate_Cargo_Open missing RimGlow.");
                Assert.IsNotNull(labelPlate, "Crate_Cargo_Open missing LabelPlate.");
                Assert.IsNotNull(labelBack, "Crate_Cargo_Open missing LabelPlate_Back.");
                Assert.IsNotNull(labelLid, "Crate_Cargo_Open missing LabelPlate_Lid.");
                Assert.IsNotNull(socket0, "Crate_Cargo_Open missing PartSocket_0.");
                Assert.IsNotNull(socket1, "Crate_Cargo_Open missing PartSocket_1.");
                Assert.IsNotNull(itemSocket, "Crate_Cargo_Open missing ItemSocket.");

                Assert.IsNull(socket0.GetComponent<Renderer>(), "PartSocket_0 must be an empty transform.");
                Assert.IsNull(socket1.GetComponent<Renderer>(), "PartSocket_1 must be an empty transform.");

                // 2. Verify main crate shell + skid true size (~1.4 m long x 0.9 m wide x 0.7 m tall) sitting on ground
                Renderer[] shellRenderers =
                {
                    instance.GetComponent<Renderer>(),
                    wallBack.GetComponent<Renderer>(),
                    wallLeft.GetComponent<Renderer>(),
                    wallRight.GetComponent<Renderer>(),
                };
                Bounds shellBounds = shellRenderers[0].bounds;
                for (int i = 1; i < shellRenderers.Length; i++)
                {
                    shellBounds.Encapsulate(shellRenderers[i].bounds);
                }

                Assert.AreEqual(0.0f, shellBounds.min.y, 0.015f, "Crate_Cargo_Open skid must sit on the ground (Y = 0).");
                Assert.AreEqual(1.40f, shellBounds.size.x, 0.03f, "Crate_Cargo_Open shell length (X) must be ~1.4 m.");
                Assert.AreEqual(0.90f, shellBounds.size.z, 0.03f, "Crate_Cargo_Open shell width (Z) must be ~0.9 m.");
                Assert.AreEqual(0.70f, shellBounds.size.y, 0.03f, "Crate_Cargo_Open shell height (Y) must be ~0.7 m.");

                // 3. Verify SidePanel_Open is hinged down to the ground and Lid is lying beside the crate on the ground
                Bounds openPanelBounds = sidePanelOpen.GetComponent<Renderer>().bounds;
                Assert.AreEqual(0.0f, openPanelBounds.min.y, 0.02f, "SidePanel_Open must be hinged down to the ground.");
                Assert.Less(openPanelBounds.max.y, 0.16f, "SidePanel_Open must lie folded down open.");

                Bounds lidBounds = lid.GetComponent<Renderer>().bounds;
                Assert.AreEqual(0.0f, lidBounds.min.y, 0.02f, "Lid must lie on the ground beside the crate.");
                Assert.Less(lidBounds.max.y, 0.12f, "Lid must lie flat on the ground.");
                Assert.Greater(lidBounds.min.x, shellBounds.max.x - 0.02f, "Lid must lie beside the crate shell on the ground.");

                // 4. Verify CargoLander family materials & LUMEN CARGO stencil plaque material
                var matNames = new HashSet<string>();
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m);
                        matNames.Add(m.name);
                    }
                }

                Assert.IsTrue(matNames.Contains(R1MaterialRecipes.CreamEnamelSlot), "Must use R1_CreamEnamel panels.");
                Assert.IsTrue(matNames.Contains(R1MaterialRecipes.OrangeStripeSlot), "Must use R1_OrangeStripe stripe.");
                Assert.IsTrue(matNames.Contains(R1MaterialRecipes.RubberSlot), "Must use R1_Rubber foam cradles & shock pads.");
                Assert.IsTrue(matNames.Contains(R1StoryPropsBuilder.CargoCrateLabelMaterialName), "Must use R1_CargoCrateLabel stencil plaque.");
                Assert.AreEqual(R1StoryPropsBuilder.CargoCrateLabelMaterialName, labelPlate.GetComponent<Renderer>().sharedMaterial.name);
                Assert.AreEqual(R1StoryPropsBuilder.CrateRimGlowAmberMaterialName, rimGlow.GetComponent<Renderer>().sharedMaterial.name);

                // 5. Verify PartSocket_0 and PartSocket_1 are raised so parts placed there show above the rim from 4 m behind / 2 m up
                Assert.Greater(socket0.position.y, 0.50f, "PartSocket_0 must be raised on the internal foam cradle deck.");
                Assert.Greater(socket1.position.y, 0.50f, "PartSocket_1 must be raised on the internal foam cradle deck.");

                GameObject coilPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PadFragmentCoilPlatePrefabPath);
                GameObject railPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PadFragmentGuideRailPrefabPath);
                GameObject coilInst = UnityEngine.Object.Instantiate(coilPrefab, socket0.position, Quaternion.identity);
                GameObject railInst = UnityEngine.Object.Instantiate(railPrefab, socket1.position, Quaternion.identity);
                try
                {
                    Bounds coilBounds = coilInst.GetComponent<Renderer>().bounds;
                    Bounds railBounds = railInst.GetComponent<Renderer>().bounds;
                    const float centralRimHeight = 0.585f;
                    Assert.Greater(coilBounds.max.y, centralRimHeight + 0.05f,
                        "PadFragment_CoilPlate at PartSocket_0 must rise clearly above the crate rim.");
                    Assert.Greater(railBounds.max.y, centralRimHeight + 0.10f,
                        "PadFragment_GuideRail at PartSocket_1 must rise clearly above the crate rim.");

                    // 6. Verify shell colliders are simple boxes and nothing sits over PartSocket_0 or PartSocket_1
                    BoxCollider[] boxes = instance.GetComponentsInChildren<BoxCollider>(true);
                    Assert.AreEqual(6, boxes.Length, "Crate_Cargo_Open shell must have 6 simple BoxColliders (skid/floor/cradle, 3 walls, open panel, lid).");

                    Vector3[] checkPoints =
                    {
                        socket0.position + Vector3.up * 0.02f,
                        socket1.position + Vector3.up * 0.02f,
                        coilInst.transform.Find("Scan_1").position,
                        railInst.transform.Find("Scan_1").position,
                    };
                    Vector3[] cameraPositions =
                    {
                        new Vector3(0f, 2f, -4f),
                        new Vector3(0f, 2f, 4f),
                    };

                    Physics.SyncTransforms();
                    for (int b = 0; b < boxes.Length; b++)
                    {
                        Bounds wb = boxes[b].bounds;
                        for (int p = 0; p < checkPoints.Length; p++)
                        {
                            Assert.IsFalse(wb.Contains(checkPoints[p]),
                                $"BoxCollider on '{boxes[b].name}' ({wb}) must not cover socket/scan point {checkPoints[p]}.");
                        }
                    }

                    // Also verify that even if OpeningLayoutPlacer.Solidify adds convex MeshColliders to every MeshFilter,
                    // no collider blocks the line of sight from a camera 4 m behind and 2 m up to either socket/part!
                    foreach (MeshFilter mf in instance.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh != null)
                        {
                            var mc = mf.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            mc.convex = true;
                        }
                    }

                    Physics.SyncTransforms();
                    Collider[] allCrateColliders = instance.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < cameraPositions.Length; c++)
                    {
                        Vector3 camPos = cameraPositions[c];
                        for (int p = 0; p < checkPoints.Length; p++)
                        {
                            Vector3 target = checkPoints[p];
                            var ray = new Ray(camPos, (target - camPos).normalized);
                            float dist = Vector3.Distance(camPos, target);
                            for (int k = 0; k < allCrateColliders.Length; k++)
                            {
                                Assert.IsFalse(allCrateColliders[k].Raycast(ray, out RaycastHit hit, dist),
                                    $"Crate collider '{allCrateColliders[k].name}' blocked sightline from {camPos} to {target} at {hit.point}.");
                            }
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(coilInst);
                    UnityEngine.Object.DestroyImmediate(railInst);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PropMissionFlag_HasSunBleachedFlagSheet_BoxCollider_AndZeroEmission()
        {
            GameObject flagPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropMissionFlagPrefabPath);
            Assert.IsNotNull(flagPrefab.transform.Find("FlagPole"), "Prop_MissionFlag missing FlagPole.");
            Transform flagSheet = flagPrefab.transform.Find("FlagSheet");
            Assert.IsNotNull(flagSheet, "Prop_MissionFlag missing FlagSheet.");
            Assert.IsNotNull(flagPrefab.transform.Find("Base_Anchor"), "Prop_MissionFlag missing Base_Anchor.");
            Assert.IsNotNull(flagPrefab.transform.Find("Flag_Anchor"), "Prop_MissionFlag missing Flag_Anchor.");

            BoxCollider box = flagPrefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Prop_MissionFlag must have a simple BoxCollider on root.");
            Vector3 boxSize = ColliderSizeInPrefabAxes(flagPrefab, box);
            Assert.Greater(boxSize.x, 0.80f);
            Assert.Greater(boxSize.y, 1.45f);

            Renderer sheetRenderer = flagSheet.GetComponent<Renderer>();
            Assert.IsNotNull(sheetRenderer);
            bool foundStripeMat = false;
            foreach (Material m in sheetRenderer.sharedMaterials)
            {
                Assert.IsNotNull(m);
                if (m.name == R1StoryPropsBuilder.MissionFlagStripeMaterialName)
                {
                    foundStripeMat = true;
                }
            }

            Assert.IsTrue(foundStripeMat, "Prop_MissionFlag FlagSheet must use R1_MissionFlagStripe for faint sun-bleached stripes.");
            AssertNoEmissiveMaterials(flagPrefab);
        }

        [Test]
        public void PropSeismicStation_HasGoldFoilShroud_Cable_SquatCentralBox_DeadStatusLamp_AndBoxCollider()
        {
            GameObject seisPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropSeismicStationPrefabPath);
            Transform shroud = seisPrefab.transform.Find("FoilShroud");
            Assert.IsNotNull(shroud, "Prop_SeismicStation missing FoilShroud.");
            Assert.IsNotNull(seisPrefab.transform.Find("SeismometerUnit"), "Prop_SeismicStation missing SeismometerUnit.");
            Assert.IsNotNull(seisPrefab.transform.Find("UmbilicalCable"), "Prop_SeismicStation missing UmbilicalCable.");
            Assert.IsNotNull(seisPrefab.transform.Find("CentralBox"), "Prop_SeismicStation missing CentralBox.");
            Transform deadLamp = seisPrefab.transform.Find("DeadStatusLamp");
            Assert.IsNotNull(deadLamp, "Prop_SeismicStation missing DeadStatusLamp.");
            Assert.IsNotNull(seisPrefab.transform.Find("Seismometer_Anchor"), "Prop_SeismicStation missing Seismometer_Anchor.");
            Assert.IsNotNull(seisPrefab.transform.Find("CentralBox_Anchor"), "Prop_SeismicStation missing CentralBox_Anchor.");
            Assert.IsNotNull(seisPrefab.transform.Find("StatusLamp_Dead"), "Prop_SeismicStation missing StatusLamp_Dead.");

            BoxCollider box = seisPrefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Prop_SeismicStation must have a simple BoxCollider on root.");
            Vector3 boxSize = ColliderSizeInPrefabAxes(seisPrefab, box);
            Assert.Greater(boxSize.x, 1.0f);

            bool hasGoldFoil = false;
            foreach (Material m in shroud.GetComponent<Renderer>().sharedMaterials)
            {
                if (m != null && m.name == R1MaterialRecipes.GoldFoilSlot)
                {
                    hasGoldFoil = true;
                }
            }

            Assert.IsTrue(hasGoldFoil, "Prop_SeismicStation FoilShroud must use R1_GoldFoil.");
            AssertNoEmissiveMaterials(seisPrefab);
        }

        [Test]
        public void PropRetroreflector_HasTilted10x10CornerCubePrismArray_Legs_AndBoxCollider()
        {
            GameObject retroPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropRetroreflectorPrefabPath);
            Assert.IsNotNull(retroPrefab.transform.Find("SupportStand"), "Prop_Retroreflector missing SupportStand.");
            Assert.IsNotNull(retroPrefab.transform.Find("PanelHousing"), "Prop_Retroreflector missing PanelHousing.");
            Transform prismArray = retroPrefab.transform.Find("PrismArray");
            Assert.IsNotNull(prismArray, "Prop_Retroreflector missing PrismArray.");
            Assert.IsNotNull(retroPrefab.transform.Find("Base_Anchor"), "Prop_Retroreflector missing Base_Anchor.");
            Assert.IsNotNull(retroPrefab.transform.Find("Array_Anchor"), "Prop_Retroreflector missing Array_Anchor.");

            BoxCollider box = retroPrefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Prop_Retroreflector must have a simple BoxCollider on root.");
            Vector3 boxSize = ColliderSizeInPrefabAxes(retroPrefab, box);
            Assert.Greater(boxSize.x, 0.50f);

            Mesh prismMesh = prismArray.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(prismMesh);
            int prismTriangles = prismMesh.triangles.Length / 3;
            Assert.GreaterOrEqual(prismTriangles, 90,
                "PrismArray must contain sculpted 3x3 large corner-cube prism facets.");
            Assert.LessOrEqual(prismTriangles, 400,
                "PrismArray must use a few large corner-cube prisms (no dense field of small repeated cells).");

            bool hasWindowGlint = false;
            foreach (Material m in prismArray.GetComponent<Renderer>().sharedMaterials)
            {
                if (m != null && m.name == R1StoryPropsBuilder.IceCoreWindowMaterialName)
                {
                    hasWindowGlint = true;
                }
            }

            Assert.IsTrue(hasWindowGlint, "Prop_Retroreflector PrismArray must use high-glint optical window material.");
            AssertNoEmissiveMaterials(retroPrefab);
        }

        [Test]
        public void PropSurveyTripod_IsFallenWithTheodoliteHead_BuckledLeg_AndBoxCollider()
        {
            GameObject tripodPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropSurveyTripodPrefabPath);
            Assert.IsNotNull(tripodPrefab.transform.Find("TripodFrame"), "Prop_SurveyTripod missing TripodFrame.");
            Assert.IsNotNull(tripodPrefab.transform.Find("BuckledLeg"), "Prop_SurveyTripod missing BuckledLeg.");
            Assert.IsNotNull(tripodPrefab.transform.Find("TheodoliteHead"), "Prop_SurveyTripod missing TheodoliteHead.");
            Assert.IsNotNull(tripodPrefab.transform.Find("Base_Anchor"), "Prop_SurveyTripod missing Base_Anchor.");
            Assert.IsNotNull(tripodPrefab.transform.Find("Head_Anchor"), "Prop_SurveyTripod missing Head_Anchor.");
            Assert.IsNotNull(tripodPrefab.transform.Find("BuckledKnee_Anchor"), "Prop_SurveyTripod missing BuckledKnee_Anchor.");

            BoxCollider box = tripodPrefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Prop_SurveyTripod must have a simple BoxCollider on root.");
            Vector3 boxSize = ColliderSizeInPrefabAxes(tripodPrefab, box);
            Assert.Greater(boxSize.x, boxSize.y * 2.5f,
                "Prop_SurveyTripod must be fallen horizontally on the ground (width much greater than height).");

            AssertNoEmissiveMaterials(tripodPrefab);
        }

        [Test]
        public void PropSampleRack_HasRack_Rake_Tongs_Scoop_TwoRackedTubes_OneFallenTube_AndBoxCollider()
        {
            GameObject rackPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropSampleRackPrefabPath);
            Assert.IsNotNull(rackPrefab.transform.Find("RackFrame"), "Prop_SampleRack missing RackFrame.");
            Assert.IsNotNull(rackPrefab.transform.Find("ToolRake"), "Prop_SampleRack missing ToolRake.");
            Assert.IsNotNull(rackPrefab.transform.Find("ToolTongs"), "Prop_SampleRack missing ToolTongs.");
            Assert.IsNotNull(rackPrefab.transform.Find("ToolScoop"), "Prop_SampleRack missing ToolScoop.");
            Assert.IsNotNull(rackPrefab.transform.Find("CoreTubesRacked"), "Prop_SampleRack missing CoreTubesRacked.");
            Assert.IsNotNull(rackPrefab.transform.Find("CoreTubeFallen"), "Prop_SampleRack missing CoreTubeFallen.");
            Assert.IsNotNull(rackPrefab.transform.Find("Base_Anchor"), "Prop_SampleRack missing Base_Anchor.");
            Assert.IsNotNull(rackPrefab.transform.Find("Tools_Anchor"), "Prop_SampleRack missing Tools_Anchor.");
            Assert.IsNotNull(rackPrefab.transform.Find("FallenTube_Anchor"), "Prop_SampleRack missing FallenTube_Anchor.");

            BoxCollider box = rackPrefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Prop_SampleRack must have a simple BoxCollider on root.");
            Vector3 boxSize = ColliderSizeInPrefabAxes(rackPrefab, box);
            Assert.Greater(boxSize.x, 0.70f);
            Assert.Greater(boxSize.y, 0.75f);

            AssertNoEmissiveMaterials(rackPrefab);
        }

        [Test]
        public void DecalBootprintTrail_HasTileablePathAnchors_BootprintMaterial_AndNoCollider()
        {
            GameObject decalPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.DecalBootprintTrailPrefabPath);
            Transform trailSheet = decalPrefab.transform.Find("TrailSheet");
            Assert.IsNotNull(trailSheet, "Decal_BootprintTrail missing TrailSheet.");
            Transform startAnchor = decalPrefab.transform.Find("Start_Anchor");
            Transform endAnchor = decalPrefab.transform.Find("End_Anchor");
            Assert.IsNotNull(startAnchor, "Decal_BootprintTrail missing Start_Anchor.");
            Assert.IsNotNull(endAnchor, "Decal_BootprintTrail missing End_Anchor.");
            Assert.IsNotNull(decalPrefab.transform.Find("Center_Anchor"), "Decal_BootprintTrail missing Center_Anchor.");
            Assert.AreEqual(3.0f, Vector3.Distance(startAnchor.localPosition, endAnchor.localPosition), 0.02f,
                "Decal_BootprintTrail Start_Anchor to End_Anchor span must be 3.0 m for seamless path tiling.");

            Assert.AreEqual(0, decalPrefab.GetComponentsInChildren<Collider>(true).Length,
                "Decal_BootprintTrail is a ground imprint and must not collide; even a low lip along a long trail bumps 07's wheels.");

            Renderer renderer = trailSheet.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            Material mat = renderer.sharedMaterial;
            Assert.IsNotNull(mat);
            Assert.AreEqual(R1StoryPropsBuilder.BootprintTrailMaterialName, mat.name);
            Assert.AreEqual(R1StoryPropsBuilder.GroundImprintShaderName, mat.shader.name,
                "R1_BootprintTrail must project through the ground-imprint shader, not paint a slab of its own.");
            Assert.IsTrue(mat.shader.isSupported, "MoonProject/R1/GroundImprint must compile.");
            Assert.IsNotNull(mat.GetTexture("_BaseMap"), "R1_BootprintTrail must have _BaseMap assigned.");
            Assert.IsNotNull(mat.GetTexture("_BumpMap"), "R1_BootprintTrail must have _BumpMap assigned.");

            AssertNoEmissiveMaterials(decalPrefab);
        }

        [Test]
        public void GeophoneLine_SensorRibbonAndPlug_HaveExpectedStructureAnchorsSpikesStencilsAndZeroEmission()
        {
            // 1. Geophone_Sensor (~0.12 m tall squat cylinder on 3 short spikes, faded orange cap, stencil 1/2/3)
            GameObject sensorPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.GeophoneSensorPrefabPath);
            Transform housing = sensorPrefab.transform.Find("SensorHousing");
            Transform orangeCap = sensorPrefab.transform.Find("OrangeCap");
            Transform stencil1 = sensorPrefab.transform.Find("Stencil_1");
            Transform stencil2 = sensorPrefab.transform.Find("Stencil_2");
            Transform stencil3 = sensorPrefab.transform.Find("Stencil_3");
            Transform baseAnchor = sensorPrefab.transform.Find("Base_Anchor");
            Transform sStart = sensorPrefab.transform.Find("Start_Anchor");
            Transform sEnd = sensorPrefab.transform.Find("End_Anchor");
            Transform capAnchor = sensorPrefab.transform.Find("Cap_Anchor");

            Assert.IsNotNull(housing, "Geophone_Sensor missing SensorHousing.");
            Assert.IsNotNull(orangeCap, "Geophone_Sensor missing OrangeCap.");
            Assert.IsNotNull(stencil1, "Geophone_Sensor missing Stencil_1.");
            Assert.IsNotNull(stencil2, "Geophone_Sensor missing Stencil_2.");
            Assert.IsNotNull(stencil3, "Geophone_Sensor missing Stencil_3.");
            Assert.IsNotNull(baseAnchor, "Geophone_Sensor missing Base_Anchor.");
            Assert.IsNotNull(sStart, "Geophone_Sensor missing Start_Anchor.");
            Assert.IsNotNull(sEnd, "Geophone_Sensor missing End_Anchor.");
            Assert.IsNotNull(capAnchor, "Geophone_Sensor missing Cap_Anchor.");

            Bounds sensorBounds = MeshChecks.PrefabBounds(sensorPrefab, sensorPrefab.transform.localToWorldMatrix);
            Assert.AreEqual(0.12f, sensorBounds.size.y, 0.02f, "Geophone_Sensor height must be ~0.12 m.");
            Assert.Less(sensorBounds.min.y, -0.005f, "Geophone_Sensor 3 short spikes must penetrate slightly below Y = 0.");

            Quaternion sensorRot = sensorPrefab.transform.localRotation;
            Vector3 capPos = sensorRot * capAnchor.localPosition;
            Vector3 sStartPos = sensorRot * sStart.localPosition;
            Vector3 sEndPos = sensorRot * sEnd.localPosition;
            Assert.AreEqual(0.105f, capPos.y, 0.02f, "Geophone_Sensor Cap_Anchor must sit at top of cap (~0.105 m).");
            Assert.AreEqual(0.18f, Vector3.Distance(sStartPos, sEndPos), 0.02f,
                "Geophone_Sensor Start_Anchor to End_Anchor span must match the 0.18 m ribbon pass-through.");

            Assert.AreEqual(R1MaterialRecipes.BareSteelSlot, orangeCap.GetComponent<Renderer>().sharedMaterial.name,
                "Geophone_Sensor OrangeCap must use R1_BareSteel (1971 Era-S bare metal).");
            AssertNoEmissiveMaterials(sensorPrefab);

            // 2. Geophone_Ribbon_10m (10 m straight flat ribbon cable segment with Start/End anchors)
            GameObject ribbonPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.GeophoneRibbon10mPrefabPath);
            Transform ribbonRun = ribbonPrefab.transform.Find("RibbonRun");
            Transform ribbonClips = ribbonPrefab.transform.Find("RibbonClips");
            Transform rStart = ribbonPrefab.transform.Find("Start_Anchor");
            Transform rEnd = ribbonPrefab.transform.Find("End_Anchor");
            Assert.IsNotNull(ribbonRun, "Geophone_Ribbon_10m missing RibbonRun.");
            Assert.IsNotNull(ribbonClips, "Geophone_Ribbon_10m missing RibbonClips.");
            Assert.IsNotNull(rStart, "Geophone_Ribbon_10m missing Start_Anchor.");
            Assert.IsNotNull(rEnd, "Geophone_Ribbon_10m missing End_Anchor.");
            Assert.IsNotNull(ribbonPrefab.transform.Find("Clip_Anchor"), "Geophone_Ribbon_10m missing Clip_Anchor.");
            Assert.IsNotNull(ribbonPrefab.transform.Find("Center_Anchor"), "Geophone_Ribbon_10m missing Center_Anchor.");

            Quaternion ribbonRot = ribbonPrefab.transform.localRotation;
            Vector3 rStartPos = ribbonRot * rStart.localPosition;
            Vector3 rEndPos = ribbonRot * rEnd.localPosition;
            Assert.AreEqual(-5.0f, rStartPos.z, 0.02f, "Geophone_Ribbon_10m Start_Anchor must sit at Z = -5.0 m in prefab Y-up axes.");
            Assert.AreEqual(5.0f, rEndPos.z, 0.02f, "Geophone_Ribbon_10m End_Anchor must sit at Z = +5.0 m in prefab Y-up axes.");
            Assert.AreEqual(10.0f, Vector3.Distance(rStartPos, rEndPos), 0.02f,
                "Geophone_Ribbon_10m Start_Anchor to End_Anchor span must be 10.0 m.");

            Bounds ribbonBounds = MeshChecks.PrefabBounds(ribbonPrefab, ribbonPrefab.transform.localToWorldMatrix);
            Assert.Less(ribbonBounds.size.y, 0.04f, "Geophone_Ribbon_10m must be a thin flat ribbon lying on the regolith.");
            AssertNoEmissiveMaterials(ribbonPrefab);

            // 3. Geophone_Plug (cable end plug that seats at Prop_SeismicStation)
            GameObject plugPrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.GeophonePlugPrefabPath);
            Assert.IsNotNull(plugPrefab.transform.Find("RibbonLead"), "Geophone_Plug missing RibbonLead.");
            Assert.IsNotNull(plugPrefab.transform.Find("PlugHead"), "Geophone_Plug missing PlugHead.");
            Transform pStart = plugPrefab.transform.Find("Start_Anchor");
            Transform pPlug = plugPrefab.transform.Find("Plug_Anchor");
            Assert.IsNotNull(pStart, "Geophone_Plug missing Start_Anchor.");
            Assert.IsNotNull(pPlug, "Geophone_Plug missing Plug_Anchor.");
            Assert.IsNotNull(plugPrefab.transform.Find("Handle_Anchor"), "Geophone_Plug missing Handle_Anchor.");

            Quaternion plugRot = plugPrefab.transform.localRotation;
            Vector3 pPlugPos = plugRot * pPlug.localPosition;
            Assert.AreEqual(0.065f, pPlugPos.y, 0.02f,
                "Geophone_Plug Plug_Anchor must sit at Y ~= 0.065 m to seat at Prop_SeismicStation.");
            AssertNoEmissiveMaterials(plugPrefab);

            // 4. Prop_GeophoneLine (assembled 3-sensor geophone line with plug + ribbon cable + Sensor_1..3)
            GameObject linePrefab = MeshChecks.LoadGeneratedPrefab(R1StoryPropsBuilder.PropGeophoneLinePrefabPath);
            Assert.IsNotNull(linePrefab.transform.Find("RibbonCable"), "Prop_GeophoneLine missing RibbonCable.");
            Assert.IsNotNull(linePrefab.transform.Find("StationPlug"), "Prop_GeophoneLine missing StationPlug.");
            Transform lineS1 = linePrefab.transform.Find("Sensor_1");
            Transform lineS2 = linePrefab.transform.Find("Sensor_2");
            Transform lineS3 = linePrefab.transform.Find("Sensor_3");
            Assert.IsNotNull(lineS1, "Prop_GeophoneLine missing Sensor_1.");
            Assert.IsNotNull(lineS2, "Prop_GeophoneLine missing Sensor_2.");
            Assert.IsNotNull(lineS3, "Prop_GeophoneLine missing Sensor_3.");
            Assert.IsNotNull(linePrefab.transform.Find("Start_Anchor"), "Prop_GeophoneLine missing Start_Anchor.");
            Assert.IsNotNull(linePrefab.transform.Find("Plug_Anchor"), "Prop_GeophoneLine missing Plug_Anchor.");
            Assert.IsNotNull(linePrefab.transform.Find("Sensor_1_Anchor"), "Prop_GeophoneLine missing Sensor_1_Anchor.");
            Assert.IsNotNull(linePrefab.transform.Find("Sensor_2_Anchor"), "Prop_GeophoneLine missing Sensor_2_Anchor.");
            Assert.IsNotNull(linePrefab.transform.Find("Sensor_3_Anchor"), "Prop_GeophoneLine missing Sensor_3_Anchor.");
            Assert.IsNotNull(linePrefab.transform.Find("End_Anchor"), "Prop_GeophoneLine missing End_Anchor.");

            Quaternion lineRot = linePrefab.transform.localRotation;
            Vector3 l1Pos = lineRot * lineS1.localPosition;
            Vector3 l2Pos = lineRot * lineS2.localPosition;
            Vector3 l3Pos = lineRot * lineS3.localPosition;
            Assert.Less(l1Pos.z, l2Pos.z, "Sensor_1 must precede Sensor_2 along the geophone line.");
            Assert.Less(l2Pos.z, l3Pos.z, "Sensor_2 must precede Sensor_3 along the geophone line.");
            AssertNoEmissiveMaterials(linePrefab);
        }

        /// <summary>
        /// The collider is fitted in the FBX root's local axes, and that root carries Blender's +90 deg X axis
        /// conversion, so its local Z is the prop's height; rotate the size into the prefab's Y-up axes.
        /// </summary>
        private static Vector3 ColliderSizeInPrefabAxes(GameObject prefab, BoxCollider box)
        {
            Vector3 size = prefab.transform.localRotation * box.size;
            return new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        private static void AssertNoEmissiveMaterials(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m);
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        $"Prefab '{prefab.name}' must have no lit R1_LampAmber material slots.");
                    if (m.HasProperty("_EmissionStrength"))
                    {
                        Assert.AreEqual(0f, m.GetFloat("_EmissionStrength"), 1e-4f,
                            $"Prefab '{prefab.name}' material '{m.name}' must have zero emission.");
                    }
                }
            }
        }

        private static Bounds MeasureInstantiatedRendererBounds(GameObject prefab)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, $"Prefab '{prefab.name}' has no Renderer components.");
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combined.Encapsulate(renderers[i].bounds);
                }

                return combined;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssertAxisBounds(string contractFile, string kind, Vector3 actual, BoundsSpec spec)
        {
            string[] axisNames = { "width (X)", "length (Y/Z)", "height (Z/Y)" };
            for (int axis = 0; axis < 3; axis++)
            {
                float got = actual[axis];
                float expected = spec.size[axis];
                Assert.AreEqual(expected, got, spec.tolerance,
                    $"{contractFile} {kind} {axisNames[axis]} outside {expected:F4} +/- {spec.tolerance:F3} m");
                if (spec.min_size != null && spec.min_size.Length == 3)
                {
                    Assert.GreaterOrEqual(got, spec.min_size[axis] - 1e-4f,
                        $"{contractFile} {kind} {axisNames[axis]} below min_size");
                }

                if (spec.max_size != null && spec.max_size.Length == 3)
                {
                    Assert.LessOrEqual(got, spec.max_size[axis] + 1e-4f,
                        $"{contractFile} {kind} {axisNames[axis]} above max_size");
                }
            }
        }
    }
}
