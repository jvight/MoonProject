using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The first day follows the opening (opening v2 §9.2, opening v3 R5–R6): the eclipse holds until the first charge
    /// and ends within 2 s of it; the Sun never sets before the night is ready; it holds at golden hour through
    /// power-on until the first dusk begins, and at last light until the night is ready; the first sunset starts the
    /// moment the night is ready, wherever the sink has brought the Sun, and reaches nightfall in its tuned length; a
    /// restored milestone or dusk snaps the clock, in whatever order a load states them.
    /// </summary>
    public sealed class OpeningSkyPacingTests
    {
        private const float Frame = 1f / 30f;
        private const float EclipseEndsWithin = 2f;
        private const float Hour = 3600f;

        private SkyCycleSettings _cycle;
        private SkySettings _sky;
        private SunClock _clock;
        private OpeningSkyPacing _pacing;

        [SetUp]
        public void SetUp()
        {
            _cycle = new SkyCycleSettings();
            _sky = new SkySettings();
            _clock = new SunClock(_cycle, _sky);
            _pacing = new OpeningSkyPacing(_cycle, _clock);
        }

        [Test]
        public void KeyTimes_FollowTheFirstDaysDescent()
        {
            Assert.Less(_pacing.GoldenHourTime, _pacing.LastLightTime);
            Assert.Less(_pacing.LastLightTime, _pacing.NightfallTime);
            Assert.Greater(_clock.ElevationAt(_pacing.GoldenHourTime), _cycle.LastLightElevation, "golden hour is high");
            Assert.LessOrEqual(_clock.ElevationAt(_pacing.LastLightTime), _cycle.LastLightElevation);
            Assert.Greater(_clock.ElevationAt(_pacing.LastLightTime), 0f, "last light is still above the horizon");
            Assert.LessOrEqual(_clock.ElevationAt(_pacing.NightfallTime), _cycle.NightfallElevation);
        }

        [Test]
        public void Eclipse_HoldsUntilTheFirstCharge_ThenEndsWithinTwoSeconds()
        {
            Run(Hour);
            Assert.IsTrue(_clock.IsHoldingEclipse, "no milestone, no sunrise, however long the player takes");

            _pacing.Reach(OpeningMilestone.FirstCharge, false, _clock);
            Run(EclipseEndsWithin);
            Assert.Less(_clock.EclipseFactor, 1f, "the Sun's limb breaks out within 2 s of the first charge");
            Assert.Greater(_clock.GlobalSunlight, 0f);
        }

        [Test]
        public void Sun_HoldsAtGoldenHour_ThroughPowerOn_ThenDuskSinksToLastLight_AndNeverSetsBeforeTheNightIsReady()
        {
            _pacing.Reach(OpeningMilestone.FirstCharge, false, _clock);
            Run(Hour);
            Assert.AreEqual(_pacing.GoldenHourTime, _clock.TimeSeconds, Frame, "holds at golden hour until power-on");

            _pacing.Reach(OpeningMilestone.PowerOn, false, _clock);
            Run(Hour);
            Assert.AreEqual(_pacing.GoldenHourTime, _clock.TimeSeconds, Frame,
                "after power-on the afternoon holds: no dusk by the clock (R5, the first free choice)");

            _pacing.BeginDusk(false, _clock);
            Run(_cycle.LastLightSinkSeconds + 1f);
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame,
                "sinks to last light in its tuned length once the first dusk begins");
            Run(Hour);
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame, "and holds there");
            Assert.Greater(_clock.Elevation, 0f, "the Sun never sets before the night is ready");
        }

        [Test]
        public void FirstSunset_RunsInItsTunedLength_ThenTheNormalPaceTakesOver()
        {
            _pacing.Reach(OpeningMilestone.PowerOn, true, _clock);
            _pacing.Reach(OpeningMilestone.NightReady, false, _clock);
            Run(_cycle.FirstSunsetSeconds * 0.5f);
            Assert.Greater(_clock.Elevation, 0f, "half way through the first sunset the Sun is still up");
            Run(_cycle.FirstSunsetSeconds * 0.5f + 1f);
            Assert.Less(_clock.Elevation, 0f, "the first sunset is over in its tuned length");

            float before = _clock.TimeSeconds;
            Run(10f);
            Assert.AreEqual(before + 10f, _clock.TimeSeconds, 2f * Frame,
                "after nightfall the day runs at its normal pace");
        }

        [TestCase(0f)]
        [TestCase(0.1f)]
        [TestCase(0.5f)]
        [TestCase(0.9f)]
        [TestCase(1f)]
        public void FirstSunset_StartsWhenTheNightIsReady_AndEndsInItsTunedLength(float sinkProgress)
        {
            _pacing.Reach(OpeningMilestone.FirstCharge, true, _clock);
            _pacing.Reach(OpeningMilestone.PowerOn, false, _clock);
            _pacing.BeginDusk(false, _clock);
            Run(_cycle.LastLightSinkSeconds * sinkProgress + (sinkProgress >= 1f ? 1f : 0f));
            float readyAt = _clock.TimeSeconds;

            _pacing.Reach(OpeningMilestone.NightReady, false, _clock);
            float seconds = 0f;
            while (_clock.TimeSeconds < _pacing.NightfallTime)
            {
                _clock.Tick(_pacing.ClockDelta(_clock.TimeSeconds, Frame), out _);
                seconds += Frame;
                Assert.Less(seconds, _cycle.FirstSunsetSeconds + 1f, "the first sunset never drags on");
            }

            Assert.AreEqual(_cycle.FirstSunsetSeconds, seconds, Frame,
                $"night ready at clock {readyAt:F0} s ({sinkProgress:P0} of the sink): nightfall in its tuned length");
        }

        [Test]
        public void RestoredMilestones_SnapTheSky_WithNoTransition()
        {
            _pacing.Reach(OpeningMilestone.FirstCharge, true, _clock);
            Assert.IsFalse(_clock.IsHoldingEclipse);
            Assert.AreEqual(_pacing.GoldenHourTime, _clock.TimeSeconds, Frame);

            _pacing.Reach(OpeningMilestone.PowerOn, true, _clock);
            Assert.AreEqual(_pacing.GoldenHourTime, _clock.TimeSeconds, Frame, "powered, no dusk yet: the afternoon");

            _pacing.BeginDusk(true, _clock);
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame, "a begun dusk: last light");

            _pacing.Reach(OpeningMilestone.NightReady, true, _clock);
            Assert.AreEqual(_pacing.NightfallTime, _clock.TimeSeconds, Frame);

            _clock.SetTime(_pacing.NightfallTime + Hour);
            _pacing.Reach(OpeningMilestone.NightReady, true, _clock);
            Assert.AreEqual(_pacing.NightfallTime + Hour, _clock.TimeSeconds, Frame,
                "a save from later in the game keeps its own time");
        }

        [Test]
        public void ARestoredDusk_StatedBeforeItsMilestones_StillSettlesAtLastLight()
        {
            _pacing.BeginDusk(true, _clock);
            Assert.IsFalse(_clock.IsHoldingEclipse, "a dusk implies the first charge");
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame);
            _pacing.Reach(OpeningMilestone.FirstCharge, true, _clock);
            _pacing.Reach(OpeningMilestone.PowerOn, true, _clock);
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame, "the milestones never pull it back");
            Run(Hour);
            Assert.AreEqual(_pacing.LastLightTime, _clock.TimeSeconds, Frame, "and it holds there until night is ready");
        }

        [Test]
        public void TheNightReadyBeforeTheDusk_StartsTheSunsetFromTheAfternoon()
        {
            _pacing.Reach(OpeningMilestone.PowerOn, true, _clock);
            _pacing.Reach(OpeningMilestone.NightReady, false, _clock);
            Run(_cycle.FirstSunsetSeconds + 1f);
            Assert.Less(_clock.Elevation, 0f, "the first sunset runs from golden hour in its tuned length");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _clock.Tick(_pacing.ClockDelta(_clock.TimeSeconds, Frame), out _);
            }
        }
    }
}
