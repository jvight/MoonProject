using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The lamp power-up at 07's first blink: dark while asleep, then a few uneven flickers that never reach full, a
    /// dark gap, and a smooth ramp to full.
    /// </summary>
    public sealed class LampPowerUpTests
    {
        private const float Frame = 1f / 120f;

        private OpeningSettings _settings;
        private LampPowerUp _lamps;

        [SetUp]
        public void SetUp()
        {
            _settings = new OpeningSettings();
            _lamps = new LampPowerUp(_settings);
        }

        [Test]
        public void Untouched_TheLampsAreSimplyOn()
        {
            _lamps.Step(Frame);
            Assert.IsFalse(_lamps.IsControlling);
            Assert.AreEqual(1f, _lamps.Level);
        }

        [Test]
        public void Asleep_TheLampsAreDark_UntilTheBlink()
        {
            _lamps.HoldDark();
            for (int i = 0; i < 240; i++)
            {
                _lamps.Step(Frame);
            }

            Assert.IsTrue(_lamps.IsControlling);
            Assert.AreEqual(0f, _lamps.Level);
        }

        [Test]
        public void TheSequence_FlickersUnevenly_FallsDark_ThenRampsSmoothlyToFull()
        {
            _lamps.HoldDark();
            _lamps.Start();
            float flickerEnd = _settings.LampFlickerSeconds;
            float gapEnd = flickerEnd + _settings.LampDarkGapSeconds;
            var peakTimes = new System.Collections.Generic.List<float>();
            float peak = 0f;
            float gapMax = 0f;
            float previousRamp = -1f;
            float previous = 0f;
            bool rising = false;
            float time = 0f;
            while (_lamps.IsControlling && time < _lamps.TotalSeconds + 1f)
            {
                float level = _lamps.Level;
                if (time < flickerEnd - Frame)
                {
                    peak = Mathf.Max(peak, level);
                    if (level > previous)
                    {
                        rising = true;
                    }
                    else if (level < previous && rising)
                    {
                        rising = false;
                        peakTimes.Add(time);
                    }
                }
                else if (time < gapEnd)
                {
                    gapMax = Mathf.Max(gapMax, level);
                }
                else
                {
                    Assert.GreaterOrEqual(level, previousRamp - 1e-4f, "The ramp only rises.");
                    previousRamp = level;
                }

                previous = level;
                _lamps.Step(Frame);
                time += Frame;
            }

            Assert.AreEqual(_settings.LampFlickerCount, peakTimes.Count, "The set number of flickers.");
            Assert.Less(peak, 0.85f, "No flicker reaches full power.");
            Assert.Greater(peak, 0.3f, "...yet each is plainly seen.");
            Assert.AreEqual(0f, gapMax, 1e-3f, "Dark between the last flicker and the ramp.");
            Assert.AreEqual(_lamps.TotalSeconds, time, 3f * Frame, "The whole sequence takes its set time.");
            Assert.IsFalse(_lamps.IsControlling);
            Assert.AreEqual(1f, _lamps.Level, "Full power at the end.");

            // Unevenness is a share of the flicker span, so it holds however short the lamps' line is tuned.
            bool uneven = false;
            float firstGap = _lamps.FlickerCenter(1) - _lamps.FlickerCenter(0);
            for (int i = 2; i < _lamps.FlickerCount; i++)
            {
                float gap = _lamps.FlickerCenter(i) - _lamps.FlickerCenter(i - 1);
                uneven |= Mathf.Abs(gap - firstGap) > 0.02f * _settings.LampFlickerSeconds;
            }

            Assert.IsTrue(uneven, "The flickers are spaced unevenly.");
        }

        [Test]
        public void ReleasingDropsTheSequence()
        {
            _lamps.HoldDark();
            _lamps.Release();
            Assert.IsFalse(_lamps.IsControlling);
            Assert.AreEqual(1f, _lamps.Level);
        }
    }
}
