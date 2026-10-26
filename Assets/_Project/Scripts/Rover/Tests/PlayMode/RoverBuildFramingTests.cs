using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Placing a structure frames 07 and the ghost together (bot findings: parked at the dock after its evening, the
    /// view stayed on the evening's frame and the build spot never came into it): entering Placing closes a held shot,
    /// and the view then eases onto the ghost, but only when the ghost starts off screen and only until it is in view.
    /// </summary>
    public sealed class RoverBuildFramingTests
    {
        private const float DockWaitSeconds = 1.5f;
        private const float FramingSeconds = 12f;
        private const float GhostMetres = 6f;
        private const float GhostBearing = 100f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        [SetUp]
        public void SetUp()
        {
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _rover?.Dispose();
            _world.Dispose();
            _physics.Dispose();
        }

        private static float Heading(Vector3 forward)
        {
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator PlacingAfterTheDocksEvening_TheViewComesRoundToTheGhost()
        {
            yield return new WaitForSeconds(2f);
            Vector3 rover = _rover.Chassis.position;
            _rover.Context.Events.Publish(new DockEveningOpened(rover));
            yield return new WaitForSeconds(DockWaitSeconds);
            Vector3 ghost = rover + Quaternion.Euler(0f, GhostBearing, 0f) * Vector3.forward * GhostMetres;
            _rover.Context.Events.Publish(new BuildPlacingChanged(true, ghost));
            yield return new WaitForSeconds(FramingSeconds);
            Vector3 viewport = _rover.Camera.WorldToViewportPoint(ghost);
            Assert.IsFalse(BuildFraming.IsOffScreen(viewport, _rover.CameraTuning.BuildFrameScreenMargin),
                $"the ghost came into view (viewport {viewport})");
        }

        [UnityTest]
        public IEnumerator AGhostAlreadyInView_NeedsNoTurnAtAll()
        {
            yield return new WaitForSeconds(2f);
            Vector3 rover = _rover.Chassis.position;
            float before = Heading(_rover.Camera.transform.forward);
            Vector3 ghost = rover + _rover.Chassis.forward * GhostMetres;
            _rover.Context.Events.Publish(new BuildPlacingChanged(true, ghost));
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(before, Heading(_rover.Camera.transform.forward), 3f, "the view stays where it was");
        }

        [UnityTest]
        public IEnumerator DrivingDuringTheTurn_EndsItAtOnce()
        {
            yield return new WaitForSeconds(2f);
            Vector3 rover = _rover.Chassis.position;
            Vector3 ghost = rover + Quaternion.Euler(0f, 160f, 0f) * Vector3.forward * GhostMetres;
            _rover.Context.Events.Publish(new BuildPlacingChanged(true, ghost));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(_rover.CameraRig.BuildFraming.IsActive, "the turn is under way");
            _rover.Drive.Drive = new Vector2(0f, 1f);
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(_rover.CameraRig.BuildFraming.IsActive, "any drive input ends it");
            _rover.Drive.Drive = Vector2.zero;
        }
    }
}
