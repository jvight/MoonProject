using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    /// <summary>07's wake cues keyed to the beats (cutscenes §1.2).</summary>
    public sealed class WakeCueSheetTests
    {
        private readonly WakeCueCall[] _calls = new WakeCueCall[WakeCueSheet.MaxCalls];
        private WakeAudioSettings _settings;
        private WakeCueSheet _sheet;

        [SetUp]
        public void SetUp()
        {
            _settings = new WakeAudioSettings();
            _sheet = new WakeCueSheet(_settings);
        }

        [TestCase(WakeBeat.Crackle, WakeCueKind.Thump)]
        [TestCase(WakeBeat.PictureOpens, WakeCueKind.Tick)]
        [TestCase(WakeBeat.EyesOpen, WakeCueKind.Tick)]
        [TestCase(WakeBeat.Stirred, WakeCueKind.Tick)]
        public void EveryVoicedBeat_PlaysItsCueAtOnce(WakeBeat beat, WakeCueKind first)
        {
            int count = _sheet.CuesFor(beat, _calls);

            Assert.GreaterOrEqual(count, 1);
            Assert.AreEqual(first, _calls[0].Kind);
            Assert.AreEqual(0f, _calls[0].Delay);
        }

        [Test]
        public void ThePictureTicksLow_TheEyesHigher_TheShortWakeHighest()
        {
            _sheet.CuesFor(WakeBeat.PictureOpens, _calls);
            Assert.AreEqual(WakeCueSheet.TickA2, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.EyesOpen, _calls);
            Assert.AreEqual(WakeCueSheet.TickD3, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.Stirred, _calls);
            Assert.AreEqual(WakeCueSheet.TickD5, _calls[0].Variant);
        }

        [Test]
        public void TheGhostChordSurfacesFaintly_AMomentIntoTheBlack()
        {
            Assert.AreEqual(2, _sheet.CuesFor(WakeBeat.Crackle, _calls));
            Assert.AreEqual(WakeCueKind.Ghost, _calls[1].Kind);
            Assert.Greater(_calls[1].Delay, 0f, "after the thump");
            Assert.AreEqual(_settings.GhostFaint, _calls[1].Volume, 1e-6f);
        }

        [TestCase(WakeBeat.FadedOut)]
        [TestCase(WakeBeat.ChecksBegin)]
        [TestCase(WakeBeat.Blinked)]
        [TestCase(WakeBeat.ChaseFrame)]
        public void CameraBeats_PlayNoCueOfTheirOwn(WakeBeat beat)
        {
            Assert.AreEqual(0, _sheet.CuesFor(beat, _calls));
        }
    }
}
