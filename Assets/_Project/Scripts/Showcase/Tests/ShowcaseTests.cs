using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Rover;
using MoonProject.Showcase.Editor;
using MoonProject.World;

namespace MoonProject.Showcase.Tests
{
    public sealed class ShowcaseTests
    {
        [Test]
        public void Easing_EndpointsAndMonotonicity_NeverSnaps()
        {
            Assert.AreEqual(0f, ShowcaseEasing.SmoothStep01(0f), 1e-6f);
            Assert.AreEqual(0.5f, ShowcaseEasing.SmoothStep01(0.5f), 1e-6f);
            Assert.AreEqual(1f, ShowcaseEasing.SmoothStep01(1f), 1e-6f);

            Assert.AreEqual(0f, ShowcaseEasing.SmootherStep01(0f), 1e-6f);
            Assert.AreEqual(0.5f, ShowcaseEasing.SmootherStep01(0.5f), 1e-6f);
            Assert.AreEqual(1f, ShowcaseEasing.SmootherStep01(1f), 1e-6f);

            float p = 0f;
            p = ShowcaseEasing.AdvanceNormalized(p, 1f, durationSeconds: 1.0f, deltaTime: 0.1f);
            Assert.That(p, Is.GreaterThan(0f).And.LessThan(0.25f), "Single 0.1s frame must not snap to 1.");

            float angle = ShowcaseEasing.SmoothAngle(170f, -170f, easeSeconds: 0.5f, deltaTime: 0.05f);
            float delta = Mathf.DeltaAngle(170f, angle);
            Assert.That(delta, Is.GreaterThan(0f).And.LessThan(20f), "Shortest-path wrap across 180 seam.");

            // Rise-and-settle curve: starts at -depth, crests at +overshoot, and settles at 0
            Assert.AreEqual(-0.42f, ShowcaseEasing.EvaluateRiseAndSettleVerticalOffset(0f, 0.42f, 0.035f), 1e-5f);
            Assert.AreEqual(0.035f, ShowcaseEasing.EvaluateRiseAndSettleVerticalOffset(0.72f, 0.42f, 0.035f), 1e-5f);
            Assert.AreEqual(0f, ShowcaseEasing.EvaluateRiseAndSettleVerticalOffset(1f, 0.42f, 0.035f), 1e-5f);
        }

        [Test]
        public void NodeSequencer_RotationAndPositionRules_EaseSmoothlyAndClamp()
        {
            ShowcaseNodeRule[] rules =
            {
                new ShowcaseNodeRule(
                    "test.ramp",
                    "Ramp",
                    "RampDoor",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 108f,
                    minLimit: 0f,
                    maxLimit: 110f,
                    easeDuration: 1.0f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "test.contacts",
                    "Contacts",
                    "ContactShoes",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.08f,
                    minLimit: 0f,
                    maxLimit: 0.10f,
                    easeDuration: 0.8f,
                    ShowcaseTriggerKey.Action2_X),
            };

            var sequencer = new ShowcaseNodeSequencer(rules);
            ShowcaseNodeChannel ramp = sequencer.FindChannel("test.ramp");
            ShowcaseNodeChannel contacts = sequencer.FindChannel("test.contacts");

            Assert.IsFalse(ramp.IsActive);
            Assert.AreEqual(0f, ramp.CurrentValue, 1e-5f);

            int toggled = sequencer.ToggleKey(ShowcaseTriggerKey.Action1_Z);
            Assert.AreEqual(1, toggled);
            Assert.IsTrue(ramp.IsActive);

            // Step one 50 ms frame: must move partway, never snapping straight to 108
            sequencer.Step(0.05f);
            Assert.That(ramp.CurrentValue, Is.GreaterThan(0f).And.LessThan(20f));

            // Step past full duration: reaches target 108
            for (int i = 0; i < 25; i++)
            {
                sequencer.Step(0.05f);
            }

            Assert.AreEqual(108f, ramp.CurrentValue, 1e-3f);

            // External target clamp check
            contacts.SetExternalTarget(0.50f);
            for (int i = 0; i < 30; i++)
            {
                sequencer.Step(0.05f);
            }

            Assert.AreEqual(0.10f, contacts.CurrentValue, 1e-3f, "Clamped to MaxLimit 0.10m.");
        }

