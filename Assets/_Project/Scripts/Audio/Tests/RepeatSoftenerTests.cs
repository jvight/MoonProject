using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class RepeatSoftenerTests
    {
        private const float MinInterval = 0.05f;
        private const float Window = 0.35f;
        private const float Decay = 0.8f;
        private const float Floor = 0.5f;

        [Test]
        public void FirstTrigger_IsFullVolume_AndTooFastRepeatsAreSkipped()
        {
            var softener = new RepeatSoftener();
            Assert.IsTrue(softener.TryTrigger(1f, MinInterval, Window, Decay, Floor, out float gain));
            Assert.AreEqual(1f, gain);
            Assert.IsFalse(softener.TryTrigger(1.02f, MinInterval, Window, Decay, Floor, out _));
        }

        [Test]
        public void Streaks_GetSofterDownToTheFloor_AndRecoverAfterAPause()
        {
            var softener = new RepeatSoftener();
            float t = 0f;
            softener.TryTrigger(t, MinInterval, Window, Decay, Floor, out _);
            float previous = 1f;
            for (int i = 1; i < 8; i++)
            {
                t += 0.1f;
                Assert.IsTrue(softener.TryTrigger(t, MinInterval, Window, Decay, Floor, out float gain));
                Assert.LessOrEqual(gain, previous);
                Assert.GreaterOrEqual(gain, Floor);
                previous = gain;
            }

            Assert.AreEqual(Floor, previous, 1e-6f);
            Assert.IsTrue(softener.TryTrigger(t + 1f, MinInterval, Window, Decay, Floor, out float rested));
            Assert.AreEqual(1f, rested, "a pause resets the streak");
        }

        [Test]
        public void SkippedRepeats_DoNotExtendTheStreak()
        {
            var softener = new RepeatSoftener();
            softener.TryTrigger(0f, MinInterval, Window, Decay, Floor, out _);
            softener.TryTrigger(0.01f, MinInterval, Window, Decay, Floor, out _);
            Assert.IsTrue(softener.TryTrigger(0.1f, MinInterval, Window, Decay, Floor, out float gain));
            Assert.AreEqual(Decay, gain, 1e-6f, "second played repeat in the streak");
        }
    }
}
