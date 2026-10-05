using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class EasedValueTests
    {
        [Test]
        public void OneTimeConstant_ClosesSixtyThreePercentOfTheGap()
        {
            var value = new EasedValue(0f);

            value.Step(1f, 0.5f, 0.5f);

            Assert.AreEqual(1f - Mathf.Exp(-1f), value.Value, 1e-5f);
        }

        [Test]
        public void Result_IsFrameRateIndependent()
        {
            var coarse = new EasedValue(0f);
            var fine = new EasedValue(0f);

            coarse.Step(1f, 0.2f, 0.3f);
            for (int i = 0; i < 10; i++)
            {
                fine.Step(1f, 0.02f, 0.3f);
            }

            Assert.AreEqual(coarse.Value, fine.Value, 1e-5f);
        }

        [Test]
        public void RiseAndFall_UseTheirOwnTimeConstants()
        {
            var rising = new EasedValue(0f);
            var falling = new EasedValue(1f);

            rising.Step(1f, 0.1f, 1f, 0.01f);
            falling.Step(0f, 0.1f, 1f, 0.01f);

            Assert.Less(rising.Value, 0.2f);
            Assert.Less(falling.Value, 0.01f);
        }

        [Test]
        public void NonPositiveTimeConstant_Jumps()
        {
            var value = new EasedValue(0f);
            value.Step(0.7f, 0.016f, 0f);
            Assert.AreEqual(0.7f, value.Value);
        }
    }
}