        [Test]
        public void NodeSequencer_ContinuousSpin_RampsVelocityUpAndDownWithoutSnappingAngle()
        {
            ShowcaseNodeRule spinRule = new ShowcaseNodeRule(
                "test.wheel",
                "Wheel",
                "Wheel_FL",
                ShowcaseMotionKind.ContinuousSpin,
                Vector3.right,
                restValue: 0f,
                activeValue: 180f,
                minLimit: 0f,
                maxLimit: 360f,
                easeDuration: 0.5f,
                ShowcaseTriggerKey.Action4_V);

            var sequencer = new ShowcaseNodeSequencer(new[] { spinRule });
            ShowcaseNodeChannel wheel = sequencer.FindChannel("test.wheel");

            sequencer.ToggleKey(ShowcaseTriggerKey.Action4_V);
            sequencer.Step(0.1f);
            Assert.That(wheel.SpinVelocityFactor01, Is.GreaterThan(0f).And.LessThan(0.5f));
            Assert.That(wheel.CurrentValue, Is.GreaterThan(0f));

            for (int i = 0; i < 10; i++)
            {
                sequencer.Step(0.1f);
            }

            Assert.AreEqual(1f, wheel.SpinVelocityFactor01, 1e-4f);
            float angleWhenStoppedTrigger = wheel.CurrentValue;

            // Toggle off: spin decelerates smoothly and stops at final accumulated angle without snapping to 0
            sequencer.ToggleKey(ShowcaseTriggerKey.Action4_V);
            for (int i = 0; i < 10; i++)
            {
                sequencer.Step(0.1f);
            }

            Assert.AreEqual(0f, wheel.SpinVelocityFactor01, 1e-4f);
            Assert.That(wheel.CurrentValue, Is.Not.EqualTo(0f).And.Not.EqualTo(angleWhenStoppedTrigger));
        }

        [Test]
        public void SolarTracker_FoldsAtNightAndEclipse_AndTracksSunInDaylight()
        {
            Assert.IsTrue(ShowcaseSolarTracker.ShouldFoldForSky(SkyPhase.Earthlit, false, 0f, 0f));
            Assert.IsTrue(ShowcaseSolarTracker.ShouldFoldForSky(SkyPhase.Eclipse, true, 1f, 0f));
            Assert.IsTrue(ShowcaseSolarTracker.ShouldFoldForSky(SkyPhase.Eclipse, false, 0.6f, 0.2f));
            Assert.IsFalse(ShowcaseSolarTracker.ShouldFoldForSky(SkyPhase.Sunlit, false, 0f, 0.9f));
            Assert.IsFalse(ShowcaseSolarTracker.ShouldFoldForSky(SkyPhase.Sunrise, false, 0f, 0.5f));

            ShowcaseSolarTracker.ComputeSolarArrayTargets(
                new Vector3(0.5f, 0.8f, -0.3f).normalized,
                shouldFold: true,
                out float foldedAzim,
                out float foldedElevOffset,
                out float foldedWing);
            Assert.AreEqual(0f, foldedAzim, 1e-4f);
            Assert.AreEqual(
                ShowcaseSolarTracker.StowedElevationDegrees - ShowcaseSolarTracker.RestElevationDegrees,
                foldedElevOffset,
                1e-4f);
            Assert.AreEqual(ShowcaseSolarTracker.FoldedWingDegrees, foldedWing, 1e-4f);

            ShowcaseSolarTracker.ComputeSolarArrayTargets(
                new Vector3(0.4f, 0.85f, -0.3f).normalized,
                shouldFold: false,
                out float activeAzim,
                out float activeElevOffset,
                out float activeWing);
            Assert.AreEqual(0f, activeWing, 1e-4f);
            Assert.That(activeAzim, Is.Not.EqualTo(0f));
            float absoluteElev = ShowcaseSolarTracker.RestElevationDegrees + activeElevOffset;
            Assert.That(
                absoluteElev,
                Is.GreaterThanOrEqualTo(ShowcaseSolarTracker.MinElevationDegrees)
                  .And.LessThanOrEqualTo(ShowcaseSolarTracker.MaxElevationDegrees));
        }

