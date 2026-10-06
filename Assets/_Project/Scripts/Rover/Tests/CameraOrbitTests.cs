using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class CameraOrbitTests
    {
        private const float Frame = 1f / 60f;
        private RoverCameraTuning _tuning;
        private CameraOrbit _orbit;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _orbit = new CameraOrbit(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds, Vector2 lookPerFrame, float speed, float descent = 0f, bool grounded = true)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _orbit.Step(lookPerFrame, speed, speed / 8f, descent, grounded, Frame);
            }
        }

        [Test]
        public void StartsBehindAtTheLowOpeningElevation()
        {
            Assert.AreEqual(0f, _orbit.YawOffset);
            Assert.AreEqual(_tuning.OpeningPitch, _orbit.Elevation);
            Assert.Less(_tuning.OpeningPitch, _tuning.DefaultPitch);
            Assert.AreEqual(_tuning.BaseFov, _orbit.FieldOfView);
        }

        [Test]
        public void OpeningShot_HoldsWhileParked_AndEasesToRestingElevationOnceDriving()
        {
            Run(10f, Vector2.zero, 0f);
            Assert.AreEqual(_tuning.OpeningPitch, _orbit.Elevation, 1e-3f, "The opening shot holds while 07 waits.");

            _orbit.Step(Vector2.zero, 3f, 0.4f, 0f, true, Frame);
            Assert.Less(_orbit.Elevation, _tuning.OpeningPitch + 0.5f, "No jump when driving starts.");
            Run(10f, Vector2.zero, 3f);
            Assert.AreEqual(_tuning.DefaultPitch, _orbit.Elevation, 0.5f);
        }

        [Test]
        public void LookInput_OrbitsByTheRequestedAmount()
        {
            Run(1f, new Vector2(1f, 0f), 0f);
            Run(1f, Vector2.zero, 0f);
            Assert.AreEqual(60f, _orbit.YawOffset, 1.5f, "Smoothing delays but never loses input.");
        }

        [Test]
        public void LookInput_GlidesInsteadOfStepping()
        {
            _orbit.Step(new Vector2(10f, 0f), 0f, 0f, 0f, true, Frame);
            Assert.Less(_orbit.YawOffset, 10f);
            Assert.Greater(_orbit.YawOffset, 0f);
        }

        [Test]
        public void Elevation_IsClamped()
        {
            Run(2f, new Vector2(0f, 5f), 0f);
            Assert.AreEqual(_tuning.MaxPitch, _orbit.Elevation, 1e-3f);
            Run(4f, new Vector2(0f, -5f), 0f);
            Assert.AreEqual(_tuning.MinPitch, _orbit.Elevation, 1e-3f);
        }

        [Test]
        public void Recenter_WaitsForIdle_ThenEasesBehindTheRover()
        {
            Run(0.75f, new Vector2(1f, 0f), 5f);
            Run(0.5f, Vector2.zero, 5f);
            float offset = _orbit.YawOffset;
            Run(_tuning.RecenterDelay - 0.7f, Vector2.zero, 5f);
            Assert.AreEqual(offset, _orbit.YawOffset, 1f, "No recentering before the idle delay.");

            Run(0.3f, Vector2.zero, 5f);
            Assert.Less(_orbit.RecenterWeight, 0.2f, "Recentering fades in, it does not yank.");

            Run(10f, Vector2.zero, 5f);
            Assert.AreEqual(0f, _orbit.YawOffset, 0.5f);
            Assert.AreEqual(_tuning.DefaultPitch, _orbit.Elevation, 0.5f);
        }

        [Test]
        public void Recenter_WhenParked_LeavesTheViewAlone()
        {
            Run(0.75f, new Vector2(1f, 0f), 0f);
            Run(0.5f, Vector2.zero, 0f);
            float offset = _orbit.YawOffset;
            Run(15f, Vector2.zero, 0f);
            Assert.AreEqual(offset, _orbit.YawOffset, 0.01f);
        }

        [Test]
        public void Recenter_IsCancelledByLookInput()
        {
            Run(0.75f, new Vector2(1f, 0f), 5f);
            Run(_tuning.RecenterDelay + 1f, Vector2.zero, 5f);
            Assert.Greater(_orbit.RecenterWeight, 0f);
            _orbit.Step(new Vector2(0.1f, 0f), 5f, 0.6f, 0f, true, Frame);
            Assert.AreEqual(0f, _orbit.RecenterWeight);
        }

        [Test]
        public void Recenter_TakesTheShortWayRound()
        {
            Run(3f, new Vector2(1f, 0f), 5f);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 150f);
            float before = Mathf.Abs(_orbit.YawOffset);
            Run(_tuning.RecenterDelay + 1.5f, Vector2.zero, 5f);
            Assert.Less(Mathf.Abs(_orbit.YawOffset), before);
        }

        [Test]
        public void Downhill_LiftsTheCameraGently()
        {
            _orbit.Step(Vector2.zero, 6f, 0.75f, _tuning.DownhillFullAngle, true, Frame);
            Assert.Less(_orbit.Lift, 1f);
            Run(5f, Vector2.zero, 6f, _tuning.DownhillFullAngle);
            Assert.AreEqual(_tuning.DownhillLift, _orbit.Lift, 0.1f);
            Run(5f, Vector2.zero, 6f, -10f);
            Assert.AreEqual(0f, _orbit.Lift, 0.1f, "Climbing never lowers the camera below its normal orbit.");
        }

        [Test]
        public void Lift_HoldsWhileAirborne()
        {
            Run(5f, Vector2.zero, 6f, _tuning.DownhillFullAngle);
            float lift = _orbit.Lift;
            Run(1f, Vector2.zero, 6f, -30f, false);
            Assert.AreEqual(lift, _orbit.Lift, 1e-4f);
        }

        [Test]
        public void FieldOfView_WidensSlightlyWithSpeed()
        {
            Run(5f, Vector2.zero, 8f);
            Assert.AreEqual(_tuning.BaseFov + _tuning.SpeedFovBoost, _orbit.FieldOfView, 0.1f);
            Run(5f, Vector2.zero, 0f);
            Assert.AreEqual(_tuning.BaseFov, _orbit.FieldOfView, 0.1f);
        }
    }
}
