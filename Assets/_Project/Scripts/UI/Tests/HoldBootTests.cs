using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The first Tab press: a boot of about 1.2 s (type, light the slots, slide the items in), once per save, taught by
    /// one "Tab · Hold" chip at the tower.
    /// </summary>
    public sealed class HoldBootTests
    {
        private const float Frame = 1f / 60f;

        private CargoScreenSettings _settings;
        private HoldBoot _boot;

        [SetUp]
        public void SetUp()
        {
            _settings = new CargoScreenSettings();
            _boot = new HoldBoot(_settings);
        }

        [Test]
        public void TheBoot_TakesAboutOnePointTwoSeconds_ForTheStarterHold()
        {
            _boot.Start(8, 2);
            Assert.That(_boot.TotalSeconds, Is.InRange(1f, 1.4f));
        }

        [Test]
        public void TheSlotsLight_LeftToRight_SixtyMillisecondsApart_AfterTheLineTypes()
        {
            _boot.Start(8, 2);
            Assert.AreEqual(0, _boot.LitCount, "dark first");
            Assert.AreEqual(0f, _boot.TypedFraction);
            Run(_settings.BootTypeSeconds * 0.5f);
            Assert.AreEqual(0.5f, _boot.TypedFraction, 0.05f);
            Assert.AreEqual(0, _boot.LitCount, "no outline before the line is typed");

            Run(_settings.BootTypeSeconds * 0.5f + _settings.BootLightStepSeconds * 0.5f);
            Assert.AreEqual(1, _boot.LitCount);
            Run(_settings.BootLightStepSeconds * 3f);
            Assert.AreEqual(4, _boot.LitCount, 1, "one more every 60 ms");
            Run(5f);
            Assert.AreEqual(8, _boot.LitCount);
        }

        [Test]
        public void TheItems_SlideIn_OneAfterAnother_OnlyAfterTheOutlinesAreLit()
        {
            _boot.Start(8, 2);
            Run(_settings.BootTypeSeconds + _settings.BootLightStepSeconds * 7f);
            Assert.AreEqual(0f, _boot.ItemProgress(0), "the lights come first");
            Run(_settings.BootLightStepSeconds * 2f + _settings.BootSlideSeconds * 0.5f);
            Assert.Greater(_boot.ItemProgress(0), 0f);
            Assert.Less(_boot.ItemProgress(1), _boot.ItemProgress(0), "the second follows the first");
            Assert.AreEqual(0, _boot.ArrivedCount);

            Run(_boot.TotalSeconds);
            Assert.AreEqual(2, _boot.ArrivedCount);
            Assert.IsFalse(_boot.IsRunning, "done after the last item");
        }

        [Test]
        public void AnEmptyHold_StillLightsItsSlots_AndEnds()
        {
            _boot.Start(8, 0);
            Run(_boot.TotalSeconds + 0.1f);
            Assert.AreEqual(8, _boot.LitCount);
            Assert.AreEqual(0, _boot.ArrivedCount);
            Assert.IsFalse(_boot.IsRunning);
        }

        [Test]
        public void StoppingTheBoot_EndsItAtOnce()
        {
            _boot.Start(8, 2);
            _boot.Step(0.2f);
            _boot.Stop();
            Assert.IsFalse(_boot.IsRunning);
        }

        [Test]
        public void TheLedger_RemembersTheBoot_AndTheChip_InTheSave()
        {
            var ledger = new PromptLedger(new PromptSettings());
            Assert.IsFalse(ledger.HoldBooted);
            ledger.RecordHoldBooted();
            ledger.RecordHoldChipShown();

            var restored = new PromptLedger(new PromptSettings());
            restored.Restore(ledger.Capture());
            Assert.IsTrue(restored.HoldBooted);
            Assert.IsTrue(restored.HoldChipShown);
        }

        [Test]
        public void TheTabChip_ShowsOnceAtTheTower_AndLeavesWhenTheHoldOpens()
        {
            var stargaze = new StargazeSettings();
            var ledger = new PromptLedger(new PromptSettings());
            var hint = new HoldHint(stargaze, ledger);
            hint.Tick(Frame, true, false);
            Assert.IsFalse(hint.WantsShown, "only at the tower");

            hint.Tick(Frame, false, true);
            Assert.IsFalse(hint.WantsShown, "not while something else holds the screen");

            hint.Tick(Frame, true, true);
            Assert.IsTrue(hint.WantsShown);
            Assert.IsTrue(ledger.HoldChipShown, "once per save");

            ledger.RecordHoldBooted();
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown, "pressing Tab answers it");
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown, "and it never returns");
        }

        [Test]
        public void TheTabChip_NeverShows_ToAPlayerWhoAlreadyOpenedTheHold()
        {
            var ledger = new PromptLedger(new PromptSettings());
            ledger.RecordHoldBooted();
            var hint = new HoldHint(new StargazeSettings(), ledger);
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown);
            Assert.IsFalse(hint.Wants(true));
        }

        [Test]
        public void TheTabChip_LeavesAfterItsHoldTime()
        {
            var stargaze = new StargazeSettings();
            var hint = new HoldHint(stargaze, new PromptLedger(new PromptSettings()));
            hint.Tick(Frame, true, true);
            hint.Tick(stargaze.HintHoldSeconds + 1f, true, true);
            Assert.IsFalse(hint.WantsShown);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _boot.Step(Frame);
            }
        }
    }
}
