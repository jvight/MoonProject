using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class GameplayAudioLogicTests
    {
        [Test]
        public void SalvageMelody_ClimbsOneNotePerStep_ThenWeavesOverTheTopWindow()
        {
            int[] expected = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 5, 6, 7, 6, 5, 4 };
            for (int step = 0; step < expected.Length; step++)
            {
                Assert.AreEqual(expected[step], SalvageMelody.NoteIndex(step, 8, 4), $"step {step}");
            }
        }

        [Test]
        public void SalvageMelody_NeverLeavesTheScale_AndNeverRepeatsANote()
        {
            int previous = -1;
            for (int step = 0; step < 500; step++)
            {
                int note = SalvageMelody.NoteIndex(step, 8, 4);
                Assert.That(note, Is.InRange(0, 7));
                Assert.AreNotEqual(previous, note, $"step {step}");
                previous = note;
            }
        }

        [Test]
        public void SalvageMelody_EdgeCases()
        {
            Assert.AreEqual(0, SalvageMelody.NoteIndex(-3, 8, 4));
            Assert.AreEqual(0, SalvageMelody.NoteIndex(5, 1, 4));
            Assert.AreEqual(1, SalvageMelody.NoteIndex(1, 2, 1));
            Assert.AreEqual(0, SalvageMelody.NoteIndex(2, 2, 1));
        }

        [Test]
        public void LoopFader_TakesExactlyItsFadeTimes_AndEndsInSilence()
        {
            var fader = new LoopFader();
            Assert.IsFalse(fader.IsAudible);

            fader.FadeIn();
            fader.Step(0.25f, 0.5f, 1f);
            Assert.AreEqual(0.5f, fader.Gain, 1e-6f, "smoothstep midpoint");
            fader.Step(0.25f, 0.5f, 1f);
            Assert.AreEqual(1f, fader.Gain, 1e-6f);

            fader.FadeOut();
            fader.Step(0.99f, 0.5f, 1f);
            Assert.IsTrue(fader.IsAudible);
            fader.Step(0.02f, 0.5f, 1f);
            Assert.IsFalse(fader.IsAudible);
            Assert.AreEqual(0f, fader.Gain);
        }

        [Test]
        public void LoopFader_ReversesSmoothlyMidFade()
        {
            var fader = new LoopFader();
            fader.FadeIn();
            fader.Step(0.3f, 1f, 1f);
            float midway = fader.Gain;
            fader.FadeOut();
            fader.Step(0.1f, 1f, 1f);
            Assert.Less(fader.Gain, midway);
            Assert.Greater(fader.Gain, 0f);
        }
    }
}
