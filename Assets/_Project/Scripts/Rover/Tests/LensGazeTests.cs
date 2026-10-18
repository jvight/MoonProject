using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The opening's gaze (cutscenes §1.3): soft limits and a heavy, critically damped neck.</summary>
    public sealed class LensGazeTests
    {
        private const float Frame = 1f / 60f;

        private RoverCameraTuning _tuning;
        private OpeningSettings _settings;
        private LensGaze _gaze;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _settings = _tuning.Opening;
            _gaze = new LensGaze(_settings);
            _gaze.Reset(0f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Gaze_FollowsTheAimSlowly_WithoutOvershoot()
        {
            _gaze.Aim(40f, 20f);
            float most = 0f;
            float atHalfSecond = 0f;
            for (float t = 0f; t < 6f; t += Frame)
            {
                _gaze.Step(Frame);
                most = Mathf.Max(most, _gaze.Yaw);
                if (t < 0.5f)
                {
                    atHalfSecond = _gaze.Yaw;
                }
            }

            Assert.Less(atHalfSecond, 25f, "an old neck: well short of the aim after half a second");
            Assert.LessOrEqual(most, 40f + 1e-3f, "critically damped: no overshoot");
            Assert.AreEqual(40f, _gaze.Yaw, 0.1f);
        }

        [Test]
        public void AnglesAndDirection_RoundTrip()
        {
            Vector2 angles = LensGaze.Angles(LensGaze.Direction(-35f, 22f));

            Assert.AreEqual(-35f, angles.x, 1e-3f);
            Assert.AreEqual(22f, angles.y, 1e-3f);
        }
    }
}
