using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Scanning a close target (owner build 2c1ea201d: the beam hid behind 07, and under a dish the view sat inside its
    /// frame): the chase view swings round to look across the beam and rises, draws back for a target overhead, and
    /// settles back behind 07 when the scan ends.
    /// </summary>
    public sealed class RoverScanFramingTests
    {
        private const float Settle = 3.5f;
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

        private float CameraYawFromRover()
        {
            return Mathf.DeltaAngle(Heading(_rover.Chassis.forward), Heading(_rover.Camera.transform.forward));
        }

        [UnityTest]
        public IEnumerator ScanningACloseTarget_TheViewLooksAcrossTheBeam_AndSettlesBackAfter()
        {
            yield return new WaitForSeconds(2f);
            float before = CameraYawFromRover();
            Vector3 target = _rover.Chassis.position + _rover.Chassis.forward * 3f + Vector3.up * 0.6f;
            _rover.Context.Events.Publish(new ScanStarted("test.close", target));
            yield return new WaitForSeconds(Settle);
            float during = CameraYawFromRover();
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(before, during)), _rover.CameraTuning.ScanMaxYawSwing * 0.6f,
                $"the view swung to look across the beam (from {before:0} to {during:0} deg off 07's heading)");
            _rover.Context.Events.Publish(new ScanStopped("test.close", 0.5f));
            yield return new WaitForSeconds(Settle);
            Assert.AreEqual(before, CameraYawFromRover(), 6f, "and it settles back behind 07 when the scan ends");
        }

        [UnityTest]
        public IEnumerator ScanningAFarTarget_TheViewStaysBehind07()
        {
            yield return new WaitForSeconds(2f);
            float before = CameraYawFromRover();
            Vector3 target = _rover.Chassis.position + _rover.Chassis.forward * (_rover.CameraTuning.ScanCloseMetres * 3f);
            _rover.Context.Events.Publish(new ScanStarted("test.far", target));
            yield return new WaitForSeconds(Settle);
            Assert.AreEqual(before, CameraYawFromRover(), 6f, "a far target is framed by the plain chase view");
        }
    }
}
