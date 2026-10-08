using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    /// Pillar 6 in the real game (M3-10): boots the built Main scene on its own save slot and, leaving 07 alone,
    /// renders the game camera as the lonely wide shot settles at the base, mid-basin looking home and in the canyon,
    /// three frames of the hand-back as 07 drives off, and the dust motes in 07's lamp (as a player orbiting round 07
    /// sees them, and up close; base and canyon) to Logs/rover-captures/*.png, with lonely.md recording each frame's
    /// distance to 07, 07's place on screen and the horizon. Game time advances a fixed 1/60 s per frame, so
    /// slow captures never skip game time. 07 is moved between places through <see cref="IRoverPlacement"/>, held
    /// parked meanwhile. Slow and needs a GPU: run on demand with --category RoverLonelySession.
    /// </summary>
    [Explicit("Slow real-game capture session; run on demand with --category RoverLonelySession.")]
    [Category("RoverLonelySession")]
    public sealed class RoverLonelySession
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 3f;

        /// <summary>Seconds after 07 starts to drive at which the hand-back frames are taken.</summary>
        private static readonly float[] HandBackFrames = { 0.15f, 0.45f, 0.9f };

        private const float HandBackThrottle = 0.6f;

        /// <summary>Mid-basin: this far from the base, on the far side from Earth, looking home.</summary>
        private const float MidBasinDistance = 160f;

        /// <summary>07 faces this many degrees off Earth's bearing there, so the gentle turn shows.</summary>
        private const float MidBasinHeadingOffset = 25f;

        /// <summary>In the canyon: this far into it from the landing apron, along its way.</summary>
        private const float CanyonInset = 12f;

        /// <summary>Game time per frame during the session (s).</summary>
        private const float FrameTime = 1f / 60f;

        /// <summary>The player orbiting round 07: bearing from its heading (deg), distance, elevation.</summary>
        private const float OrbitBearing = 145f;
        private const float OrbitDistance = 6f;
        private const float OrbitElevation = 10f;

        /// <summary>The motes close-up: a camera off to the side ahead of the lamp, looking across its beam.</summary>
        private const float MoteSide = 2.4f;
        private const float MoteAhead = 0.9f;
        private const float MoteRise = 0.45f;
        private const float MoteLookAhead = 1.3f;
        private const float MoteLookRise = 0.25f;
        private const float MoteFov = 40f;
        private const float MoteSettleSeconds = 8f;

        /// <summary>Height (m) of the middle of 07's body above its ground contact.</summary>
        private const float BodyHeight = 0.8f;

        private const float HorizonReach = 1000f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-captures"));

        private readonly List<string> _problems = new List<string>();
        private readonly object _hold = new object();
        private readonly StringBuilder _report = new StringBuilder();
        private IDisposable _awoke;
        private bool _awake;
        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;

        [SetUp]
        public void SetUp()
        {
            Application.logMessageReceived += OnLog;
            Time.captureDeltaTime = FrameTime;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Application.logMessageReceived -= OnLog;
            _awoke?.Dispose();
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
        [Timeout(900000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator LonelyWideShots_AndLampMotes_AreCapturedInTheRealGame()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The rover session loads the scene through the editor.");
#endif
            FindSystems();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            IRoverRig rig = _context.Get<IRoverRig>();
            rig.SetHoldStill(_hold, true);
            yield return Wait(SettleSeconds);
            _report.AppendLine("# The lonely wide shot (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Camera to 07 (m) | 07 on screen (x, y) | Horizon y | Wide frame |");
            _report.AppendLine("|---|---:|---|---:|---|");
            Capture("base-chase");

            rig.SetHoldStill(_hold, false);
            yield return WaitForWideShot();
            Capture("base-wide");

            var drive = new ScriptedDrive();
            _rover.SetDriveSource(drive);
            drive.Drive = new Vector2(0f, HandBackThrottle);
            float driving = Time.time;
            for (int i = 0; i < HandBackFrames.Length; i++)
            {
                while (Time.time < driving + HandBackFrames[i])
                {
                    yield return null;
                }

                Capture($"base-handback-{i + 1}");
            }

            drive.Drive = Vector2.zero;
            rig.SetHoldStill(_hold, true);
            yield return Wait(MoteSettleSeconds);
            Capture("base-motes-chase");
            CaptureMotes("base-motes");

            ITerrainQuery terrain = _context.Get<ITerrainQuery>();
            IWorldLayout layout = _context.Get<IWorldLayout>();
            float earth = WideShotComposer.Bearing(layout.EarthDirection);
            Vector3 midBasin = layout.BasePosition - WideShotComposer.Direction(earth) * MidBasinDistance;
            Assert.IsTrue(terrain.IsDrivable(midBasin.x, midBasin.z), "Mid-basin spot is open ground.");
            yield return MoveTo(midBasin, earth + MidBasinHeadingOffset);
            rig.SetHoldStill(_hold, false);
            yield return WaitForWideShot();
            Capture("midbasin-wide");

            rig.SetHoldStill(_hold, true);
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing),
                "The world has the canyon's landing apron.");
            Vector3 canyon = landing.Position + landing.Forward * CanyonInset;
            Assert.IsTrue(terrain.IsDrivable(canyon.x, canyon.z), "The canyon spot is open ground.");
            yield return MoveTo(canyon, WideShotComposer.Bearing(landing.Forward));
            rig.SetHoldStill(_hold, false);
            yield return WaitForWideShot();
            Capture("canyon-wide");
            rig.SetHoldStill(_hold, true);
            yield return Wait(MoteSettleSeconds);
            CaptureMotes("canyon-motes");
            rig.SetHoldStill(_hold, false);

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "lonely.md"), _report.ToString());
            Debug.Log("[rover-lonely] " + _report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private void FindSystems()
        {
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
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        /// <summary>Waits for the frame to open and settle, plus a moment of its breathing.</summary>
        private IEnumerator WaitForWideShot()
        {
            WideShot wide = _cameraRig.WideShot;
            float timeout = Time.time + 60f;
            while (!(wide.IsOpen && wide.Weight > 0.97f))
            {
                Assert.Less(Time.time, timeout, "The wide shot opens and settles.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
        }

        /// <summary>
        /// Sets 07 down at <paramref name="ground"/> facing <paramref name="yaw"/> through
        /// <see cref="IRoverPlacement"/> (held parked, so the wide shot stays closed), then lets the view settle.
        /// </summary>
        private IEnumerator MoveTo(Vector3 ground, float yaw)
        {
            yield return Wait(1f);
            _context.Get<IRoverPlacement>().PlaceAt(ground, Quaternion.Euler(0f, yaw, 0f));
            yield return Wait(SettleSeconds);
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            Transform camera = _view.transform;
            Vector3 body = _rover.Position + Vector3.up * BodyHeight;
            Vector3 onScreen = _view.WorldToViewportPoint(body);
            Vector3 level = camera.position
                + Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized * HorizonReach;
            float horizon = _view.WorldToViewportPoint(level).y;
            WideShot wide = _cameraRig.WideShot;
            string frame = wide.Weight > 0f
                ? string.Format(CultureInfo.InvariantCulture,
                    "yaw {0:0}, elev {1:0.0}, {2:0.0} m, screen y {3:0.00} (weight {4:0.00})", wide.Frame.Yaw,
                    wide.Frame.Elevation, wide.Frame.Distance, wide.Frame.ScreenY, wide.Weight)
                : "chase";
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| {0} | {1:0.0} | {2:0.00}, {3:0.00} | {4:0.00} | {5} |", name,
                Vector3.Distance(camera.position, body), onScreen.x, onScreen.y, horizon, frame));
        }

        /// <summary>
        /// The motes in 07's lamp, with the game camera's lens and post-processing: as a player who orbits round to
        /// 07's face sees them (the game's field of view), and up close across the beam.
        /// </summary>
        private void CaptureMotes(string name)
        {
            RoverLampMotes motes = _rover.GetComponentInChildren<RoverLampMotes>(true);
            Transform lamp = motes.transform;
            Vector3 ahead = Vector3.ProjectOnPlane(lamp.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, ahead);
            Vector3 beam = lamp.position + ahead * MoteLookAhead + Vector3.up * MoteLookRise;

            float heading = WideShotComposer.Bearing(ahead) + OrbitBearing;
            float elevation = OrbitElevation * Mathf.Deg2Rad;
            Vector3 orbit = beam + WideShotComposer.Direction(heading) * (OrbitDistance * Mathf.Cos(elevation))
                + Vector3.up * (OrbitDistance * Mathf.Sin(elevation));
            RenderReview(name + "-orbit", orbit, beam, _view.fieldOfView);

            Vector3 close = lamp.position + right * MoteSide + ahead * MoteAhead + Vector3.up * MoteRise;
            RenderReview(name + "-close", close, beam, MoteFov);

            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| {0} | orbit + close-up | {1} motes alive | | visibility {2:0.00} |", name,
                motes.System.particleCount, motes.Visibility));
        }

        private void RenderReview(string name, Vector3 eye, Vector3 look, float fieldOfView)
        {
            var host = new GameObject("MoteReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(_view);
                camera.enabled = false;
                UniversalAdditionalCameraData source = _view.GetUniversalAdditionalCameraData();
                UniversalAdditionalCameraData target = camera.GetUniversalAdditionalCameraData();
                target.renderPostProcessing = source.renderPostProcessing;
                target.antialiasing = source.antialiasing;
                target.antialiasingQuality = source.antialiasingQuality;
                target.renderShadows = source.renderShadows;
                target.dithering = source.dithering;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                camera.fieldOfView = fieldOfView;
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }
    }
}
