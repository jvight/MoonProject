using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>The self-test's camera: in front of 07, near, eased between parts, round to the beacon at the end.</summary>
    public sealed class PostShotTests
    {
        private static readonly Vector3 Body = new Vector3(4f, 2f, -3f);
        private const float Heading = 40f;

        private PostSettings _settings;
        private PostShot _shot;

        [SetUp]
        public void SetUp()
        {
            _settings = new PostSettings();
            _shot = new PostShot(_settings);
        }

        [Test]
        public void TheBaseShot_IsInFrontOfTheRover_AndLooksBackAtIt()
        {
            ShotPose pose = _shot.Base(Body, Heading);
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 offset = pose.Position - Body;
            offset.y = 0f;
            Assert.Greater(Vector3.Dot(offset.normalized, forward), 0.5f, "the camera is in front, where the CRT face is");
            Assert.Less(Vector3.Dot(pose.Rotation * Vector3.forward, forward), -0.5f, "and looks back at 07");
        }

        [Test]
        public void TheShot_FollowsTheRoverWherever_ItIsAndWhichWayItFaces()
        {
            ShotPose a = _shot.At(Body, Heading, BootCheckStep.Wheels, 0.5f);
            Vector3 shift = new Vector3(10f, 0f, 7f);
            ShotPose b = _shot.At(Body + shift, Heading, BootCheckStep.Wheels, 0.5f);
            Assert.AreEqual(0f, Vector3.Distance(a.Position + shift, b.Position), 1e-4f);
            ShotPose turned = _shot.At(Body, Heading + 90f, BootCheckStep.Wheels, 0.5f);
            Assert.AreEqual(Vector3.Distance(a.Position, Body), Vector3.Distance(turned.Position, Body), 1e-3f,
                "turning 07 turns the camera round it at the same distance");
        }

        [Test]
        public void TheFocus_EasesFromThePreviousPart_AndThenStaysPut()
        {
            ShotPose early = _shot.At(Body, Heading, BootCheckStep.Cutter, 0f);
            ShotPose previous = _shot.At(Body, Heading, BootCheckStep.Motor, 1f);
            Assert.AreEqual(0f, Vector3.Distance(early.Position, previous.Position), 1e-3f, "checks join without a jump");
            ShotPose settled = _shot.At(Body, Heading, BootCheckStep.Cutter, 0.5f);
            ShotPose later = _shot.At(Body, Heading, BootCheckStep.Cutter, 0.9f);
            Assert.AreEqual(0f, Vector3.Distance(settled.Position, later.Position), 1e-3f);
        }

        [Test]
        public void TheCamera_NeverGetsCloserThanTheCloseShot_OrFartherThanTheFullBodyShot()
        {
            for (int step = (int)BootCheckStep.Lens; step <= (int)BootCheckStep.Begin; step++)
            {
                for (float p = 0f; p <= 1f; p += 0.25f)
                {
                    ShotPose pose = _shot.At(Body, Heading, (BootCheckStep)step, p);
                    float distance = Vector3.Distance(pose.Position, Body);
                    Assert.Greater(distance, 0.8f, $"{(BootCheckStep)step} at {p}: not inside the hull");
                    Assert.Less(distance, 6f, $"{(BootCheckStep)step} at {p}: not lost");
                }
            }
        }

        [Test]
        public void TheOverShoulderShot_LooksAtTheBeacon()
        {
            var beacon = new Vector3(30f, 3f, 20f);
            ShotPose pose = _shot.OverShoulder(Body, Heading, beacon);
            Vector3 toBeacon = (beacon - pose.Position).normalized;
            Assert.Greater(Vector3.Dot(pose.Rotation * Vector3.forward, toBeacon), 0.999f);
        }

        [Test]
        public void Blend_StartsAtOnePose_AndEndsAtTheOther()
        {
            ShotPose a = _shot.Base(Body, Heading);
            ShotPose b = _shot.OverShoulder(Body, Heading, new Vector3(30f, 3f, 20f));
            Assert.AreEqual(0f, Vector3.Distance(PostShot.Blend(a, b, 0f).Position, a.Position), 1e-4f);
            Assert.AreEqual(0f, Vector3.Distance(PostShot.Blend(a, b, 1f).Position, b.Position), 1e-4f);
        }
    }
}
