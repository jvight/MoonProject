using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>Design ruling 7 (real wiring): wedged against a wall, 07 is lifted gently onto open ground.</summary>
    public sealed class RoverStuckRecoveryTests : InputTestFixture
    {
        /// <summary>07 counts as at the wall once its contact point is this close to the wall line (m).</summary>
        private const float WallReach = 2f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(TestWorld.WallX, TestWorld.WallZ - 12f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PushingIntoAWall_LiftsSoftlyOntoOpenGround_ThenDrivesOn()
        {
            RoverController controller = _rover.Controller;
            RecoverySettings recovery = _rover.Tuning.Recovery;
            var lifts = new List<RoverRecovering>();
            var landings = new List<RoverLanded>();
            using (_rover.Context.Events.Subscribe<RoverRecovering>(lifts.Add))
            using (_rover.Context.Events.Subscribe<RoverLanded>(landings.Add))
            {
                _rover.Drive.Drive = new Vector2(0f, 1f);
                float pushedAt = -1f;
                float timeout = Time.time + 20f;
                while (lifts.Count == 0 && Time.time < timeout)
                {
                    yield return new WaitForFixedUpdate();
                    bool atWall = TestWorld.Local(controller.Position).z > TestWorld.WallZ - WallReach;
                    if (pushedAt < 0f && controller.Speed < 0.2f && atWall)
                    {
                        pushedAt = Time.time;
                    }
                }

                Assert.AreEqual(1, lifts.Count, "07 pushed into the wall was never helped out.");
                float waited = Time.time - pushedAt;
                Debug.Log($"[rover-stuck] helped {waited:0.00} s after stalling at the wall");
                Assert.That(waited, Is.InRange(recovery.StuckTime - 0.5f, recovery.StuckTime + 1f));

                Vector3 to = lifts[0].To;
                Vector3 local = TestWorld.Local(to);
                Assert.Less(local.z, TestWorld.WallZ - 1f, "Set down on the open side, behind 07.");
                Assert.IsFalse(Physics.CheckSphere(to, controller.SphereRadius, Layers.PropMask), "Not inside rock.");

                float lowestAbove = float.MaxValue;
                float highest = 0f;
                while (controller.IsRecovering)
                {
                    yield return new WaitForFixedUpdate();
                    Vector3 sphere = controller.PhysicsBody.position;
                    highest = Mathf.Max(highest, sphere.y - TestWorld.Height(sphere.x, sphere.z));
                    lowestAbove = Mathf.Min(lowestAbove, sphere.y - TestWorld.Height(sphere.x, sphere.z));
                }

                Assert.Greater(highest, recovery.LiftHeight, "A visible, gentle lift.");
                Assert.GreaterOrEqual(lowestAbove, controller.SphereRadius - 0.05f, "Never dragged through ground.");
                Assert.Less(Vector3.Distance(controller.PhysicsBody.position, to), 0.05f);
                Assert.IsEmpty(landings, "Set down softly: no landing thump.");

                _rover.Drive.Drive = new Vector2(1f, 1f);
                Vector3 before = controller.Position;
                float until = Time.time + 3f;
                while (Time.time < until)
                {
                    yield return null;
                }

                Assert.Greater(Vector3.Distance(before, controller.Position), 3f, "07 drives on afterwards.");
                _rover.Drive.Drive = Vector2.zero;
            }
        }

        [UnityTest]
        public IEnumerator TheRecoveryLift_NeverCountsAsStillness()
        {
            RoverController controller = _rover.Controller;
            IRoverStillness stillness = _rover.Context.Get<IRoverStillness>();
            _rover.Drive.Drive = new Vector2(0f, 1f);
            float timeout = Time.time + 20f;
            while (!controller.IsRecovering && Time.time < timeout)
            {
                yield return null;
            }

            Assert.IsTrue(controller.IsRecovering, "07 pushed into the wall is lifted out.");
            _rover.Drive.Drive = Vector2.zero;
            int frames = 0;
            while (controller.IsRecovering)
            {
                yield return null;
                if (!controller.IsRecovering)
                {
                    break;
                }

                frames++;
                Assert.AreEqual(0f, stillness.StillSeconds, "Hands off the stick, but 07 is being lifted: not still.");
            }

            Assert.Greater(frames, 10, "The lift was observed.");
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                yield return null;
            }

            Assert.Greater(stillness.StillSeconds, 1f, "Set down and left alone, 07 rests again.");
        }
    }
}
