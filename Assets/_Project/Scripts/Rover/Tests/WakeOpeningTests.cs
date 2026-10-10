using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The fresh game's first shot holds on 07's lens, then eases to the chase frame softly.</summary>
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
        public void WithoutInput_HoldsThenEasesOutOverTheEaseTime()
        {
            _opening.Begin();
            Run(_tuning.WakeHoldSeconds - 0.5f);
            Assert.IsFalse(_opening.IsEasing, "Holds on the lens.");
            Assert.AreEqual(1f, _opening.Weight);

            Run(0.5f + Step);
            Assert.IsTrue(_opening.IsEasing, "The hold ran out.");

            float previous = _opening.Weight;
            float halfway = 0f;
            for (float t = 0f; t < _tuning.WakeEaseSeconds - 2f * Step; t += Step)
            {
                _opening.Step(false, Step);
                Assert.LessOrEqual(_opening.Weight, previous, "Only ever eases toward the chase frame.");
                Assert.Less(previous - _opening.Weight, 0.02f, "No jump.");
                previous = _opening.Weight;
                halfway = t < 0.5f * _tuning.WakeEaseSeconds ? _opening.Weight : halfway;
            }

            Assert.That(halfway, Is.InRange(0.4f, 0.6f), "Halfway through the ease, half handed over.");
            Run(3f * Step);
            Assert.IsFalse(_opening.IsActive);
            Assert.AreEqual(0f, _opening.Weight);
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
    }
}
