using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
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
                Assert.AreEqual(6, tuning.Exhibits.Length);

                // Check minimum 12m spacing between all exhibit pairs
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

                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.Rover07, ShowcasePaths.RoverPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.CargoLander, ShowcasePaths.CargoLanderPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.ChargingPad, ShowcasePaths.ChargingPadPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.SolarArray, ShowcasePaths.SolarArrayPrefab);
                VerifyExhibitNodePaths(tuning, ShowcaseExhibitId.PowerPylon, ShowcasePaths.PowerPylonPrefab);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        private static void VerifyExhibitNodePaths(
            ShowcaseTuning tuning,
            ShowcaseExhibitId exhibitId,
            string prefabPath)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(exhibitId);
            Assert.IsNotNull(def, $"Missing exhibit definition for {exhibitId}.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at '{prefabPath}'.");

            foreach (ShowcaseNodeRule rule in def.Nodes)
            {
                Transform target = prefab.transform.Find(rule.NodePath);
                Assert.IsNotNull(
                    target,
                    $"Exhibit {exhibitId} rule '{rule.RuleId}' path '{rule.NodePath}' not found in '{prefabPath}'.");
            }
        }
    }
}
