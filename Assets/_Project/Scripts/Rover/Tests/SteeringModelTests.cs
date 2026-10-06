using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class SteeringModelTests
    {
        private const float TopSpeed = 8f;
        private SteeringSettings _steering;

        [SetUp]
        public void SetUp()
        {
            _steering = new SteeringSettings();
        }

        [Test]
        public void TurnRate_AtStandstill_IsTheCalmPivotRate()
        {
            Assert.AreEqual(_steering.PivotTurnRate, SteeringModel.TurnRate(_steering, 0f, TopSpeed), 1e-4f);
            Assert.Greater(_steering.PivotTurnRate, 0f, "07 can always turn on the spot: no three-point turns.");
            Assert.Less(_steering.PivotTurnRate, _steering.PeakTurnRate);
        }

        [Test]
        public void TurnRate_PeaksAtWalkingPaceAndWidensAtTopSpeed()
        {
            float peak = SteeringModel.TurnRate(_steering, _steering.PeakTurnSpeed, TopSpeed);
            float top = SteeringModel.TurnRate(_steering, TopSpeed, TopSpeed);
            Assert.AreEqual(_steering.PeakTurnRate, peak, 1e-3f);
            Assert.AreEqual(_steering.TopSpeedTurnRate, top, 1e-3f);
            Assert.Less(top, peak);
        }

        [Test]
        public void TurnRate_IsSymmetricInSpeedSign()
        {
            Assert.AreEqual(SteeringModel.TurnRate(_steering, 2f, TopSpeed),
                SteeringModel.TurnRate(_steering, -2f, TopSpeed), 1e-5f);
        }

        [Test]
        public void TurnRadius_AtTopSpeed_IsAComfortableArc()
        {
            float radius = TopSpeed / (SteeringModel.TurnRate(_steering, TopSpeed, TopSpeed) * Mathf.Deg2Rad);
            Assert.That(radius, Is.InRange(5f, 12f));
        }

        [TestCase(5f, 1f, 1f)]
        [TestCase(0f, 0f, 1f)]
        [TestCase(0f, 1f, 1f)]
        [TestCase(0f, -1f, -1f)]
        [TestCase(-2f, 0f, -1f)]
        [TestCase(-2f, 1f, -1f)]
        [TestCase(3f, -1f, 1f)]
        public void SteerDirection_FlipsOnlyWhenBackingUp(float forwardSpeed, float throttle, float expected)
        {
            Assert.AreEqual(expected, SteeringModel.SteerDirection(_steering, forwardSpeed, throttle, 0.05f));
        }

        [Test]
        public void YawRate_InTheAir_IsReduced()
        {
            float ground = SteeringModel.YawRate(_steering, 1f, 1f, 4f, TopSpeed, true);
            float air = SteeringModel.YawRate(_steering, 1f, 1f, 4f, TopSpeed, false);
            Assert.AreEqual(ground * _steering.AirTurnFactor, air, 1e-4f);
        }

        [Test]
        public void YawRate_RightInput_TurnsClockwise()
        {
            Assert.Greater(SteeringModel.YawRate(_steering, 1f, 1f, 4f, TopSpeed, true), 0f);
            Assert.Less(SteeringModel.YawRate(_steering, 1f, -1f, -2f, TopSpeed, true), 0f);
        }
    }
}
