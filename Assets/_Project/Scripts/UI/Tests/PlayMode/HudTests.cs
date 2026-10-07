using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The driving HUD end to end: prompts appear only near usable things and only a few times, the reticle only while
    /// aiming, the scrap chip only on change, the memory card after a deposit, and the tower panel buys only on a
    /// deliberate hold.
    /// </summary>
    public sealed class HudTests : InputTestFixture
    {
        private string _slot;
        private InputActionAsset _controls;
        private UiTestRig _rig;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
        }

        public override void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            BootstrapHarness.DeleteSaveFiles(_slot);
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Prompt_AppearsNearAUsableThing_NamesItsKey_AndBowsOutWhenUsed()
        {
            InputSystem.AddDevice<Keyboard>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot();
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, new Vector3(0f, 0f, 12f), true);
            yield return null;
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompts before 07 wakes");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual("Dig", _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text, "keyboard glyph from the real binding");

            yield return Tap(gamepad.buttonSouth);
            yield return Seconds(0.1f);
            Assert.AreEqual(InputDeviceKind.Gamepad, _rig.Bootstrap.Context.Input.ActiveDevice);
            Assert.AreEqual("X", _rig.Ui.Layout.PromptGlyphLabel.text, "the glyph follows the device");
            Assert.IsTrue(_rig.Ui.Layout.PromptGlyph.ClassListContains(GlyphView.PadClass));

            Events.Publish(new ExcavationStarted(new Vector3(0f, 0f, 12f)));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "doing it ends the prompt");
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Excavate));
            Assert.AreEqual(1, _rig.Ui.Ledger.Shown(InteractionKind.Excavate));
        }

        [UnityTest]
        public IEnumerator Prompt_HidesBehindTheCamera_AndWhilePaused()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, -10f), true);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "never pinned to the screen edge for something behind you");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1f);
            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "the pause menu has the stage alone");
        }

        [UnityTest]
        public IEnumerator Reticle_ShowsOnlyWhileAiming_AndBrightensOnAHoveredTarget()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "no permanent crosshair");

            Press(mouse.rightButton);
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Reticle.IsVisible, "holding the tether with nothing in the cone");
            Assert.IsFalse(_rig.Ui.Reticle.IsHighlighted);

            _rig.Fakes.TetherState = TetherAimState.Hovering;
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Reticle.IsHighlighted, "a hovered relic brightens it");
            Assert.Greater(_rig.Ui.Layout.ReticleHover.resolvedStyle.scale.value.x, 1.1f, "and it gently opens");

            _rig.Fakes.TetherState = TetherAimState.Towing;
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "towing: the beam says it all");

            Release(mouse.rightButton);
            _rig.Fakes.TetherState = TetherAimState.Idle;
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible);
        }

        [UnityTest]
        public IEnumerator ScrapChip_DriftsInOnAChange_CountsUp_AndLeaves()
        {
            Boot();
            yield return null;
            _rig.Fakes.SetBalance(0);
            yield return Seconds(0.2f);
            Assert.IsFalse(_rig.Ui.Chip.IsVisible, "a balance without a change (a loaded save) shows nothing");

            _rig.Fakes.SetBalance(9);
            yield return Seconds(0.2f);
            Assert.IsTrue(_rig.Ui.Chip.IsVisible);
            Assert.Less(_rig.Ui.Chip.Shown, 9, "it counts rather than jumps");
            yield return Seconds(1.5f);
            Assert.AreEqual(9, _rig.Ui.Chip.Shown);
            Assert.AreEqual("9", _rig.Ui.Layout.ScrapChipCount.text);
            yield return Seconds(_rig.Tuning.ScrapChip.LingerSeconds + _rig.Tuning.ScrapChip.Reveal.FadeOut + 0.3f);
            Assert.IsFalse(_rig.Ui.Chip.IsVisible, "then it leaves the screen to the moon");
        }

        [UnityTest]
        public IEnumerator MemoryCard_TellsTheRelicsMemory_AndEscapeClosesItWithoutPausing()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 2));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Card.IsVisible);
            Assert.AreEqual("rubber_duck", _rig.Ui.Card.Current);
            Assert.AreEqual("Senior Debugging Consultant", _rig.Ui.Layout.MemoryCardName.text);
            StringAssert.StartsWith("Kenji explained", _rig.Ui.Layout.MemoryCardText.text);
            Assert.AreEqual("Memory 2 of 6", _rig.Ui.Layout.MemoryCardCaption.text);
            Assert.AreEqual("Esc", _rig.Ui.Layout.MemoryCardGlyphLabel.text);
            Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the card never blocks driving");

            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Card.IsVisible, "Esc closes the topmost thing first");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "and does not also open the pause menu");
        }

        [UnityTest]
        public IEnumerator TowerPanel_BuysOnlyOnADeliberateHold()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            _rig.Fakes.SetBalance(20);
            Press(keyboard.eKey);
            _rig.Fakes.AtStation = true;
            yield return Seconds(2f);
            Assert.IsTrue(_rig.Ui.Tower.IsVisible);
            Assert.AreEqual(0, _rig.Fakes.Purchases, "arriving with the button already held buys nothing");
            Assert.AreEqual("Wake the old mast", _rig.Ui.Layout.TowerTitle.text);
            Assert.AreEqual("Level 1 of 3", _rig.Ui.Layout.TowerLevel.text);
            Assert.AreEqual("20", _rig.Ui.Layout.ScrapChipCount.text, "the pinned chip shows the balance");
            Assert.AreEqual("15", _rig.Ui.Layout.TowerCost.text);
            Assert.IsTrue(_rig.Ui.Chip.IsVisible, "the balance stays in view at the pad");

            Release(keyboard.eKey);
            yield return Seconds(0.2f);
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds * 0.5f);
            Assert.AreEqual(0, _rig.Fakes.Purchases, "not before the ring is full");
            Assert.Greater(_rig.Ui.Tower.HoldProgress, 0.2f);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds);
            Assert.AreEqual(1, _rig.Fakes.Purchases);
            Assert.IsTrue(_rig.Ui.Tower.IsCelebrating);
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "one purchase per hold");
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.AreEqual("Raise the mast", _rig.Ui.Layout.TowerTitle.text, "the next level is offered");
            Assert.AreEqual(DisplayStyle.Flex, _rig.Ui.Layout.TowerNeed.resolvedStyle.display,
                "and the panel says how much scrap is still to gather");
            StringAssert.StartsWith("35 more scrap", _rig.Ui.Layout.TowerNeed.text);

            _rig.Fakes.AtStation = false;
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Tower.IsVisible);
        }

        [UnityTest]
        public IEnumerator ACrewLogAndATape_EachGetTheirCard_OneAfterTheOther()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new CrewLogFound(TestStrings.FirstLog, Vector3.zero));
            Events.Publish(new CassetteCollected(TestStrings.FirstTape, Vector3.zero, 1, 3));
            yield return Seconds(1f);
            Assert.AreEqual(TestStrings.FirstLog, _rig.Ui.Card.Current);
            StringAssert.StartsWith("Night one of Lumen After Dark", _rig.Ui.Layout.MemoryCardText.text);
            Assert.IsTrue(_rig.Ui.Layout.MemoryCard.ClassListContains(MemoryCard.UntitledClass));

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(_rig.Tuning.MemoryCard.Reveal.FadeOut + 0.6f);
            Assert.AreEqual(TestStrings.FirstTape, _rig.Ui.Card.Current, "the liner card waited its turn");
            Assert.AreEqual("Lumen After Dark, Vol. 1", _rig.Ui.Layout.MemoryCardName.text);
            Assert.AreEqual("1/3", _rig.Ui.Layout.MemoryCardCount.text);
            Assert.IsTrue(_rig.Ui.Layout.MemoryCard.ClassListContains(MemoryCard.LinerClass));
        }

        [UnityTest]
        public IEnumerator DialReadout_AnswersATurnOfTheDial_NeverTheLoad()
        {
            Boot();
            _rig.Fakes.DialUnlocked = true;
            _rig.Fakes.Tune(RadioChannel.TapeDeck, TestStrings.SecondTape);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "a program loaded with the save is not a turn of the dial");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            _rig.Fakes.AddTape(TestStrings.FirstTape);
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "a collected tape is not a turn either");

            _rig.Fakes.Tune(RadioChannel.QuietHours, TestStrings.SecondTape);
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Dial.IsVisible);
            Assert.AreEqual(Text(UiKeys.RadioChannelName(RadioChannel.QuietHours)), _rig.Ui.Layout.DialStation.text);
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.DialTape.resolvedStyle.display);

            _rig.Fakes.Tune(RadioChannel.TapeDeck, TestStrings.FirstTape);
            Events.Publish(new TickerLine(TestStrings.HomeKey));
            yield return Seconds(0.5f);
            Assert.AreEqual(Text(UiKeys.RadioChannelName(RadioChannel.TapeDeck)), _rig.Ui.Layout.DialStation.text,
                "turning again rewrites it in place");
            Assert.AreEqual(Text(UiKeys.CassetteTitle(TestStrings.FirstTape)), _rig.Ui.Layout.DialTape.text);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the ticker waits for the readout");

            yield return Seconds(_rig.Tuning.DialReadout.HoldSeconds + _rig.Tuning.DialReadout.Reveal.FadeOut);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "it fades after a moment");
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "then the ticker has its turn");
        }

        [UnityTest]
        public IEnumerator Ticker_SpeaksOnceAwake_MakesWayForACard_AndComesBack()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new TickerLine(TestStrings.SignalKey, "140"));
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the radio waits for 07 to wake");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "then the line drifts in");
            Assert.AreEqual("Bell's picking something up… bearing 140.", _rig.Ui.Layout.TickerText.text);
            Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the ticker never blocks driving");

            Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 2));
            Events.Publish(new TickerLine(TestStrings.HomeKey));
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "a card is coming: the ticker makes way");
            yield return Seconds(0.6f);
            Assert.IsTrue(_rig.Ui.Card.IsVisible);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "never under a card");

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Card.IsVisible);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "the line comes back after the card");
            Assert.AreEqual("Bell's picking something up… bearing 140.", _rig.Ui.Layout.TickerText.text,
                "the interrupted line first, then the next");
            Assert.AreEqual(1, _rig.Ui.TickerLines.Waiting);
        }

        [UnityTest]
        public IEnumerator Ticker_WaitsDuringADig_AndWhilePaused()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            Events.Publish(new ExcavationStarted(new Vector3(0f, 0f, 12f)));
            Events.Publish(new TickerLine(TestStrings.HomeKey));
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "never during a dig");

            Events.Publish(new ExcavationStopped(new Vector3(0f, 0f, 12f), true));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Ticker.IsShown);

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(_rig.Ui.TickerLines.HoldSeconds + 1f);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(0.6f);
            Assert.IsTrue(_rig.Ui.Ticker.IsShown, "the pause held the line where it was");
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        /// <summary>The tables' own words for <paramref name="key"/>, so a test never guesses copy.</summary>
        private string Text(string key)
        {
            return _rig.Ui.Localization.Get(key);
        }

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
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
