using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    /// <summary>The charge run's notes (M3-26 section 2) on Rover's lamp clock: one per lamp, landing on the
    /// ignition.</summary>
    public sealed class ChargeRunTests
    {
        private const float Frame = 1f / 60f;

        private static List<float> Play(ChargeRun run, float seconds, List<int> lamps)
        {
            var times = new List<float>();
            for (float t = 0f; t <= seconds + 2f * Frame; t += Frame)
            {
                run.Advance(Frame);
                while (run.TryNext(out int lamp))
                {
                    lamps.Add(lamp);
                    times.Add(t + Frame);
                }
            }

            return times;
        }

        [Test]
        public void Onsets_FollowRoversRule()
        {
            const float t = 0.65f;
            for (int i = 0; i < 5; i++)
            {
                float expected = t * Mathf.Sqrt((i + 1) / 5f);
                Assert.AreEqual(expected, ChargeRun.Onset(i, 5, t, 0f), 1e-5f, $"lamp {i}, fresh");
            }

            Assert.AreEqual(0.3f * Mathf.Sqrt((5f / 9f - 0.5f) / 0.5f), ChargeRun.Onset(4, 9, 0.3f, 0.5f), 1e-5f);
            Assert.AreEqual(0.3f, ChargeRun.Onset(8, 9, 0.3f, 0.5f), 1e-5f, "the last lamp lands on the ignition");
        }

        [Test]
        public void AFreshRun_SoundsEveryLamp_InOrder_Accelerating_AndLandsOnTheIgnition()
        {
            var run = new ChargeRun();
            run.Begin(5, 0.65f, 0f);
            var lamps = new List<int>();
            List<float> times = Play(run, 0.65f, lamps);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, lamps);
            Assert.AreEqual(0.65f, times[4], Frame + 1e-4f, "the last on the ignition");
            Assert.Greater(times[1] - times[0], times[4] - times[3], "accelerating");
            Assert.IsFalse(run.IsRunning);
        }

        [Test]
        public void AResume_SkipsTheLampsStillLit()
        {
            Assert.AreEqual(4, ChargeRun.FirstUnlit(9, 0.5f), "4/9 < 0.5 < 5/9");
            Assert.AreEqual(2, ChargeRun.FirstUnlit(5, 0.5f), "2/5 < 0.5 < 3/5");
            Assert.AreEqual(0, ChargeRun.FirstUnlit(5, 0f));
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
            run.Advance(0.3f);
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
