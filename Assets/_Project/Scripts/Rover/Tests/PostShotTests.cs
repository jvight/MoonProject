using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The wake's one shot: aimed at 07's face from a little low in front, wide on all of 07, then pushed straight in
    /// until the face fills its tuned share of the frame; it never turns or zooms.
    /// </summary>
    public sealed class PostShotTests
    {
        private static readonly Vector3 Face = new Vector3(4f, 3.3f, -3f);
        private const float Heading = 40f;
        private const float FaceHeight = 0.37f;

        private PostSettings _settings;
        private PostShot _shot;

        [SetUp]
        public void SetUp()
        {
            _settings = new PostSettings();
            _shot = new PostShot(_settings, FaceHeight);
        }

        [Test]
        public void TheShot_IsInFrontOfTheRover_AndLooksAtTheFace()
        {
            ShotPose pose = _shot.At(Face, Heading, 0f);
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 offset = pose.Position - Face;
            offset.y = 0f;
            Assert.Greater(Vector3.Dot(offset.normalized, forward), 0.5f, "the camera is in front, where the CRT face is");
            Assert.Greater(Vector3.Dot(pose.Rotation * Vector3.forward, (Face - pose.Position).normalized), 0.9999f,
                "and looks straight at the face");
        }

        [Test]
        public void TheShot_IsALittleLow_LookingSoftlyUp()
        {
            ShotPose pose = _shot.At(Face, Heading, 0f);
            Assert.Less(pose.Position.y, Face.y, "the camera sits below the face");
            float pitch = Mathf.Asin((pose.Rotation * Vector3.forward).y) * Mathf.Rad2Deg;
            Assert.That(pitch, Is.InRange(0f, 15f), "and tilts up only a little");
        }

        [Test]
        public void WideAtTheStart_AllOf07IsInFrame()
        {
            ShotPose pose = _shot.At(Face, Heading, 0f);
            float halfHeight = Vector3.Distance(pose.Position, Face) * Mathf.Tan(0.5f * pose.Fov * Mathf.Deg2Rad);
            const float FaceAboveGround = 1.3f;
            Assert.Greater(halfHeight, FaceAboveGround, "07's wheels are in frame below its face");
        }

        [Test]
        public void ThePushIn_NeverTurnsOrZooms_AndEndsWithTheFaceFillingItsShare()
        {
            ShotPose start = _shot.At(Face, Heading, 0f);
            ShotPose end = _shot.At(Face, Heading, 1f);
            Assert.AreEqual(0f, Quaternion.Angle(start.Rotation, end.Rotation), 1e-3f, "one shot: no pan, no orbit");
            Assert.AreEqual(start.Fov, end.Fov, 1e-5f, "no zoom");
            Vector3 along = (end.Position - start.Position).normalized;
            Assert.Greater(Vector3.Dot(along, start.Rotation * Vector3.forward), 0.9999f, "straight along the view");
            float frameHeight = 2f * Vector3.Distance(end.Position, Face) * Mathf.Tan(0.5f * end.Fov * Mathf.Deg2Rad);
            Assert.AreEqual(_settings.FaceFill, FaceHeight / frameHeight, 1e-3f, "the face fills its tuned share");
            Assert.That(_settings.FaceFill, Is.InRange(0.35f, 0.4f), "35-40 % of the frame's height");
        }

        [Test]
        public void ThePushIn_EasesInAndOut()
        {
            float Step(float a, float b) =>
                Vector3.Distance(_shot.At(Face, Heading, a).Position, _shot.At(Face, Heading, b).Position);
            Assert.Less(Step(0f, 0.05f), Step(0.475f, 0.525f) * 0.2f, "it starts from rest");
            Assert.Less(Step(0.95f, 1f), Step(0.475f, 0.525f) * 0.2f, "and settles to rest");
            Assert.AreEqual(0f, Step(1f, 2f), 1e-5f, "past the end it holds");
        }

        [Test]
        public void TheFaceAngles_PointFromTheFaceIntoTheCamera()
        {
            Vector2 angles = _shot.FaceAngles(Heading);
            Vector3 direction = Quaternion.Euler(0f, Heading, 0f) * LensGaze.Direction(angles.x, angles.y);
            Vector3 toCamera = (_shot.At(Face, Heading, 0f).Position - Face).normalized;
            Assert.Greater(Vector3.Dot(direction, toCamera), 0.999f);
            Assert.AreEqual(_settings.ShotSideYaw, angles.x, 1e-3f);
            Assert.AreEqual(_settings.ShotElevation, angles.y, 1e-3f);
        }

        private static readonly Vector3 Body = new Vector3(4f, 2f, -3f);

        private static ShotPose Chase()
        {
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 position = Body - forward * 4.5f + Vector3.up * 1.8f;
            return new ShotPose(position, Quaternion.LookRotation(Body + Vector3.up - position, Vector3.up), 50f);
        }

        [Test]
        public void TheWideShot_IsFurtherRoundAndBack_WithAllOf07InFrame()
        {
            ShotPose wide = _shot.Wide(Body, Heading);
            Vector3 look = Body + Vector3.up * _settings.WideLookHeight;
            Assert.Greater(Vector3.Dot(wide.Rotation * Vector3.forward, (look - wide.Position).normalized), 0.9999f,
                "it looks at the middle of 07");
            float halfHeight = _settings.WideDistance * Mathf.Tan(0.5f * wide.Fov * Mathf.Deg2Rad);
            Assert.Greater(halfHeight, _settings.WideLookHeight, "the wheels are in frame");
            Assert.Greater(_settings.WideSideYaw, _settings.ShotSideYaw, "the pull-back moves out diagonally");
        }

        [Test]
        public void ThePullBack_EasesInOneStraightLine_FromTheCloseUpToTheWideShot()
        {
            ShotPose close = _shot.At(Face, Heading, 1f);
            ShotPose wide = _shot.Wide(Body, Heading);
            Assert.AreEqual(0f, Vector3.Distance(PostShot.PullBack(close, wide, 0f).Position, close.Position), 1e-4f);
            Assert.AreEqual(0f, Vector3.Distance(PostShot.PullBack(close, wide, 1f).Position, wide.Position), 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(PostShot.PullBack(close, wide, 1f).Rotation, wide.Rotation), 1e-3f);
            float last = float.MaxValue;
            Vector3 line = (wide.Position - close.Position).normalized;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                ShotPose pose = PostShot.PullBack(close, wide, t);
                float left = Vector3.Distance(pose.Position, wide.Position);
                Assert.LessOrEqual(left, last + 1e-5f, "it never backs up");
                Vector3 off = pose.Position - close.Position;
                Assert.Less((off - Vector3.Dot(off, line) * line).magnitude, 1e-3f, "one straight line, no orbit");
                last = left;
            }
        }

        [Test]
        public void TheMoveBehind_GoesRoundTheShortWay_NeverPastTheChaseFrame_AndEndsOnIt()
        {
            ShotPose wide = _shot.Wide(Body, Heading);
            ShotPose chase = Chase();
            ShotPose start = PostShot.Round(wide, chase, Body, 0f);
            ShotPose end = PostShot.Round(wide, chase, Body, 1f);
            Assert.AreEqual(0f, Vector3.Distance(start.Position, wide.Position), 1e-3f, "it starts on the wide shot");
            Assert.AreEqual(0f, Vector3.Distance(end.Position, chase.Position), 1e-3f, "and ends on the chase frame");
            Assert.AreEqual(0f, Quaternion.Angle(end.Rotation, chase.Rotation), 1e-3f);

            float Bearing(Vector3 p) => Mathf.Atan2(p.x - Body.x, p.z - Body.z) * Mathf.Rad2Deg;
            float total = Mathf.DeltaAngle(Bearing(wide.Position), Bearing(chase.Position));
            Assert.LessOrEqual(Mathf.Abs(total), 180f, "the short way round");
            float lastLeft = Mathf.Abs(total);
            float lastAngle = 0f;
            for (float t = 0.02f; t <= 1.0001f; t += 0.02f)
            {
                ShotPose pose = PostShot.Round(wide, chase, Body, t);
                float left = Mathf.Abs(Mathf.DeltaAngle(Bearing(pose.Position), Bearing(chase.Position)));
                Assert.LessOrEqual(left, lastLeft + 1e-3f, "it only ever closes on the chase bearing, never past it");
                Vector3 toBody = (Body + Vector3.up - pose.Position).normalized;
                Assert.Greater(Vector3.Dot(pose.Rotation * Vector3.forward, toBody), 0.8f, "07 stays in view");
                float step = Quaternion.Angle(PostShot.Round(wide, chase, Body, t - 0.02f).Rotation, pose.Rotation);
                Assert.Less(step, 12f, "the turn is gentle, never a whip");
                lastLeft = left;
                lastAngle = step;
            }

            Assert.Less(lastAngle, 1f, "it settles onto the chase frame");
        }
    }
}
