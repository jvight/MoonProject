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
        public void Signal_StaticIsFaintWhenClear_FullWhenLost_AndFallsWithClarity()
        {
            var signal = new RadioSignal(_tuning);
            float edge = _tuning.SignalRadius + _tuning.FalloffWidth;
            signal.Snap(0f);
            Assert.AreEqual(_tuning.StaticFloorVolume, signal.StaticVolume, 1e-6f);
            signal.Snap(edge);
            Assert.AreEqual(_tuning.StaticMaxVolume, signal.StaticVolume, 1e-6f);

            float previous = _tuning.StaticMaxVolume;
            for (float distance = edge; distance >= 0f; distance -= 5f)
            {
                signal.Snap(distance);
                Assert.LessOrEqual(signal.StaticVolume, previous + 1e-6f);
                previous = signal.StaticVolume;
            }
        }

        [Test]
        public void Signal_ClarityEasesTowardsTheDistance_AndSnapsWhenSetDown()
        {
            var signal = new RadioSignal(_tuning);
            float lost = _tuning.SignalRadius + _tuning.FalloffWidth;
            signal.Snap(lost);
            Assert.AreEqual(0f, signal.Clarity, 1e-5f);

            signal.Step(0f, 0.1f);
            Assert.Greater(signal.Clarity, 0f);
            Assert.Less(signal.Clarity, 0.5f, "eased, never a switch");

            for (int i = 0; i < 2000; i++)
            {
                signal.Step(0f, 0.02f);
            }

            Assert.AreEqual(1f, signal.Clarity, 0.01f);
            signal.Snap(lost);
            Assert.AreEqual(0f, signal.Clarity, 1e-5f);
        }
    }
}
