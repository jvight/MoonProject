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
            ("pad_fragment_coil_plate.json", "Props/PadFragment_CoilPlate.fbx"),
            ("pad_fragment_guide_rail.json", "Props/PadFragment_GuideRail.fbx"),
            ("ice_core_tube.json", "Props/IceCoreTube.fbx"),
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
