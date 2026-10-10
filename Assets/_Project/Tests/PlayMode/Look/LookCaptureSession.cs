using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Tests.PlayMode.Look
{
    /// <summary>
    /// The fixed "look" capture set of docs/features/M3-18 ("Process"): shoots the M3-09 title screen with its UI over
    /// a save, then boots the built Main scene on a fresh save, lets 07 wake, wakes home's power, waits for it to
    /// settle and shoots every other frame of look_set.json with the game camera's own lens, post-processing and anti-aliasing. Writes
    /// Logs/look-captures/&lt;stamp&gt;/&lt;frame&gt;.png; tools/look_report.py measures them. Needs a GPU: run with
    /// <c>python tools/look_report.py capture</c> (unity_batch tests --platform playmode --category LookCapture).
    /// </summary>
    [Explicit("Slow capture of the fixed look set in the real Main scene; run with --category LookCapture.")]
    [Category("LookCapture")]
    public sealed class LookCaptureSession
    {
        private const string SetPath = "Assets/_Project/Tests/PlayMode/Look/look_set.json";
        private const float WakeTimeout = 20f;

        private static readonly string OutputRoot =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "look-captures"));

        // A save with no sections, so the title opens on its menu (Continue) rather than the first boot's language step.
        private const string EmptySave =
            "{\"formatVersion\":1,\"contentVersion\":\"" + SaveService.OldestCompatibleContent + "\"," +
            "\"savedAtUtc\":\"2026-10-11T00:00:00Z\",\"sections\":[]}";

        private readonly LookCaptureScene _scene = new LookCaptureScene();
        private GameBootstrap _bootstrap;
        private readonly List<string> _problems = new List<string>();
        private IDisposable _awoke;
        private bool _awake;
        private RenderTexture _uiTarget;
        private PanelSettings _uiSettings;
        private RenderTexture _uiPreviousTarget;
        private bool _uiPreviousClear;
        private Color _uiPreviousClearValue;

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
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, true);
#endif
            Scene scene = SceneManager.GetSceneByPath(_scene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            if (_uiSettings != null)
            {
                _uiSettings.targetTexture = _uiPreviousTarget;
                _uiSettings.clearColor = _uiPreviousClear;
                _uiSettings.colorClearValue = _uiPreviousClearValue;
                _uiSettings = null;
            }

            if (_uiTarget != null)
            {
                _uiTarget.Release();
                Object.Destroy(_uiTarget);
            }

            BootstrapHarness.DeleteSaveFiles(_scene.SaveSlot);
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(LookCaptureScene))]
        [PostBuildCleanup(typeof(LookCaptureScene))]
        public IEnumerator LookSet_IsCaptured_InTheRealGame()
        {
            LookSet set = JsonUtility.FromJson<LookSet>(File.ReadAllText(SetPath));
            Assert.IsNotEmpty(set.frames, $"{SetPath} lists frames");
            string folder = Path.Combine(OutputRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            foreach (LookFrame frame in set.frames)
            {
                if (frame.kind == "title")
                {
                    yield return CaptureTitleScreen(set, Path.Combine(folder, frame.name + ".png"));
                }
            }

            BootstrapHarness.DeleteSaveFiles(_scene.SaveSlot);
            yield return Load(set);
            GameContext context = _bootstrap.Context;
            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            Camera view = context.Get<IViewCamera>().Camera;
            context.Events.Publish(new BasePowerChanged(BasePowerStage.Home, false));
            yield return new WaitForSeconds(set.homeSettleSeconds);
            var host = new GameObject("LookCaptureCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);
                foreach (LookFrame frame in set.frames)
                {
                    if (frame.kind == "title")
                    {
                        continue;
                    }

                    Place(camera, view, frame, context);
                    FrameCapture.SavePng(camera, set.width, set.height, Path.Combine(folder, frame.name + ".png"));
                    yield return null;
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Debug.Log("[look] captures " + folder);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// The M3-09 title screen as a returning player sees it: boots over a save with the title on (a test run
        /// otherwise skips it, see MoonProject.Editor.Automation.TestRunTitleSkip), lets the menu settle and shoots it
        /// with its UI. The rest of the set then boots fresh, as before.
        /// </summary>
        private IEnumerator CaptureTitleScreen(LookSet set, string path)
        {
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, false);
#endif
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory, _scene.SaveSlot + SaveService.Extension),
                EmptySave);
            yield return Load(set);
            Assert.AreEqual(GameFlowState.Title, _bootstrap.Context.Get<IGameFlow>().State, "the game boots into the title");
            yield return new WaitForSecondsRealtime(set.titleSeconds);
            CaptureWithUi(_bootstrap.Context.Get<IViewCamera>().Camera, set, path);
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, true);
#endif
        }

        /// <summary>Loads the scene copy (replacing any loaded one) and points its UI at the offscreen target.</summary>
        private IEnumerator Load(LookSet set)
        {
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(_scene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The look capture loads the scene through the editor.");
#endif
            _bootstrap = null;
            UIDocument document = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(_scene.ScenePath).GetRootGameObjects())
            {
                _bootstrap = _bootstrap != null ? _bootstrap : root.GetComponent<GameBootstrap>();
                document = document != null ? document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(_bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(document, "the scene has its UI document");
            RedirectUi(document, set.width, set.height);
        }

        /// <summary>
        /// Points the scene's PanelSettings at an offscreen target, in memory only (never saved; TearDown puts it
        /// back), so the title can be composited over the game camera's render: a batch editor has no screen to grab.
        /// The panel does not clear the screen it draws on, so the target is cleared to transparent each frame here;
        /// otherwise the curtain drawn opaque at boot stays under everything drawn after it.
        /// </summary>
        private void RedirectUi(UIDocument document, int width, int height)
        {
            if (_uiTarget == null)
            {
                _uiTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _uiTarget.Create();
            }

            if (_uiSettings == null)
            {
                _uiSettings = document.panelSettings;
                _uiPreviousTarget = _uiSettings.targetTexture;
                _uiPreviousClear = _uiSettings.clearColor;
                _uiPreviousClearValue = _uiSettings.colorClearValue;
            }

            _uiSettings.clearColor = true;
            _uiSettings.colorClearValue = Color.clear;
            _uiSettings.targetTexture = _uiTarget;
        }

        private void CaptureWithUi(Camera view, LookSet set, string path)
        {
            Texture2D world = FrameCapture.Render(view, set.width, set.height);
            var ui = new Texture2D(set.width, set.height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _uiTarget;
            ui.ReadPixels(new Rect(0, 0, set.width, set.height), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            try
            {
                Color32[] background = world.GetPixels32();
                Color32[] overlay = ui.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    background[i] = Over(overlay[i], background[i]);
                }

                world.SetPixels32(background);
                world.Apply(false);
                FrameCapture.WritePng(world, path);
            }
            finally
            {
                Object.DestroyImmediate(world);
                Object.DestroyImmediate(ui);
            }
        }

        /// <summary>The UI target holds premultiplied colour.</summary>
        private static Color32 Over(Color32 ui, Color32 world)
        {
            int keep = 255 - ui.a;
            return new Color32(
                (byte)Mathf.Min(255, ui.r + world.r * keep / 255),
                (byte)Mathf.Min(255, ui.g + world.g * keep / 255),
                (byte)Mathf.Min(255, ui.b + world.b * keep / 255),
                255);
        }

        private static void Place(Camera camera, Camera view, LookFrame frame, GameContext context)
        {
            camera.fieldOfView = frame.fov > 0f ? frame.fov : view.fieldOfView;
            switch (frame.kind)
            {
                case "view":
                    camera.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                    return;
                case "fixed":
                    Look(camera, ToVector(frame.position), ToVector(frame.lookAt));
                    return;
                case "fromBase":
                case "fromAnchor":
                    Assert.IsTrue(context.Get<IWorldAnchors>().TryGet(frame.anchor, out WorldAnchor anchor),
                        $"the world has anchor '{frame.anchor}' ({frame.name})");
                    Vector3 home = context.Get<IWorldLayout>().BasePosition;
                    Vector3 look = anchor.Position + Vector3.up * frame.lookHeight;
                    Vector3 toHome = Vector3.ProjectOnPlane(home - anchor.Position, Vector3.up).normalized;
                    Vector3 eye = frame.kind == "fromBase"
                        ? home - toHome * frame.eyeOffset + Vector3.up * frame.eyeHeight
                        : anchor.Position + toHome * frame.eyeOffset + Vector3.up * frame.eyeHeight;
                    Look(camera, eye, look);
                    return;
                default:
                    Assert.Fail($"{SetPath}: frame '{frame.name}' has unknown kind '{frame.kind}'");
                    return;
            }
        }

        private static void Look(Camera camera, Vector3 eye, Vector3 target)
        {
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
        }

        private static Vector3 ToVector(float[] values)
        {
            Assert.AreEqual(3, values?.Length ?? 0, "a position or lookAt has three numbers");
            return new Vector3(values[0], values[1], values[2]);
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

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {condition}");
            }
        }

        [Serializable]
        private sealed class LookSet
        {
            public int width = 1600;
            public int height = 900;
            public float titleSeconds;
            public float homeSettleSeconds;
            public LookFrame[] frames;
        }

        /// <summary>One frame: "title", "view" (the game camera once home is awake), "fixed" (position, lookAt),
        /// "fromBase" (from home toward an anchor) or "fromAnchor" (beside an anchor, on its home side).</summary>
        [Serializable]
        private sealed class LookFrame
        {
            public string name;
            public string kind;
            public float[] position;
            public float[] lookAt;
            public string anchor;
            public float eyeOffset;
            public float eyeHeight;
            public float lookHeight;
            public float fov;
        }
    }
}
