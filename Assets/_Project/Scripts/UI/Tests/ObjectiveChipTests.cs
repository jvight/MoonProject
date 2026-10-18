using NUnit.Framework;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The objective's key chip: a step that needs a verb shows its key with its line, retires when the verb is used
    /// in that step, and starts over on the next step that needs it.
    /// </summary>
    public sealed class ObjectiveChipTests
    {
        private ObjectiveChip _chip;

        [SetUp]
        public void SetUp()
        {
            _chip = new ObjectiveChip();
        }

        [TestCase(OpeningKeys.ScanLander, "Scan")]
        [TestCase(OpeningKeys.FindSolar, "Scan")]
        [TestCase(OpeningKeys.ScanTower, "Scan")]
        [TestCase(OpeningKeys.CutOptics, "Cut")]
        [TestCase(OpeningKeys.BuildPad, "Build")]
        [TestCase(OpeningKeys.BuildSolar, "Build")]
        [TestCase(OpeningKeys.BuildBatteryBank, "Build")]
        [TestCase(OpeningKeys.BuildMast, "Build")]
        [TestCase(OpeningKeys.FitIgniter, "Fit")]
        [TestCase(OpeningKeys.DriveToLander, "None")]
        [TestCase(OpeningKeys.CollectParts, "None")]
        [TestCase(OpeningKeys.Charge, "None")]
        [TestCase("", "None")]
        public void EveryStep_NamesItsVerb(string key, string expected)
        {
            Assert.AreEqual(expected, ObjectiveVerbs.Of(key).ToString());
        }

        [Test]
        public void TheChip_ShowsWithItsLine_AndOnlyThen()
        {
            _chip.SetKey(OpeningKeys.BuildPad);
            Assert.AreEqual(HintWord.Build, _chip.Word(true, false, false));
            Assert.AreEqual(HintWord.None, _chip.Word(false, false, false), "no line, no chip");
        }

        [Test]
        public void TheChip_RetiresWhenItsVerbIsUsed_NotOnAnotherVerb()
        {
            _chip.SetKey(OpeningKeys.BuildPad);
            _chip.Used(ObjectiveVerb.Scan);
            Assert.AreEqual(HintWord.Build, _chip.Word(true, false, false));

            _chip.Used(ObjectiveVerb.Build);
            Assert.AreEqual(HintWord.None, _chip.Word(true, false, false));
        }

        [Test]
        public void ANextStepThatNeedsTheVerb_ShowsItAgain()
        {
            _chip.SetKey(OpeningKeys.ScanLander);
            _chip.Used(ObjectiveVerb.Scan);
            _chip.SetKey(OpeningKeys.CollectParts);
            Assert.AreEqual(HintWord.None, _chip.Word(true, false, false), "collecting needs no chip");

            _chip.SetKey(OpeningKeys.FindSolar);
            Assert.AreEqual(HintWord.Scan, _chip.Word(true, false, false));
        }

        [Test]
        public void FittingTheIgniter_OffersTabOnlyWhileItCanBeFittedAndTheFaceIsClosed()
        {
            _chip.SetKey(OpeningKeys.FitIgniter);
            Assert.AreEqual(HintWord.None, _chip.Word(true, false, false), "not docked yet");
            Assert.AreEqual(HintWord.Fit, _chip.Word(true, true, false));
            Assert.AreEqual(HintWord.None, _chip.Word(true, true, true), "the face is open: Tab is answered");

            _chip.Used(ObjectiveVerb.Fit);
            Assert.AreEqual(HintWord.None, _chip.Word(true, true, false));
        }
    }
}
