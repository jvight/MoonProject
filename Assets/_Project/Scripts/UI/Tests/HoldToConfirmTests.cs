using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The tower's hold-to-confirm ring: no accidental purchases, nothing snaps, exactly one purchase per hold.
    /// </summary>
    public sealed class HoldToConfirmTests
    {
        private const float Frame = 1f / 60f;

        private TowerPanelSettings _settings;
        private HoldToConfirm _hold;
        private int _starts;

        [SetUp]
        public void SetUp()
        {
            _settings = new TowerPanelSettings();
            _hold = new HoldToConfirm(_settings);
            _starts = 0;
        }

        [Test]
        public void AFullHold_ConfirmsOnce_AfterTheHoldTime()
        {
            Assert.AreEqual(0, Run(true, false, 0.1f), "releasing arms the ring");
            Assert.AreEqual(0, Run(true, true, _settings.HoldSeconds - 0.05f), "not before the hold time");
            Assert.Greater(_hold.Progress, 0.8f);
            Assert.AreEqual(1, Run(true, true, 0.1f), "it confirms when full");
            Assert.AreEqual(0, Run(true, true, 3f), "holding on does not buy again");
            Assert.AreEqual(1f, _hold.Progress, "the ring stays full while still held");
        }

        [Test]
        public void EachArmedPress_ReportsOneStart()
        {
            Run(true, false, Frame);
            Run(true, true, _settings.HoldSeconds * 0.3f);
            Assert.AreEqual(1, _starts, "the ring starting is reported once");
            Run(true, false, Frame);
            Run(true, true, _settings.HoldSeconds + 0.1f);
            Assert.AreEqual(2, _starts, "a fresh press after letting go starts again");
            Run(true, true, 1f);
            Assert.AreEqual(2, _starts, "holding a full ring starts nothing new");
            Run(false, true, 1f);
            Assert.AreEqual(2, _starts, "an unavailable offer never starts");
        }

        [Test]
        public void AButtonAlreadyHeld_WhenTheOfferAppears_NeverBuys()
        {
            Run(false, true, 1f);
            Assert.AreEqual(0, Run(true, true, 5f), "driving onto the pad with the button down is not a purchase");
            Assert.AreEqual(0f, _hold.Progress);
            Run(true, false, Frame);
            Assert.AreEqual(1, Run(true, true, _settings.HoldSeconds + 0.05f), "a fresh press works");
        }

        [Test]
        public void LettingGoEarly_DrainsTheRingSmoothly()
        {
            Run(true, false, Frame);
            Run(true, true, _settings.HoldSeconds * 0.5f);
            float held = _hold.Progress;
            Run(true, false, Frame);
            Assert.Greater(_hold.Progress, 0f, "no snap to empty");
            Assert.Less(_hold.Progress, held, "it drains");
            Run(true, false, _settings.DrainSeconds);
            Assert.AreEqual(0f, _hold.Progress);
        }

        [Test]
        public void TheOfferGoingAway_StopsTheFill_AndNeedsAFreshPress()
        {
            Run(true, false, Frame);
            Run(true, true, _settings.HoldSeconds * 0.5f);
            Run(false, true, 0.1f);
            Assert.AreEqual(0, Run(true, true, 2f), "the hold that was interrupted does not resume and buy");
        }

        [Test]
        public void AnUnaffordableOffer_NeverFills()
        {
            Run(false, false, Frame);
            Assert.AreEqual(0, Run(false, true, 2f));
            Assert.AreEqual(0f, _hold.Progress);
        }

        private int Run(bool available, bool held, float seconds)
        {
            int confirmations = 0;
            for (float t = 0f; t < seconds - 1e-6f; t += Frame)
            {
                HoldStep step = _hold.Step(available, held, Frame);
                if (step == HoldStep.Confirmed)
                {
                    confirmations++;
                }
                else if (step == HoldStep.Started)
                {
                    _starts++;
                }
            }

            return confirmations;
        }
    }
}
