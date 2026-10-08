using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class LongitudinalDriveTests
    {
        private const float Step = 0.02f;
        private DriveSettings _drive;

        [SetUp]
        public void SetUp()
        {
            _drive = new DriveSettings();
        }

        private float Integrate(ref float speed, float throttle, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                speed += LongitudinalDrive.Acceleration(_drive, speed, throttle, Step) * Step;
                elapsed += Step;
            }

            return speed;
        }

        private float TimeUntil(ref float speed, float throttle, Func<float, bool> done, float limit = 20f)
        {
            float elapsed = 0f;
            while (!done(speed) && elapsed < limit)
            {
                speed += LongitudinalDrive.Acceleration(_drive, speed, throttle, Step) * Step;
                elapsed += Step;
            }

            return elapsed;
        }

        [Test]
        public void FullThrottle_ReachesNinetyPercentInAccelerationTime()
        {
            float speed = 0f;
            float time = TimeUntil(ref speed, 1f, v => v >= 0.9f * _drive.TopSpeed);
            Assert.AreEqual(_drive.AccelerationTime, time, 0.05f);
        }

        [Test]
        public void ExtraTopSpeed_SettlesAtTheBoostedCruise_ThenCoastsBackSoftly()
        {
            const float extra = 1.6f;
            float speed = _drive.TopSpeed;
            for (float t = 0f; t < 10f; t += Step)
            {
                speed += LongitudinalDrive.Acceleration(_drive, speed, 1f, extra, Step) * Step;
            }

            Assert.That(speed, Is.InRange(_drive.TopSpeed + 0.98f * extra, _drive.TopSpeed + extra));
            float decel = -LongitudinalDrive.Acceleration(_drive, speed, 1f, 0f, Step);
            Assert.Greater(decel, 0f, "Without the boost it slows back to the normal cruise...");
            Assert.Less(decel, LongitudinalDrive.Acceleration(_drive, 0f, 1f, Step), "...gently, no brake.");
            Assert.AreEqual(LongitudinalDrive.Acceleration(_drive, 3f, -1f, Step),
                LongitudinalDrive.Acceleration(_drive, 3f, -1f, extra, Step), "The boost never touches braking.");
        }

        [Test]
        public void FullThrottle_EasesOutAndNeverExceedsTopSpeed()
        {
            float speed = 0f;
            Integrate(ref speed, 1f, 20f);
            Assert.That(speed, Is.InRange(0.995f * _drive.TopSpeed, _drive.TopSpeed));
            Assert.Less(LongitudinalDrive.Acceleration(_drive, 0.95f * _drive.TopSpeed, 1f, Step),
                0.2f * LongitudinalDrive.Acceleration(_drive, 0f, 1f, Step));
        }

        [Test]
        public void HalfThrottle_SettlesAtHalfTopSpeed()
        {
            float speed = 0f;
            Integrate(ref speed, 0.5f, 20f);
            Assert.AreEqual(0.5f * _drive.TopSpeed, speed, 0.05f);
        }

        [Test]
        public void Release_CoastsToAStopInCoastStopTime()
        {
            float speed = _drive.TopSpeed;
            float time = TimeUntil(ref speed, 0f, v => v <= 0f);
            Assert.AreEqual(_drive.CoastStopTime, time, 0.06f);
            Assert.AreEqual(0f, speed, 1e-5f);
        }

        [Test]
        public void Release_DecelerationTapersNearTheStop()
        {
            float early = -LongitudinalDrive.Acceleration(_drive, _drive.TopSpeed, 0f, Step);
            float late = -LongitudinalDrive.Acceleration(_drive, 0.05f * _drive.TopSpeed, 0f, Step);
            Assert.Less(late, 0.4f * early);
        }

        [Test]
        public void AtRest_WithoutInput_StaysAtRest()
        {
            Assert.AreEqual(0f, LongitudinalDrive.Acceleration(_drive, 0f, 0f, Step));
        }

        [Test]
        public void Release_NeverOvershootsThroughZero()
        {
            float speed = 0.01f;
            speed += LongitudinalDrive.Acceleration(_drive, speed, 0f, Step) * Step;
            Assert.GreaterOrEqual(speed, 0f);
        }

        [Test]
        public void OppositeInput_BrakesSoftlyThenReverses()
        {
            float speed = _drive.TopSpeed;
            float brakeTime = TimeUntil(ref speed, -1f, v => v <= _drive.ReverseEngageSpeed);
            Assert.LessOrEqual(brakeTime, _drive.BrakeStopTime + 0.05f);
            Assert.Greater(brakeTime, 0.5f * _drive.BrakeStopTime, "Braking must ease, not slam.");

            Integrate(ref speed, -1f, 15f);
            Assert.AreEqual(-_drive.ReverseTopSpeed, speed, 0.05f);
        }

        [Test]
        public void Reverse_IsSlowerThanForward()
        {
            Assert.Less(_drive.ReverseTopSpeed, _drive.TopSpeed);
        }

        [Test]
        public void EasingOffThrottle_SlowsToTheNewTargetWithoutStopping()
        {
            float speed = _drive.TopSpeed;
            Integrate(ref speed, 0.5f, 10f);
            Assert.AreEqual(0.5f * _drive.TopSpeed, speed, 0.1f);
        }

        [Test]
        public void Coast_FromOverspeed_IsCappedAtPeakDeceleration()
        {
            float decel = -LongitudinalDrive.Acceleration(_drive, 3f * _drive.TopSpeed, 0f, Step);
            Assert.AreEqual(_drive.CoastDeceleration, decel, 1e-3f);
            Assert.That(Mathf.Approximately(_drive.CoastDeceleration,
                _drive.TopSpeed / (_drive.CoastStopTime * (1f - _drive.CoastEase))));
        }

        [Test]
        public void HoldStill_EasesToAStopInHoldStopTime_WithoutRebound()
        {
            float speed = _drive.TopSpeed;
            float elapsed = 0f;
            float previousDecel = float.MaxValue;
            while (speed > 0f && elapsed < 10f)
            {
                float accel = LongitudinalDrive.HoldAcceleration(_drive, speed, Step);
                Assert.LessOrEqual(-accel, previousDecel + 1e-4f, "The brake only ever softens.");
                previousDecel = -accel;
                speed += accel * Step;
                elapsed += Step;
                Assert.GreaterOrEqual(speed, -1e-5f, "Never rebounds backwards.");
            }

            Assert.AreEqual(_drive.HoldStopTime, elapsed, 0.06f);
            Assert.AreEqual(0f, LongitudinalDrive.HoldAcceleration(_drive, 0f, Step), "Parked stays parked.");
        }

        [Test]
        public void HoldStill_WhileReversing_StopsToo()
        {
            float speed = -_drive.ReverseTopSpeed;
            for (int i = 0; i < 200; i++)
            {
                speed += LongitudinalDrive.HoldAcceleration(_drive, speed, Step) * Step;
                Assert.LessOrEqual(speed, 1e-5f);
            }

            Assert.AreEqual(0f, speed, 1e-5f);
        }
    }
}
