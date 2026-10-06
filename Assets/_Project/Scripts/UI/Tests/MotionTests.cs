using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The easing building blocks: nothing pops, reversals are continuous, numbers count, time eases.
    /// </summary>
    public sealed class MotionTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void Fade_ReachesShownAfterItsFadeIn_AndHiddenAfterItsFadeOut()
        {
            var settings = new RevealSettings(0.5f, 0.25f, 0f, 1f, 1f, 1f);
            var fade = new Fade(settings);
            fade.Set(true);
            Step(fade, 0.49f);
            Assert.IsFalse(fade.IsShown, "not before the fade-in time");
            Step(fade, 0.02f);
            Assert.IsTrue(fade.IsShown);
            Assert.AreEqual(1f, fade.Value, 1e-5f);

            fade.Set(false);
            Step(fade, 0.26f);
            Assert.IsTrue(fade.IsHidden);
            Assert.AreEqual(0f, fade.Value, 1e-5f);
        }

        [Test]
        public void Fade_TurningAroundMidway_ContinuesFromWhereItWas()
        {
            var fade = new Fade(new RevealSettings(1f, 1f, 0f, 1f, 1f, 1f));
            fade.Set(true);
            Step(fade, 0.4f);
            float before = fade.Value;
            fade.Set(false);
            fade.Step(Frame);
            Assert.AreEqual(before, fade.Value, 0.03f, "no jump when reversing");
            Assert.Less(fade.Value, before, "and it heads back down");
        }

        [Test]
        public void Fade_EasesInAndOut_SlowAtBothEnds()
        {
            var fade = new Fade(new RevealSettings(1f, 1f, 0f, 1f, 1f, 1f));
            fade.Set(true);
            fade.Step(0.1f);
            float start = fade.Value;
            Step(fade, 0.4f);
            float middle = fade.Value;
            fade.Step(0.1f);
            Assert.Less(start, 0.1f * 0.5f, "starts gently (well under linear)");
            Assert.Greater(fade.Value - middle, start, "moves fastest in the middle");
        }

        [Test]
        public void SoftSpring_OvershootsSoftly_ThenSettlesExactly()
        {
            var spring = new SoftSpring(0.9f);
            float peak = 0f;
            for (int i = 0; i < 600; i++)
            {
                spring.Step(1f, Frame, 1.6f, 0.6f);
                peak = Mathf.Max(peak, spring.Value);
            }

            Assert.Greater(peak, 1f, "a soft overshoot");
            Assert.Less(peak, 1.03f, "but only a soft one");
            Assert.AreEqual(1f, spring.Value, "it comes to rest on the target");
            Assert.IsTrue(spring.IsAtRest(1f));
        }

        [Test]
        public void SoftSpring_SurvivesALongFrame()
        {
            var spring = new SoftSpring(0f);
            spring.Step(1f, 2f, 6f, 0.3f);
            Assert.IsFalse(float.IsNaN(spring.Value));
            Assert.Less(Mathf.Abs(spring.Value - 1f), 0.5f, "sub-stepping keeps a hitch from exploding the spring");
        }

        [Test]
        public void CountUp_CountsTowardsTheTarget_NeverPastIt_AndReportsOnlyChanges()
        {
            var settings = new ScrapChipSettings();
            var count = new CountUp(settings);
            count.Snap(10);
            count.SetTarget(30);
            int last = count.Shown;
            int changes = 0;
            for (int i = 0; i < 200; i++)
            {
                if (count.Step(Frame))
                {
                    changes++;
                    Assert.Greater(count.Shown, last, "counts up, one way");
                    Assert.LessOrEqual(count.Shown, 30);
                    last = count.Shown;
                }
            }

            Assert.AreEqual(30, count.Shown);
            Assert.IsTrue(count.IsSettled);
            Assert.LessOrEqual(changes, 20, "a changed flag only when the shown number moves");
            Assert.IsFalse(count.Step(Frame), "settled: nothing to update");
        }

        [Test]
        public void CountUp_TakesLongerForBiggerChanges_WithinItsBounds()
        {
            var settings = new ScrapChipSettings();
            Assert.Greater(SecondsToCount(settings, 12), SecondsToCount(settings, 3), "a bigger gift counts longer");
            float huge = SecondsToCount(settings, 500);
            Assert.LessOrEqual(huge, settings.MaxCountSeconds + 2 * Frame, "but never forever");
            Assert.Greater(huge, settings.MaxCountSeconds * 0.8f);
        }

        [Test]
        public void CountUp_ANewTargetMidCount_ContinuesFromTheShownNumber()
        {
            var count = new CountUp(new ScrapChipSettings());
            count.Snap(0);
            count.SetTarget(20);
            for (int i = 0; i < 10; i++)
            {
                count.Step(Frame);
            }

            int shown = count.Shown;
            count.SetTarget(25);
            count.Step(Frame);
            Assert.GreaterOrEqual(count.Shown, shown, "never jumps back");
        }

        [Test]
        public void PauseClock_EasesTimeToAStop_AndBackToFullSpeed()
        {
            var settings = new PauseSettings();
            var clock = new PauseClock(settings);
            Assert.AreEqual(1f, clock.Scale);
            clock.Pause();
            clock.Step(settings.FreezeSeconds * 0.5f);
            Assert.That(clock.Scale, Is.InRange(0.01f, 0.99f), "half-way it is slowing, not frozen");
            clock.Step(settings.FreezeSeconds);
            Assert.AreEqual(0f, clock.Scale, 1e-6f);
            Assert.IsTrue(clock.IsSettled);

            clock.Resume();
            Assert.IsFalse(clock.IsSettled);
            clock.Step(settings.ResumeSeconds + Frame);
            Assert.AreEqual(1f, clock.Scale, 1e-6f);
            Assert.IsTrue(clock.IsSettled);
        }

        [Test]
        public void IntText_ReusesTheSameStringForANumber()
        {
            var text = new IntText(100);
            Assert.AreEqual("42", text.Get(42));
            Assert.AreSame(text.Get(42), text.Get(42));
            Assert.AreEqual("1234", text.Get(1234), "beyond the cache it still works");
        }

        [Test]
        public void MemoryCard_ReadingTimeGrowsWithTheText_WithinBounds()
        {
            var settings = new MemoryCardSettings();
            Assert.AreEqual(settings.MinReadSeconds, settings.ReadSeconds(0));
            Assert.Greater(settings.ReadSeconds(160), settings.ReadSeconds(100));
            Assert.AreEqual(settings.MaxReadSeconds, settings.ReadSeconds(10000));
        }

        private static float SecondsToCount(ScrapChipSettings settings, int delta)
        {
            var count = new CountUp(settings);
            count.Snap(0);
            count.SetTarget(delta);
            float elapsed = 0f;
            while (!count.IsSettled && elapsed < 10f)
            {
                count.Step(Frame);
                elapsed += Frame;
            }

            return elapsed;
        }

        private static void Step(Fade fade, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-6f; t += Frame)
            {
                fade.Step(Mathf.Min(Frame, seconds - t));
            }
        }
    }
}
