using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void NightLights_LightNearestToTheBaseFirst_OnePerInterval_AfterTheDelay()
        {
            var lights = new NightLightsSequence();
            lights.Add(1, new Vector3(30f, 0f, 0f));
            lights.Add(2, new Vector3(5f, 0f, 0f));
            lights.Add(3, new Vector3(0f, 0f, -12f));
            lights.Begin(Vector3.zero, 2f);

            Assert.IsFalse(lights.Step(1.9f, 0.5f, out _, out _), "the night settles first");
            Assert.IsTrue(lights.Step(0.2f, 0.5f, out Vector3 first, out int firstOrder));
            Assert.AreEqual(new Vector3(5f, 0f, 0f), first);
            Assert.AreEqual(0, firstOrder);
            Assert.IsFalse(lights.Step(0.4f, 0.5f, out _, out _));
            Assert.IsTrue(lights.Step(0.1f, 0.5f, out Vector3 second, out int secondOrder));
            Assert.AreEqual(new Vector3(0f, 0f, -12f), second);
            Assert.AreEqual(1, secondOrder);
            Assert.IsTrue(lights.Step(0.5f, 0.5f, out Vector3 third, out _));
            Assert.AreEqual(new Vector3(30f, 0f, 0f), third);
            Assert.IsFalse(lights.Running);
            Assert.IsFalse(lights.Step(5f, 0.5f, out _, out _));
        }

        [Test]
        public void NightLights_ARemovedStructureNeverLights_AndAReAddedOneMoves()
        {
            var lights = new NightLightsSequence();
            lights.Add(1, new Vector3(4f, 0f, 0f));
            lights.Add(2, new Vector3(8f, 0f, 0f));
            lights.Add(2, new Vector3(2f, 0f, 0f));
            lights.Remove(1);
            Assert.AreEqual(1, lights.Count);
            lights.Begin(Vector3.zero, 0f);
            Assert.IsTrue(lights.Step(0f, 1f, out Vector3 only, out _));
            Assert.AreEqual(new Vector3(2f, 0f, 0f), only);
            Assert.IsFalse(lights.Running);
        }

        [Test]
        public void NightLights_ASunriseStopsTheSequence()
        {
            var lights = new NightLightsSequence();
            lights.Add(1, Vector3.one);
            lights.Add(2, Vector3.one * 2f);
            lights.Begin(Vector3.zero, 0f);
            Assert.IsTrue(lights.Step(0f, 1f, out _, out _));
            lights.Stop();
            Assert.IsFalse(lights.Running);
            Assert.IsFalse(lights.Step(2f, 1f, out _, out _));
        }
    }
}
