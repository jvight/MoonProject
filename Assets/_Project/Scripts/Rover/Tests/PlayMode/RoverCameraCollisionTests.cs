using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The chase camera never sits inside a solid prop (a habitat module, the lander, a ruin wall), however 07 stands:
    /// with a wall right behind 07 the camera would be inside it, so it must pull in or lift, softly and without a pop.
    /// </summary>
    public sealed class RoverCameraCollisionTests
    {
        private const float BehindWall = 3f;
        private const float SettleSeconds = 1.5f;
        private const float WatchSeconds = 4f;
        private const float InsideProbeRadius = 0.05f;
        private const float MaxStepPerFrame = 0.5f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        [SetUp]
        public void SetUp()
        {
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(TestWorld.WallX, TestWorld.WallZ + BehindWall), 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
        }

        [UnityTest]
        public IEnumerator WithAWallBehind07_TheCameraStaysOutOfItAndNeverPops()
        {
            Transform camera = _rover.Camera.transform;
            float until = Time.time + SettleSeconds;
            while (Time.time < until)
            {
                yield return null;
            }

            Vector3 last = camera.position;
            float worstStep = 0f;
            int insideFrames = 0;
            until = Time.time + WatchSeconds;
            while (Time.time < until)
            {
                yield return null;
                Vector3 now = camera.position;
                worstStep = Mathf.Max(worstStep, Vector3.Distance(last, now));
                last = now;
                if (Physics.CheckSphere(now, InsideProbeRadius, Layers.PropMask, QueryTriggerInteraction.Ignore))
                {
                    insideFrames++;
                }
            }

            Assert.AreEqual(0, insideFrames, "The camera was inside a solid prop.");
            Assert.Less(worstStep, MaxStepPerFrame, "The camera popped instead of easing.");
        }
    }
}
