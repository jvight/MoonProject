using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The one guidance slot: the highest wanting element holds it, a lower one waits, and the slot passes on only once
    /// the screen is clear, so two guidance elements are never on screen together.
    /// </summary>
    public sealed class GuidanceSlotTests
    {
        private readonly bool[] _wants = new bool[GuidanceSlot.KindCount];
        private readonly bool[] _visible = new bool[GuidanceSlot.KindCount];
        private GuidanceSlot _slot;

        [SetUp]
        public void SetUp()
        {
            _slot = new GuidanceSlot();
            System.Array.Clear(_wants, 0, _wants.Length);
            System.Array.Clear(_visible, 0, _visible.Length);
        }

        [Test]
        public void NothingWanting_GrantsNothing()
        {
            _slot.Step(_wants, _visible);
            Assert.IsNull(_slot.Granted);
        }

        [Test]
        public void TheHighestWantingKind_HoldsTheSlot()
        {
            _wants[(int)GuidanceKind.Battery] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Battery));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective), "the lower one waits");

            _wants[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Battery));
        }

        [Test]
        public void ALowerElementOnScreen_IsFadedOutBeforeTheHigherOneStarts()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _visible[(int)GuidanceKind.Objective] = true;
            _slot.Step(_wants, _visible);
            Assert.IsNull(_slot.Granted, "nobody may show while the objective line is still fading");

            _visible[(int)GuidanceKind.Objective] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
        }

        [Test]
        public void TheHolderKeepsTheSlot_WhileItIsTheOnlyOneUp()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _visible[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
        }

        [Test]
        public void AWrongSizedFlagArray_IsRefused()
        {
            Assert.Throws<System.ArgumentException>(() => _slot.Step(new bool[2], _visible));
        }
    }
}
