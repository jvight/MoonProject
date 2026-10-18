using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The lander's answers to 07's self-test: one sharp flash per spark of the cutter, dark before its cue and after,
    /// and a single wave along the cable that runs from the first marker to the last once, then is gone.
    /// </summary>
    public sealed class BeaconCueTests
    {
        private const int Markers = 10;

        private LanderBeaconSettings _beacon;

        [SetUp]
        public void SetUp()
        {
            _beacon = new LanderBeaconSettings();
        }

        [Test]
        public void Flash_IsOneSharpFlash_DarkBeforeItsCueAndAfter()
        {
            Assert.AreEqual(0f, Flash(-0.01f), "nothing before the spark");
            Assert.AreEqual(1f, Flash(_beacon.FlashSeconds * 0.5f), 1e-4f, "fully lit in the middle");
            Assert.AreEqual(0f, Flash(_beacon.FlashSeconds), "dark once it has been lit its length");
            Assert.AreEqual(0f, Flash(_beacon.FlashSeconds + 1f), "and stays dark");
        }

        [Test]
        public void Flash_RisesThroughItsEdgeRatherThanPopping()
        {
            Assert.That(Flash(_beacon.FlashEdgeSeconds * 0.5f), Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void BeginWave_RunsTheCableOnceFromThePostToTheLander()
        {
            float run = _beacon.MarkerRunSeconds;
            float width = _beacon.MarkerWaveWidth;
            Assert.AreEqual(0f, CableMarkerWave.Once(-0.1f, 0, Markers, run, width), "nothing before 07 begins");
            Assert.AreEqual(1f, CableMarkerWave.Once(0f, 0, Markers, run, width), 1e-4f, "it starts at 07's post");
            Assert.AreEqual(0f, CableMarkerWave.Once(0f, Markers - 1, Markers, run, width), "not yet at the lander");
            Assert.AreEqual(1f, CableMarkerWave.Once(run, Markers - 1, Markers, run, width), 1e-4f,
                "it reaches the lander after its run");
            float after = run * (1f + width) + 0.01f;
            for (int i = 0; i < Markers; i++)
            {
                Assert.AreEqual(0f, CableMarkerWave.Once(after, i, Markers, run, width), $"marker {i}: it runs once");
            }
        }

        private float Flash(float elapsed)
        {
            return BeaconFlash.Level(elapsed, _beacon.FlashSeconds, _beacon.FlashEdgeSeconds);
        }
    }
}
