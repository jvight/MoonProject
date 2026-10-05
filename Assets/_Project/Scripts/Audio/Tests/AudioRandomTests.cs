using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class AudioRandomTests
    {
        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var a = new AudioRandom(42u);
            var b = new AudioRandom(42u);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
            }
        }

        [Test]
        public void NextFloat_StaysInUnitInterval()
        {
            var random = new AudioRandom(7u);
            for (int i = 0; i < 10000; i++)
            {
                float v = random.NextFloat();
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test]
        public void PickAvoiding_NeverRepeatsAndReachesEveryOtherIndex()
        {
            var random = new AudioRandom(3u);
            var seen = new bool[5];
            int last = 2;
            for (int i = 0; i < 2000; i++)
            {
                int pick = random.PickAvoiding(5, last);
                Assert.AreNotEqual(last, pick);
                seen[pick] = true;
                last = pick;
            }

            CollectionAssert.DoesNotContain(seen, false);
            Assert.AreEqual(0, random.PickAvoiding(1, 0));
        }
    }
}
