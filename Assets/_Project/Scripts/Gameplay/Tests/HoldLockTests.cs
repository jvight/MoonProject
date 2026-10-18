using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    public sealed class HoldLockTests
    {
        private sealed class Target
        {
        }

        private static bool Valid(Target target)
        {
            return true;
        }

        [Test]
        public void ReleasedKey_OffersWhatTheVerbOffers()
        {
            var hold = new HoldLock<Target>();
            var a = new Target();

            Assert.AreSame(a, hold.Resolve(false, a, false, Valid));
            Assert.IsNull(hold.Resolve(false, null, false, Valid));
        }

        [Test]
        public void TheTargetAtThePress_StaysWhileTheOfferFlips()
        {
            var hold = new HoldLock<Target>();
            var seal = new Target();
            var other = new Target();

            Assert.AreSame(seal, hold.Resolve(true, seal, false, Valid));
            Assert.AreSame(seal, hold.Resolve(true, other, false, Valid), "another offer cannot take the hold");
            Assert.AreSame(seal, hold.Resolve(true, null, true, Valid), "nor can yielding to another verb");
            Assert.AreSame(seal, hold.Locked);
        }

        [Test]
        public void ReleasingTheKey_FreesTheLock()
        {
            var hold = new HoldLock<Target>();
            var seal = new Target();
            var other = new Target();
            hold.Resolve(true, seal, false, Valid);
            hold.Resolve(false, other, false, Valid);

            Assert.AreSame(other, hold.Resolve(true, other, false, Valid), "the next press locks anew");
        }

        [Test]
        public void AHoldThatBeganYielded_StaysSilentUntilReleased()
        {
            var hold = new HoldLock<Target>();
            var seal = new Target();

            Assert.IsNull(hold.Resolve(true, null, true, Valid));
            Assert.IsNull(hold.Resolve(true, seal, false, Valid), "the seal cannot take a hold the scan began");
            Assert.AreSame(seal, hold.Resolve(false, seal, false, Valid));
        }

        [Test]
        public void ALockedTargetThatCannotBeWorkedAnymore_EndsTheHoldWithoutHandingItOver()
        {
            var hold = new HoldLock<Target>();
            var seal = new Target();
            var other = new Target();
            hold.Resolve(true, seal, false, Valid);

            Assert.IsNull(hold.Resolve(true, other, false, _ => false));
            Assert.IsNull(hold.Resolve(true, other, false, Valid), "a neighbour does not inherit the hold");
        }

        [Test]
        public void AHoldWithNoTargetAtThePress_TakesOneThatComesIntoReach()
        {
            var hold = new HoldLock<Target>();
            var seal = new Target();

            Assert.IsNull(hold.Resolve(true, null, false, Valid));
            Assert.AreSame(seal, hold.Resolve(true, seal, false, Valid));
        }
    }
}
