using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's "where am I" look around after a radio-hop: left, right, settle, smoothly.</summary>
    public sealed class LookAroundTests
    {
        private const float Frame = 1f / 60f;

        private RoverCharacterTuning _tuning;
        private LookAround _look;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            _look = new LookAround(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Offset_TurnsLeftFirst_ThenRight_ThenSettles()
        {
            float leftAt = -1f;
            float rightAt = -1f;
            float left = 0f;
            float right = 0f;
            for (float t = 0f; t <= 1f; t += 0.001f)
            {
                float yaw = LookAround.Offset(_tuning, t).x;
                if (yaw < left)
                {
                    left = yaw;
                    leftAt = t;
                }

                if (yaw > right)
                {
                    right = yaw;
                    rightAt = t;
                }
            }

            Assert.AreEqual(-_tuning.LookAroundYaw, left, 0.01f, "Left as far as tuned.");
            Assert.AreEqual(_tuning.LookAroundYaw, right, 0.01f, "Then right as far.");
            Assert.Less(leftAt, rightAt, "Left first.");
            Assert.AreEqual(Vector2.zero, LookAround.Offset(_tuning, 0f));
            Assert.AreEqual(0f, LookAround.Offset(_tuning, 1f).x, 1e-3f, "Settles ahead.");
            Assert.AreEqual(_tuning.LookAroundLift, LookAround.Offset(_tuning, 0.5f).y, 1e-3f, "Scans the horizon.");
        }

        [Test]
        public void Offset_IsSmooth_NeverAJump()
        {
            Vector2 previous = LookAround.Offset(_tuning, 0f);
            float step = Frame / _tuning.LookAroundSeconds;
            float largest = 0f;
            for (float t = step; t <= 1f; t += step)
            {
                Vector2 next = LookAround.Offset(_tuning, t);
                largest = Mathf.Max(largest, Vector2.Distance(previous, next));
                previous = next;
            }

            float meanPerFrame = 4f * _tuning.LookAroundYaw / (_tuning.LookAroundSeconds / Frame);
            Assert.Less(largest, 4f * meanPerFrame, $"Largest change {largest:0.00} deg in a frame.");
        }

        [Test]
        public void Step_WaitsForTheDelay_ThenEndsOnItsOwn()
        {
            Assert.IsFalse(_look.IsActive);
            _look.Start();
            for (float t = 0f; t < _tuning.LookAroundDelay - Frame; t += Frame)
            {
                _look.Step(Frame);
                Assert.AreEqual(0f, _look.Aim.x, 1e-4f, "Still easing in: the head holds ahead.");
            }

            float largest = 0f;
            for (float t = 0f; t < _tuning.LookAroundSeconds + 2f * Frame; t += Frame)
            {
                _look.Step(Frame);
                largest = Mathf.Max(largest, Mathf.Abs(_look.Aim.x));
            }

            Assert.Greater(largest, 0.9f * _tuning.LookAroundYaw, "It looked around.");
            Assert.IsFalse(_look.IsActive, "And it is over.");
            Assert.AreEqual(Vector2.zero, _look.Aim);
        }
    }
}
