using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class JumpAudioLogicTests
    {
        private const float Frame = 1f / 60f;

        private JumpAudioTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<JumpAudioTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private static void Run(JumpChargeModel charge, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                charge.Step(Frame, 0.12f, 0.05f);
            }
        }

        [Test]
        public void ChargeSteps_ClimbDMajorPentatonic()
        {
            var charge = new JumpChargeModel();
            int[] expected = { 0, 2, 4, 7, 9 };
            for (int step = 0; step < 5; step++)
            {
                charge.Charge(step / 4f);
                Run(charge, 0.5f);
                Assert.AreEqual(expected[step], charge.TargetSemitones, $"step {step}");
                Assert.AreEqual(Mathf.Pow(2f, expected[step] / 12f), charge.Pitch, 1e-3f, $"step {step} in tune");
            }

            Assert.IsTrue(charge.IsCharging);
            Assert.Greater(charge.Gain(0.55f), 0.99f, "full swell at full charge");
        }

        [Test]
        public void Charge_StartsFromSilence_AndSwellsWithStrength()
        {
            var charge = new JumpChargeModel();
            Assert.IsFalse(charge.IsAudible);
            charge.Charge(0f);
            Assert.AreEqual(0f, charge.Gain(0.55f), "no pop at the start");
            Run(charge, 0.3f);
            float start = charge.Gain(0.55f);
            charge.Charge(0.5f);
            Run(charge, 0.1f);
            Assert.Greater(charge.Gain(0.55f), start);
        }

        [Test]
        public void Leap_StopsQuickly_Cancel_FadesSoftly()
        {
            var leap = new JumpChargeModel();
            var cancel = new JumpChargeModel();
            foreach (JumpChargeModel model in new[] { leap, cancel })
            {
                model.Charge(0f);
                Run(model, 0.3f);
            }

            leap.Leap(0.06f);
            cancel.Cancel(0.2f);
            Run(leap, 0.1f);
            Run(cancel, 0.1f);
            Assert.IsFalse(leap.IsAudible, "the boing takes over at once");
            Assert.IsTrue(cancel.IsAudible, "a cancel lets go gently");
            Run(cancel, 0.15f);
            Assert.IsFalse(cancel.IsAudible);
            Assert.IsFalse(cancel.IsCharging);
        }

        [Test]
        public void ANewCharge_StartsAgainFromD()
        {
            var charge = new JumpChargeModel();
            charge.Charge(1f);
            Run(charge, 0.5f);
            charge.Leap(0.06f);
            Run(charge, 0.2f);
            charge.Charge(0f);
            Assert.AreEqual(1f, charge.Pitch, 1e-6f);
        }
    }
}
