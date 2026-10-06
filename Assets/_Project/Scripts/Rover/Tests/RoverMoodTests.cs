using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class RoverMoodTests
    {
        private const float Frame = 1f / 60f;
        private RoverCharacterTuning _tuning;
        private RoverMood _mood;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            _mood = new RoverMood(_tuning, 7u);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private bool Run(float seconds, float speed, float input)
        {
            bool woke = false;
            for (float t = 0f; t < seconds; t += Frame)
            {
                woke |= _mood.Step(speed, input, Frame);
            }

            return woke;
        }

        [Test]
        public void Driving_StaysActive()
        {
            Run(20f, 5f, 1f);
            Assert.IsFalse(_mood.IsDaydreaming);
            Assert.AreEqual(0f, _mood.Idle, 1e-4f);
            Assert.AreEqual(0f, _mood.WingOpen, 1e-4f);
        }

        [Test]
        public void StandingStill_DriftsIntoADaydreamSlowly()
        {
            Run(_tuning.IdleDelay - 0.1f, 0f, 0f);
            Assert.IsFalse(_mood.IsDaydreaming);
            Assert.AreEqual(0f, _mood.Idle, 1e-4f);

            Run(0.3f, 0f, 0f);
            Assert.IsTrue(_mood.IsDaydreaming);
            Assert.Less(_mood.Idle, 0.2f, "The daydream eases in; it does not snap.");

            Run(10f, 0f, 0f);
            Assert.Greater(_mood.Idle, 0.95f);
        }

        [Test]
        public void Daydream_DroopsTheLid_AndDeepensTheBreath()
        {
            Run(1f, 0f, 0f);
            float activeLid = _mood.LidClosure;
            Run(20f, 0f, 0f);
            Assert.Greater(_mood.LidClosure, activeLid + 0.15f);

            float low = float.MaxValue;
            float high = float.MinValue;
            for (float t = 0f; t < _tuning.BreathPeriod; t += Frame)
            {
                _mood.Step(0f, 0f, Frame);
                low = Mathf.Min(low, _mood.EyeGlow);
                high = Mathf.Max(high, _mood.EyeGlow);
            }

            Assert.Greater(high - low, 0.2f, "The eye glow breathes visibly while daydreaming.");
        }

        [Test]
        public void Daydream_WingSighsOpenPastRestThenSettles()
        {
            Run(_tuning.IdleDelay, 0f, 0f);
            float peak = 0f;
            for (float t = 0f; t < 15f; t += Frame)
            {
                _mood.Step(0f, 0f, Frame);
                peak = Mathf.Max(peak, _mood.WingOpen);
            }

            Run(20f, 0f, 0f);
            float rest = _mood.WingOpen;
            Assert.AreEqual(_tuning.WingIdleOpen, rest, 0.01f);
            Assert.Greater(peak, rest + 0.01f, "Opens a little further, like a sigh, then settles.");
        }

        [Test]
        public void DrivingOffFromADeepDaydream_WakesOnce()
        {
            Run(15f, 0f, 0f);
            Assert.IsTrue(_mood.Step(0f, 1f, Frame));
            Assert.IsFalse(Run(2f, 3f, 1f));
            Assert.Less(_mood.Idle, 0.05f);
        }

        [Test]
        public void BriefPause_DoesNotWake()
        {
            Run(_tuning.IdleDelay + 0.05f, 0f, 0f);
            Assert.IsFalse(_mood.Step(0f, 1f, Frame), "Barely asleep: no perk-up.");
        }

        [Test]
        public void PerkUp_SwellsToItsStrengthAndFades()
        {
            _mood.PerkUp(0.8f);
            float peak = 0f;
            for (float t = 0f; t < 1f; t += Frame)
            {
                _mood.Step(5f, 1f, Frame);
                peak = Mathf.Max(peak, _mood.Perk);
            }

            Assert.AreEqual(0.8f, peak, 0.03f);
            Run(4f, 5f, 1f);
            Assert.Less(_mood.Perk, 0.02f);
        }

        [Test]
        public void PerkUp_WidensTheEyeAndBrightensTheGlow()
        {
            Run(1f, 5f, 1f);
            float calmLid = _mood.LidClosure;
            _mood.PerkUp(1f);
            Run(1f / (2f * Mathf.PI * _tuning.PerkFrequency), 5f, 1f);
            Assert.Less(_mood.LidClosure, calmLid);
            Assert.Greater(_mood.EyeGlow, 1.2f);
            Assert.Greater(_mood.HeadPitchOffset, 0f);
        }

        [Test]
        public void HardLanding_Oofs_SoftLandingDoesNot()
        {
            Assert.AreEqual(0f, _mood.OofStrength(_tuning.OofImpactSpeed * 0.5f));
            Assert.AreEqual(_tuning.OofMinStrength, _mood.OofStrength(_tuning.OofImpactSpeed), 1e-4f);
            Assert.AreEqual(1f, _mood.OofStrength(_tuning.OofFullImpact * 2f), 1e-4f);

            _mood.FeelImpact(1f);
            Run(0.2f, 0f, 0f);
            Assert.Less(_mood.HeadPitchOffset, 0f, "The head dips.");
        }

        [Test]
        public void Blinks_AreOccasionalSlowAndComplete()
        {
            int blinks = 0;
            bool inBlink = false;
            float longest = 0f;
            float current = 0f;
            float maxClosure = 0f;
            for (float t = 0f; t < 60f; t += Frame)
            {
                _mood.Step(5f, 1f, Frame);
                bool blinking = _mood.Blink > 0f;
                if (blinking && !inBlink)
                {
                    blinks++;
                    current = 0f;
                }

                current = blinking ? current + Frame : current;
                longest = Mathf.Max(longest, current);
                maxClosure = Mathf.Max(maxClosure, _mood.LidClosure);
                inBlink = blinking;
            }

            int most = Mathf.CeilToInt(60f / _tuning.BlinkMinInterval);
            int fewest = Mathf.FloorToInt(60f / (_tuning.BlinkMaxInterval + _tuning.BlinkDuration)) - 1;
            Assert.That(blinks, Is.InRange(fewest, most));
            Assert.AreEqual(_tuning.BlinkDuration, longest, 2f * Frame);
            Assert.Greater(maxClosure, 0.98f);
        }

        [Test]
        public void SameSeed_SameBlinks()
        {
            var twin = new RoverMood(_tuning, 7u);
            for (float t = 0f; t < 30f; t += Frame)
            {
                _mood.Step(1f, 1f, Frame);
                twin.Step(1f, 1f, Frame);
                Assert.AreEqual(_mood.Blink, twin.Blink);
            }
        }

        [Test]
        public void AntennaTip_BlinksBetweenRestAndFullGlow()
        {
            float low = float.MaxValue;
            float high = 0f;
            for (float t = 0f; t < 2f * _tuning.TipBlinkPeriod; t += Frame)
            {
                _mood.Step(0f, 0f, Frame);
                low = Mathf.Min(low, _mood.TipGlow);
                high = Mathf.Max(high, _mood.TipGlow);
            }

            Assert.AreEqual(_tuning.TipRestGlow, low, 0.01f);
            Assert.Greater(high, 0.95f);
        }

        [Test]
        public void ActiveCalmEye_GlowsAtAuthoredBrightnessOnAverage()
        {
            float sum = 0f;
            int samples = 0;
            for (float t = 0f; t < _tuning.BreathPeriod * 2f; t += Frame)
            {
                _mood.Step(5f, 1f, Frame);
                if (_mood.Blink <= 0f)
                {
                    sum += _mood.EyeGlow;
                    samples++;
                }
            }

            Assert.AreEqual(1f, sum / samples, 0.03f);
        }
    }
}
