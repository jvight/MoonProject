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
        // The Begin wave is filmed at the tear, where it dies: from a little back toward 07's post and above.
        private const float PostWaveEyeHeight = 2.6f;
        private const float PostWaveEyeBack = 6f;
        private const float PostWaveFov = 45f;
        private const float PostWaveShotSeconds = 0.25f;

        // While the strips are shot, game time steps this much per frame however long a capture takes, so the
        // flashes and the wave are filmed frame by frame.
        private const float PostFrameSeconds = 0.05f;
        private const float PostTail = 0.3f;
        private const string HaloIntensity = "_Intensity";
        // The landing zone: midway looks at the tear from a little back toward the wake and above, the port view at
        // the plug from in front of the port; the zone's edge view stands this far
        // back from the lander toward the wake and this high; the overhead hangs this high above the lander, its
        // look nudged toward the wake so the wake-side edge shows below the lander.
        private const float LandingTearBack = 6f;
        private const float LandingTearHeight = 2.4f;
        private const float LandingTearFov = 45f;
        private const float LandingPortBack = 5f;
        private const float LandingPortHeight = 1.8f;
        private const string PortName = "ServicePort";
        private const float LandingEdgeBack = 48f;
        private const float LandingEdgeHeight = 14f;
        private const float LandingOverheadHeight = 110f;
        private const float LandingOverheadTilt = 0.01f;
        private const float LandingOverheadFov = 50f;
        // The blast zone: 07 stops this far short of the lander for its chase view; golden hour is the lowest sun (at
        // least this many degrees up) with no terrain between it and a point in the zone, searched for in steps
        // through a day from a sunlit clock time; each light settles this long before it is shot.
        private const float BlastChaseBack = 20f;
        private const float BlastSunlitSeconds = 210f;
        private const float BlastGoldenMinElevation = 4f;
        private const float BlastShadowProbeHeight = 3f;
        private const float BlastShadowProbeReach = 2000f;
        private const float BlastSearchSeconds = 7200f;
        private const float BlastSearchStep = 20f;
        private const float BlastSettleSeconds = 2f;
        // The basin's story props (basin dressing §2, §5): the Scout from the lane in the eclipse, from the apron at
        // last light (07's chase behind it, a wide eye above the apron), close up, and its shadow from above; the
        // ruins' front door from the trail's start at golden hour.
        private const float ScoutApronEyeHeight = 3.2f;
        private const float ScoutLookHeight = 1.5f;
        private const float ScoutCloseDistance = 9f;
        private const float ScoutCloseEyeHeight = 2.2f;
        private const float ScoutCloseBearingOffset = 60f;
        private const float ScoutCloseFov = 45f;
        private const float ScoutApronFov = 40f;
        private const float ScoutOverheadHeight = 70f;
        private const float ScoutShadowMid = 30f;
        private static readonly Vector2 FrontDoorTrailStart = new Vector2(27f, 147f);
        private static readonly Vector2 FrontDoorTrailEnd = new Vector2(80f, 182f);

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
        /// The landing zone on a real New Game (opening v2 §4): the lander's blast-swept halo and the old, torn cable
        /// from 07's dead post toward the lander, seen from the wake, at the tear midway, at the plug in the lander's
        /// port, from the zone's edge and from straight above the lander. Logs/world-captures/landing-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator LandingZone_IsCapturedFromTheWakeMidwayAndAbove()
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
            Vector3 toLander = Vector3.ProjectOnPlane(lander - wake, Vector3.up).normalized;
            Vector3 lift = Vector3.up * OpeningLookHeight;
            Vector3 eye = wake + toLander * OpeningWakeLead + Vector3.up * OpeningEyeHeight;
            LanderBeacon beacon = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                beacon = beacon != null ? beacon : root.GetComponentInChildren<LanderBeacon>(true);
            }

            Assert.IsNotNull(beacon, "World stood the lander's beacon");
            Vector3 tear = ((Renderer)new SerializedObject(beacon).FindProperty("_spark").objectReferenceValue)
                .transform.position;
            Vector3 midway = tear - toLander * LandingTearBack + Vector3.up * LandingTearHeight;
            Vector3 edge = lander - toLander * LandingEdgeBack + Vector3.up * LandingEdgeHeight;
            Vector3 port = FindNamed(PortName).position;
            Vector3 outward = Vector3.ProjectOnPlane(port - lander, Vector3.up).normalized;
            Vector3 portEye = port + outward * LandingPortBack + Vector3.up * LandingPortHeight;
            Camera view = context.Get<IViewCamera>().Camera;
            var host = new GameObject("LandingReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                foreach (Pose pose in new[]
                         {
                             LookPose("wake_toward_the_lander", eye, lander + lift, OpeningFov),
                             LookPose("midway_at_the_tear", midway, tear, LandingTearFov),
                             LookPose("port_plug", portEye, port, LandingTearFov),
                             LookPose("zone_edge_toward_the_lander", edge, lander, OpeningFov),
                             LookPose("overhead", lander + Vector3.up * LandingOverheadHeight,
                                 lander + toLander * LandingOverheadTilt, LandingOverheadFov),
                         })
                {
                    Place(camera, pose);
                    FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, "landing-" + pose.name + ".png"));
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The lander's blast-swept zone (opening v2 §4) in the eclipse and at golden hour: from 07's chase camera
        /// stopped 20 m short of the lander, from the zone's edge and from straight above.
        /// Logs/world-captures/blast-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator BlastZone_IsCapturedInTheEclipseAndAtGoldenHour()
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
            SkyCycleSystem sky = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                sky = sky != null ? sky : root.GetComponentInChildren<SkyCycleSystem>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(sky, "the scene has its sky cycle");
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
            Vector3 toLander = Vector3.ProjectOnPlane(lander - wake, Vector3.up).normalized;
            Vector3 edge = lander - toLander * LandingEdgeBack + Vector3.up * LandingEdgeHeight;
            Vector3 chase = lander - toLander * BlastChaseBack;
            chase.y = context.Get<ITerrainQuery>().SampleHeight(chase.x, chase.z);
            context.Get<IRoverPlacement>().PlaceAt(chase, Quaternion.LookRotation(toLander, Vector3.up));
            Camera view = context.Get<IViewCamera>().Camera;
            var host = new GameObject("BlastReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                foreach (string light in new[] { "eclipse", "golden" })
                {
                    if (light == "golden")
                    {
                        // The first charge lets the eclipse end; golden hour is the sun low over the rim after
                        // it has stood higher.
                        context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
                        yield return null;
                        float clock = -1f;
                        float lowest = float.MaxValue;
                        Vector3 above = lander - toLander * BlastChaseBack * 0.5f + Vector3.up * BlastShadowProbeHeight;
                        for (float t = BlastSunlitSeconds; t < BlastSunlitSeconds + BlastSearchSeconds;
                             t += BlastSearchStep)
                        {
                            sky.SetClockTime(t);
                            Vector3 toSun = sky.SunDirection;
                            float elevation = Mathf.Asin(toSun.y) * Mathf.Rad2Deg;
                            if (elevation >= BlastGoldenMinElevation && elevation < lowest
                                && !Physics.Raycast(above, toSun, BlastShadowProbeReach, Layers.GroundMask,
                                    QueryTriggerInteraction.Ignore))
                            {
                                lowest = elevation;
                                clock = t;
                            }
                        }

                        Assert.GreaterOrEqual(clock, 0f, "a low sun lights the lander some time in the day");
                        sky.SetClockTime(clock);
                        Assert.IsFalse(sky.IsHoldingEclipse, "the eclipse has ended");
                        Debug.Log($"[blast] golden hour: sun {lowest:F1} deg up");
                        Debug.Log($"[blast] golden hour at clock {clock} s");
                    }

                    yield return new WaitForSeconds(BlastSettleSeconds);
                    FrameCapture.SavePng(view, CaptureWidth, CaptureHeight,
                        Path.Combine(CaptureFolder, "blast-" + light + "-chase.png"));
                    foreach (Pose pose in new[]
                             {
                                 LookPose("edge", edge, lander, OpeningFov),
                                 LookPose("overhead", lander + Vector3.up * LandingOverheadHeight,
                                     lander + toLander * LandingOverheadTilt, LandingOverheadFov),
                             })
                    {
                        Place(camera, pose);
                        FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                            Path.Combine(CaptureFolder, "blast-" + light + "-" + pose.name + ".png"));
                    }
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The basin's story props against the real sky (basin dressing §2, §5): in the eclipse, the Scout from the
        /// first drive's lane (nothing new may show); at golden hour, the ruins' front door from the trail's start; at
        /// last light, the Scout's shadow from the apron (07's chase view and a wide eye), the Scout close up and its
        /// shadow from above. The Sun stands where the opening's milestones leave it. Logs/world-captures/basin-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator BasinProps_AreCapturedInTheirLight()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            GroundShaping shaping =
                AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).CreateSurface().Shaping;
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
            var terrain = context.Get<ITerrainQuery>();
            IWorldAnchors anchors = context.Get<IWorldAnchors>();
            Vector3 wake = Anchor(anchors, WorldAnchorIds.PlaceWake).Position;
            Vector3 lander = Anchor(anchors, WorldAnchorIds.PlaceLander).Position;
            Vector3 apron = Anchor(anchors, WorldAnchorIds.PlaceApron).Position;
            Vector3 scout = OnGround(terrain, shaping.ScoutSpot);
            Vector3 scoutLook = scout + Vector3.up * ScoutLookHeight;
            Vector3 toScout = Vector3.ProjectOnPlane(scout - apron, Vector3.up).normalized;

            Vector3 lane = Vector3.ProjectOnPlane(lander - wake, Vector3.up);
            float along = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(scout - wake, Vector3.up), lane)
                                        / lane.sqrMagnitude);
            Vector3 onLane = OnGround(terrain, new Vector2(wake.x + lane.x * along, wake.z + lane.z * along));
            Vector3 closeEye = scout
                               + Quaternion.Euler(0f, ScoutCloseBearingOffset, 0f) * -toScout * ScoutCloseDistance;
            closeEye.y = terrain.SampleHeight(closeEye.x, closeEye.z) + ScoutCloseEyeHeight;
            Vector3 shadowMid = scout - toScout * ScoutShadowMid;
            Vector3 doorEye = OnGround(terrain, FrontDoorTrailStart) + Vector3.up * ScoutApronEyeHeight;
            Vector3 doorLook = OnGround(terrain, FrontDoorTrailEnd) + Vector3.up * ScoutLookHeight;

            var rover = context.Get<IRoverPlacement>();
            Camera view = context.Get<IViewCamera>().Camera;
            var host = new GameObject("BasinReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);

                rover.PlaceAt(onLane, Quaternion.LookRotation(Vector3.ProjectOnPlane(scout - onLane, Vector3.up)));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Shoot(view, "basin-eclipse-lane-chase");
                Shoot(camera, LookPose("eclipse-lane", onLane + Vector3.up * OpeningEyeHeight, scoutLook, OpeningFov));

                context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Debug.Log($"[basin] golden hour: sun at {SunReport(context)}");
                Shoot(camera, LookPose("golden-front-door", doorEye, doorLook, ScoutApronFov));

                context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, true));
                rover.PlaceAt(apron, Quaternion.LookRotation(toScout, Vector3.up));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Debug.Log($"[basin] last light: sun at {SunReport(context)}; the Scout at {scout}");
                Shoot(view, "basin-lastlight-apron-chase");
                foreach (Pose pose in new[]
                         {
                             LookPose("lastlight-apron", apron + Vector3.up * ScoutApronEyeHeight, scoutLook,
                                 ScoutApronFov),
                             LookPose("lastlight-close", closeEye, scoutLook, ScoutCloseFov),
                             LookPose("lastlight-overhead", shadowMid + Vector3.up * ScoutOverheadHeight,
                                 shadowMid + toScout * LandingOverheadTilt, LandingOverheadFov),
                         })
                {
                    Shoot(camera, pose);
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static void Shoot(Camera camera, Pose pose)
        {
            Place(camera, pose);
            Shoot(camera, "basin-" + pose.name);
        }

        private static void Shoot(Camera camera, string name)
        {
            FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
        }

        private static string SunReport(GameContext context)
        {
            Vector3 toSun = context.Get<ISunlight>().SunDirection;
            float bearing = Mathf.Repeat(Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg, 360f);
            return $"{bearing:F1} deg, {Mathf.Asin(toSun.y) * Mathf.Rad2Deg:F2} deg up";
        }

        private static Vector3 OnGround(ITerrainQuery terrain, Vector2 spot)
        {
            return new Vector3(spot.x, terrain.SampleHeight(spot.x, spot.y), spot.y);
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
            SerializedProperty postClips = wiring.FindProperty("_postClips");
            var first = (Renderer)postClips.GetArrayElementAtIndex(0).objectReferenceValue;
            var last = (Renderer)postClips.GetArrayElementAtIndex(postClips.arraySize - 1).objectReferenceValue;
            var spark = (Renderer)wiring.FindProperty("_spark").objectReferenceValue;
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

                Vector3 tear = spark.transform.position;
                Place(camera, LookPose("post_wave", tear - toLander * PostWaveEyeBack + Vector3.up * PostWaveEyeHeight,
                    tear, PostWaveFov));
                report.AppendLine().AppendLine("## One wave runs 07's side of the cable and dies at the tear")
                    .AppendLine().AppendLine("| Frame | s after Begin | First clip | Last clip | Spark |")
                    .AppendLine("|---|---:|---:|---:|---:|");
                var glow = new MaterialPropertyBlock();
                float calmCeiling = tuning.ClipHaloRest + tuning.MarkerHaloIntensity * 0.5f;
                float firstPeakAt = -1f;
                float lastPeakAt = -1f;
                float sparkAt = -1f;
                float run = tuning.MarkerRunSeconds + tuning.SparkDelay
                    + Mathf.Max(tuning.TearStutterSeconds, tuning.SparkSeconds) + PostTail;
                float nextShot = 0f;
                for (int frame = 0; Time.time - beginAt < run;)
                {
                    float since = Time.time - beginAt;
                    first.GetPropertyBlock(glow);
                    float firstGlow = glow.GetFloat(HaloIntensity);
                    last.GetPropertyBlock(glow);
                    float lastGlow = glow.GetFloat(HaloIntensity);
                    spark.GetPropertyBlock(glow);
                    float sparkGlow = glow.GetFloat(HaloIntensity);
                    firstPeakAt = firstPeakAt < 0f && firstGlow > calmCeiling ? since : firstPeakAt;
                    lastPeakAt = lastPeakAt < 0f && lastGlow > calmCeiling ? since : lastPeakAt;
                    sparkAt = sparkAt < 0f && sparkGlow > 0f ? since : sparkAt;
                    if (since >= nextShot)
                    {
                        string name = string.Format(CultureInfo.InvariantCulture, "post-wave-{0:00}", frame++);
                        FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                            Path.Combine(CaptureFolder, name + ".png"));
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                            "| {0} | {1:F2} | {2:F2} | {3:F2} | {4:F2} |", name, since, firstGlow, lastGlow,
                            sparkGlow));
                        nextShot += PostWaveShotSeconds;
                    }

                    yield return null;
                }

                Assert.GreaterOrEqual(firstPeakAt, 0f, "the wave lights 07's end of the cable");
                Assert.Greater(lastPeakAt, firstPeakAt, "and reaches the tear after it");
                Assert.Greater(sparkAt, lastPeakAt, "where one cold spark ticks once it has died");
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
