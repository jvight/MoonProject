using System.Collections.Generic;
using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    /// <summary>The charge run's notes (M3-26 section 2): one per lamp, easing in, landing on the ignition.</summary>
    public sealed class ChargeRunTests
    {
        private const float Frame = 1f / 60f;

        private static List<float> Play(ChargeRun run, float seconds, List<int> lamps)
        {
            var times = new List<float>();
            for (float t = 0f; t <= seconds + Frame; t += Frame)
            {
                while (run.TryNext(out int lamp))
                {
                    lamps.Add(lamp);
                    times.Add(t);
                }

                run.Advance(Frame);
            }

            return times;
        }

        [Test]
        public void AFreshRun_SoundsEveryLamp_InOrder_EasingIn_AndLandsOnTheIgnition()
        {
            var run = new ChargeRun();
            run.Begin(5, 0.65f, 0f);
            var lamps = new List<int>();
            List<float> times = Play(run, 0.65f, lamps);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, lamps);
            Assert.AreEqual(0f, times[0], 1e-4f, "the first lamp at once");
            Assert.AreEqual(0.65f, times[4], Frame + 1e-4f, "the last on the ignition");
            Assert.Greater(times[1] - times[0], times[4] - times[3], "accelerating");
            Assert.IsFalse(run.IsRunning);
        }

        [Test]
        public void AResume_StartsFromTheMiddleLamp()
        {
            var run = new ChargeRun();
            run.Begin(9, 0.3f, 0.5f);
            var lamps = new List<int>();
            Play(run, 0.3f, lamps);
            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8 }, lamps, "the line is still warm");
        }

        [Test]
        public void ACancel_StopsTheRun()
        {
            var run = new ChargeRun();
            run.Begin(7, 0.45f, 0f);
            run.Advance(0.1f);
            Assert.IsTrue(run.TryNext(out _));
            run.Stop();
            run.Advance(1f);
            Assert.IsFalse(run.TryNext(out _));
            Assert.IsFalse(run.IsRunning);
        }

        [Test]
        public void ALongFrame_PassesSeveralLamps_NoneLost()
        {
            var run = new ChargeRun();
            run.Begin(5, 0.65f, 0f);
            run.Advance(1f);
            int count = 0;
            while (run.TryNext(out _))
            {
                count++;
            }

            Assert.AreEqual(5, count);
        }
    }
}
