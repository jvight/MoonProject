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
        [TestCase(WakeBeat.Flicker, WakeCueKind.Hum)]
        [TestCase(WakeBeat.GazeHome, WakeCueKind.Servo)]
        [TestCase(WakeBeat.GazePilot, WakeCueKind.Servo)]
        [TestCase(WakeBeat.GazePeak, WakeCueKind.Servo)]
        [TestCase(WakeBeat.FarFocusSettled, WakeCueKind.Tick)]
        [TestCase(WakeBeat.GazeEarth, WakeCueKind.Servo)]
        [TestCase(WakeBeat.Blinked, WakeCueKind.Tick)]
        [TestCase(WakeBeat.Stirred, WakeCueKind.Tick)]
        public void EveryVoicedBeat_PlaysItsCueAtOnce(WakeBeat beat, WakeCueKind first)
        {
            int count = _sheet.CuesFor(beat, false, _calls);

            Assert.GreaterOrEqual(count, 1);
            Assert.AreEqual(first, _calls[0].Kind);
            Assert.AreEqual(0f, _calls[0].Delay);
        }

        [Test]
        public void TheLidStagesTickLowThenHigher_AndTheBlinkClicksThenAsks()
        {
            _sheet.CuesFor(WakeBeat.LidCracked, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickA2, _calls[0].Variant);
            _sheet.CuesFor(WakeBeat.LidHalf, false, _calls);
            Assert.AreEqual(WakeCueSheet.TickD3, _calls[0].Variant);

            Assert.AreEqual(2, _sheet.CuesFor(WakeBeat.Blinked, false, _calls));
            Assert.AreEqual(WakeCueSheet.TickD5, _calls[0].Variant);
            Assert.AreEqual(WakeCueKind.Chirp, _calls[1].Kind);
            Assert.Greater(_calls[1].Delay, 0f, "the 'hm?' follows the lid's click");
        }

        [Test]
        public void TheGhostChordSurfacesThreeTimes_EachClearer()
        {
            float last = 0f;
            int lastVariant = -1;
            foreach (WakeBeat beat in new[] { WakeBeat.Crackle, WakeBeat.GazePilot, WakeBeat.GazeEarth })
            {
                int count = _sheet.CuesFor(beat, false, _calls);
                WakeCueCall ghost = _calls[count - 1];
                Assert.AreEqual(WakeCueKind.Ghost, ghost.Kind, $"{beat} brings the ghost chord");
                Assert.Greater(ghost.Volume, last, "each ghost is clearer than the last");
                Assert.Greater(ghost.Variant, lastVariant);
                last = ghost.Volume;
                lastVariant = ghost.Variant;
            }
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

        [TestCase(WakeBeat.PushedIn)]
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
