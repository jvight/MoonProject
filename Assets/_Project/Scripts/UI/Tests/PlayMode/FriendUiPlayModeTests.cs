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
    /// first times; and when she wakes, her name over her with no story card.
    /// </summary>
    public sealed class FriendUiPlayModeTests : InputTestFixture
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
            UiTestRig.DeleteSlotFiles(_slot);
            Object.Destroy(_controls);
            base.TearDown();
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

            Events.Publish(new FriendRepairStarted("tilly"));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Repair));
        }

        [UnityTest]
        public IEnumerator Waking_ShowsHerName_WithNoStoryCard()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Fakes.Position = Site + new Vector3(0f, 0f, -6f);
            Tilly(FriendState.Awake, 3);
            yield return null;

            Events.Publish(new FriendRepaired("tilly"));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.FriendName.IsVisible);
            Assert.AreEqual("Tilly", _rig.Ui.Layout.FriendNameText.text);
            Assert.IsFalse(_rig.Ui.Card.IsVisible, "no story card on a friend's wake");
            Assert.IsFalse(_rig.Ui.Card.IsBusy, "nothing queued on the story card");

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
