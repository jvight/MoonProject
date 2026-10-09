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
    /// beat. Drive input then ends the beat. Slow and needs a GPU: run on demand with --category RoverStargazeCaptures.
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

        private IEnumerator Boot()
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

            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return Wait(SettleSeconds);
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
