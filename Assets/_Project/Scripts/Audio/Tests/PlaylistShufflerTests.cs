using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class PlaylistShufflerTests
    {
        [Test]
        public void Next_NeverRepeatsImmediately_AcrossRounds([Values(2, 3, 6)] int count)
        {
            var shuffler = new PlaylistShuffler(count, new AudioRandom(11u));
            int last = shuffler.Next();
            for (int i = 0; i < 500; i++)
            {
                int next = shuffler.Next();
                Assert.AreNotEqual(last, next);
                last = next;
            }
        }

        [Test]
        public void EachRound_PlaysEveryTrackOnce()
        {
            var shuffler = new PlaylistShuffler(6, new AudioRandom(5u));
            for (int round = 0; round < 20; round++)
            {
                var seen = new bool[6];
                for (int i = 0; i < 6; i++)
                {
                    int track = shuffler.Next();
                    Assert.IsFalse(seen[track], $"track {track} repeated within round {round}");
                    seen[track] = true;
                }
            }
        }

        [Test]
        public void Resize_GrowsThePool_AndTheNewTrackJoinsTheNextRound()
        {
            var shuffler = new PlaylistShuffler(4, new AudioRandom(7u));
            shuffler.Resize(3);
            Assert.AreEqual(3, shuffler.Count);
            Assert.AreEqual(4, shuffler.Capacity);
            for (int i = 0; i < 30; i++)
            {
                Assert.Less(shuffler.Next(), 3, "only the pool's tracks");
            }

            int last = shuffler.Next();
            shuffler.Resize(4);
            var seen = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                int track = shuffler.Next();
                if (i == 0)
                {
                    Assert.AreNotEqual(last, track, "no immediate repeat across a resize");
                }

                Assert.IsFalse(seen[track]);
                seen[track] = true;
            }

            Assert.IsTrue(seen[3], "the new tape plays within the first round");
        }

        [Test]
        public void Resize_BeyondCapacity_Throws()
        {
            var shuffler = new PlaylistShuffler(2, new AudioRandom(1u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => shuffler.Resize(3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => shuffler.Resize(-1));
            shuffler.Resize(0);
            Assert.AreEqual(-1, shuffler.Next());
        }

        [Test]
        public void SingleAndEmptyPlaylists()
        {
            Assert.AreEqual(0, new PlaylistShuffler(1, new AudioRandom(1u)).Next());
            Assert.AreEqual(-1, new PlaylistShuffler(0, new AudioRandom(1u)).Next());
        }
    }
}
