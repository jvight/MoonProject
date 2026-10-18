using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The camera lifts and slides aside to show a scan target 07's body would hide.</summary>
    public sealed class TargetFramingTests
    {
        private const float Frame = 1f / 60f;
        private static readonly Vector3 Rover = Vector3.zero;

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
            bool mayFrame = true, float viewYaw = 0f)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _framing.Step(
                    new TargetFramingInput(hasTarget, Rover, target, viewYaw, speed, looking, mayFrame), Frame);
            }
        }

        [Test]
        public void StoppedNearATargetAhead_LiftsToFullAfterTheRamp()
        {
            Run(_tuning.FramingEngageDelay + _tuning.FramingRampSeconds + 0.1f, new Vector3(0f, 0f, 4f));

            Assert.AreEqual(1f, _framing.Weight, 0.001f);
            Assert.AreEqual(_tuning.FramingLift, _framing.Elevation, 0.01f);
        }

        [Test]
        public void WaitsForTheEngageDelay()
        {
            Run(_tuning.FramingEngageDelay * 0.5f, new Vector3(0f, 0f, 4f));

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void EasesInSlowly_NoSuddenLift()
        {
            Run(_tuning.FramingEngageDelay + 0.5f, new Vector3(0f, 0f, 4f));

            Assert.Less(_framing.Weight, 0.1f);
        }

        [Test]
        public void TargetBeyondRange_IsLeftAlone()
        {
            Run(10f, new Vector3(0f, 0f, _tuning.FramingRange + 1f));

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void TargetBehindTheCamera_IsLeftAlone()
        {
            Run(10f, new Vector3(0f, 0f, -4f));

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void Driving_NeverFrames()
        {
            Run(10f, new Vector3(0f, 0f, 4f), _tuning.FramingMaxSpeed + 1f);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void NothingToFrame_NeverFrames()
        {
            Run(10f, new Vector3(0f, 0f, 4f), hasTarget: false);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void WhileSomethingElseFramesTheView_NeverFrames()
        {
            Run(10f, new Vector3(0f, 0f, 4f), mayFrame: false);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void LookInput_KeepsItFromStarting()
        {
            Run(10f, new Vector3(0f, 0f, 4f), looking: true);

            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void LookInput_EasesItBackWithoutAJump()
        {
            Run(10f, new Vector3(0f, 0f, 4f));
            float before = _framing.Weight;
            Run(Frame, new Vector3(0f, 0f, 4f), looking: true);

            Assert.AreEqual(before, _framing.Weight, 0.02f);
            Run(_tuning.FramingReleaseSeconds + 0.1f, new Vector3(0f, 0f, 4f), looking: true);
            Assert.AreEqual(0f, _framing.Weight);
        }

        [Test]
        public void DrivingOff_EasesBackOverTheReleaseTime()
        {
            Run(10f, new Vector3(0f, 0f, 4f));
            Run(_tuning.FramingReleaseSeconds * 0.5f, new Vector3(0f, 0f, 4f), _tuning.FramingMaxSpeed + 2f);

            Assert.That(_framing.Weight, Is.GreaterThan(0.2f).And.LessThan(0.8f));
            Run(_tuning.FramingReleaseSeconds, new Vector3(0f, 0f, 4f), _tuning.FramingMaxSpeed + 2f);
            Assert.AreEqual(0f, _framing.Weight);
        }

        [TestCase(5f, 1f)]
        [TestCase(-5f, -1f)]
        public void SlidesTowardTheTargetsSide(float x, float side)
        {
            Run(10f, new Vector3(x, 0f, 2.5f));

            Assert.AreEqual(side * _tuning.FramingYaw, _framing.YawOffset, 0.01f);
        }

        [Test]
        public void TargetStraightAhead_AddsNoSideShift()
        {
            Run(10f, new Vector3(0f, 0f, 4f));

            Assert.AreEqual(0f, _framing.YawOffset, 0.01f);
        }
    }
}
