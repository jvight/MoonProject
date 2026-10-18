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
        [TestCase(WakeBeat.LidCracked, WakeCueKind.Tick)]
        [TestCase(WakeBeat.LidHalf, WakeCueKind.Tick)]
        [TestCase(WakeBeat.FocusHunting, WakeCueKind.Whine)]
        [TestCase(WakeBeat.FocusSettled, WakeCueKind.Tick)]
        [TestCase(WakeBeat.Stirred, WakeCueKind.Tick)]
        public void EveryVoicedBeat_PlaysItsCueAtOnce(WakeBeat beat, WakeCueKind first)
        {
            int count = _sheet.CuesFor(beat, false, _calls);

            Assert.GreaterOrEqual(count, 1);
            Assert.AreEqual(first, _calls[0].Kind);
            Assert.AreEqual(0f, _calls[0].Delay);
        }

        [Test]
        public void TheLidStagesTickLowThenHigher_TheFocusLandsHigher_TheShortWakeHighest()
        {
            _sheet.CuesFor(WakeBeat.LidCracked, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickA2, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.LidHalf, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickD3, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.FocusSettled, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickA4, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.Stirred, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickD5, _calls[0].Variant);
        }

        [Test]
        public void TheGhostChordSurfacesFaintly_AMomentIntoTheBlack()
        {
            Assert.AreEqual(2, _sheet.CuesFor(WakeBeat.Crackle, false, _calls));
            Assert.AreEqual(WakeCueKind.Ghost, _calls[1].Kind);
            Assert.Greater(_calls[1].Delay, 0f, "after the thump");
            Assert.AreEqual(_settings.GhostFaint, _calls[1].Volume, 1e-6f);
        }

        [Test]
        public void Hurried_IsSofter_AndLeavesTheGhostChordOut()
        {
            int calm = _sheet.CuesFor(WakeBeat.Crackle, false, _calls);
            float calmThump = _calls[0].Volume;
            int hurried = _sheet.CuesFor(WakeBeat.Crackle, true, _calls);

            Assert.AreEqual(calm - 1, hurried, "no ghost chord when hurried");
            Assert.Less(_calls[0].Volume, calmThump);
        }

        [TestCase(WakeBeat.FadedOut)]
        [TestCase(WakeBeat.PullingOut)]
        [TestCase(WakeBeat.PulledOut)]
        [TestCase(WakeBeat.OverShoulder)]
        [TestCase(WakeBeat.ChaseFrame)]
        public void CameraBeats_PlayNoCueOfTheirOwn(WakeBeat beat)
        {
            Assert.AreEqual(0, _sheet.CuesFor(beat, false, _calls));
        }
    }
}
