using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class VoicePoolTests
    {
        [Test]
        public void Acquire_PrefersFreeVoices()
        {
            var pool = new VoicePool(3);

            int a = pool.Acquire(0f, 1f);
            int b = pool.Acquire(0f, 1f);
            int c = pool.Acquire(0f, 1f);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, new[] { a, b, c });
            Assert.AreEqual(3, pool.CountBusy(0.5f));
        }

        [Test]
        public void Acquire_WhenFull_StealsTheVoiceFinishingSoonest()
        {
            var pool = new VoicePool(3);
            pool.Acquire(0f, 5f);
            pool.Acquire(0f, 1f);
            pool.Acquire(0f, 3f);

            Assert.AreEqual(1, pool.Acquire(0.5f, 2f));
        }

        [Test]
        public void FinishedVoices_AreFreeAgain()
        {
            var pool = new VoicePool(2);
            int voice = pool.Acquire(0f, 1f);

            Assert.IsTrue(pool.IsBusy(voice, 0.9f));
            Assert.IsFalse(pool.IsBusy(voice, 1.1f));
            Assert.AreEqual(voice, pool.Acquire(1.1f, 1f));

            pool.Release(voice);
            Assert.AreEqual(0, pool.CountBusy(1.2f));
        }
    }
}
