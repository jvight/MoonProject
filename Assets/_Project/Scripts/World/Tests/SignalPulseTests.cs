using NUnit.Framework;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The ring out of the call's point of light (<see cref="SignalPulse"/>): one per tone as it begins, blooming over
    /// the pulse time; a tone's soft edge or a dropout's flutter never fires it twice; nothing before the first tone.
    /// </summary>
    public sealed class SignalPulseTests
    {
        private const float Frame = 1f / 60f;
        private const float On = 0.5f;
        private const float Off = 0.2f;
        private const float Pulse = 0.3f;

        [Test]
        public void BeforeTheFirstTone_ThereIsNoRing()
        {
            var pulse = new SignalPulse();
            pulse.Step(0f, Frame, On, Off);
            Assert.AreEqual(1f, pulse.Progress(Pulse));
        }

        [Test]
        public void EachTone_StartsOneRing_ThatBloomsOverThePulseTime()
        {
            var pulse = new SignalPulse();
            pulse.Step(0.8f, Frame, On, Off);
            Assert.AreEqual(0f, pulse.Progress(Pulse), "a ring as the tone begins");
            Run(pulse, 0.8f, Pulse * 0.5f);
            Assert.AreEqual(0.5f, pulse.Progress(Pulse), 0.06f, "halfway out halfway through");
            Run(pulse, 0.8f, Pulse);
            Assert.AreEqual(1f, pulse.Progress(Pulse), "gone while the tone holds");

            Run(pulse, 0f, 0.1f);
            pulse.Step(1f, Frame, On, Off);
            Assert.AreEqual(0f, pulse.Progress(Pulse), "the next tone starts the next ring");
        }

        [Test]
        public void AFlutterAboveTheOffThreshold_NeverFiresTwice()
        {
            var pulse = new SignalPulse();
            pulse.Step(1f, Frame, On, Off);
            Run(pulse, 1f, Pulse);
            pulse.Step(0.3f, Frame, On, Off);
            pulse.Step(0.9f, Frame, On, Off);
            Assert.AreEqual(1f, pulse.Progress(Pulse), "a dip that stays above off is the same tone");
        }

        private static void Run(SignalPulse pulse, float keying, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                pulse.Step(keying, Frame, On, Off);
            }
        }
    }
}
