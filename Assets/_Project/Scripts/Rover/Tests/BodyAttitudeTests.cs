using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class BodyAttitudeTests
    {
        private const float Frame = 1f / 60f;
        private RoverRigTuning _tuning;
        private BodyAttitude _attitude;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverRigTuning>();
            _attitude = new BodyAttitude(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Ground(Vector3 normal, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _attitude.StepGrounded(normal, Frame);
            }
        }

        [Test]
        public void SettlesOntoTheGroundPlane()
        {
            Vector3 normal = GroundPlaneFit.Tilt(20f, -5f) * Vector3.up;
            Ground(normal, 3f);
            Assert.AreEqual(GroundPlaneFit.PitchOf(normal), _attitude.Pitch, 0.1f);
            Assert.AreEqual(GroundPlaneFit.RollOf(normal), _attitude.Roll, 0.1f);
        }

        [Test]
        public void SettlingIsSprungNotSnapped()
        {
            _attitude.StepGrounded(GroundPlaneFit.Tilt(20f, 0f) * Vector3.up, Frame);
            Assert.Less(_attitude.Pitch, 2f, "No frame-one snap onto a new slope.");
        }

        [Test]
        public void SteepSlope_IsClampedToMaxTilt()
        {
            Ground(GroundPlaneFit.Tilt(70f, 0f) * Vector3.up, 3f);
            Assert.LessOrEqual(_attitude.Pitch, _tuning.MaxTilt + 1e-3f);
        }

        [Test]
        public void InTheAir_DriftsBackTowardUpright()
        {
            _attitude.Reset(25f, -15f);
            for (float t = 0f; t < 4f; t += Frame)
            {
                _attitude.StepAirborne(0f, 6f, Frame);
            }

            Assert.AreEqual(0f, _attitude.Pitch, 0.5f);
            Assert.AreEqual(0f, _attitude.Roll, 0.5f);
        }

        [Test]
        public void InTheAir_NoseFollowsTheFlightPathALittle()
        {
            for (float t = 0f; t < 3f; t += Frame)
            {
                _attitude.StepAirborne(-3f, 8f, Frame);
            }

            Assert.Less(_attitude.Pitch, -1f);
            Assert.GreaterOrEqual(_attitude.Pitch, -_tuning.MaxAirPitch - 1e-3f);
        }

        [Test]
        public void VerticalDrop_StaysLevel()
        {
            for (float t = 0f; t < 3f; t += Frame)
            {
                _attitude.StepAirborne(-3f, 0f, Frame);
            }

            Assert.AreEqual(0f, _attitude.Pitch, 0.01f);
        }
    }
}
