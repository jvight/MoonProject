using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class RadioDeckMixerTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void Change_FadesTheLiveDeckOut_AndStartsTheIncomingOnAnotherAfterTheDelay()
        {
            var mixer = new RadioDeckMixer();
            int first = mixer.StartNow(0f);
            Assert.AreEqual(first, mixer.Live);
            Assert.AreEqual(1f, mixer.Level(first));

            mixer.Change(true, 0.6f, 0.4f, 0.8f, 1.2f);
            Assert.AreEqual(-1, mixer.Live);
            Assert.IsTrue(mixer.StartPending);

            float t = 0f;
            int started = -1;
            float startTime = -1f;
            while (t < 2f)
            {
                t += Frame;
                if (mixer.Step(Frame, out int deck))
                {
                    Assert.AreEqual(-1, started, "one start per change");
                    started = deck;
                    startTime = t;
                }
            }

            Assert.AreNotEqual(first, started);
            Assert.AreEqual(0.4f, startTime, 2f * Frame);
            Assert.AreEqual(started, mixer.Live);
            Assert.AreEqual(1f, mixer.Level(started));
            Assert.AreEqual(0f, mixer.Level(first));
            Assert.IsTrue(mixer.IsIdle(first));
            Assert.IsFalse(mixer.IsIdle(started));
        }

        [Test]
        public void Change_ToNothing_FadesToSilence()
        {
            var mixer = new RadioDeckMixer();
            int deck = mixer.StartNow(0f);
            mixer.Change(false, 0.5f, 0f, 0f, 1f);
            for (int i = 0; i < 60; i++)
            {
                Assert.IsFalse(mixer.Step(Frame, out _));
            }

            Assert.AreEqual(0f, mixer.Level(deck));
            Assert.AreEqual(-1, mixer.Live);
            Assert.IsTrue(mixer.IsIdle(deck));
        }

        [Test]
        public void Swell_RisesAndFallsOverTheChange()
        {
            var mixer = new RadioDeckMixer();
            mixer.StartNow(0f);
            Assert.AreEqual(0f, mixer.Swell);
            mixer.Change(true, 0.6f, 0.5f, 0.6f, 1f);
            float peak = 0f;
            for (int i = 0; i < 90; i++)
            {
                mixer.Step(Frame, out _);
                peak = Mathf.Max(peak, mixer.Swell);
            }

            Assert.Greater(peak, 0.95f);
            Assert.AreEqual(0f, mixer.Swell);
        }

        [Test]
        public void SpinningTheDial_NeverRestartsADeckThatIsStillSounding()
        {
            var mixer = new RadioDeckMixer();
            mixer.StartNow(0f);
            var previous = new float[RadioDeckMixer.DeckCount];
            int starts = 0;
            for (int i = 0; i < 600; i++)
            {
                if (i % 7 == 0)
                {
                    mixer.Change(true, 0.6f, 0.05f, 0.6f, 1.2f);
                }

                for (int d = 0; d < RadioDeckMixer.DeckCount; d++)
                {
                    previous[d] = mixer.Level(d);
                }

                if (mixer.Step(Frame, out int deck))
                {
                    starts++;
                    Assert.AreEqual(0f, previous[deck], $"frame {i}: deck {deck} restarted while audible");
                }

                for (int d = 0; d < RadioDeckMixer.DeckCount; d++)
                {
                    float level = mixer.Level(d);
                    Assert.IsFalse(float.IsNaN(level));
                    Assert.LessOrEqual(Mathf.Abs(level - previous[d]), Frame / 0.6f + 1e-5f,
                        $"frame {i}: deck {d} jumped");
                }
            }

            Assert.Greater(starts, 10, "the dial still lands on music as it spins");
        }

        [Test]
        public void InstantFades_SnapWithoutNaN()
        {
            var mixer = new RadioDeckMixer();
            int first = mixer.StartNow(0f);
            mixer.Change(true, 0f, 0f, 0f, 0f);
            Assert.AreEqual(0f, mixer.Level(first));
            Assert.IsTrue(mixer.Step(0f, out int deck));
            Assert.AreEqual(1f, mixer.Level(deck));
            Assert.AreEqual(0f, mixer.Swell);
        }
    }
}
