using System;
using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    public sealed class LandmarkPilotLevelTests
    {
        private const float Lit = 0.5f;
        private const float Ease = 2f;
        private const float Frame = 0.1f;

        [Test]
        public void StaysDarkUntilTheHeartIsFound()
        {
            var pilot = new LandmarkPilotLevel(Lit, Ease);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(0f, pilot.Step(false, Frame));
            }

            Assert.IsFalse(pilot.Found);
        }

        [Test]
        public void RisesGentlyOnceFound_AndSettlesOnTheLitGlow()
        {
            var pilot = new LandmarkPilotLevel(Lit, Ease);

            float previous = 0f;
            float first = pilot.Step(true, Frame);
            Assert.Greater(first, 0f);
            Assert.Less(first, Lit * 0.1f, "it eases in, it does not pop");
            for (int i = 0; i < 400; i++)
            {
                float level = pilot.Step(true, Frame);
                Assert.GreaterOrEqual(level, previous);
                previous = level;
            }

            Assert.AreEqual(Lit, previous, 1e-3f);
        }

        [Test]
        public void StaysLitAfterwards_EvenIfTheSiteIsPickedClean()
        {
            var pilot = new LandmarkPilotLevel(Lit, Ease);
            pilot.Snap(true);

            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(Lit, pilot.Step(false, Frame), 1e-6f);
            }

            Assert.IsTrue(pilot.Found);
        }

        [Test]
        public void SnapShowsTheSettledStateAtOnce()
        {
            var known = new LandmarkPilotLevel(Lit, Ease);
            var unknown = new LandmarkPilotLevel(Lit, Ease);

            Assert.AreEqual(Lit, known.Snap(true));
            Assert.AreEqual(0f, unknown.Snap(false));
        }

        [Test]
        public void ANegativeGlowIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LandmarkPilotLevel(-0.1f, Ease));
        }
    }
}
