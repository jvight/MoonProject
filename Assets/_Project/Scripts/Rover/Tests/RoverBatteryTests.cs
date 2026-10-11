using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The battery on 07's side (docs/features/M3-20): the leap refuses at the lip, the crawl caps the drive softly,
    /// and the flank lamps show the cells.
    /// </summary>
    public sealed class RoverBatteryTests
    {
        private const float Step = 0.02f;

        [Test]
        public void An_UnaffordableLeap_IsRefusedOncePerPress_AndTheCoilsNeverCharge()
        {
            var jump = new HoverJump(new HoverJumpSettings());
            jump.Step(true, false, true, false, Step);

            Assert.AreEqual(HoverJumpEvent.Refused, jump.Step(true, true, true, false, Step));
            Assert.IsFalse(jump.IsCharging);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(HoverJumpEvent.None, jump.Step(true, true, true, false, Step), "held: no re-refusal");
            }

            Assert.AreEqual(HoverJumpEvent.None, jump.Step(true, false, true, false, Step), "released: no leap");
            Assert.AreEqual(0f, jump.Charge);
        }

        [Test]
        public void An_AffordableLeap_ChargesAsAlways()
        {
            var jump = new HoverJump(new HoverJumpSettings());
            jump.Step(true, false, true, true, Step);

            Assert.AreEqual(HoverJumpEvent.ChargeProgress, jump.Step(true, true, true, true, Step));
            Assert.IsTrue(jump.IsCharging);
        }

        [Test]
        public void TheCrawlCap_LowersTheTopSpeed_AndAFaster07CoastsDownSoftly()
        {
            var drive = new DriveSettings();
            float crawl = 0.35f * drive.TopSpeed;

            Assert.Greater(LongitudinalDrive.Acceleration(drive, crawl * 0.5f, 1f, 0f, 0.35f, Step), 0f,
                "below the cap it still accelerates");
            Assert.AreEqual(0f, LongitudinalDrive.Acceleration(drive, crawl, 1f, 0f, 0.35f, Step), 1e-4f,
                "it settles on the cap");

            float coast = LongitudinalDrive.Acceleration(drive, drive.TopSpeed, 1f, 0f, 0.35f, Step);
            Assert.Less(coast, 0f, "above the cap it slows");
            Assert.GreaterOrEqual(coast, -drive.CoastDeceleration, "by coasting, never braking");
            Assert.AreEqual(LongitudinalDrive.Acceleration(drive, 2f, 1f, 0f, Step),
                LongitudinalDrive.Acceleration(drive, 2f, 1f, 0f, 1f, Step), "a full battery drives as always");
        }

        [Test]
        public void TheFlankLamps_LightFullCells_BreatheTheDrawnOne_AndLeaveEmptyGlassDark()
        {
            var lamps = new FlankLampSettings();

            Assert.AreEqual(lamps.LitGlow, FlankLampStrip.Level(lamps, 0, 2.5f, false, 0f));
            Assert.AreEqual(lamps.LitGlow, FlankLampStrip.Level(lamps, 1, 2.5f, false, 0f));
            Assert.AreEqual(lamps.LitGlow * 0.5f, FlankLampStrip.Level(lamps, 2, 2.5f, false, 0f), 1e-5f,
                "the drawn cell at the top of its breath");
            float low = FlankLampStrip.Level(lamps, 2, 2.5f, false, lamps.BreathPeriod * 0.5f);
            Assert.AreEqual(lamps.LitGlow * 0.5f * (1f - lamps.BreathDepth), low, 1e-5f, "and at its bottom");
            Assert.AreEqual(0f, FlankLampStrip.Level(lamps, 3, 2.5f, false, 0f), "dark glass");
        }

        [Test]
        public void Crawling_OnlyTheFirstLampBreathes_Faintly()
        {
            var lamps = new FlankLampSettings();

            Assert.AreEqual(lamps.ReserveGlow, FlankLampStrip.Level(lamps, 0, 0f, true, 0f), 1e-5f);
            Assert.AreEqual(0f, FlankLampStrip.Level(lamps, 1, 0f, true, 0f));
        }
    }
}
