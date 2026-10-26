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
    /// and the view then eases onto the ghost.
    /// </summary>
    public sealed class RoverBuildFramingTests
    {
        private const float DockWaitSeconds = 1.5f;
        private const float FramingSeconds = 12f;
        private const float GhostMetres = 6f;
        private const float GhostBearing = 100f;
        private const float ToleranceDegrees = 25f;

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
            float view = Heading(_rover.Camera.transform.forward);
            Assert.AreEqual(0f, Mathf.DeltaAngle(view, GhostBearing), ToleranceDegrees,
                $"the view looks toward the ghost (it looks along {view:0} deg, the ghost lies at {GhostBearing} deg)");
        }
    }
}
