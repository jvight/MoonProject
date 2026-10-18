using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.Universal;
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
    /// scene is torn down before play mode ends). Needs a GPU, takes a
    /// few minutes: run explicitly with
    /// <c>python tools/unity_batch.py tests --platform playmode --category UiCapture</c>. PNGs land in
    /// Logs/ui-captures.
    /// </summary>
    [Category("UiCapture")]
    [Explicit("Renders the full Main scene; run with --category UiCapture.")]
    public sealed partial class UiCaptureTests : InputTestFixture
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const int Width = 1600;
        private const int Height = 900;
        private const string OutputFolder = "Logs/ui-captures";
        private const float SceneSettleSeconds = 4f;

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
                UiTestRig.DeleteSlotFiles(_slot);
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
            yield return LoadMain();
            GameBootstrap bootstrap = FindBootstrap();
            GameContext context = bootstrap.Context;
            Camera camera = context.Get<IViewCamera>().Camera;

            UISystem ui = BuildUi(context, out FakeGameServices fakes, out SaveService save);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);

            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            yield return Capture(camera, folder, "01_title");
            Submit(ui.Layout.ResumeButton);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(_tuning.Title.Reveal.FadeOut + 0.5f);

            Transform view = camera.transform;
            Vector3 site = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 9f +
                           view.right * 1.5f + Vector3.down * 1.5f;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, site, true);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture(camera, folder, "02_prompt_build");

            fakes.PrimaryHint = InteractionHint.None;
            fakes.SetMaterials(4, 2, 1);
            fakes.SetMaterials(7, 2, 3);
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds * 0.5f);
            yield return Capture(camera, folder, "04_pickup_feed_gain");
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds);

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(camera, folder, "12_pause");
            Assert.AreEqual(1f, ui.Backdrop.Weight, 0.01f, "the frame behind the pause menu is blurred");
            Submit(ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "13_pause_settings");
            Submit(ui.Layout.LanguageButton);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(camera, folder, "14_pause_settings_vi");
            yield return Tap(keyboard.escapeKey);
            Submit(ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture(camera, folder, "15_pause_quit_vi");
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            Assert.IsTrue(save.SaveNow(), "the UI saved to its test slot");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The title's New Game question over a save, resting on Keep this moon. Rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureNewGameQuestion</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureNewGameQuestion()
        {
#if UNITY_EDITOR
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn);
            Submit(ui.Layout.NewGameButton);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(ui.Pause.IsAskingNewGame);
            yield return Capture(camera, folder, "39_title_new_game");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(ui.Pause.IsTitle && !ui.Pause.IsAskingNewGame, "Esc keeps this moon, back to the menu");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The first boot's title (no save): the language is asked with a heading, and a New Game press that comes
        /// before the answer asks again. Rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureFirstBootTitle</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFirstBootTitle()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(false));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn);
            Assert.IsTrue(ui.Pause.IsChoosingLanguage);
            Submit(ui.Layout.NewGameButton);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(ui.Pause.IsChoosingLanguage, "New Game before the answer asks the language again");
            yield return Capture(camera, folder, "40_title_first_boot");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// A place's card the first time 07 comes near, then the soft next line once 07 rests, the opening's protocol
        /// readout and a recipe card while looking at the tower's hopper (M3-24 B). Rerun on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureGuidance</c>.
        /// </summary>
        /// <summary>
        /// The feel pass's UI (docs/design/feel-pass.md): the stock count rolling up a unit at a time with a tick event
        /// per unit (a second collect carries the roll on), and the hold ring's charge arc filling on a cut's
        /// Anticipate beat, then handing over to the progress fill at ignition. Captures 70-73. Rerun with
        /// --filter CaptureFeelPass.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFeelPass_TheRollAndTheChargingRing()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);

            var ticks = new List<StockCountTicked>();
            using (context.Events.Subscribe<StockCountTicked>(ticks.Add))
            {
                fakes.SetMaterials(2, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.LingerSeconds * 0.3f);
                Assert.AreEqual(2, ui.Pickups.Shown(SalvageMaterial.Metal));
                ticks.Clear();

                fakes.SetMaterials(8, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.StepSeconds * 2.5f);
                int midway = ui.Pickups.Shown(SalvageMaterial.Metal);
                Assert.That(midway, Is.InRange(3, 7), "the count is rolling, not jumping");
                yield return Capture(camera, folder, "70_stock_rolling");
                fakes.SetMaterials(10, 0, 0);
                yield return new WaitForSecondsRealtime(_tuning.PickupFeed.StepSeconds * 12f);
                Assert.AreEqual(10, ui.Pickups.Shown(SalvageMaterial.Metal));
                yield return Capture(camera, folder, "71_stock_rolled");

                // The scene's own UI answers the same stock event, so each tick arrives once per UI: count units.
                var units = new SortedDictionary<int, int>();
                foreach (StockCountTicked tick in ticks)
                {
                    units[tick.Index] = tick.Remaining;
                }

                Assert.AreEqual(8, units.Count, "a tick for each of the eight units from 2 to 10");
                int expected = 0;
                foreach (KeyValuePair<int, int> unit in units)
                {
                    Assert.AreEqual(expected, unit.Key, "the index keeps climbing through a second collect");
                    expected++;
                }

                Assert.AreEqual(0, units[7], "the last unit has none to come: the roll lands on 10");
            }

            Camera siteCamera = camera;
            Transform view = camera.transform;
            Vector3 cut = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 6f +
                          Vector3.down * 1.2f;
            fakes.Camera = siteCamera;
            fakes.HasTarget = true;
            fakes.CutPoint = cut;
            fakes.Material = SalvageMaterial.Wiring;
            for (int warm = 0; warm < 5; warm++)
            {
                yield return null;
            }

            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, cut, Vector3.up, 0f, 0.65f,
                SalvageMaterial.Wiring));
            float last = 0f;
            bool seenFilling = false;
            float end = Time.realtimeSinceStartup + 2f;
            while (ui.SalvageRing.Charge < 1f && Time.realtimeSinceStartup < end)
            {
                float now = ui.SalvageRing.Charge;
                Assert.GreaterOrEqual(now, last, "the charge only fills");
                last = now;
                if (!seenFilling && now > 0.3f)
                {
                    seenFilling = true;
                    Assert.IsTrue(ui.SalvageRing.IsVisible, "the ring is up for the charge, before any progress");
                    yield return Capture(siteCamera, folder, "72_ring_charging");
                }

                yield return null;
            }

            Assert.IsTrue(seenFilling, "the charge segment was seen filling");
            Assert.AreEqual(1f, ui.SalvageRing.Charge, 1e-3f, "full by the end of the beat");
            fakes.IsCutting = true;
            fakes.CutProgress = 0.3f;
            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Act, cut, Vector3.up, 0f, 0f,
                SalvageMaterial.Wiring));
            yield return new WaitForSecondsRealtime(_tuning.Salvage.ChargeHandoffSeconds * 0.4f);
            Assert.AreEqual(0.3f, ui.SalvageRing.Progress, 0.02f, "the progress fill follows from ignition");
            yield return Capture(siteCamera, folder, "73_ring_ignited");
            yield return new WaitForSecondsRealtime(_tuning.Salvage.ChargeHandoffSeconds);
            Assert.AreEqual(0f, ui.SalvageRing.Charge, "the charge has handed over");

            fakes.IsCutting = false;
            context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Cancel, cut, Vector3.up, 0.3f, 0f,
                SalvageMaterial.Wiring));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(0.3f, ui.SalvageRing.Progress, 0.02f, "releasing keeps the progress, still on the ring");
            Assert.IsTrue(ui.SalvageRing.IsVisible);
            fakes.HasTarget = false;
            fakes.CutProgress = 0f;
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The save tick after a GameSaved (capture 80). Rerun with --filter CaptureSaveTick.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureSaveTick()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            FreezeWorld();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);

            context.Events.Publish(new GameSaved());
            yield return new WaitForSecondsRealtime(_tuning.SaveTick.Reveal.FadeIn + 0.2f);
            Assert.IsTrue(ui.SaveTick.IsVisible, "the tick is up after a save");
            yield return Capture(camera, folder, "80_save_tick");
            yield return new WaitForSecondsRealtime(_tuning.SaveTick.HoldSeconds + _tuning.SaveTick.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(ui.SaveTick.IsVisible, "and gone a moment later");

#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>The settings' three tabs over the title (captures 92-94). Rerun with --filter CaptureSettingsTabs.</summary>
        [UnityTest]
        public IEnumerator CaptureSettingsTabs_SoundCameraDisplay()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            Submit(ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(_tuning.Pause.SettingsPanel.FadeIn + 0.5f);
            yield return Capture(camera, folder, "92_settings_sound");
            Submit(ui.Layout.TabCamera);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture(camera, folder, "93_settings_camera");
            Submit(ui.Layout.TabDisplay);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture(camera, folder, "94_settings_display");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// The cargo screen's pages, the unlock card, a lunar pickup row and the build prompt (captures 94-99). Rerun with
        /// --filter CaptureCargoScreen.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureCargoScreen_HoldDevices_UnlockCard_AndALunarPickup()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            Reframe(1920, 1080);
            FreezeWorld();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            string logsFolder = Path.GetFullPath("Logs");
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(logsFolder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(0.5f);
            fakes.Hold.Set(3, new MaterialSlot(SalvageMaterial.Metal, 4, 4), new MaterialSlot(SalvageMaterial.Metal, 2, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 3, 4), new MaterialSlot(SalvageMaterial.Optics, 1, 4),
                new MaterialSlot(SalvageMaterial.Ilmenite, 5, 6));
            fakes.Hold.WaitingBundles = 3;
            fakes.Hold.AddPart("part.igniter");
            ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + _tuning.Cargo.BootTypeSeconds +
                                                    _tuning.Cargo.BootLightStepSeconds * 3f);
            Assert.IsTrue(ui.Cargo.IsBooting, "the first press boots the hold");
            yield return Capture(camera, folder, "94_hold_boot_lights");
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(ui.Cargo.IsOpen && !ui.Cargo.IsBooting);
            yield return Capture(camera, folder, "95_cargo_screen");
            yield return Capture(camera, folder, "96_seven_tab_cutter_fault");
            yield return Capture(camera, logsFolder, "tab_field_1920x1080");
            fakes.Systems.CanFitIgniter = true;
            ui.Cargo.View.FocusDevice(FaceDevices.Cutter);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(ui.Cargo.View.IsIgniterOffered, "on the pad the cutter offers the spare igniter");
            yield return Capture(camera, folder, "96b_seven_tab_igniter_offered");
            yield return Capture(camera, logsFolder, "tab_pad_1920x1080");
            ui.Cargo.View.SubmitFocused();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "96c_seven_tab_cutter_ok");
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.6f);

            ui.UnlockCard.Learned(StructureIds.ChargingPad);
            yield return new WaitForSecondsRealtime(_tuning.UnlockCard.Reveal.FadeIn + 0.8f);
            Assert.IsTrue(ui.UnlockCard.IsVisible);
            yield return Capture(camera, folder, "97_unlock_card");
            yield return new WaitForSecondsRealtime(_tuning.UnlockCard.HoldSeconds + _tuning.UnlockCard.Reveal.FadeOut);

            fakes.SetUnits(SalvageMaterial.Ilmenite, 8);
            fakes.SetUnits(SalvageMaterial.WaterIce, 7);
            yield return new WaitForSecondsRealtime(_tuning.PickupFeed.MaxCountSeconds * 0.5f + 0.4f);
            yield return Capture(camera, folder, "98_pickup_lunar");

            Transform view = camera.transform;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Build,
                view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 6f + Vector3.down * 1.2f,
                true);
            yield return new WaitForSecondsRealtime(_tuning.Prompts.Reveal.FadeIn + 1.5f);
            yield return Capture(camera, folder, "99_build_prompt");
            fakes.PrimaryHint = InteractionHint.None;
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>The credits over the title (capture 90). Rerun with --filter CaptureCredits.</summary>
        [UnityTest]
        public IEnumerator CaptureCredits_OverTheTitle()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            Submit(ui.Layout.CreditsButton);
            yield return new WaitForSecondsRealtime(_tuning.Credits.Reveal.FadeIn + 0.5f);
            Assert.IsTrue(ui.Pause.IsShowingCredits);
            yield return Capture(camera, folder, "90_credits_top");
            ui.Layout.CreditsScroll.scrollOffset = new Vector2(0f, 700f);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture(camera, folder, "91_credits_fonts_and_voice");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// Captures TITLE-SCREEN v1 in Vietnamese and English at 1920x1080, Vietnamese at 1280x720, and the three
        /// standalone animation frames (t = 1.1s, 5.1s, 9.0s) into Logs/. Rerun with --filter CaptureTitleScreenV1.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureTitleScreenV1()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            FreezeWorld();
            yield return null;
            string logsFolder = Path.GetFullPath("Logs");
            Directory.CreateDirectory(logsFolder);

            Reframe(1920, 1080);
            ui.Localization.SetLanguage("vi");
            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.3f);

            ui.Pause.SetTitleAnimationTime(1.1f, false);
            yield return Capture(camera, logsFolder, "title_vi_1920x1080");

            ui.Pause.SetTitleAnimationTime(5.1f, false);
            yield return Capture(camera, logsFolder, "title_vi_5_1s_1920x1080");

            ui.Localization.SetLanguage("en");
            ui.Pause.SetTitleAnimationTime(1.1f, false);
            yield return Capture(camera, logsFolder, "title_en_1920x1080");

            Reframe(1280, 720);
            ui.Localization.SetLanguage("vi");
            ui.Pause.SetTitleAnimationTime(1.1f, false);
            yield return Capture(camera, logsFolder, "title_vi_1280x720");

            ui.Pause.SetTitleAnimationTime(1.1f, true);
            yield return Capture(camera, logsFolder, "title_anim_1_1s");

            ui.Pause.SetTitleAnimationTime(5.1f, true);
            yield return Capture(camera, logsFolder, "title_anim_5_1s");

            ui.Pause.SetTitleAnimationTime(9.0f, true);
            yield return Capture(camera, logsFolder, "title_anim_9_0s");
            Debug.Log($"[title-perf] TitleScenePainter.LastPaintMilliseconds = {ui.Pause.TitleScenePaintMs:F3} ms");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        /// <summary>
        /// 07's readout and the objective line (docs/design/opening-v2.md sections 2, 3, 10): the boot diagnostics are
        /// the one guidance element while they show and the objective waits for them, the cutter refusing flashes its
        /// line, then the protocol line, the lander's reserve, a scanned panel's text and the Sun's arc. Rerun with
        /// --filter CaptureGuidance.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureGuidance()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            context.Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            context.Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle));
            context.Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Line.FadeIn +
                                                    _tuning.Guidance.ObjectiveDelaySeconds + 2.5f);
            Assert.IsFalse(ui.Readout.IsVisible, "no boot card: the diagnostics were on 07's face");
            Assert.IsTrue(ui.Objective.ShowsObjective, "the first objective line stands alone");
            yield return Capture(camera, folder, "49_objective_line");

            context.Events.Publish(new CutterRefused(Vector3.zero));
            yield return null;
            Assert.AreEqual(ReadoutKind.System, ui.Readout.Showing, "a refused cut shows the cutter's card");
            Assert.IsTrue(ui.Readout.IsFlashing(0), "and flashes its line");
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Readout.FadeIn + 0.5f);
            yield return Capture(camera, folder, "49a_cutter_refused");

            yield return new WaitForSecondsRealtime(_tuning.Guidance.StatusHoldSeconds +
                                                    _tuning.Guidance.Readout.FadeOut + 0.6f);
            Assert.IsFalse(ui.Readout.IsVisible, "the cutter's card fades by itself");

            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, false));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Readout.FadeIn + 0.5f);
            Assert.AreEqual(ReadoutKind.Protocol, ui.Readout.Showing);
            yield return Capture(camera, folder, "49c_protocol_power_on");
            fakes.Sun.ElevationDegrees = 9f;
            yield return new WaitForSecondsRealtime(_tuning.Guidance.StatusHoldSeconds +
                                                    _tuning.Guidance.Readout.FadeOut + _tuning.Guidance.SunArc.FadeIn);
            Assert.IsTrue(ui.SunArc.IsVisible, "the Sun's arc comes with the power");
            yield return Capture(camera, folder, "49d_sun_arc");

            context.Events.Publish(new LanderReserveChanged(0f, false));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Readout.FadeIn + 0.5f);
            Assert.AreEqual(ReadoutKind.Reserve, ui.Readout.Showing);
            yield return Capture(camera, folder, "49e_lander_reserve");
            yield return new WaitForSecondsRealtime(_tuning.Guidance.StatusHoldSeconds +
                                                    _tuning.Guidance.Readout.FadeOut + 0.5f);

            context.Events.Publish(new StoryScanned("airlock_panel", "story.airlock_panel", Vector3.zero));
            yield return new WaitForSecondsRealtime(_tuning.Guidance.Readout.FadeIn + 0.5f);
            Assert.AreEqual(ReadoutKind.Story, ui.Readout.Showing);
            yield return Capture(camera, folder, "49f_story_panel");
            ui.Localization.SetLanguage("vi");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture(camera, folder, "49g_story_panel_vi");
            ui.Localization.SetLanguage("en");
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Holds the live world still: its own sonar, home and rover would answer over the scripted moments, and the
        /// UI runs on unscaled time. Restored in <see cref="TearDown"/>.
        /// </summary>
        private static void FreezeWorld()
        {
            Time.timeScale = 0f;
        }

        private static IEnumerator LoadMain()
        {
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(MainScene,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(SceneSettleSeconds);
        }

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

        private static Transform ActiveDescendant(Transform node, string name)
        {
            if (!node.gameObject.activeInHierarchy)
            {
                return null;
            }

            if (node.name == name)
            {
                return node;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform hit = ActiveDescendant(node.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Renders the UI at another frame size from now on (the panel lays itself out for it).</summary>
        private void Reframe(int width, int height)
        {
            RenderTexture previous = _target;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            _panel.targetTexture = _target;
            previous.Release();
            Object.DestroyImmediate(previous);
        }

        private UISystem BuildUi(GameContext context, out FakeGameServices fakes, out SaveService save)
        {
            foreach (UISystem existing in Object.FindObjectsByType<UISystem>(FindObjectsSortMode.None))
            {
                existing.gameObject.SetActive(false);
            }

            _fakesHost = new GameObject("CaptureFakes");
            fakes = _fakesHost.AddComponent<FakeGameServices>();
            fakes.Camera = context.Get<IViewCamera>().Camera;
            fakes.Position = context.Get<IRoverState>().Position;
            fakes.Initialize(new GameContext(context.Events, context.Input));
            fakes.SetMaterials(10, 6, 2);

            _target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            _panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiTestRig.PanelSettingsPath));
            _panel.targetTexture = _target;
            _panel.clearColor = true;
            _panel.colorClearValue = Color.clear;
            _tuning = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UiTuning>(UiTestRig.TuningPath));
            _tuning.Prompts.AddMissingDefaults();

            _uiHost = new GameObject("CaptureUI");
            _uiHost.SetActive(false);
            var document = _uiHost.AddComponent<UIDocument>();
            document.panelSettings = _panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiTestRig.UxmlPath);
            var ui = _uiHost.AddComponent<UISystem>();
            ui.Wire(document, _tuning, new[]
                {
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.EnglishPath),
                    AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.VietnamesePath),
                }, AssetDatabase.LoadAssetAtPath<WordmarkShape>(UiTestRig.WordmarkPath),
                AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiTestRig.RoverDiagramPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.CreditsPath));
            ui.QuitAction = () => { };
            _uiHost.SetActive(true);

            _slot = BootstrapHarness.NewTestSlot();
            save = new SaveService(SaveService.DefaultDirectory, _slot);
            ui.Initialize(new UiServices(context.Events, context.Input, fakes, fakes, context.Get<IAudioSettings>(),
                context.Get<ILookSettings>(), context.Get<IFeelSettings>(), save, fakes, fakes, fakes,
                context.Get<IGameFlow>(), new SettingsFile(SaveService.DefaultDirectory, _slot), fakes, fakes,
                fakes.Hold, fakes.Catalog, fakes.Systems, sun: fakes.Sun));
            save.Load();
            return ui;
        }

        /// <summary>
        /// 07's power-on self-test typed from a replayed Rover event stream, in Vietnamese and English: the checks with
        /// their dots, the alerts in amber, no guidance until it finishes. Rerun with --filter CapturePost.
        /// </summary>
        [UnityTest]
        public IEnumerator CapturePost_TypesEachCheck_AlertsInAmber_AndHoldsGuidanceUntilItEnds()
        {
#if UNITY_EDITOR
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out _, out _);
            Reframe(1920, 1080);
            FreezeWorld();
            yield return null;
            string folder = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(folder);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            foreach (string language in new[] { "vi", "en" })
            {
                ui.Localization.SetLanguage(language);
                for (int i = 0; i < PostStream.Length; i++)
                {
                    BootCheckStep step = PostStream[i];
                    context.Events.Publish(new BootCheckStarted(step));
                    yield return new WaitForSecondsRealtime(PostStepSeconds * 0.7f);
                    context.Events.Publish(new BootCheckCompleted(step, PostResults[i]));
                    yield return new WaitForSecondsRealtime(PostStepSeconds * 0.3f);
                    Assert.IsTrue(ui.Post.IsRunning, "the self-test runs until Rover says it is over");
                    if (step == BootCheckStep.Cutter)
                    {
                        yield return Capture(camera, folder, "post_cutter_" + language);
                    }
                }

                yield return new WaitForSecondsRealtime(0.6f);
                Assert.IsTrue(ui.PostView.Line(PostStream.Length - 1).text.Length > 0, "the last line has typed");
                Assert.IsTrue(ui.PostView.Line(8).ClassListContains(PostView.AlertClass), "the cutter's fault is amber");
                yield return Capture(camera, folder, "post_done_" + language);
                context.Events.Publish(new BootCheckFinished());
                yield return null;
                Assert.IsFalse(ui.Post.IsRunning);
                yield return new WaitForSecondsRealtime(_tuning.Post.LingerSeconds + 0.3f);
                Assert.AreEqual(DisplayStyle.None, ui.PostView.Root.resolvedStyle.display, "the log leaves the CRT");
            }
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

        private const float PostStepSeconds = 0.9f;

        private static readonly BootCheckStep[] PostStream =
        {
            BootCheckStep.Carrier, BootCheckStep.Resume, BootCheckStep.Agenda, BootCheckStep.Lens, BootCheckStep.Lamps,
            BootCheckStep.Head, BootCheckStep.Wheels, BootCheckStep.Motor, BootCheckStep.Cutter, BootCheckStep.SolarWing,
            BootCheckStep.Antenna, BootCheckStep.Battery, BootCheckStep.Assembly, BootCheckStep.Begin,
        };

        private static readonly BootCheckResult[] PostResults =
        {
            BootCheckResult.Info, BootCheckResult.Info, BootCheckResult.Info, BootCheckResult.Ok, BootCheckResult.Ok,
            BootCheckResult.Ok, BootCheckResult.Ok, BootCheckResult.Ok, BootCheckResult.Fault, BootCheckResult.Jammed,
            BootCheckResult.ReceiveOnly, BootCheckResult.Ok, BootCheckResult.Info, BootCheckResult.Info,
        };

        private IEnumerator Capture(Camera camera, string folder, string name)
        {
            yield return null;
            int width = _target.width;
            int height = _target.height;
            Texture2D world = FrameCapture.Render(camera, width, height);
            var ui = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
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
