using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the four 0.6 machine fauna species (<c>PanelMite</c>, <c>Sweeper</c>, <c>SurveyDrone</c>,
    /// <c>Trencher</c>): generated prefabs, child motion pivots, <see cref="FaunaMotionTuning"/> and
    /// <see cref="FaunaIdleMotion"/> procedural loops, single-lamp <c>_EmissionStrength</c> modulation contract,
    /// relative scale vs Rover 07, and zero per-frame GC allocation.
    /// </summary>
    public sealed class R1FaunaTests
    {
        private const string RoverPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";

        [OneTimeSetUp]
        public void EnsureBuilt()
        {
            R1MaterialLibrary.BuildMaterials();
            R1ModelBuilder.Build();
            R1FaunaBuilder.Build();
        }

        [Test]
        public void FaunaMotionTuning_AssetExists_AndRespectsNoStrobingLampContract()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<FaunaMotionTuning>(R1FaunaBuilder.TuningAssetPath);
            Assert.IsNotNull(tuning, $"Missing '{R1FaunaBuilder.TuningAssetPath}'.");
            Assert.Greater(tuning.LampBaseEmissionStrength, 0.1f);
            Assert.Less(tuning.LampBreathFrequencyHz, 3.0f,
                "Lamp breathing frequency must stay strictly below 3 Hz (no strobing or fast flicker).");
        }

        [Test]
        public void PanelMite_PrefabHasRequiredPivots_WarmLumenLamp_AndPalmSizedScaleVsRover07()
        {
            GameObject mitePrefab = MeshChecks.LoadGeneratedPrefab(R1FaunaBuilder.PanelMitePrefabPath);
            GameObject roverPrefab = MeshChecks.LoadGeneratedPrefab(RoverPrefabPath);

            var motion = mitePrefab.GetComponent<FaunaIdleMotion>();
            Assert.IsNotNull(motion, "PanelMite.prefab root must have a FaunaIdleMotion component.");
            Assert.AreEqual(FaunaSpecies.PanelMite, motion.Species);
            Assert.DoesNotThrow(() => motion.ValidateOrThrow());

            Assert.IsNotNull(mitePrefab.transform.Find("Body"), "PanelMite missing Body.");
            Assert.IsNotNull(mitePrefab.transform.Find("Lamp"), "PanelMite missing Lamp.");
            Assert.IsNotNull(mitePrefab.transform.Find("Wiper"), "PanelMite missing Wiper.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_L0"), "PanelMite missing Leg_L0.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_L1"), "PanelMite missing Leg_L1.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_L2"), "PanelMite missing Leg_L2.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_R0"), "PanelMite missing Leg_R0.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_R1"), "PanelMite missing Leg_R1.");
            Assert.IsNotNull(mitePrefab.transform.Find("Leg_R2"), "PanelMite missing Leg_R2.");

            AssertSingleLampAndValidR1Materials(mitePrefab, R1MaterialRecipes.LampAmberSlot);

            Bounds miteBounds = MeshChecks.PrefabBounds(mitePrefab, mitePrefab.transform.localToWorldMatrix);
            Bounds roverBounds = MeshChecks.PrefabBounds(roverPrefab, roverPrefab.transform.localToWorldMatrix);
            Assert.Less(miteBounds.size.z, roverBounds.size.z * 0.25f,
                "PanelMite must be palm-sized (< 25% of Rover 07's length).");
        }

        [Test]
        public void Sweeper_PrefabHasRequiredPivots_WarmLumenLamp_AndOnePointTwoTimesRover07Length()
        {
            GameObject sweeperPrefab = MeshChecks.LoadGeneratedPrefab(R1FaunaBuilder.SweeperPrefabPath);
            GameObject roverPrefab = MeshChecks.LoadGeneratedPrefab(RoverPrefabPath);

            var motion = sweeperPrefab.GetComponent<FaunaIdleMotion>();
            Assert.IsNotNull(motion, "Sweeper.prefab root must have a FaunaIdleMotion component.");
            Assert.AreEqual(FaunaSpecies.Sweeper, motion.Species);
            Assert.DoesNotThrow(() => motion.ValidateOrThrow());

            Assert.IsNotNull(sweeperPrefab.transform.Find("Body"), "Sweeper missing Body.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("Lamp"), "Sweeper missing Lamp.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("BrushArm"), "Sweeper missing BrushArm.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("BrushArm/Brush"), "Sweeper missing BrushArm/Brush.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("Wheel_FL"), "Sweeper missing Wheel_FL.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("Wheel_FR"), "Sweeper missing Wheel_FR.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("Wheel_RL"), "Sweeper missing Wheel_RL.");
            Assert.IsNotNull(sweeperPrefab.transform.Find("Wheel_RR"), "Sweeper missing Wheel_RR.");

            AssertSingleLampAndValidR1Materials(sweeperPrefab, R1MaterialRecipes.LampAmberSlot);

            Bounds sweeperBounds = MeshChecks.PrefabBounds(sweeperPrefab, sweeperPrefab.transform.localToWorldMatrix);
            Bounds roverBounds = MeshChecks.PrefabBounds(roverPrefab, roverPrefab.transform.localToWorldMatrix);
            float lengthRatio = sweeperBounds.size.z / roverBounds.size.z;
            Assert.AreEqual(1.20f, lengthRatio, 0.10f,
                $"Sweeper length ({sweeperBounds.size.z:F2} m) must be ~1.2x Rover 07's length ({roverBounds.size.z:F2} m).");
            Assert.Greater(sweeperBounds.size.x, roverBounds.size.x,
                "Sweeper wide brush head must exceed Rover 07's width.");
        }

        [Test]
        public void SurveyDrone_PrefabHasRequiredPivots_CyanTychoLamp_AndHexColdGasAnatomy()
        {
            GameObject dronePrefab = MeshChecks.LoadGeneratedPrefab(R1FaunaBuilder.SurveyDronePrefabPath);

            var motion = dronePrefab.GetComponent<FaunaIdleMotion>();
            Assert.IsNotNull(motion, "SurveyDrone.prefab root must have a FaunaIdleMotion component.");
            Assert.AreEqual(FaunaSpecies.SurveyDrone, motion.Species);
            Assert.DoesNotThrow(() => motion.ValidateOrThrow());

            Assert.IsNotNull(dronePrefab.transform.Find("Body"), "SurveyDrone missing Body.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Lamp"), "SurveyDrone missing Body/Lamp.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Scanner"), "SurveyDrone missing Body/Scanner.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Nozzle_FL"), "SurveyDrone missing Body/Nozzle_FL.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Nozzle_FR"), "SurveyDrone missing Body/Nozzle_FR.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Nozzle_RL"), "SurveyDrone missing Body/Nozzle_RL.");
            Assert.IsNotNull(dronePrefab.transform.Find("Body/Nozzle_RR"), "SurveyDrone missing Body/Nozzle_RR.");

            AssertSingleLampAndValidR1Materials(dronePrefab, R1FaunaBuilder.FaunaLampCyanMaterialName);
        }

        [Test]
        public void Trencher_PrefabHasRequiredPivots_WarmLumenLamp_AndLargerScaleThanRover07()
        {
            GameObject trencherPrefab = MeshChecks.LoadGeneratedPrefab(R1FaunaBuilder.TrencherPrefabPath);
            GameObject roverPrefab = MeshChecks.LoadGeneratedPrefab(RoverPrefabPath);

            var motion = trencherPrefab.GetComponent<FaunaIdleMotion>();
            Assert.IsNotNull(motion, "Trencher.prefab root must have a FaunaIdleMotion component.");
            Assert.AreEqual(FaunaSpecies.Trencher, motion.Species);
            Assert.DoesNotThrow(() => motion.ValidateOrThrow());

            Assert.IsNotNull(trencherPrefab.transform.Find("Body"), "Trencher missing Body.");
            Assert.IsNotNull(trencherPrefab.transform.Find("Lamp"), "Trencher missing Lamp.");
            Assert.IsNotNull(trencherPrefab.transform.Find("TrenchBoom"), "Trencher missing TrenchBoom.");
            Assert.IsNotNull(trencherPrefab.transform.Find("TrenchBoom/TrenchWheel"), "Trencher missing TrenchBoom/TrenchWheel.");
            Assert.IsNotNull(trencherPrefab.transform.Find("CableSpool"), "Trencher missing CableSpool.");
            Assert.IsNotNull(trencherPrefab.transform.Find("Roller_FL"), "Trencher missing Roller_FL.");
            Assert.IsNotNull(trencherPrefab.transform.Find("Roller_FR"), "Trencher missing Roller_FR.");
            Assert.IsNotNull(trencherPrefab.transform.Find("Roller_RL"), "Trencher missing Roller_RL.");
            Assert.IsNotNull(trencherPrefab.transform.Find("Roller_RR"), "Trencher missing Roller_RR.");

            AssertSingleLampAndValidR1Materials(trencherPrefab, R1MaterialRecipes.LampAmberSlot);

            Bounds trencherBounds = MeshChecks.PrefabBounds(trencherPrefab, trencherPrefab.transform.localToWorldMatrix);
            Bounds roverBounds = MeshChecks.PrefabBounds(roverPrefab, roverPrefab.transform.localToWorldMatrix);
            Assert.Greater(trencherBounds.size.x, roverBounds.size.x, "Trencher must be wider than Rover 07.");
            Assert.Greater(trencherBounds.size.z, roverBounds.size.z * 1.4f, "Trencher must be longer than Rover 07.");
            Assert.Greater(trencherBounds.size.y, roverBounds.size.y, "Trencher must be taller than Rover 07.");
        }

        [TestCase(R1FaunaBuilder.PanelMitePrefabPath)]
        [TestCase(R1FaunaBuilder.SweeperPrefabPath)]
        [TestCase(R1FaunaBuilder.SurveyDronePrefabPath)]
        [TestCase(R1FaunaBuilder.TrencherPrefabPath)]
        public void FaunaIdleMotion_StepAnimatesPivots_ModulatesEmissionStrengthOnly_AndAllocatesZeroBytes(string prefabPath)
        {
            GameObject prefab = MeshChecks.LoadGeneratedPrefab(prefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var motion = instance.GetComponent<FaunaIdleMotion>();
                Assert.IsNotNull(motion);
                motion.ValidateOrThrow();
                motion.CacheRestPose();

                Color authoredEmissionColor = motion.LampRenderer.sharedMaterial.GetColor("_EmissionColor");

                motion.Speed = 1.2f;
                motion.Step(0.1f);

                // Warm up before measuring managed allocations
                for (int i = 0; i < 4; i++)
                {
                    motion.Step(0.016f);
                }

                long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 60; i++)
                {
                    motion.Step(0.016f);
                }

                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
                Assert.AreEqual(0L, allocatedBytes,
                    $"FaunaIdleMotion.Step on '{prefab.name}' must allocate 0 bytes per frame (allocated {allocatedBytes} B).");

                Assert.Greater(motion.TravelDistance, 0.5f, "TravelDistance must advance when Speed > 0.");
                Assert.Greater(motion.CurrentLampEmissionStrength, 0.1f);

                // Verify MaterialPropertyBlock sets _EmissionStrength and never overwrites _EmissionColor
                var block = new MaterialPropertyBlock();
                motion.LampRenderer.GetPropertyBlock(block);
                Assert.AreEqual(motion.CurrentLampEmissionStrength, block.GetFloat("_EmissionStrength"), 1e-4f);
                Assert.AreEqual(Color.clear, block.GetColor("_EmissionColor"),
                    "FaunaIdleMotion must never overwrite _EmissionColor in the MaterialPropertyBlock.");
                Assert.AreEqual(authoredEmissionColor, motion.LampRenderer.sharedMaterial.GetColor("_EmissionColor"),
                    "Shared material _EmissionColor must remain untouched.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void FaunaIdleMotion_ValidateOrThrow_ThrowsInvalidOperationException_WhenMissingRequiredReference()
        {
            var go = new GameObject("BrokenFaunaTestObject");
            try
            {
                var motion = go.AddComponent<FaunaIdleMotion>();
                var ex = Assert.Throws<InvalidOperationException>(() => motion.ValidateOrThrow());
                StringAssert.Contains("BrokenFaunaTestObject", ex.Message);
                StringAssert.Contains("_tuning", ex.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void AssertSingleLampAndValidR1Materials(GameObject prefab, string expectedLampMaterialName)
        {
            Transform lampTransform = prefab.transform.Find("Lamp") ?? prefab.transform.Find("Body/Lamp");
            Assert.IsNotNull(lampTransform, $"'{prefab.name}' must have a child named 'Lamp'.");
            Renderer lampRenderer = lampTransform.GetComponent<Renderer>();
            Assert.IsNotNull(lampRenderer, $"'{prefab.name}/Lamp' must have a Renderer.");
            Assert.AreEqual(expectedLampMaterialName, lampRenderer.sharedMaterial.name,
                $"'{prefab.name}/Lamp' must use '{expectedLampMaterialName}'.");

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                var filter = renderer.GetComponent<MeshFilter>();
                Assert.IsNotNull(filter, $"Renderer '{renderer.name}' on '{prefab.name}' is missing MeshFilter.");
                Assert.IsNotNull(filter.sharedMesh, $"MeshFilter '{renderer.name}' on '{prefab.name}' has null mesh.");
                Assert.AreEqual(filter.sharedMesh.vertexCount, filter.sharedMesh.colors32.Length,
                    $"Mesh '{filter.sharedMesh.name}' on '{prefab.name}' must have baked vertex colours.");

                Material[] mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    Assert.IsNotNull(mat, $"Renderer '{renderer.name}' on '{prefab.name}' has null material.");
                    Assert.AreEqual(R1MaterialRecipes.ShaderName, mat.shader.name,
                        $"Material '{mat.name}' on '{prefab.name}' must use '{R1MaterialRecipes.ShaderName}' (never CrtFace).");

                    if (renderer != lampRenderer)
                    {
                        Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, mat.name,
                            $"Only the 'Lamp' child on '{prefab.name}' may use an emissive lamp material (found on '{renderer.name}').");
                        Assert.AreNotEqual(R1FaunaBuilder.FaunaLampCyanMaterialName, mat.name,
                            $"Only the 'Lamp' child on '{prefab.name}' may use an emissive lamp material (found on '{renderer.name}').");
                    }
                }
            }
        }
    }
}
