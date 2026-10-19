using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The Outsight Worlds boot splash's timeline (BOOT_SPLASH_SPEC.md): the spec's sampled frames, a hitch that never
    /// jumps the draw, and a skip that waits for the second launch.
    /// </summary>
    public sealed class BootSplashTests
    {
        private const float Close = 0.01f;
        private const float Frame = 1f / 60f;

        private BootSplashSettings _settings;
        private BootSplash _splash;

        [SetUp]
        public void SetUp()
        {
            _settings = new BootSplashSettings();
            _splash = new BootSplash(_settings);
        }

        [Test]
        public void NeverStarted_ItHoldsNothing()
        {
            Assert.IsTrue(_splash.IsDone);
            Assert.IsFalse(_splash.IsPlaying);
        }

        [TestCase(0.533f, 0.5147f, 0.1754f, 0.8621f, 0f, 0f)]
        [TestCase(1.6f, 1f, 1f, 1f, 0.8017f, 0.0554f)]
        [TestCase(2.4f, 1f, 1f, 1f, 1f, 1f)]
        [TestCase(3.467f, 0.5278f, 1f, 0.5278f, 1f, 0.5278f)]
        [TestCase(3.733f, 0.0593f, 1f, 0.0593f, 0.5192f, 0.0593f)]
        public void ItsLayers_FollowTheSpecsSampledFrames(float time, float ring, float drawn, float stroke,
            float glint, float word)
        {
            _splash.Start(false);
            _splash.Seek(time);
            Assert.AreEqual(ring, _splash.RingAlpha, Close, "ring");
            Assert.AreEqual(drawn, _splash.StrokeProgress, Close, "stroke drawn");
            Assert.AreEqual(stroke, _splash.StrokeAlpha, Close, "stroke");
            Assert.AreEqual(glint, _splash.GlintAlpha, Close, "glint");
            Assert.AreEqual(word, _splash.WordAlpha, Close, "wordmark");
        }

        [TestCase(1.333f, 0.3733f)]
        [TestCase(1.6f, 1.1114f)]
        [TestCase(1.867f, 1.0191f)]
        [TestCase(2.4f, 1f)]
        public void TheGlint_BloomsSettlesAndShrinksAsItFades(float time, float scale)
        {
            _splash.Start(false);
            _splash.Seek(time);
            Assert.AreEqual(scale, _splash.GlintScale, Close);
        }

        [Test]
        public void ALoadHitch_CountsAsOneShortStep_SoTheDrawIsNeverSkipped()
        {
            _splash.Start(false);
            _splash.Tick(2f);
            Assert.AreEqual(_settings.MaxStep, _splash.Time, 1e-5f);
            Assert.IsTrue(_splash.IsPlaying);
        }

        [Test]
        public void TheFirstLaunch_PlaysItWhole_LaterOnesMaySkipIt()
        {
            _splash.Start(false);
            _splash.Skip();
            Run(_settings.SkipFadeSeconds + 0.1f);
            Assert.IsTrue(_splash.IsPlaying, "the very first launch cannot skip");
            Run(_settings.Duration);
            Assert.IsTrue(_splash.IsDone, "it plays out on its own");

            _splash.Start(true);
            Run(1f);
            _splash.Skip();
            float ring = _splash.RingAlpha;
            Run(_settings.SkipFadeSeconds * 0.5f);
            Assert.Less(_splash.RingAlpha, ring, "a skip fades it out");
            Run(_settings.SkipFadeSeconds);
            Assert.IsTrue(_splash.IsDone, "and lets the title in early");
            Assert.Less(_splash.Time, _settings.Duration);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _splash.Tick(Frame);
            }
        }
    }
}
