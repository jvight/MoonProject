using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The antenna in 07's listening (first-hour v2 §3): the tip lights cyan from tower level 1 (at once on a load),
    /// and each listen lifts the antenna and blinks the tip softly, then everything settles back.
    /// </summary>
    public sealed class AntennaListenTests
    {
        private const float Frame = 1f / 120f;

        private RoverCharacterTuning _tuning;
        private AntennaListenSettings _settings;
        private AntennaListen _antenna;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            _settings = _tuning.Listening;
            _antenna = new AntennaListen(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Asleep_TheTipKeepsItsWarmCallSign_AndTheAntennaRests()
        {
            Run(1f);
            Assert.AreEqual(0f, _antenna.Lit);
            Assert.AreEqual(0f, _antenna.Lift);
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1f), _antenna.TipEmission(0.5f), "white: the authored warmth");
        }

        [Test]
        public void TowerWakes_TheTipEasesToCyan_OrIsLitAtOnceOnALoad()
        {
            _antenna.SetAwake(true, false);
            Run(Frame);
            Assert.Less(_antenna.Lit, 0.1f, "eased in, never a snap");
            Run(_settings.LitHalfLife * 8f);
            Assert.AreEqual(1f, _antenna.Lit, 0.01f);
            Color lit = _antenna.TipEmission(0f);
            Assert.Greater(lit.b, lit.r, "cyan over the warm swatch");
            Assert.Greater(lit.g, lit.r);

            var loaded = new AntennaListen(_settings);
            loaded.SetAwake(true, true);
            Assert.AreEqual(1f, loaded.Lit, "a loaded game starts lit");
        }

        [Test]
        public void Listen_LiftsTheAntenna_BlinksTheTip_ThenSettles()
        {
            _antenna.SetAwake(true, true);
            _antenna.Listen();
            float highest = 0f;
            int blinks = 0;
            bool bright = false;
            float total = _settings.LiftRise + _settings.LiftHold + _settings.LiftFall;
            for (float t = 0f; t < total + 0.5f; t += Frame)
            {
                Run(Frame);
                highest = Mathf.Max(highest, _antenna.Lift);
                bool now = _antenna.Blink > 0.5f;
                blinks += now && !bright ? 1 : 0;
                bright = now;
            }

            Assert.AreEqual(_settings.LiftHeight, highest, 1e-3f, "the antenna rises its full height");
            Assert.AreEqual(_settings.BlinkCount, blinks, "the tip blinks its count");
            Assert.AreEqual(0f, _antenna.Lift, 1e-4f, "and settles back");
            Assert.AreEqual(0f, _antenna.Blink);
            Assert.GreaterOrEqual(_settings.BlinkPeriod, 1f / 3f, "nothing blinks at 3 Hz or faster");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds - 1e-6f; t += Frame)
            {
                _antenna.Step(Frame);
            }
        }
    }
}
