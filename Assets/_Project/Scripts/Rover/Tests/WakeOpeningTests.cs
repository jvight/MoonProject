using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The fresh game's opening: the lens beat holds, the camera turns round to Earth, then eases to the chase frame
    /// softly, sooner on any input.
    /// </summary>
    public sealed class WakeOpeningTests
    {
        private const float Step = 1f / 60f;

        private RoverCameraTuning _tuning;
        private WakeOpening _opening;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _opening = new WakeOpening(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds, bool input = false)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _opening.Step(input, Step);
            }
        }

        [Test]
        public void NotBegun_IsIdle()
        {
            Run(1f, true);
            Assert.IsFalse(_opening.IsActive);
            Assert.AreEqual(0f, _opening.Weight);
        }

        [Test]
        public void TheLensBeatHolds_ThenTheCameraTurnsToEarth()
        {
            _opening.Begin();
            Run(_tuning.WakeLensSeconds - 0.1f);
            Assert.AreEqual(0f, _opening.Turn, "Beat 1: on the lens.");

            float previous = 0f;
            for (float t = 0f; t < _tuning.WakeTurnSeconds; t += Step)
            {
                _opening.Step(false, Step);
                Assert.GreaterOrEqual(_opening.Turn, previous, "Turns one way only.");
                Assert.Less(_opening.Turn - previous, 0.02f, "No jump.");
                previous = _opening.Turn;
            }

            Run(0.2f);
            Assert.AreEqual(1f, _opening.Turn, 1e-4f, "Beat 2: over the shoulder at Earth.");
            Assert.IsFalse(_opening.IsEasing, "Still holding.");
            Assert.AreEqual(1f, _opening.Weight);
        }

        [Test]
        public void WithoutInput_HoldsThenEasesOutOverTheEaseTime()
        {
            _opening.Begin();
            Run(_tuning.WakeHoldSeconds - 0.5f);
            Assert.IsFalse(_opening.IsEasing);
            Run(0.5f + Step);
            Assert.IsTrue(_opening.IsEasing, "The hold ran out.");

            float previous = _opening.Weight;
            for (float t = 0f; t < _tuning.WakeEaseSeconds - 2f * Step; t += Step)
            {
                _opening.Step(false, Step);
                Assert.LessOrEqual(_opening.Weight, previous, "Only ever eases toward the chase frame.");
                Assert.Less(previous - _opening.Weight, 0.02f, "No jump.");
                previous = _opening.Weight;
            }

            Run(3f * Step);
            Assert.IsFalse(_opening.IsActive);
            Assert.AreEqual(0f, _opening.Weight);
            Assert.AreEqual(0f, _opening.Turn);
        }

        [Test]
        public void AnyInput_StartsTheEaseAtOnce_AndBeginIsIgnoredMeanwhile()
        {
            _opening.Begin();
            Run(1f);
            _opening.Step(true, Step);
            Assert.IsTrue(_opening.IsEasing);
            Run(1f);
            float weight = _opening.Weight;
            _opening.Begin();
            Assert.AreEqual(weight, _opening.Weight, "A second Begin does not restart it.");
            Run(_tuning.WakeEaseSeconds);
            Assert.IsFalse(_opening.IsActive);
        }

        [TestCase(12f, 15f)]
        [TestCase(25f, 5f)]
        public void EarthScreenY_PutsEarthAtTheAskedHeight(float earthElevation, float cameraElevation)
        {
            const float Fov = 50f;
            const float Height = 0.75f;
            float screenY = WakeOpening.EarthScreenY(earthElevation, cameraElevation, Height, Fov);
            float halfHeight = Mathf.Tan(0.5f * Fov * Mathf.Deg2Rad);
            float pitch = -cameraElevation + Mathf.Atan(2f * screenY * halfHeight) * Mathf.Rad2Deg;
            float earthY = 0.5f + 0.5f * Mathf.Tan((earthElevation - pitch) * Mathf.Deg2Rad) / halfHeight;
            Assert.AreEqual(Height, earthY, 1e-3f, "Camera pitch = looking down at 07, raised by the offset.");
        }

        [Test]
        public void Held_StaysOnTheLensShot_IgnoringInputAndTheClock()
        {
            _opening.Begin(true);
            Run(_tuning.WakeHoldSeconds + 5f, true);

            Assert.IsTrue(_opening.IsHeld && !_opening.IsEasing, "the title and the lens hold beat 1");
            Assert.AreEqual(0f, _opening.Turn);
            Assert.AreEqual(1f, _opening.Weight);
        }

        [Test]
        public void StartTurn_GoesOverTheShoulder_AndEaseOutHandsToTheChase()
        {
            _opening.Begin(true);
            _opening.StartTurn();
            Run(_tuning.WakeTurnSeconds + Step);
            Assert.AreEqual(1f, _opening.Turn, 1e-3f, "beat 2 over the shoulder");

            _opening.EaseOut();
            Run(_tuning.WakeEaseSeconds + Step);
            Assert.IsFalse(_opening.IsActive, "handed to the chase frame");
        }

        [Test]
        public void TitleDrift_StartsAfterAWhileUntouched_AndEasesBackWithinASecond()
        {
            OpeningSettings settings = _tuning.Opening;
            _opening.Begin(true);
            Run(settings.TitleDriftDelay - 1f);
            Assert.AreEqual(0f, _opening.DriftYaw, "still before the delay");

            float most = 0f;
            for (float t = 0f; t < settings.TitleDriftPeriod; t += Step)
            {
                _opening.Step(false, Step);
                most = Mathf.Max(most, Mathf.Abs(_opening.DriftYaw));
            }

            Assert.Greater(most, 0.5f * settings.TitleDriftYaw, "the lens shot drifts");
            Assert.LessOrEqual(most, settings.TitleDriftYaw + 1e-4f, "within its swing");

            _opening.StartTurn();
            Run(1f + Step);
            Assert.AreEqual(0f, _opening.DriftYaw, 1e-4f, "back within a second once a choice is made");
        }
    }
}
