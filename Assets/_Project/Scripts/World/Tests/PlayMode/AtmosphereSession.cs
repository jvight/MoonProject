using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// The look of the real game against pillar 6 (M3-10): boots the built Main scene on its own save slot, seeded so
    /// that Bell is repaired and home at her corner (her dial is one of the warm points), lets 07 wake and the base
    /// warm up, then, for the game camera's own view at spawn, each pose of Editor/Captures/atmosphere_views.json, the
    /// lander up close (the owner's view) and two poses at Bell's corner, renders the game's own camera (same lens, post-processing and anti-aliasing) to Logs/world-captures/atmosphere-*.png
    /// and times <see cref="TimedFrames"/> 1920x1080 frames there, each waited for on the GPU, so the median is the
    /// full render cost of a frame (CPU submit and GPU, serialised). Writes atmosphere.md. Slow and needs a GPU: run
    /// on demand with --category AtmosphereSession.
    /// </summary>
    [Explicit("Slow look-and-cost session in the real Main scene; run on demand with --category AtmosphereSession.")]
    [Category("AtmosphereSession")]
    public sealed class AtmosphereSession
    {
        private const string PosesPath = "Assets/_Project/Scripts/World/Editor/Captures/atmosphere_views.json";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const int TimedWidth = 1920;
        private const int TimedHeight = 1080;
        private const int WarmUpFrames = 10;
        private const int TimedFrames = 60;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 4f;

        // The save seeded before boot: Bell (FriendState.Awake = 3, every part and item held) home at her corner,
        // in the envelope and section format of Core's SaveService and Gameplay's friends section (version 2).
        private const string BellHomeSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.4.1\",\"savedAtUtc\":\"2026-10-08T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.friends\"," +
            "\"version\":2,\"json\":\"{\\\"friends\\\":[{\\\"id\\\":\\\"bell\\\",\\\"state\\\":3," +
            "\\\"parts\\\":15,\\\"items\\\":15,\\\"discovered\\\":true,\\\"welcomed\\\":true}]}\"}]}";

        // Bell's corner on the radio tower, seen up close and as a warm point from 40 m.
        private const string BellCornerName = "BellCorner";
        private const float BellNearDistance = 4.5f;
        private const float BellNearSide = 1.8f;
        private const float BellNearHeight = 1.8f;
        private const float BellFarDistance = 40f;
        private const float BellFarHeight = 9f;
        private const float BellLookHeight = 0.8f;

        // The lander up close, the way the owner looks at the base (Art's lander_close, in the lander's frame).
        private const string LanderName = "Lander";
        private static readonly Vector3 LanderCloseEye = new Vector3(0.6f, 2.3f, 8.4f);
        private static readonly Vector3 LanderCloseLook = new Vector3(0f, 2.6f, 0f);
        private const float LanderCloseFov = 55f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "world-captures"));

        private readonly List<string> _problems = new List<string>();
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
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator Poses_AreCapturedAndTimed_InTheRealGame()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            string save = Path.Combine(SaveService.DefaultDirectory,
                CanyonSessionScene.SaveSlot + SaveService.Extension);
            File.WriteAllText(save, BellHomeSave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The atmosphere session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            Transform bellCorner = null;
            Transform lander = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    bellCorner = bellCorner != null || node.name != BellCornerName ? bellCorner : node;
                    lander = lander != null || node.name != LanderName ? lander : node;
                }
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            GameContext context = bootstrap.Context;
            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);

            Assert.IsNotNull(bellCorner, "the radio tower carries Bell's corner");
            Assert.IsNotNull(lander, "the base has its lander");
            Camera view = context.Get<IViewCamera>().Camera;
            PoseFile file = JsonUtility.FromJson<PoseFile>(File.ReadAllText(PosesPath));
            var poses = new List<Pose> { ViewPose(view.transform, view.fieldOfView) };
            poses.AddRange(file.poses);
            poses.Add(LookPose("lander_close", lander.TransformPoint(LanderCloseEye),
                lander.TransformPoint(LanderCloseLook), LanderCloseFov));
            poses.Add(BellPose(bellCorner, "bell_corner_near"));
            poses.Add(BellPose(bellCorner, null));
            var report = new StringBuilder();
            report.AppendLine("# Atmosphere session (real Main scene)");
            report.AppendLine();
            report.AppendLine($"Render frame time at {TimedWidth}x{TimedHeight}, {TimedFrames} frames per pose, each " +
                              "waited for on the GPU (CPU submit + GPU, serialised). " + SystemInfo.graphicsDeviceName);
            report.AppendLine();
            report.AppendLine("| Pose | Median ms | p90 ms |");
            report.AppendLine("|---|---:|---:|");
            var host = new GameObject("AtmosphereReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                foreach (Pose pose in poses)
                {
                    Place(camera, pose);
                    FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, "atmosphere-" + pose.name + ".png"));
                    (double median, double p90) = MeasureFrames(camera);
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:F2} | {2:F2} |",
                        pose.name, median, p90));
                    yield return null;
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "atmosphere.md"), report.ToString());
            Debug.Log("[atmosphere] " + report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>The game camera's URP settings (post-processing, anti-aliasing); CopyFrom skips them.</summary>
        private static void CopyPipelineSettings(Camera from, Camera to)
        {
            UniversalAdditionalCameraData source = from.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData target = to.GetUniversalAdditionalCameraData();
            target.renderPostProcessing = source.renderPostProcessing;
            target.antialiasing = source.antialiasing;
            target.antialiasingQuality = source.antialiasingQuality;
            target.renderShadows = source.renderShadows;
            target.dithering = source.dithering;
        }

        /// <summary>A pose at Bell's corner: up close beside it, or (unnamed) as a warm point from 40 m.</summary>
        private static Pose BellPose(Transform corner, string nearName)
        {
            Vector3 look = corner.position + Vector3.up * BellLookHeight;
            Vector3 eye = nearName != null
                ? corner.position + corner.forward * BellNearDistance + corner.right * BellNearSide
                    + Vector3.up * BellNearHeight
                : corner.position + corner.forward * BellFarDistance + Vector3.up * BellFarHeight;
            return LookPose(nearName ?? "bell_corner_from_40m", eye, look, 50f);
        }

        /// <summary>What the game camera itself shows once 07 is awake: the player's real first frame.</summary>
        private static Pose ViewPose(Transform view, float fov)
        {
            Vector3 eye = view.position;
            Vector3 euler = view.eulerAngles;
            return new Pose
            {
                name = "spawn_game_camera",
                position = new[] { eye.x, eye.y, eye.z },
                euler = new[] { euler.x, euler.y, euler.z },
                fov = fov,
            };
        }

        private static Pose LookPose(string name, Vector3 eye, Vector3 look, float fov)
        {
            return new Pose
            {
                name = name,
                position = new[] { eye.x, eye.y, eye.z },
                lookAt = new[] { look.x, look.y, look.z },
                fov = fov,
            };
        }

        private static void Place(Camera camera, Pose pose)
        {
            var position = new Vector3(pose.position[0], pose.position[1], pose.position[2]);
            Quaternion rotation = pose.lookAt != null && pose.lookAt.Length == 3
                ? Quaternion.LookRotation(new Vector3(pose.lookAt[0], pose.lookAt[1], pose.lookAt[2]) - position)
                : Quaternion.Euler(pose.euler[0], pose.euler[1], pose.euler[2]);
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = pose.fov;
        }

        private static (double Median, double P90) MeasureFrames(Camera camera)
        {
            var target = new RenderTexture(TimedWidth, TimedHeight, 24, RenderTextureFormat.ARGB32);
            var samples = new double[TimedFrames];
            try
            {
                camera.targetTexture = target;
                var watch = new Stopwatch();
                for (int i = -WarmUpFrames; i < TimedFrames; i++)
                {
                    watch.Restart();
                    camera.Render();
                    AsyncGPUReadback.Request(target, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    if (i >= 0)
                    {
                        samples[i] = watch.Elapsed.TotalMilliseconds;
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }

            Array.Sort(samples);
            return (samples[TimedFrames / 2], samples[TimedFrames * 9 / 10]);
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
            public float[] euler = Array.Empty<float>();
            public float fov = 60f;
        }
    }
}
