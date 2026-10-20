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

        // The v3 cues' fixed points: the array from its approach, the tower low from the base's side against the sky.
        private const string CueArrayName = "FallenSolarArray";
        private const string CueTowerName = "RadioTower";
        private const string CueGlintName = "ArrayGlint";
        private const float CueArrayStandOff = 14f;
        private const float CueTowerStandOff = 12f;
        private const float CueLowEyeHeight = 1.2f;
        private const float CueTowerLook = 7f;
        private const string CueLampName = "ModuleLamp";

        // The call's point of light: when the face shot holds, one sign's length, how near its brightest a shot waits
        // for.
        private const string SkyPointName = "CallSkyPoint";
        private const float SkyPointFaceShotSeconds = 3f;
        private const float SkyPointSignSeconds = 4f;
        private const float SkyPointPeakSlack = 0.02f;

        // The live series: a frame every tenth of a second, in its own folder, after the call settles from broken.
        private const float SkyPointFrameSeconds = 0.1f;
        private const float SkyPointSettleSeconds = 0.5f;
        private const string SkyPointSeriesFolder = "skypoint-series";
        private const float SkyPointProbeDistance = 100f;

        // The array's glint watched from the pad: how long after the lamp-out, and a frame every few seconds.
        private const int GlintWatchSeconds = 20;
        private const int GlintShotEvery = 4;
        private const float GlintZoomFov = 10f;
        private const string GlintSparkleName = "Sparkle";
        private const string CueLampLens = "PowerModule_StatusLamp";
        private const float CueLampStandOff = 10f;
        private const float CueLampPeak = 0.9f;
        private const float CueLampTimeout = 30f;

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
        // The basin's story props (basin dressing §2, §5): the Scout from the lane in the eclipse, from the apron in
        // each light and close up on the first night (07's chase behind it, a wide eye above the apron); the ruins'
        // front door from the trail's start at golden hour. The Scout's brightest pixel is read within this many
        // pixels of where it stands on screen; the first night is shot this long (clock seconds) after nightfall.
        private const float ScoutApronEyeHeight = 3.2f;
        private const float ScoutLookHeight = 1.5f;
        private const float ScoutCloseDistance = 9f;
        private const float ScoutCloseEyeHeight = 2.2f;
        private const float ScoutCloseBearingOffset = 60f;
        private const float ScoutCloseFov = 45f;
        private const float ScoutApronFov = 40f;
        // The relief's review: 07 a third of the way up the first drive's cable, the apron's eye looking far out over
        // the plain, a low eye on the south ridge's flank, and a high eye behind the first boulder track's top.
        private const float ReliefCableShare = 0.35f;
        private const float ReliefFar = 150f;
        private const float ReliefFarDrop = 6f;
        private const float ReliefRidgeStandOff = 45f;
        private const float ReliefRidgeLookAhead = 20f;
        private const float ReliefLowEyeHeight = 2f;
        private const float ReliefCraterStandOff = 20f;
        private const float ReliefCraterEyeHeight = 12f;

        // The story scan hosts from 07's chase camera, this far back toward home on the drive up to each.
        // 07 stands this far to the side of the line, facing along it, so the host shows ahead beside it.
        private const float HostApproachDistance = 16f;
        private const float HostSideStep = 5f;

        // The near-field rubble from 07's chase camera: 07 parked this far to either side of the cable's lane.
        private const float ReliefRubbleStandOff = 35f;

        // The drive-by: 07 set down this far further on each frame (12 m/s at 60 frames a second), a shot every so many.
        private const float DriveByStep = 0.2f;
        private const int DriveByFrames = 300;
        private const int DriveByShotEvery = 25;
        private const float ReliefOverviewBack = 2.2f;
        private const float ReliefOverviewHeight = 30f;
        private const int ScoutPeakWindow = 40;
        private const float PeakChange = 0.01f;
        private const float FirstNightSeconds = 300f;
        private static readonly Vector2 FrontDoorTrailStart = new Vector2(27f, 147f);
        private static readonly Vector2 FrontDoorTrailEnd = new Vector2(79.5f, 185.5f);
        private static readonly Vector2 TrailStart = new Vector2(27f, 147f);
        private static readonly Vector2 TrailBend = new Vector2(42.7f, 148.6f);
        private const float ReflectorLookHeight = 0.3f;

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
        /// The call's point of light beside dark Earth (opening v3 purpose §2) on a real New Game, at R1: from 07's
        /// face shot, from the first chase view after the hand-over, and from the chase camera with 07 turned toward
        /// it, each at the brightest moment of one 4 s sign, with its level and whether a call signal is heard in the
        /// log. Logs/world-captures/skypoint-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator CallSkyPoint_IsCapturedFromTheFaceShotAndTheChase()
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
            var point = FindNamed(SkyPointName).GetComponent<CallSkyPoint>();
            Assert.IsNotNull(point, "the call's point of light stands in the sky");
            Camera view = context.Get<IViewCamera>().Camera;
            yield return new WaitForSeconds(SkyPointFaceShotSeconds);
            yield return ShootSkyPoint(context, view, point, "skypoint-1-face-shot");

            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);
            yield return ShootSkyPoint(context, view, point, "skypoint-2-first-chase-view");

            Vector3 toward = Vector3.ProjectOnPlane(context.Get<ICallSource>().Direction, Vector3.up).normalized;
            Vector3 at = context.Get<IRoverState>().Position;
            context.Get<IRoverPlacement>().PlaceAt(OnGround(context.Get<ITerrainQuery>(), new Vector2(at.x, at.z)),
                Quaternion.LookRotation(toward, Vector3.up));
            yield return new WaitForSeconds(SettleSeconds);
            yield return ShootSkyPoint(context, view, point, "skypoint-3-chase-toward-the-light");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The call's point of light live, from 07's chase camera turned toward it on a real New Game: one full 4 s sign
        /// while the call is broken, then, once 07's first charge locks it, one full sign steady, as a frame series
        /// (Logs/world-captures/skypoint-series/broken-NN.png, locked-NN.png) with each frame's level and ring in the
        /// log.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator CallSkyPoint_IsFilmedThroughOneSign_BrokenThenLocked()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath);
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

            var point = FindNamed(SkyPointName).GetComponent<CallSkyPoint>();
            Camera view = context.Get<IViewCamera>().Camera;
            Vector3 toward = Vector3.ProjectOnPlane(context.Get<ICallSource>().Direction, Vector3.up).normalized;
            Vector3 at = context.Get<IRoverState>().Position;
            context.Get<IRoverPlacement>().PlaceAt(OnGround(context.Get<ITerrainQuery>(), new Vector2(at.x, at.z)),
                Quaternion.LookRotation(toward, Vector3.up));
            yield return new WaitForSeconds(SettleSeconds);
            yield return FilmSkyPoint(context, view, point, "broken");

            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            yield return new WaitForSeconds(settings.Sky.CallPoint.StateFadeSeconds + SkyPointSettleSeconds);
            Assert.AreEqual(CallState.Locked, context.Get<ICallSource>().State, "the first charge locks the call");
            yield return FilmSkyPoint(context, view, point, "locked");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The fallen array's glint from the pad at the lamp-out (opening v3 R4), on a real New Game: 07 on the apron
        /// facing the array as its first charge ends, the lander's reserve spent and the eclipse breaking; every second
        /// for <see cref="GlintWatchSeconds"/> the Sun, each wing's glint and whatever stands between the chase view and
        /// each wing go to the log, with frames at Logs/world-captures/glint-pad-NN.png.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator ArrayGlint_IsWatchedFromThePad_AtTheLampOut()
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

            Vector3 apron = Anchor(context.Get<IWorldAnchors>(), WorldAnchorIds.PlaceApron).Position;
            Transform array = FindNamed(CueArrayName);
            Transform halos = FindNamed(CueGlintName);
            var glint = halos.GetComponent<ArrayGlint>();
            Vector3 toArray = Vector3.ProjectOnPlane(array.position - apron, Vector3.up).normalized;
            context.Get<IRoverPlacement>().PlaceAt(OnGround(context.Get<ITerrainQuery>(),
                new Vector2(apron.x, apron.z)), Quaternion.LookRotation(toArray, Vector3.up));
            yield return new WaitForSeconds(SettleSeconds);
            context.Events.Publish(new LanderReserveChanged(0f, false));
            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            Camera view = context.Get<IViewCamera>().Camera;
            for (int second = 0; second <= GlintWatchSeconds; second++)
            {
                var line = new StringBuilder($"[glint-pad] {second:00} s: sun {SunReport(context)}; " +
                    $"array {Vector3.Distance(view.transform.position, array.position):F0} m off, viewport " +
                    $"{view.WorldToViewportPoint(array.position)}; wings");
                int wing = 0;
                foreach (Transform sparkle in halos)
                {
                    if (sparkle.name != GlintSparkleName)
                    {
                        continue;
                    }

                    Vector3 at = sparkle.position;
                    bool hitSomething = Physics.Linecast(view.transform.position, at, out RaycastHit hit,
                        Layers.GroundMask | Layers.PropMask, QueryTriggerInteraction.Ignore);
                    string between = hitSomething && !hit.collider.transform.IsChildOf(array)
                        ? $"blocked by {hit.collider.name} at {hit.distance:F0} m"
                        : "clear";
                    line.Append($" {wing}: {glint.LevelOf(wing):F2} ({between})");
                    wing++;
                }

                Debug.Log(line.ToString());
                if (second % GlintShotEvery == 0)
                {
                    Shoot(view, $"glint-pad-{second:00}");
                }

                yield return new WaitForSeconds(1f);
            }

            // The player turning from the dark lander toward the light: the same eye, looking at the array.
            var host = new GameObject("GlintTurnCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                Place(camera, LookPose("glint-pad-turned", view.transform.position, array.position, OpeningFov));
                Shoot(camera, "glint-pad-turned");
                Place(camera, LookPose("glint-pad-zoom", view.transform.position, array.position, GlintZoomFov));
                Shoot(camera, "glint-pad-zoom");
                Debug.Log($"[glint-pad] turned toward the array: wings {glint.LevelOf(0):F2} / {glint.LevelOf(1):F2}");
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// One full sign of the point, a frame about every <see cref="SkyPointFrameSeconds"/> (each frame's own time
        /// is logged, as a capture takes time of its own).
        /// </summary>
        private static IEnumerator FilmSkyPoint(GameContext context, Camera camera, CallSkyPoint point, string phase)
        {
            int frames = Mathf.CeilToInt(SkyPointSignSeconds / SkyPointFrameSeconds);
            var log = new StringBuilder($"[skypoint] {phase} (seconds level/ring @pixel):");
            float started = Time.time;
            for (int i = 0; i < frames; i++)
            {
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                    Path.Combine(CaptureFolder, SkyPointSeriesFolder, $"{phase}-{i:00}.png"));
                Vector2 pixel = CapturePixel(camera,
                    camera.transform.position + context.Get<ICallSource>().Direction * SkyPointProbeDistance);
                log.Append($" {Time.time - started:F2} {point.Level:F2}/{point.Pulse:F2} @{pixel.x:F0},{pixel.y:F0};");
                yield return new WaitForSeconds(SkyPointFrameSeconds);
            }

            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Where <paramref name="world"/> lands in a capture of <paramref name="camera"/>, in pixels from its top left:
        /// projected at the capture's own aspect, which the camera takes while it renders into the capture's target.
        /// </summary>
        private static Vector2 CapturePixel(Camera camera, Vector3 world)
        {
            float aspect = (float)CaptureWidth / CaptureHeight;
            Matrix4x4 projection = Matrix4x4.Perspective(camera.fieldOfView, aspect, camera.nearClipPlane,
                camera.farClipPlane);
            Vector3 clip = projection.MultiplyPoint(camera.worldToCameraMatrix.MultiplyPoint(world));
            return new Vector2((clip.x + 1f) * 0.5f * CaptureWidth, (1f - clip.y) * 0.5f * CaptureHeight);
        }

        /// <summary>
        /// Waits through one sign for the point's brightest moment, then shoots <paramref name="camera"/> and logs
        /// its level, the call's state and whether its signal is heard.
        /// </summary>
        private static IEnumerator ShootSkyPoint(GameContext context, Camera camera, CallSkyPoint point, string name)
        {
            float started = Time.time;
            float brightest = -1f;
            while (Time.time - started < SkyPointSignSeconds)
            {
                brightest = Mathf.Max(brightest, point.Level);
                yield return null;
            }

            started = Time.time;
            while (point.Level < brightest - SkyPointPeakSlack && Time.time - started < SkyPointSignSeconds)
            {
                yield return null;
            }

            Shoot(camera, name);
            bool heard = context.TryGet(out ICallSignal _);
            Debug.Log($"[skypoint] {name}: level {point.Level:F2} (brightest {brightest:F2}), call " +
                      $"{context.Get<ICallSource>().State}, signal heard {heard}");
        }

        /// <summary>
        /// The opening v3 world cues at their fixed points (opening-v3-plan slice 1.2), on a real New Game:
        /// the lander's amber beacon in 07's first forward view; the lander from the apron, lit, and dark once its
        /// reserve is spent (its beacon, its port's pilot lamp and the glow along its cables die together); the fallen
        /// array glinting in the new sunlight once the eclipse ends; and the radio tower ruin against the stars at
        /// night, with the habitat's power module lamp blinking beside it. Logs/world-captures/cue-*.png, with the
        /// array's glint levels in the log.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator OpeningCues_AreCapturedAtTheirFixedPoints()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath);
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
            Shoot(view, "cue-1-first-forward-view");

            IWorldAnchors anchors = context.Get<IWorldAnchors>();
            Vector3 lander = Anchor(anchors, WorldAnchorIds.PlaceLander).Position;
            Vector3 apron = Anchor(anchors, WorldAnchorIds.PlaceApron).Position;
            Vector3 lift = Vector3.up * OpeningLookHeight;
            Vector3 apronEye = apron + Vector3.up * OpeningEyeHeight;
            Transform array = FindNamed(CueArrayName);
            Transform tower = FindNamed(CueTowerName);
            var glint = FindNamed(CueGlintName).GetComponent<ArrayGlint>();
            Assert.IsNotNull(glint, "the fallen array glints");
            var host = new GameObject("CueReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                Place(camera, LookPose("cue-2-apron-lander-lit", apronEye, lander + lift, OpeningFov));
                Shoot(camera, "cue-2-apron-lander-lit");
                context.Events.Publish(new LanderReserveChanged(0f, false));
                yield return new WaitForSeconds(settings.Landmarks.LanderBeacon.ReserveDarkSeconds + SettleSeconds);
                Shoot(camera, "cue-2-apron-lander-dark");

                context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
                yield return new WaitForSeconds(SettleSeconds);
                // 07 on the array's approach from the base: the glint follows the view camera that chases it.
                Vector3 toArray = Vector3.ProjectOnPlane(array.position - apron, Vector3.up).normalized;
                Vector3 approach = array.position - toArray * CueArrayStandOff;
                context.Get<IRoverPlacement>().PlaceAt(OnGround(context.Get<ITerrainQuery>(),
                    new Vector2(approach.x, approach.z)), Quaternion.LookRotation(toArray, Vector3.up));
                yield return new WaitForSeconds(SettleSeconds);
                Debug.Log($"[cues] the array's glint after the eclipse from the approach: wing levels " +
                          $"{glint.LevelOf(0):F2} / {glint.LevelOf(1):F2}; sun at {SunReport(context)}");
                Shoot(view, "cue-3-array-glint");

                foreach (OpeningMilestone milestone in new[] { OpeningMilestone.PowerOn, OpeningMilestone.NightReady })
                {
                    context.Events.Publish(new OpeningMilestoneReached(milestone, true));
                }

                yield return new WaitForSeconds(SettleSeconds);
                // Low on the far side of the ruins from the base, looking back up at the tower: the night sky behind it,
                // the rest of the cluster out of the way.
                Vector3 toTower = Vector3.ProjectOnPlane(tower.position - apron, Vector3.up).normalized;
                Vector3 towerEye = tower.position + toTower * CueTowerStandOff + Vector3.up * CueLowEyeHeight;
                Place(camera, LookPose("cue-4-night-tower", towerEye, tower.position + Vector3.up * CueTowerLook,
                    OpeningFov));
                Shoot(camera, "cue-4-night-tower");

                var lamp = FindNamed(CueLampName).GetComponent<ModuleStatusLamp>();
                Transform lens = FindNamed(CueLampLens);
                Vector3 toLamp = Vector3.ProjectOnPlane(lens.position - apron, Vector3.up).normalized;
                Vector3 lampEye = lens.position - toLamp * CueLampStandOff + Vector3.up * CueLowEyeHeight;
                Place(camera, LookPose("cue-5-module-lamp-night", lampEye, lens.position, OpeningFov));
                started = Time.time;
                while (lamp.Level < CueLampPeak)
                {
                    Assert.Less(Time.time - started, CueLampTimeout, "the module lamp blinks at night");
                    yield return null;
                }

                Shoot(camera, "cue-5-module-lamp-night");
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
        /// The databank's story scan hosts from 07's chase camera on the drive up to each (Kestrel-3's hull panel, 06
        /// slumped at micro.06, 01 by the Peak road's rockfall), to check by eye that each reads and stands clear:
        /// Logs/world-captures/hosts-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator StoryHosts_AreCapturedFromTheDriveUp()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            LandmarkSettings landmarks = AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).Landmarks;
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
            var rover = context.Get<IRoverPlacement>();
            Camera view = context.Get<IViewCamera>().Camera;
            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            foreach (PropPlacement host in new[] { landmarks.KestrelPanel, landmarks.Elder06, landmarks.Elder01 })
            {
                Vector3 near = Anchor(anchors, host.Anchor).Position;
                var spot = new Vector2(near.x + host.Offset.x, near.z + host.Offset.y);
                Vector2 inward = spot.normalized;
                Vector2 back = spot - inward * HostApproachDistance + new Vector2(inward.y, -inward.x) * HostSideStep;
                rover.PlaceAt(OnGround(terrain, back), Quaternion.LookRotation(new Vector3(inward.x, 0f, inward.y)));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Shoot(view, "hosts-" + host.Id);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The basin floor's gentle relief at golden hour (owner, 2026-10-19), to check by eye for any dotted or holey
        /// pattern: the owner's view (07's chase camera on the plain by the first drive's cable, The Peak ahead), the
        /// plain from the apron looking south and west, the south wrinkle ridge in raking light from its lee flank, and
        /// the big old crater from above its north rim and down its boulder tracks, and the near-field rubble from
        /// 07's chase camera parked off the cable's lane to either side. Logs/world-captures/relief-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator Relief_IsCapturedInRakingLight()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            SurfaceSettings ground = AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).Surface;
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
            Vector3 peak = context.Get<IWorldLayout>().PeakPosition;
            Vector3 onCable = OnGround(terrain, Vector2.Lerp(new Vector2(wake.x, wake.z),
                new Vector2(lander.x, lander.z), ReliefCableShare));
            Vector3 apronEye = apron + Vector3.up * ScoutApronEyeHeight;

            WrinkleRidge ridge = ground.Relief.Ridges[0];
            Vector2 along = MoonSurface.BearingToDirection(ridge.Bearing);
            Vector2 middle = ridge.Start + along * (ridge.Length * 0.5f);
            // From the lee side, looking back at the steep face the golden-hour Sun only grazes.
            Vector2 left = new Vector2(-along.y, along.x);
            Vector2 flank = middle + (ridge.SteepToLeft ? left : -left) * ReliefRidgeStandOff;
            Vector3 ridgeEye = OnGround(terrain, flank) + Vector3.up * ReliefLowEyeHeight;
            Vector3 ridgeLook = OnGround(terrain, middle + along * ReliefRidgeLookAhead);

            BoulderTrack track = ground.Relief.Tracks[0];
            Vector2 down = (track.End - track.Start).normalized;
            Vector3 craterEye = OnGround(terrain, track.Start - down * ReliefCraterStandOff)
                                + Vector3.up * ReliefCraterEyeHeight;
            Vector3 craterLook = OnGround(terrain, track.End);
            Crater big = default;
            foreach (Crater crater in AssetDatabase.LoadAssetAtPath<WorldSettings>(OpeningSettingsPath).CreateSurface()
                         .Craters)
            {
                big = crater.Radius > big.Radius ? crater : big;
            }

            Vector3 bigCentre = OnGround(terrain, big.Center);
            Vector3 overviewEye = OnGround(terrain, big.Center + Vector2.up * (big.Radius * ReliefOverviewBack))
                                  + Vector3.up * ReliefOverviewHeight;

            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            var rover = context.Get<IRoverPlacement>();
            Vector3 toPeak = Vector3.ProjectOnPlane(peak - onCable, Vector3.up).normalized;
            rover.PlaceAt(onCable, Quaternion.LookRotation(toPeak, Vector3.up));
            Camera view = context.Get<IViewCamera>().Camera;
            var host = new GameObject("ReliefReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                yield return new WaitForSeconds(BlastSettleSeconds);
                Debug.Log($"[relief] golden hour: sun at {SunReport(context)}");
                Shoot(view, "relief-owner-view");
                foreach (Pose pose in new[]
                         {
                             LookPose("apron-south", apronEye, apronEye + Vector3.back * ReliefFar
                                 + Vector3.down * ReliefFarDrop, OpeningFov),
                             LookPose("apron-west", apronEye, apronEye + Vector3.left * ReliefFar
                                 + Vector3.down * ReliefFarDrop, OpeningFov),
                             LookPose("ridge-raking", ridgeEye, ridgeLook, OpeningFov),
                             LookPose("crater-tracks", craterEye, craterLook, OpeningFov),
                             LookPose("crater-overview", overviewEye, bigCentre, OpeningFov),
                         })
                {
                    Place(camera, pose);
                    Shoot(camera, "relief-" + pose.name);
                }

                var offLane = new Vector3(toPeak.z, 0f, -toPeak.x) * ReliefRubbleStandOff;
                foreach ((string side, Vector3 shift) in new[] { ("east", offLane), ("west", -offLane) })
                {
                    Vector3 spot = OnGround(terrain, new Vector2(onCable.x + shift.x, onCable.z + shift.z));
                    rover.PlaceAt(spot, Quaternion.LookRotation(toPeak, Vector3.up));
                    yield return new WaitForSeconds(BlastSettleSeconds);
                    Shoot(view, "relief-rubble-chase-" + side);
                }

                // A drive-by at 07's top speed through the west rubble: the stream keeps ahead of the chase camera and
                // the rubble's far edge thins in by its distance fade, never popping.
                Vector3 driveFrom = OnGround(terrain, new Vector2(onCable.x - offLane.x, onCable.z - offLane.z));
                Quaternion heading = Quaternion.LookRotation(toPeak, Vector3.up);
                for (int frame = 0; frame <= DriveByFrames; frame++)
                {
                    Vector3 passing = driveFrom + toPeak * (frame * DriveByStep);
                    rover.PlaceAt(OnGround(terrain, new Vector2(passing.x, passing.z)), heading);
                    yield return null;
                    if (frame % DriveByShotEvery == 0)
                    {
                        Shoot(view, $"relief-rubble-drive-{frame / DriveByShotEvery:00}");
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
        /// first drive's lane (nothing new may show); at golden hour, the ruins' front door from the trail's start, the
        /// retroreflector's glint from the apron and bootprint trail A; on the first night, the Scout's earthlit glint
        /// from the apron (07's chase view and a wide eye) and close up. The Sun stands where the opening's milestones
        /// leave it; the Scout's and the retroreflector's brightest pixels from the lane and the apron go to the log.
        /// Logs/world-captures/basin-*.png.
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
            SkyCycleSystem sky = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                sky = sky != null ? sky : root.GetComponentInChildren<SkyCycleSystem>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(sky, "the scene has its sky cycle");
            Transform scoutProp = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                scoutProp = scoutProp != null ? scoutProp : root.transform.Find("BasinProps/Prop_OldLander");
            }

            Assert.IsNotNull(scoutProp, "[World]/BasinProps/Prop_OldLander stands");
            Transform reflector = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                reflector = reflector != null ? reflector : root.transform.Find("BasinProps/Prop_Retroreflector");
            }

            Assert.IsNotNull(reflector, "[World]/BasinProps/Prop_Retroreflector stands");
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
            Vector3 apronEye = apron + Vector3.up * ScoutApronEyeHeight;
            Vector3 laneEye = onLane + Vector3.up * OpeningEyeHeight;
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
                Shoot(camera, LookPose("eclipse-lane", laneEye, scoutLook, OpeningFov));
                ReportPeaks(camera, scoutProp, "eclipse", laneEye, apronEye, scoutLook);
                ReportPeaks(camera, reflector, "eclipse, the retroreflector", laneEye, apronEye,
                    reflector.position + Vector3.up * ReflectorLookHeight);
                Shoot(camera, LookPose("eclipse-apron-glint", apronEye,
                    reflector.position + Vector3.up * ReflectorLookHeight, ScoutApronFov));

                context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Debug.Log($"[basin] golden hour: sun at {SunReport(context)}");
                Shoot(camera, LookPose("golden-front-door", doorEye, doorLook, ScoutApronFov));
                Shoot(camera, LookPose("golden-apron-scout", apronEye, scoutLook, ScoutApronFov));
                ReportPeaks(camera, scoutProp, "golden hour", laneEye, apronEye, scoutLook);
                Vector3 glint = reflector.position + Vector3.up * ReflectorLookHeight;
                Shoot(camera, LookPose("golden-apron-glint", apronEye, glint, ScoutApronFov));
                Shoot(view, "basin-golden-apron-chase");
                ReportPeaks(camera, reflector, "golden hour, the retroreflector", laneEye, apronEye, glint);
                Shoot(camera, LookPose("golden-trail", OnGround(terrain, TrailStart) + Vector3.up * ScoutApronEyeHeight,
                    OnGround(terrain, TrailBend) + Vector3.up * ScoutLookHeight, OpeningFov));

                context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, true));
                sky.SetClockTime(sky.TimeSeconds + FirstNightSeconds);
                rover.PlaceAt(apron, Quaternion.LookRotation(toScout, Vector3.up));
                yield return new WaitForSeconds(BlastSettleSeconds);
                Debug.Log($"[basin] first night: sun at {SunReport(context)}; the Scout at {scout}");
                Shoot(view, "basin-night-apron-chase");
                Shoot(camera, LookPose("night-apron", apronEye, scoutLook, ScoutApronFov));
                Shoot(camera, LookPose("night-close", closeEye, scoutLook, ScoutCloseFov));
                ReportPeaks(camera, scoutProp, "first night", laneEye, apronEye, scoutLook);
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

        /// <summary>
        /// What a prop adds to the view from the lane and from the apron, to the log: its brightest pixel and how
        /// much brighter than the ground behind it that pixel stands.
        /// </summary>
        private static void ReportPeaks(Camera camera, Transform prop, string light, Vector3 laneEye, Vector3 apronEye,
            Vector3 scout)
        {
            Place(camera, LookPose("peak", laneEye, scout, OpeningFov));
            Vector2 lane = Peak(camera, prop, scout);
            Place(camera, LookPose("peak", apronEye, scout, ScoutApronFov));
            Vector2 apron = Peak(camera, prop, scout);
            Debug.Log($"[basin] {light}: brightest pixel {lane.x:F3} (+{lane.y:F3} over the ground) from " +
                      $"the lane, {apron.x:F3} (+{apron.y:F3}) from the apron (luma 0..1, display-encoded)");
        }

        /// <summary>
        /// Within the peak window round <paramref name="at"/>, of the steady pixels <paramref name="prop"/> changes: the
        /// brightest luma with it (x) and the largest rise over the view without it (y).
        /// </summary>
        private static Vector2 Peak(Camera camera, Transform prop, Vector3 at)
        {
            Color[] with = Window(camera, at, out int width);
            Color[] again = Window(camera, at, out _);
            prop.gameObject.SetActive(false);
            Color[] without = Window(camera, at, out _);
            prop.gameObject.SetActive(true);
            var peak = Vector2.zero;
            for (int i = 0; i < with.Length && width > 0; i++)
            {
                // The stars twinkle from frame to frame: only pixels that hold still belong to the Scout.
                float lit = Luma(with[i]);
                float rise = lit - Luma(without[i]);
                if (Mathf.Abs(rise) > PeakChange && Mathf.Abs(lit - Luma(again[i])) < PeakChange)
                {
                    peak = new Vector2(Mathf.Max(peak.x, lit), Mathf.Max(peak.y, rise));
                }
            }

            return peak;
        }

        private static float Luma(Color c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private static Color[] Window(Camera camera, Vector3 at, out int width)
        {
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                Vector3 screen = camera.WorldToScreenPoint(at);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                int x0 = Mathf.Clamp(Mathf.RoundToInt(screen.x) - ScoutPeakWindow, 0, CaptureWidth - 1);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(screen.y) - ScoutPeakWindow, 0, CaptureHeight - 1);
                width = Mathf.Min(ScoutPeakWindow * 2, CaptureWidth - x0);
                int height = Mathf.Min(ScoutPeakWindow * 2, CaptureHeight - y0);
                return pixels.GetPixels(x0, y0, width, height);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                Object.Destroy(pixels);
                target.Release();
                Object.Destroy(target);
            }
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
