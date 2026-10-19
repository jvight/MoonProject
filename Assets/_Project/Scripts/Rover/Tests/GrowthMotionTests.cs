using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>The growth parts' motion (opening v3 2.6): slow, eased, soft, and it ends exactly at rest.</summary>
    public sealed class GrowthMotionTests
    {
        private const float Step = 1f / 60f;
        private const float Tolerance = 1e-4f;

        private GrowthSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new GrowthSettings();
        }

        [Test]
        public void TheAntennaSegment_StartsRetracted_AndGlidesOutOverAboutOneAndAHalfSeconds()
        {
            Assert.AreEqual(0f, GrowthMotion.AntennaOut(_settings, 0f), Tolerance, "retracted at the event");
            Assert.AreEqual(0.35f, _settings.AntennaExtendMetres, "Art's 0.35 m of travel");
            Assert.AreEqual(_settings.AntennaExtendMetres, GrowthMotion.AntennaOut(_settings, _settings.AntennaSeconds),
                Tolerance, "fully out when the slide ends");
            Assert.AreEqual(1.2f, _settings.AntennaSeconds, "about 1.2 s");
        }

        [Test]
        public void TheAntennaSlide_IsEasedInAndOut_NeverFasterInTheMiddleThanASmoothStepAllows()
        {
            float previous = 0f;
            float fastest = 0f;
            float previousStep = 0f;
            for (float t = Step; t <= _settings.AntennaSeconds; t += Step)
            {
                float now = GrowthMotion.AntennaOut(_settings, t);
                float step = now - previous;
                Assert.GreaterOrEqual(step, -Tolerance, "only outward during the slide");
                Assert.LessOrEqual(System.Math.Abs(step - previousStep), 0.01f, "no jolt frame to frame");
                fastest = System.Math.Max(fastest, step);
                previousStep = step;
                previous = now;
            }

            float perSecond = fastest / Step;
            Assert.LessOrEqual(perSecond, _settings.AntennaExtendMetres / _settings.AntennaSeconds * 1.51f,
                "its peak speed is a smooth step's 1.5 times the mean: eased at both ends");
            Assert.Less(GrowthMotion.AntennaOut(_settings, Step), _settings.AntennaExtendMetres * 0.01f, "it eases in");
        }

        [Test]
        public void TheAntennaSettle_GivesALittle_ThenRestsExactlyOut()
        {
            float lowest = float.MaxValue;
            float end = _settings.AntennaSeconds + _settings.AntennaSettleSeconds;
            for (float t = _settings.AntennaSeconds; t <= end; t += Step)
            {
                lowest = System.Math.Min(lowest, GrowthMotion.AntennaOut(_settings, t));
            }

            Assert.Less(lowest, _settings.AntennaExtendMetres, "a soft mechanical give");
            Assert.Greater(lowest, _settings.AntennaExtendMetres - _settings.AntennaSettleMetres - Tolerance,
                "never more than the tuned give");
            Assert.IsTrue(GrowthMotion.AntennaDone(_settings, end));
            Assert.AreEqual(_settings.AntennaExtendMetres, GrowthMotion.AntennaOut(_settings, end + 5f), Tolerance);
        }

        [Test]
        public void TheIgniter_SlidesInFromOutsideTheSocket_PressesPastHomeAtTheClick_AndRestsAtHome()
        {
            Assert.AreEqual(_settings.IgniterSeatMetres, GrowthMotion.IgniterOut(_settings, 0f), Tolerance);
            float previous = _settings.IgniterSeatMetres;
            for (float t = Step; t < _settings.IgniterSeatSeconds; t += Step)
            {
                float out01 = GrowthMotion.IgniterOut(_settings, t);
                Assert.LessOrEqual(out01, previous + Tolerance, "it only goes in");
                previous = out01;
            }

            float deepest = 0f;
            float clickEnd = _settings.IgniterSeatSeconds + _settings.IgniterClickSeconds;
            for (float t = _settings.IgniterSeatSeconds; t <= clickEnd; t += Step)
            {
                deepest = System.Math.Min(deepest, GrowthMotion.IgniterOut(_settings, t));
            }

            Assert.AreEqual(-_settings.IgniterClickMetres, deepest, 0.0006f, "the click presses a little past home");
            Assert.IsTrue(GrowthMotion.IgniterDone(_settings, clickEnd));
            Assert.AreEqual(0f, GrowthMotion.IgniterOut(_settings, clickEnd + 1f), Tolerance, "and rests exactly at home");
        }
    }
}
