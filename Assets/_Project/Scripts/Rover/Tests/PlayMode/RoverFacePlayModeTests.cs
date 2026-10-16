using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// PlayMode verification and real-game capture suite for 07's CRT face and rear vent echo (0.5.7).
    /// Boots the real Main scene via <see cref="RoverSessionScene"/>, exercises the feeling sequence
    /// (wake boot -> Calm -> Worried on low battery -> Asleep while charging -> wake blink to Calm ->
    /// Curious while scanning -> Happy on fragment scan -> rear vent echo), and writes day and night
    /// chase-camera captures to <c>Logs/captures/</c>.
    /// </summary>
    public sealed class RoverFacePlayModeTests
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float SunriseTimeout = 35f;
        private const float FrontChaseYawOffset = 172f;
        private const float OpenGroundOffset = 10f;

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private readonly object _holdOwner = new object();
        private readonly object _gazeOwner = new object();
        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private RoverBodyLanguage _body;
        private RoverFaceDirector _faceDirector;
        private FaceDisplay _faceDisplay;
        private ISunlight _sunlight;
        private Camera _view;
        private IDisposable _awokeSub;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
            _awake = false;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _awokeSub?.Dispose();
            _awokeSub = null;

            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator RealScene_SimulatesLowBatteryChargingAndFragmentScan_AndCapturesNightAndDay()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return BootRealScene();

            Assert.IsNotNull(_faceDirector, "RoverFaceDirector must be wired in the real scene.");
            Assert.IsNotNull(_faceDisplay, "FaceDisplay must be wired on Screen in the real scene.");
            Assert.IsNotNull(_faceDirector.Director, "FaceDirector logic instance must be initialized.");
            Assert.IsNotNull(_faceDirector.RearVentRenderer, "RearVentStrip renderer must be wired.");
            Assert.IsTrue(_faceDirector.Director.HasBootedOnWake, "Fresh game start must trigger the CRT boot sequence.");
            Assert.GreaterOrEqual(_faceDirector.Director.BootCount, 1);

            // Place 07 on open ground near home facing Earth so the dock does not auto-charge 07
            // and 07's head looks straight ahead toward the front-orbited chase camera.
            IWorldLayout layout = _context.Get<IWorldLayout>();
            ITerrainQuery terrain = _context.Get<ITerrainQuery>();
            float earthBearing = WideShotComposer.Bearing(layout.EarthDirection);
            Vector3 forward = WideShotComposer.Direction(earthBearing);
            Vector3 openSpot = _rover.Position + forward * OpenGroundOffset;
            openSpot.y = terrain.SampleHeight(openSpot.x, openSpot.z);

            _context.Get<IRoverRig>().SetHoldStill(_holdOwner, true);
            _context.Get<IRoverPlacement>().PlaceAt(openSpot, Quaternion.Euler(0f, earthBearing, 0f));
            _cameraRig.GentleCamera = true;
            yield return WaitSeconds(1.2f);

            // Verify we are in the opening night (eclipse hold active, no direct sunlight).
            Assert.IsTrue(_sunlight.IsHoldingEclipse, "Fresh session starts in the opening night eclipse.");
            Assert.Less(_sunlight.SunlightAt(_rover.Position), 0.05f);

            yield return RunAndCaptureSequence("night");

            // Release the opening eclipse and advance into full sunlit lunar day.
            _sunlight.ReleaseOpeningEclipse();
            float sunriseDeadline = Time.time + SunriseTimeout;
            while (_sunlight.SunlightAt(_rover.Position) < 0.95f)
            {
                Assert.Less(Time.time, sunriseDeadline, "Sun breaks out of eclipse into lunar day.");
                yield return null;
            }

            // Allow key light smoothing to settle into full daytime illumination.
            yield return WaitSeconds(2.5f);
            Assert.Greater(_sunlight.SunlightAt(_rover.Position), 0.95f);

            yield return RunAndCaptureSequence("day");

            _rover.Gaze.Clear(_gazeOwner);
            _context.Get<IRoverRig>().SetHoldStill(_holdOwner, false);
        }

        private IEnumerator RunAndCaptureSequence(string prefix)
        {
            FaceDirector director = _faceDirector.Director;
            FaceState state = director.State;

            // Orbit the chase camera around to the front of 07 at chase distance.
            OrbitChaseCameraToFront();
            PointRoverGazeAtCamera();
            yield return WaitSeconds(1.0f);

            // 1. Wake boot sequence -> Calm
            _context.Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle, false));
            Assert.IsTrue(state.IsBooting, $"{prefix}: WakeBeat.Crackle must start Boot().");
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
            director.State.SetBootProgress(0.32f);
            _faceDisplay.ApplyToRenderer();
            Capture($"{prefix}-wake-boot");

            // Finish boot and settle in Calm
            while (state.IsBooting)
            {
                yield return null;
            }

            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling);
            float calmVentEmission = ReadRearVentEmission();

            // 2. Low battery -> Worried (with battery symbol)
            _context.Events.Publish(new BatteryWarned(BatteryWarning.Low, _rover.Position));
            director.SetBatteryCharge(0.18f);
            Assert.AreEqual(FaceFeeling.Worried, director.CurrentFeeling, $"{prefix}: low battery must select Worried.");
            yield return WaitSeconds(0.55f);
            Assert.AreEqual(FaceFeeling.Worried, director.CurrentFeeling);
            Assert.AreEqual(FaceSymbol.Battery, state.ActiveSymbol, $"{prefix}: Worried must show the Battery symbol.");
            Assert.Greater(state.SymbolAlpha, 0.7f);
            Capture($"{prefix}-worried-low-battery");

            // 3. Charging on -> Asleep (with zzz symbol, overriding low battery)
            _context.Events.Publish(new ChargingChanged(true, ChargerKind.Dock, _rover.Position));
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling, $"{prefix}: charging must override low battery with Asleep.");
            yield return WaitSeconds(0.55f);
            Assert.AreEqual(FaceFeeling.Asleep, director.CurrentFeeling);
            Assert.AreEqual(FaceSymbol.Zzz, state.ActiveSymbol, $"{prefix}: Asleep must show the Zzz symbol.");
            Assert.Greater(state.SymbolAlpha, 0.7f);
            float asleepVentEmission = ReadRearVentEmission();
            Assert.Less(asleepVentEmission, calmVentEmission, $"{prefix}: RearVentStrip must dim while Asleep.");
            Capture($"{prefix}-asleep-charging");

            // Charging off -> wake blink, then Calm
            int blinksBefore = director.WakeBlinkCount;
            _context.Events.Publish(new ChargingChanged(false, ChargerKind.Dock, _rover.Position));
            director.SetBatteryCharge(0.85f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling, $"{prefix}: unplugging from charger must return to Calm.");
            Assert.Greater(director.WakeBlinkCount, blinksBefore, $"{prefix}: unplugging from charger must trigger Blink().");
            yield return WaitSeconds(0.45f);

            // 4. Scanning -> Curious
            Vector3 roverForward = _rover.Rotation * Vector3.forward;
            Vector3 roverRight = _rover.Rotation * Vector3.right;
            Vector3 scanPoint = _rover.Position + roverForward * 6f + roverRight * 0.6f + Vector3.up * 0.9f;
            _context.Events.Publish(new ScanStarted("fragment.cradle.1", scanPoint));
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling, $"{prefix}: ScanStarted must select Curious.");
            yield return WaitSeconds(0.50f);
            Assert.AreEqual(FaceFeeling.Curious, director.CurrentFeeling);
            Capture($"{prefix}-curious-scanning");

            // 5. Fragment scanned -> Happy beat
            _context.Events.Publish(new FragmentScanned("fragment.cradle.1", "rover.cargo_cradle", 0, 3, false, 0));
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling, $"{prefix}: FragmentScanned must select Happy.");
            yield return WaitSeconds(0.45f);
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);
            float happyVentEmission = ReadRearVentEmission();
            Assert.Greater(happyVentEmission, calmVentEmission, $"{prefix}: RearVentStrip must brighten on Happy.");
            Capture($"{prefix}-happy-fragment");

            // 6. Rear vent echo seen from behind in the chase camera
            _rover.Gaze.Clear(_gazeOwner);
            OrbitChaseCameraBehind();
            yield return WaitSeconds(0.85f);
            _context.Events.Publish(new FragmentScanned("fragment.cradle.2", "rover.cargo_cradle", 1, 3, false, 0));
            yield return WaitSeconds(0.20f);
            Assert.AreEqual(FaceFeeling.Happy, director.CurrentFeeling);
            Assert.Greater(ReadRearVentEmission(), calmVentEmission);
            Capture($"{prefix}-rear-vent-echo");

            // Wait for the short Happy beat to finish and verify it returns to Calm
            yield return WaitSeconds(_faceDirector.Tuning.FragmentHappyHoldSeconds + 0.25f);
            Assert.AreEqual(FaceFeeling.Calm, director.CurrentFeeling, $"{prefix}: Happy beat must return to Calm after its hold.");
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
            throw new NotSupportedException("RoverFacePlayModeTests loads the scene through the editor.");
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

        private void OrbitChaseCameraToFront()
        {
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(_rover.Heading + FrontChaseYawOffset);
        }

        private void OrbitChaseCameraBehind()
        {
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(_rover.Heading);
        }

        private void PointRoverGazeAtCamera()
        {
            Vector3 lookTarget = _rover.Position + (_rover.Rotation * Vector3.forward) * 8f + Vector3.up * 1.15f;
            _rover.Gaze.Set(_gazeOwner, lookTarget, priority: 100);
        }

        private float ReadRearVentEmission()
        {
            var block = new MaterialPropertyBlock();
            _faceDirector.RearVentRenderer.GetPropertyBlock(block);
            return block.GetFloat(EmissionStrengthId);
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
