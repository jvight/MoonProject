using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the R1 SSR-3 elder rover family models and prefabs:
    /// <list type="bullet">
    ///   <item><c>SSR3_Chassis</c> (<c>tools/art/qa/contracts/ssr3_chassis.json</c>): ~1.40 m L × 0.95 m W × 0.75 m H to deck,
    ///   4 wheels (<c>Wheel_FL</c>, <c>Wheel_FR</c>, <c>Wheel_RL</c>, <c>Wheel_RR</c>), single off-center <c>Headlamp</c> with
    ///   dark <c>R1_DarkGlass</c> lens, <c>Topper_Socket</c>, <c>Unit_Number</c>, no <c>R1_LampAmber</c>, and baked vertex colours.</item>
    ///   <item><c>SSR3_Topper_06</c> (<c>tools/art/qa/contracts/ssr3_topper_06.json</c>): 6-cell battery rack with <c>R1_Copper</c>
    ///   busbars (<c>BatteryRack</c>), 6 dark glass cell status tubes (<c>CellTubes</c>), rear flanged cable reel (<c>CableReel</c>)
    ///   with wound <c>R1_Rubber</c> cable, no <c>R1_LampAmber</c>, and baked vertex colours.</item>
    ///   <item><c>Elder06.prefab</c>: combined chassis + topper + <c>Unit_Number_06</c> (<c>06</c> stencil), settled ~4 cm into dust.</item>
    /// </list>
    /// </summary>
    public sealed class R1Ssr3Tests
    {
        private const string ContractsDir = "tools/art/qa/contracts";

        private static readonly (string ContractFile, string FbxRelativePath)[] Ssr3Contracts =
        {
            ("ssr3_chassis.json", "Rover/SSR3_Chassis.fbx"),
            ("ssr3_topper_06.json", "Rover/SSR3_Topper_06.fbx"),
            ("ssr3_tracks.json", "Rover/SSR3_Tracks.fbx"),
            ("ssr3_topper_03.json", "Rover/SSR3_Topper_03.fbx"),
            ("ssr3_topper_02.json", "Rover/SSR3_Topper_02.fbx"),
            ("ssr3_topper_05.json", "Rover/SSR3_Topper_05.fbx"),
            ("ssr3_topper_04.json", "Rover/SSR3_Topper_04.fbx"),
            ("ssr3_topper_01.json", "Rover/SSR3_Topper_01.fbx"),
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
            R1Ssr3Builder.Build();
        }

        private static IEnumerable<TestCaseData> ContractCases()
        {
            for (int i = 0; i < Ssr3Contracts.Length; i++)
            {
                (string contractFile, string fbxRelativePath) = Ssr3Contracts[i];
                yield return new TestCaseData(contractFile, fbxRelativePath)
                    .SetName($"Ssr3_{Path.GetFileNameWithoutExtension(contractFile)}_MatchesContractBounds");
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void Ssr3Prefab_RendererAndMeshBounds_MatchContract(string contractFile, string fbxRelativePath)
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
        public void Ssr3Chassis_ExposesRequiredRigSockets_UsesDarkHeadlamp_AndHasBakedWeathering()
        {
            GameObject chassisPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3ChassisPrefabPath);
            Assert.IsNotNull(chassisPrefab.transform.Find("Body"), "SSR3_Chassis missing Body.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Headlamp"), "SSR3_Chassis missing Headlamp.");
            Transform headlampGlass = chassisPrefab.transform.Find("Headlamp/HeadlampGlass");
            Assert.IsNotNull(headlampGlass, "SSR3_Chassis missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_FL"), "SSR3_Chassis missing Wheel_FL.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_FR"), "SSR3_Chassis missing Wheel_FR.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_RL"), "SSR3_Chassis missing Wheel_RL.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_RR"), "SSR3_Chassis missing Wheel_RR.");
            Transform topperSocket = chassisPrefab.transform.Find("Topper_Socket");
            Assert.IsNotNull(topperSocket, "SSR3_Chassis missing Topper_Socket.");
            Vector3 topperWorldPos = chassisPrefab.transform.TransformPoint(topperSocket.localPosition);
            Assert.AreEqual(0.69f, topperWorldPos.y, 0.02f, "Topper_Socket should sit on the dorsal deck (~0.69 m Y).");
            Assert.IsNotNull(chassisPrefab.transform.Find("Unit_Number"), "SSR3_Chassis missing Unit_Number.");

            Renderer glassRenderer = headlampGlass.GetComponent<Renderer>();
            Assert.IsNotNull(glassRenderer, "HeadlampGlass must have a Renderer.");
            Assert.AreEqual(R1MaterialRecipes.DarkGlassSlot, glassRenderer.sharedMaterial.name,
                "SSR3_Chassis HeadlampGlass must use dark R1_DarkGlass.");

            AssertNoLampAmberAndValidVertexColors(chassisPrefab, minRenderers: 7);
        }

        [Test]
        public void Ssr3Topper06_HasBatteryRack_DarkGlassCellTubes_AndRearCableReel()
        {
            GameObject topperPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper06PrefabPath);
            Transform batteryRack = topperPrefab.transform.Find("BatteryRack");
            Transform cellTubes = topperPrefab.transform.Find("CellTubes");
            Transform cableReel = topperPrefab.transform.Find("CableReel");

            Assert.IsNotNull(batteryRack, "SSR3_Topper_06 missing BatteryRack.");
            Assert.IsNotNull(cellTubes, "SSR3_Topper_06 missing CellTubes.");
            Assert.IsNotNull(cableReel, "SSR3_Topper_06 missing CableReel.");

            AssertNoLampAmberAndValidVertexColors(topperPrefab, minRenderers: 3);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topperPrefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CopperSlot), "SSR3_Topper_06 must use R1_Copper busbars.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.DarkGlassSlot), "SSR3_Topper_06 must use R1_DarkGlass cell status tubes.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_06 must use R1_Brass tube collars.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.SagePatchSlot), "SSR3_Topper_06 must use R1_SagePatch cable reel flanges.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.RubberSlot), "SSR3_Topper_06 must use R1_Rubber wound cable.");
        }

        [Test]
        public void Elder06Prefab_CombinesChassisTopperAndUnit06Stencil_Settled4CmIntoDust()
        {
            GameObject elder06Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder06PrefabPath);
            Assert.IsNotNull(elder06Prefab.transform.Find("Body"), "Elder06 missing Body.");
            Assert.IsNotNull(elder06Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder06 missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(elder06Prefab.transform.Find("Wheel_FL"), "Elder06 missing Wheel_FL.");
            Assert.IsNotNull(elder06Prefab.transform.Find("Wheel_FR"), "Elder06 missing Wheel_FR.");
            Assert.IsNotNull(elder06Prefab.transform.Find("Wheel_RL"), "Elder06 missing Wheel_RL.");
            Assert.IsNotNull(elder06Prefab.transform.Find("Wheel_RR"), "Elder06 missing Wheel_RR.");

            Transform mountedTopper = elder06Prefab.transform.Find("Topper_Socket/SSR3_Topper_06");
            Assert.IsNotNull(mountedTopper, "Elder06 missing Topper_Socket/SSR3_Topper_06.");
            Assert.IsNotNull(mountedTopper.Find("BatteryRack"), "Elder06 topper missing BatteryRack.");
            Assert.IsNotNull(mountedTopper.Find("CellTubes"), "Elder06 topper missing CellTubes.");
            Assert.IsNotNull(mountedTopper.Find("CableReel"), "Elder06 topper missing CableReel.");

            Transform unit06Node = elder06Prefab.transform.Find("Unit_Number/Unit_Number_06");
            Assert.IsNotNull(unit06Node, "Elder06 missing Unit_Number/Unit_Number_06.");
            MeshFilter unit06Mf = unit06Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit06Mf, "Unit_Number_06 must have a MeshFilter.");
            Assert.IsNotNull(unit06Mf.sharedMesh, "Unit_Number_06 must have a sharedMesh.");
            Assert.Greater(unit06Mf.sharedMesh.vertexCount, 24, "Unit_Number_06 mesh must contain the '06' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder06Prefab, minRenderers: 11);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder06Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder06DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder06 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(1.036f, elderBounds.max.y, 0.06f,
                "Elder06 top of cable reel / glass tubes should sit at ~1.04 m above ground.");
        }

        [Test]
        public void Ssr3Tracks_HasLeftAndRightTrackUnits_NoLampAmber_AndBakedWeathering()
        {
            GameObject tracksPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3TracksPrefabPath);
            Assert.IsNotNull(tracksPrefab.transform.Find("Track_L"), "SSR3_Tracks missing Track_L.");
            Assert.IsNotNull(tracksPrefab.transform.Find("Track_R"), "SSR3_Tracks missing Track_R.");

            AssertNoLampAmberAndValidVertexColors(tracksPrefab, minRenderers: 2);
        }

        [Test]
        public void Ssr3Topper03_HasCryoBox_CopperCoolingCoils_AndFrostedIceBlockWithFrostMaterial()
        {
            GameObject topper03Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper03PrefabPath);
            Transform cryoBox = topper03Prefab.transform.Find("CryoBox");
            Transform coolingCoils = topper03Prefab.transform.Find("CoolingCoils");
            Transform frostBlock = topper03Prefab.transform.Find("FrostBlock");

            Assert.IsNotNull(cryoBox, "SSR3_Topper_03 missing CryoBox.");
            Assert.IsNotNull(coolingCoils, "SSR3_Topper_03 missing CoolingCoils.");
            Assert.IsNotNull(frostBlock, "SSR3_Topper_03 missing FrostBlock.");

            Renderer frostRenderer = frostBlock.GetComponent<Renderer>();
            Assert.IsNotNull(frostRenderer, "FrostBlock must have a Renderer.");
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreFrostMaterialName, frostRenderer.sharedMaterial.name,
                "SSR3_Topper_03 FrostBlock must use R1_IceCoreFrost material (frost as material, no particles).");
            Assert.IsEmpty(topper03Prefab.GetComponentsInChildren<ParticleSystem>(true),
                "SSR3_Topper_03 must not use any ParticleSystem.");

            AssertNoLampAmberAndValidVertexColors(topper03Prefab, minRenderers: 3);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topper03Prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.GoldFoilSlot), "SSR3_Topper_03 must use R1_GoldFoil on CryoBox.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CopperSlot), "SSR3_Topper_03 must use R1_Copper cooling coils.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_03 must use R1_Brass fittings/fins.");
        }

        [Test]
        public void Elder03Prefab_ReplacesWheelsWithTracks_MountsTopper03AndUnit03Stencil_Settled4CmIntoDust()
        {
            GameObject elder03Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder03PrefabPath);
            Assert.IsNotNull(elder03Prefab.transform.Find("Body"), "Elder03 missing Body.");
            Assert.IsNotNull(elder03Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder03 missing Headlamp/HeadlampGlass.");
            Assert.IsNull(elder03Prefab.transform.Find("Wheel_FL"), "Elder03 must replace Wheel_FL with SSR3_Tracks.");
            Assert.IsNull(elder03Prefab.transform.Find("Wheel_FR"), "Elder03 must replace Wheel_FR with SSR3_Tracks.");
            Assert.IsNull(elder03Prefab.transform.Find("Wheel_RL"), "Elder03 must replace Wheel_RL with SSR3_Tracks.");
            Assert.IsNull(elder03Prefab.transform.Find("Wheel_RR"), "Elder03 must replace Wheel_RR with SSR3_Tracks.");

            Transform mountedTracks = elder03Prefab.transform.Find("SSR3_Tracks");
            Assert.IsNotNull(mountedTracks, "Elder03 missing SSR3_Tracks.");
            Assert.IsNotNull(mountedTracks.Find("Track_L"), "Elder03 SSR3_Tracks missing Track_L.");
            Assert.IsNotNull(mountedTracks.Find("Track_R"), "Elder03 SSR3_Tracks missing Track_R.");

            Transform mountedTopper = elder03Prefab.transform.Find("Topper_Socket/SSR3_Topper_03");
            Assert.IsNotNull(mountedTopper, "Elder03 missing Topper_Socket/SSR3_Topper_03.");
            Assert.IsNotNull(mountedTopper.Find("CryoBox"), "Elder03 topper missing CryoBox.");
            Assert.IsNotNull(mountedTopper.Find("CoolingCoils"), "Elder03 topper missing CoolingCoils.");
            Transform frostBlock = mountedTopper.Find("FrostBlock");
            Assert.IsNotNull(frostBlock, "Elder03 topper missing FrostBlock.");
            Assert.AreEqual(R1StoryPropsBuilder.IceCoreFrostMaterialName, frostBlock.GetComponent<Renderer>().sharedMaterial.name,
                "Elder03 FrostBlock must use R1_IceCoreFrost.");

            Transform unit03Node = elder03Prefab.transform.Find("Unit_Number/Unit_Number_03");
            Assert.IsNotNull(unit03Node, "Elder03 missing Unit_Number/Unit_Number_03.");
            MeshFilter unit03Mf = unit03Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit03Mf, "Unit_Number_03 must have a MeshFilter.");
            Assert.IsNotNull(unit03Mf.sharedMesh, "Unit_Number_03 must have a sharedMesh.");
            Assert.Greater(unit03Mf.sharedMesh.vertexCount, 24, "Unit_Number_03 mesh must contain the '03' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder03Prefab, minRenderers: 9);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder03Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder06DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder03 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(1.037f, elderBounds.max.y, 0.06f,
                "Elder03 top of FrostBlock should sit at ~1.04 m above ground.");
        }

        [Test]
        public void Ssr3Topper02_HasRollBar_FourDishFloodlights_AndDarkFloodlightLenses()
        {
            GameObject topper02Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper02PrefabPath);
            Transform rollBar = topper02Prefab.transform.Find("RollBar");
            Transform floodlights = topper02Prefab.transform.Find("Floodlights");
            Transform floodlightLenses = topper02Prefab.transform.Find("FloodlightLenses");

            Assert.IsNotNull(rollBar, "SSR3_Topper_02 missing RollBar.");
            Assert.IsNotNull(floodlights, "SSR3_Topper_02 missing Floodlights.");
            Assert.IsNotNull(floodlightLenses, "SSR3_Topper_02 missing FloodlightLenses.");

            Renderer lensesRenderer = floodlightLenses.GetComponent<Renderer>();
            Assert.IsNotNull(lensesRenderer, "FloodlightLenses must have a Renderer.");
            Assert.AreEqual(R1MaterialRecipes.DarkGlassSlot, lensesRenderer.sharedMaterial.name,
                "SSR3_Topper_02 FloodlightLenses must use dark R1_DarkGlass (lamps dark).");

            AssertNoLampAmberAndValidVertexColors(topper02Prefab, minRenderers: 3);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topper02Prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_02 must use R1_Brass parabolic dish reflectors.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.DarkGlassSlot), "SSR3_Topper_02 must use R1_DarkGlass lenses.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.RubberSlot), "SSR3_Topper_02 must use R1_Rubber floodlight power cables.");
        }

        [Test]
        public void Elder02Prefab_ReplacesWheelsWithTracks_MountsTopper02AndUnit02Stencil_Settled4CmIntoDust()
        {
            GameObject elder02Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder02PrefabPath);
            Assert.IsNotNull(elder02Prefab.transform.Find("Body"), "Elder02 missing Body.");
            Assert.IsNotNull(elder02Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder02 missing Headlamp/HeadlampGlass.");
            Assert.IsNull(elder02Prefab.transform.Find("Wheel_FL"), "Elder02 must replace Wheel_FL with SSR3_Tracks.");
            Assert.IsNull(elder02Prefab.transform.Find("Wheel_FR"), "Elder02 must replace Wheel_FR with SSR3_Tracks.");
            Assert.IsNull(elder02Prefab.transform.Find("Wheel_RL"), "Elder02 must replace Wheel_RL with SSR3_Tracks.");
            Assert.IsNull(elder02Prefab.transform.Find("Wheel_RR"), "Elder02 must replace Wheel_RR with SSR3_Tracks.");

            Transform mountedTracks = elder02Prefab.transform.Find("SSR3_Tracks");
            Assert.IsNotNull(mountedTracks, "Elder02 missing SSR3_Tracks.");
            Assert.IsNotNull(mountedTracks.Find("Track_L"), "Elder02 SSR3_Tracks missing Track_L.");
            Assert.IsNotNull(mountedTracks.Find("Track_R"), "Elder02 SSR3_Tracks missing Track_R.");

            Transform mountedTopper = elder02Prefab.transform.Find("Topper_Socket/SSR3_Topper_02");
            Assert.IsNotNull(mountedTopper, "Elder02 missing Topper_Socket/SSR3_Topper_02.");
            Assert.IsNotNull(mountedTopper.Find("RollBar"), "Elder02 topper missing RollBar.");
            Assert.IsNotNull(mountedTopper.Find("Floodlights"), "Elder02 topper missing Floodlights.");
            Assert.IsNotNull(mountedTopper.Find("FloodlightLenses"), "Elder02 topper missing FloodlightLenses.");

            Transform unit02Node = elder02Prefab.transform.Find("Unit_Number/Unit_Number_02");
            Assert.IsNotNull(unit02Node, "Elder02 missing Unit_Number/Unit_Number_02.");
            MeshFilter unit02Mf = unit02Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit02Mf, "Unit_Number_02 must have a MeshFilter.");
            Assert.IsNotNull(unit02Mf.sharedMesh, "Unit_Number_02 must have a sharedMesh.");
            Assert.Greater(unit02Mf.sharedMesh.vertexCount, 24, "Unit_Number_02 mesh must contain the '02' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder02Prefab, minRenderers: 9);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder02Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder06DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder02 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(1.25f, elderBounds.max.y, 0.06f,
                "Elder02 top of floodlights should sit at ~1.25 m above ground.");
        }

        [Test]
        public void Ssr3Topper05_HasTurntable_FoldedCopperCoilMast_TwoHornFeeds_AndRfCabinet()
        {
            GameObject topperPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper05PrefabPath);
            Transform turntable = topperPrefab.transform.Find("Turntable");
            Transform rfCabinet = topperPrefab.transform.Find("RfCabinet");
            Transform coilMast = topperPrefab.transform.Find("CoilMast");
            Transform hornFeeds = topperPrefab.transform.Find("HornFeeds");

            Assert.IsNotNull(turntable, "SSR3_Topper_05 missing Turntable.");
            Assert.IsNotNull(rfCabinet, "SSR3_Topper_05 missing RfCabinet.");
            Assert.IsNotNull(coilMast, "SSR3_Topper_05 missing CoilMast.");
            Assert.IsNotNull(hornFeeds, "SSR3_Topper_05 missing HornFeeds.");

            AssertNoLampAmberAndValidVertexColors(topperPrefab, minRenderers: 4);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topperPrefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CopperSlot), "SSR3_Topper_05 must use R1_Copper helical coil and coax lines.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_05 must use R1_Brass horn feeds and collars.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CreamEnamelSlot), "SSR3_Topper_05 must use R1_CreamEnamel turntable/cabinet.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.DarkGlassSlot), "SSR3_Topper_05 must use R1_DarkGlass waveguide throats.");
        }

        [Test]
        public void Elder05Prefab_CombinesChassisTopperAndUnit05Stencil_Settled4CmIntoDust()
        {
            GameObject elder05Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder05PrefabPath);
            Assert.IsNotNull(elder05Prefab.transform.Find("Body"), "Elder05 missing Body.");
            Assert.IsNotNull(elder05Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder05 missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(elder05Prefab.transform.Find("Wheel_FL"), "Elder05 missing Wheel_FL.");
            Assert.IsNotNull(elder05Prefab.transform.Find("Wheel_FR"), "Elder05 missing Wheel_FR.");
            Assert.IsNotNull(elder05Prefab.transform.Find("Wheel_RL"), "Elder05 missing Wheel_RL.");
            Assert.IsNotNull(elder05Prefab.transform.Find("Wheel_RR"), "Elder05 missing Wheel_RR.");

            Transform mountedTopper = elder05Prefab.transform.Find("Topper_Socket/SSR3_Topper_05");
            Assert.IsNotNull(mountedTopper, "Elder05 missing Topper_Socket/SSR3_Topper_05.");
            Assert.IsNotNull(mountedTopper.Find("Turntable"), "Elder05 topper missing Turntable.");
            Assert.IsNotNull(mountedTopper.Find("RfCabinet"), "Elder05 topper missing RfCabinet.");
            Assert.IsNotNull(mountedTopper.Find("CoilMast"), "Elder05 topper missing CoilMast.");
            Assert.IsNotNull(mountedTopper.Find("HornFeeds"), "Elder05 topper missing HornFeeds.");

            Transform unit05Node = elder05Prefab.transform.Find("Unit_Number/Unit_Number_05");
            Assert.IsNotNull(unit05Node, "Elder05 missing Unit_Number/Unit_Number_05.");
            MeshFilter unit05Mf = unit05Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit05Mf, "Unit_Number_05 must have a MeshFilter.");
            Assert.IsNotNull(unit05Mf.sharedMesh, "Unit_Number_05 must have a sharedMesh.");
            Assert.Greater(unit05Mf.sharedMesh.vertexCount, 24, "Unit_Number_05 mesh must contain the '05' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder05Prefab, minRenderers: 12);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder05Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder05DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder05 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(1.37f, elderBounds.max.y, 0.06f,
                "Elder05 top of half-folded copper coil mast should sit at ~1.37 m above ground.");
        }

        [Test]
        public void Ssr3Topper04_HasGasBottles_CopperLines_JumpThrusters_AndSuspensionKit()
        {
            GameObject topperPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper04PrefabPath);
            Transform gasBottles = topperPrefab.transform.Find("GasBottles");
            Transform copperLines = topperPrefab.transform.Find("CopperLines");
            Transform jumpThrusters = topperPrefab.transform.Find("JumpThrusters");
            Transform suspensionKit = topperPrefab.transform.Find("SuspensionKit");

            Assert.IsNotNull(gasBottles, "SSR3_Topper_04 missing GasBottles.");
            Assert.IsNotNull(copperLines, "SSR3_Topper_04 missing CopperLines.");
            Assert.IsNotNull(jumpThrusters, "SSR3_Topper_04 missing JumpThrusters.");
            Assert.IsNotNull(suspensionKit, "SSR3_Topper_04 missing SuspensionKit.");

            AssertNoLampAmberAndValidVertexColors(topperPrefab, minRenderers: 4);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topperPrefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_04 must use R1_Brass gas bottles and thruster bells.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CopperSlot), "SSR3_Topper_04 must use R1_Copper pressure lines and suspension coils.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.DarkGlassSlot), "SSR3_Topper_04 must use R1_DarkGlass pressure gauge faces.");
        }

        [Test]
        public void Elder04Prefab_CombinesLiftedChassisTopperAndUnit04Stencil_Settled4CmIntoDust()
        {
            GameObject elder04Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder04PrefabPath);
            Assert.IsNotNull(elder04Prefab.transform.Find("Body"), "Elder04 missing Body.");
            Assert.IsNotNull(elder04Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder04 missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(elder04Prefab.transform.Find("Wheel_FL"), "Elder04 missing Wheel_FL.");
            Assert.IsNotNull(elder04Prefab.transform.Find("Wheel_FR"), "Elder04 missing Wheel_FR.");
            Assert.IsNotNull(elder04Prefab.transform.Find("Wheel_RL"), "Elder04 missing Wheel_RL.");
            Assert.IsNotNull(elder04Prefab.transform.Find("Wheel_RR"), "Elder04 missing Wheel_RR.");

            Transform topperSocket = elder04Prefab.transform.Find("Topper_Socket");
            Assert.IsNotNull(topperSocket, "Elder04 missing Topper_Socket.");
            Vector3 topperWorldPos = elder04Prefab.transform.TransformPoint(topperSocket.localPosition);
            Assert.AreEqual(0.69f + R1Ssr3Builder.Elder04SuspensionLiftMeters - R1Ssr3Builder.Elder04DustSettleMeters,
                topperWorldPos.y, 0.02f,
                "Elder04 Topper_Socket must be lifted by the raised suspension kit (+0.14 m) minus dust settle (-0.04 m).");

            Transform mountedTopper = elder04Prefab.transform.Find("Topper_Socket/SSR3_Topper_04");
            Assert.IsNotNull(mountedTopper, "Elder04 missing Topper_Socket/SSR3_Topper_04.");
            Assert.IsNotNull(mountedTopper.Find("GasBottles"), "Elder04 topper missing GasBottles.");
            Assert.IsNotNull(mountedTopper.Find("CopperLines"), "Elder04 topper missing CopperLines.");
            Assert.IsNotNull(mountedTopper.Find("JumpThrusters"), "Elder04 topper missing JumpThrusters.");
            Assert.IsNotNull(mountedTopper.Find("SuspensionKit"), "Elder04 topper missing SuspensionKit.");

            Transform unit04Node = elder04Prefab.transform.Find("Unit_Number/Unit_Number_04");
            Assert.IsNotNull(unit04Node, "Elder04 missing Unit_Number/Unit_Number_04.");
            MeshFilter unit04Mf = unit04Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit04Mf, "Unit_Number_04 must have a MeshFilter.");
            Assert.IsNotNull(unit04Mf.sharedMesh, "Unit_Number_04 must have a sharedMesh.");
            Assert.Greater(unit04Mf.sharedMesh.vertexCount, 24, "Unit_Number_04 mesh must contain the '04' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder04Prefab, minRenderers: 12);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder04Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder04DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder04 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(1.078f, elderBounds.max.y, 0.06f,
                "Elder04 top of lifted gas bottles should sit at ~1.08 m above ground.");
        }

        [Test]
        public void Ssr3Topper01_HasLatticeMast_AltAzYoke_TiltedTelescopeTube_CalibrationPlate_AndBakedDustWithoutParticles()
        {
            GameObject topperPrefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Ssr3Topper01PrefabPath);
            Transform latticeMast = topperPrefab.transform.Find("LatticeMast");
            Transform altAzYoke = topperPrefab.transform.Find("AltAzYoke");
            Transform telescopeTube = topperPrefab.transform.Find("TelescopeTube");
            Transform calibrationPlate = topperPrefab.transform.Find("CalibrationPlate");

            Assert.IsNotNull(latticeMast, "SSR3_Topper_01 missing LatticeMast.");
            Assert.IsNotNull(altAzYoke, "SSR3_Topper_01 missing AltAzYoke.");
            Assert.IsNotNull(telescopeTube, "SSR3_Topper_01 missing TelescopeTube.");
            Assert.IsNotNull(calibrationPlate, "SSR3_Topper_01 missing CalibrationPlate.");

            Assert.IsEmpty(topperPrefab.GetComponentsInChildren<ParticleSystem>(true),
                "SSR3_Topper_01 must not use any ParticleSystem (dust is baked into vertex colours / material).");

            AssertNoLampAmberAndValidVertexColors(topperPrefab, minRenderers: 4);

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in topperPrefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CreamEnamelSlot), "SSR3_Topper_01 must use R1_CreamEnamel.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.OrangeStripeSlot), "SSR3_Topper_01 must use R1_OrangeStripe.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.BrassSlot), "SSR3_Topper_01 must use R1_Brass on alt-az yoke and collars.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.CopperSlot), "SSR3_Topper_01 must use R1_Copper cable runs down the mast.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.RubberSlot), "SSR3_Topper_01 must use R1_Rubber umbilical cables down the mast.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.DarkGlassSlot), "SSR3_Topper_01 must use R1_DarkGlass optical lenses.");

            // Verify TelescopeTube is tilted down toward the front CalibrationPlate at rest and carries settled dust in vertex-colour B on upward faces
            Mesh calMesh = calibrationPlate.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(calMesh, "CalibrationPlate must have a sharedMesh.");
            Vector3 calCenter = calibrationPlate.TransformPoint(calMesh.bounds.center);
            float frontSign = Mathf.Sign(calCenter.z);

            Mesh tubeMesh = telescopeTube.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(tubeMesh, "TelescopeTube must have a sharedMesh.");
            Vector3[] verts = tubeMesh.vertices;
            Vector3[] norms = tubeMesh.normals;
            Color[] cols = tubeMesh.colors;

            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p = telescopeTube.TransformPoint(verts[i]);
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            float tubeMidZ = (minZ + maxZ) * 0.5f;
            float frontSumY = 0f;
            int frontCount = 0;
            float rearSumY = 0f;
            int rearCount = 0;
            float upwardDustMax = 0f;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p = telescopeTube.TransformPoint(verts[i]);
                Vector3 n = telescopeTube.TransformDirection(norms[i]).normalized;
                float signedForwardZ = (p.z - tubeMidZ) * frontSign;
                if (signedForwardZ > 0.18f)
                {
                    frontSumY += p.y;
                    frontCount++;
                }
                else if (signedForwardZ < -0.18f)
                {
                    rearSumY += p.y;
                    rearCount++;
                }

                if (n.y > 0.5f && cols[i].b > upwardDustMax)
                {
                    upwardDustMax = cols[i].b;
                }
            }

            Assert.Greater(frontCount, 0, "TelescopeTube must have vertices on the forward sun-shade hood.");
            Assert.Greater(rearCount, 0, "TelescopeTube must have vertices on the rear sensor breech.");
            Assert.Less(frontSumY / frontCount, rearSumY / rearCount - 0.08f,
                "TelescopeTube must be tilted down at rest (forward sun-shade hood lower in Y than rear breech).");
            Assert.Greater(upwardDustMax, 0.35f,
                "Upward-facing surfaces on TelescopeTube must carry settled vertex-colour dust (B channel).");
        }

        [Test]
        public void Elder01Prefab_CombinesChassisWheelsTopperAndUnit01Stencil_ReachesAbout2Point6M_Settled4CmIntoDust()
        {
            GameObject elder01Prefab = MeshChecks.LoadGeneratedPrefab(R1Ssr3Builder.Elder01PrefabPath);
            Assert.IsNotNull(elder01Prefab.transform.Find("Body"), "Elder01 missing Body.");
            Assert.IsNotNull(elder01Prefab.transform.Find("Headlamp/HeadlampGlass"), "Elder01 missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(elder01Prefab.transform.Find("Wheel_FL"), "Elder01 missing Wheel_FL.");
            Assert.IsNotNull(elder01Prefab.transform.Find("Wheel_FR"), "Elder01 missing Wheel_FR.");
            Assert.IsNotNull(elder01Prefab.transform.Find("Wheel_RL"), "Elder01 missing Wheel_RL.");
            Assert.IsNotNull(elder01Prefab.transform.Find("Wheel_RR"), "Elder01 missing Wheel_RR.");

            Transform mountedTopper = elder01Prefab.transform.Find("Topper_Socket/SSR3_Topper_01");
            Assert.IsNotNull(mountedTopper, "Elder01 missing Topper_Socket/SSR3_Topper_01.");
            Assert.IsNotNull(mountedTopper.Find("LatticeMast"), "Elder01 topper missing LatticeMast.");
            Assert.IsNotNull(mountedTopper.Find("AltAzYoke"), "Elder01 topper missing AltAzYoke.");
            Assert.IsNotNull(mountedTopper.Find("TelescopeTube"), "Elder01 topper missing TelescopeTube.");
            Assert.IsNotNull(mountedTopper.Find("CalibrationPlate"), "Elder01 topper missing CalibrationPlate.");

            Transform unit01Node = elder01Prefab.transform.Find("Unit_Number/Unit_Number_01");
            Assert.IsNotNull(unit01Node, "Elder01 missing Unit_Number/Unit_Number_01.");
            MeshFilter unit01Mf = unit01Node.GetComponent<MeshFilter>();
            Assert.IsNotNull(unit01Mf, "Unit_Number_01 must have a MeshFilter.");
            Assert.IsNotNull(unit01Mf.sharedMesh, "Unit_Number_01 must have a sharedMesh.");
            Assert.Greater(unit01Mf.sharedMesh.vertexCount, 24, "Unit_Number_01 mesh must contain the '01' stencil geometry.");

            AssertNoLampAmberAndValidVertexColors(elder01Prefab, minRenderers: 12);

            Bounds elderBounds = MeasureInstantiatedRendererBounds(elder01Prefab);
            Assert.AreEqual(-R1Ssr3Builder.Elder01DustSettleMeters, elderBounds.min.y, 0.01f,
                "Elder01 must be settled ~4 cm (-0.04 m Y) into the lunar dust.");
            Assert.AreEqual(2.636f, elderBounds.max.y, 0.06f,
                "Elder01 tall star-tracker mast should reach ~2.6 m above ground.");
        }

        private static void AssertNoLampAmberAndValidVertexColors(GameObject prefab, int minRenderers)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.GreaterOrEqual(renderers.Length, minRenderers,
                $"Prefab '{prefab.name}' must have at least {minRenderers} mesh renderers.");
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, $"Null material on '{prefab.name}/{r.name}'.");
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        $"'{prefab.name}/{r.name}' must not use active R1_LampAmber.");
                }
            }

            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in filters)
            {
                Mesh mesh = mf.sharedMesh;
                Assert.IsNotNull(mesh, $"Missing sharedMesh on '{prefab.name}/{mf.name}'.");
                Color[] colors = mesh.colors;
                Assert.IsNotNull(colors, $"Mesh '{prefab.name}/{mesh.name}' is missing vertex colors.");
                Assert.AreEqual(mesh.vertexCount, colors.Length,
                    $"Mesh '{prefab.name}/{mesh.name}' vertex color count mismatch.");
            }
        }

        private static Bounds MeasureInstantiatedRendererBounds(GameObject prefab)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(renderers, $"Prefab '{prefab.name}' has no Renderer components.");
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

        private static void AssertAxisBounds(string contractFile, string kind, Vector3 actual, BoundsSpec expected)
        {
            string[] axisNames = { "width (X)", "length (Z)", "height (Y)" };
            for (int axis = 0; axis < 3; axis++)
            {
                float actualVal = actual[axis];
                float targetVal = expected.size[axis];
                float minVal = expected.min_size != null && expected.min_size.Length == 3
                    ? expected.min_size[axis]
                    : targetVal - expected.tolerance;
                float maxVal = expected.max_size != null && expected.max_size.Length == 3
                    ? expected.max_size[axis]
                    : targetVal + expected.tolerance;

                Assert.GreaterOrEqual(actualVal, minVal,
                    $"[{contractFile}] {kind} {axisNames[axis]} = {actualVal:F3} m is below contract min {minVal:F3} m (target {targetVal:F3} m).");
                Assert.LessOrEqual(actualVal, maxVal,
                    $"[{contractFile}] {kind} {axisNames[axis]} = {actualVal:F3} m exceeds contract max {maxVal:F3} m (target {targetVal:F3} m).");
            }
        }
    }
}