        [Test]
        public void PhaseStepper_AllSixPresets_MatchRealSunClockPhases()
        {
            var tuning = ScriptableObject.CreateInstance<ShowcaseTuning>();
            try
            {
                ShowcaseTuningBuilder.PopulateDefaultTuning(tuning);
                var stepper = new ShowcasePhaseStepper(tuning.SkyPresets, initialPresetIndex: 0);

                var worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/_Project/Data/Tuning/WorldSettings.asset");
                Assert.IsNotNull(worldSettings, "WorldSettings.asset must exist.");
                var clock = new SunClock(worldSettings.SkyCycle, worldSettings.Sky);

                // Preset 0: EclipseHold
                ShowcaseSkyPreset p0 = stepper.CurrentPreset;
                Assert.AreEqual(ShowcaseSkyPresetId.EclipseHold, p0.PresetId);
                clock.SetTime(p0.TimeSeconds, p0.HoldEclipse);
                Assert.IsTrue(clock.IsHoldingEclipse);
                Assert.AreEqual(SkyPhase.Eclipse, clock.Phase);
                Assert.AreEqual(1f, clock.EclipseFactor, 1e-3f);

                // Preset 1: DawnBead
                ShowcaseSkyPreset p1 = stepper.NextPreset();
                Assert.AreEqual(ShowcaseSkyPresetId.DawnBead, p1.PresetId);
                clock.SetTime(p1.TimeSeconds, p1.HoldEclipse);
                Assert.IsFalse(clock.IsHoldingEclipse);
                Assert.AreEqual(SkyPhase.Eclipse, clock.Phase);
                Assert.That(clock.EclipseFactor, Is.GreaterThan(0.05f).And.LessThan(0.99f));

                // Preset 2: Sunrise
                ShowcaseSkyPreset p2 = stepper.NextPreset();
                Assert.AreEqual(ShowcaseSkyPresetId.Sunrise, p2.PresetId);
                clock.SetTime(p2.TimeSeconds, p2.HoldEclipse);
                Assert.AreEqual(SkyPhase.Sunrise, clock.Phase);

                // Preset 3: FullSunlit
                ShowcaseSkyPreset p3 = stepper.NextPreset();
                Assert.AreEqual(ShowcaseSkyPresetId.FullSunlit, p3.PresetId);
                clock.SetTime(p3.TimeSeconds, p3.HoldEclipse);
                Assert.AreEqual(SkyPhase.Sunlit, clock.Phase);

                // Preset 4: Sunset
                ShowcaseSkyPreset p4 = stepper.NextPreset();
                Assert.AreEqual(ShowcaseSkyPresetId.Sunset, p4.PresetId);
                clock.SetTime(p4.TimeSeconds, p4.HoldEclipse);
                Assert.AreEqual(SkyPhase.Sunset, clock.Phase);

                // Preset 5: EarthlitNight
                ShowcaseSkyPreset p5 = stepper.NextPreset();
                Assert.AreEqual(ShowcaseSkyPresetId.EarthlitNight, p5.PresetId);
                clock.SetTime(p5.TimeSeconds, p5.HoldEclipse);
                Assert.AreEqual(SkyPhase.Earthlit, clock.Phase);

                // Wrap back to Preset 0
                Assert.AreEqual(ShowcaseSkyPresetId.EclipseHold, stepper.NextPreset().PresetId);

                // Scrub wrap around 7200s
                float wrappedBack = stepper.ComputeScrubbedTime(10f, -1f, 0.1f);
                Assert.That(wrappedBack, Is.GreaterThan(7100f).And.LessThan(7200f));

                // Fast time toggle
                Assert.IsFalse(stepper.IsFastTime);
                Assert.AreEqual(1f, stepper.TimeScale, 1e-4f);
                Assert.IsTrue(stepper.ToggleFastTime());
                Assert.AreEqual(60f, stepper.TimeScale, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void InMemorySaveService_WorksWithSunClock_WithoutDiskWrites()
        {
            var save = new ShowcaseInMemorySaveService();
            Assert.IsTrue(save.IsLoaded);
            Assert.AreEqual(SaveLoadResult.NoSave, save.LoadResult);
            Assert.AreEqual(string.Empty, save.FilePath);

            var worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/_Project/Data/Tuning/WorldSettings.asset");
            var clock = new SunClock(worldSettings.SkyCycle, worldSettings.Sky);
            using (save.Register(clock.CreateSaveSection()))
            {
                Assert.AreEqual(1, save.RegisteredSectionCount);
                clock.ResolveLoadedSave(save);
                Assert.IsTrue(clock.IsHoldingEclipse, "NoSave keeps the opening eclipse hold until set or released.");
            }

            Assert.AreEqual(0, save.RegisteredSectionCount);
        }

        [Test]
        public void ShowcaseTuning_AllExhibitNodePaths_ResolveInR1Prefabs()
        {
            var tuning = ScriptableObject.CreateInstance<ShowcaseTuning>();
            try
            {
                ShowcaseTuningBuilder.PopulateDefaultTuning(tuning);
                Assert.IsNull(tuning.Validate(), "Default ShowcaseTuning must pass validation.");
                Assert.AreEqual(15, tuning.Exhibits.Length);
                Assert.AreEqual(5, tuning.RuinPieces.Length);

                // Check minimum 12m spacing between all 15 exhibit pairs
                for (int i = 0; i < tuning.Exhibits.Length; i++)
                {
                    for (int j = i + 1; j < tuning.Exhibits.Length; j++)
                    {
                        float dist = Vector3.Distance(
                            tuning.Exhibits[i].PositionOffset,
                            tuning.Exhibits[j].PositionOffset);
                        Assert.That(dist, Is.GreaterThanOrEqualTo(11.9f),
                            $"Exhibits {tuning.Exhibits[i].ExhibitId} and {tuning.Exhibits[j].ExhibitId} must be >= 12m apart.");
                    }
                }

                // Original 6 + Rover07Upgraded + 7 new R1 structures
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.Rover07, ShowcasePaths.RoverPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.Rover07Upgraded, ShowcasePaths.RoverPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.CargoLander, ShowcasePaths.CargoLanderPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.ChargingPad, ShowcasePaths.ChargingPadPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.SolarArray, ShowcasePaths.SolarArrayPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.PowerPylon, ShowcasePaths.PowerPylonPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.BatteryBank, ShowcasePaths.BatteryBankPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.Smelter, ShowcasePaths.SmelterPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.Fabricator, ShowcasePaths.FabricatorPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.StorageRack, ShowcasePaths.StorageRackPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.UpgradeBay, ShowcasePaths.UpgradeBayPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.AntennaMast, ShowcasePaths.AntennaMastPrefab, expectFourStages: true);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.VehiclePad, ShowcasePaths.VehiclePadPrefab, expectFourStages: true);

                // Ruins Corner prefabs and AirlockPanel
                string[] ruinPrefabs =
                {
                    ShowcasePaths.HabitatModulesPrefab,
                    ShowcasePaths.RadioTowerPrefab,
                    ShowcasePaths.KenjiWorkshopPrefab,
                    ShowcasePaths.FallenSolarArrayPrefab,
                    ShowcasePaths.ScatterSetPrefab,
                };
                foreach (string ruinPath in ruinPrefabs)
                {
                    var ruinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruinPath);
                    Assert.IsNotNull(ruinPrefab, $"Missing ruin prefab at '{ruinPath}'.");
                }

                var habitatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePaths.HabitatModulesPrefab);
                Assert.IsNotNull(habitatPrefab.transform.Find("AirlockPanel"), "HabitatModules must contain AirlockPanel.");

                // Rover07Upgraded socket paths and kit prefabs
                var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePaths.RoverPrefab);
                string[] socketPaths =
                {
                    RoverModelNodes.HeadlampSocket,
                    RoverModelNodes.DrumSocketLeft,
                    RoverModelNodes.DrumSocketRight,
                    RoverModelNodes.BinSocketLeft,
                    RoverModelNodes.BinSocketRight,
                    RoverModelNodes.CargoSocket,
                    RoverModelNodes.SpineSocket,
                    RoverModelNodes.CoilSocket,
                    RoverModelNodes.DeckSocket,
                    RoverModelNodes.RingSocket,
                    RoverModelNodes.LensSocket,
                    RoverModelNodes.Eye,
                    RoverModelNodes.ScanFanSocket,
                };
                foreach (string socketPath in socketPaths)
                {
                    Assert.IsNotNull(
                        FindNodeByPathOrName(roverPrefab.transform, socketPath),
                        $"RoverModel missing socket '{socketPath}'.");
                }

