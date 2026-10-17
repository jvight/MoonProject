using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// EditMode unit and wiring tests for <see cref="FaceDirector"/>, <see cref="FaceDirectorTuning"/>, and
    /// <see cref="RoverPrefabBuilder"/>'s CRT face wiring (0.5.7).
    /// </summary>
    public sealed class FaceDirectorTests
    {
        private FaceTuning _faceTuning;
        private FaceDirectorTuning _directorTuning;

        [SetUp]
        public void SetUp()
        {
            _faceTuning = ScriptableObject.CreateInstance<FaceTuning>();
            _directorTuning = ScriptableObject.CreateInstance<FaceDirectorTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_faceTuning != null)
            {
                Object.DestroyImmediate(_faceTuning);
            }

            if (_directorTuning != null)
            {
                Object.DestroyImmediate(_directorTuning);
            }
        }

        [Test]
        public void Boot_PlaysOnlyOnWakeOrNewGameStart_ThenSettlesToCalm()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            Assert.IsFalse(director.HasBootedOnWake, "Fresh director must not boot before wake.");
            Assert.AreEqual(0, director.BootCount);
            Assert.IsFalse(director.State.IsBooting);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.OnSceneStart(isNewGame: false);
            director.OnBatteryWarned();
            director.OnScanStarted();
            director.OnScanStopped();
            director.OnFragmentScanned();
            director.Step(2.0f);

            Assert.AreEqual(0, director.BootCount, "Non-wake events and loaded-game start must never trigger Boot().");

            director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Crackle, false));
            Assert.IsTrue(director.HasBootedOnWake);
            Assert.AreEqual(1, director.BootCount);
            Assert.IsTrue(director.State.IsBooting);
            Assert.AreEqual(0f, director.State.BootProgress, 1e-5f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.Step(0.3f);
            float midBoot = director.State.BootProgress;
            Assert.That(midBoot, Is.GreaterThan(0.1f).And.LessThan(1.0f));

            director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.LidHalf, false));
            director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.GazeHome, false));
            Assert.AreEqual(1, director.BootCount, "Later wake beats in the same wake sequence must not restart Boot().");
            Assert.AreEqual(midBoot, director.State.BootProgress, 1e-5f);

            director.Step(2.0f);
            Assert.IsFalse(director.State.IsBooting);
            Assert.AreEqual(1f, director.State.BootProgress, 1e-5f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            var freshStartDirector = new FaceDirector(_faceTuning, _directorTuning, seed: 7);
            freshStartDirector.OnSceneStart(isNewGame: true);
            Assert.AreEqual(1, freshStartDirector.BootCount);
            Assert.IsTrue(freshStartDirector.State.IsBooting);
            Assert.AreEqual(FaceFeeling.Calm, freshStartDirector.CurrentFeeling);
        }

        [Test]
        public void Charging_OverridesLowBatteryAndShortBeats_AndWakesWithBlinkToCalm()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnBatteryWarned(BatteryWarning.Low);
            Assert.AreEqual(FaceFeeling.Worried, director.CurrentFeeling);
            Assert.AreEqual(FaceSymbol.Battery, director.State.ActiveSymbol);

            director.OnChargingChanged(true);
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, "Charging must override low battery.");
            Assert.AreEqual(FaceSymbol.Zzz, director.State.ActiveSymbol);

            director.OnScanStarted();
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, "Charging must override scanning.");
            director.OnFragmentScanned();
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, "Charging must override fragment Happy beat.");
            director.OnUpgradePurchased();
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, "Charging must override Proud beat.");
            director.OnLanded(0.95f);
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, "Charging must override Startled beat.");

            director.OnChargingChanged(false);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling, "Unplugging from charger must transition to Calm.");
            Assert.AreEqual(1, director.WakeBlinkCount, "Unplugging from charger must trigger a wake blink.");
            Assert.IsTrue(director.State.IsBlinking, "FaceState.IsBlinking must be active right after charging ends.");
        }

        [Test]
        public void ScanAndFragmentBeats_ReturnToWorriedWhileBatteryIsLow()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.SetBatteryCharge(0.18f);
            director.Step(0.5f);
            Assert.AreEqual(FaceFeeling.Worried, director.CurrentFeeling);
            Assert.AreEqual(FaceSymbol.Battery, director.State.ActiveSymbol);

            director.OnScanStarted();
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling, "Active scan shows Curious.");

            director.Step(0.6f);
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling);

            director.OnScanStopped();
            director.Step(0.1f);
            Assert.AreEqual(
                FaceFeeling.Worried,
                director.CurrentFeeling,
                "Stopping scan after ScanMinHoldSeconds must return to Worried while battery is low.");

            director.OnFragmentScanned();
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling, "FragmentScanned plays a short Happy beat.");

            director.Step(_directorTuning.FragmentHappyHoldSeconds * 0.5f);
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);

            director.Step(_directorTuning.FragmentHappyHoldSeconds);
            Assert.AreEqual(
                FaceFeeling.Worried,
                director.CurrentFeeling,
                "Short Happy beat must return to Worried while battery remains low.");
        }

        [Test]
        public void BlueprintCompleted_PlaysLongerHappyBeatThanFragmentScanned()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnFragmentScanned();
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);
            director.Step(_directorTuning.FragmentHappyHoldSeconds + 0.1f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.OnFragmentScanned();
            director.OnBlueprintCompleted();
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);

            director.Step(_directorTuning.FragmentHappyHoldSeconds + 0.25f);
            Assert.AreEqual(
                FaceFeeling.Happy,
                director.CurrentFeeling,
                "BlueprintCompleted must hold Happy longer than a single FragmentScanned beat.");

            director.Step(_directorTuning.BlueprintHappyHoldSeconds);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
        }

        [Test]
        public void LowPower_EntersTired_AndRampsSetLowPowerInAndOut()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnLowPowerChanged(true);
            Assert.AreEqual(FaceFeeling.Tired, director.CurrentFeeling);
            Assert.AreEqual(0f, director.LowPowerAmount, 1e-5f);

            director.Step(_directorTuning.LowPowerRampSeconds * 0.5f);
            Assert.That(director.LowPowerAmount, Is.GreaterThan(0.3f).And.LessThan(0.7f));
            Assert.AreEqual(director.LowPowerAmount, director.State.LowPower, 1e-5f);

            director.Step(_directorTuning.LowPowerRampSeconds);
            Assert.AreEqual(1f, director.LowPowerAmount, 1e-5f);
            Assert.AreEqual(1f, director.State.LowPower, 1e-5f);

            director.OnLowPowerChanged(false);
            director.Step(_directorTuning.LowPowerRecoverSeconds + 0.1f);
            Assert.AreEqual(0f, director.LowPowerAmount, 1e-5f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
        }

        [Test]
        public void BatteryStates_PreemptShortBeats_AndOverrideEarthGlance()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnFragmentScanned();
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);

            director.OnBatteryWarned(BatteryWarning.Low);
            Assert.AreEqual(
                FaceFeeling.Worried,
                director.CurrentFeeling,
                "Battery warning must immediately override an active short beat.");

            director.SetEarthInView(true);
            director.Step(0.2f);
            Assert.AreEqual(
                FaceFeeling.Worried,
                director.CurrentFeeling,
                "Low battery Worried must have higher priority than an ambient Earth glance.");
        }

        [Test]
        public void UpgradeAndPartFitted_TriggerProud_AndHardLandingOrFall_TriggerStartled()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnPartFitted(restored: true);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling, "Restored equipment on load must not play Proud.");

            director.OnUpgradePurchased();
            Assert.AreEqual(FaceFeeling.Proud, director.CurrentFeeling);
            director.Step(_directorTuning.ProudHoldSeconds + 0.1f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.OnLanded(_directorTuning.HardLandingImpactThreshold * 0.5f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling, "Soft landing must not startle 07.");

            director.OnLanded(_directorTuning.HardLandingImpactThreshold + 0.1f);
            Assert.AreEqual(FaceFeeling.Startled, director.CurrentFeeling);
            director.Step(_directorTuning.StartledHoldSeconds + 0.1f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.StepMotion(
                isGrounded: false,
                verticalSpeed: -(_directorTuning.FallVerticalSpeedThreshold + 1.5f),
                planarSpeed: 0.5f,
                inSunlight: true,
                deltaTime: _directorTuning.FallMinAirborneSeconds + 0.05f);
            Assert.AreEqual(FaceFeeling.Startled, director.CurrentFeeling, "Falling airborne must trigger Startled.");
        }

        [Test]
        public void Holds_PreventFlickerOnBriefEvents()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.OnScanStarted();
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling);

            director.Step(0.05f);
            director.OnScanStopped();
            Assert.AreEqual(
                FaceFeeling.Curious,
                director.CurrentFeeling,
                "ScanMinHoldSeconds must prevent a brief scan tap from flickering back to Calm immediately.");

            director.Step(_directorTuning.ScanMinHoldSeconds);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);

            director.SetBatteryCharge(0.20f);
            Assert.AreEqual(FaceFeeling.Worried, director.CurrentFeeling);

            director.Step(0.05f);
            director.SetBatteryCharge(0.80f);
            Assert.AreEqual(
                FaceFeeling.Worried,
                director.CurrentFeeling,
                "MinFeelingHoldSeconds must prevent rapid battery transitions from flickering.");

            director.Step(_directorTuning.MinFeelingHoldSeconds);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
        }

        [Test]
        public void NightIdleSlowLooks_AndEarthGlance_WorkWithoutEverEnteringSad()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            director.StepMotion(
                isGrounded: true,
                verticalSpeed: 0f,
                planarSpeed: 0f,
                inSunlight: false,
                deltaTime: _directorTuning.NightIdleDelaySeconds + 0.5f);
            director.Step(0.2f);

            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
            Assert.IsTrue(director.IsNightIdleSlowLookActive, "Stationary at night past delay must enable slow looks.");
            Assert.IsTrue(director.State.HasLookTarget);

            director.UpdateEarthView(Vector3.forward, Vector3.forward);
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling, "Earth in view triggers a gentle Curious glance.");

            director.Step(_directorTuning.EarthGlanceHoldSeconds + 0.1f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
            Assert.AreNotEqual(FaceFeeling.Sad, director.CurrentFeeling);
        }

        [Test]
        public void LookTarget_FollowsBodyLanguageGaze_AndEyesLeadHeadSlightly()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);

            Vector3 leading = director.UpdateGaze(
                hasExplicitGaze: true,
                targetYawDeg: 20f,
                targetPitchDeg: 8f,
                currentYawDeg: 0f,
                currentPitchDeg: 0f);
            Assert.Greater(director.LastLeadRatio, 1.0f, "Eyes must lead the head while the head is turning.");

            Vector3 settled = director.UpdateGaze(
                hasExplicitGaze: true,
                targetYawDeg: 20f,
                targetPitchDeg: 8f,
                currentYawDeg: 20f,
                currentPitchDeg: 8f);
            Assert.Greater(
                leading.x,
                settled.x,
                "Eye horizontal look offset must be stronger when leading an un-slewed head turn than after the head settles.");
        }

        [Test]
        public void RearVentStrip_EchoesScreenBrightnessAndMood()
        {
            var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7);
            director.Step(0.5f);
            float calmEmission = director.RearVentEmissionStrength;

            director.OnChargingChanged(true);
            director.Step(0.5f);
            float asleepEmission = director.RearVentEmissionStrength;
            Assert.Less(asleepEmission, calmEmission, "RearVentStrip must dim when 07 is asleep on the charger.");

            director.OnChargingChanged(false);
            director.OnLowPowerChanged(true);
            director.Step(_directorTuning.LowPowerRampSeconds + 0.2f);
            float lowPowerEmission = director.RearVentEmissionStrength;
            Assert.Less(lowPowerEmission, asleepEmission, "RearVentStrip must dim further in low-power mode.");

            director.OnLowPowerChanged(false);
            director.Step(_directorTuning.LowPowerRecoverSeconds + 0.2f);
            director.OnFragmentScanned();
            director.Step(0.1f);
            float happyEmission = director.RearVentEmissionStrength;
            Assert.Greater(happyEmission, calmEmission, "RearVentStrip must brighten on Happy/Proud beats.");
        }

        [Test]
        public void RoverPrefabBuilder_WiresFaceDisplayAndRoverFaceDirectorOnScreen()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                var bodyLanguage = rover.GetComponent<RoverBodyLanguage>();
                Assert.IsNotNull(bodyLanguage);
                Assert.IsNotNull(bodyLanguage.FaceDirector, "RoverBodyLanguage._faceDirector must be wired.");

                Transform screen = rover.transform.Find("Visual/Chassis/RoverModel/Neck/NeckLift/Head/Screen");
                Assert.IsNotNull(screen, "Neck/NeckLift/Head/Screen must exist in assembled Rover.");

                var faceDisplay = screen.GetComponent<FaceDisplay>();
                var faceDirector = screen.GetComponent<RoverFaceDirector>();
                Assert.IsNotNull(faceDisplay, "FaceDisplay must be added on Neck/NeckLift/Head/Screen.");
                Assert.IsNotNull(faceDirector, "RoverFaceDirector must be added on Neck/NeckLift/Head/Screen.");
                Assert.AreSame(faceDirector, bodyLanguage.FaceDirector);

                Assert.IsTrue(faceDisplay.ValidateWiring(), "FaceDisplay wiring must be valid.");
                Assert.IsTrue(faceDirector.ValidateWiring(), "RoverFaceDirector wiring must be valid.");
                Assert.AreEqual(
                    AssetDatabase.LoadAssetAtPath<FaceTuning>(RoverAssetPaths.FaceTuning),
                    faceDisplay.Tuning);
                Assert.AreEqual(
                    AssetDatabase.LoadAssetAtPath<FaceDirectorTuning>(RoverAssetPaths.FaceDirectorTuning),
                    faceDirector.Tuning);
                Assert.AreEqual("RearVentStrip", faceDirector.RearVentStrip.name);
                Assert.IsNotNull(faceDirector.RearVentRenderer);
            }
        }

        [Test]
        public void ValidateWiring_FailsLoudlyWithError_WhenReferencesAreMissing()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject empty = scratch.Create("UnwiredFace");
                var display = empty.AddComponent<FaceDisplay>();
                LogAssert.Expect(LogType.Error, new Regex(@"^FaceDisplay: missing required wiring"));
                Assert.IsFalse(display.ValidateWiring(), "Unwired FaceDisplay must fail ValidateWiring.");

                var director = empty.AddComponent<RoverFaceDirector>();
                LogAssert.Expect(LogType.Error, new Regex(@"^RoverFaceDirector: missing required wiring"));
                Assert.IsFalse(director.ValidateWiring(), "Unwired RoverFaceDirector must fail ValidateWiring.");

                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                var bodyLanguage = rover.GetComponent<RoverBodyLanguage>();
                var so = new SerializedObject(bodyLanguage);
                so.FindProperty("_faceDirector").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();

                MethodInfo validate = typeof(RoverBodyLanguage).GetMethod(
                    "ValidateWiring",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(validate);
                LogAssert.Expect(
                    LogType.Error,
                    "RoverBodyLanguage: RoverFaceDirector on Screen is not assigned or incomplete (0.5.7 face contract).");
                bool valid = (bool)validate.Invoke(bodyLanguage, null);
                Assert.IsFalse(valid, "RoverBodyLanguage must fail fast without runtime fallback when _faceDirector is missing.");
                Assert.IsNull(bodyLanguage.FaceDirector, "ValidateWiring must not self-heal _faceDirector at runtime.");
            }
        }

        [Test]
        public void RoverFaceCued_PublishesBootBlinkFeelingsAndScreenOpenCloseOnEventBus()
        {
            var events = new EventBus();
            var cues = new System.Collections.Generic.List<RoverFaceCueKind>();
            using (events.Subscribe<RoverFaceCued>(c => cues.Add(c.Kind)))
            {
                var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7, events: events);

                // 1. Boot on wake
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Crackle, false));
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Boot }, cues,
                    "Starting wake boot must publish RoverFaceCued(Boot).");

                // Finish boot and step through wake blink
                cues.Clear();
                director.Step(1.5f);
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Blinked, false));
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink,
                    "WakeBeat.Blinked must publish RoverFaceCued(Blink).");

                // 2. Feeling changes: Happy, Worried, Asleep, Blink (unplug), Proud, Startled
                cues.Clear();
                director.OnFragmentScanned();
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Happy }, cues);

                cues.Clear();
                director.OnBatteryWarned(BatteryWarning.Low);
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Worried }, cues);

                cues.Clear();
                director.OnChargingChanged(true);
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Asleep }, cues);

                cues.Clear();
                director.OnChargingChanged(false);
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink,
                    "Unplugging from charger triggers a wake blink cue.");

                cues.Clear();
                director.OnUpgradePurchased();
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Proud }, cues);

                director.Step(_directorTuning.ProudHoldSeconds + 0.1f);
                cues.Clear();
                director.OnLanded(_directorTuning.HardLandingImpactThreshold + 0.2f);
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Startled }, cues);

                // 3. Periodic blink during calm stepping
                director.Step(_directorTuning.StartledHoldSeconds + 0.1f);
                cues.Clear();
                director.Step(10.0f);
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink,
                    "Periodic blink must publish RoverFaceCued(Blink).");

                // 4. CRT channel-switch open and close
                cues.Clear();
                director.SetScreenMode(true);
                Assert.AreEqual(RoverFaceCueKind.ScreenOpen, cues[0],
                    "Opening the CRT face screen must publish RoverFaceCueKind.ScreenOpen.");
                director.Step(0.5f);
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink,
                    "Opening lead-in blink must publish RoverFaceCueKind.Blink.");

                cues.Clear();
                director.SetScreenMode(false);
                Assert.AreEqual(RoverFaceCueKind.ScreenClose, cues[0],
                    "Closing the CRT face screen must publish RoverFaceCueKind.ScreenClose.");
                director.Step(0.5f);
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink,
                    "Closing happy blink must publish RoverFaceCueKind.Blink.");
            }
        }

        [Test]
        public void OpeningDarkHold_KeepsScreenDarkUntilPulledOut_ThenBootsAndSlowBlinksWithCuriousGlance()
        {
            var events = new EventBus();
            var cues = new System.Collections.Generic.List<RoverFaceCueKind>();
            using (events.Subscribe<RoverFaceCued>(c => cues.Add(c.Kind)))
            {
                var director = new FaceDirector(_faceTuning, _directorTuning, seed: 7, events: events);
                director.SetOpeningDarkHold(true);

                Assert.IsTrue(director.IsHoldingDarkForOpening);
                Assert.IsTrue(director.State.IsDarkHold);
                Assert.AreEqual(0f, director.State.Brightness, 1e-5f);
                Assert.AreEqual(0f, director.RearVentEmissionStrength, 1e-5f);

                director.OnSceneStart(isNewGame: true);
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Crackle, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.LidCracked, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.LidHalf, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.FocusHunting, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Flicker, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.GazeHome, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.GazePilot, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.GazePeak, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.GazeEarth, false));
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.PullingOut, false));
                director.Step(1.0f);

                Assert.IsTrue(director.IsHoldingDarkForOpening, "CRT stays dark through the lens POV and pull-out.");
                Assert.IsTrue(director.State.IsDarkHold);
                Assert.AreEqual(0, director.BootCount, "Face must not boot before PulledOut.");
                Assert.AreEqual(0f, director.State.Brightness, 1e-5f);
                Assert.AreEqual(0f, director.RearVentEmissionStrength, 1e-5f);
                Assert.IsEmpty(cues);

                // Reveal at PulledOut: single phosphor line flickers on and opens into two eyes
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.PulledOut, false));
                Assert.IsFalse(director.IsHoldingDarkForOpening);
                Assert.IsFalse(director.State.IsDarkHold);
                Assert.AreEqual(1, director.BootCount);
                Assert.IsTrue(director.State.IsBooting);
                CollectionAssert.AreEqual(new[] { RoverFaceCueKind.Boot }, cues);

                director.Step(1.2f);
                Assert.IsFalse(director.State.IsBooting, "Boot completes before WakeBeat.Blinked.");
                Assert.AreEqual(1f, director.State.BootProgress, 1e-5f);

                // Slow blink + curious glance at Blinked
                cues.Clear();
                director.OnWakeBeatReached(new RoverWakeBeatReached(WakeBeat.Blinked, false));
                Assert.AreEqual(1, director.WakeBlinkCount);
                Assert.IsTrue(director.State.IsBlinking);
                Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling);
                CollectionAssert.Contains(cues, RoverFaceCueKind.Blink);
            }
        }
    }
}
