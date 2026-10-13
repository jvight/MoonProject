using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The low-battery hint: Gameplay's one line when the charge first falls to 30%, kept as guidance (never on the
    /// ticker), shown for a short while, gone when a charger starts and never said at full.
    /// </summary>
    public sealed class LowBatteryHintTests
    {
        private const float Frame = 1f / 60f;

        private readonly StubBattery _battery = new StubBattery();
        private GuidanceSettings _settings;
        private ILocalization _localization;
        private LowBatteryHint _hint;

        [SetUp]
        public void SetUp()
        {
            _settings = new GuidanceSettings();
            _battery.Charging = false;
            _battery.Fraction = 0.3f;
            _localization = TestTables.Localization(new EventBus(),
                LowBatteryHint.DockKey, "Battery low. Charge at the dock at home.", "Pin yếu. Sạc ở đế tại nhà.",
                LowBatteryHint.MastKey, "Battery low. You can charge at {0}.", "Pin yếu. Có thể sạc ở {0}.",
                "hop.node.mast", "the Mast", "Cột");
            _hint = new LowBatteryHint(_settings, _battery, _localization);
        }

        [Test]
        public void OnlyGameplaysTwoLowLines_AreTheHint()
        {
            Assert.IsTrue(LowBatteryHint.IsLowLine(new TickerLine(LowBatteryHint.DockKey)));
            Assert.IsTrue(LowBatteryHint.IsLowLine(new TickerLine(LowBatteryHint.MastKey, "hop.node.mast", true)));
            Assert.IsFalse(LowBatteryHint.IsLowLine(new TickerLine("ticker.relay.online")));
        }

        [Test]
        public void AnOfferedLine_WantsTheScreen_WithItsWords_AndGoesAfterItsHoldTime()
        {
            _hint.Tick(Frame, false);
            Assert.IsFalse(_hint.WantsShown, "nothing offered, nothing wanted");

            _hint.Offer(new TickerLine(LowBatteryHint.MastKey, "hop.node.mast", true));
            _hint.Tick(Frame, false);
            Assert.IsTrue(_hint.WantsShown);
            Assert.AreEqual("Battery low. You can charge at the Mast.", _hint.Text);

            _hint.Tick(_settings.LowBatteryHoldSeconds + 1f, true);
            Assert.IsFalse(_hint.WantsShown, "said once, for a short while");
            _hint.Tick(Frame, false);
            Assert.IsFalse(_hint.WantsShown, "and not again");
        }

        [Test]
        public void AWaitingHint_KeepsWaiting_UntilItIsShown()
        {
            _hint.Offer(new TickerLine(LowBatteryHint.DockKey));
            _hint.Tick(_settings.LowBatteryHoldSeconds * 3f, false);
            Assert.IsTrue(_hint.WantsShown, "the hold time counts only while it is on screen");
        }

        [Test]
        public void ACharger_EndsTheHint_AndAFullChargeNeverGetsOne()
        {
            _hint.Offer(new TickerLine(LowBatteryHint.DockKey));
            _battery.Charging = true;
            _hint.Tick(Frame, true);
            Assert.IsFalse(_hint.WantsShown, "charging ends it at once");

            _battery.Charging = false;
            _battery.Fraction = 1f;
            _hint.Offer(new TickerLine(LowBatteryHint.DockKey));
            _hint.Tick(Frame, false);
            Assert.IsFalse(_hint.WantsShown, "never at full");
        }

        [Test]
        public void ANewLanguage_RewritesTheWords()
        {
            _hint.Offer(new TickerLine(LowBatteryHint.DockKey));
            string before = _hint.Text;
            _hint.Relocalize();
            Assert.AreEqual(before, _hint.Text);
        }

        private sealed class StubBattery : IRoverBattery
        {
            public bool Charging { get; set; }

            public float Fraction { get; set; }

            public int Cells => 4;

            public float Charge => Fraction * Cells;

            public float Charge01 => Fraction;

            public bool IsLowPower => false;

            public float DriveCap01 => 1f;

            public bool CanLeap => true;

            public float Drums01 => 0f;

            public bool IsCharging => Charging;

            public ChargerKind Charger => default;
        }
    }
}
