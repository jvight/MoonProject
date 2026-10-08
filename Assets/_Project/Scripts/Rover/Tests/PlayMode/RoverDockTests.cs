using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Resting on the charging dock (M3-14, real wiring): docked, 07 eases from where it stopped onto the dock's
    /// anchor in about a second, never a snap, and its road light dims; a touch of the stick shows in its drive input
    /// at once (gameplay undocks on it), and undocked it is free and driving straight away while its light comes back.
    /// </summary>
    public sealed class RoverDockTests : InputTestFixture
    {
        private const float AnchorYaw = 14f;

        /// <summary>Two physics steps after a full push, the eased drive input is well past any dead zone.</summary>
        private const float TouchShows = 0.2f;

        /// <summary>Settling onto the dock is a slow ease (m/s, deg/s at most).</summary>
        private const float MaxSettleSpeed = 1.5f;
        private const float MaxSettleTurn = 40f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -200f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Docked_07EasesOntoTheAnchor_DimsItsLamp_AndATouchLetsItGoAtOnce()
        {
            yield return Wait(1f);
            RoverController controller = _rover.Controller;
            Vector3 anchor = controller.Position + new Vector3(0.5f, 0f, 0.4f);
            Quaternion facing = Quaternion.Euler(0f, AnchorYaw, 0f);
            _rover.Context.Events.Publish(new RoverDockChanged(true, anchor, facing));
            Assert.IsTrue(controller.IsDocked);

            var trace = new CameraTrace(_rover.Rig.transform);
            float started = Time.time;
            float settledAt = -1f;
            while (Time.time - started < 2.5f)
            {
                yield return null;
                trace.Step(0f);
                bool there = Vector3.Distance(controller.Position, anchor) < 0.01f
                    && Mathf.Abs(Mathf.DeltaAngle(controller.Heading, AnchorYaw)) < 0.5f;
                if (settledAt < 0f && there)
                {
                    settledAt = Time.time - started;
                }
            }

            DockSettings dock = _rover.Tuning.Dock;
            Debug.Log($"[rover-dock] settled on the anchor after {settledAt:0.00} s ({trace}); lamp at "
                + $"{_rover.Kit.LampLevel:0.00}");
            Assert.That(settledAt, Is.InRange(0.6f * dock.SettleSeconds, 1.3f * dock.SettleSeconds),
                "About a second onto the anchor.");
            Assert.Less(trace.Fastest, MaxSettleSpeed, "Eased, never a snap.");
            Assert.Less(trace.FastestTurn, MaxSettleTurn);
            Assert.AreEqual(dock.LampDim, _rover.Kit.LampLevel, 0.05f, "The road light dims.");
            float dimmed = _rover.Headlamp.intensity;

            _rover.Drive.Drive = new Vector2(0f, 1f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(controller.DriveInput.y, TouchShows, "A touch shows in the drive input at once...");
            Assert.Less(Vector3.Distance(controller.Position, anchor), 0.01f, "...while the dock still holds 07.");

            _rover.Context.Events.Publish(new RoverDockChanged(false, anchor, facing));
            Assert.IsFalse(controller.IsDocked, "Undocked: free at once.");
            yield return Wait(0.6f);
            _rover.Drive.Drive = Vector2.zero;
            Assert.Greater(Vector3.Distance(controller.Position, anchor), 0.1f, "Driving away straight off.");
            Assert.Greater(_rover.Headlamp.intensity, dimmed, "The road light comes back up.");
        }
    }
}
