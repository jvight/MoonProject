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
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Does home carry across the basin while 07 is really out there (M3-10, pillar 6)? Boots the built Main scene on
    /// the playthrough's slot, seeded like the world's atmosphere session (Bell repaired, home and welcomed), then
    /// drives 07 out to the far base poses of World/Editor/Captures/atmosphere_views.json. At each, 07 stops a few
    /// metres in front of the pose's eye facing home and the base settles to how it looks with 07 that far away; the
    /// pose is rendered from a review camera with the game camera's lens and post-processing, and the game camera's
    /// own frame behind 07 is saved too (Logs/gameplay-captures/home-*.png, report home-carry.md with the lamps', the
    /// windows' and the halo's levels). Any error in the log fails it. Slow and needs a GPU: run on demand with
    /// --category HomeCarrySession.
    /// </summary>
    [Explicit("Slow look session in the real Main scene; run on demand with --category HomeCarrySession.")]
    [Category("HomeCarrySession")]
    public sealed class HomeCarrySession
    {
        private const string PosesPath = "Assets/_Project/Scripts/World/Editor/Captures/atmosphere_views.json";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 4f;

        /// <summary>The base eases its lights over a few seconds (BaseTuning warm ease); wait well past it.</summary>
        private const float BaseSettleSeconds = 9f;

        /// <summary>07 stops this far (m) in front of a pose's eye, on its line to the base.</summary>
        private const float StandAhead = 9f;

        /// <summary>...after first driving this far (m) past the eye, so it arrives facing home.</summary>
        private const float RunPast = 14f;

        private const float ArriveRadius = 2.5f;
        private const float DriveTimeout = 120f;
        private const float StopSpeed = 0.4f;
        private const float StopTimeout = 8f;

        private static readonly string[] FarPoses = { "base_from_150m", "base_from_300m" };

        // Bell (FriendState.Awake = 3, every part and item held) home at her corner, welcomed: the same save the
        // world's atmosphere session seeds, in Core's envelope and Gameplay's friends section format (version 2).
        private const string BellHomeSave =
            "{\"formatVersion\":1,\"savedAtUtc\":\"2026-10-08T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.friends\"," +
            "\"version\":2,\"json\":\"{\\\"friends\\\":[{\\\"id\\\":\\\"bell\\\",\\\"state\\\":3," +
            "\\\"parts\\\":15,\\\"items\\\":15,\\\"discovered\\\":true,\\\"welcomed\\\":true}]}\"}]}";

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private IDisposable _awoke;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator Home_IsCapturedFromAcrossTheBasin_With07OutThere()
        {
            yield return Boot();
            PoseFile file = JsonUtility.FromJson<PoseFile>(File.ReadAllText(PosesPath));
            var report = new StringBuilder();
            report.AppendLine("# Home from across the basin, 07 out there (real Main scene)");
            report.AppendLine();
            report.AppendLine(
                "| Pose | 07 from the lander (m) | Lamp warmth | Window glow | Halo glow | Halo radius (m) |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (string name in FarPoses)
            {
                Pose pose = Find(file, name);
                var eye = new Vector3(pose.position[0], pose.position[1], pose.position[2]);
                var look = new Vector3(pose.lookAt[0], pose.lookAt[1], pose.lookAt[2]);
                Vector3 towardHome = look - eye;
                towardHome.y = 0f;
                towardHome.Normalize();
                yield return DriveTo(eye - towardHome * RunPast, "past the eye of " + name);
                yield return DriveTo(eye + towardHome * StandAhead, "the stand of " + name);
                yield return new WaitForSeconds(BaseSettleSeconds);

                HomeBase home = _gameplay.Home;
                float distance = SurfaceRules.HorizontalDistance(_rover.Position, home.LanderPosition);
                Review(eye, look, pose.fov, "home-away-" + name);
                float halo = home.Halo.Level;
                float radius = home.Halo.Radius;
                FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight,
                    Path.Combine(GameplayFixture.CaptureFolder, "home-away-" + name + "-game.png"));
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:F0} | {2:F2} | {3:F2} | {4:F2} | {5:F1} |", name, distance, home.Warmth,
                    home.WindowLevel, halo, radius));
                Assert.Greater(halo, 0f, $"home glows as a warm point seen from {name}");
            }

            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(Path.Combine(GameplayFixture.CaptureFolder, "home-carry.md"), report.ToString());
            Debug.Log("[home-carry] " + report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), BellHomeSave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The home carry session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            _rover = (RoverController)_context.Get<IRoverRig>();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);
            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
        }

        private IEnumerator DriveTo(Vector3 target, string what)
        {
            _pilot.GoTo(target, ArriveRadius, 1f);
            float deadline = Time.time + DriveTimeout;
            while (!_pilot.Arrived && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_pilot.Arrived, $"07 reaches {what} within {DriveTimeout:F0} s");
            _pilot.Target = null;
            deadline = Time.time + StopTimeout;
            while (_rover.Speed > StopSpeed && Time.time < deadline)
            {
                yield return null;
            }
        }

        /// <summary>Renders the pose from a second camera with the game camera's lens and post-processing.</summary>
        private void Review(Vector3 eye, Vector3 look, float fov, string name)
        {
            Camera view = _context.Get<IViewCamera>().Camera;
            var host = new GameObject("HomeCarryReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                UniversalAdditionalCameraData source = view.GetUniversalAdditionalCameraData();
                UniversalAdditionalCameraData target = camera.GetUniversalAdditionalCameraData();
                target.renderPostProcessing = source.renderPostProcessing;
                target.antialiasing = source.antialiasing;
                target.antialiasingQuality = source.antialiasingQuality;
                target.renderShadows = source.renderShadows;
                target.dithering = source.dithering;
                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
                camera.fieldOfView = fov;
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                    Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private static Pose Find(PoseFile file, string name)
        {
            foreach (Pose pose in file.poses)
            {
                if (pose.name == name)
                {
                    Assert.AreEqual(3, pose.lookAt.Length, $"pose '{name}' looks at a point");
                    return pose;
                }
            }

            Assert.Fail($"{PosesPath} has no pose '{name}'");
            return null;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }

        [Serializable]
        private sealed class PoseFile
        {
            public Pose[] poses = Array.Empty<Pose>();
        }

        [Serializable]
        private sealed class Pose
        {
            public string name = string.Empty;
            public float[] position = Array.Empty<float>();
            public float[] lookAt = Array.Empty<float>();
            public float fov = 60f;
        }
    }
}
