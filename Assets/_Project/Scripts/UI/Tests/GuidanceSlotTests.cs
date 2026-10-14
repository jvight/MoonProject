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
            _wants[(int)GuidanceKind.Place] = true;
            _wants[(int)GuidanceKind.Next] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Place));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Next), "the lower one waits");

            _wants[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Place));
        }

        [Test]
        public void ALowerElementOnScreen_IsFadedOutBeforeTheHigherOneStarts()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _wants[(int)GuidanceKind.Next] = true;
            _visible[(int)GuidanceKind.Next] = true;
            _slot.Step(_wants, _visible);
            Assert.IsNull(_slot.Granted, "nobody may show while the next line is still fading");

            _visible[(int)GuidanceKind.Next] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
        }

        [Test]
        public void TheHolderKeepsTheSlot_WhileItIsTheOnlyOneUp()
        {
            _wants[(int)GuidanceKind.Card] = true;
            _visible[(int)GuidanceKind.Card] = true;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Card));
        }

        [Test]
        public void TheTicker_NeverSharesTheSlot_WaitsForHigherKinds_AndStepsAheadOfTheNextLine()
        {
            _wants[(int)GuidanceKind.Ticker] = true;
            _wants[(int)GuidanceKind.Next] = true;
            _visible[(int)GuidanceKind.Next] = true;
            _slot.Step(_wants, _visible);
            Assert.IsNull(_slot.Granted, "the next line fades out before the ticker starts");

            _visible[(int)GuidanceKind.Next] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Ticker));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Next));

            _visible[(int)GuidanceKind.Ticker] = true;
            _wants[(int)GuidanceKind.Place] = true;
            _slot.Step(_wants, _visible);
            Assert.IsNull(_slot.Granted, "a place card waits for the ticker to fade out");

            _visible[(int)GuidanceKind.Ticker] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Place));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Ticker), "the ticker waits while the place card holds the slot");

            _wants[(int)GuidanceKind.Place] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Ticker), "then the ticker gets its turn");

            _wants[(int)GuidanceKind.Ticker] = false;
            _slot.Step(_wants, _visible);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Next), "and once the ticker is done, the next line returns");
        }

        [Test]
        public void AWrongSizedFlagArray_IsRefused()
        {
            Assert.Throws<System.ArgumentException>(() => _slot.Step(new bool[2], _visible));
        }
    }
}