                string[] kitPrefabs =
                {
                    ShowcasePaths.KitLampBarPrefab,
                    ShowcasePaths.KitCapacitorDrumPrefab,
                    ShowcasePaths.KitSideBinPrefab,
                    ShowcasePaths.KitCargoRackPrefab,
                    ShowcasePaths.KitBatterySpinePrefab,
                    ShowcasePaths.HoverCoilsPrefab,
                    ShowcasePaths.KitTapeDeckPrefab,
                    ShowcasePaths.KitTetherRingPrefab,
                    ShowcasePaths.FocusLensPrefab,
                    ShowcasePaths.LensGlassPrefab,
                    ShowcasePaths.ScanFanPrefab,
                };
                foreach (string kitPath in kitPrefabs)
                {
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(kitPath), $"Missing kit prefab at '{kitPath}'.");
                }
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void ExhibitRig_BuildStageSequence_RisesAndSettlesOneByOne()
        {
            var tuning = ScriptableObject.CreateInstance<ShowcaseTuning>();
            var rootGo = new GameObject("TestBatteryBank");
            try
            {
                ShowcaseTuningBuilder.PopulateDefaultTuning(tuning);
                var rig = rootGo.AddComponent<ShowcaseExhibitRig>();

                var stageRoots = new Transform[4];
                var restPositions = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    var stageGo = new GameObject($"Stage_{i + 1}");
                    stageGo.transform.SetParent(rootGo.transform, false);
                    stageRoots[i] = stageGo.transform;
                    restPositions[i] = Vector3.zero;
                }

                var so = new SerializedObject(rig);
                so.FindProperty("_exhibitId").enumValueIndex = (int)ShowcaseExhibitId.BatteryBank;
                so.FindProperty("_tuning").objectReferenceValue = tuning;
                SerializedProperty stagesProp = so.FindProperty("_stageRoots");
                stagesProp.arraySize = 4;
                SerializedProperty restProp = so.FindProperty("_stageRestLocalPositions");
                restProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = stageRoots[i];
                    restProp.GetArrayElementAtIndex(i).vector3Value = restPositions[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                rig.Initialize(tuning);
                Assert.AreEqual(4, rig.VisibleStageCount);

                // Trigger 1-by-1 build stage sequence
                rig.TriggerStageSequence();
                Assert.IsTrue(rig.IsStageSequenceRunning);
                Assert.AreEqual(1, rig.VisibleStageCount);
                Assert.IsTrue(stageRoots[0].gameObject.activeSelf);
                Assert.IsFalse(stageRoots[1].gameObject.activeSelf);
                Assert.Less(stageRoots[0].localPosition.y, -0.30f, "Stage 1 starts below ground.");

                // Step past Stage 2 start (0.60s)
                rig.StepFrame(0.60f, Vector3.zero, Vector3.up, SkyPhase.Sunlit, false, 0f, 1f);
                Assert.AreEqual(2, rig.VisibleStageCount);
                Assert.IsTrue(stageRoots[1].gameObject.activeSelf);
                Assert.IsFalse(stageRoots[2].gameObject.activeSelf);

                // Step past all 4 stages completing (3.0s)
                for (int i = 0; i < 30; i++)
                {
                    rig.StepFrame(0.10f, Vector3.zero, Vector3.up, SkyPhase.Sunlit, false, 0f, 1f);
                }

                Assert.IsFalse(rig.IsStageSequenceRunning);
                Assert.AreEqual(4, rig.VisibleStageCount);
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsTrue(stageRoots[i].gameObject.activeSelf);
                    Assert.AreEqual(0f, stageRoots[i].localPosition.y, 1e-4f, $"Stage_{i + 1} settles back to 0.");
                }
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
                Object.DestroyImmediate(tuning);
            }
        }

        private static void VerifyExhibitNodePaths(
            ShowcaseTuning tuning,
            ShowcaseExhibitId exhibitId,
            string prefabPath,
            bool expectFourStages = false)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(exhibitId);
            Assert.IsNotNull(def, $"Missing exhibit definition for {exhibitId}.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at '{prefabPath}'.");

            if (expectFourStages)
            {
                for (int s = 1; s <= 4; s++)
                {
                    Assert.IsNotNull(
                        prefab.transform.Find($"Stage_{s}"),
                        $"Exhibit {exhibitId} prefab '{prefabPath}' missing Stage_{s}.");
                }
            }

            foreach (ShowcaseNodeRule rule in def.Nodes)
            {
                Transform target = FindNodeByPathOrName(prefab.transform, rule.NodePath);
                Assert.IsNotNull(
                    target,
                    $"Exhibit {exhibitId} rule '{rule.RuleId}' path '{rule.NodePath}' not found in '{prefabPath}'.");
            }
        }

        private static Transform FindNodeByPathOrName(Transform root, string pathOrName)
        {
            Transform found = root.Find(pathOrName);
            if (found != null)
            {
                return found;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == pathOrName)
                {
                    return descendants[i];
                }
            }

            return null;
        }
    }
}
