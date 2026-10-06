using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Meeting Tilly end to end: warm pips over her while 07 is near, filling as parts come back; the repair prompt the
    /// first times; and when she wakes, her name over her and Ines's log on the story card.
    /// </summary>
    public sealed class FriendUiTests : InputTestFixture
    {
        private static readonly Vector3 Site = new Vector3(0f, 0f, 10f);

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
        public IEnumerator PartsReadout_ShowsNearABrokenFriend_FillsAsPartsReturn_AndLeaves()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Tilly(FriendState.Dormant, 0);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.FriendReadout.IsVisible, "07 is far away: nothing on screen");

            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -8f);
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.FriendReadout.IsVisible);
            Assert.AreEqual(3, _rig.Ui.FriendReadout.PipCount, "one pip per missing part");
            Assert.AreEqual(0f, _rig.Ui.FriendReadout.ShownParts);

            Tilly(FriendState.PartsGathering, 1);
            yield return null;
            Assert.Less(_rig.Ui.FriendReadout.ShownParts, 1f, "a returning part fills its pip, never pops it on");
            yield return Seconds(_rig.Tuning.Friends.PipFillSeconds + 0.2f);
            Assert.AreEqual(1f, _rig.Ui.FriendReadout.ShownParts);

            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -(_rig.Tuning.Friends.HideDistance + 5f));
            yield return Seconds(1.2f);
            Assert.IsFalse(_rig.Ui.FriendReadout.IsVisible, "driving away lets it go");

            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -8f);
            Tilly(FriendState.Repairing, 3);
            yield return Seconds(1.2f);
            Assert.IsFalse(_rig.Ui.FriendReadout.IsVisible, "once the repair starts the readout has said its piece");
        }

        [UnityTest]
        public IEnumerator RepairPrompt_NamesTheDigKey_AndBowsOutWhenTheRepairStarts()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -4f);
            Tilly(FriendState.PartsGathering, 3);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Repair, Site, true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual("Repair", _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text);
            Assert.IsTrue(_rig.Ui.FriendReadout.IsVisible);
            Rect readout = _rig.Ui.Layout.FriendReadout.worldBound;
            Rect prompt = _rig.Ui.Layout.Prompt.worldBound;
            Assert.LessOrEqual(readout.yMax, prompt.yMin + 1f, "the full pips rest on top of the prompt, not under it");
            Assert.Less(Mathf.Abs(readout.center.x - prompt.center.x), 2f, "in one column");

            Events.Publish(new FriendRepairStarted("tilly"));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Repair));
        }

        [UnityTest]
        public IEnumerator Waking_ShowsHerName_ThenInessLog()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -6f);
            Tilly(FriendState.Awake, 3);
            yield return null;

            Events.Publish(new FriendRepaired("tilly"));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.FriendName.IsVisible);
            Assert.AreEqual("Tilly", _rig.Ui.Layout.FriendName.text);
            Assert.IsTrue(_rig.Ui.Card.IsVisible);
            Assert.AreEqual("tilly", _rig.Ui.Card.Current);
            Assert.AreEqual("From Tilly's memory", _rig.Ui.Layout.MemoryCardCaption.text);
            Assert.AreEqual("Tilly", _rig.Ui.Layout.MemoryCardName.text);
            StringAssert.StartsWith("Walked the rim at sunrise", _rig.Ui.Layout.MemoryCardText.text);

            _rig.Ui.Localization.SetLanguage("vi");
            yield return null;
            Assert.AreEqual("Từ ký ức của Tilly", _rig.Ui.Layout.MemoryCardCaption.text, "the card changes in place");

            Tilly(FriendState.Awake, 3, Site + Vector3.up * 2f);
            yield return Seconds(_rig.Tuning.Friends.NameHoldSeconds + _rig.Tuning.Friends.Name.FadeOut + 0.5f);
            Assert.IsFalse(_rig.Ui.FriendName.IsVisible, "her name rests, then fades away");
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
        }

        private void Tilly(FriendState state, int collected)
        {
            Tilly(state, collected, Site);
        }

        private void Tilly(FriendState state, int collected, Vector3 position)
        {
            _rig.Fakes.TillyStatus = new FriendStatus(state, collected, 3, true,
                state == FriendState.PartsGathering && collected == 3, position);
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
    }
}
