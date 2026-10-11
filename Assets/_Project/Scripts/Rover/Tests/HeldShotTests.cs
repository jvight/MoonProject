using NUnit.Framework;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The dock's evening: eased out and back, held until closed or for its hold.</summary>
    public sealed class HeldShotTests
    {
        private const float Frame = 1f / 60f;

        private readonly WideShotSettings _settings = new WideShotSettings(4f, 1f, 16f, 18f, 0.2f, 6f);
        private readonly WideShotFrame _frame = new WideShotFrame(90f, 18f, 16f, 0.2f);

        private static void Run(HeldShot shot, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                shot.Step(Frame);
            }
        }

        [Test]
        public void Opening_EasesOutOverItsOpenTime_NeverACut()
        {
            var shot = new HeldShot();
            shot.Open(_settings, _frame, 0f);
            shot.Step(Frame);
            Assert.Less(shot.Weight, 0.05f, "Starts from the chase camera.");
            Run(shot, 0.5f * _settings.OpenSeconds);
            Assert.That(shot.Weight, Is.InRange(0.2f, 0.95f), "Halfway, eased.");
            Run(shot, _settings.OpenSeconds);
            Assert.Greater(shot.Weight, 0.95f);
        }

        [Test]
        public void WithoutAHold_StaysOpen_UntilClosed_ThenEasesBack()
        {
            var shot = new HeldShot();
            shot.Open(_settings, _frame, 0f);
            Run(shot, 60f);
            Assert.IsTrue(shot.IsOpen);
            Assert.IsFalse(shot.HoldRanOut);
            shot.Close();
            shot.Step(Frame);
            Assert.Greater(shot.Weight, 0.9f, "Closing eases, it does not cut.");
            Run(shot, 3f * _settings.HandBackSeconds);
            Assert.AreEqual(0f, shot.Weight, "Settles fully back, so nothing stays blended in.");
        }

        [Test]
        public void AHold_RunsOut_AfterItsSeconds()
        {
            var shot = new HeldShot();
            shot.Open(_settings, _frame, 5f);
            Run(shot, 4.9f);
            Assert.IsFalse(shot.HoldRanOut);
            Run(shot, 0.2f);
            Assert.IsTrue(shot.HoldRanOut);
        }

        [Test]
        public void Reset_IsBackAtOnce()
        {
            var shot = new HeldShot();
            shot.Open(_settings, _frame, 0f);
            Run(shot, 10f);
            shot.Reset();
            Assert.IsFalse(shot.IsOpen);
            Assert.AreEqual(0f, shot.Weight);
        }
    }
}
