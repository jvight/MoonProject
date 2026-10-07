using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class FootstepCadenceTests
    {
        private const float Stride = 0.5f;
        private const float Teleport = 3f;

        [Test]
        public void OneStepPerStride_OfHorizontalTravel()
        {
            var steps = new FootstepCadence();
            Assert.IsFalse(steps.Step(Vector3.zero, Stride, Teleport), "the first position only sets the start");

            int count = 0;
            for (int i = 1; i <= 104; i++)
            {
                if (steps.Step(new Vector3(0.05f * i, 0f, 0f), Stride, Teleport))
                {
                    count++;
                }
            }

            Assert.AreEqual(10, count, "5.2 m at 0.5 m per stride");
        }

        [Test]
        public void ClimbingInPlace_IsNotWalking()
        {
            var steps = new FootstepCadence();
            steps.Step(Vector3.zero, Stride, Teleport);
            for (int i = 1; i <= 50; i++)
            {
                Assert.IsFalse(steps.Step(new Vector3(0f, 0.1f * i, 0f), Stride, Teleport));
            }
        }

        [Test]
        public void BeingPlacedAtHome_IsNotABurstOfSteps()
        {
            var steps = new FootstepCadence();
            steps.Step(Vector3.zero, Stride, Teleport);
            steps.Step(new Vector3(0.4f, 0f, 0f), Stride, Teleport);
            Assert.IsFalse(steps.Step(new Vector3(80f, 0f, 60f), Stride, Teleport), "a jump past the teleport range");
            Assert.IsFalse(steps.Step(new Vector3(80.2f, 0f, 60f), Stride, Teleport),
                "travel before the jump was forgotten");
            Assert.IsTrue(steps.Step(new Vector3(80.6f, 0f, 60f), Stride, Teleport));
        }

        [Test]
        public void AFastFrame_NeverBanksMoreThanOneExtraStep()
        {
            var steps = new FootstepCadence();
            steps.Step(Vector3.zero, Stride, Teleport);
            Assert.IsTrue(steps.Step(new Vector3(2.9f, 0f, 0f), Stride, Teleport));
            Assert.IsTrue(steps.Step(new Vector3(2.95f, 0f, 0f), Stride, Teleport), "one banked step");
            Assert.IsFalse(steps.Step(new Vector3(3f, 0f, 0f), Stride, Teleport), "not a burst");
        }
    }
}
