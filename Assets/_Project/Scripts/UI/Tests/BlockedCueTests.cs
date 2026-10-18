using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The quiet answer to a blocked hold of E: the chip dims and shakes softly, 07's face says why.</summary>
    public sealed class BlockedCueTests
    {
        private const float Step = 0.02f;

        private GuidanceSettings _settings;
        private BlockedCue _cue;

        [SetUp]
        public void SetUp()
        {
            _settings = new GuidanceSettings();
            _cue = new BlockedCue(_settings);
        }

        [Test]
        public void AtRest_TheChipIsUntouched_AndTheFaceSilent()
        {
            Assert.IsFalse(_cue.IsActive);
            Assert.IsFalse(_cue.FaceShown);
            Assert.AreEqual(0f, _cue.ChipOffset);
            Assert.AreEqual(1f, _cue.ChipOpacity);
        }

        [Test]
        public void ABlock_DimsTheChipAtOnce_ShakesItWithinItsSize_ThenEverythingSettles()
        {
            _cue.Start();
            Assert.AreEqual(_settings.BlockedChipOpacity, _cue.ChipOpacity, 1e-4f, "dimmed at once");
            Assert.IsTrue(_cue.FaceShown, "the face says why");
            float widest = 0f;
            float last = _cue.ChipOpacity;
            for (float t = 0f; t < _settings.BlockedChipSeconds; t += Step)
            {
                _cue.Tick(Step);
                widest = System.Math.Max(widest, System.Math.Abs(_cue.ChipOffset));
                Assert.GreaterOrEqual(_cue.ChipOpacity, last - 1e-5f, "the chip only brightens back");
                last = _cue.ChipOpacity;
            }

            Assert.Greater(widest, 0f, "it shook");
            Assert.LessOrEqual(widest, _settings.BlockedShakePixels, "a small shake");
            Run(_settings.BlockedChipSeconds + _settings.BlockedFaceSeconds);
            Assert.IsFalse(_cue.IsActive);
            Assert.IsFalse(_cue.FaceShown, "the face lets go");
            Assert.AreEqual(0f, _cue.ChipOffset);
            Assert.AreEqual(1f, _cue.ChipOpacity);
        }

        [Test]
        public void AnotherBlock_StartsTheCueOver()
        {
            _cue.Start();
            Run(_settings.BlockedFaceSeconds - 0.1f);
            _cue.Start();
            Run(0.5f);
            Assert.IsTrue(_cue.FaceShown, "the face keeps saying it for the new block");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _cue.Tick(Step);
            }
        }
    }
}
