using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class ChassisJellyTests
    {
        private const float Frame = 1f / 60f;
        private RoverRigTuning _tuning;
        private ChassisJelly _jelly;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverRigTuning>();
            _jelly = new ChassisJelly(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Accelerating_SquatsNoseUp_Braking_DipsIt()
        {
            for (int i = 0; i < 120; i++)
            {
                _jelly.Step(new Vector3(0f, 0f, 4f), 0f, Frame);
            }

            Assert.Greater(_jelly.Pitch, 1f);
            Assert.Greater(_jelly.AntennaPitch, 1f, "Antenna lags backwards while speeding up.");

            for (int i = 0; i < 120; i++)
            {
                _jelly.Step(new Vector3(0f, 0f, -4f), 0f, Frame);
            }

            Assert.Less(_jelly.Pitch, -1f);
        }

        [Test]
        public void RightTurn_RollsOutward()
        {
            for (int i = 0; i < 120; i++)
            {
                _jelly.Step(new Vector3(5f, 0f, 0f), 0f, Frame);
            }

            Assert.Greater(_jelly.Roll, 1f, "Right-side-up = leaning left, away from a right turn.");
        }

        [Test]
        public void ReleasingAcceleration_SettlesWithAtMostTwoOvershootsWithinOneAndAHalfSeconds()
        {
            for (int i = 0; i < 180; i++)
            {
                _jelly.Step(new Vector3(0f, 0f, 4.5f), 0f, Frame);
            }

            float start = _jelly.Pitch;
            int signChanges = 0;
            float previous = start;
            float settledAt = -1f;
            for (int i = 1; i <= 240; i++)
            {
                _jelly.Step(Vector3.zero, 0f, Frame);
                float pitch = _jelly.Pitch;
                if (Mathf.Abs(pitch) > 0.05f * Mathf.Abs(start) && Mathf.Sign(pitch) != Mathf.Sign(previous))
                {
                    signChanges++;
                    previous = pitch;
                }

                if (settledAt < 0f && Mathf.Abs(pitch) < 0.05f * Mathf.Abs(start))
                {
                    settledAt = i * Frame;
                }
                else if (Mathf.Abs(pitch) >= 0.05f * Mathf.Abs(start))
                {
                    settledAt = -1f;
                }
            }

            Assert.GreaterOrEqual(signChanges, 1, "The jelly needs one soft overshoot to sell the feel.");
            Assert.LessOrEqual(signChanges, 2);
            Assert.That(settledAt, Is.InRange(0f, 1.5f));
        }

        [Test]
        public void HardLanding_SquashesThenRecovers()
        {
            _jelly.Step(new Vector3(0f, 150f, 0f), 0f, 0.02f);
            float lowest = 0f;
            for (int i = 0; i < 30; i++)
            {
                _jelly.Step(Vector3.zero, 0f, Frame);
                lowest = Mathf.Min(lowest, _jelly.Heave);
            }

            Assert.Less(lowest, -0.03f);
            Assert.GreaterOrEqual(lowest, -_tuning.MaxHeave);
            for (int i = 0; i < 180; i++)
            {
                _jelly.Step(Vector3.zero, 0f, Frame);
            }

            Assert.AreEqual(0f, _jelly.Heave, 0.005f);
        }

        [Test]
        public void ChargingAJump_SinksAndSquatsTheChassisSoftly()
        {
            _jelly.Step(Vector3.zero, 1f, Frame);
            Assert.Greater(_jelly.Heave, -0.02f, "The crouch eases in.");
            for (int i = 0; i < 120; i++)
            {
                _jelly.Step(Vector3.zero, 1f, Frame);
            }

            Assert.AreEqual(-_tuning.CrouchDepth, _jelly.Heave, 0.01f);
            Assert.AreEqual(_tuning.CrouchLean, _jelly.Pitch, 0.2f);
        }

        [Test]
        public void HugeImpacts_StayWithinLimits()
        {
            for (int i = 0; i < 10; i++)
            {
                _jelly.Step(new Vector3(800f, -900f, 700f), 0f, Frame);
                Assert.LessOrEqual(Mathf.Abs(_jelly.Pitch), _tuning.MaxLean);
                Assert.LessOrEqual(Mathf.Abs(_jelly.Roll), _tuning.MaxLean);
                Assert.LessOrEqual(Mathf.Abs(_jelly.Heave), _tuning.MaxHeave);
                Assert.LessOrEqual(Mathf.Abs(_jelly.AntennaPitch), _tuning.AntennaMaxAngle);
            }
        }
    }
}
