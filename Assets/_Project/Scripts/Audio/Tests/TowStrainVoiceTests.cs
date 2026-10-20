using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    /// <summary>The tow cable's creak: silent when slack, louder and higher as it strains, never falling back.</summary>
    public sealed class TowStrainVoiceTests
    {
        private const float Slack = 0.08f;

        [Test]
        public void Gain_IsSilentWhenSlack_AndNeverFallsAsTheCableStrains()
        {
            Assert.AreEqual(0f, TowStrainVoice.Gain(0f, Slack));
            Assert.AreEqual(0f, TowStrainVoice.Gain(Slack, Slack));
            Assert.AreEqual(1f, TowStrainVoice.Gain(1f, Slack), 1e-5f);
            float previous = -1f;
            for (int i = 0; i <= 100; i++)
            {
                float gain = TowStrainVoice.Gain(i * 0.01f, Slack);
                Assert.GreaterOrEqual(gain, previous);
                previous = gain;
            }
        }

        [Test]
        public void Pitch_RisesFromTautToStretched()
        {
            Assert.AreEqual(0.85f, TowStrainVoice.Pitch(0f, 0.85f, 1.2f), 1e-5f);
            Assert.AreEqual(1.2f, TowStrainVoice.Pitch(1f, 0.85f, 1.2f), 1e-5f);
            Assert.Less(TowStrainVoice.Pitch(0.3f, 0.85f, 1.2f), TowStrainVoice.Pitch(0.7f, 0.85f, 1.2f));
        }
    }
}
