using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class LampFitBeatTests
    {
        private GrowthSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new GrowthSettings();
        }

        [Test]
        public void TheHead_LooksLeftThenRight_ThenIsDone()
        {
            Assert.AreEqual(0, LampFitBeat.LookSide(_settings, 0f), "still while the pods clamp");
            Assert.AreEqual(-1, LampFitBeat.LookSide(_settings, _settings.FitLookStartSeconds + 0.01f));
            Assert.AreEqual(1, LampFitBeat.LookSide(_settings,
                _settings.FitLookStartSeconds + _settings.FitLookSeconds + 0.01f));
            float end = _settings.FitLookStartSeconds + 2f * _settings.FitLookSeconds;
            Assert.AreEqual(0, LampFitBeat.LookSide(_settings, end + 0.01f));
            Assert.IsFalse(LampFitBeat.Done(_settings, end - 0.01f));
            Assert.IsTrue(LampFitBeat.Done(_settings, end));
        }

        [Test]
        public void TheChirp_ComesAfterThePodsHaveClamped_AndBeforeTheLook()
        {
            Assert.IsFalse(LampFitBeat.ChirpDue(_settings, 0f));
            Assert.IsTrue(LampFitBeat.ChirpDue(_settings, _settings.FitChirpSeconds));
            Assert.GreaterOrEqual(_settings.FitChirpSeconds, _settings.HeadlampFitSeconds, "the pods are home first");
            Assert.LessOrEqual(_settings.FitChirpSeconds, _settings.FitLookStartSeconds, "the chirp leads the look");
        }
    }
}
