using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class LandingDetectorTests
    {
        private const float Step = 0.02f;
        private LandingSettings _settings;
        private LandingDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _settings = new LandingSettings();
            _detector = new LandingDetector(_settings);
        }

        private bool Fly(float seconds, float speedIntoGround)
        {
            bool announced = false;
            for (float t = 0f; t < seconds - 1e-4f; t += Step)
            {
                announced |= _detector.Step(false, speedIntoGround, Step);
            }

            return announced;
        }

        [Test]
        public void StartsGrounded()
        {
            Assert.IsTrue(_detector.IsGrounded);
            Assert.AreEqual(0f, _detector.AirTime);
        }

        [Test]
        public void ShortContactLoss_WithinCoyoteTime_StaysGrounded()
        {
            Fly(_settings.CoyoteTime * 0.5f, 0f);
            Assert.IsTrue(_detector.IsGrounded);
            Assert.IsFalse(_detector.Step(true, 1f, Step));
        }

        [Test]
        public void LongContactLoss_BecomesAirborne_AndCountsFromFirstLoss()
        {
            Fly(0.5f, -1f);
            Assert.IsFalse(_detector.IsGrounded);
            Assert.AreEqual(0.5f, _detector.AirTime, Step);
        }

        [Test]
        public void RealHop_IsAnnouncedWithImpactAndAirTime()
        {
            Fly(1f, 2f);
            Assert.IsTrue(_detector.Step(true, 2.5f, Step));
            Assert.AreEqual(2.5f, _detector.LastImpactSpeed, 1e-5f);
            Assert.AreEqual(1f, _detector.LastAirTime, Step);
            Assert.IsTrue(_detector.IsGrounded);
            Assert.AreEqual(0f, _detector.AirTime);
        }

        [Test]
        public void Impact_UsesPreviousStep_WhenSolverAlreadyAbsorbedIt()
        {
            Fly(1f, 3f);
            Assert.IsTrue(_detector.Step(true, 0f, Step));
            Assert.AreEqual(3f, _detector.LastImpactSpeed, 1e-5f);
        }

        [Test]
        public void SoftTouchdown_IsSilent()
        {
            Fly(1f, _settings.MinImpactSpeed * 0.5f);
            Assert.IsFalse(_detector.Step(true, _settings.MinImpactSpeed * 0.5f, Step));
            Assert.IsTrue(_detector.IsGrounded);
        }

        [Test]
        public void MicroHop_JustPastCoyoteTime_IsSilent()
        {
            Fly(_settings.CoyoteTime + 2f * Step, 3f);
            Assert.Less(_detector.AirTime, _settings.MinAirTime);
            Assert.IsFalse(_detector.Step(true, 3f, Step));
        }

        [Test]
        public void Reset_ReturnsToGrounded()
        {
            Fly(1f, 1f);
            _detector.Reset();
            Assert.IsTrue(_detector.IsGrounded);
            Assert.IsFalse(_detector.Step(true, 5f, Step));
        }
    }
}
