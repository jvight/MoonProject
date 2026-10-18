using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// How long until sunset (the Sun-arc readout): the sunset is when the Sun's lower limb meets the terrain horizon
    /// along its bearing, a higher rim brings it sooner, and the opening's pace turns the clock span into real
    /// seconds, infinite while a hold stops the Sun short of it.
    /// </summary>
    public sealed class SunsetForecastTests
    {
        private const float LimbTolerance = 0.05f;
        private const float RidgeHeight = 40f;
        private const float RidgeDistance = 100f;

        private SkyCycleSettings _cycle;
        private SunClock _clock;
        private OpeningSkyPacing _pacing;

        [SetUp]
        public void SetUp()
        {
            _cycle = new SkyCycleSettings();
            _clock = new SunClock(_cycle, new SkySettings());
            _pacing = new OpeningSkyPacing(_cycle, _clock);
        }

        [Test]
        public void Sunset_IsWhenTheLowerLimbMeetsAFlatHorizon()
        {
            var flat = new Ground((x, z) => 0f);
            float from = _pacing.GoldenHourTime;
            float sunset = _clock.SunsetTimeAfter(flat, Vector3.zero, from);

            Assert.Greater(sunset, from, "the Sun stands above the horizon at golden hour");
            Assert.Less(sunset, from + _cycle.DayLengthSeconds, "and sets within the day");
            float limb = _cycle.SunDiameter * 0.5f;
            Assert.AreEqual(limb, _clock.ElevationAt(sunset), LimbTolerance, "the lower limb sits on the horizon");
            Assert.Greater(_clock.ElevationAt(sunset - 1f), limb, "a second earlier it is still clear");
            Assert.AreEqual(sunset, _clock.SunsetTimeAfter(flat, Vector3.zero, sunset), 1e-3f,
                "once set, the sunset is now");
        }

        [Test]
        public void Sunset_ComesSoonerBehindAHigherRim()
        {
            var flat = new Ground((x, z) => 0f);
            var walled = new Ground((x, z) => new Vector2(x, z).magnitude >= RidgeDistance ? RidgeHeight : 0f);
            float from = _pacing.GoldenHourTime;

            Assert.Less(_clock.SunsetTimeAfter(walled, Vector3.zero, from),
                _clock.SunsetTimeAfter(flat, Vector3.zero, from), "the rim hides the Sun before the flat horizon");
        }

        [Test]
        public void RealSeconds_FollowTheFirstDaysPace()
        {
            _pacing.Reach(OpeningMilestone.NightReady, true, _clock);
            Assert.AreEqual(_cycle.FirstSunsetSeconds,
                _pacing.RealSecondsBetween(_pacing.LastLightTime, _pacing.NightfallTime), 1e-2f,
                "the first sunset runs in its tuned length");
            float later = _pacing.NightfallTime + 100f;
            Assert.AreEqual(100f, _pacing.RealSecondsBetween(_pacing.NightfallTime, later), 1e-3f,
                "after nightfall a clock second is a real second");
            Assert.AreEqual(0f, _pacing.RealSecondsBetween(later, _pacing.NightfallTime), "nothing behind");
        }

        [Test]
        public void RealSeconds_AreInfiniteWhileAHoldStopsTheSunShortOfIt()
        {
            _pacing.Reach(OpeningMilestone.FirstCharge, false, _clock);
            Assert.AreEqual(float.PositiveInfinity,
                _pacing.RealSecondsBetween(_pacing.GoldenHourTime, _pacing.NightfallTime), "held at golden hour");

            _pacing.Reach(OpeningMilestone.PowerOn, false, _clock);
            Assert.AreEqual(float.PositiveInfinity,
                _pacing.RealSecondsBetween(_pacing.GoldenHourTime, _pacing.NightfallTime), "held at last light");
            Assert.AreEqual(_cycle.LastLightSinkSeconds,
                _pacing.RealSecondsBetween(_pacing.GoldenHourTime, _pacing.LastLightTime), 1e-2f,
                "the sink to last light still runs in its tuned length");
        }

        /// <summary>Ground of a given height everywhere, all of it drivable.</summary>
        private sealed class Ground : ITerrainQuery
        {
            private readonly Func<float, float, float> _height;

            public Ground(Func<float, float, float> height)
            {
                _height = height;
            }

            public Rect PlayableArea => new Rect(-1000f, -1000f, 2000f, 2000f);

            public bool IsDrivable(float x, float z)
            {
                return true;
            }

            public float SampleHeight(float x, float z)
            {
                return _height(x, z);
            }

            public Vector3 SampleNormal(float x, float z)
            {
                return Vector3.up;
            }
        }
    }
}
