using NUnit.Framework;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The objective's key chip: it takes a reminder line's place only once the step's object is in reach, names what E
    /// does there (or is doing), and never shows beside a next line.
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
        [TestCase(OpeningKeys.ScanElder06, "Scan")]
        [TestCase(OpeningKeys.CutOptics, "Cut")]
        [TestCase(OpeningKeys.CutHeadlampCrate, "Cut")]
        [TestCase(OpeningKeys.BuildPad, "Build")]
        [TestCase(OpeningKeys.BuildSolar, "Build")]
        [TestCase(OpeningKeys.BuildBatteryBank, "Build")]
        [TestCase(OpeningKeys.BuildMast, "Build")]
        [TestCase(OpeningKeys.FitIgniter, "None")]
        [TestCase(OpeningKeys.DriveToLander, "None")]
        [TestCase(OpeningKeys.CollectParts, "None")]
        [TestCase(OpeningKeys.Charge, "None")]
        [TestCase(OpeningKeys.ParkOnPad, "None")]
        [TestCase("", "None")]
        public void EveryStep_NamesItsVerb(string key, string expected)
        {
            Assert.AreEqual(expected, ObjectiveVerbs.Of(key).ToString());
        }

        [Test]
        public void TheChip_ShowsOnlyOnceTheStepsObjectIsInReach_NeverBeforeIt()
        {
            _chip.SetKey(OpeningKeys.ScanLander);
            Assert.AreEqual(HintWord.None, Word(true, InteractionKind.None), "nothing in reach: the line says it");
            Assert.AreEqual(HintWord.Scan, Word(true, InteractionKind.Scan), "a fragment in reach: its key shows");
            Assert.AreEqual(HintWord.None, Word(true, InteractionKind.PickUp), "another verb in reach is not this step's");
            Assert.AreEqual(HintWord.Scan, Word(true, InteractionKind.Scan), "and again at the next fragment");
        }

        [Test]
        public void ANextLine_HasNoChip_ItDrawsItsOwnKey()
        {
            _chip.SetKey(OpeningKeys.BuildPad);
            Assert.AreEqual(HintWord.None, Word(false, InteractionKind.Build));
            Assert.AreEqual(HintWord.None, Word(false, InteractionKind.Salvage));
            Assert.AreEqual(HintWord.None, Word(false, InteractionKind.None, InteractionKind.Salvage));
        }

        [Test]
        public void ANewStep_NamesItsOwnVerb()
        {
            _chip.SetKey(OpeningKeys.ScanLander);
            _chip.SetKey(OpeningKeys.CollectParts);
            Assert.AreEqual(HintWord.None, Word(true, InteractionKind.PickUp), "collecting needs no chip");

            _chip.SetKey(OpeningKeys.BuildPad);
            Assert.AreEqual(HintWord.Build, Word(true, InteractionKind.Build), "at the weld point");
        }

        [Test]
        public void AnETargetInReach_NamesWhatEDoesThere_NotTheStepsVerb()
        {
            _chip.SetKey(OpeningKeys.ScanLander);
            Assert.AreEqual(HintWord.Cut, Word(true, InteractionKind.Salvage),
                "a crate seal is the candidate on a scan step: E cuts it");
            _chip.SetKey(OpeningKeys.BuildPad);
            Assert.AreEqual(HintWord.Cut, Word(true, InteractionKind.Salvage),
                "a cable reel on a build step: E cuts it, B is not what E does");
            Assert.AreEqual(HintWord.Scan, Word(true, InteractionKind.Scan));
        }

        [Test]
        public void WhileEIsHeld_TheChipNamesWhatEIsDoing_WhateverTheCandidateBecomes()
        {
            _chip.SetKey(OpeningKeys.ScanLander);
            Assert.AreEqual(HintWord.Cut, Word(true, InteractionKind.Scan, InteractionKind.Salvage),
                "cutting while a scan target slides in front: still Cut");
            Assert.AreEqual(HintWord.Cut, Word(true, InteractionKind.None, InteractionKind.Salvage),
                "cutting with nothing offered: still Cut");
            Assert.AreEqual(HintWord.Scan, Word(true, InteractionKind.Salvage, InteractionKind.Scan),
                "scanning while a seal comes into reach: still Scan");
        }

        private HintWord Word(bool reminderUp, InteractionKind offered, InteractionKind held = InteractionKind.None)
        {
            return _chip.Word(reminderUp, offered, held);
        }
    }
}
