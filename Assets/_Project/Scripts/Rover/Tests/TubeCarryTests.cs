using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>A blueprint tube rides on 07's back from its scan until the last carried one is delivered.</summary>
    public sealed class TubeCarryTests
    {
        private const float HalfLife = 0.25f;
        private const float Step = 1f / 60f;

        private static void Run(TubeCarry carry, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                carry.Step(Step);
            }
        }

        [Test]
        public void FoundTube_EasesOn_AndOffWhenDelivered()
        {
            var carry = new TubeCarry(HalfLife);
            carry.Found("rover.cargo_cradle");
            carry.Step(Step);
            Assert.That(carry.Scale, Is.InRange(0.001f, 0.2f), "Pops on, eased.");
            Run(carry, 2f);
            Assert.Greater(carry.Scale, 0.99f);

            carry.Delivered("rover.cargo_cradle");
            Assert.IsFalse(carry.IsCarrying);
            Run(carry, 2f);
            Assert.Less(carry.Scale, 0.01f);
        }

        [Test]
        public void Restore_PutsTheTubesBackAtOnce_AndAnyDeliveryTakesOneOff()
        {
            var carry = new TubeCarry(HalfLife);
            carry.Restore(2);
            Assert.AreEqual(1f, carry.Scale, "No ride-in after a load.");
            carry.Step(Step);
            Assert.AreEqual(1f, carry.Scale, 1e-4f);

            carry.Delivered("rover.cargo_cradle");
            Assert.IsTrue(carry.IsCarrying, "One of the two is left.");
            carry.Delivered("rover.boost_coils");
            Assert.IsFalse(carry.IsCarrying);
            carry.Delivered("rover.boost_coils");
            Assert.IsFalse(carry.IsCarrying, "A stray delivery takes nothing below none.");
        }

        [Test]
        public void RestoreNone_ClearsTheBackAtOnce()
        {
            var carry = new TubeCarry(HalfLife);
            carry.Found("rover.cargo_cradle");
            Run(carry, 2f);
            carry.Restore(0);
            Assert.IsFalse(carry.IsCarrying);
            Assert.AreEqual(0f, carry.Scale, "A load with nothing carried shows nothing, with no ride-out.");
        }

        [Test]
        public void TwoTubes_StayUntilBothAreDelivered()
        {
            var carry = new TubeCarry(HalfLife);
            carry.Found("rover.cargo_cradle");
            carry.Found("rover.warm_headlamp");
            carry.Delivered("rover.cargo_cradle");
            Assert.IsTrue(carry.IsCarrying);
            carry.Delivered("rover.warm_headlamp");
            Assert.IsFalse(carry.IsCarrying);
        }
    }
}
