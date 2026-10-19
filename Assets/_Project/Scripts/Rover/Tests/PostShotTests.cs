using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The wake's camera after the self-test: the wide front three-quarter with all of 07 in frame, the straight eased
    /// pull-back to it from the face close-up, and the eased move round to the chase frame that never swings past it.
    /// </summary>
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

        /// <summary>A face close-up like the inventory's: a metre in front of the face, a little to one side.</summary>
        private static ShotPose Close()
        {
            Vector3 face = Body + Quaternion.Euler(0f, Heading, 0f) * new Vector3(0f, 1.25f, 0.4f);
            Vector3 eye = face + Quaternion.Euler(0f, Heading + 15f, 0f) * Vector3.forward * 1f;
            return new ShotPose(eye, Quaternion.LookRotation(face - eye, Vector3.up), 40f);
        }

        private static ShotPose Chase()
        {
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 position = Body - forward * 4.5f + Vector3.up * 1.8f;
            return new ShotPose(position, Quaternion.LookRotation(Body + Vector3.up - position, Vector3.up), 50f);
        }

        [Test]
        public void TheWideShot_IsInFront_LooksAtTheMiddleOf07_WithAllOfItInFrame()
        {
            ShotPose wide = _shot.Wide(Body, Heading);
            Vector3 forward = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
            Vector3 offset = wide.Position - Body;
            offset.y = 0f;
            Assert.Greater(Vector3.Dot(offset.normalized, forward), 0.5f, "the camera is in front of 07");
            Vector3 look = Body + Vector3.up * _settings.WideLookHeight;
            Assert.Greater(Vector3.Dot(wide.Rotation * Vector3.forward, (look - wide.Position).normalized), 0.9999f,
                "it looks at the middle of 07");
            float halfHeight = _settings.WideDistance * Mathf.Tan(0.5f * wide.Fov * Mathf.Deg2Rad);
            Assert.Greater(halfHeight, _settings.WideLookHeight, "the wheels are in frame");
        }

        [Test]
        public void ThePullBack_EasesInOneStraightLine_FromTheCloseUpToTheWideShot()
        {
            ShotPose close = Close();
            ShotPose wide = _shot.Wide(Body, Heading);
            Assert.AreEqual(0f, Vector3.Distance(PostShot.Glide(close, wide, 0f).Position, close.Position), 1e-4f);
            Assert.AreEqual(0f, Vector3.Distance(PostShot.Glide(close, wide, 1f).Position, wide.Position), 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(PostShot.Glide(close, wide, 1f).Rotation, wide.Rotation), 1e-3f);
            float last = float.MaxValue;
            Vector3 line = (wide.Position - close.Position).normalized;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                ShotPose pose = PostShot.Glide(close, wide, t);
                float left = Vector3.Distance(pose.Position, wide.Position);
                Assert.LessOrEqual(left, last + 1e-5f, "it never backs up");
                Vector3 off = pose.Position - close.Position;
                Assert.Less((off - Vector3.Dot(off, line) * line).magnitude, 1e-3f, "one straight line, no orbit");
                last = left;
            }
        }

        [Test]
        public void ThePullBack_StartsAndEndsAtRest()
        {
            ShotPose close = Close();
            ShotPose wide = _shot.Wide(Body, Heading);
            float Step(float a, float b) =>
                Vector3.Distance(PostShot.Glide(close, wide, a).Position, PostShot.Glide(close, wide, b).Position);
            Assert.Less(Step(0f, 0.05f), Step(0.475f, 0.525f) * 0.2f, "it starts from rest");
            Assert.Less(Step(0.95f, 1f), Step(0.475f, 0.525f) * 0.2f, "and settles to rest");
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
