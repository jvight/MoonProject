using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>When a friend's gift shows: silently on load, with the soft moment when 07 next comes home.</summary>
    public sealed class FriendGiftTests
    {
        [Test]
        public void FriendAlreadyRepaired_OnLoad_ShowsSilently_EvenAway()
        {
            var gift = new FriendGift();
            Assert.AreEqual(GiftCue.ShowSilently, gift.Step(true, false));
            Assert.IsTrue(gift.Given);
            Assert.AreEqual(GiftCue.None, gift.Step(true, true), "Never presented again.");
        }

        [Test]
        public void FriendStillBroken_NoGift()
        {
            var gift = new FriendGift();
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(GiftCue.None, gift.Step(false, i % 2 == 0));
            }

            Assert.IsFalse(gift.Given);
        }

        [Test]
        public void RepairedDuringPlay_WaitsFor07ToComeHome_ThenPresentsOnce()
        {
            var gift = new FriendGift();
            Assert.AreEqual(GiftCue.None, gift.Step(false, true));
            Assert.AreEqual(GiftCue.None, gift.Step(true, false), "Repaired out in the field: not yet.");
            Assert.AreEqual(GiftCue.None, gift.Step(true, false));
            Assert.AreEqual(GiftCue.Present, gift.Step(true, true), "07 comes home: the gift.");
            Assert.IsTrue(gift.Given);
            Assert.AreEqual(GiftCue.None, gift.Step(true, true), "Once.");
        }

        [Test]
        public void RepairedDuringPlay_PendingSurvivesAFriendNapping()
        {
            var gift = new FriendGift();
            gift.Step(false, false);
            gift.Step(true, false);
            Assert.AreEqual(GiftCue.Present, gift.Step(false, true), "Earned is earned, even if she is off again.");
        }
    }
}
