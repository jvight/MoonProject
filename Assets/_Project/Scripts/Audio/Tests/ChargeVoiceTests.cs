using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    /// <summary>The weighted charge-up's voice (0.5.5 section 5.5): brace, whine up the stages, hold, drain.</summary>
    public sealed class ChargeVoiceTests
    {
        private const float Frame = 1f / 60f;
        private const float Whine = 0.7f;

        private static void Run(ChargeVoice voice, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                voice.Step(Frame, Whine);
            }
        }

        [Test]
        public void AFreshCharge_WaitsOutTheBrace_ThenTheWhineRisesTowardD()
        {
            var voice = new ChargeVoice();
            voice.Begin(false, 0.5f, 0.45f, 0.35f);
            Run(voice, 0.4f);
            Assert.AreEqual(0f, voice.WhineLevel, 1e-4f, "silent through the brace");
            Assert.Greater(voice.HumLevel, 0f, "the hum on D is already swelling");
            Run(voice, 0.5f);
            Assert.Greater(voice.WhineLevel, 0.3f);
            Assert.Greater(voice.Pitch, 0.75f, "gliding up from below D");
            Assert.Less(voice.Pitch, 1f);
        }

        [Test]
        public void EachLock_StepsOntoDThenAThenDOctave_AndTheFullLockHolds()
        {
            var voice = new ChargeVoice();
            voice.Begin(true, 0.5f, 0.45f, 0.35f);
            voice.Lock(1, 3, 0.55f);
            Assert.AreEqual(1f, voice.Pitch, 1e-4f, "D");
            voice.Lock(2, 3, 0.75f);
            Assert.AreEqual(1.4983f, voice.Pitch, 1e-4f, "A");
            voice.Lock(3, 3, 0.9f);
            Assert.AreEqual(2f, voice.Pitch, 1e-4f, "D'");
            Run(voice, 0.3f);
            Assert.AreEqual(2f, voice.Pitch, 1e-4f, "held through the breath");
            Assert.IsTrue(voice.IsSounding);
            voice.Fire();
            Assert.IsFalse(voice.IsSounding, "the ignition takes over");
            Assert.AreEqual(0f, voice.WhineLevel);
        }

        [Test]
        public void ABuildingStage_GlidesTowardTheNextNote()
        {
            var voice = new ChargeVoice();
            voice.Begin(true, 0.5f, 0.45f, 0.35f);
            voice.Lock(1, 2, 0.55f);
            Run(voice, 0.3f);
            Assert.Greater(voice.Pitch, 1.2f, "most of the way to A, easing into it");
            Assert.Less(voice.Pitch, 1.4983f);
        }

        [Test]
        public void ARelease_GlidesDownAndFades_ThenFallsSilent()
        {
            var voice = new ChargeVoice();
            voice.Begin(true, 0.5f, 0.45f, 0.35f);
            voice.Lock(1, 3, 0.55f);
            Run(voice, 0.2f);
            voice.Drain(0.2f);
            Run(voice, 0.1f);
            Assert.Less(voice.Pitch, 1f, "falling");
            Run(voice, 0.2f);
            Assert.IsFalse(voice.IsSounding);
            Assert.AreEqual(0f, voice.WhineLevel);
        }

        [Test]
        public void ALockAfterARelease_IsIgnored()
        {
            var voice = new ChargeVoice();
            voice.Begin(true, 0.5f, 0.45f, 0.35f);
            voice.Drain(0.2f);
            voice.Lock(1, 2, 0.55f);
            Run(voice, 0.3f);
            Assert.IsFalse(voice.IsSounding);
        }
    }
}
