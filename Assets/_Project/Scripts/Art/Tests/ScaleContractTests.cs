using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies that every generated R1 prefab in <c>Assets/_Project/Generated/Art/R1/Models/</c> matches the
    /// world-space bounding-box contract (<c>bounds.size</c>, <c>bounds.tolerance</c>, <c>bounds.min_size</c>,
    /// <c>bounds.max_size</c>) in <c>tools/art/qa/contracts/*.json</c> (docs/design/scale.md).
    /// </summary>
    public sealed class ScaleContractTests
    {
        private const string ContractsDir = "tools/art/qa/contracts";

        private static readonly (string ContractFile, string FbxRelativePath)[] Contracts =
        {
            ("antenna_mast.json", "Structures/AntennaMast.fbx"),
            ("battery_bank.json", "Structures/BatteryBank.fbx"),
            ("cargo_lander.json", "Lander/CargoLander.fbx"),
            ("charging_pad.json", "Structures/ChargingPad.fbx"),
            ("crate_cargo_open.json", "Crates/Crate_Cargo_Open.fbx"),
            ("crate_l.json", "Crates/Crate_L.fbx"),
            ("crate_m.json", "Crates/Crate_M.fbx"),
            ("crate_s.json", "Crates/Crate_S.fbx"),
            ("fallen_solar_array.json", "Ruins/FallenSolarArray.fbx"),
            ("focus_lens.json", "Rover/FocusLens.fbx"),
            ("habitat_modules.json", "Ruins/HabitatModules.fbx"),
            ("hover_coils.json", "Rover/HoverCoils.fbx"),
            ("kenji_workshop.json", "Ruins/KenjiWorkshop.fbx"),
            ("kestrel_antenna_boom.json", "Wreck/Kestrel_AntennaBoom.fbx"),
            ("kestrel_cable_tray.json", "Wreck/Kestrel_CableTray.fbx"),
            ("kestrel_engine_bell.json", "Wreck/Kestrel_EngineBell.fbx"),
            ("kestrel_hull_panel.json", "Wreck/Kestrel_HullPanel.fbx"),
            ("kestrel_landing_foot.json", "Wreck/Kestrel_LandingFoot.fbx"),
            ("kestrel_strut_frame.json", "Wreck/Kestrel_StrutFrame.fbx"),
            ("kestrel_tank.json", "Wreck/Kestrel_Tank.fbx"),
            ("kit_battery_spine.json", "Rover/Kit_BatterySpine.fbx"),
            ("kit_capacitor_drum.json", "Rover/Kit_CapacitorDrum.fbx"),
            ("kit_cargo_rack.json", "Rover/Kit_CargoRack.fbx"),
            ("kit_lamp_bar.json", "Rover/Kit_LampBar.fbx"),
            ("kit_side_bin.json", "Rover/Kit_SideBin.fbx"),
            ("kit_tether_ring.json", "Rover/Kit_TetherRing.fbx"),
            ("lens_glass.json", "Rover/LensGlass.fbx"),
            ("power_pylon.json", "Structures/PowerPylon.fbx"),
            ("radio_tower.json", "Ruins/RadioTower.fbx"),
            ("rock_00.json", "Rocks/Rock_00.fbx"),
            ("rock_01.json", "Rocks/Rock_01.fbx"),
            ("rock_02.json", "Rocks/Rock_02.fbx"),
            ("rock_03.json", "Rocks/Rock_03.fbx"),
            ("rock_04.json", "Rocks/Rock_04.fbx"),
            ("rock_05.json", "Rocks/Rock_05.fbx"),
            ("rock_06.json", "Rocks/Rock_06.fbx"),
            ("rock_07.json", "Rocks/Rock_07.fbx"),
            ("rock_08.json", "Rocks/Rock_08.fbx"),
            ("rock_09.json", "Rocks/Rock_09.fbx"),
            ("rock_10.json", "Rocks/Rock_10.fbx"),
            ("rock_11.json", "Rocks/Rock_11.fbx"),
            ("rock_12.json", "Rocks/Rock_12.fbx"),
            ("rock_13.json", "Rocks/Rock_13.fbx"),
            ("rockfall_boulder_00.json", "Rocks/Rockfall_Boulder_00.fbx"),
            ("rockfall_boulder_01.json", "Rocks/Rockfall_Boulder_01.fbx"),
            ("rockfall_boulder_02.json", "Rocks/Rockfall_Boulder_02.fbx"),
            ("rockfall_boulder_03.json", "Rocks/Rockfall_Boulder_03.fbx"),
            ("rockfall_talus_slab.json", "Rocks/Rockfall_TalusSlab.fbx"),
            ("rover.json", "Rover/RoverModel.fbx"),
            ("scan_fan.json", "Rover/ScanFan.fbx"),
            ("scatter_set.json", "Ruins/ScatterSet.fbx"),
            ("solar_array.json", "Structures/SolarArray.fbx"),
            ("solar_wing.json", "Rover/SolarWing.fbx"),
            ("tape_deck.json", "Rover/Kit_TapeDeck.fbx"),
            ("mare_ground_patch.json", "Mare/MareGround_Patch.fbx"),
            ("decal_mare_swirl.json", "Mare/Decal_MareSwirl.fbx"),
            ("decal_mare_ejecta_ray.json", "Mare/Decal_MareEjectaRay.fbx"),
            ("mare_rock_00.json", "Mare/MareRock_00.fbx"),
            ("mare_rock_01.json", "Mare/MareRock_01.fbx"),
            ("mare_rock_02.json", "Mare/MareRock_02.fbx"),
            ("mare_rock_03.json", "Mare/MareRock_03.fbx"),
            ("mare_rock_04.json", "Mare/MareRock_04.fbx"),
            ("mare_rock_05.json", "Mare/MareRock_05.fbx"),
            ("mare_outcrop_ilmenite_00.json", "Mare/MareOutcrop_Ilmenite_00.fbx"),
            ("mare_outcrop_ilmenite_01.json", "Mare/MareOutcrop_Ilmenite_01.fbx"),
            ("rille_wall_straight.json", "Mare/Rille_Wall_Straight.fbx"),
            ("rille_wall_curve.json", "Mare/Rille_Wall_Curve.fbx"),
            ("rille_slope_ramp.json", "Mare/Rille_Slope_Ramp.fbx"),
            ("rille_mouth_corner.json", "Mare/Rille_Mouth_Corner.fbx"),
            ("derelict_mare_convoy.json", "Mare/Derelict_MareConvoy.fbx"),
            ("derelict_mare_survey_drone.json", "Mare/Derelict_MareSurveyDrone.fbx"),
            ("derelict_tardigrade_capsule.json", "Mare/Derelict_TardigradeCapsule.fbx"),
            ("panel_mite.json", "Fauna/PanelMite.fbx"),
            ("sweeper.json", "Fauna/Sweeper.fbx"),
            ("survey_drone.json", "Fauna/SurveyDrone.fbx"),
            ("trencher.json", "Fauna/Trencher.fbx"),
            ("prop_spare_igniter.json", "Props/Prop_SpareIgniter.fbx"),
            ("rim_talus_00.json", "Rocks/RimTalus_00.fbx"),
            ("rim_talus_01.json", "Rocks/RimTalus_01.fbx"),
            ("rim_talus_02.json", "Rocks/RimTalus_02.fbx"),
            ("rim_talus_03.json", "Rocks/RimTalus_03.fbx"),
            ("rim_talus_04.json", "Rocks/RimTalus_04.fbx"),
            ("rim_talus_05.json", "Rocks/RimTalus_05.fbx"),
            ("rim_talus_06.json", "Rocks/RimTalus_06.fbx"),
            ("rim_talus_07.json", "Rocks/RimTalus_07.fbx"),
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

        private static IEnumerable<TestCaseData> ContractCases()
        {
            for (int i = 0; i < Contracts.Length; i++)
            {
                (string contractFile, string fbxRelativePath) = Contracts[i];
                yield return new TestCaseData(contractFile, fbxRelativePath)
                    .SetName($"Prefab_{Path.GetFileNameWithoutExtension(contractFile)}_MatchesContractBounds");
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void BuiltPrefab_RendererAndMeshBounds_MatchContract(string contractFile, string fbxRelativePath)
        {
            string contractPath = Path.Combine(ContractsDir, contractFile);
            Assert.IsTrue(File.Exists(contractPath), $"Missing contract JSON at '{contractPath}'.");
            ContractSpec contract = JsonUtility.FromJson<ContractSpec>(File.ReadAllText(contractPath));
            Assert.IsNotNull(contract.bounds.size, $"Contract '{contractFile}' is missing bounds.size.");
            Assert.AreEqual(3, contract.bounds.size.Length, $"Contract '{contractFile}' bounds.size must have 3 axes.");
            Assert.Greater(contract.bounds.tolerance, 0f, $"Contract '{contractFile}' bounds.tolerance must be positive.");

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
        public void KestrelAntennaBoom_HasCutPoint_UsesEraMaterials_AndHasValidVertexColors()
        {
            string fbxPath = R1MaterialRecipes.ModelsFolder + "/Wreck/Kestrel_AntennaBoom.fbx";
            string prefabPath = R1ModelBuilder.GetGeneratedPrefabPath(fbxPath);
            GameObject prefab = MeshChecks.LoadGeneratedPrefab(prefabPath);

            Transform cutPoint = prefab.transform.Find("CutPoint");
            Assert.IsNotNull(cutPoint, "Kestrel_AntennaBoom must have a child CutPoint node.");

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, $"Null material on '{prefab.name}/{r.name}'.");
                    usedSlots.Add(m.name);
                }
            }

            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.WhitePaintSlot), "Kestrel_AntennaBoom must use R1_WhitePaint.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.TealMarkSlot), "Kestrel_AntennaBoom must use R1_TealMark.");
            Assert.IsTrue(usedSlots.Contains(R1MaterialRecipes.SolarCellSlot), "Kestrel_AntennaBoom must use R1_SolarCell.");

            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = mf.sharedMesh;
                Assert.IsNotNull(mesh, $"Missing sharedMesh on '{prefab.name}/{mf.name}'.");
                Assert.AreEqual(mesh.vertexCount, mesh.colors.Length, $"Mesh '{mesh.name}' is missing baked vertex colors.");
            }
        }

        [Test]
        public void RockfallKit_FreshBouldersAndTalusSlab_AreBiggerLighterTonedAndGrounded()
        {
            Material weatheredBasalt = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            Material freshBasalt = R1MaterialLibrary.LoadFreshBasaltMaterial();
            Material talusMat = R1MaterialLibrary.LoadRockfallTalusMaterial();
            Assert.IsNotNull(freshBasalt, "Missing R1_FreshBasalt material.");
            Assert.IsNotNull(talusMat, "Missing R1_RockfallTalus material.");

            float weatheredBaseLuma = weatheredBasalt.GetColor("_BaseColor").grayscale;
            float freshBaseLuma = freshBasalt.GetColor("_BaseColor").grayscale;
            float talusBaseLuma = talusMat.GetColor("_BaseColor").grayscale;
            Assert.Greater(freshBaseLuma, weatheredBaseLuma * 1.40f,
                "Fresh rockfall boulders must have a lighter-toned _BaseColor than weathered R1_Basalt.");
            Assert.Greater(talusBaseLuma, weatheredBaseLuma * 1.45f,
                "Rockfall talus slab must have a lighter-toned _BaseColor than weathered R1_Basalt.");
            Assert.Greater(
                freshBasalt.GetColor("_EdgeWearColor").grayscale,
                weatheredBasalt.GetColor("_EdgeWearColor").grayscale + 0.20f,
                "Fresh rockfall cleavage faces (_EdgeWearColor) must be paler than weathered R1_Basalt.");
            Assert.AreEqual(0f, freshBasalt.GetFloat("_EmissionStrength"), 1e-5f,
                "Fresh rockfall boulders must never emit or bloom.");
            Assert.AreEqual(0f, talusMat.GetFloat("_EmissionStrength"), 1e-5f,
                "Rockfall talus slab must never emit or bloom.");

            GameObject rock13Prefab = MeshChecks.LoadGeneratedPrefab(
                R1ModelBuilder.GetGeneratedPrefabPath(R1MaterialRecipes.ModelsFolder + "/Rocks/Rock_13.fbx"));
            Bounds rock13Bounds = MeshChecks.PrefabBounds(rock13Prefab, rock13Prefab.transform.localToWorldMatrix);

            float prevWidth = 0f;
            for (int i = 0; i < R1ModelBuilder.FreshRockfallBoulderFbxPaths.Length; i++)
            {
                string fbxPath = R1ModelBuilder.FreshRockfallBoulderFbxPaths[i];
                GameObject boulderPrefab = MeshChecks.LoadGeneratedPrefab(R1ModelBuilder.GetGeneratedPrefabPath(fbxPath));
                Bounds bounds = MeshChecks.PrefabBounds(boulderPrefab, boulderPrefab.transform.localToWorldMatrix);

                Assert.GreaterOrEqual(bounds.size.x, 10.0f, $"{boulderPrefab.name} width must be >= 10 m.");
                Assert.LessOrEqual(bounds.size.x, 16.5f, $"{boulderPrefab.name} width must be <= 16.5 m.");
                Assert.Greater(bounds.size.x, rock13Bounds.size.x,
                    $"{boulderPrefab.name} must be wider than weathered Rock_13 ({rock13Bounds.size.x:F2} m).");
                Assert.Greater(bounds.size.y, rock13Bounds.size.y,
                    $"{boulderPrefab.name} must be taller than weathered Rock_13 ({rock13Bounds.size.y:F2} m).");
                Assert.Greater(bounds.size.x, prevWidth, "Fresh rockfall boulders must grow monotonically across 00..03.");
                Assert.Less(bounds.min.y, -0.5f, $"{boulderPrefab.name} must have a buried base so it never floats on slopes.");
                prevWidth = bounds.size.x;

                Renderer renderer = boulderPrefab.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(renderer);
                Assert.AreEqual(R1MaterialLibrary.FreshBasaltMaterialName, renderer.sharedMaterial.name);

                Mesh mesh = boulderPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.AreEqual(mesh.vertexCount, mesh.colors.Length);
            }

            GameObject talusPrefab = MeshChecks.LoadGeneratedPrefab(
                R1ModelBuilder.GetGeneratedPrefabPath(R1ModelBuilder.RockfallTalusSlabFbxPath));
            Bounds talusBounds = MeshChecks.PrefabBounds(talusPrefab, talusPrefab.transform.localToWorldMatrix);
            Assert.GreaterOrEqual(talusBounds.size.x, 40.0f, "Rockfall_TalusSlab must be at least 40 m wide.");
            Assert.LessOrEqual(talusBounds.size.x, 50.0f, "Rockfall_TalusSlab must be at most 50 m wide.");
            Assert.Less(talusBounds.min.y, -0.4f, "Rockfall_TalusSlab must have a buried root below ground.");

            Renderer talusRenderer = talusPrefab.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(talusRenderer);
            Assert.AreEqual(R1MaterialLibrary.RockfallTalusMaterialName, talusRenderer.sharedMaterial.name);
        }

        [Test]
        public void CraterRimSlopeMaterial_AndTalusScatterKit_AreTriplanarBandFreeAndGrounded()
        {
            Material rimSlope = R1MaterialLibrary.LoadRimSlopeMaterial();
            Material rimTalus = R1MaterialLibrary.LoadRimTalusMaterial();
            Assert.IsNotNull(rimSlope, "Missing R1_RimSlope material.");
            Assert.IsNotNull(rimTalus, "Missing R1_RimTalus material.");
            Assert.AreEqual("MoonProject/World/R1Terrain", rimSlope.shader.name);
            Assert.AreEqual(R1MaterialRecipes.ShaderName, rimTalus.shader.name);

            Assert.IsNotNull(rimSlope.GetTexture("_BaseMap"), "R1_RimSlope must bind _BaseMap (regolith albedo).");
            Assert.IsNotNull(rimSlope.GetTexture("_BumpMap"), "R1_RimSlope must bind _BumpMap (regolith normal).");
            Assert.IsNotNull(rimSlope.GetTexture("_MaskMap"), "R1_RimSlope must bind _MaskMap (regolith mask).");
            Assert.IsNotNull(rimSlope.GetTexture("_RockMap"), "R1_RimSlope must bind _RockMap (triplanar basalt rock).");
            Assert.IsNotNull(rimSlope.GetTexture("_RockBumpMap"), "R1_RimSlope must bind _RockBumpMap (triplanar basalt normal).");

            Assert.AreEqual(1f, rimTalus.GetFloat("_TriplanarMapping"), 1e-5f,
                "R1_RimTalus must enable world-space triplanar mapping (_TriplanarMapping == 1).");
            Assert.Greater(rimTalus.GetFloat("_SteepRockDarkening"), 0.15f,
                "R1_RimTalus must darken steep faces to match fresh-rock exposures.");
            Assert.Greater(rimTalus.GetFloat("_ColorVarAmount"), 0.10f,
                "R1_RimTalus must have strong 3D macro variation to break tiling.");
            Assert.AreEqual(0f, rimTalus.GetFloat("_EmissionStrength"), 1e-5f,
                "R1_RimTalus must never emit or bloom.");

            Assert.AreEqual(8, R1ModelBuilder.RimTalusFbxPaths.Length, "Rim talus scatter kit must have 8 pieces (RimTalus_00..07).");
            float prevWidth = 0.45f;
            for (int i = 0; i < R1ModelBuilder.RimTalusFbxPaths.Length; i++)
            {
                string fbxPath = R1ModelBuilder.RimTalusFbxPaths[i];
                GameObject talusPiece = MeshChecks.LoadGeneratedPrefab(R1ModelBuilder.GetGeneratedPrefabPath(fbxPath));
                Bounds bounds = MeshChecks.PrefabBounds(talusPiece, talusPiece.transform.localToWorldMatrix);

                Assert.GreaterOrEqual(bounds.size.x, 0.50f, $"{talusPiece.name} width must be >= 0.50 m.");
                Assert.LessOrEqual(bounds.size.x, 4.00f, $"{talusPiece.name} width must be <= 4.00 m.");
                Assert.Greater(bounds.size.x, prevWidth, $"RimTalus pieces must grow monotonically in width ({talusPiece.name}).");
                Assert.Less(bounds.min.y, -0.05f, $"{talusPiece.name} must have a buried base so it never floats on slopes.");
                prevWidth = bounds.size.x;

                Renderer renderer = talusPiece.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(renderer, $"Missing Renderer on {talusPiece.name}.");
                Assert.AreEqual(R1MaterialLibrary.RimTalusMaterialName, renderer.sharedMaterial.name);

                Mesh mesh = talusPiece.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh, $"Missing sharedMesh on {talusPiece.name}.");
                Assert.IsTrue(mesh.isReadable, $"{talusPiece.name} mesh must be CPU-readable for World scatter.");
                Assert.AreEqual(mesh.vertexCount, mesh.colors.Length, $"{talusPiece.name} must have baked R1 vertex colors.");
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
