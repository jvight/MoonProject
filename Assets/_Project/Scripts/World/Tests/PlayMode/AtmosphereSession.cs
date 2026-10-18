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
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// The look of the real game against pillar 6 (M3-10): boots the built Main scene on a new game, lets 07 wake,
    /// then, for the game camera's own view at spawn, each pose of Editor/Captures/atmosphere_views.json and the canyon
    /// from the pad, renders the game's own camera (same lens, post-processing and anti-aliasing) to
    /// Logs/world-captures/atmosphere-*.png
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

        // The Playthrough's step-16 review frame: from 3 m above the pad toward the canyon lip (2 m up), game lens.
        private const float CanyonViewEyeHeight = 3f;
        private const float CanyonViewLookHeight = 2f;

        // Opening v2 (§4, §6 acceptance): from 07's wake (chase height, a few metres ahead of the post so 07's own lamps
        // stay behind the camera) toward the lander in front of The Peak, and from the build apron toward the ruins and
        // toward the dead west mast.
        private const float OpeningEyeHeight = 3.2f;
        private const float OpeningWakeLead = 6f;
        private const float OpeningLookHeight = 4f;
        private const float OpeningFov = 60f;
        private const string OpeningSettingsPath = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        private const string OpeningSignalPillar = "SignalPillar";

        // 07's self-test cues on a New Game (opening v2's POST): the beacon's answering flashes seen from the apron's
        // edge and from the wake, the Begin wave seen from just behind 07's post looking down the cable toward the
        // lander.
        private const float PostTimeout = 120f;
        private const float PostBeaconEyeHeight = 5f;
        private const float PostBeaconLookHeight = 6f;
        private const float PostBeaconFov = 40f;
        private const float PostWaveEyeHeight = 7f;
        private const float PostWaveEyeBack = 6f;
        private const float PostWaveLookAhead = 40f;
        private const float PostWaveFov = 55f;
        private const float PostWaveShotSeconds = 0.25f;

        // While the strips are shot, game time steps this much per frame however long a capture takes, so the
        // flashes and the wave are filmed frame by frame.
        private const float PostFrameSeconds = 0.05f;
        private const float PostTail = 0.3f;
        private const string EmissionStrength = "_EmissionStrength";

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
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
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
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
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

            Camera view = context.Get<IViewCamera>().Camera;
            PoseFile file = JsonUtility.FromJson<PoseFile>(File.ReadAllText(PosesPath));
            var poses = new List<Pose> { ViewPose(view.transform, view.fieldOfView) };
            poses.AddRange(file.poses);
            Assert.IsTrue(context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip),
                "the World has the canyon lip");
            poses.Add(LookPose("toward_the_canyon",
                context.Get<IWorldLayout>().BasePosition + Vector3.up * CanyonViewEyeHeight,
                lip.Position + Vector3.up * CanyonViewLookHeight, view.fieldOfView));
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

        /// <summary>
        /// Opening v2's layout in the real game at the start of a new game (the eclipse holding): from 07's wake toward
        /// the landed lander in front of The Peak under the eclipsed Earth (at a flash of its beacon), and from the build
        /// apron toward the Lumen ruins and toward the dead west mast; then, after the first nightfall (the opening's
        /// milestones replayed), the apron toward the west with the signal pillar shown, as at B11. Writes
        /// Logs/world-captures/opening-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator OpeningLayout_IsCapturedFromTheWakeAndTheApron()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
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
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
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
            IWorldAnchors anchors = context.Get<IWorldAnchors>();
            Vector3 wake = Anchor(anchors, WorldAnchorIds.PlaceWake).Position;
            Vector3 lander = Anchor(anchors, WorldAnchorIds.PlaceLander).Position;
            Vector3 apron = Anchor(anchors, WorldAnchorIds.PlaceApron).Position;
            Vector3 ruins = Anchor(anchors, WorldAnchorIds.PlaceRuins).Position;
            Vector3 mast = Anchor(anchors, WorldAnchorIds.RelayPrefix + 1).Position;
            Vector3 lift = Vector3.up * OpeningLookHeight;
            Vector3 toLander = Vector3.ProjectOnPlane(lander - wake, Vector3.up).normalized;
            Vector3 eye = wake + toLander * OpeningWakeLead + Vector3.up * OpeningEyeHeight;
            Vector3 apronEye = apron + Vector3.up * OpeningEyeHeight;
            Camera view = context.Get<IViewCamera>().Camera;
            float period = AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).Landmarks.LanderBeacon.Period;
            while (Mathf.Repeat(Time.time, period) > Time.deltaTime * 2f)
            {
                yield return null;
            }

            var host = new GameObject("OpeningReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                foreach (Pose pose in new[]
                         {
                             LookPose("wake_toward_the_lander", eye, lander + lift, OpeningFov),
                             LookPose("apron_toward_the_ruins", apronEye, ruins + lift, OpeningFov),
                             LookPose("apron_toward_the_west_mast", apronEye, mast + lift, OpeningFov),
                         })
                {
                    Place(camera, pose);
                    FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, "opening-" + pose.name + ".png"));
                }

                foreach (OpeningMilestone milestone in new[]
                         {
                             OpeningMilestone.FirstCharge, OpeningMilestone.PowerOn, OpeningMilestone.NightReady,
                         })
                {
                    context.Events.Publish(new OpeningMilestoneReached(milestone, true));
                }

                FindNamed(OpeningSignalPillar).gameObject.SetActive(true);
                yield return new WaitForSeconds(SettleSeconds);
                Place(camera, LookPose("b11_night_apron_toward_the_signal", apronEye, mast + lift, OpeningFov));
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                    Path.Combine(CaptureFolder, "opening-b11_night_apron_toward_the_signal.png"));
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// 07's self-test on a real New Game: the lander's beacon blinks sharply with each spark of 07's failing cutter,
        /// and when 07 begins its recovery one bright wave runs the cable from 07's post to the lander. Strips of frames
        /// go to Logs/world-captures/post-*.png with post-cues.md (the beacon light's intensity and the first
        /// and last markers' glow per frame).
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator PostCues_FlashTheBeaconAndRunTheCable_OnANewGame()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            bool skippedTitle = SessionState.GetBool(GameBootstrap.TestRunSkipsTitleKey, false);
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, false);
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
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            GameContext context = bootstrap.Context;
            LanderBeaconSettings tuning =
                AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).Landmarks.LanderBeacon;
            LanderBeacon beacon = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                beacon = beacon != null ? beacon : root.GetComponentInChildren<LanderBeacon>(true);
            }

            Assert.IsNotNull(beacon, "World stood the lander's beacon");
            var wiring = new SerializedObject(beacon);
            var light = (Light)wiring.FindProperty("_light").objectReferenceValue;
            SerializedProperty markerList = wiring.FindProperty("_markers");
            var first = (Renderer)markerList.GetArrayElementAtIndex(0).objectReferenceValue;
            var last = (Renderer)markerList.GetArrayElementAtIndex(markerList.arraySize - 1).objectReferenceValue;
            float cutterStartedAt = -1f;
            float cutterAt = -1f;
            float beginAt = -1f;
            int sparkPulse = -1;
            IDisposable completed = context.Events.Subscribe<BootCheckCompleted>(check =>
                cutterAt = check.Step == BootCheckStep.Cutter ? Time.time : cutterAt);
            IDisposable pulses = context.Events.Subscribe<BootCheckPulse>(pulse =>
                sparkPulse = pulse.Step == BootCheckStep.Cutter ? pulse.Index : sparkPulse);
            IDisposable cutterStarted = context.Events.Subscribe<BootCheckStarted>(check =>
                cutterStartedAt = check.Step == BootCheckStep.Cutter ? Time.time : cutterStartedAt);
            IDisposable started = context.Events.Subscribe<BootCheckStarted>(check =>
                beginAt = check.Step == BootCheckStep.Begin ? Time.time : beginAt);
            var report = new StringBuilder("# 07's self-test cues (real Main scene, New Game)\n\n");
            var host = new GameObject("PostReviewCamera");
            try
            {
                Camera view = context.Get<IViewCamera>().Camera;
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                IWorldAnchors anchors = context.Get<IWorldAnchors>();
                Vector3 wake = Anchor(anchors, WorldAnchorIds.PlaceWake).Position;
                Vector3 lander = Anchor(anchors, WorldAnchorIds.PlaceLander).Position;
                Vector3 apron = Anchor(anchors, WorldAnchorIds.PlaceApron).Position;
                Vector3 toLander = Vector3.ProjectOnPlane(lander - wake, Vector3.up).normalized;
                Directory.CreateDirectory(CaptureFolder);

                context.Events.Publish(new NewGameRequested());
                float requested = Time.time;
                while (cutterStartedAt < 0f)
                {
                    Assert.Less(Time.time - requested, PostTimeout, "07's self-test reaches the cutter check");
                    yield return null;
                }

                FrameCapture.SavePng(view, CaptureWidth, CaptureHeight,
                    Path.Combine(CaptureFolder, "post-cutter-game.png"));
                Time.captureDeltaTime = PostFrameSeconds;
                Pose nearBeacon = LookPose("post_beacon", apron + Vector3.up * PostBeaconEyeHeight,
                    lander + Vector3.up * PostBeaconLookHeight, PostBeaconFov);
                Pose fromWake = LookPose("post_beacon_wake",
                    wake + toLander * OpeningWakeLead + Vector3.up * OpeningEyeHeight,
                    lander + Vector3.up * OpeningLookHeight, OpeningFov);
                report.AppendLine("## The beacon blinks with the cutter's sparks").AppendLine()
                    .AppendLine("| Frame | s into the cutter check | Cutter pulse | Beacon light |")
                    .AppendLine("|---|---:|---:|---:|");
                float flashLevel = tuning.LightIntensity * (1f + tuning.FlashBoost) * 0.5f;
                float brightest = 0f;
                int flashes = 0;
                bool lit = false;
                for (int frame = 0; cutterAt < 0f || Time.time - cutterAt < PostTail; frame++)
                {
                    Assert.Less(Time.time - requested, PostTimeout, "the cutter check completes");
                    string name = string.Format(CultureInfo.InvariantCulture, "post-beacon-{0:00}", frame);
                    Place(camera, nearBeacon);
                    FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, name + ".png"));
                    Place(camera, fromWake);
                    FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, name + "-wake.png"));
                    brightest = Mathf.Max(brightest, light.intensity);
                    flashes += !lit && light.intensity > flashLevel ? 1 : 0;
                    lit = light.intensity > flashLevel;
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:F2} | {2} | {3:F2} |",
                        name, Time.time - cutterStartedAt, sparkPulse, light.intensity));
                    yield return null;
                }

                Assert.Greater(brightest, flashLevel, "the answering flashes outshine the slow pulse's peak");
                Assert.AreEqual(tuning.LastSparkPulse - tuning.FirstSparkPulse + 1, flashes,
                    "one flash per spark of the cutter");

                while (beginAt < 0f)
                {
                    Assert.Less(Time.time - requested, PostTimeout, "07 begins its recovery");
                    yield return null;
                }

                Place(camera, LookPose("post_wave", wake - toLander * PostWaveEyeBack + Vector3.up * PostWaveEyeHeight,
                    wake + toLander * PostWaveLookAhead, PostWaveFov));
                report.AppendLine().AppendLine("## One wave runs the cable when 07 begins").AppendLine()
                    .AppendLine("| Frame | s after Begin | First marker | Last marker |")
                    .AppendLine("|---|---:|---:|---:|");
                var glow = new MaterialPropertyBlock();
                float calmCeiling = tuning.MarkerGlow + tuning.MarkerPulse;
                float firstPeakAt = -1f;
                float lastPeakAt = -1f;
                float run = tuning.MarkerRunSeconds * (1f + tuning.MarkerWaveWidth) + PostTail;
                float nextShot = 0f;
                for (int frame = 0; Time.time - beginAt < run;)
                {
                    float since = Time.time - beginAt;
                    first.GetPropertyBlock(glow);
                    float firstGlow = glow.GetFloat(EmissionStrength);
                    last.GetPropertyBlock(glow);
                    float lastGlow = glow.GetFloat(EmissionStrength);
                    firstPeakAt = firstPeakAt < 0f && firstGlow > calmCeiling ? since : firstPeakAt;
                    lastPeakAt = lastPeakAt < 0f && lastGlow > calmCeiling ? since : lastPeakAt;
                    if (since >= nextShot)
                    {
                        string name = string.Format(CultureInfo.InvariantCulture, "post-wave-{0:00}", frame++);
                        FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                            Path.Combine(CaptureFolder, name + ".png"));
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                            "| {0} | {1:F2} | {2:F2} | {3:F2} |", name, since, firstGlow, lastGlow));
                        nextShot += PostWaveShotSeconds;
                    }

                    yield return null;
                }

                Assert.GreaterOrEqual(firstPeakAt, 0f, "the wave lights 07's end of the cable past the calm waves");
                Assert.Greater(lastPeakAt, firstPeakAt, "and reaches the lander's end after it");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                completed.Dispose();
                pulses.Dispose();
                cutterStarted.Dispose();
                started.Dispose();
                Object.Destroy(host);
                File.WriteAllText(Path.Combine(CaptureFolder, "post-cues.md"), report.ToString());
#if UNITY_EDITOR
                SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, skippedTitle);
#endif
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static WorldAnchor Anchor(IWorldAnchors anchors, string id)
        {
            Assert.IsTrue(anchors.TryGet(id, out WorldAnchor anchor), $"the World has {id}");
            return anchor;
        }

        private static Transform FindNamed(string name)
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node;
                    }
                }
            }

            throw new InvalidOperationException($"The scene has no '{name}'.");
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
