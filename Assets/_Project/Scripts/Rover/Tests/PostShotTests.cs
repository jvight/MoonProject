using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The wake's one shot: in front of 07's face, a little low, fixed but for a slow push-in of a few percent.</summary>
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
        public void TheShot_IsInFrontOfTheRover_AndLooksBackAtIt()
        {
            ShotPose pose = _shot.At(Body, Heading, 0f);
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 offset = pose.Position - Body;
            offset.y = 0f;
            Assert.Greater(Vector3.Dot(offset.normalized, forward), 0.5f, "the camera is in front, where the CRT face is");
            Assert.Less(Vector3.Dot(pose.Rotation * Vector3.forward, forward), -0.5f, "and looks back at 07");
        }

        [Test]
        public void TheShot_IsALittleLow_LookingSoftlyUpAtTheFace()
        {
            ShotPose pose = _shot.At(Body, Heading, 0f);
            Assert.Less(pose.Position.y, Body.y + _settings.ShotLook.y, "the camera sits below the point it frames");
            float pitch = Mathf.Asin((pose.Rotation * Vector3.forward).y) * Mathf.Rad2Deg;
            Assert.That(pitch, Is.InRange(0f, 10f), "and tilts up only a little");
        }

        [Test]
        public void TheShot_NeverTurns_AndOnlyCreepsInAFewPercent()
        {
            ShotPose start = _shot.At(Body, Heading, 0f);
            ShotPose end = _shot.At(Body, Heading, 1f);
            Assert.AreEqual(0f, Quaternion.Angle(start.Rotation, end.Rotation), 1e-3f, "one shot: no pan, no orbit");
            Assert.AreEqual(start.Fov, end.Fov, 1e-5f, "no zoom");
            Vector3 look = Body + Quaternion.Euler(0f, Heading, 0f) * _settings.ShotLook;
            float startDistance = Vector3.Distance(start.Position, look);
            float creep = startDistance - Vector3.Distance(end.Position, look);
            Assert.AreEqual(_settings.PushIn * startDistance, creep, 1e-3f, "the push-in is its tuned share");
            Assert.LessOrEqual(_settings.PushIn, 0.1f, "a few percent at most");
            Vector3 along = (end.Position - start.Position).normalized;
            Assert.Greater(Vector3.Dot(along, start.Rotation * Vector3.forward), 0.999f, "straight along the view");
        }

        [Test]
        public void ThePushIn_EasesInAndOut()
        {
            float Step(float a, float b) => Vector3.Distance(_shot.At(Body, Heading, a).Position, _shot.At(Body, Heading, b).Position);
            Assert.Less(Step(0f, 0.05f), Step(0.475f, 0.525f) * 0.2f, "it starts from rest");
            Assert.Less(Step(0.95f, 1f), Step(0.475f, 0.525f) * 0.2f, "and settles to rest");
            Assert.AreEqual(0f, Step(1f, 2f), 1e-5f, "past the end it holds");
        }

        [Test]
        public void TheShot_FollowsTheRoverWhereverItIsAndWhichWayItFaces()
        {
            ShotPose a = _shot.At(Body, Heading, 0.5f);
            Vector3 shift = new Vector3(10f, 0f, 7f);
            ShotPose b = _shot.At(Body + shift, Heading, 0.5f);
            Assert.AreEqual(0f, Vector3.Distance(a.Position + shift, b.Position), 1e-4f);
            ShotPose turned = _shot.At(Body, Heading + 90f, 0.5f);
            Assert.AreEqual(Vector3.Distance(a.Position, Body), Vector3.Distance(turned.Position, Body), 1e-3f,
                "turning 07 turns the camera round it at the same distance");
        }

        [Test]
        public void TheFaceAngles_PointFromTheFaceIntoTheCamera()
        {
            Vector3 face = Body + Quaternion.Euler(0f, Heading, 0f) * new Vector3(0f, 1.2f, 0.3f);
            Vector2 angles = _shot.FaceAngles(Body, Heading, face);
            Vector3 direction = Quaternion.Euler(0f, Heading, 0f) * LensGaze.Direction(angles.x, angles.y);
            Vector3 toCamera = (_shot.At(Body, Heading, 0f).Position - face).normalized;
            Assert.Greater(Vector3.Dot(direction, toCamera), 0.999f);
            Assert.AreEqual(_settings.ShotSideYaw, angles.x, 5f, "the head turns about as far as the shot stands round");
        }
    }
}
