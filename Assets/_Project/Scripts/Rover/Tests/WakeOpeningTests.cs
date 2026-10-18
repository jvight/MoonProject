using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The fresh title's held frame on 07's face: it holds with nothing moving it but its slow drift after a while
    /// untouched, the drift freezes for New Game's fade, and the handover ends it at once.
    /// </summary>
    public sealed class WakeOpeningTests
    {
        private const float Step = 1f / 60f;

        private RoverCameraTuning _tuning;
        private WakeOpening _opening;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _opening = new WakeOpening(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _opening.Step(Step);
            }
        }

        [Test]
        public void NotBegun_IsIdle()
        {
            Run(1f);
            Assert.IsFalse(_opening.IsActive);
            Assert.AreEqual(0f, _opening.Weight);
        }

        [Test]
        public void Begun_HoldsTheFrame_UntilEnded()
        {
            _opening.Begin();
            Run(60f);
            Assert.IsTrue(_opening.IsActive, "only the handover ends it");
            Assert.AreEqual(1f, _opening.Weight);
            _opening.End();
            Assert.IsFalse(_opening.IsActive);
            Assert.AreEqual(0f, _opening.Weight, "ended at once, no ease");
            Assert.AreEqual(0f, _opening.DriftYaw);
        }

        [Test]
        public void TitleDrift_StartsAfterAWhileUntouched_WithinItsSwing()
        {
            OpeningSettings settings = _tuning.Opening;
            _opening.Begin();
            Run(settings.TitleDriftDelay - 1f);
            Assert.AreEqual(0f, _opening.DriftYaw, "still before the delay");

            float most = 0f;
            for (float t = 0f; t < settings.TitleDriftPeriod; t += Step)
            {
                _opening.Step(Step);
                most = Mathf.Max(most, Mathf.Abs(_opening.DriftYaw));
            }

            Assert.Greater(most, 0.5f * settings.TitleDriftYaw, "the held frame drifts");
            Assert.LessOrEqual(most, settings.TitleDriftYaw + 1e-4f, "within its swing");
        }

        [Test]
        public void FreezeDrift_KeepsTheSwingWhereItIs()
        {
            OpeningSettings settings = _tuning.Opening;
            _opening.Begin();
            Run(settings.TitleDriftDelay + 0.25f * settings.TitleDriftPeriod);
            float frozen = _opening.DriftYaw;
            Assert.AreNotEqual(0f, frozen);
            _opening.FreezeDrift();
            Run(5f);
            Assert.AreEqual(frozen, _opening.DriftYaw, 1e-6f, "New Game's fade never moves the camera");
        }
    }
}
