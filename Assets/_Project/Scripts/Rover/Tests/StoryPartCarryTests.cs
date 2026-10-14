using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>A tower story part rides on 07's back from its pickup until it is delivered.</summary>
    public sealed class StoryPartCarryTests
    {
        private const float HalfLife = 0.25f;
        private const float Step = 1f / 60f;

        private static void Run(StoryPartCarry carry, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                carry.Step(Step);
            }
        }

        [Test]
        public void StoryPart_EasesOn_AndEasesOffStillNamedAfterDelivery()
        {
            var carry = new StoryPartCarry(HalfLife);
            Assert.IsNull(carry.Part);
            carry.PartFound("Part_TowerCoil", false);
            Assert.AreEqual("Part_TowerCoil", carry.Part);
            carry.Step(Step);
            Assert.That(carry.PartScale, Is.InRange(0.001f, 0.2f), "Pops on, eased.");
            Run(carry, 2f);
            Assert.Greater(carry.PartScale, 0.99f);

            carry.PartDelivered("Part_TowerCoil");
            carry.Step(Step);
            Assert.AreEqual("Part_TowerCoil", carry.Part, "Still named while it eases off.");
            Run(carry, 2f);
            Assert.Less(carry.PartScale, 0.01f);
        }

        [Test]
        public void StoryParts_ShowTheFirstCarried_AndARestoredOneAtOnce()
        {
            var carry = new StoryPartCarry(HalfLife);
            carry.PartFound("Part_Transformer", true);
            Assert.AreEqual(1f, carry.PartScale, "No ride-in after a load.");
            carry.PartFound("Part_TowerCoil", false);
            Assert.AreEqual("Part_Transformer", carry.Part, "The first picked up stays on show.");
            carry.PartDelivered("Part_Transformer");
            Assert.AreEqual("Part_TowerCoil", carry.Part);
        }
    }
}
