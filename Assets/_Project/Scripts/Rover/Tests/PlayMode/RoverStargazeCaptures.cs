using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
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
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07 gazing at the stars in the real Main scene (M3-15 §5): at an open spot in the basin and beside the lander,
    /// the player tips the view up with the gamepad until the camera sits under 07 looking at the sky, then rests.
    /// Frames before, while 07 lifts its head, and once the stargazing beat began go to Logs/rover-captures/*.png, with
    /// stargaze.md recording the orbit elevation, the camera's height above the dust, 07's share of the gaze and the
    /// beat. Drive input then ends the beat. The dock's evening (M3-16 P6) is caught the same way: 07 parked on its
    /// dock, the wide shot of home's lamps, its ease back, and drive input ending it early (evening-*.png).
    /// Slow and needs a GPU: run on demand with --category RoverStargazeCaptures.
    /// </summary>
    [Explicit("Slow real-game capture session; run on demand with --category RoverStargazeCaptures.")]
    [Category("RoverStargazeCaptures")]
    public sealed class RoverStargazeCaptures : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 2f;

        /// <summary>The open basin: this far from the base, on the far side from Earth.</summary>
        private const float BasinDistance = 160f;

        /// <summary>Beside the lander: this far back from its charging dock, turned this far off it.</summary>
        private const float LanderBack = 6f;
        private const float LanderSide = 3f;

        /// <summary>Orbit elevation (deg) the view is tipped to: the tuning's full stargaze elevation.</summary>
        private const float LookUpElevation = -20f;

        private const float LookStick = 0.8f;

        /// <summary>How long the stick is tried one way to learn which way tips the view up.</summary>
        private const float StickProbeSeconds = 0.3f;
        private const float LookTimeout = 6f;
        private const float BeatTimeout = 8f;
        private const float LiftFrameSeconds = 1.5f;
        private const float DriveStick = 0.6f;
        private const float EndTimeout = 1f;
        private const float MinCameraClearance = 0.2f;
        private const string DockAnchorName = "DockAnchor";

        /// <summary>The wake opening: the gaze share the lens frame waits for, waits, and the mid-ease frame.</summary>
        private const float WakeLensLift = 0.6f;
        private const float WakeBeatTimeout = 12f;
        private const float WakeEarthSettleSeconds = 1f;
        private const float UpperThird = 2f / 3f;
        private const float WakeEaseFrameSeconds = 1.8f;
        private const float WakeEaseTimeout = 8f;

        /// <summary>The dock's evening: seconds to the settled frame, the frame mid-ease back, and timeouts.</summary>
        private const float EveningSettleSeconds = 5f;
        private const float EveningSettled = 0.9f;
        private const float EveningBackFrameSeconds = 1f;
        private const float EveningEndTimeout = 20f;
        private const float EveningEarlySeconds = 2f;

        /// <summary>How far (m) along Earth's direction a point is projected to find Earth on screen.</summary>
        private const float EarthProbeDistance = 1000f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-captures"));

        private readonly StringBuilder _report = new StringBuilder();
        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private ITerrainQuery _terrain;
        private Gamepad _gamepad;
        private bool _awake;
        private bool _stargazing;
        private IDisposable[] _subscriptions;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_subscriptions != null)
            {
                foreach (IDisposable subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
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
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator LookingUp_07GazesAtTheStars_InTheBasinAndBesideTheLander()
        {
            yield return Boot();
            _report.AppendLine("# Stargazing (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");

            IWorldLayout layout = _context.Get<IWorldLayout>();
            float earth = WideShotComposer.Bearing(layout.EarthDirection);
            Vector3 basin = layout.BasePosition - WideShotComposer.Direction(earth) * BasinDistance;
            Assert.IsTrue(_terrain.IsDrivable(basin.x, basin.z), "The basin spot is open ground.");
            yield return Stargaze("stargaze-basin", basin, earth);

            Transform dock = FindNode(DockAnchorName);
            Assert.IsNotNull(dock, "The lander carries the charging dock's DockAnchor.");
            Vector3 lander = dock.position - dock.forward * LanderBack + dock.right * LanderSide;
            yield return Stargaze("stargaze-lander", lander, dock.eulerAngles.y);

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "stargaze.md"), _report.ToString());
            Debug.Log("[rover-stargaze] " + _report);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator FreshGame_OpensOnTheLens_TurnsToEarth_ThenEasesToTheChase()
        {
            yield return Boot(true);
            _report.AppendLine("# The wake opening (real Main scene, fresh game)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            WakeOpening opening = _cameraRig.Opening;
            Transform lens = _context.Get<IRoverRig>().TetherOrigin;
            Vector3 earth = _context.Get<IWorldLayout>().EarthDirection;
            Assert.IsTrue(opening.IsActive && !opening.IsEasing, "A fresh game opens on 07's lens.");
            float timeout = Time.time + WakeBeatTimeout;
            while (_cameraRig.SkyLift < WakeLensLift)
            {
                Assert.Less(Time.time, timeout, "07 looks up at the stars.");
                yield return null;
            }

            Assert.Less(opening.Turn, 0.01f, "Still beat 1, on the lens.");
            Capture("wake-1-lens");
            Vector3 eye = _view.transform.position;
            Vector3 lensOnScreen = _view.WorldToViewportPoint(lens.position);
            float facing = Vector3.Dot(lens.forward, (eye - lens.position).normalized);
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "\nwake-1: lens on screen ({0:0.00}, {1:0.00}), lens facing the camera {2:0.00}", lensOnScreen.x,
                lensOnScreen.y, facing));
            Assert.IsTrue(InFrame(lensOnScreen, 0f, 1f) && facing > 0f,
                $"Beat 1 sees the lens: on screen {lensOnScreen}, facing {facing:0.00}.");

            timeout = Time.time + WakeBeatTimeout;
            while (opening.Turn < 1f)
            {
                Assert.Less(Time.time, timeout, "The camera turns round to Earth.");
                yield return null;
            }

            yield return Wait(WakeEarthSettleSeconds);
            Assert.IsFalse(opening.IsEasing, "Still holding before any input.");
            Capture("wake-2-earth");
            eye = _view.transform.position;
            Vector3 earthOnScreen = _view.WorldToViewportPoint(eye + earth * EarthProbeDistance);
            lensOnScreen = _view.WorldToViewportPoint(lens.position);
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "\nwake-2: Earth on screen ({0:0.00}, {1:0.00}), lens ({2:0.00}, {3:0.00})", earthOnScreen.x,
                earthOnScreen.y, lensOnScreen.x, lensOnScreen.y));
            Assert.IsTrue(InFrame(earthOnScreen, UpperThird, 1f),
                $"Beat 2 frames Earth in the upper third: on screen {earthOnScreen}.");
            Assert.Less(lensOnScreen.y, earthOnScreen.y, "07's head sits below Earth.");

            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            Assert.IsTrue(opening.IsEasing, "Any input starts the ease to the chase frame.");
            yield return Wait(WakeEaseFrameSeconds);
            Assert.That(opening.Weight, Is.InRange(0.05f, 0.95f), "Midway through the ease.");
            Capture("wake-3-easing");

            timeout = Time.time + WakeEaseTimeout;
            while (opening.IsActive)
            {
                Assert.Less(Time.time, timeout, "The opening hands over to the chase frame.");
                yield return null;
            }

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "wake.md"), _report.ToString());
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator DockEvening_EasesOutToHomesLamps_HoldsThenEasesBack()
        {
            yield return Boot();
            _report.AppendLine("# The dock's evening (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first (the evening comes after a trip).");
                yield return null;
            }

            Transform dock = FindNode(DockAnchorName);
            Assert.IsNotNull(dock, "The lander carries the charging dock's DockAnchor.");
            _context.Get<IRoverPlacement>().PlaceAt(dock.position, dock.rotation);
            yield return Wait(SettleSeconds);
            Capture("evening-0-parked");
            HeldShot held = _cameraRig.Held;
            _context.Events.Publish(new DockEveningOpened(dock.position));
            Assert.IsTrue(held.IsOpen, "The evening shot opens.");
            yield return Wait(EveningSettleSeconds);
            Assert.Greater(held.Weight, EveningSettled, "Settled on the wide shot of home.");
            Capture("evening-1-wide");
            WideShotFrame frame = held.Frame;
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "\nevening-1: frame yaw {0:0} deg, elevation {1:0.0} deg, distance {2:0.0} m; camera {3:0.0} m off",
                frame.Yaw, frame.Elevation, frame.Distance,
                Vector3.Distance(_view.transform.position, _rover.Position)));
            float timeout = Time.time + EveningEndTimeout;
            while (held.IsOpen)
            {
                Assert.Less(Time.time, timeout, "The hold runs out and the view eases back.");
                yield return null;
            }

            yield return Wait(EveningBackFrameSeconds);
            Assert.That(held.Weight, Is.InRange(0.05f, 0.95f), "Easing back, never a cut.");
            Capture("evening-2-easing-back");
            while (held.Weight > 0f)
            {
                Assert.Less(Time.time, timeout + EveningEndTimeout, "Back on the chase frame.");
                yield return null;
            }

            _context.Events.Publish(new DockEveningOpened(dock.position));
            yield return Wait(EveningEarlySeconds);
            Set(_gamepad.leftStick, new Vector2(0f, DriveStick));
            yield return null;
            yield return null;
            Assert.IsFalse(held.IsOpen, "Drive input ends the evening early.");
            Set(_gamepad.leftStick, Vector2.zero);
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "evening.md"), _report.ToString());
        }

        private IEnumerator Stargaze(string name, Vector3 ground, float yaw)
        {
            _context.Get<IRoverPlacement>().PlaceAt(ground, Quaternion.Euler(0f, yaw, 0f));
            yield return Wait(SettleSeconds);
            Capture(name + "-0-level");

            float target = LookUpElevation;
            float before = _cameraRig.Orbit.Elevation;
            Set(_gamepad.rightStick, new Vector2(0f, LookStick));
            yield return Wait(StickProbeSeconds);
            float sign = _cameraRig.Orbit.Elevation < before ? 1f : -1f;
            Set(_gamepad.rightStick, new Vector2(0f, LookStick * sign));
            float timeout = Time.time + LookTimeout;
            while (_cameraRig.Orbit.Elevation > target)
            {
                Assert.Less(Time.time, timeout, "The view tips up until the camera looks up from under 07.");
                yield return null;
            }

            Set(_gamepad.rightStick, Vector2.zero);
            yield return Wait(LiftFrameSeconds);
            Capture(name + "-1-lifting");

            timeout = Time.time + BeatTimeout;
            while (!_stargazing)
            {
                Assert.Less(Time.time, timeout, "The stargazing beat begins after a rest looking up.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
            Assert.Greater(_cameraRig.SkyLift, 0.9f, "07 has joined the gaze.");
            Capture(name + "-2-beat");

            Set(_gamepad.leftStick, new Vector2(0f, DriveStick));
            timeout = Time.time + EndTimeout;
            while (_stargazing)
            {
                Assert.Less(Time.time, timeout, "Drive input ends the beat.");
                yield return null;
            }

            Set(_gamepad.leftStick, Vector2.zero);
            yield return Wait(SettleSeconds);
        }

        /// <param name="freshGame">Publish the base waking from nothing, as Gameplay does loading a fresh game.</param>
        private IEnumerator Boot(bool freshGame = false)
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            _gamepad = InputSystem.AddDevice<Gamepad>();
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The stargaze session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_rover, "the scene has 07");
            Assert.IsNotNull(_cameraRig, "the scene has the camera rig");
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _terrain = _context.Get<ITerrainQuery>();
            _subscriptions = new[]
            {
                _context.Events.Subscribe<RoverAwoke>(_ => _awake = true),
                _context.Events.Subscribe<StargazingChanged>(changed => _stargazing = changed.IsStargazing),
            };
            if (freshGame)
            {
                _context.Events.Publish(new BasePowerChanged(BasePowerStage.Asleep, true));
            }

            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            if (!freshGame)
            {
                yield return Wait(SettleSeconds);
            }
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            Vector3 camera = _view.transform.position;
            float clearance = camera.y - _terrain.SampleHeight(camera.x, camera.z);
            Assert.Greater(clearance, MinCameraClearance, $"{name}: the camera stays out of the dust.");
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| {0} | {1:0.0} | {2:0.00} | {3:0.00} | {4} |", name, _cameraRig.Orbit.Elevation, clearance,
                _cameraRig.SkyLift, _stargazing ? "on" : "off"));
        }

        /// <summary>In front of the camera, inside the frame across, and between the heights given.</summary>
        private static bool InFrame(Vector3 viewport, float minY, float maxY)
        {
            return viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y >= minY && viewport.y < maxY;
        }

        private static Transform FindNode(string name)
        {
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}
