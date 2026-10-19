using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>One base lamp's glow: its wait, its warm flicker-catch, its ease up and its ease off.</summary>
    public sealed class LampEaseTests
    {
        private const float Rise = 1.5f;
        private const float Fall = 2.5f;
        private const float Depth = 0.4f;
        private const float Hertz = 6f;
        private const float Flicker = 0.7f;
        private const float Frame = 1f / 60f;

        private static bool Step(LampEase lamp)
        {
            return lamp.Step(Frame, Rise, Fall, Depth, Hertz, Flicker);
        }

        [Test]
        public void ALampWaitsItsTurn_ThenBeginsOnce()
        {
            var lamp = new LampEase();
            lamp.Light(0.5f);
            int begun = 0;
            float level = 0f;
            for (float t = 0f; t < 0.45f; t += Frame)
            {
                begun += Step(lamp) ? 1 : 0;
                level = lamp.Level;
            }

            Assert.AreEqual(0, begun, "it has not begun while it waits");
            Assert.AreEqual(0f, level, "and is dark");
            for (float t = 0f; t < 0.2f; t += Frame)
            {
                begun += Step(lamp) ? 1 : 0;
            }

            Assert.AreEqual(1, begun, "it begins once, at its turn");
            Assert.AreEqual(LampEase.Phase.Rising, lamp.State);
        }

        [Test]
        public void ALampEasesUpToFull_ItsFlickerDipsItButNeverPastFull()
        {
            var lamp = new LampEase();
            lamp.Light(0f);
            float peak = 0f;
            float dips = 0f;
            float last = 0f;
            for (float t = 0f; t < Rise + 0.2f; t += Frame)
            {
                Step(lamp);
                peak = System.Math.Max(peak, lamp.Level);
                if (lamp.Level < last - 1e-4f)
                {
                    dips++;
                }

                last = lamp.Level;
            }

            Assert.AreEqual(1f, lamp.Level, 1e-5f, "full at the end");
            Assert.AreEqual(LampEase.Phase.On, lamp.State);
            Assert.LessOrEqual(peak, 1f, "never brighter than full");
            Assert.Greater(dips, 0f, "the warm flicker dips the glow while the lamp catches");
        }

        [Test]
        public void AfterTheCatch_TheGlowOnlyRises()
        {
            var lamp = new LampEase();
            lamp.Light(0f);
            for (float t = 0f; t < Flicker; t += Frame)
            {
                Step(lamp);
            }

            float last = lamp.Level;
            for (float t = 0f; t < Rise - Flicker - Frame; t += Frame)
            {
                Step(lamp);
                Assert.GreaterOrEqual(lamp.Level, last - 1e-5f, "no flicker once the lamp has caught");
                last = lamp.Level;
            }
        }

        [Test]
        public void ALitLampEasesOff_AndStaysOff()
        {
            var lamp = new LampEase();
            lamp.Snap(true);
            lamp.Extinguish();
            float last = 1f;
            for (float t = 0f; t < Fall + 0.2f; t += Frame)
            {
                Step(lamp);
                Assert.LessOrEqual(lamp.Level, last + 1e-5f, "it only dims");
                last = lamp.Level;
            }

            Assert.AreEqual(0f, lamp.Level);
            Assert.AreEqual(LampEase.Phase.Off, lamp.State);
        }

        [Test]
        public void ARisingLampAskedToLightAgain_KeepsItsPlace()
        {
            var lamp = new LampEase();
            lamp.Light(0f);
            for (int i = 0; i < 30; i++)
            {
                Step(lamp);
            }

            float level = lamp.Level;
            lamp.Light(5f);
            Assert.AreEqual(LampEase.Phase.Rising, lamp.State, "a lit lamp is left as it is");
            Assert.AreEqual(level, lamp.Level);
        }

        [Test]
        public void SnapStartsALampAtItsSteadyState_WithNoBeat()
        {
            var lamp = new LampEase();
            lamp.Snap(true);
            Assert.AreEqual(1f, lamp.Level);
            Assert.IsFalse(Step(lamp), "a lamp that starts lit never begins");
            lamp.Snap(false);
            Assert.AreEqual(0f, lamp.Level);
        }
    }
}
