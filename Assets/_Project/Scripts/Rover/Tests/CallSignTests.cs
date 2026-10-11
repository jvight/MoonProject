using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The antenna's call sign (cutscenes §1.5): three short, one long, about every 12 s at rest.</summary>
    public sealed class CallSignTests
    {
        private const float Frame = 1f / 120f;
        private const float Lit = 0.5f;

        private RoverCharacterTuning _tuning;
        private CallSign _sign;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            _sign = new CallSign(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Restarted_BlinksThreeShortThenOneLong()
        {
            _sign.Restart();
            int pulses = 0;
            float on = 0f;
            float longest = 0f;
            float shortest = float.MaxValue;
            bool wasLit = false;
            for (float t = 0f; t < _sign.PatternSeconds + 0.5f; t += Frame)
            {
                _sign.Step(true, Frame);
                bool lit = _sign.Glow > Lit;
                if (lit)
                {
                    on += Frame;
                }
                else if (wasLit)
                {
                    pulses++;
                    longest = Mathf.Max(longest, on);
                    shortest = Mathf.Min(shortest, on);
                    on = 0f;
                }

                wasLit = lit;
            }

            Assert.AreEqual(4, pulses, "three short and one long");
            Assert.Greater(longest, 2f * shortest, "the last pulse is clearly long");
        }

        [Test]
        public void AtRest_RepeatsEveryPeriod_AndRestsWhileDriving()
        {
            float firstLit = -1f;
            float secondLit = -1f;
            bool wasLit = false;
            float t = 0f;
            for (; t < 2.5f * _tuning.CallSignPeriod; t += Frame)
            {
                _sign.Step(true, Frame);
                bool lit = _sign.Glow > Lit;
                if (lit && !wasLit)
                {
                    if (firstLit < 0f)
                    {
                        firstLit = t;
                    }
                    else if (secondLit < 0f && t - firstLit > _sign.PatternSeconds)
                    {
                        secondLit = t;
                    }
                }

                wasLit = lit;
            }

            Assert.AreEqual(_tuning.CallSignPeriod, secondLit - firstLit, 0.05f);

            var driving = new CallSign(_tuning);
            for (float s = 0f; s < 2f * _tuning.CallSignPeriod; s += Frame)
            {
                driving.Step(false, Frame);
                Assert.AreEqual(0f, driving.Glow, "on the move the tip rests");
            }
        }

        [Test]
        public void ShortPulsesRepeatSlowerThanThreePerSecond()
        {
            Assert.GreaterOrEqual(_tuning.CallSignShort + _tuning.CallSignGap, 1f / 3f);
        }
    }
}
