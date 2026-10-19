using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The one guidance slot: the highest wanting element holds it, a lower one waits, and the slot passes on only once
    /// the screen is clear, except that a prompt cross-fades over the objective line so the screen is never blank.
    /// </summary>
    public sealed class GuidanceSlotTests
    {
        private const float Frame = 1f / 60f;
        private const float HandOver = 0.3f;

        private readonly bool[] _wants = new bool[GuidanceSlot.KindCount];
        private readonly bool[] _visible = new bool[GuidanceSlot.KindCount];
        private GuidanceSlot _slot;

        [SetUp]
        public void SetUp()
        {
            _slot = new GuidanceSlot(HandOver);
            System.Array.Clear(_wants, 0, _wants.Length);
            System.Array.Clear(_visible, 0, _visible.Length);
        }

        [Test]
        public void NothingWanting_GrantsNothing()
        {
            _slot.Step(_wants, _visible, Frame);
            Assert.IsNull(_slot.Granted);
        }

        [Test]
        public void TheHighestWantingKind_HoldsTheSlot()
        {
            _wants[(int)GuidanceKind.Battery] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Battery));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective), "the lower one waits");

            _wants[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Battery));
        }

        [Test]
        public void TheFacesStoryLine_TakesTheSlotFromEverything_WhichFadesFirstAndComesBackAfter()
        {
            _wants[(int)GuidanceKind.CallLine] = true;
            _wants[(int)GuidanceKind.Prompt] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _visible[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsNull(_slot.Granted, "nothing shows while the prompt is still fading");
            _visible[(int)GuidanceKind.Prompt] = false;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.CallLine), "the line is the one element");
            Assert.IsFalse(_slot.Allows(GuidanceKind.Prompt));
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective));

            _wants[(int)GuidanceKind.CallLine] = false;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt), "then the rest come back, one at a time");
        }

        [Test]
        public void ALowerElementOnScreen_IsFadedOutBeforeTheHigherOneStarts()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _wants[(int)GuidanceKind.Battery] = true;
            _visible[(int)GuidanceKind.Battery] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsNull(_slot.Granted, "nobody may show while the battery hint is still fading");

            _visible[(int)GuidanceKind.Battery] = false;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
        }

        [Test]
        public void APromptTakingOverFromTheLine_StartsAtOnce_AndTheLineStaysUntilThePromptHasFadedInAWhile()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _visible[(int)GuidanceKind.Objective] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt), "the prompt starts over the line, no blank wait");
            Assert.IsTrue(_slot.Allows(GuidanceKind.Objective), "the line keeps its place until the prompt is up");

            _visible[(int)GuidanceKind.Prompt] = true;
            float t = 0f;
            while (_slot.Allows(GuidanceKind.Objective) && t < 1f)
            {
                t += Frame;
                _slot.Step(_wants, _visible, Frame);
            }

            Assert.AreEqual(HandOver, t, 2f * Frame, "the two cross-fade for the hand-over time, then the line goes");
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));

            _visible[(int)GuidanceKind.Objective] = false;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsFalse(_slot.HandingOver);
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective), "the prompt alone holds the slot");
        }

        [Test]
        public void TheLineIsHeldOnlyUnderAPrompt_NotUnderTheBatteryHintOrABuildStep()
        {
            _wants[(int)GuidanceKind.Battery] = true;
            _wants[(int)GuidanceKind.Objective] = true;
            _visible[(int)GuidanceKind.Objective] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsNull(_slot.Granted, "the battery hint waits for the line to fade");
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective));

            _wants[(int)GuidanceKind.BuildStep] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsNull(_slot.Granted);
            Assert.IsFalse(_slot.Allows(GuidanceKind.Objective));
        }

        [Test]
        public void ANegativeHandOver_IsRefused()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GuidanceSlot(-1f));
        }

        [Test]
        public void TheHolderKeepsTheSlot_WhileItIsTheOnlyOneUp()
        {
            _wants[(int)GuidanceKind.Prompt] = true;
            _visible[(int)GuidanceKind.Prompt] = true;
            _slot.Step(_wants, _visible, Frame);
            Assert.IsTrue(_slot.Allows(GuidanceKind.Prompt));
        }

        [Test]
        public void AWrongSizedFlagArray_IsRefused()
        {
            Assert.Throws<System.ArgumentException>(() => _slot.Step(new bool[2], _visible, Frame));
        }
    }
}
