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
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-08T00:00:00Z\"," +
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

        // The Playthrough's step-16 review frame: from 3 m above the pad toward the canyon lip (2 m up), game lens.
        private const float CanyonViewEyeHeight = 3f;
        private const float CanyonViewLookHeight = 2f;

        // The orange vent's bead field (M3-27) from home, and from a chase-like distance on the way to it.
        private const float VentCloseDistance = 22f;
        private const float VentCloseHeight = 5f;

        // Kestrel-3's scrap field (0.5.5): looking down the furrow from its far end toward the crater.
        private const float ScrapFieldLookAhead = 25f;

        // After power reaches Home (seconds since the previous frame): the cable halfway, the windows half-lit with
        // the porch lamps starting, and home settled.
        private static readonly (float Wait, string Name)[] WakeFrames =
        {
            (0.8f, "wake-1-cable"),
            (2.4f, "wake-2-windows"),
            (8f, "wake-3-home"),
        };

        // The whole base awake (the owner's frame is the woken base at night): power stepped to the Lift, settled.
        private const float LiftSettleSeconds = 8f;

        // The tower's build beat as Gameplay publishes it (feel-pass §8.2), and the flare seen just after Impact.
        private const string TowerPilotName = "TowerPilot";

        // The cable frame: home's power cable seen from the chase camera's distance and height.
        private const string CableFrame = "wake-1-cable";
        private const string HomeCableName = "PowerCable_Home";
        private const float CableChaseDistance = 10f;
        private const float CableChaseHeight = 3.5f;
        private const float TowerAnticipateSeconds = 1.2f;
        private const int TowerActSteps = 4;
        private const float TowerActSeconds = 0.25f;
        private const float TowerFlareCaptureSeconds = 0.25f;

        // The reach ring (polish wave 1): the tower's reach grows from a token radius to this, seen mid roll-out.
        private const float RingBaselineRadius = 1f;
        private const float RingReach = 70f;
        private const float RingCaptureSeconds = 0.8f;

        // The network's handshake (north-star beat 7): the four basin masts restored in the save, the farthest lit one
        // lit again, seen from high behind home looking out along the chain, at three moments of the pulse.
        private const string BasinMastsSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.5\",\"savedAtUtc\":\"2026-10-13T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.relays\",\"version\":1,\"json\":\"{\\\"masts\\\":[" +
            "{\\\"id\\\":\\\"relay.0\\\",\\\"part\\\":true,\\\"paid\\\":60,\\\"restored\\\":true}," +
            "{\\\"id\\\":\\\"relay.1\\\",\\\"part\\\":true,\\\"paid\\\":60,\\\"restored\\\":true}," +
            "{\\\"id\\\":\\\"relay.2\\\",\\\"part\\\":true,\\\"paid\\\":60,\\\"restored\\\":true}," +
            "{\\\"id\\\":\\\"relay.3\\\",\\\"part\\\":true,\\\"paid\\\":60,\\\"restored\\\":true}" +
            "]}\"}]}";

        private const float PulseViewBack = 70f;
        private const float PulseViewHeight = 45f;
        private const float PulseViewFov = 50f;
        private const string DockName = "DockAnchor";
        private const float DockEyeHeight = 2.5f;
        private const float PulseCaptureHop = 0.5f;
        private const string WorldSettingsPath = "Assets/_Project/Data/Tuning/WorldSettings.asset";

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
            Assert.IsTrue(context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip),
                "the World has the canyon lip");
            poses.Add(LookPose("toward_the_canyon",
                context.Get<IWorldLayout>().BasePosition + Vector3.up * CanyonViewEyeHeight,
                lip.Position + Vector3.up * CanyonViewLookHeight, view.fieldOfView));
            Assert.IsTrue(context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.PlaceOrangeVent, out WorldAnchor vent),
                "the World has the orange vent");
            Vector3 home = context.Get<IWorldLayout>().BasePosition;
            Vector3 toVent = Vector3.ProjectOnPlane(vent.Position - home, Vector3.up).normalized;
            poses.Add(LookPose("orange_vent_from_base", home + Vector3.up * CanyonViewEyeHeight, vent.Position,
                view.fieldOfView));
            Assert.IsTrue(context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.KestrelTrail, out WorldAnchor trail),
                "the World has Kestrel-3's trail");
            poses.Add(LookPose("kestrel_scrap_field",
                trail.Position - trail.Forward * VentCloseDistance * 0.5f + Vector3.up * VentCloseHeight,
                trail.Position + trail.Forward * ScrapFieldLookAhead, view.fieldOfView));
            poses.Add(LookPose("orange_vent_close",
                vent.Position - toVent * VentCloseDistance + Vector3.up * VentCloseHeight, vent.Position,
                view.fieldOfView));
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
        /// The dark start and home's wake (M3-16), from the game camera's own spawn view on a fresh save: asleep (only
        /// the dock, 07's eye and the tower's pilot warm), power travelling the cable, the windows waking, and home
        /// settled, then the whole base awake at the Lift and its reach ring rolling out. Writes
        /// Logs/world-captures/wake-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator HomeWake_IsCapturedFromTheSpawnView()
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
            Assert.AreEqual(0f, context.Get<IHomeLights>().Level(HomeLight.Windows), "a fresh game starts asleep");
            Capture(view, "wake-0-asleep");

            // The tower comes online the way Gameplay plays it (feel-pass §8.2): the stage arrives with the build
            // beat, the top light flares on Impact, and the cable race and the windows follow its Follow (0).
            Vector3 tower = FindNamed(TowerPilotName).position;
            Publish(context, FeelPhase.Anticipate, tower, 0f, TowerAnticipateSeconds);
            context.Events.Publish(new BasePowerChanged(BasePowerStage.Home, false));
            yield return new WaitForSeconds(TowerAnticipateSeconds);
            for (int step = 1; step <= TowerActSteps; step++)
            {
                Publish(context, FeelPhase.Act, tower, (float)step / TowerActSteps, 0f);
                yield return new WaitForSeconds(TowerActSeconds);
            }

            Publish(context, FeelPhase.Impact, tower, 1f, 0f);
            yield return new WaitForSeconds(TowerFlareCaptureSeconds);
            Capture(view, "wake-0b-tower-flare");
            Publish(context, FeelPhase.Follow, tower, 0f, 0f);
            foreach ((float wait, string name) in WakeFrames)
            {
                yield return new WaitForSeconds(wait);
                if (name == CableFrame)
                {
                    CaptureCable(view, name);
                    continue;
                }

                Capture(view, name);
            }

            Assert.AreEqual(1f, context.Get<IHomeLights>().Level(HomeLight.Halo), "home has settled");
            Publish(context, FeelPhase.Follow, tower, 1f, 0f);
            Publish(context, FeelPhase.Anticipate, tower, 0f, TowerAnticipateSeconds);
            context.Events.Publish(new BasePowerChanged(BasePowerStage.Chart, false));
            Publish(context, FeelPhase.Impact, tower, 1f, 0f);
            Publish(context, FeelPhase.Follow, tower, 0f, 0f);
            Publish(context, FeelPhase.Follow, tower, 1f, 0f);
            yield return new WaitForSeconds(LiftSettleSeconds);
            Capture(view, "wake-4-lift");
            context.Events.Publish(new SignalRadiusChanged(RingBaselineRadius));
            context.Events.Publish(new SignalRadiusChanged(RingReach));
            yield return new WaitForSeconds(RingCaptureSeconds);
            Capture(view, "wake-5-reach-ring");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The network's handshake (north-star §4a beat 7) from a distance: with the basin's masts restored, the
        /// farthest lit mast lights again and the pulse runs lamp to lamp back to home and out. Captures mid-hop on the
        /// first link, mid-way through the chain and as it reaches home, from high behind home and from the dock (the
        /// player's view home), to Logs/world-captures/network-pulse-*.png.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator NetworkPulse_IsCapturedMidHandshake_FromAfar()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                CanyonSessionScene.SaveSlot + SaveService.Extension), BasinMastsSave);
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
            IStationReach reach = context.Get<IStationReach>();
            Vector3 home = reach.GetNode(0).LampPosition;
            int origin = -1;
            Vector3 centre = Vector3.zero;
            for (int i = 1; i < reach.NodeCount; i++)
            {
                RelayNode node = reach.GetNode(i);
                if (!node.Lit)
                {
                    continue;
                }

                centre += node.LampPosition;
                bool farther = origin < 0 || (node.LampPosition - home).sqrMagnitude
                    > (reach.GetNode(origin).LampPosition - home).sqrMagnitude;
                origin = farther ? i : origin;
            }

            Assert.Greater(origin, 0, "the save lights at least one mast");
            centre /= reach.LitCount - 1;
            var hops = new List<NetworkPulseReached>();
            using (context.Events.Subscribe<NetworkPulseReached>(hops.Add))
            {
                RelayNode lit = reach.GetNode(origin);
                context.Events.Publish(new RelayRestored(lit.Id, lit.LampPosition, reach.LitCount - 1,
                    reach.NodeCount - 1, "home", 1f));
                for (int i = 0; i < reach.NodeCount; i++)
                {
                    RelayNode node = reach.GetNode(i);
                    Debug.Log($"[network-pulse] {node.Id} lit {node.Lit} lamp {node.LampPosition}");
                }

                Vector3 outward = Vector3.ProjectOnPlane(centre - home, Vector3.up).normalized;
                Vector3 eye = home - outward * PulseViewBack + Vector3.up * PulseViewHeight;
                Pose pose = LookPose("network_pulse", eye, Vector3.Lerp(home, centre, 0.5f), PulseViewFov);
                Camera view = context.Get<IViewCamera>().Camera;
                Vector3 dockEye = FindNamed(DockName).position + Vector3.up * DockEyeHeight;
                Pose dock = LookPose("network_pulse_dock", dockEye, centre, view.fieldOfView);
                float hop = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldSettingsPath).NetworkPulse.HopSeconds;
                yield return new WaitForSeconds(PulseCaptureHop * hop);
                CapturePose(view, pose, "network-pulse-1-first-link");
                CapturePose(view, dock, "network-pulse-dock-1-first-link");
                yield return new WaitForSeconds(hop);
                CapturePose(view, pose, "network-pulse-2-chain");
                CapturePose(view, dock, "network-pulse-dock-2-chain");
                while (!hops.Exists(reached => reached.NodeId == "home"))
                {
                    Assert.Less(Time.time - started, WakeTimeout * 10f, "the handshake reaches home");
                    yield return null;
                }

                yield return new WaitForSeconds(PulseCaptureHop * hop);
                CapturePose(view, pose, "network-pulse-3-home");
                CapturePose(view, dock, "network-pulse-dock-3-home");
            }

            Debug.Log($"[network-pulse] lit masts {reach.LitCount - 1}, origin {reach.GetNode(origin).Id}, " +
                $"hops so far {hops.Count}, farthest hop {hops[0].Hops}");
            Assert.AreEqual(0, hops[0].Hop, "the handshake starts at the new mast");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static void Publish(GameContext context, FeelPhase phase, Vector3 tower, float strength, float seconds)
        {
            context.Events.Publish(new FeelBeat(FeelKind.Build, phase, tower, Vector3.up, strength, seconds));
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

        /// <summary>
        /// The power pulse on home's cable, from the chase distance: behind the cable along the game camera's heading,
        /// at the chase camera's height, looking at the middle of its glass beads.
        /// </summary>
        private static void CaptureCable(Camera view, string name)
        {
            var beads = new Bounds();
            bool any = false;
            foreach (Renderer bead in FindNamed(HomeCableName).GetComponentsInChildren<Renderer>())
            {
                if (any)
                {
                    beads.Encapsulate(bead.bounds);
                }
                else
                {
                    beads = bead.bounds;
                    any = true;
                }
            }

            Vector3 heading = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            Vector3 eye = beads.center - heading * CableChaseDistance + Vector3.up * CableChaseHeight;
            var host = new GameObject("CableReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(beads.center - eye));
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private static void CapturePose(Camera view, Pose pose, string name)
        {
            var host = new GameObject("PulseReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                Place(camera, pose);
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private static void Capture(Camera view, string name)
        {
            var host = new GameObject("WakeReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                    Path.Combine(CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
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
