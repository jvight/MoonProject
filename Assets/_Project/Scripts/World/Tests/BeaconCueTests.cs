using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The lander's answers to 07's self-test: a short burst of sharp flashes that is dark before its cue and after its
    /// last flash, and a single wave along the cable that runs from the first marker to the last once, then is gone.
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
        public void Flash_LightsItsCountOfSharpFlashes_ThenGoesDark()
        {
            float period = _beacon.FlashPeriod;
            Assert.AreEqual(0f, Flash(-0.01f), "nothing before the cue");
            int lit = 0;
            for (int flash = 0; flash < _beacon.FlashCount; flash++)
            {
                float middleOfFlash = (flash + _beacon.FlashOnShare * 0.5f) * period;
                float betweenFlashes = (flash + (1f + _beacon.FlashOnShare) * 0.5f) * period;
                Assert.AreEqual(1f, Flash(middleOfFlash), 1e-4f, $"flash {flash} is fully lit");
                Assert.AreEqual(0f, Flash(betweenFlashes), 1e-4f, $"dark after flash {flash}");
                lit++;
            }

            Assert.AreEqual(_beacon.FlashCount, lit);
            Assert.AreEqual(0f, Flash(_beacon.FlashCount * period + 0.01f), "dark after the last flash");
        }

        [Test]
        public void Flash_RisesThroughItsEdgeRatherThanPopping()
        {
            float halfEdge = _beacon.FlashEdge * _beacon.FlashPeriod * 0.5f;
            Assert.That(Flash(halfEdge), Is.GreaterThan(0f).And.LessThan(1f));
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
            return BeaconFlash.Level(elapsed, _beacon.FlashCount, _beacon.FlashPeriod, _beacon.FlashOnShare,
                _beacon.FlashEdge);
        }
    }
}
