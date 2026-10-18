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
            ("rover.json", "Rover/RoverModel.fbx"),
            ("scan_fan.json", "Rover/ScanFan.fbx"),
            ("scatter_set.json", "Ruins/ScatterSet.fbx"),
            ("solar_array.json", "Structures/SolarArray.fbx"),
            ("solar_wing.json", "Rover/SolarWing.fbx"),
            ("tape_deck.json", "Rover/Kit_TapeDeck.fbx"),
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
