using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class HoverCoilMotionTests
    {
        private const float Frame = 1f / 60f;
        private HoverCoilSettings _settings;
        private HoverCoilMotion _motion;

        [SetUp]
        public void SetUp()
        {
            _settings = new HoverCoilSettings();
            _motion = new HoverCoilMotion(_settings);
        }

        /// <summary>Steps <paramref name="seconds"/> of frames; returns the largest coil length seen.</summary>
        private float Run(float seconds, bool shown, float charge)
        {
            float longest = 0f;
            for (float t = 0f; t < seconds; t += Frame)
            {
                _motion.Step(shown, charge, Frame);
                longest = Mathf.Max(longest, _motion.CoilLength);
            }

            return longest;
        }

        [Test]
        public void WithoutTheAbility_TheCoilsStayHiddenAndDark()
        {
            Run(2f, false, 1f);
            Assert.IsFalse(_motion.Visible);
            Assert.AreEqual(0f, _motion.Glow);
        }

        [Test]
        public void OnceOn07_TheCoilsAreThereAtRestLength()
        {
            _motion.Step(true, 0f, Frame);
            Assert.IsTrue(_motion.Visible);
            Assert.AreEqual(1f, _motion.CoilLength, 1e-4f);
            _motion.Step(false, 0f, Frame);
            Assert.IsTrue(_motion.Visible, "Once fitted they stay.");
        }

        [Test]
        public void Charging_SquashesTheCoilsAndLightsThemUp()
        {
            Run(0.5f, true, 0f);
            Run(1f, true, 1f);
            Assert.AreEqual(1f - _settings.ChargeSquash, _motion.CoilLength, 0.01f);
            Assert.AreEqual(1f, _motion.Glow, 0.01f);

            Run(1.2f, true, 0.5f);
            Assert.AreEqual(1f - 0.5f * _settings.ChargeSquash, _motion.CoilLength, 0.01f);
            Assert.AreEqual(0.5f, _motion.Glow, 0.01f);
        }

        [Test]
        public void Releasing_SpringsTheCoilsOut_AndALeapKicksHarder()
        {
            Run(1f, true, 1f);
            float released = Run(0.6f, true, 0f);
            Assert.Greater(released, 1.05f, "Letting go of a full charge springs the coils past their rest length.");

            Run(1f, true, 1f);
            _motion.Leap(1f);
            float leapt = Run(0.6f, true, 0f);
            Assert.Greater(leapt, released, "The leap kicks them out further.");
            Assert.LessOrEqual(leapt, _settings.MaxStretch + 1e-4f);
            Run(2f, true, 0f);
            Assert.AreEqual(1f, _motion.CoilLength, 0.01f, "And they settle back.");
        }

        [Test]
        public void ATap_GivesTheSpringsASmallBoing()
        {
            Run(0.5f, true, 0f);
            _motion.Leap(0f);
            Assert.Greater(Run(0.5f, true, 0f), 1.05f);
        }

        [Test]
        public void AfterTheCharge_TheGlowFadesToExactlyDark()
        {
            Run(1f, true, 1f);
            Run(0.1f, true, 0f);
            Assert.Greater(_motion.Glow, 0.3f, "It fades rather than snapping off.");
            Run(3f, true, 0f);
            Assert.AreEqual(0f, _motion.Glow, "Dark, so the light switches off.");
        }
    }
}
