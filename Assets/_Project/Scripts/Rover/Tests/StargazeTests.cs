using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>Stargazing: 07 slowly joins the look up, the beat waits for rest, and driving ends it gently.</summary>
    public sealed class StargazeTests
    {
        private const float Step = 1f / 60f;

        private RoverCameraTuning _tuning;
        private Stargaze _stargaze;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _stargaze = new Stargaze(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        /// <summary>Runs <paramref name="seconds"/>; returns how many times the beat switched.</summary>
        private int Run(float seconds, float elevation, bool moving = false, bool free = true, float stillAtStart = 0f)
        {
            int switches = 0;
            for (float t = 0f; t < seconds; t += Step)
            {
                float still = moving ? 0f : stillAtStart + t;
                switches += _stargaze.Step(elevation, still, moving, free, Step) ? 1 : 0;
            }

            return switches;
        }

        [Test]
        public void LevelView_NoGazeAndNoBeat()
        {
            Assert.AreEqual(0, Run(10f, _tuning.DefaultPitch));
            Assert.AreEqual(0f, _stargaze.Lift, 1e-4f);
            Assert.IsFalse(_stargaze.IsActive);
        }

        [Test]
        public void LookingUp_07LiftsSlowly_AndTheBeatWaitsForRest()
        {
            Assert.AreEqual(0, Run(1f, _tuning.StargazeFullElevation));
            Assert.That(_stargaze.Lift, Is.InRange(0.2f, 0.6f), "Eased in, not snapped.");
            Assert.IsFalse(_stargaze.IsActive, "Not yet: the beat waits for rest.");

            Assert.AreEqual(1, Run(_tuning.StargazeDelay, _tuning.StargazeFullElevation, stillAtStart: 1f));
            Assert.IsTrue(_stargaze.IsActive);
            Assert.Greater(_stargaze.Lift, 0.8f);
        }

        [Test]
        public void NudgingTheViewDown_KeepsTheBeat_DrivingEndsIt()
        {
            Run(_tuning.StargazeDelay + 1f, _tuning.StargazeFullElevation);
            Assert.IsTrue(_stargaze.IsActive);

            float nudged = Mathf.Lerp(_tuning.StargazeStartElevation, _tuning.StargazeFullElevation, 0.45f);
            Assert.AreEqual(0, Run(1f, nudged, stillAtStart: 0f));
            Assert.IsTrue(_stargaze.IsActive, "Between hold and begin shares, the beat holds.");

            Assert.AreEqual(1, Run(0.1f, _tuning.StargazeFullElevation, moving: true));
            Assert.IsFalse(_stargaze.IsActive);
            Assert.Greater(_stargaze.Lift, 0.3f, "07's head eases down, it does not drop.");
            Run(2f, _tuning.StargazeFullElevation, moving: true);
            Assert.Less(_stargaze.Lift, 0.05f);
        }

        [Test]
        public void CameraNotThePlayers_NoGaze()
        {
            Assert.AreEqual(0, Run(10f, _tuning.StargazeFullElevation, free: false));
            Assert.AreEqual(0f, _stargaze.Lift, 1e-4f);
        }
    }
}
