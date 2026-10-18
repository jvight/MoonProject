using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The camera moves to a view in which 07's body does not hide the scan target.</summary>
    public sealed class TargetFramingTests
    {
        private const float Frame = 1f / 60f;
        private const float Elevation = 15f;
        private const float Radius = 5f;
        private const float Fov = 50f;
        private const float Aspect = 16f / 9f;
        private const float FollowHeight = 1.2f;
        private const float SettleSeconds = 2f;
        private const float Slack = 0.01f;

        private static readonly Vector3 Rover = Vector3.zero;
        private static readonly Vector3 Follow = new Vector3(0f, FollowHeight, 0f);

        /// <summary>Just past 07's nose, on the crate's lid: the body hides it from straight behind.</summary>
        private static readonly Vector3 Hidden = new Vector3(0f, 0.8f, 2.7f);

        private readonly Vector3[] _targets = new Vector3[1];
        private RoverCameraTuning _tuning;
        private TargetFraming _framing;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _framing = new TargetFraming(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds, Vector3 target, float speed = 0f, bool looking = false, bool hasTarget = true,
            bool mayFrame = true)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _targets[0] = target;
                _framing.Step(new TargetFramingInput(Rover, 0f, _targets, hasTarget ? 1 : 0, Follow, 0f, Elevation,
                    Radius, Fov, Aspect, speed, looking, mayFrame), Frame);
            }
        }

        private Vector3 FramedEye()
        {
            return TargetFraming.EyePosition(Follow + _framing.Shift, _framing.YawOffset, Elevation + _framing.Elevation,
                Radius * _framing.RadiusScale);
        }

        [Test]
        public void TheBodyHidesTheTargetFromStraightBehind_Precondition()
        {
            Vector3 eye = TargetFraming.EyePosition(Follow, 0f, Elevation, Radius);

            Assert.IsFalse(TargetFraming.BodyClear(_tuning, Rover, 0f, eye, Hidden));
        }

        [Test]
        public void StoppedBehindABodyHidingTheTarget_MovesToAViewThatClearsTheBody_WithinTwoSeconds()
        {
            Run(SettleSeconds, Hidden);

            Assert.AreEqual(1f, _framing.Weight, Slack);
            Assert.IsTrue(TargetFraming.BodyClear(_tuning, Rover, 0f, FramedEye(), Hidden),
                "07's body is no longer between the camera and the target");
        }

        [Test]
        public void TheMoveStaysInsideTheTunedLimits()
        {
            Run(SettleSeconds, Hidden);

            Assert.LessOrEqual(Mathf.Abs(_framing.YawOffset), _tuning.FramingYaw + Slack);
            Assert.That(_framing.Elevation, Is.InRange(0f, _tuning.FramingLift + Slack));
            Assert.GreaterOrEqual(_framing.RadiusScale, _tuning.FramingMinRadiusScale - Slack);
        }

        [Test]
        public void ATargetAlreadyInPlainView_MovesNothing()
        {
            Run(SettleSeconds, new Vector3(2.5f, 0.8f, 2.7f));

            Assert.AreEqual(0f, _framing.Elevation, Slack);
            Assert.AreEqual(0f, _framing.YawOffset, Slack);
            Assert.AreEqual(1f, _framing.RadiusScale, Slack);
        }

        [Test]
        public void WaitsForTheEngageDelay()
        {
            Run(_tuning.FramingEngageDelay * 0.5f, Hidden);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void EasesInSoftly_NoSuddenMove()
        {
            Run(_tuning.FramingEngageDelay + 0.1f, Hidden);

            Assert.Less(_framing.Weight, 0.05f);
        }

        [Test]
        public void TargetBeyondRange_IsLeftAlone()
        {
            Run(10f, new Vector3(0f, 0.8f, _tuning.FramingRange + 1f));

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void TargetBehindTheCamera_IsLeftAlone()
        {
            Run(10f, new Vector3(0f, 0.8f, -4f));

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void Driving_NeverFrames()
        {
            Run(10f, Hidden, _tuning.FramingMaxSpeed + 1f);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void NothingToFrame_NeverFrames()
        {
            Run(10f, Hidden, hasTarget: false);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void WhileSomethingElseFramesTheView_NeverFrames()
        {
            Run(10f, Hidden, mayFrame: false);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void LookInput_KeepsItFromStarting()
        {
            Run(10f, Hidden, looking: true);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void LookInput_EasesItBackWithoutAJump()
        {
            Run(10f, Hidden);
            float before = _framing.Weight;
            Run(Frame, Hidden, looking: true);

            Assert.AreEqual(before, _framing.Weight, 0.02f);
            Run(_tuning.FramingReleaseSeconds + 0.1f, Hidden, looking: true);
            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void DrivingOff_EasesBackOverTheReleaseTime()
        {
            Run(10f, Hidden);
            Run(_tuning.FramingReleaseSeconds * 0.5f, Hidden, _tuning.FramingMaxSpeed + 2f);

            Assert.That(_framing.Weight, Is.GreaterThan(0.2f).And.LessThan(0.8f));
            Run(_tuning.FramingReleaseSeconds, Hidden, _tuning.FramingMaxSpeed + 2f);
            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void NoFrameJumpsMoreThanAFewDegreesOfSwing()
        {
            float last = 0f;
            float worst = 0f;
            for (float t = 0f; t < 5f; t += Frame)
            {
                Run(Frame, Hidden);
                worst = Mathf.Max(worst, Mathf.Abs(_framing.YawOffset - last));
                last = _framing.YawOffset;
            }

            Assert.Less(worst, 2f, "the swing eases, it never snaps");
        }
    }
}
