using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class RadioTests
    {
        private RadioTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RadioTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void SignalField_IsClearInsideRadius_GoneBeyondFalloff_AndHalfInTheMiddle()
        {
            Assert.AreEqual(1f, SignalField.Clarity(10f, 60f, 140f));
            Assert.AreEqual(1f, SignalField.Clarity(60f, 60f, 140f));
            Assert.AreEqual(0.5f, SignalField.Clarity(130f, 60f, 140f), 1e-5f);
            Assert.AreEqual(0f, SignalField.Clarity(500f, 60f, 140f));
        }

        [Test]
        public void SignalField_FallsMonotonically()
        {
            float previous = 1f;
            for (float d = 0f; d < 300f; d += 2.5f)
            {
                float clarity = SignalField.Clarity(d, 60f, 140f);
                Assert.LessOrEqual(clarity, previous + 1e-6f);
                previous = clarity;
            }
        }

        [Test]
        public void HorizontalDistance_IgnoresHeight()
        {
            Assert.AreEqual(5f, SignalField.HorizontalDistance(new Vector3(3f, 40f, 4f), Vector3.zero), 1e-5f);
        }

        [Test]
        public void Mix_ClearSignal_IsOpenAndQuiet_LostSignal_IsHazyAndStatic()
        {
            RadioMix clear = RadioMix.Evaluate(1f, _tuning);
            RadioMix lost = RadioMix.Evaluate(0f, _tuning);

            Assert.AreEqual(_tuning.MaxCutoff, clear.CutoffHz, 1f);
            Assert.AreEqual(_tuning.MinCutoff, lost.CutoffHz, 1e-3f);
            Assert.AreEqual(_tuning.StaticFloorVolume, clear.StaticVolume, 1e-6f);
            Assert.AreEqual(_tuning.StaticMaxVolume, lost.StaticVolume, 1e-6f);
            Assert.AreEqual(1f, clear.MusicVolume, 1e-6f);
            Assert.Greater(lost.WobbleCents, clear.WobbleCents);
        }

        [Test]
        public void Mix_CutoffRisesAndStaticFallsWithClarity()
        {
            RadioMix previous = RadioMix.Evaluate(0f, _tuning);
            for (float c = 0.05f; c <= 1f; c += 0.05f)
            {
                RadioMix mix = RadioMix.Evaluate(c, _tuning);
                Assert.Greater(mix.CutoffHz, previous.CutoffHz);
                Assert.LessOrEqual(mix.StaticVolume, previous.StaticVolume);
                previous = mix;
            }
        }

        [Test]
        public void Signal_RadiusBloomsInSlowly_AndClarityFollows()
        {
            var signal = new RadioSignal(_tuning);
            float distance = _tuning.SignalRadius + _tuning.FalloffWidth;
            signal.Snap(distance);
            Assert.AreEqual(0f, signal.Clarity, 1e-5f);

            signal.SetTargetRadius(distance + 10f);
            signal.Step(distance, 0.1f);
            Assert.Less(signal.Radius, distance);

            for (int i = 0; i < 2000; i++)
            {
                signal.Step(distance, 0.02f);
            }

            Assert.AreEqual(distance + 10f, signal.Radius, 0.01f);
            Assert.AreEqual(1f, signal.Clarity, 0.01f);
        }

        [Test]
        public void Crossfade_FadesOutThenIn_WithStaticInTheMiddle()
        {
            var fade = new RadioCrossfade();
            fade.Begin(2f, 0.5f, 0.5f);
            Assert.AreEqual(1f, fade.OutgoingGain, 1e-5f);
            Assert.AreEqual(0f, fade.IncomingGain, 1e-5f);

            int starts = 0;
            float peakSwell = 0f;
            for (int i = 0; i < 100 && fade.Active; i++)
            {
                starts += fade.Step(0.05f) ? 1 : 0;
                peakSwell = Mathf.Max(peakSwell, fade.StaticSwell);
            }

            Assert.AreEqual(1, starts);
            Assert.IsFalse(fade.Active);
            Assert.AreEqual(1f, fade.IncomingGain);
            Assert.Greater(peakSwell, 0.95f);
        }

        [Test]
        public void Crossfade_LargeStep_StillStartsIncomingOnce()
        {
            var fade = new RadioCrossfade();
            fade.Begin(1f, 0.5f, 0.6f);
            Assert.IsTrue(fade.Step(5f));
            Assert.IsFalse(fade.Active);
            Assert.IsFalse(fade.Step(1f));
        }

        [Test]
        public void WowFlutter_StaysInRange_AndMapsCentsToPitch()
        {
            var wobble = new WowFlutter();
            for (int i = 0; i < 1000; i++)
            {
                float v = wobble.Step(0.016f, 0.55f, 6.3f, 0.3f);
                Assert.LessOrEqual(Mathf.Abs(v), 1f + 1e-5f);
            }

            Assert.AreEqual(2f, WowFlutter.PitchFactor(1f, 1200f), 1e-5f);
            Assert.AreEqual(1f, WowFlutter.PitchFactor(0f, 50f), 1e-6f);
        }
    }
}
