using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The pause menu end to end with scripted devices: game time eases to a stop and back, the rover controls turn
    /// off and on, the cursor is freed and recaptured, settings drive the services and survive a reload, and the
    /// language changes live. The same menu as the title (M3-09): Continue, New Game asking once over a save, the first
    /// boot's language step, the quiet rest, and the dark drawn before App is asked to reload.
    /// </summary>
    public sealed class PauseMenuTests : InputTestFixture
    {
        private const float Settle = 0.8f;

        private readonly List<string> _slots = new List<string>();
        private InputActionAsset _controls;
        private UiTestRig _rig;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            foreach (string slot in _slots)
            {
                UiTestRig.DeleteSlotFiles(slot);
            }

            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Escape_PausesTheGame_AndEscapeAgainResumesIt()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            var changes = new List<bool>();
            EventBus events = _rig.Bootstrap.Context.Events;
            using (events.Subscribe<PauseChanged>(changed => changes.Add(changed.Paused)))
            {
                yield return null;
                Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "driving before the pause");
                Assert.IsFalse(_rig.Ui.Cursor.InMenu, "the cursor is captured while driving");

                yield return Tap(keyboard.escapeKey);
                Assert.IsTrue(_rig.Ui.Pause.IsOpen);
                Assert.IsFalse(_rig.Bootstrap.Context.Input.Enabled, "the rover controls are off");
                Assert.IsTrue(_rig.Ui.Cursor.InMenu, "the cursor is free in the menu");
                Assert.Greater(Time.timeScale, 0f, "time eases to a stop, it does not snap");
                yield return new WaitForSecondsRealtime(Settle);
                Assert.AreEqual(0f, Time.timeScale, "game time has stopped");
                Assert.IsTrue(_rig.Bootstrap.Context.Input.Menu.Enabled, "the menu map stays live");
                Assert.AreEqual(DisplayStyle.Flex, _rig.Ui.Layout.PauseMain.resolvedStyle.display);
                Assert.AreSame(_rig.Ui.Layout.ResumeButton, Focused(), "Resume has keyboard/gamepad focus");

                yield return Tap(keyboard.escapeKey);
                Assert.IsFalse(_rig.Ui.Pause.IsOpen);
                Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the rover controls are back");
                Assert.IsFalse(_rig.Ui.Cursor.InMenu);
                Assert.IsNull(Focused(), "nothing keeps focus while driving (no stray navigation)");
                yield return new WaitForSecondsRealtime(Settle);
                Assert.AreEqual(1f, Time.timeScale, "full speed again");
                CollectionAssert.AreEqual(new[] { true, false }, changes);
            }
        }

        [UnityTest]
        public IEnumerator GamepadStart_PausesAndResumes_AndEastStepsBack()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;

            yield return Tap(gamepad.startButton);
            Assert.IsTrue(_rig.Ui.Pause.IsOpen);
            yield return Tap(gamepad.buttonEast);
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "B backs out of the menu");
            yield return Tap(gamepad.startButton);
            Assert.IsTrue(_rig.Ui.Pause.IsOpen);
            yield return Tap(gamepad.startButton);
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "Start toggles");
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Settings_DriveTheServices_SwitchLanguageLive_AndSurviveAReload()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            UiLayout layout = _rig.Ui.Layout;
            Assert.AreEqual("Resume", layout.ResumeButton.text, "English first");

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsSettingsOpen);
            Assert.AreSame(layout.MasterSlider, Focused(), "focus moves into the settings");

            layout.MusicSlider.value = 4;
            layout.LookSlider.value = 8;
            layout.InvertToggle.value = true;
            Submit(layout.LanguageButton);
            yield return null;
            Assert.AreEqual(0.4f, _rig.Fakes.GetVolume(AudioBus.Music), 1e-5f);
            Assert.AreEqual(2f, _rig.Fakes.Sensitivity, 1e-5f);
            Assert.IsTrue(_rig.Fakes.InvertY);
            Assert.AreEqual("Tiếp tục", layout.ResumeButton.text, "the menu speaks Vietnamese at once");
            Assert.AreEqual("Tiếng Việt", layout.LanguageButton.text);

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(_rig.Ui.Pause.IsSettingsOpen, "Esc closes the settings first (and saves them)");
            Assert.IsTrue(_rig.Ui.Pause.IsOpen);
            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(_rig.Ui.Pause.IsOpen);

            _rig.Dispose();
            _rig = null;
            yield return null;
            Boot(slot);
            yield return null;
            Assert.AreEqual(0.4f, _rig.Fakes.GetVolume(AudioBus.Music), 1e-5f, "volumes come back");
            Assert.AreEqual(2f, _rig.Fakes.Sensitivity, 1e-5f, "look speed comes back");
            Assert.IsTrue(_rig.Fakes.InvertY);
            Assert.AreEqual("Tiếp tục", _rig.Ui.Layout.ResumeButton.text, "and so does the language");
        }

        [UnityTest]
        public IEnumerator Settings_SurviveANewGame_SoTheLanguageStays()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.LanguageButton);
            yield return Tap(keyboard.escapeKey);
            yield return Tap(keyboard.escapeKey);
            _rig.Bootstrap.Context.Get<ISaveService>().PutAway();

            _rig.Dispose();
            _rig = null;
            yield return null;
            Boot(slot);
            yield return null;
            Assert.AreEqual("vi", _rig.Bootstrap.Context.Get<ILocalization>().Language,
                "a new moon, the same player: still Vietnamese");
        }

        [UnityTest]
        public IEnumerator Settings_InAnOlderSave_MoveIntoTheSettingsFileOnce()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _slots.Add(slot);
            var old = new SaveService(SaveService.DefaultDirectory, slot);
            var data = new SettingsSaveData { language = "vi", music = 0.3f };
            using (old.Register(new SaveSection<SettingsSaveData>(UiSaveKeys.Settings, UiSaveKeys.SettingsVersion,
                       () => data, _ => { })))
            {
                old.Load();
                Assert.IsTrue(old.SaveNow());
            }

            var settings = new SettingsFile(SaveService.DefaultDirectory, slot);
            Assert.IsFalse(settings.TryRead(out _), "an older save: no settings file yet");
            _rig = UiTestRig.Boot(_controls, slot);
            yield return null;
            Assert.AreEqual("vi", _rig.Bootstrap.Context.Get<ILocalization>().Language);
            Assert.AreEqual(0.3f, _rig.Fakes.GetVolume(AudioBus.Music), 1e-5f);
            Assert.IsTrue(settings.TryRead(out string json), "the settings moved into their own file");
            StringAssert.Contains("\"vi\"", json);
        }

        [UnityTest]
        public IEnumerator CassetteLine_ShowsOnceATapeIsOwned_CountingEveryTapeInTheGame()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            UiLayout layout = _rig.Ui.Layout;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(DisplayStyle.None, layout.PauseCassettes.resolvedStyle.display, "nothing to count yet");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);

            _rig.Fakes.AddTape(UiTestRig.FirstTape);
            _rig.Fakes.AddTape(UiTestRig.SecondTape);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(DisplayStyle.Flex, layout.PauseCassettes.resolvedStyle.display);
            Assert.AreEqual(string.Format(_rig.Ui.Localization.Get(UiKeys.PauseCassettes), 2, 3),
                layout.PauseCassettesCount.text, "both numbers from the program: the real total, never 8");

            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(layout.LanguageButton);
            yield return null;
            Assert.AreEqual(string.Format(_rig.Ui.Localization.Get(UiKeys.PauseCassettes), 2, 3),
                layout.PauseCassettesCount.text);
            StringAssert.Contains("2/3", layout.PauseCassettesCount.text, "and in Vietnamese at once");
            Assert.AreEqual("vi", _rig.Bootstrap.Context.Get<ILocalization>().Language);
        }

        [UnityTest]
        public IEnumerator RelayLine_ShowsOnceAMastIsLit_CountingEveryMast()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            UiLayout layout = _rig.Ui.Layout;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(DisplayStyle.None, layout.PauseRelays.resolvedStyle.display,
                "no relay count before the world has shown a relay");
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);

            _rig.Fakes.LitMasts = 2;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(DisplayStyle.Flex, layout.PauseRelays.resolvedStyle.display);
            Assert.AreEqual(string.Format(_rig.Ui.Localization.Get(UiKeys.PauseRelays), 2, 4),
                layout.PauseRelaysCount.text, "lit masts of every mast, both from the relay status");
        }

        /// <summary>
        /// Arrow keys, the d-pad and the stick reach UI Toolkit as navigation events through the Input System's UI
        /// provider (which an InputTestFixture cannot drive); this checks what the menu itself must get right: every
        /// item is focusable in order and focus walks down and back up the page.
        /// </summary>
        [UnityTest]
        public IEnumerator NavigationEvents_WalkFocusThroughTheMenu()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Assert.AreSame(layout.ResumeButton, Focused());

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.SettingsButton, Focused(), "down moves to the next button");
            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.QuitButton, Focused(), "no New Game in the pause, and no title to quit to here");
            Assert.AreEqual(DisplayStyle.None, layout.NewGameButton.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.QuitToTitleButton.resolvedStyle.display);
            yield return Navigate(NavigationMoveEvent.Direction.Up);
            Assert.AreSame(layout.SettingsButton, Focused(), "and up moves back");
        }

        [UnityTest]
        public IEnumerator Title_Quit_DrawsTheDark_ThenQuitsOnce()
        {
            Boot(BootstrapHarness.NewTestSlot());
            int quits = 0;
            _rig.Ui.QuitAction = () => quits++;
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.QuitButton);
            Submit(_rig.Ui.Layout.QuitButton);
            yield return null;
            Assert.AreEqual(0, quits, "the dark comes first, and the title never asks");
            Assert.IsFalse(_rig.Ui.Pause.IsAskingToQuit);
            yield return new WaitForSecondsRealtime(_rig.Tuning.Title.Curtain.FadeIn + 0.5f);
            Assert.AreEqual(1, quits, "once, however often it is pressed");
        }

        [UnityTest]
        public IEnumerator Title_WithASave_RestsOnContinue_WhichHandsOverToPlay()
        {
            Boot(BootstrapHarness.NewTestSlot());
            int continued = 0;
            _rig.Bootstrap.Context.Events.Subscribe<ContinueRequested>(_ => continued++);
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Assert.IsTrue(_rig.Ui.Pause.IsTitle);
            Assert.AreSame(layout.ResumeButton, Focused(), "Continue is the default");
            Assert.AreEqual("Continue", layout.ResumeButton.text);
            Assert.AreEqual(DisplayStyle.Flex, layout.NewGameButton.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.QuitToTitleButton.resolvedStyle.display);
            Assert.AreEqual(1f, Time.timeScale, "the world keeps living behind the title");

            _rig.Ui.Pause.Back();
            Assert.IsTrue(_rig.Ui.Pause.IsTitle, "Esc never backs out of the title");
            Submit(layout.ResumeButton);
            Assert.AreEqual(1, continued);
            Assert.IsFalse(_rig.Ui.Pause.IsOpen);
        }

        [UnityTest]
        public IEnumerator Title_NewGameOverASave_AsksOnce_OnTheSafeAnswer_ThenDrawsTheDark()
        {
            Boot(BootstrapHarness.NewTestSlot());
            int asked = 0;
            _rig.Bootstrap.Context.Events.Subscribe<NewGameRequested>(_ => asked++);
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Submit(layout.NewGameButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsAskingNewGame);
            Assert.AreSame(layout.NewGameKeepButton, Focused(), "the safe answer has focus");
            Assert.AreEqual(_rig.Bootstrap.Context.Get<ILocalization>().Get("ui.title.new_game_question"),
                layout.PageNewGame.Q<Label>(className: "pause__question").text);
            _rig.Ui.Pause.Back();
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsAskingNewGame);
            Assert.AreSame(layout.NewGameButton, Focused(), "back to the button that asked");

            Submit(layout.NewGameButton);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(layout.NewGameConfirmButton);
            yield return null;
            Assert.AreEqual(0, asked, "the dark comes first");
            yield return new WaitForSecondsRealtime(_rig.Tuning.Title.Curtain.FadeIn + 0.5f);
            Assert.AreEqual(1, asked);
        }

        [UnityTest]
        public IEnumerator Title_FirstBoot_AsksTheLanguage_ThenNewGameStartsRightThere()
        {
            Boot(BootstrapHarness.NewTestSlot());
            int asked = 0;
            _rig.Bootstrap.Context.Events.Subscribe<NewGameRequested>(_ => asked++);
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(false));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Assert.IsTrue(_rig.Ui.Pause.IsChoosingLanguage);
            var english = (Button)layout.PageLanguage[0];
            var vietnamese = (Button)layout.PageLanguage[1];
            Assert.AreSame(english, Focused());
            Assert.AreEqual("Tiếng Việt", vietnamese.text, "each language in its own words");

            Submit(vietnamese);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsChoosingLanguage);
            Assert.AreEqual("vi", _rig.Bootstrap.Context.Get<ILocalization>().Language);
            Assert.AreEqual(DisplayStyle.None, layout.ResumeButton.resolvedStyle.display, "nothing to continue");
            Assert.AreSame(layout.NewGameButton, Focused(), "New Game is the default with no save");
            Assert.AreEqual(_rig.Bootstrap.Context.Get<ILocalization>().Get("ui.title.new_game"), layout.NewGameButton.text);

            Submit(layout.NewGameButton);
            Assert.AreEqual(1, asked, "no question and no dark: the world behind the title is already fresh");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen);
        }

        [UnityTest]
        public IEnumerator Title_Credits_OpenOnTheirOwn_DriftSlowly_ScrollByKey_AndBackReturnsToTheMenu()
        {
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Assert.AreEqual(DisplayStyle.Flex, layout.CreditsButton.resolvedStyle.display, "the title offers Credits");

            Submit(layout.CreditsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsShowingCredits);
            Assert.AreSame(layout.CreditsBack, Focused(), "Back has focus: the gamepad can leave");
            StringAssert.Contains("Version", ((Label)layout.CreditsScroll.contentContainer[1]).text);
            Assert.AreEqual("Lunarion", ((Label)layout.CreditsScroll.contentContainer[0]).text);

            float start = layout.CreditsScroll.scrollOffset.y;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(layout.CreditsScroll.scrollOffset.y, start, "the credits drift up by themselves");

            float before = layout.CreditsScroll.scrollOffset.y;
            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.Greater(layout.CreditsScroll.scrollOffset.y, before + 50f, "Down scrolls them a step");

            _rig.Ui.Pause.Back();
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsShowingCredits);
            Assert.IsTrue(_rig.Ui.Pause.IsTitle, "Back returns to the title's menu, not the game");
            Assert.AreSame(layout.CreditsButton, Focused(), "focus returns to the Credits entry");
        }

        [UnityTest]
        public IEnumerator Settings_HaveThreeTabs_OnePageEach_AndTheDisplayPageStepsItsChoices()
        {
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(0, _rig.Ui.Pause.SettingsTab, "Sound first");
            Assert.AreEqual(DisplayStyle.Flex, layout.PageSound.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.PageCamera.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.PageDisplay.resolvedStyle.display);
            Assert.AreSame(layout.MasterSlider, Focused(), "focus goes into the Sound page");

            Submit(layout.TabCamera);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, layout.PageCamera.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.PageSound.resolvedStyle.display);
            Assert.IsTrue(layout.TabCamera.ClassListContains("settings-tab--on"));
            Assert.IsFalse(layout.TabSound.ClassListContains("settings-tab--on"));

            Submit(layout.TabDisplay);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, layout.PageDisplay.resolvedStyle.display);
            string resolution = layout.ResolutionValue.text;
            StringAssert.Contains("×", resolution);
            Submit(layout.ResolutionNext);
            Assert.AreNotEqual(resolution, layout.ResolutionValue.text, "the arrows step the resolution");
            Submit(layout.ResolutionPrev);
            Assert.AreEqual(resolution, layout.ResolutionValue.text, "and back");

            ILocalization words = _rig.Bootstrap.Context.Get<ILocalization>();
            Assert.AreEqual(words.Get(UiKeys.DisplayModeName(DisplayMode.Borderless)), layout.ModeValue.text,
                "borderless is the default mode");
            Submit(layout.ModeNext);
            Assert.AreEqual(words.Get(UiKeys.DisplayModeName(DisplayMode.Exclusive)), layout.ModeValue.text);
            Submit(layout.ModeNext);
            Assert.AreEqual(words.Get(UiKeys.DisplayModeName(DisplayMode.Windowed)), layout.ModeValue.text);

            Assert.IsTrue(layout.VSyncToggle.value, "V-Sync is on by default");
            Assert.IsTrue(layout.FpsRow.ClassListContains("setting-row--idle"), "the FPS limit rests while V-Sync paces");
            layout.VSyncToggle.value = false;
            Assert.IsFalse(layout.FpsRow.ClassListContains("setting-row--idle"));
            Submit(layout.FpsNext);
            Assert.AreEqual("120", layout.FpsValue.text, "30, 60, then 120");
            Submit(layout.FpsNext);
            Submit(layout.FpsNext);
            Assert.AreEqual(words.Get(UiKeys.DisplayUnlimited), layout.FpsValue.text);

            _rig.Ui.Pause.Back();
            yield return new WaitForSecondsRealtime(Settle);
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(0, _rig.Ui.Pause.SettingsTab, "the settings open on Sound again");
        }

        [UnityTest]
        public IEnumerator Title_RestsWhenUntouched_AndAnyStepBringsItBack()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            var tuning = new UnityEditor.SerializedObject(_rig.Tuning);
            tuning.FindProperty("_title._idleSeconds").floatValue = 0.5f;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsResting, "left alone, the menu leaves the world to itself");
            yield return Tap(keyboard.downArrowKey);
            yield return new WaitForSecondsRealtime(Settle * 0.5f);
            Assert.IsFalse(_rig.Ui.Pause.IsResting);
            Assert.IsTrue(_rig.Ui.Pause.IsTitle);
        }

        [UnityTest]
        public IEnumerator Quit_AsksFirst_AndStayReturnsToTheMenu()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            int quits = 0;
            _rig.Ui.QuitAction = () => quits++;
            yield return null;

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsAskingToQuit);
            Assert.AreEqual(0, quits, "one press never quits");
            Assert.AreSame(_rig.Ui.Layout.QuitStayButton, Focused(), "the safe answer has focus");

            Submit(_rig.Ui.Layout.QuitStayButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsAskingToQuit);
            Submit(_rig.Ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.QuitConfirmButton);
            Assert.AreEqual(1, quits);
        }

        [UnityTest]
        public IEnumerator Title_HoverAndFocus_UnifyAsOneSystem_AndMouseLeaveKeepsFocus()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;

            Button[] menuButtons =
            {
                layout.ResumeButton,
                layout.NewGameButton,
                layout.SettingsButton,
                layout.CreditsButton,
                layout.QuitButton,
            };

            Assert.AreSame(layout.ResumeButton, Focused(), "Continue starts focused");
            AssertSingleFocusedCursor(menuButtons, layout.ResumeButton);

            PointerEnter(layout.SettingsButton);
            yield return null;
            Assert.AreSame(layout.SettingsButton, Focused(), "hovering an item focuses it");
            AssertSingleFocusedCursor(menuButtons, layout.SettingsButton);

            PointerLeave(layout.SettingsButton);
            yield return null;
            Assert.AreSame(layout.SettingsButton, Focused(), "moving the mouse off the menu keeps the last focus");
            AssertSingleFocusedCursor(menuButtons, layout.SettingsButton);

            Focused()?.Blur();
            yield return null;
            Assert.AreSame(layout.SettingsButton, Focused(), "clicking empty space never leaves zero items focused");
            AssertSingleFocusedCursor(menuButtons, layout.SettingsButton);

            yield return Tap(keyboard.sKey);
            Assert.AreSame(layout.CreditsButton, Focused(), "S key steps focus down");
            AssertSingleFocusedCursor(menuButtons, layout.CreditsButton);

            yield return Tap(keyboard.wKey);
            Assert.AreSame(layout.SettingsButton, Focused(), "W key steps focus up");
            AssertSingleFocusedCursor(menuButtons, layout.SettingsButton);
        }

        [UnityTest]
        public IEnumerator Settings_AllControls_ReachableAndChangeable_WithKeyboardAndGamepad_AndBackRestoresFocus()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            _rig.Bootstrap.Context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(Settle);
            UiLayout layout = _rig.Ui.Layout;

            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsSettingsOpen);
            Assert.AreSame(layout.MasterSlider, Focused(), "Sound tab's first control is focused");

            int masterBefore = layout.MasterSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(masterBefore - 1, layout.MasterSlider.value, "MasterSlider stepped with Left");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.MusicSlider, Focused());
            int musicBefore = layout.MusicSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(musicBefore - 1, layout.MusicSlider.value, "MusicSlider stepped with Left");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.SfxSlider, Focused());
            int sfxBefore = layout.SfxSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(sfxBefore - 1, layout.SfxSlider.value, "SfxSlider stepped with Left");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.AmbienceSlider, Focused());
            int ambienceBefore = layout.AmbienceSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(ambienceBefore - 1, layout.AmbienceSlider.value, "AmbienceSlider stepped with Left");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.SettingsBackButton, Focused(), "Down from last row reaches Back");

            for (int i = 0; i < 5; i++)
            {
                yield return Navigate(NavigationMoveEvent.Direction.Up);
            }

            Assert.AreSame(layout.TabSound, Focused(), "Up from first row reaches the active tab");

            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(1, _rig.Ui.Pause.SettingsTab, "Right on tab switches to Camera tab");
            Assert.AreSame(layout.TabCamera, Focused());

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.LookSlider, Focused());
            int lookBefore = layout.LookSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(lookBefore + 1, layout.LookSlider.value, "LookSlider stepped with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.InvertToggle, Focused());
            bool invertBefore = layout.InvertToggle.value;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(invertBefore, layout.InvertToggle.value, "InvertToggle toggled with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.GentleCameraToggle, Focused());
            bool gentleBefore = layout.GentleCameraToggle.value;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(gentleBefore, layout.GentleCameraToggle.value, "GentleCameraToggle toggled with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.RumbleToggle, Focused());
            bool rumbleBefore = layout.RumbleToggle.value;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(rumbleBefore, layout.RumbleToggle.value, "RumbleToggle toggled with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.RumbleSlider, Focused());
            int rumbleStepBefore = layout.RumbleSlider.value;
            yield return Navigate(NavigationMoveEvent.Direction.Left);
            Assert.AreEqual(rumbleStepBefore - 1, layout.RumbleSlider.value, "RumbleSlider stepped with Left");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.GuidanceToggle, Focused());
            bool guidanceBefore = layout.GuidanceToggle.value;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(guidanceBefore, layout.GuidanceToggle.value, "GuidanceToggle toggled with Right");

            for (int i = 0; i < 6; i++)
            {
                yield return Navigate(NavigationMoveEvent.Direction.Up);
            }

            Assert.AreSame(layout.TabCamera, Focused());
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(2, _rig.Ui.Pause.SettingsTab, "Right on tab switches to Display tab");
            Assert.AreSame(layout.TabDisplay, Focused());

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.ResolutionPrev, Focused());
            string resBefore = layout.ResolutionValue.text;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(resBefore, layout.ResolutionValue.text, "Resolution stepped with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.ModePrev, Focused());
            string modeBefore = layout.ModeValue.text;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(modeBefore, layout.ModeValue.text, "Window Mode stepped with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.VSyncToggle, Focused());
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.IsFalse(layout.VSyncToggle.value, "VSync toggled off with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.FpsPrev, Focused());
            string fpsBefore = layout.FpsValue.text;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(fpsBefore, layout.FpsValue.text, "FPS cap stepped with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.TextSizeButton, Focused());
            string sizeBefore = layout.TextSizeButton.text;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(sizeBefore, layout.TextSizeButton.text, "Text size stepped with Right");

            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.LanguageButton, Focused());
            string langBefore = layout.LanguageButton.text;
            yield return Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreNotEqual(langBefore, layout.LanguageButton.text, "Language stepped with Right");

            yield return Tap(gamepad.buttonEast);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsSettingsOpen, "Gamepad B backs out of Settings");
            Assert.AreSame(layout.SettingsButton, Focused(), "focus returns to SettingsButton");

            yield return Tap(keyboard.sKey);
            Assert.AreSame(layout.CreditsButton, Focused());
            Submit(layout.CreditsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(_rig.Ui.Pause.IsShowingCredits);
            Assert.AreSame(layout.CreditsBack, Focused());

            yield return Tap(gamepad.buttonEast);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsFalse(_rig.Ui.Pause.IsShowingCredits, "Gamepad B backs out of Credits");
            Assert.AreSame(layout.CreditsButton, Focused(), "focus returns to CreditsButton");
        }

        private static void AssertSingleFocusedCursor(Button[] buttons, Button expected)
        {
            int shownCursors = 0;
            foreach (Button button in buttons)
            {
                Label cursor = button.Q<Label>(className: "menu-button__cursor");
                Assert.IsNotNull(cursor);
                if (cursor.resolvedStyle.display == DisplayStyle.Flex)
                {
                    shownCursors++;
                    Assert.AreSame(expected, button, "only the focused button shows the '>' cursor");
                }
            }

            Assert.AreEqual(1, shownCursors, "exactly one button shows the '>' cursor");
        }

        private static void PointerEnter(VisualElement target)
        {
            using (PointerEnterEvent evt = PointerEnterEvent.GetPooled())
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        private static void PointerLeave(VisualElement target)
        {
            using (PointerLeaveEvent evt = PointerLeaveEvent.GetPooled())
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        private void Boot(string slot)
        {
            _slots.Add(slot);
            _rig = UiTestRig.Boot(_controls, slot);
        }

        private Focusable Focused()
        {
            return _rig.Ui.Layout.Root.panel.focusController.focusedElement;
        }

        private IEnumerator Navigate(NavigationMoveEvent.Direction direction)
        {
            var focused = (VisualElement)Focused();
            using (NavigationMoveEvent move = NavigationMoveEvent.GetPooled(direction))
            {
                move.target = focused;
                focused.SendEvent(move);
            }

            yield return null;
        }

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
