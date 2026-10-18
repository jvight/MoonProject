using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The offline cutter's refusal: a short, fading head-shake of the turret, then parked again.</summary>
    public sealed class CutterRefusalTests
    {
        private const float Frame = 1f / 60f;

        private CutFocusSettings _settings;
        private CutterRefusal _refusal;

        [SetUp]
        public void SetUp()
        {
            _settings = new CutFocusSettings();
            _refusal = new CutterRefusal(_settings);
        }

        [Test]
        public void ParkedUntilARefusal()
        {
            _refusal.Step(Frame);
            Assert.IsFalse(_refusal.IsActive);
            Assert.AreEqual(0f, _refusal.Yaw);
            Assert.AreEqual(0f, _refusal.Pitch);
        }

        [Test]
        public void ARefusal_ShakesBothWays_FadesOut_AndEndsParked()
        {
            _refusal.Trigger();
            float maxYaw = 0f;
            float minYaw = 0f;
            float maxPitch = 0f;
            float elapsed = 0f;
            while (_refusal.IsActive && elapsed < 2f)
            {
                _refusal.Step(Frame);
                elapsed += Frame;
                maxYaw = Mathf.Max(maxYaw, _refusal.Yaw);
                minYaw = Mathf.Min(minYaw, _refusal.Yaw);
                maxPitch = Mathf.Max(maxPitch, _refusal.Pitch);
            }

            Assert.Greater(maxYaw, 1f, "It shakes one way...");
            Assert.Less(minYaw, -1f, "...and the other.");
            Assert.LessOrEqual(maxYaw, _settings.RefusalYawDegrees, "Never past its swing.");
            Assert.Greater(maxPitch, 0.5f, "It dips as it shakes.");
            Assert.AreEqual(_settings.RefusalSeconds, elapsed, 2f * Frame, "It lasts its set time.");
            Assert.IsFalse(_refusal.IsActive);
            Assert.AreEqual(0f, _refusal.Yaw);
            Assert.AreEqual(0f, _refusal.Pitch);
        }

        [Test]
        public void ARepeatedPress_StartsTheShakeOver()
        {
            _refusal.Trigger();
            for (int i = 0; i < 20; i++)
            {
                _refusal.Step(Frame);
            }

            _refusal.Trigger();
            _refusal.Step(Frame);
            Assert.IsTrue(_refusal.IsActive);
            Assert.Less(Mathf.Abs(_refusal.Yaw), 2f, "A fresh shake starts from the parked pose.");
        }
    }
}
