using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The supply pod's locator beacon (0.5.5): a slow pulse that peaks once per period and never goes fully dark.
    /// </summary>
    public sealed class LocatorBlinkTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Blink_PeaksOncePerPeriod_AndRestsAtItsFloor()
        {
            var canyon = new CanyonSettings();
            float period = canyon.BeaconPeriod;
            float floor = canyon.BeaconFloor;
            Assert.AreEqual(1f, LocatorBlink.Level(0f, period, canyon.BeaconSharpness, floor), Tolerance, "a peak");
            Assert.AreEqual(1f, LocatorBlink.Level(period, period, canyon.BeaconSharpness, floor), Tolerance,
                "once per period");
            Assert.AreEqual(floor, LocatorBlink.Level(period * 0.5f, period, canyon.BeaconSharpness, floor), Tolerance,
                "between flashes it rests at its floor");
            Assert.Greater(floor, 0f, "never fully dark: the ledge still reads from home");
        }
    }
}
