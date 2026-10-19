using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The battery on 07's side (docs/features/M3-20): the crawl caps the drive softly and the flank lamps show the
    /// cells.
    /// </summary>
    public sealed class RoverBatteryTests
    {
        private const float Step = 0.02f;

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
        public void TheOpeningReserveCap_LowersTopSpeedOnly_AccelerationOffTheLineIsUntouched()
        {
            var drive = new DriveSettings();
            const float cap = 0.7f;

            Assert.AreEqual(LongitudinalDrive.Acceleration(drive, 0f, 1f, 0f, Step),
                LongitudinalDrive.Acceleration(drive, 0f, 1f, 0f, cap, Step), 1e-5f,
                "off the line 07 pulls as hard as ever");
            Assert.AreEqual(0f, LongitudinalDrive.Acceleration(drive, cap * drive.TopSpeed, 1f, 0f, cap, Step), 1e-4f,
                "the speed settles on the capped top");
            Assert.Greater(LongitudinalDrive.Acceleration(drive, 0.69f * drive.TopSpeed, 1f, 0f, cap, Step), -1e-4f,
                "no earlier braking before the capped top");
            Assert.AreEqual(LongitudinalDrive.Acceleration(drive, 0.5f * drive.ReverseTopSpeed, -1f, 0f, Step),
                LongitudinalDrive.Acceleration(drive, 0.5f * drive.ReverseTopSpeed, -1f, 0f, cap, Step), 1e-4f,
                "the cap is on the forward top only, not on reversing's pull");
        }

        [Test]
        public void TheOpeningReserve_DimsTheLamps_ByTheTunedShares()
        {
            var lamps = new FlankLampSettings();

            Assert.Less(lamps.OpeningReserveGlow, 1f);
            Assert.Less(lamps.OpeningReserveHeadlamp, 1f);
            Assert.Greater(lamps.OpeningReserveHeadlamp, lamps.CrawlHeadlamp, "dim is not the crawl's near-dark");
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
