using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The lab camera's poses (docs/features/M3-19 §6): the ring camera opposite the way 07 faces and over its
    /// shoulder, inside the cabin's walls; the crane path from the chase view through its waypoints to the ring.
    /// </summary>
    public sealed class LabShotTests
    {
        private const float CabinRadius = 2.8f;
        private const float RingRadius = 2.4f;

        private readonly LabCameraSettings _settings = new LabCameraSettings();

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(217f)]
        public void TheRingCamera_StandsBehind07OverItsShoulder_UnderTheRail_InsideTheWalls(float facing)
        {
            var axis = new Vector3(5f, 3.6f, -2f);
            float rail = axis.y + 2.3f;
            Vector3 camera = LabShot.RingPosition(axis, rail, RingRadius, facing, _settings);
            Vector3 flat = new Vector3(camera.x - axis.x, 0f, camera.z - axis.z);
            Assert.AreEqual(RingRadius, flat.magnitude, 1e-4f, "on the ring");
            Assert.Less(flat.magnitude + _settings.NearClip, CabinRadius, "never through the wall");
            Assert.AreEqual(rail - _settings.RailDrop, camera.y, 1e-4f, "hanging just under the rail");
            float behind = Vector3.Angle(flat, -LabShot.Direction(facing));
            Assert.AreEqual(_settings.Shoulder, behind, 0.01f, "opposite the station 07 faces, over its shoulder");

            Vector3 aim = LabShot.RingAim(axis, axis.y + 1.25f, 2.4f, facing, _settings);
            Vector3 ahead = new Vector3(aim.x - axis.x, 0f, aim.z - axis.z);
            Assert.Less(Vector3.Angle(ahead, LabShot.Direction(facing)), 0.01f, "looking toward the station");
        }

        [Test]
        public void TheCranePath_LeavesTheChaseView_PassesItsWaypoints_AndEndsOnTheRing()
        {
            var chase = new Vector3(0f, 2f, 9f);
            var under = new Vector3(0f, 1.3f, 2.4f);
            var ring = new Vector3(0.5f, 5.7f, -2.3f);
            Vector3 entry = LabShot.Shaft(Vector3.zero, 2.3f, ring, _settings);
            Vector3 floor = LabShot.Shaft(Vector3.zero, 4.2f, ring, _settings);
            Assert.AreEqual(_settings.ShaftOut, new Vector3(entry.x, 0f, entry.z).magnitude, 1e-4f,
                "it rises through the shaft's gap, beside the platform");
            Assert.Less(Vector3.Distance(chase, LabShot.Crane(chase, under, entry, floor, ring, 0f)), 1e-4f,
                "the crane starts exactly where the chase camera stood");
            Assert.Less(Vector3.Distance(under, LabShot.Crane(chase, under, entry, floor, ring, 0.25f)), 1e-4f);
            Assert.Less(Vector3.Distance(entry, LabShot.Crane(chase, under, entry, floor, ring, 0.5f)), 1e-4f);
            Assert.Less(Vector3.Distance(floor, LabShot.Crane(chase, under, entry, floor, ring, 0.75f)), 1e-4f);
            Assert.Less(Vector3.Distance(ring, LabShot.Crane(chase, under, entry, floor, ring, 1f)), 1e-4f);
            Vector3 previous = chase;
            for (int i = 1; i <= 60; i++)
            {
                Vector3 next = LabShot.Crane(chase, under, entry, floor, ring, i / 60f);
                Assert.Less(Vector3.Distance(previous, next), 0.6f, "a smooth path, no jumps");
                previous = next;
            }

            Vector3 below = LabShot.UnderBelly(Vector3.zero, chase, _settings);
            Assert.AreEqual(_settings.UnderBellyHeight, below.y, 1e-4f);
            Assert.Greater(below.z, 0f, "under the belly on the chase view's side");
        }
    }
}
