using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The call's point of light (<see cref="CallSkyPointLevel"/>): with no signal it holds its glow, steady; the
    /// sign's keying lifts it to its peak and never past it; it is dimmer between the tones while the call is broken
    /// and settles to steady over the fade once 07 has power; a loaded game takes its state at once.
    /// </summary>
    public sealed class CallSkyPointLevelTests
    {
        private const float Frame = 1f / 60f;
        private const float Fade = 1.2f;
        private const float Rest = 0.35f;
        private const float BrokenRest = 0.12f;

        [Test]
        public void WithNoSignal_TheLightHoldsItsGlow_Steady()
        {
            var level = new CallSkyPointLevel();
            level.OnCall(CallState.Clear, true);
            for (float t = 0f; t < 8f; t += Frame)
            {
                level.Step(Frame, Fade);
                Assert.AreEqual(Rest, level.Level(0f, Rest, BrokenRest), 1e-5f, $"steady at {t:F2} s");
            }
        }

        [Test]
        public void TheKeying_LiftsTheLightToItsPeak_AndNeverPast()
        {
            var level = new CallSkyPointLevel();
            level.OnCall(CallState.Locked, true);
            Assert.AreEqual(1f, level.Level(1f, Rest, BrokenRest), 1e-5f);
            Assert.AreEqual(1f, level.Level(3f, Rest, BrokenRest), 1e-5f, "an overdriven keying is clamped");
            float half = level.Level(0.5f, Rest, BrokenRest);
            Assert.That(half, Is.InRange(Rest + 0.01f, 0.99f), "a soft edge of a tone lies between glow and peak");
        }

        [Test]
        public void WhileBroken_ItIsDimmerBetweenTones_AndSettlesToSteadyOverTheFade()
        {
            var level = new CallSkyPointLevel();
            level.OnCall(CallState.Broken, true);
            Assert.AreEqual(BrokenRest, level.Level(0f, Rest, BrokenRest), 1e-5f);

            level.OnCall(CallState.Locked, false);
            Run(level, Fade * 0.5f);
            Assert.AreEqual(0.5f, level.Broken, 0.02f, "halfway settled halfway through the fade");
            Run(level, Fade);
            Assert.AreEqual(Rest, level.Level(0f, Rest, BrokenRest), 1e-5f);
        }

        [Test]
        public void ALoadedGame_TakesItsCallStateAtOnce()
        {
            var level = new CallSkyPointLevel();
            level.OnCall(CallState.Answered, true);
            Assert.AreEqual(0f, level.Broken);
        }

        private static void Run(CallSkyPointLevel level, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                level.Step(Frame, Fade);
            }
        }
    }
}
