using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// End-to-end PlayMode verification and real-game capture suite for the FACE-INV diegetic CRT inventory pipeline.
    /// Boots <c>Main.unity</c> via <see cref="RoverSessionScene"/>, presses <c>Tab</c> through the real input system,
    /// verifies the 6-beat choreography (chase -> 07 brakes &amp; lifts head -> eyes collapse to horizontal CRT line ->
    /// camera arrives with backdrop blur -> line expands into <c>CargoScreen</c> on 07's curved CRT face -> closing
    /// collapses to line and returns with a happy <c>^ ^</c> blink), verifies curved-screen UV raycasting, hover
    /// tooltips, kit drag onto a socket bubble, gamepad/keyboard focus order, <see cref="RoverFaceCued"/> sound cues,
    /// night vs. day lighting (<c>vi</c>), and rock occlusion avoidance, and writes all required PNGs to <c>Logs/r2/</c>.
    /// </summary>
    public sealed class RoverFaceInventoryPlayModeTests : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float SunriseTimeout = 35f;
        private const float OpenGroundOffset = 12f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
        private static readonly string R2CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "r2"));
        private static readonly string ScreenshotsFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screenshots"));

        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private RoverBodyLanguage _body;
        private RoverFaceDirector _faceDirector;
        private FaceDisplay _faceDisplay;
        private UISystem _uiSystem;
        private ISunlight _sunlight;
        private Camera _view;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Gamepad _gamepad;
        private IDisposable _awokeSub;
        private readonly List<IDisposable> _eventSubs = new List<IDisposable>();
        private readonly List<RoverFaceCueKind> _faceCues = new List<RoverFaceCueKind>();
        private GameObject _rockObstacle;
        private GameObject _slopeSurface;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _awake = false;
            _faceCues.Clear();
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _awokeSub?.Dispose();
            _awokeSub = null;
            for (int i = 0; i < _eventSubs.Count; i++)
            {
                _eventSubs[i]?.Dispose();
            }

            _eventSubs.Clear();
            _faceCues.Clear();

            if (_rockObstacle != null)
            {
                Object.DestroyImmediate(_rockObstacle);
                _rockObstacle = null;
            }

            if (_slopeSurface != null)
            {
                Object.DestroyImmediate(_slopeSurface);
                _slopeSurface = null;
            }

            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator RealScene_TabOpensInventoryOnCrtFace_CapturesSixBeatsNightDayAndRockOcclusion()
        {
            Directory.CreateDirectory(CaptureFolder);
            Directory.CreateDirectory(R2CaptureFolder);
            Directory.CreateDirectory(ScreenshotsFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();

            yield return BootRealScene();

            Assert.IsNotNull(_faceDirector, "RoverFaceDirector must be wired in the real scene.");
            Assert.IsNotNull(_faceDisplay, "FaceDisplay must be wired on Screen in the real scene.");
            Assert.IsNotNull(_uiSystem, "UISystem must be wired in the real scene.");
            Assert.IsNotNull(_uiSystem.Cargo?.View, "FaceInventoryView must be hosted by CargoScreen in UISystem.");
            FaceInventoryView view = _uiSystem.Cargo.View;

            IRoverFaceScreen faceScreen = _context.Get<IRoverFaceScreen>();
            Assert.IsNotNull(faceScreen, "IRoverFaceScreen must be registered in GameContext by RoverController.");
            Assert.IsFalse(faceScreen.IsOpen, "Face screen must start closed.");

            var screenCollider = _body.ScreenTransform.GetComponent<MeshCollider>();
            Assert.IsNotNull(screenCollider, "Live Rover Screen must have a MeshCollider.");
            Assert.IsNotNull(screenCollider.sharedMesh, "Live Rover Screen MeshCollider must have a sharedMesh.");
            Assert.IsTrue(screenCollider.sharedMesh.isReadable,
                "Live Rover Screen MeshCollider.sharedMesh must be readable so RaycastHit.textureCoord works in player builds.");
            Assert.AreEqual(screenCollider.sharedMesh.vertexCount, screenCollider.sharedMesh.uv.Length);

            // Switch to Vietnamese (vi) so the face inventory captures show the full localized Vietnamese UI.
            _context.Get<ILocalization>().SetLanguage("vi");

            // Populate hold (4 stacks: 4+1 Metal, 3 Wiring, 2 Optics) and equipment before placing 07 on open basin ground.
            PopulateHoldAndEquipment();

            IWorldLayout layout = _context.Get<IWorldLayout>();
            ITerrainQuery terrain = _context.Get<ITerrainQuery>();
            float earthBearing = WideShotComposer.Bearing(layout.EarthDirection);
            Vector3 forward = WideShotComposer.Direction(earthBearing);
            Vector3 openSpot = layout.BasePosition - forward * 28f;
            openSpot.y = terrain.SampleHeight(openSpot.x, openSpot.z);

            _context.Get<IRoverPlacement>().PlaceAt(openSpot, Quaternion.Euler(0f, earthBearing, 0f));
            _cameraRig.GentleCamera = false;

            // Subscribe to audio/feel events so we verify all sound hooks fire during open/boot/close.
            var uiCues = new List<UiCueKind>();
            int holdBootStarted = 0;
            int holdSlotsLit = 0;
            int holdItemsArrived = 0;
            int hatchCycles = 0;
            int affirmChirps = 0;
            _eventSubs.Add(_context.Events.Subscribe<UiCue>(c => uiCues.Add(c.Kind)));
            _eventSubs.Add(_context.Events.Subscribe<HoldBootStarted>(_ => holdBootStarted++));
            _eventSubs.Add(_context.Events.Subscribe<HoldSlotLit>(_ => holdSlotsLit++));
            _eventSubs.Add(_context.Events.Subscribe<HoldItemArrived>(_ => holdItemsArrived++));
            _eventSubs.Add(_context.Events.Subscribe<RoverHatchCycleRequested>(_ => hatchCycles++));
            _eventSubs.Add(_context.Events.Subscribe<RoverChirpRequested>(c =>
            {
                if (c.Mood == ChirpMood.Affirm)
                {
                    affirmChirps++;
                }
            }));

            // Angle chase camera to a three-quarter front view so Beat 1 clearly shows 07's chassis & head on the night lunar surface.
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(_rover.Heading + 162f);
            yield return WaitSeconds(1.0f);

            // Verify we start in the opening earthlit night.
            Assert.IsTrue(_sunlight.IsHoldingEclipse, "Fresh session starts in the opening night eclipse.");
            Assert.Less(_sunlight.SunlightAt(_rover.Position), 0.05f);

            FaceState faceState = _faceDirector.Director.State;
            Assert.AreEqual(0f, faceState.ScreenContentMix, 1e-4f);

            // BEAT 1: Chase Camera (Driving / Ready)
            Capture("face-inv-beat-1-chase");

            // Press Tab to open the inventory on 07's CRT face.
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);

            Assert.IsTrue(faceScreen.IsOpen, "Pressing Tab must open IRoverFaceScreen.");
            Assert.IsTrue(_cameraRig.IsFaceInspectOpen, "Pressing Tab must activate face-inspect mode on RoverCameraRig.");
            Assert.IsTrue(_rover.IsHeldStill, "07 must brake and hold still while the face inventory is open.");
            Assert.IsTrue(_body.IsScreenInspectOpen, "RoverBodyLanguage must enter screen-inspect head pose.");
            Assert.AreEqual(CursorLockMode.Confined, _uiSystem.Cursor.LockState, "Inventory open must confine the mouse cursor to the window.");
            Assert.IsTrue(_uiSystem.Cursor.IsVisible, "Inventory open must make the mouse cursor visible.");
            Assert.AreEqual(0, view.FocusedHoldCell, "Opening inventory must place initial focus on hold cell 0.");
            Assert.IsNotNull(_faceDisplay.ContentTexture, "FaceDisplay must receive the 1152x768 UI RenderTexture.");
            Assert.AreEqual(1152, _faceDisplay.ContentTexture.width);
            Assert.AreEqual(768, _faceDisplay.ContentTexture.height);
            CollectionAssert.Contains(uiCues, UiCueKind.Confirm, "Opening Tab must publish UiCue(UiCueKind.Confirm).");
            Assert.AreEqual(1, holdBootStarted, "First Tab open must publish HoldBootStarted.");
            Assert.AreEqual(1, hatchCycles, "First Tab open must publish RoverHatchCycleRequested.");

            // BEAT 2: 07 Stops & Lifts Head (0.00 - 0.18 s lead-in: calm eyes visible while head lifts +10 deg and camera swings)
            yield return WaitSeconds(0.12f);
            Assert.AreEqual(0f, faceState.ScreenContentMix, 1e-4f, "Beat 2: eyes must still be visible during head lift lead-in.");
            Assert.AreEqual(0f, faceState.ScreenVerticalPinch, 1e-4f);
            Capture("face-inv-beat-2-head-lift");

            // BEAT 3: Blink & Collapse to CRT Line (mid-arc channel switch ~0.32 s)
            while (faceState.ScreenVerticalPinch < 0.85f)
            {
                yield return null;
            }

            Assert.Greater(faceState.ScreenVerticalPinch, 0.85f, "Beat 3: CRT face must pinch vertically into a horizontal line.");
            Assert.Greater(faceState.ScreenLineGlow, 0.85f, "Beat 3: horizontal CRT line glow must be hot.");
            CollectionAssert.Contains(_faceCues, RoverFaceCueKind.ScreenOpen, "CRT channel-switch open must publish RoverFaceCued(ScreenOpen).");
            CollectionAssert.Contains(_faceCues, RoverFaceCueKind.Blink, "Blinking on channel switch must publish RoverFaceCued(Blink).");
            Capture("face-inv-beat-3-line-collapse");

            // BEAT 4: Camera Arrives at Screen (~0.45 s: camera settles in front of Screen while CRT carrier line holds)
            while (_cameraRig.FaceInspectWeight < 0.84f && faceState.ScreenContentMix < 0.5f)
            {
                yield return null;
            }

            Capture("face-inv-beat-4-camera-arrives");

            // Wait for the horizontal line to expand into the inventory UI and for the first-open hold boot to complete.
            float bootDeadline = Time.time + 5.0f;
            while (faceState.ScreenContentMix < 0.99f
                || faceState.ScreenVerticalPinch > 0.01f
                || _cameraRig.FaceInspectWeight < 0.99f
                || holdSlotsLit < 8
                || holdItemsArrived < 4
                || affirmChirps < 1)
            {
                Assert.Less(Time.time, bootDeadline, "Hold boot and CRT screen expansion must complete within 5 seconds.");
                yield return null;
            }

            yield return WaitSeconds(0.15f);
            Assert.AreEqual(1f, faceState.ScreenContentMix, 1e-4f);
            Assert.AreEqual(0f, faceState.ScreenVerticalPinch, 1e-4f);
            Assert.AreEqual(8, holdSlotsLit, "All 8 hold slots must light during the hold boot.");
            Assert.AreEqual(4, holdItemsArrived, "All 4 stocked material stacks must slide in during the hold boot.");
            Assert.GreaterOrEqual(affirmChirps, 1, "07 must chirp affirm when the hold boot finishes.");

            // Verify the mouse raycast hits Screen's MeshCollider near UV (0.5, 0.5) from the viewport center.
            Ray centerRay = _view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Assert.IsTrue(faceScreen.TryRaycastScreenUv(centerRay, out Vector2 hitUv),
                "Center viewport ray must hit 07's Screen MeshCollider while face-inspect is open.");
            Assert.AreEqual(0.5f, hitUv.x, 0.10f, "Center ray must hit near horizontal center of Screen.");
            Assert.AreEqual(0.5f, hitUv.y, 0.10f, "Center ray must hit near vertical center of Screen.");

            // BEAT 5: Line Expands into Inventory (and Night Open capture in Vietnamese)
            view.ClearFocus();
            yield return null;
            Assert.IsFalse(view.IsTooltipVisible, "Resting night capture starts with no tooltip visible.");
            Capture("face-inv-beat-5-inventory-expand");
            Capture("face-inv-night-open");
            Capture("face-inv-night-open-vi");

            // CURVED SCREEN INPUT 1: Hover tooltip on 07's curved 3D CRT face.
            Vector2 cell0Panel = FaceInventoryView.GetHoldCellReferenceRect(0).center;
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(cell0Panel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 cell0Screen),
                "Hold cell 0 center must project onto 07's curved 3D CRT screen in viewport coordinates.");
            Set(_mouse.position, cell0Screen);
            yield return null;
            yield return null;
            Assert.IsTrue(view.IsTooltipVisible, "Hovering hold cell 0 on 07's curved CRT face must show the tooltip.");
            StringAssert.AreEqualIgnoringCase("Kim loại", view.TooltipNameText,
                "Tooltip on 07's face must display the Vietnamese material name.");
            Capture("face-inv-hover-tooltip");
            CaptureScreenshot("face_inv_hover");

            // CURVED SCREEN INPUT 2: Drag waiting kit (HoverCoils in hold cell 5) across 07's curved CRT screen onto Belly socket bubble.
            Vector2 kitCellPanel = FaceInventoryView.GetHoldCellReferenceRect(5).center;
            Vector2 bellySocketPanel = FaceInventoryView.GetOrbitalSocketReferenceCenter(FaceInventoryView.OrbitalIndexOf(RoverSocket.Belly));
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(kitCellPanel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 kitScreen),
                "Waiting kit hold cell 5 must project onto 07's curved 3D CRT screen.");
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(bellySocketPanel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 bellyScreen),
                "Belly orbital socket bubble must project onto 07's curved 3D CRT screen.");

            Set(_mouse.position, kitScreen);
            yield return null;
            Press(_mouse.leftButton);
            yield return null;

            Set(_mouse.position, bellyScreen);
            yield return null;
            yield return null;
            Assert.IsTrue(view.IsDraggingKit,
                "Pressing left mouse on a waiting kit cell and dragging across 07's curved face must begin a kit drag.");
            Assert.IsTrue(view.IsSocketLeaderCyan(RoverSocket.Belly),
                "Dragging HoverCoils over the Belly orbital socket bubble must light its leader line cyan.");
            Capture("face-inv-drag-socket");
            CaptureScreenshot("face_inv_drag");

            // Release the drag back over cell 0 so HoverCoils remains in the hold and cell 0 regains focus.
            Set(_mouse.position, cell0Screen);
            yield return null;
            Release(_mouse.leftButton);
            yield return null;
            Assert.IsFalse(view.IsDraggingKit, "Releasing mouse over the hold grid must end the drag without fitting.");
            Assert.AreEqual(0, view.FocusedHoldCell, "Releasing mouse over hold cell 0 must focus hold cell 0.");

            // CURVED SCREEN INPUT 3: Verify 2D spatial keyboard/gamepad focus order across hold cells and orbital sockets (no direct UI method calls).
            yield return TapKey(_keyboard.rightArrowKey);
            Assert.AreEqual(1, view.FocusedHoldCell);
            yield return TapKey(_keyboard.rightArrowKey);
            Assert.AreEqual(2, view.FocusedHoldCell);
            yield return TapKey(_keyboard.rightArrowKey);
            Assert.AreEqual(3, view.FocusedHoldCell);
            yield return TapKey(_keyboard.rightArrowKey);
            Assert.AreEqual(FaceInventoryView.OrbitalIndexOf(RoverSocket.Deck), view.FocusedSocketIndex,
                "Navigating Right from column 3 of row 0 must focus RoverSocket.Deck.");
            yield return TapKey(_keyboard.downArrowKey);
            Assert.AreEqual(FaceInventoryView.OrbitalIndexOf(RoverSocket.Hold), view.FocusedSocketIndex,
                "Navigating Down from Deck must move spatially down to RoverSocket.Hold.");
            yield return TapKey(_keyboard.upArrowKey);
            Assert.AreEqual(FaceInventoryView.OrbitalIndexOf(RoverSocket.Deck), view.FocusedSocketIndex);
            yield return TapKey(_keyboard.upArrowKey);
            Assert.AreEqual(FaceInventoryView.OrbitalIndexOf(RoverSocket.Back), view.FocusedSocketIndex,
                "Navigating Up from Deck must move spatially up to RoverSocket.Back.");
            yield return TapKey(_keyboard.leftArrowKey);
            Assert.AreEqual(3, view.FocusedHoldCell, "Navigating Left from Back must return focus to hold cell 3.");

            // BEAT 6: Close Returns with Happy Blink (^ ^)
            int happyBlinksBefore = faceState.ClosingHappyBlinkCount;
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);

            Assert.IsFalse(faceScreen.IsOpen, "Pressing Tab again must close IRoverFaceScreen.");
            Assert.AreEqual(CursorLockMode.Locked, _uiSystem.Cursor.LockState, "Closing inventory must restore the Locked drive cursor.");
            Assert.IsFalse(_uiSystem.Cursor.IsVisible, "Closing inventory must hide the drive cursor.");
            CollectionAssert.Contains(uiCues, UiCueKind.Back, "Closing Tab must publish UiCue(UiCueKind.Back).");

            // Step through the 0.14s collapse + 0.16s expand until the happy arc eyes (^ ^) open after the blink.
            while (faceState.ClosingHappyBlinkCount <= happyBlinksBefore || faceState.ScreenVerticalPinch > 0.08f || faceState.BlinkMultiplier < 0.75f)
            {
                yield return null;
            }

            Assert.AreEqual(0f, faceState.ScreenContentMix, 1e-4f, "Beat 6: screen must have returned to eye mode.");
            Assert.Greater(faceState.LeftEye.Arc, 0.5f, "Beat 6: closing must show happy curved arc eyes (^ ^).");
            CollectionAssert.Contains(_faceCues, RoverFaceCueKind.ScreenClose, "CRT channel-switch close must publish RoverFaceCued(ScreenClose).");
            Capture("face-inv-beat-6-close-happy-blink");

            // Wait for camera to finish easing back to chase view.
            while (_cameraRig.FaceInspectWeight > 0.01f)
            {
                yield return null;
            }

            Assert.IsFalse(_rover.IsHeldStill, "Closing the face inventory must release 07's hold-still brake.");

            // DAYLIGHT CAPTURE: Release opening eclipse and advance into sunlit lunar day.
            _sunlight.ReleaseOpeningEclipse();
            float sunriseDeadline = Time.time + SunriseTimeout;
            while (_sunlight.SunlightAt(_rover.Position) < 0.95f)
            {
                Assert.Less(Time.time, sunriseDeadline, "Sun breaks out of eclipse into lunar day.");
                yield return null;
            }

            yield return WaitSeconds(2.0f);
            Assert.Greater(_sunlight.SunlightAt(_rover.Position), 0.95f);

            yield return TapTab();
            yield return WaitSeconds(0.90f);
            Assert.IsTrue(faceScreen.IsOpen);
            Assert.AreEqual(1f, faceState.ScreenContentMix, 1e-4f);
            Assert.AreEqual(0f, faceState.ScreenVerticalPinch, 1e-4f);
            view.ClearFocus();
            yield return null;
            Capture("face-inv-day-open");
            Capture("face-inv-day-open-vi");

            yield return TapTab();
            yield return WaitSeconds(0.60f);
            Assert.IsFalse(faceScreen.IsOpen);

            // ROCK OCCLUSION CAPTURE: Place a rock obstacle directly on the default 0-degree face-inspect camera spot.
            _body.EvaluateScreenPose(0f, 10f, out Vector3 screenPos, out Quaternion screenRot);
            FaceInspectComposer.ComposePose(screenPos, screenRot, -0.01f, 0.67f, out _, out Vector3 defaultEye);

            _rockObstacle = CreateRockObstacle(defaultEye);
            Physics.SyncTransforms();
            yield return null;

            yield return TapTab();
            yield return WaitSeconds(0.90f);

            Assert.IsTrue(faceScreen.IsOpen);
            Assert.IsTrue(_cameraRig.TryGetFaceInspectShot(out FaceInspectShot occlusionShot));
            Assert.AreNotEqual(FaceInspectProbeChoice.Default, occlusionShot.Choice,
                "When a rock blocks the default 0-degree spot, FaceInspectComposer must choose a clear side or closer angle.");
            Assert.IsFalse(
                Physics.CheckSphere(_view.transform.position, 0.08f, Layers.GroundMask | Layers.PropMask, QueryTriggerInteraction.Ignore),
                "The face-inspect camera must never be placed inside rock or terrain geometry.");
            Capture("face-inv-rock-occlusion");

            yield return TapTab();
            yield return WaitSeconds(0.60f);

            Object.DestroyImmediate(_rockObstacle);
            _rockObstacle = null;
            Physics.SyncTransforms();
            yield return null;

            // 15-DEGREE SLOPE + REAL PLAYER INPUT VERIFICATION (Keyboard, Mouse, Gamepad; zero direct UI method calls):
            Quaternion slopeRot = Quaternion.Euler(0f, earthBearing, 15f);
            Vector3 slopeOrigin = openSpot + new Vector3(0f, 0.45f, 0f);
            _slopeSurface = CreateSlopeCollider(slopeOrigin, slopeRot);
            Physics.SyncTransforms();
            _context.Get<IRoverPlacement>().PlaceAt(slopeOrigin, slopeRot);
            _cameraRig.Snap();
            yield return WaitSeconds(0.60f);

            float roverRollDeg = Mathf.DeltaAngle(0f, _rover.Rotation.eulerAngles.z);
            Assert.Greater(Mathf.Abs(roverRollDeg), 11f, "07 must be parked on a ~15-degree roll slope.");
            Quaternion closedSlopeScreenRot = _body.ScreenTransform.rotation;
            Assert.AreEqual(CursorLockMode.Locked, _uiSystem.Cursor.LockState, "Before opening inventory, drive cursor is Locked.");
            Assert.IsFalse(_uiSystem.Cursor.IsVisible, "Before opening inventory, drive cursor is hidden.");

            // Open with Tab on the 15-degree slope.
            yield return TapTab();
            float slopeOpenDeadline = Time.time + 3.0f;
            while (faceState.ScreenContentMix < 0.99f || faceState.ScreenVerticalPinch > 0.01f || _cameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, slopeOpenDeadline, "Face inventory must open on the 15-degree slope.");
                yield return null;
            }

            yield return null;
            Assert.IsTrue(faceScreen.IsOpen);
            Assert.AreEqual(CursorLockMode.Confined, _uiSystem.Cursor.LockState, "Cursor must be Confined while inventory is open.");
            Assert.IsTrue(_uiSystem.Cursor.IsVisible, "Cursor must be visible while inventory is open.");
            Assert.AreEqual(0, view.FocusedHoldCell, "Focus must start on hold cell 0 on open without direct method calls.");

            Assert.IsTrue(_cameraRig.TryGetFaceInspectShot(out FaceInspectShot slopeShot));
            _body.EvaluateScreenPose(slopeShot.NeckYawOffset, 10f, out Vector3 slopeScreenPos, out Quaternion slopeScreenRot);

            // 1. Camera up and roll align with the Screen so the UI reads upright and square in the viewport.
            Vector3 screenUpWorld = (slopeScreenRot * Vector3.up).normalized;
            Assert.Less(Vector3.Angle(_view.transform.up, screenUpWorld), 0.5f,
                "Face-inspect camera up-vector must align with the Screen's up-vector on a 15-degree slope.");
            Assert.Less(Mathf.Abs(Vector3.Dot(_view.transform.right, screenUpWorld)), 0.01f,
                "Face-inspect camera must have zero roll relative to the Screen on a 15-degree slope.");

            // 2. 07 eases its head toward level (counter-roll) as it lifts its head.
            float closedHorizontalTilt = Mathf.Abs((closedSlopeScreenRot * Vector3.right).y);
            float openHorizontalTilt = Mathf.Abs((slopeScreenRot * Vector3.right).y);
            Assert.Less(openHorizontalTilt, closedHorizontalTilt - 0.05f,
                "07 must counter-roll its head toward the horizon when lifting its head for screen inspect.");

            // 3. Top edge of the Screen projects horizontally in the viewport and fills ~75% of viewport height with no clipping.
            Vector3 topLeftCorner = slopeScreenPos + slopeScreenRot * new Vector3(-0.18f, 0.16f, 0f);
            Vector3 topRightCorner = slopeScreenPos + slopeScreenRot * new Vector3(0.18f, 0.16f, 0f);
            Vector3 bottomLeftCorner = slopeScreenPos + slopeScreenRot * new Vector3(-0.18f, -0.16f, 0f);
            Vector3 topLeftVp = _view.WorldToViewportPoint(topLeftCorner);
            Vector3 topRightVp = _view.WorldToViewportPoint(topRightCorner);
            Vector3 bottomLeftVp = _view.WorldToViewportPoint(bottomLeftCorner);
            Assert.AreEqual(topLeftVp.y, topRightVp.y, 0.005f,
                "Top edge of 07's Screen must be level in viewport coordinates even on a 15-degree slope.");
            float viewportHeightFill = topLeftVp.y - bottomLeftVp.y;
            Assert.AreEqual(0.75f, viewportHeightFill, 0.08f,
                "07's Screen must still fill ~75% of viewport height on a 15-degree slope.");
            Assert.Greater(bottomLeftVp.y, 0.05f, "Screen bottom must not clip outside the viewport.");
            Assert.Less(topLeftVp.y, 0.95f, "Screen top must not clip outside the viewport.");

            CaptureScreenshot("face_inv_slope_upright");

            // 4. Gameplay input (driving W, camera look) is blocked while inventory is open.
            Vector3 heldPos = _rover.Position;
            Quaternion heldCamRot = _view.transform.rotation;
            Press(_keyboard.wKey);
            Set(_mouse.delta, new Vector2(45f, -30f));
            yield return null;
            yield return null;
            Release(_keyboard.wKey);
            Set(_mouse.delta, Vector2.zero);
            Assert.IsTrue(_rover.IsHeldStill, "Driving must remain blocked while inventory is open.");
            Assert.Less(Vector3.Distance(heldPos, _rover.Position), 0.02f, "07 must not drive while inventory is open.");
            Assert.Less(Quaternion.Angle(heldCamRot, _view.transform.rotation), 0.5f,
                "Camera look input must not rotate the face-inspect camera while inventory is open.");

            // 5. Hover a cell, drag a kit to a socket with Mouse, move with Keyboard/Gamepad, press Enter/A, and close with Esc.
            Vector2 cell1Panel = FaceInventoryView.GetHoldCellReferenceRect(1).center;
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(cell1Panel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 slopeCell1Screen));
            Set(_mouse.position, slopeCell1Screen);
            yield return null;
            yield return null;
            Assert.IsTrue(view.IsTooltipVisible, "Mouse hover on the 15-degree sloped screen must show the cell tooltip.");

            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(kitCellPanel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 slopeKitScreen));
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(bellySocketPanel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 slopeBellyScreen));
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(cell0Panel, _view, faceScreen, FaceScreenMapper.DefaultPanelSize, out Vector2 slopeCell0Screen));

            Set(_mouse.position, slopeKitScreen);
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            Set(_mouse.position, slopeBellyScreen);
            yield return null;
            yield return null;
            Assert.IsTrue(view.IsDraggingKit, "Mouse kit drag must work on the 15-degree sloped screen.");
            Assert.IsTrue(view.IsSocketLeaderCyan(RoverSocket.Belly), "Dragging over Belly socket on slope must light its leader cyan.");

            Set(_mouse.position, slopeCell0Screen);
            yield return null;
            Release(_mouse.leftButton);
            yield return null;
            Assert.IsFalse(view.IsDraggingKit);
            Assert.AreEqual(0, view.FocusedHoldCell, "Releasing drag over hold cell 0 focuses cell 0.");

            // Move with Keyboard WASD + arrows and Gamepad D-pad, then press Enter and Gamepad A (buttonSouth).
            yield return TapKey(_keyboard.sKey);
            Assert.AreEqual(4, view.FocusedHoldCell, "WASD S must move focus down from cell 0 to cell 4.");
            yield return TapKey(_keyboard.enterKey);
            Assert.IsNotEmpty(view.ToastText,
                "Pressing Enter on a waiting kit away from the Rover Bay must trigger the fit-at-bay toast.");

            yield return TapButton(_gamepad.dpad.right);
            Assert.AreEqual(5, view.FocusedHoldCell, "Gamepad D-pad Right must move focus from cell 4 to cell 5.");
            yield return TapButton(_gamepad.buttonSouth);
            Assert.IsNotEmpty(view.ToastText,
                "Pressing Gamepad A (buttonSouth) on a waiting kit away from the Rover Bay must trigger the fit-at-bay toast.");

            // Close with Esc and verify previous cursor state and gameplay input are restored.
            yield return TapKey(_keyboard.escapeKey);
            Assert.IsFalse(faceScreen.IsOpen, "Pressing Esc must close the face inventory.");
            Assert.AreEqual(CursorLockMode.Locked, _uiSystem.Cursor.LockState, "Closing with Esc must restore the prior cursor lock state.");
            Assert.IsFalse(_uiSystem.Cursor.IsVisible, "Closing with Esc must restore the prior cursor visibility.");

            while (_cameraRig.FaceInspectWeight > 0.01f)
            {
                yield return null;
            }

            Assert.IsFalse(_rover.IsHeldStill, "Closing with Esc must restore gameplay driving input.");
        }

        private void PopulateHoldAndEquipment()
        {
            if (_context.TryGet(out ICargoHold hold))
            {
                MethodInfo addMethod = hold.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
                if (addMethod != null)
                {
                    addMethod.Invoke(hold, new object[] { SalvageMaterial.Metal, 5 });
                    addMethod.Invoke(hold, new object[] { SalvageMaterial.Wiring, 3 });
                    addMethod.Invoke(hold, new object[] { SalvageMaterial.Optics, 2 });
                }
            }

            if (_context.TryGet(out IRoverEquipment equipment))
            {
                MethodInfo restoreOwned = equipment.GetType().GetMethod("RestoreOwned", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo craft = equipment.GetType().GetMethod("Craft", BindingFlags.Instance | BindingFlags.Public);
                restoreOwned?.Invoke(equipment, new object[] { RoverKitPiece.TetherRing });
                restoreOwned?.Invoke(equipment, new object[] { RoverKitPiece.SolarCell });
                craft?.Invoke(equipment, new object[] { RoverKitPiece.LampBar });
                craft?.Invoke(equipment, new object[] { RoverKitPiece.HoverCoils });
            }
        }

        private GameObject CreateRockObstacle(Vector3 position)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "FaceInspectTestBoulder";
            rock.layer = Layers.Prop;
            // Offset slightly toward the right side of the default eye so the Left (-28 deg) probe is clear
            // while the rock remains visible on the right edge of the frame as visual proof of avoidance.
            Vector3 roverRight = _rover.Rotation * Vector3.right;
            rock.transform.position = position + roverRight * 0.18f - Vector3.up * 0.05f;
            rock.transform.rotation = Quaternion.Euler(18f, 34f, -12f);
            rock.transform.localScale = new Vector3(0.52f, 0.62f, 0.52f);
#if UNITY_EDITOR
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Generated/Art/R1/Materials/R1_LunarRock.mat");
            if (mat != null)
            {
                rock.GetComponent<Renderer>().sharedMaterial = mat;
            }
#endif
            return rock;
        }

        private static GameObject CreateSlopeCollider(Vector3 surfaceOrigin, Quaternion slopeRotation)
        {
            var slab = new GameObject("FaceInspectTestSlope15");
            slab.layer = Layers.Ground;
            slab.transform.rotation = slopeRotation;
            slab.transform.position = surfaceOrigin - (slopeRotation * Vector3.up) * 0.10f;
            var box = slab.AddComponent<BoxCollider>();
            box.size = new Vector3(6f, 0.20f, 6f);
            return slab;
        }

        private IEnumerator TapTab()
        {
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
        }

        private IEnumerator TapKey(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private IEnumerator TapButton(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        private IEnumerator BootRealScene()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("RoverFaceInventoryPlayModeTests loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
                _body = _body != null ? _body : root.GetComponentInChildren<RoverBodyLanguage>(true);
                _uiSystem = _uiSystem != null ? _uiSystem : root.GetComponentInChildren<UISystem>(true);
            }

            Assert.IsNotNull(bootstrap, "Main scene must contain GameBootstrap.");
            Assert.IsNotNull(_rover, "Main scene must contain RoverController.");
            Assert.IsNotNull(_cameraRig, "Main scene must contain RoverCameraRig.");
            Assert.IsNotNull(_body, "Main scene must contain RoverBodyLanguage.");

            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _sunlight = _context.Get<ISunlight>();
            _faceDirector = _body.FaceDirector;
            _faceDisplay = _faceDirector != null ? _faceDirector.Display : null;

            _eventSubs.Add(_context.Events.Subscribe<RoverFaceCued>(c => _faceCues.Add(c.Kind)));
            _awokeSub = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float start = Time.time;
            while (!_awake || _body.Mood.Wake.Sleep > 0.05f)
            {
                Assert.Less(Time.time - start, WakeTimeout, "07 must finish waking in the real scene.");
                yield return null;
            }
        }

        private void Capture(string name)
        {
            string path = Path.Combine(CaptureFolder, name + ".png");
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, path);
            Assert.IsTrue(File.Exists(path), $"Capture must be written to {path}.");

            string r2Path = Path.Combine(R2CaptureFolder, name + ".png");
            File.Copy(path, r2Path, true);
            Assert.IsTrue(File.Exists(r2Path), $"Capture must be written to {r2Path}.");
        }

        private void CaptureScreenshot(string name)
        {
            string path = Path.Combine(ScreenshotsFolder, name + ".png");
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, path);
            Assert.IsTrue(File.Exists(path), $"Screenshot must be written to {path}.");
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}
