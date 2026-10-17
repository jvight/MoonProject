using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Automated PlayMode feel-capture scenarios for Gameplay &amp; First Minutes (Task CORE FEEL AUDIT):
    /// - Scenario 6 (<c>verbs</c>): Scan, Cutter (wreck + sealed crate), Tether &amp; Winch, Build mode, Face Inventory, Dock &amp; Charge.
    /// - Scenario 7 (<c>first_minutes</c>): Title -&gt; New Game -&gt; Opening wake -&gt; first control -&gt; crater -&gt; tower -&gt; depot -&gt; ridge -&gt; ruins basin -&gt; chasm lip, capturing 1 frame every 2 s plus key beats.
    /// </summary>
    [Explicit("Automated feel capture harness for Gameplay scenarios; invoked by tools/feel_capture.py.")]
    [Category("FeelCapture")]
    [PrebuildSetup(typeof(PlaythroughScene))]
    [PostBuildCleanup(typeof(PlaythroughScene))]
    public sealed class FeelCaptureGameplayTests : InputTestFixture
    {
        private const float FrameTime = 1f / 30f;
        private const float WakeTimeout = 25f;
        private const float WakeEaseTimeout = 8f;
        private const string FallenArrayFirstFragmentId = "ruin.FallenSolarArray.0";
        private const string DuckRelicId = "rubber_duck";

        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private Mouse _mouse;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private UIDocument _document;
        private EventRecorder _events;
        private IDisposable _awokeSub;
        private bool _awake;
        private float _nextPeriodicCapture;
#if UNITY_EDITOR
        private bool _savedSkipTitle;
#endif

        public override void Setup()
        {
            base.Setup();
#if UNITY_EDITOR
            _savedSkipTitle = SessionState.GetBool(GameBootstrap.TestRunSkipsTitleKey, false);
#endif
            Time.captureDeltaTime = FrameTime;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _rover?.SetDriveSource(null);
            _events?.Dispose();
            _awokeSub?.Dispose();
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, _savedSkipTitle);
#endif
            UnloadPlaythroughScene();
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Scenario06_Verbs()
        {
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, true);
#endif
            yield return BootScene(waitForWakeAndHandover: true, disableProgression: true);

            var terrain = _context.Get<ITerrainQuery>();
            var placement = _context.Get<IRoverPlacement>();
            var abilities = _context.Get<IRoverAbilities>();
            using var recorder = new FeelCaptureRecorder("verbs", _view, _rover, terrain, _document);
            recorder.EnableUiComposite(true);

            BuildField build = _gameplay.Build;
            ScanField scans = _gameplay.Scans;
            SalvageField salvage = _gameplay.Salvage;

            // -----------------------------------------------------------------
            // 1. SCAN (Hold E on FallenSolarArray fragment)
            // -----------------------------------------------------------------
            ScanFragment fallenArrayFrag = scans.Find(FallenArrayFirstFragmentId);
            Assert.IsNotNull(fallenArrayFrag, "FallenSolarArray scan fragment must exist.");
            yield return PlaceRoverForScan(fallenArrayFrag);
            yield return StepFrames(recorder, 12);

            Press(_keyboard.eKey);
            float scanDeadline = Time.time + 10f;
            bool capturedMidScan = false;
            while (!fallenArrayFrag.Scanned && Time.time < scanDeadline)
            {
                yield return null;
                recorder.StepFrame();
                if (!capturedMidScan && fallenArrayFrag.Progress >= 0.48f)
                {
                    recorder.CaptureFrame("scan_beam_active");
                    capturedMidScan = true;
                }
            }

            Release(_keyboard.eKey);
            yield return StepFrames(recorder, 18);
            recorder.CaptureFrame("scan_complete_toast");
            recorder.SetMetric("scanFragmentCompleted", fallenArrayFrag.Scanned ? 1f : 0f);

            // -----------------------------------------------------------------
            // 2. LASER CUTTER ON WRECK PIECE + SEALED CONTAINER (Finding #14)
            // -----------------------------------------------------------------
            SalvageSite depot = salvage.Find(WorldAnchorIds.SitePrefix + "depot");
            Assert.IsNotNull(depot, "Supply depot must exist.");
            SalvagePiece targetPiece = null;
            foreach (SalvagePiece piece in depot.Pieces)
            {
                if (piece.IsCuttable)
                {
                    targetPiece = piece;
                    break;
                }
            }

            Assert.IsNotNull(targetPiece, "Depot must have a cuttable salvage piece.");
            Vector3 cutPos = targetPiece.CutPosition;
            Vector3 cutOutward = Flat(targetPiece.CutNormal).magnitude >= 0.35f
                ? Flat(targetPiece.CutNormal).normalized
                : Flat(cutPos - depot.Position).normalized;
            Vector3 cutSpot = Flat(cutPos + cutOutward * 4.2f);
            cutSpot.y = terrain.SampleHeight(cutSpot.x, cutSpot.z);
            placement.PlaceAt(cutSpot, Quaternion.LookRotation(Flat(cutPos - cutSpot).normalized, Vector3.up));
            yield return StepFrames(recorder, 20);

            int cutsBefore = CompletedCuts();
            Press(_keyboard.eKey);
            bool capturedCharge = false;
            bool capturedCutBeam = false;
            float cutDeadline = Time.time + 10f;
            while (CompletedCuts() == cutsBefore && Time.time < cutDeadline)
            {
                yield return null;
                recorder.StepFrame();
                if (!capturedCharge && salvage.Cutting != null && _events.SalvageCutStarted.Count == 0)
                {
                    recorder.CaptureFrame("cutter_wreck_charge");
                    capturedCharge = true;
                }

                if (!capturedCutBeam && _events.SalvageCutStarted.Count > 0 && salvage.Cutting != null &&
                    salvage.Cutting.Progress >= 0.4f)
                {
                    recorder.SetMetric(
                        "wreckCutPointErrorMeters",
                        Vector3.Distance(salvage.CutPoint, salvage.Cutting.CutPosition));
                    recorder.CaptureFrame("cutter_wreck_beam");
                    capturedCutBeam = true;
                }
            }

            Release(_keyboard.eKey);
            yield return StepFrames(recorder, 8);
            recorder.CaptureFrame("cutter_wreck_sever");

            // Cut any remaining depot pieces so the sealed coil container in the depot heart becomes available.
            foreach (SalvagePiece piece in depot.Pieces)
            {
                if (!piece.IsCuttable)
                {
                    continue;
                }

                Vector3 pPos = piece.CutPosition;
                Vector3 pOut = Flat(piece.CutNormal).magnitude >= 0.35f
                    ? Flat(piece.CutNormal).normalized
                    : Flat(pPos - depot.Position).normalized;
                Vector3 pSpot = Flat(pPos + pOut * 4.2f);
                pSpot.y = terrain.SampleHeight(pSpot.x, pSpot.z);
                placement.PlaceAt(pSpot, Quaternion.LookRotation(Flat(pPos - pSpot).normalized, Vector3.up));
                yield return StepFrames(recorder, 12);
                int prevDone = CompletedCuts();
                Press(_keyboard.eKey);
                float dl = Time.time + 8f;
                while (CompletedCuts() == prevDone && Time.time < dl)
                {
                    yield return null;
                    recorder.StepFrame();
                }

                Release(_keyboard.eKey);
                yield return StepFrames(recorder, 4);
            }

            // Cut the SealedContainer in the depot heart to capture Finding #14 (beam fires at world origin (0,0,0)).
            SealedContainer coilContainer = _gameplay.Containers.Find(
                TowerPartField.ContainerId(_gameplay.TowerParts.PartAt(0)));
            if (coilContainer != null)
            {
                float availDeadline = Time.time + 3f;
                while (!coilContainer.Available && Time.time < availDeadline)
                {
                    yield return null;
                    recorder.StepFrame();
                }

                Vector3 sealFront = Flat(
                    coilContainer.CutPoint.position - coilContainer.Root.right * 4.6f + coilContainer.Root.forward * 0.4f);
                sealFront.y = terrain.SampleHeight(sealFront.x, sealFront.z);
                Quaternion sealLook = Quaternion.LookRotation(
                    Flat(coilContainer.CutPoint.position - sealFront).normalized,
                    Vector3.up) * Quaternion.Euler(0f, -18f, 0f);
                placement.PlaceAt(sealFront, sealLook);
                yield return StepFrames(recorder, 15);

                Press(_keyboard.eKey);
                bool capturedSealCut = false;
                float sealDeadline = Time.time + 8f;
                while (!coilContainer.Opened && Time.time < sealDeadline)
                {
                    yield return null;
                    recorder.StepFrame();
                    if (!capturedSealCut && coilContainer.Progress >= 0.35f)
                    {
                        float anchorDistFromTarget = Vector3.Distance(
                            salvage.CutPoint,
                            coilContainer.CutPoint.position);
                        recorder.SetMetric("sealedContainerCutAnchorMagnitude", salvage.CutPoint.magnitude);
                        recorder.SetMetric("sealedContainerCutPointErrorMeters", anchorDistFromTarget);
                        recorder.SetMetric("sealedContainerHeldStillDuringCut", _rover.IsHeldStill ? 1f : 0f);
                        recorder.CaptureFrame("sealed_crate_cut_missing_beam");
                        capturedSealCut = true;
                    }
                }

                Release(_keyboard.eKey);
                yield return StepFrames(recorder, 10);
            }

            // -----------------------------------------------------------------
            // 3. TETHER & WINCH (Attach to loose relic, tow, and scroll winch)
            // -----------------------------------------------------------------
            abilities.Grant(RoverAbility.Tether);
            _gameplay.Equipment.FitItself(RoverKitPiece.TetherRing);
            SalvageSite garage = salvage.Find(WorldAnchorIds.SitePrefix + "garage");
            Relic duck = null;
            if (garage != null)
            {
                foreach (Relic r in garage.Relics)
                {
                    if (r.Definition.Id == DuckRelicId)
                    {
                        duck = r;
                        break;
                    }
                }
            }

            if (duck != null)
            {
                Vector3 basePos = _context.Get<IWorldLayout>().BasePosition;
                Vector3 towStart = new Vector3(basePos.x + 22f, 0f, basePos.z - 12f);
                towStart.y = terrain.SampleHeight(towStart.x, towStart.z);
                placement.PlaceAt(towStart, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                Vector3 duckSpot = towStart + Vector3.forward * 4.2f;
                duckSpot.y = terrain.SampleHeight(duckSpot.x, duckSpot.z) + 0.5f;
                duck.BeginLift();
                duck.SetLiftPose(duckSpot, Quaternion.identity, 1f);
                duck.Surface(Vector3.zero, Vector3.zero);
                yield return StepFrames(recorder, 18);

                Press(_mouse.rightButton);
                float latchDeadline = Time.time + 4f;
                while (_gameplay.Tether.State != TetherAimState.Towing && Time.time < latchDeadline)
                {
                    Set(_gamepad.leftStick, new Vector2(0f, 0.35f));
                    yield return null;
                    recorder.StepFrame();
                }

                Set(_gamepad.leftStick, Vector2.zero);
                yield return StepFrames(recorder, 6);
                float initialTetherLen = _gameplay.Tether.Length;
                recorder.SetMetric("tetherInitialLengthMeters", initialTetherLen);
                recorder.CaptureFrame("tether_attached_tow");

                // Reel in with scroll wheel while driving forward slowly
                float reelUntil = Time.time + 1.4f;
                while (Time.time < reelUntil)
                {
                    Set(_mouse.scroll, new Vector2(0f, 1f));
                    Set(_gamepad.leftStick, new Vector2(0f, 0.7f));
                    yield return null;
                    recorder.StepFrame();
                }

                Set(_mouse.scroll, Vector2.zero);
                Set(_gamepad.leftStick, Vector2.zero);
                float reeledInLen = _gameplay.Tether.Length;
                recorder.SetMetric("tetherReeledInLengthMeters", reeledInLen);
                recorder.CaptureFrame("tether_winch_reeling");

                Release(_mouse.rightButton);
                yield return StepFrames(recorder, 10);
            }

            // -----------------------------------------------------------------
            // 4. BUILD MODE (B menu -> E ghost -> tap-E cancel bug -> hold-E weld)
            // -----------------------------------------------------------------
            Vector3 buildSpot = _context.Get<IWorldLayout>().BasePosition + new Vector3(18f, 0f, 14f);
            buildSpot.y = terrain.SampleHeight(buildSpot.x, buildSpot.z);
            placement.PlaceAt(buildSpot, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return StepFrames(recorder, 15);

            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition padDef));
            _gameplay.Materials.Stow(padDef.BuildCost);
            _gameplay.Materials.Stow(padDef.BuildCost);

            Vector3 validAim = buildSpot + Vector3.forward * 4.6f;
            validAim.y = terrain.SampleHeight(validAim.x, validAim.z);
            build.SetAimPointOverride(validAim);

            // 4a. Press B to open build strip menu
            yield return TapKey(recorder, _keyboard.bKey);
            yield return StepFrames(recorder, 20);
            VisualElement buildStrip = _document.rootVisualElement.Q<VisualElement>("build-strip");
            bool stripVisibleInMenu = buildStrip != null
                && buildStrip.style.display != DisplayStyle.None
                && buildStrip.style.opacity.value > 0.05f;
            recorder.SetMetric("buildStripVisibleInMenu", stripVisibleInMenu ? 1f : 0f);
            recorder.SetMetric("buildModeConsumesInput", _context.Input.HasModeOwner && _rover.IsHeldStill ? 1f : 0f);
            recorder.CaptureFrame("build_strip_menu");

            // 4b. Tap E to enter Placing state (demonstrating build strip staying visible in Placing state)
            yield return TapKey(recorder, _keyboard.eKey);
            yield return StepFrames(recorder, 20);
            bool stripVisibleInPlacing = buildStrip != null
                && buildStrip.style.display != DisplayStyle.None
                && buildStrip.style.opacity.value > 0.05f;
            recorder.SetMetric("buildStripVisibleInPlacing", stripVisibleInPlacing ? 1f : 0f);
            recorder.CaptureFrame("build_placing_ghost");

            // 4c. Verify Finding #21 (Blocker): Tapping E to place stays in Welding awaiting Hold E, leaving 0 orphan frames.
            Press(_keyboard.eKey);
            yield return null;
            recorder.StepFrame();
            bool enteredWeldingOnPress = build.State == BuildModeState.Welding;
            Release(_keyboard.eKey);
            yield return null;
            recorder.StepFrame();
            bool canceledImmediatelyOnRelease = enteredWeldingOnPress && build.State == BuildModeState.Closed;
            int orphanedFrames = 0;
            for (int i = 0; i < _gameplay.PlacedBase.Count; i++)
            {
                PlacedStructure ps = _gameplay.PlacedBase.StructureAt(i);
                if (!ps.IsBuilt &&
                    (build.State != BuildModeState.Welding || ps.InstanceId != build.Controller.ActiveWeldInstanceId))
                {
                    orphanedFrames++;
                }
            }

            recorder.SetMetric("tapPlaceImmediateCancelBug", canceledImmediatelyOnRelease ? 1f : 0f);
            recorder.SetMetric("unfinishedFramesLeftOnTapPlace", orphanedFrames);

            // 4d. Hold E continuously to weld the placed ChargingPad to completion
            Vector3 validAim2 = buildSpot + new Vector3(4.5f, 0f, 3.8f);
            validAim2.y = terrain.SampleHeight(validAim2.x, validAim2.z);
            build.SetAimPointOverride(validAim2);
            if (build.State == BuildModeState.Closed)
            {
                yield return TapKey(recorder, _keyboard.bKey);
                yield return StepFrames(recorder, 6);
                yield return TapKey(recorder, _keyboard.eKey);
                yield return StepFrames(recorder, 6);
            }

            Press(_keyboard.eKey);
            yield return null;
            recorder.StepFrame();
            int placedId = build.Controller.ActiveWeldInstanceId;
            bool capturedWeld = false;
            float weldDeadline = Time.time + padDef.BuildSeconds + 3f;
            while (Time.time < weldDeadline)
            {
                yield return null;
                recorder.StepFrame();
                if (_gameplay.PlacedBase.TryGetStructure(placedId, out PlacedStructure current))
                {
                    if (!capturedWeld && current.BuildProgress01 >= 0.45f)
                    {
                        recorder.SetMetric("buildWeldBeamLayerCount", build.WeldBeamLayerCount);
                        recorder.SetMetric("buildWeldLightIntensity", build.WeldLightIntensity);
                        recorder.SetMetric("buildWeldSeamHeat", build.WeldSeamHeat);
                        recorder.CaptureFrame("build_welding_pad");
                        capturedWeld = true;
                    }

                    if (current.IsBuilt)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            Release(_keyboard.eKey);
            yield return StepFrames(recorder, 12);
            recorder.CaptureFrame("build_pad_complete");
            build.SetAimPointOverride(null);

            // -----------------------------------------------------------------
            // 5. FACE INVENTORY (Tab open / close)
            // -----------------------------------------------------------------
            yield return TapKey(recorder, _keyboard.tabKey);
            yield return StepFrames(recorder, 24);
            recorder.CaptureFrame("face_inventory_open");

            yield return TapKey(recorder, _keyboard.tabKey);
            yield return StepFrames(recorder, 20);
            recorder.CaptureFrame("face_inventory_closed");

            // -----------------------------------------------------------------
            // 6. DOCK & CHARGE (Park on home ChargingDock)
            // -----------------------------------------------------------------
            ChargingDock dock = _gameplay.Home.Dock;
            Vector3 dockFacing = Flat(dock.Rotation * Vector3.forward).normalized;
            Vector3 dockSpot = Flat(dock.Position);
            dockSpot.y = terrain.SampleHeight(dockSpot.x, dockSpot.z);
            placement.PlaceAt(dockSpot, Quaternion.LookRotation(dockFacing, Vector3.up));

            float dockDeadline = Time.time + _gameplay.Home.Tuning.DockDelay + 3f;
            while (!dock.Docked && Time.time < dockDeadline)
            {
                yield return null;
                recorder.StepFrame();
            }

            yield return StepFrames(recorder, 25);
            recorder.SetMetric("dockedState", dock.Docked ? 1f : 0f);
            recorder.SetMetric("dockGlowLevel", dock.GlowLevel);
            recorder.CaptureFrame("dock_charging");

            recorder.SaveReport();
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Scenario07_FirstMinutes()
        {
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, false);
#endif
            yield return BootScene(waitForWakeAndHandover: false, disableProgression: false);

            var terrain = _context.Get<ITerrainQuery>();
            using var recorder = new FeelCaptureRecorder("first_minutes", _view, _rover, terrain, _document);
            recorder.EnableUiComposite(true);

            var flow = _context.Get<IGameFlow>();
            Assert.AreEqual(GameFlowState.Title, flow.State, "First-minutes scenario boots into the Title screen.");

            // Let first-boot Language Picker settle, then select English and capture Title main menu
            yield return StepFrames(recorder, 18);
            Button langButton = _document.rootVisualElement.Q<VisualElement>("page-language")?.Q<Button>();
            if (langButton != null)
            {
                using NavigationSubmitEvent submitLang = NavigationSubmitEvent.GetPooled();
                submitLang.target = langButton;
                langButton.SendEvent(submitLang);
                yield return StepFrames(recorder, 22);
            }

            recorder.CaptureFrame("title_screen");

            // Click New Game button on Title screen so PauseMenu closes the title CRT overlay and publishes NewGameRequested
            float newGameStartTime = Time.time;
            Button newGameButton = _document.rootVisualElement.Q<Button>("button-new-game");
            if (newGameButton != null)
            {
                using NavigationSubmitEvent submitNewGame = NavigationSubmitEvent.GetPooled();
                submitNewGame.target = newGameButton;
                newGameButton.SendEvent(submitNewGame);
            }
            else
            {
                _context.Events.Publish(new NewGameRequested());
            }

            yield return StepFrames(recorder, 18);
            recorder.CaptureFrame("title_new_game_push_in");

            // Wait for 07 to wake on the charging dock
            float wakeDeadline = Time.time + WakeTimeout;
            bool capturedOpening = false;
            while (!_awake && Time.time < wakeDeadline)
            {
                yield return null;
                recorder.StepFrame();
                if (!capturedOpening && Time.time - newGameStartTime >= 2.0f)
                {
                    recorder.CaptureFrame("opening_wake_sequence");
                    capturedOpening = true;
                }
            }

            Assert.IsTrue(_awake, "07 wakes up on the charging dock.");
            float wakeDuration = Time.time - newGameStartTime;
            recorder.SetMetric("titleToWakeSeconds", wakeDuration);
            recorder.CaptureFrame("rover_awoke_on_dock");

            // Hand over from opening camera with gentle right-stick look
            if (_cameraRig.Opening.IsActive)
            {
                float lookStart = Time.time;
                while (Time.time - lookStart < 0.35f)
                {
                    Set(_gamepad.rightStick, new Vector2(0.75f, 0f));
                    yield return null;
                    recorder.StepFrame();
                }

                Set(_gamepad.rightStick, Vector2.zero);
                float easeDeadline = Time.time + WakeEaseTimeout;
                while (_cameraRig.Opening.IsActive && Time.time < easeDeadline)
                {
                    yield return null;
                    recorder.StepFrame();
                }
            }

            recorder.CaptureFrame("first_control_handover");

            // Track PlaceCards and PlaceFirstApproached events to verify Finding #13 (GuidanceSlot mutex drop)
            int placesApproached = 0;
            using IDisposable placeSub = _context.Events.Subscribe<PlaceFirstApproached>(_ => placesApproached++);

            // Set up virtual-stick Autopilot to drive the first minutes
            var pilot = new Autopilot(_rover);
            _rover.SetDriveSource(pilot);
            _nextPeriodicCapture = Time.time + 2.0f;

            // Beat 1: First look around the landing crater & horizon silhouette (pan camera 3.2s)
            float panUntil = Time.time + 3.2f;
            while (Time.time < panUntil)
            {
                Set(_gamepad.rightStick, new Vector2(0.55f, 0.1f));
                yield return null;
                StepPeriodicCapture(recorder);
            }

            Set(_gamepad.rightStick, Vector2.zero);
            recorder.CaptureFrame("crater_horizon_silhouette");

            // Beat 2: Drive from dock to the sleeping Radio Tower
            RadioTower tower = _gameplay.Tower;
            yield return DriveAutopilotWithPeriodicCaptures(
                recorder,
                pilot,
                Flat(tower.PadCentre),
                arriveRadius: 4.5f,
                maxThrottle: 0.7f,
                timeout: 35f);
            recorder.CaptureFrame("sleeping_radio_tower");

            // Beat 3: Drive out of the base crater to the Supply Depot
            SalvageSite depot = _gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + "depot");
            Assert.IsNotNull(depot);
            Vector3 toDepot = Flat(depot.Position - _rover.Position).normalized;
            Vector3 depotApproach = Flat(depot.Position - toDepot * 15f);
            yield return DriveAutopilotWithPeriodicCaptures(
                recorder,
                pilot,
                depotApproach,
                arriveRadius: 3.0f,
                maxThrottle: 1.0f,
                timeout: 45f);
            recorder.CaptureFrame("arriving_supply_depot");

            // Cut one piece at the supply depot
            SalvagePiece firstPiece = null;
            foreach (SalvagePiece p in depot.Pieces)
            {
                if (p.IsCuttable)
                {
                    firstPiece = p;
                    break;
                }
            }

            if (firstPiece != null)
            {
                Vector3 cut = firstPiece.CutPosition;
                Vector3 outward = Flat(firstPiece.CutNormal).magnitude >= 0.35f
                    ? Flat(firstPiece.CutNormal).normalized
                    : Flat(cut - depot.Position).normalized;
                yield return DriveAutopilotWithPeriodicCaptures(
                    recorder,
                    pilot,
                    Flat(cut + outward * 4.5f),
                    arriveRadius: 1.2f,
                    maxThrottle: 0.5f,
                    timeout: 25f);

                float settleUntil = Time.time + 1.5f;
                while (_gameplay.Salvage.Candidate == null && Time.time < settleUntil)
                {
                    yield return null;
                    StepPeriodicCapture(recorder);
                }

                if (_gameplay.Salvage.Candidate != null)
                {
                    int beforeCuts = CompletedCuts();
                    Press(_keyboard.eKey);
                    float cutUntil = Time.time + 6f;
                    while (CompletedCuts() == beforeCuts && Time.time < cutUntil)
                    {
                        yield return null;
                        StepPeriodicCapture(recorder);
                    }

                    Release(_keyboard.eKey);
                    recorder.CaptureFrame("depot_first_salvage_cut");
                }
            }

            // Beat 4: Drive up the ridge toward the Ruins Basin cluster
            ScanFragment fallenArrayFrag = _gameplay.Scans.Find(FallenArrayFirstFragmentId);
            Vector3 ruinsTarget = fallenArrayFrag != null
                ? Flat(fallenArrayFrag.Point + new Vector3(-8f, 0f, -8f))
                : Flat(_context.Get<IWorldLayout>().BasePosition + new Vector3(45f, 0f, 55f));
            Vector3 midRidge = Vector3.Lerp(Flat(_rover.Position), ruinsTarget, 0.5f);
            yield return DriveAutopilotWithPeriodicCaptures(
                recorder,
                pilot,
                midRidge,
                arriveRadius: 4.0f,
                maxThrottle: 1.0f,
                timeout: 35f);
            recorder.CaptureFrame("ridge_climb_earthset_vista");

            // Beat 5: Enter the Ruins Basin
            yield return DriveAutopilotWithPeriodicCaptures(
                recorder,
                pilot,
                ruinsTarget,
                arriveRadius: 4.0f,
                maxThrottle: 0.85f,
                timeout: 40f);
            recorder.CaptureFrame("entering_ruins_basin");

            VisualElement placeCardEl = _document.rootVisualElement.Q<VisualElement>("place-card");
            bool placeCardVisible = placeCardEl != null && placeCardEl.resolvedStyle.opacity > 0.1f;
            recorder.SetMetric("placesApproachedCount", placesApproached);
            recorder.SetMetric("placeCardVisibleOnRuinsEntry", placeCardVisible ? 1f : 0f);

            // Beat 6: Drive to the Chasm Lip and measure Finding #12 (ChasmLookSeconds hard input lock)
            if (_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip))
            {
                Vector3 lipStand = Flat(lip.Position - lip.Forward * 8f);
                // Arm _bellLit on BaseProgression so approaching the lip triggers StepChasm
                FieldInfo bellLitField = typeof(BaseProgression).GetField(
                    "_bellLit",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bellLitField?.SetValue(_gameplay.Progression, true);

                yield return DriveAutopilotWithPeriodicCaptures(
                    recorder,
                    pilot,
                    lipStand,
                    arriveRadius: 2.5f,
                    maxThrottle: 0.85f,
                    timeout: 55f);

                float chasmWait = Time.time + 2.5f;
                while (!_gameplay.Progression.LookingAcross && Time.time < chasmWait)
                {
                    yield return null;
                    StepPeriodicCapture(recorder);
                }

                bool heldStillAtChasm = _rover.IsHeldStill;
                recorder.SetMetric("chasmLipHardLocksDriveInput", heldStillAtChasm ? 1f : 0f);
                recorder.CaptureFrame("chasm_lip_input_locked");

                float lockStart = Time.time;
                while (_gameplay.Progression.LookingAcross && Time.time - lockStart < 5f)
                {
                    // Player tries to steer/reverse while locked at chasm lip
                    pilot.Reverse = 0.8f;
                    yield return null;
                    StepPeriodicCapture(recorder);
                }

                pilot.Reverse = 0f;
                recorder.SetMetric("chasmLipHardLockSeconds", Time.time - lockStart);
            }

            _rover.SetDriveSource(null);
            recorder.SaveReport();
        }

        private IEnumerator DriveAutopilotWithPeriodicCaptures(
            FeelCaptureRecorder recorder,
            Autopilot pilot,
            Vector3 target,
            float arriveRadius,
            float maxThrottle,
            float timeout)
        {
            pilot.GoTo(target, arriveRadius, maxThrottle);
            float deadline = Time.time + timeout;
            while (!pilot.Arrived && Time.time < deadline)
            {
                yield return null;
                StepPeriodicCapture(recorder);
            }

            pilot.Target = null;
            float settleUntil = Time.time + 0.6f;
            while (_rover.Speed > 0.4f && Time.time < settleUntil)
            {
                yield return null;
                StepPeriodicCapture(recorder);
            }
        }

        private void StepPeriodicCapture(FeelCaptureRecorder recorder)
        {
            recorder.StepFrame();
            if (Time.time >= _nextPeriodicCapture)
            {
                int sec = Mathf.RoundToInt(recorder.ElapsedSeconds);
                recorder.CaptureFrame($"t_{sec:D3}s");
                _nextPeriodicCapture = Time.time + 2.0f;
            }
        }

        private IEnumerator BootScene(bool waitForWakeAndHandover, bool disableProgression)
        {
            _context = null;
            _gameplay = null;
            _rover = null;
            _cameraRig = null;
            _view = null;
            _document = null;
            _awake = false;

#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("FeelCaptureGameplayTests loads PlaythroughScene in the Editor.");
#endif

            GameBootstrap bootstrap = null;
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
                _document = _document != null ? _document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(bootstrap, "Scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Scene has GameplaySystem.");
            Assert.IsNotNull(_rover, "Scene has RoverController.");
            Assert.IsNotNull(_cameraRig, "Scene has RoverCameraRig.");
            Assert.IsNotNull(_document, "Scene has UIDocument.");

            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _events = new EventRecorder(_context.Events);

            if (disableProgression)
            {
                BaseProgression progression = _gameplay.GetComponentInChildren<BaseProgression>(true);
                if (progression != null)
                {
                    progression.enabled = false;
                }
            }

            _awokeSub = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            if (!waitForWakeAndHandover)
            {
                yield break;
            }

            float wakeDeadline = Time.time + WakeTimeout;
            while (!_awake && Time.time < wakeDeadline)
            {
                yield return null;
            }

            Assert.IsTrue(_awake, "07 woke up.");
            if (_cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(0.8f, 0f));
                float until = Time.time + 0.3f;
                while (Time.time < until)
                {
                    yield return null;
                }

                Set(_gamepad.rightStick, Vector2.zero);
                float easeDeadline = Time.time + WakeEaseTimeout;
                while (_cameraRig.Opening.IsActive && Time.time < easeDeadline)
                {
                    yield return null;
                }
            }

            // Settle initial prompts
            Press(_keyboard.wKey);
            yield return null;
            yield return null;
            Release(_keyboard.wKey);
            _context.Events.Publish(new RoverWideShotChanged(false));
            _context.Events.Publish(new NextStepChanged(string.Empty));
            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator PlaceRoverForScan(ScanFragment fragment)
        {
            var terrain = _context.Get<ITerrainQuery>();
            var placement = _context.Get<IRoverPlacement>();
            Transform host = fragment.Root.parent != null ? fragment.Root.parent : fragment.Root;
            Vector3 away = Flat(fragment.Point - host.position);
            float baseBearing = away.sqrMagnitude > 1e-4f ? SurfaceRules.Bearing(away.normalized) : 195f;
            float[] distances = { 3.4f, 4.0f, 4.6f };

            foreach (float distance in distances)
            {
                for (int step = 0; step < 24; step++)
                {
                    float sign = (step & 1) == 0 ? 1f : -1f;
                    float offset = (step / 2) * 15f * sign;
                    Vector3 dir = SurfaceRules.BearingDirection(baseBearing + offset);
                    Vector3 spot = Flat(fragment.Point + dir * distance);
                    float ground = terrain.SampleHeight(spot.x, spot.z);
                    if (Physics.CheckSphere(
                        new Vector3(spot.x, ground + 1.6f, spot.z),
                        1.4f,
                        Layers.PropMask,
                        QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    Vector3 lookDir = Flat(fragment.Point - spot).normalized;
                    Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up);
                    placement.PlaceAt(new Vector3(spot.x, ground, spot.z), rot);
                    yield return null;
                    yield return null;
                    if (_gameplay.Scans.Candidate == fragment)
                    {
                        yield break;
                    }
                }
            }
        }

        private static IEnumerator StepFrames(FeelCaptureRecorder recorder, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                recorder.StepFrame();
            }
        }

        private IEnumerator TapKey(FeelCaptureRecorder recorder, ButtonControl key)
        {
            Press(key);
            yield return null;
            recorder.StepFrame();
            Release(key);
            yield return null;
            recorder.StepFrame();
        }

        private int CompletedCuts()
        {
            int completed = 0;
            foreach (EventRecorder.Timed<SalvageCutStopped> stopped in _events.SalvageCutStopped)
            {
                completed += stopped.Value.Completed ? 1 : 0;
            }

            return completed;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static void UnloadPlaythroughScene()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
