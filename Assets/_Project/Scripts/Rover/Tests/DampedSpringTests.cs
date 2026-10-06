using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class DampedSpringTests
    {
        [TestCase(0.3f)]
        [TestCase(1f)]
        [TestCase(2f)]
        public void Step_ConvergesToTarget(float damping)
        {
            var spring = new DampedSpring(0f);
            for (int i = 0; i < 600; i++)
            {
                spring.Step(1f, 2f, damping, 1f / 60f);
            }

            Assert.AreEqual(1f, spring.Value, 1e-3f);
            Assert.AreEqual(0f, spring.Velocity, 1e-2f);
        }

        [TestCase(0.42f)]
        [TestCase(1f)]
        [TestCase(1.6f)]
        public void Step_IsFrameRateIndependent(float damping)
        {
            var slow = new DampedSpring(0f);
            var fast = new DampedSpring(0f);
            slow.AddVelocity(2f);
            fast.AddVelocity(2f);
            for (int i = 0; i < 30; i++)
            {
                slow.Step(1f, 1.5f, damping, 1f / 30f);
            }

            for (int i = 0; i < 144; i++)
            {
                fast.Step(1f, 1.5f, damping, 1f / 144f);
            }

            Assert.AreEqual(slow.Value, fast.Value, 1e-4f);
            Assert.AreEqual(slow.Velocity, fast.Velocity, 1e-3f);
        }

        [Test]
        public void Step_JellyDamping_OvershootsOnceSoftly()
        {
            const float Visible = 0.02f;
            var spring = new DampedSpring(0f);
            int crossings = 0;
            float previous = spring.Value - 1f;
            float peak = 0f;
            for (int i = 0; i < 600; i++)
            {
                spring.Step(1f, 1.5f, 0.42f, 1f / 60f);
                float error = spring.Value - 1f;
                if (Mathf.Sign(error) != Mathf.Sign(previous) && Mathf.Abs(error) > Visible)
                {
                    crossings++;
                }

                if (Mathf.Abs(error) > Visible)
                {
                    previous = error;
                }

                peak = Mathf.Max(peak, spring.Value);
            }

            Assert.That(peak, Is.InRange(1.1f, 1.35f), "One visible, soft overshoot.");
            Assert.LessOrEqual(crossings, 2, "Settles within two visible overshoots.");
        }

        [TestCase(1f)]
        [TestCase(1.5f)]
        public void Step_CriticalOrOverDamped_NeverOvershoots(float damping)
        {
            var spring = new DampedSpring(0f);
            for (int i = 0; i < 600; i++)
            {
                spring.Step(1f, 3f, damping, 1f / 60f);
                Assert.LessOrEqual(spring.Value, 1f + 1e-5f);
            }
        }

        [Test]
        public void Clamp_StopsAtLimitAndDropsOutwardVelocity()
        {
            var spring = new DampedSpring(0f);
            spring.AddVelocity(50f);
            spring.Step(0f, 1f, 0.5f, 0.1f);
            spring.Clamp(-0.1f, 0.1f);

            Assert.AreEqual(0.1f, spring.Value);
            Assert.LessOrEqual(spring.Velocity, 0f);
        }

        [Test]
        public void Step_ZeroDeltaTime_DoesNothing()
        {
            var spring = new DampedSpring(0.5f);
            spring.Step(1f, 2f, 0.5f, 0f);
            Assert.AreEqual(0.5f, spring.Value);
        }
    }
}
