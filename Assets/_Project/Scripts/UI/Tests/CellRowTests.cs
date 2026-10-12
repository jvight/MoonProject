using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The cell row's show rules and the cell it calls "in use" (docs/design/first-hour-v2 6.4).</summary>
    public sealed class CellRowTests
    {
        private const float Step = 0.1f;

        [Test]
        public void TheRow_StaysHidden_UntilItHasRevealedItself()
        {
            var settings = new CellRowSettings();
            var rules = new CellRowRules(settings);

            rules.Tick(Step, false, true, false, false, false);

            Assert.IsFalse(rules.Shown, "a game that never dropped below the reveal share has no row");
        }

        [Test]
        public void TheRow_ShowsWhileNotFull_AndWhileCharging_ThenHoldsAFewSecondsAfterFull()
        {
            var settings = new CellRowSettings();
            var rules = new CellRowRules(settings);

            rules.Tick(Step, true, true, false, false, false);
            Assert.IsTrue(rules.Shown, "not full");
            rules.Tick(Step, true, true, true, true, false);
            Assert.IsTrue(rules.Shown, "a charger is still filling the cells");

            Run(rules, settings.FullHoldSeconds * 0.5f, true);
            Assert.IsTrue(rules.Shown, "the last cell is seen filling");
            Run(rules, settings.FullHoldSeconds, true);
            Assert.IsFalse(rules.Shown, "full and settled");

            rules.Tick(Step, true, true, false, false, false);
            Assert.IsTrue(rules.Shown, "a cell spent brings it back");
        }

        [Test]
        public void ARow_RevealedOnAFullLoad_DoesNotFlashForTheHold()
        {
            var settings = new CellRowSettings();
            var rules = new CellRowRules(settings);

            rules.Tick(Step, true, true, true, false, false);

            Assert.IsFalse(rules.Shown);
        }

        [Test]
        public void TheBeamWorking_DimsTheRow_ButNeverHidesIt()
        {
            var settings = new CellRowSettings();
            var rules = new CellRowRules(settings);

            rules.Tick(Step, true, true, false, false, true);

            Assert.IsTrue(rules.Shown);
            Assert.AreEqual(settings.DimShare, rules.Dim);
            rules.Tick(Step, true, true, false, false, false);
            Assert.AreEqual(1f, rules.Dim);
        }

        [Test]
        public void ANeedForTheScreen_HidesTheRow()
        {
            var rules = new CellRowRules(new CellRowSettings());

            rules.Tick(Step, true, false, false, false, false);

            Assert.IsFalse(rules.Shown);
        }

        [TestCase(4, 2.5f, 2)]
        [TestCase(4, 0.3f, 0)]
        [TestCase(4, 3f, -1)]
        [TestCase(4, 4f, -1)]
        [TestCase(4, 0f, -1)]
        [TestCase(6, 5.9f, 5)]
        public void TheCellInUse_IsTheLastLitOneWhileItIsPartFull(int cells, float charge, int expected)
        {
            Assert.AreEqual(expected, CellRowRules.InUse(cells, charge));
        }

        private static void Run(CellRowRules rules, float seconds, bool full)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                rules.Tick(Step, true, true, full, false, false);
            }
        }
    }
}
