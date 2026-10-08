using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The radio-hop's soft dark: it follows gameplay's eased fade exactly, never cuts even when the value jumps, never
    /// reaches pure black, and its grain crawls at a calm, tuned pace only while the dark is up.
    /// </summary>
    public sealed class HopVeilTests
    {
        private const float Frame = 1f / 60f;
        private const float FadeOut = 0.7f;
        private const float Dark = 0.6f;
        private const float FadeIn = 0.8f;

        private RelaySettings _settings;
        private HopVeil _veil;

        [SetUp]
        public void SetUp()
        {
            _settings = new RelaySettings();
            _veil = new HopVeil(_settings);
        }

        [Test]
        public void WithoutAHop_TheScreenIsClear()
        {
            for (int i = 0; i < 120; i++)
            {
                _veil.Step(0f, Frame);
            }

            Assert.IsTrue(_veil.IsClear);
            Assert.AreEqual(0f, _veil.Darkness);
            Assert.AreEqual(0f, _veil.Grain);
            Assert.AreEqual(0, _veil.GrainRevision, "no grain work while clear");
        }

        [Test]
        public void ItFollowsTheEasedFade_Exactly_AndSettlesClearAfterTheHop()
        {
            float worst = 0f;
            float total = FadeOut + Dark + FadeIn;
            for (float t = 0f; t < total + 0.5f; t += Frame)
            {
                float fade = HopFade(t);
                _veil.Step(fade, Frame);
                worst = Mathf.Max(worst, Mathf.Abs(_veil.Level - fade));
            }

            Assert.Less(worst, 1e-4f, "the UI follows gameplay's value");
            Assert.IsTrue(_veil.IsClear, "and lets go completely");
        }

        [Test]
        public void TheDarkIsSoft_NeverPureBlack()
        {
            for (int i = 0; i < 240; i++)
            {
                _veil.Step(1f, Frame);
            }

            Assert.AreEqual(1f, _veil.Level);
            Assert.AreEqual(_settings.VeilMaxDarkness, _veil.Darkness, 1e-5f);
            Assert.Less(_veil.Darkness, 1f);
            Assert.AreEqual(_settings.GrainOpacity, _veil.Grain, 1e-5f, "the static is at its strongest in the dark");
        }

        [Test]
        public void AFadeThatJumps_StillEases_NeverCuts()
        {
            for (int i = 0; i < 240; i++)
            {
                _veil.Step(1f, Frame);
            }

            float before = _veil.Level;
            _veil.Step(0f, Frame);
            Assert.Greater(_veil.Level, before * 0.5f, "a hop cut short does not snap the screen back");
            float largest = 0f;
            for (int i = 0; i < 120; i++)
            {
                float previous = _veil.Level;
                _veil.Step(0f, Frame);
                largest = Mathf.Max(largest, previous - _veil.Level);
            }

            Assert.LessOrEqual(largest, _settings.VeilMaxChangePerSecond * Frame + 1e-5f,
                "every frame's step is small");
            Assert.IsTrue(_veil.IsClear);
        }

        [Test]
        public void Pausing_HoldsTheVeil()
        {
            for (int i = 0; i < 20; i++)
            {
                _veil.Step(0.5f, Frame);
            }

            float level = _veil.Level;
            for (int i = 0; i < 100; i++)
            {
                _veil.Step(1f, 0f);
            }

            Assert.AreEqual(level, _veil.Level);
        }

        [Test]
        public void TheGrain_CrawlsAtItsPace_WithinOneTile_TheSameEveryRun()
        {
            var twin = new HopVeil(_settings);
            float seconds = 2f;
            for (float t = 0f; t < seconds; t += Frame)
            {
                _veil.Step(1f, Frame);
                twin.Step(1f, Frame);
                Assert.That(_veil.GrainOffset.x, Is.InRange(-_settings.GrainTilePixels, 0f));
                Assert.That(_veil.GrainOffset.y, Is.InRange(-_settings.GrainTilePixels, 0f));
                Assert.AreEqual(_veil.GrainOffset, twin.GrainOffset, "seeded: captures repeat");
            }

            int expected = Mathf.RoundToInt(seconds * _settings.GrainStepsPerSecond);
            Assert.That(_veil.GrainRevision, Is.InRange(expected - 2, expected + 2), "it crawls, it never strobes");
        }

        /// <summary>The shape of gameplay's hop fade (HopSequence): eased out, a rest, eased back in.</summary>
        private static float HopFade(float t)
        {
            if (t < FadeOut)
            {
                return UiEase.InOutSine(t / FadeOut);
            }

            return t < FadeOut + Dark ? 1f : 1f - UiEase.InOutSine((t - FadeOut - Dark) / FadeIn);
        }
    }
}
