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
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The pause menu end to end with scripted devices: game time eases to a stop and back, the rover controls turn
    /// off and on, the cursor is freed and recaptured, settings drive the services and survive a reload, and the
    /// language changes live.
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
                BootstrapHarness.DeleteSaveFiles(slot);
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
            Assert.AreEqual("vi", _rig.Ui.Localization.Language);
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
            Assert.AreSame(layout.NewGameButton, Focused());
            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreSame(layout.QuitButton, Focused());
            yield return Navigate(NavigationMoveEvent.Direction.Up);
            Assert.AreSame(layout.NewGameButton, Focused(), "and up moves back");
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
