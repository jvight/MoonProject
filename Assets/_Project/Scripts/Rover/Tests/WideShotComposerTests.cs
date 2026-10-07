using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The wide shot's composition math: a gentle turn toward Earth or The Peak, the terrain clearance clamp (rise,
    /// then come closer) and the breathing.
    /// </summary>
    public sealed class WideShotComposerTests
    {
        private static readonly Vector3 Follow = new Vector3(0f, 1.1f, 0f);

        private WideShotSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new WideShotSettings();
        }

        private static Vector3 CameraPosition(Vector3 follow, WideShotFrame frame)
        {
            float elevation = frame.Elevation * Mathf.Deg2Rad;
            return follow - WideShotComposer.Direction(frame.Yaw) * (frame.Distance * Mathf.Cos(elevation))
                + Vector3.up * (frame.Distance * Mathf.Sin(elevation));
        }

        /// <summary>The camera keeps its clearance and the line to 07 stays above the ground all the way.</summary>
        private void AssertClear(ITerrainQuery terrain, WideShotFrame frame)
        {
            Vector3 camera = CameraPosition(Follow, frame);
            Assert.GreaterOrEqual(camera.y, terrain.SampleHeight(camera.x, camera.z) + 0.95f * _settings.Clearance,
                "The camera keeps its clearance above the ground.");
            for (int i = 1; i < 32; i++)
            {
                Vector3 point = Vector3.Lerp(Follow, camera, i / 32f);
                Assert.Greater(point.y, terrain.SampleHeight(point.x, point.z),
                    $"The line to 07 clears the ground at {point}.");
            }
        }

        [Test]
        public void Bearing_AndDirection_AreInverse()
        {
            Assert.AreEqual(0f, WideShotComposer.Bearing(Vector3.forward), 1e-4f);
            Assert.AreEqual(90f, WideShotComposer.Bearing(Vector3.right), 1e-4f);
            Assert.AreEqual(-135f, WideShotComposer.Bearing(WideShotComposer.Direction(-135f)), 1e-3f);
        }

        [Test]
        public void SubjectSwing_TurnsAShareOfTheWayTowardEarth()
        {
            float swing = WideShotComposer.SubjectSwing(_settings, 10f, 30f, 170f);
            Assert.AreEqual(20f * _settings.YawShare, swing, 1e-3f);
        }

        [Test]
        public void SubjectSwing_IsAGentleBias_NeverAHardTurn()
        {
            Assert.AreEqual(_settings.MaxYawSwing, WideShotComposer.SubjectSwing(_settings, 0f, 95f, 180f), 1e-3f);
            Assert.AreEqual(-_settings.MaxYawSwing, WideShotComposer.SubjectSwing(_settings, 0f, -95f, 180f), 1e-3f);
        }

        [Test]
        public void SubjectSwing_PrefersEarth_UnlessThePeakIsClearlyNearer()
        {
            float nearlyAsClose = WideShotComposer.SubjectSwing(_settings, 0f, 30f, 20f);
            Assert.AreEqual(30f * _settings.YawShare, nearlyAsClose, 1e-3f, "Earth wins a close call.");
            float peakMuchNearer = WideShotComposer.SubjectSwing(_settings, 0f, 70f, -10f);
            Assert.AreEqual(-10f * _settings.YawShare, peakMuchNearer, 1e-3f, "The Peak when it is clearly nearer.");
        }

        [Test]
        public void SubjectSwing_OutOfReach_LeavesTheViewWhereThePlayerLeftIt()
        {
            Assert.AreEqual(0f, WideShotComposer.SubjectSwing(_settings, 0f, 150f, -160f), 1e-3f);
            float peakOnly = WideShotComposer.SubjectSwing(_settings, 0f, 170f, 20f);
            Assert.AreEqual(20f * _settings.YawShare, peakOnly, 1e-3f, "The Peak when Earth is behind.");
        }

        [Test]
        public void Fit_OnFlatGround_UsesTheFullLowWideFrame()
        {
            var flat = new FuncTerrain((x, z) => 0f);
            WideShotFrame frame = WideShotComposer.Fit(_settings, flat, Follow, 25f, 0f);
            Assert.AreEqual(25f, frame.Yaw, 1e-4f);
            Assert.AreEqual(_settings.Distance, frame.Distance, 1e-3f);
            Assert.AreEqual(_settings.Elevation, frame.Elevation, 1e-3f);
            Assert.AreEqual(_settings.ScreenY, frame.ScreenY, 1e-4f);
            AssertClear(flat, frame);
        }

        [Test]
        public void Fit_GroundRisingBehind_LiftsTheCameraJustEnough()
        {
            float slope = Mathf.Tan(15f * Mathf.Deg2Rad);
            var rising = new FuncTerrain((x, z) => z < 0f ? -z * slope : 0f);
            WideShotFrame frame = WideShotComposer.Fit(_settings, rising, Follow, 0f, 0f);
            Assert.AreEqual(_settings.Distance, frame.Distance, 1e-3f, "Rising first keeps the full distance.");
            Assert.Greater(frame.Elevation, _settings.Elevation + 5f);
            Assert.LessOrEqual(frame.Elevation, _settings.MaxElevation);
            AssertClear(rising, frame);
        }

        [Test]
        public void Fit_CliffCloseBehind_ComesCloserInstead()
        {
            var cliff = new FuncTerrain((x, z) => z < -15f ? 40f : 0f);
            WideShotFrame frame = WideShotComposer.Fit(_settings, cliff, Follow, 0f, 0f);
            Assert.Less(frame.Distance, _settings.Distance);
            Assert.GreaterOrEqual(frame.Distance, _settings.MinDistance);
            Assert.Greater(CameraPosition(Follow, frame).z, -15f, "The camera stays out of the cliff.");
            AssertClear(cliff, frame);
        }

        [Test]
        public void Fit_NothingFits_SettlesForTheClosestHighestFrame()
        {
            var pit = new FuncTerrain((x, z) => x * x + z * z < 4f ? 0f : 200f);
            WideShotFrame frame = WideShotComposer.Fit(_settings, pit, Follow, 0f, 0f);
            Assert.AreEqual(_settings.MinDistance, frame.Distance, 1e-3f);
            Assert.AreEqual(_settings.MaxElevation, frame.Elevation, 1e-3f);
        }

        [Test]
        public void ShutIn_TellsOpenGroundFromACanyon()
        {
            Assert.AreEqual(0f, WideShotComposer.ShutIn(_settings, new FuncTerrain((x, z) => 0f), Follow), 1e-4f);
            var farRim = new FuncTerrain((x, z) => x * x + z * z > 250f * 250f ? 120f : 0f);
            Assert.AreEqual(0f, WideShotComposer.ShutIn(_settings, farRim, Follow), 1e-4f, "A far rim is a horizon.");
            var canyon = new FuncTerrain((x, z) => Mathf.Abs(x) < 6f ? 0f : 30f);
            Assert.Greater(WideShotComposer.ShutIn(_settings, canyon, Follow), 0.95f, "Walls close all round.");
        }

        [Test]
        public void Solve_InANarrowCanyon_TakesTheCloserHigherFrameAlongIt()
        {
            var canyon = new FuncTerrain((x, z) => Mathf.Abs(x) < 6f ? 0f : 30f);
            float shutIn = WideShotComposer.ShutIn(_settings, canyon, Follow);
            WideShotFrame frame = WideShotComposer.Solve(_settings, canyon, Follow, 0f, _settings.MaxYawSwing, shutIn);
            Assert.AreEqual(0f, frame.Yaw, 1e-3f, "No turn toward Earth: it would put a wall in the frame.");
            Assert.AreEqual(_settings.ShutInDistance, frame.Distance, 0.05f);
            Assert.GreaterOrEqual(frame.Elevation, _settings.ShutInElevation - 1e-3f);
            Assert.AreEqual(_settings.ShutInScreenY, frame.ScreenY, 0.01f);
            AssertClear(canyon, frame);
        }

        [Test]
        public void Solve_InAWideValley_TakesTheFullTurnTowardEarth()
        {
            var valley = new FuncTerrain((x, z) => Mathf.Abs(x) < 40f ? 0f : 30f);
            float shutIn = WideShotComposer.ShutIn(_settings, valley, Follow);
            Assert.AreEqual(0f, shutIn, 1e-4f, "Open enough for the full wide shot.");
            WideShotFrame frame = WideShotComposer.Solve(_settings, valley, Follow, 0f, _settings.MaxYawSwing, shutIn);
            Assert.AreEqual(_settings.MaxYawSwing, frame.Yaw, 1e-3f);
            Assert.AreEqual(_settings.Distance, frame.Distance, 1e-3f);
            AssertClear(valley, frame);
        }

        [Test]
        public void Solve_HalfwayTurn_WhenOnlyItFitsWide()
        {
            var valley = new FuncTerrain((x, z) => Mathf.Abs(x) < 13f ? 0f : 30f);
            WideShotFrame frame = WideShotComposer.Solve(_settings, valley, Follow, 0f, 40f, 0f);
            Assert.AreEqual(20f, frame.Yaw, 1e-3f);
            Assert.AreEqual(_settings.Distance, frame.Distance, 1e-3f);
            AssertClear(valley, frame);
        }

        [Test]
        public void Breath_StartsFromRest_AndStaysWithinItsAmplitude()
        {
            Assert.AreEqual(0f, WideShotComposer.Breath(2f, 20f, 0f), 1e-5f);
            float largest = 0f;
            for (float t = 0f; t < 40f; t += 0.1f)
            {
                largest = Mathf.Max(largest, Mathf.Abs(WideShotComposer.Breath(2f, 20f, t)));
            }

            Assert.AreEqual(2f, largest, 0.01f);
        }
    }
}
