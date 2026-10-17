using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The face inventory's selection belongs to the last input used (keys/pad or a really moving mouse).</summary>
    public sealed class InputHandoffTests
    {
        [Test]
        public void ANewScreen_IsTheKeysUntilTheMouseReallyMoves()
        {
            var handoff = new InputHandoff();

            Assert.IsFalse(handoff.HoverMaySelect, "a pointer resting over the screen does not select");
            handoff.NoteMouse();
            Assert.IsTrue(handoff.HoverMaySelect);
        }

        [Test]
        public void AnArrowPress_TakesTheSelectionBackFromTheMouse()
        {
            var handoff = new InputHandoff();
            handoff.NoteMouse();

            handoff.NoteNavigation();

            Assert.IsFalse(handoff.HoverMaySelect, "a cell sliding under a still pointer cannot steal the arrows' place");
        }

        [Test]
        public void TheLastInputUsedWins_InEitherOrder()
        {
            var handoff = new InputHandoff();
            handoff.NoteNavigation();
            handoff.NoteMouse();
            handoff.NoteMouse();
            Assert.IsTrue(handoff.MouseIsActive, "hover then arrows continue from the hovered slot");
            handoff.NoteNavigation();
            handoff.NoteNavigation();
            Assert.IsFalse(handoff.MouseIsActive, "arrows then hover is ignored until the mouse moves");
        }

        [Test]
        public void Reset_HandsTheScreenToTheKeys()
        {
            var handoff = new InputHandoff();
            handoff.NoteMouse();

            handoff.Reset();

            Assert.IsFalse(handoff.HoverMaySelect);
        }
    }
}
