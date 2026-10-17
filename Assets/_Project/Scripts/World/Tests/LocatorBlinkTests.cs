using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The landed lander's emergency beacon (opening v2 §4): a slow pulse that peaks once per period and never goes
    /// fully dark.
    /// </summary>
    public sealed class LocatorBlinkTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Blink_PeaksOncePerPeriod_AndRestsAtItsFloor()
        {
            var beacon = new LanderBeaconSettings();
            float period = beacon.Period;
            float floor = beacon.Floor;
            Assert.AreEqual(1f, LocatorBlink.Level(0f, period, beacon.Sharpness, floor), Tolerance, "a peak");
            Assert.AreEqual(1f, LocatorBlink.Level(period, period, beacon.Sharpness, floor), Tolerance,
                "once per period");
            Assert.AreEqual(floor, LocatorBlink.Level(period * 0.5f, period, beacon.Sharpness, floor), Tolerance,
                "between flashes it rests at its floor");
            Assert.Greater(floor, 0f, "never fully dark: the lander still reads from the wake");
        }
    }
}
