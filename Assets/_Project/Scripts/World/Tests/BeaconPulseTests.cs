using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    public sealed class BeaconPulseTests
    {
        private const float Period = 3.2f;
        private const float Sharpness = 1.6f;

        [Test]
        public void Pulse_StaysInRange_AndPeaksOncePerPeriod()
        {
            Assert.AreEqual(0f, BeaconPulse.Evaluate(0f, Period, Sharpness), 1e-5f);
            Assert.AreEqual(1f, BeaconPulse.Evaluate(Period * 0.5f, Period, Sharpness), 1e-5f);
            Assert.AreEqual(BeaconPulse.Evaluate(0.7f, Period, Sharpness),
                BeaconPulse.Evaluate(0.7f + Period * 3f, Period, Sharpness), 1e-4f);
            for (float t = 0f; t < Period * 2f; t += 0.01f)
            {
                Assert.That(BeaconPulse.Evaluate(t, Period, Sharpness), Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Pulse_EasesWithoutJumps()
        {
            // Never a harsh blink: at 60 fps the light changes by a few percent per frame at most, and it starts and
            // stops changing smoothly (zero slope at the dimmest and brightest points).
            const float frame = 1f / 60f;
            float previous = BeaconPulse.Evaluate(0f, Period, Sharpness);
            for (float t = frame; t < Period * 2f; t += frame)
            {
                float current = BeaconPulse.Evaluate(t, Period, Sharpness);
                Assert.Less(Mathf.Abs(current - previous), 0.04f, $"jump at {t:0.00} s");
                previous = current;
            }

            float nearPeak = BeaconPulse.Evaluate(Period * 0.5f - frame, Period, Sharpness);
            Assert.Less(1f - nearPeak, 0.001f, "the peak must be flat, not a spike");
        }
    }
}
