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
        public void SingleAndEmptyPlaylists()
        {
            Assert.AreEqual(0, new PlaylistShuffler(1, new AudioRandom(1u)).Next());
            Assert.AreEqual(-1, new PlaylistShuffler(0, new AudioRandom(1u)).Next());
        }
    }
}
