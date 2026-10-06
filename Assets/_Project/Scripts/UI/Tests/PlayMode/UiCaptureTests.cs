using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Screenshots of every UI state over the real Main scene (world, rover, sky, real camera), for taste review. The
    /// UI renders into its own target and is composited over the camera's frame. Gameplay state is staged through
    /// <see cref="FakeGameServices"/>; the scene's own save slot is never written (the UI saves to a test slot and the
    /// scene is torn down before play mode ends). Needs a GPU, takes a minute: run explicitly with
    /// <c>python tools/unity_batch.py tests --platform playmode --category UiCapture</c>. PNGs land in
    /// Logs/ui-captures.
    /// </summary>
    [Category("UiCapture")]
    [Explicit("Renders the full Main scene; run with --category UiCapture.")]
    public sealed class UiCaptureTests : InputTestFixture
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const int Width = 1600;
        private const int Height = 900;
        private const string OutputFolder = "Logs/ui-captures";

        private string _slot;
        private GameObject _uiHost;
        private GameObject _fakesHost;
        private PanelSettings _panel;
        private RenderTexture _target;
        private UiTuning _tuning;

        public override void TearDown()
        {
            Object.DestroyImmediate(_uiHost);
            Object.DestroyImmediate(_fakesHost);
            Object.DestroyImmediate(_panel);
            Object.DestroyImmediate(_tuning);
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            Scene scene = SceneManager.GetSceneByPath(MainScene);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            if (_slot != null)
            {
                BootstrapHarness.DeleteSaveFiles(_slot);
            }

            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator CaptureEveryUiState_OverTheMainScene()
        {
#if UNITY_EDITOR
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(MainScene,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(4f);
            GameBootstrap bootstrap = FindBootstrap();
            GameContext context = bootstrap.Context;
            Camera camera = context.Get<IViewCamera>().Camera;

            UISystem ui = BuildUi(context, out FakeGameServices fakes, out SaveService save);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);

            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(_tuning.Title.Delay + _tuning.Title.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "01_title");
            yield return new WaitForSecondsRealtime(_tuning.Title.Hold + _tuning.Title.Reveal.FadeOut + 0.5f);

            Transform view = camera.transform;
            Vector3 site = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 9f +
                           view.right * 1.5f + Vector3.down * 1.5f;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, site, true);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(camera, folder, "02_prompt_dig");

            fakes.PrimaryHint = InteractionHint.None;
            fakes.TetherState = TetherAimState.Hovering;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "03_reticle_hover");
            fakes.TetherState = TetherAimState.Idle;

            fakes.SetBalance(0);
            fakes.SetBalance(23);
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Capture(camera, folder, "04_scrap_chip");

            context.Events.Publish(new RelicDeposited("cassette_player", Vector3.zero, 3));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "05_memory_card");
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(1.5f);

            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.2f);
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_tuning.TowerPanel.HoldSeconds * 0.55f);
            yield return Capture(camera, folder, "06_tower_holding");
            Release(keyboard.eKey);
            fakes.SetBalance(6);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(camera, folder, "07_tower_short");
            fakes.AtStation = false;
            yield return new WaitForSecondsRealtime(1f);

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(camera, folder, "08_pause");
            Submit(ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "09_pause_settings");
            Submit(ui.Layout.LanguageButton);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(camera, folder, "10_pause_settings_vi");
            yield return Tap(keyboard.escapeKey);
            Submit(ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "11_pause_quit_vi");
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            context.Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 4));
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.AppearDelay +
                                                    _tuning.MemoryCard.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "12_memory_card_vi");
            Assert.IsTrue(save.SaveNow(), "the UI saved to its test slot");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private static GameBootstrap FindBootstrap()
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(MainScene).GetRootGameObjects())
            {
                var bootstrap = root.GetComponent<GameBootstrap>();
                if (bootstrap != null && bootstrap.Context != null)
                {
                    return bootstrap;
                }
            }

            throw new InvalidOperationException($"{MainScene} has no initialised GameBootstrap at its root.");
        }

        private UISystem BuildUi(GameContext context, out FakeGameServices fakes, out SaveService save)
        {
            _fakesHost = new GameObject("CaptureFakes");
            fakes = _fakesHost.AddComponent<FakeGameServices>();
            fakes.Upgrade = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(UiTestRig.UpgradePath);
            fakes.Initialize(new GameContext(context.Events, context.Input));

            _target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            _panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiTestRig.PanelSettingsPath));
            _panel.targetTexture = _target;
            _panel.clearColor = true;
            _panel.colorClearValue = Color.clear;
            _tuning = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UiTuning>(UiTestRig.TuningPath));

            _uiHost = new GameObject("CaptureUI");
            _uiHost.SetActive(false);
            var document = _uiHost.AddComponent<UIDocument>();
            document.panelSettings = _panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiTestRig.UxmlPath);
            var ui = _uiHost.AddComponent<UISystem>();
            ui.Wire(document, _tuning, AssetDatabase.LoadAssetAtPath<RelicCatalog>(UiTestRig.CatalogPath),
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.EnglishPath),
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.VietnamesePath),
                });
            ui.QuitAction = () => { };
            _uiHost.SetActive(true);

            _slot = BootstrapHarness.NewTestSlot();
            save = new SaveService(SaveService.DefaultDirectory, _slot);
            ui.Initialize(new UiServices(context.Events, context.Input, context.Get<IViewCamera>(),
                context.Get<IAudioSettings>(), context.Get<ILookSettings>(), save, fakes, fakes, fakes, fakes));
            save.Load();
            return ui;
        }

        private IEnumerator Capture(Camera camera, string folder, string name)
        {
            yield return null;
            Texture2D world = FrameCapture.Render(camera, Width, Height);
            var ui = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
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
                string path = Path.Combine(folder, name + ".png");
                FrameCapture.WritePng(world, path);
                Debug.Log($"[ui-capture] {path}");
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        /// <summary>UI Toolkit writes premultiplied colour: result = ui + world x (1 - ui alpha).</summary>
        private static Color32 Over(Color32 ui, Color32 world)
        {
            int keep = 255 - ui.a;
            return new Color32(
                (byte)Mathf.Min(255, ui.r + world.r * keep / 255),
                (byte)Mathf.Min(255, ui.g + world.g * keep / 255),
                (byte)Mathf.Min(255, ui.b + world.b * keep / 255),
                255);
        }
#endif

        private static void Submit(VisualElement target)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = target;
                target.SendEvent(submit);
            }
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }
    }
}
