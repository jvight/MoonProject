using NUnit.Framework;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The charge run (M3-26 §2.2): timing, resume, ignition, drain and fade.</summary>
    public sealed class ChargeRunTests
    {
        private readonly ChargeRunSettings _settings = new ChargeRunSettings();

        [Test]
        public void TheLastLamp_LightsAtIgnition_AndTheRunAccelerates()
        {
            Assert.AreEqual(0.65f, ChargeRun.Onset(4, 5, 0.65f, 0f), 1e-5f, "The last lamp lands at ignition.");
            float first = ChargeRun.Onset(0, 5, 0.65f, 0f);
            float second = ChargeRun.Onset(1, 5, 0.65f, 0f) - first;
            float last = ChargeRun.Onset(4, 5, 0.65f, 0f) - ChargeRun.Onset(3, 5, 0.65f, 0f);
            Assert.Less(last, second, "Ease-in: the gaps shrink toward the emitter.");
        }

        [Test]
        public void AResume_StartsPartWay_TheWarmLampsAtOnce()
        {
            Assert.AreEqual(0f, ChargeRun.Onset(0, 5, 0.3f, 0.5f), "Under the start share: lit at once.");
            Assert.AreEqual(0f, ChargeRun.Onset(1, 5, 0.3f, 0.5f));
            Assert.Greater(ChargeRun.Onset(2, 5, 0.3f, 0.5f), 0f);
            Assert.AreEqual(0.3f, ChargeRun.Onset(4, 5, 0.3f, 0.5f), 1e-5f);
        }

        [Test]
        public void TheLamps_FlareHoldSpikeDrainAndRest()
        {
            var run = new ChargeRun(_settings, 5);
            run.Begin(0.65f, 0f);
            run.Step(0.01f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(4), "The emitter's lamp waits for the run.");
            run.Step(0.65f);
            Assert.AreEqual(_settings.ChargedGlow, run.Glow(0), 1e-5f, "Charged once the run has passed.");
            run.Ignite();
            Assert.Greater(run.Glow(0), _settings.ChargedGlow, "Every lamp spikes at ignition.");
            run.Step(0.2f);
            run.Drain();
            run.Step(0.06f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(4), "The emitter end drains first ...");
            Assert.AreEqual(_settings.ChargedGlow, run.Glow(0), 1e-5f, "... the battery end last.");
            run.Step(0.2f);
            Assert.AreEqual(_settings.RestGlow, run.Glow(0));
        }

        [Test]
        public void TheBreak_FadesTheLamps()
        {
            var run = new ChargeRun(_settings, 5);
            run.Begin(0.1f, 0f);
            run.Step(0.2f);
            run.Ignite();
            run.Fade();
            run.Step(_settings.FadeSeconds * 0.5f);
            Assert.That(run.Glow(2), Is.InRange(_settings.RestGlow, _settings.ChargedGlow));
            run.Step(_settings.FadeSeconds);
            Assert.AreEqual(_settings.RestGlow, run.Glow(2));
        }
    }
}
