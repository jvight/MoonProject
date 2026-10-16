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
    /// collapses to line and returns with a happy <c>^ ^</c> blink), verifies screen UV raycasting, sound hooks,
    /// night vs. day lighting, and rock occlusion avoidance, and writes all required PNGs to <c>Logs/captures/</c>.
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

        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private RoverBodyLanguage _body;
        private RoverFaceDirector _faceDirector;
        private FaceDisplay _faceDisplay;
        private ISunlight _sunlight;
        private Camera _view;
        private Keyboard _keyboard;
        private IDisposable _awokeSub;
        private readonly List<IDisposable> _eventSubs = new List<IDisposable>();
        private GameObject _rockObstacle;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _awake = false;
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

            if (_rockObstacle != null)
            {
                Object.DestroyImmediate(_rockObstacle);
                _rockObstacle = null;
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
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();

            yield return BootRealScene();

            Assert.IsNotNull(_faceDirector, "RoverFaceDirector must be wired in the real scene.");
            Assert.IsNotNull(_faceDisplay, "FaceDisplay must be wired on Screen in the real scene.");

            IRoverFaceScreen faceScreen = _context.Get<IRoverFaceScreen>();
            Assert.IsNotNull(faceScreen, "IRoverFaceScreen must be registered in GameContext by RoverController.");
            Assert.IsFalse(faceScreen.IsOpen, "Face screen must start closed.");

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

            // BEAT 5: Line Expands into Inventory (and Night Open capture)
            Capture("face-inv-beat-5-inventory-expand");
            Capture("face-inv-night-open");

            // BEAT 6: Close Returns with Happy Blink (^ ^)
            int happyBlinksBefore = faceState.ClosingHappyBlinkCount;
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);

            Assert.IsFalse(faceScreen.IsOpen, "Pressing Tab again must close IRoverFaceScreen.");
            CollectionAssert.Contains(uiCues, UiCueKind.Back, "Closing Tab must publish UiCue(UiCueKind.Back).");

            // Step through the 0.14s collapse + 0.16s expand until the happy arc eyes (^ ^) open after the blink.
            while (faceState.ClosingHappyBlinkCount <= happyBlinksBefore || faceState.ScreenVerticalPinch > 0.08f || faceState.BlinkMultiplier < 0.75f)
            {
                yield return null;
            }

            Assert.AreEqual(0f, faceState.ScreenContentMix, 1e-4f, "Beat 6: screen must have returned to eye mode.");
            Assert.Greater(faceState.LeftEye.Arc, 0.5f, "Beat 6: closing must show happy curved arc eyes (^ ^).");
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
            Capture("face-inv-day-open");

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

        private IEnumerator TapTab()
        {
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
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
