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
using MoonProject.Gameplay;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Every UI touch is published as a <see cref="UiCue"/> exactly once, at the right moment, and the back buttons
    /// follow one rule: Esc / Start / B close a memory card first, otherwise they step back one level of the menu.
    /// </summary>
    public sealed class UiCueTests : InputTestFixture
    {
        private const float Settle = 0.8f;

        private readonly List<UiCueKind> _cues = new List<UiCueKind>();
        private string _slot;
        private InputActionAsset _controls;
        private UiTestRig _rig;
        private System.IDisposable _subscription;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
            _cues.Clear();
        }

        public override void TearDown()
        {
            _subscription?.Dispose();
            _rig?.Dispose();
            _rig = null;
            BootstrapHarness.DeleteSaveFiles(_slot);
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PauseMenu_CuesEveryTouch_InOrder()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            UiLayout layout = _rig.Ui.Layout;

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            yield return Navigate(NavigationMoveEvent.Direction.Down);
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            layout.MusicSlider.value = 3;
            Submit(layout.LanguageButton);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(Settle);

            CollectionAssert.AreEqual(new[]
            {
                UiCueKind.MenuOpen, UiCueKind.FocusMove, UiCueKind.Confirm, UiCueKind.SliderStep, UiCueKind.Confirm,
                UiCueKind.Back, UiCueKind.MenuClose,
            }, _cues, "focus placed by the menu itself is silent; the player's moves and presses are not");
        }

        [UnityTest]
        public IEnumerator GamepadB_BacksOutOfEachSubmenu_ThenClosesTheMenu()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot();
            yield return null;
            PauseMenu pause = _rig.Ui.Pause;

            yield return Tap(gamepad.startButton);
            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.SettingsButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(pause.IsSettingsOpen);
            yield return Tap(gamepad.buttonEast);
            Assert.IsFalse(pause.IsSettingsOpen, "B closes the settings");
            Assert.IsTrue(pause.IsOpen, "but not the menu");

            yield return new WaitForSecondsRealtime(Settle);
            Submit(_rig.Ui.Layout.QuitButton);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.IsTrue(pause.IsAskingToQuit);
            yield return Tap(gamepad.buttonEast);
            Assert.IsFalse(pause.IsAskingToQuit, "B leaves the quit question");
            Assert.IsTrue(pause.IsOpen);

            yield return Tap(gamepad.buttonEast);
            Assert.IsFalse(pause.IsOpen, "B from the top level resumes");
            Assert.AreEqual(UiCueKind.MenuClose, _cues[_cues.Count - 1]);
        }

        [UnityTest]
        public IEnumerator Start_ClosesAMemoryCardFirst_ThenPauses()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot();
            yield return null;
            Events.Publish(new RelicDeposited("teapot", Vector3.zero, 1));
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(_rig.Ui.Card.CanDismiss);
            CollectionAssert.AreEqual(new[] { UiCueKind.CardShown }, _cues);

            yield return Tap(gamepad.startButton);
            Assert.IsFalse(_rig.Ui.Card.CanDismiss, "Start closes the card");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "and does not also pause");
            Assert.AreEqual(UiCueKind.Back, _cues[_cues.Count - 1]);

            yield return Tap(gamepad.startButton);
            Assert.IsTrue(_rig.Ui.Pause.IsOpen, "with no card up, Start pauses");
        }

        [UnityTest]
        public IEnumerator HudCues_PromptShown_HoldFill_HoldComplete()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, new Vector3(0f, 0f, 12f), true);
            yield return new WaitForSecondsRealtime(1.5f);
            CollectionAssert.AreEqual(new[] { UiCueKind.PromptShown }, _cues, "one cue per appearance");

            _cues.Clear();
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(20, 20, 20);
            _rig.Fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.5f);
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_rig.Tuning.TowerPanel.HoldSeconds + 0.3f);
            Release(keyboard.eKey);
            yield return null;
            CollectionAssert.AreEqual(new[] { UiCueKind.HoldFill, UiCueKind.HoldComplete }, _cues);
        }

        [UnityTest]
        public IEnumerator BenchCues_ATapIsAFocusMove_NeverAHoldSwell()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            _rig.Fakes.Bench = _rig.TestBench(new Recipe(2, 1, 0), new Recipe(3, 2, 0), new Recipe(1, 2, 2));
            _rig.Fakes.SetMaterials(20, 20, 20);
            _rig.Fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.5f);
            _cues.Clear();
            yield return Tap(keyboard.eKey);
            yield return new WaitForSecondsRealtime(0.3f);
            CollectionAssert.AreEqual(new[] { UiCueKind.FocusMove }, _cues, "a tap picks, it does not swell");

            _cues.Clear();
            Press(keyboard.eKey);
            yield return new WaitForSecondsRealtime(_rig.Tuning.TowerPanel.TapSeconds +
                                                    _rig.Tuning.TowerPanel.HoldSeconds + 0.3f);
            Release(keyboard.eKey);
            yield return null;
            CollectionAssert.AreEqual(new[] { UiCueKind.HoldFill, UiCueKind.HoldComplete }, _cues);
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
            _subscription = Events.Subscribe<UiCue>(cue => _cues.Add(cue.Kind));
        }

        private IEnumerator Navigate(NavigationMoveEvent.Direction direction)
        {
            var focused = (VisualElement)_rig.Ui.Layout.Root.panel.focusController.focusedElement;
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
