using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class FaceCloseSequenceTests
    {
        private const float Frame = 1f / 60f;
        private const float Hold = 0.5f;
        private const float DipOut = 0.15f;
        private const float DipIn = 0.2f;

        private static FaceCloseSequence Started()
        {
            var sequence = new FaceCloseSequence();
            sequence.Begin(Hold, DipOut, DipIn);
            return sequence;
        }

        [Test]
        public void TheViewStaysClear_WhileTheScreenPowersOff()
        {
            FaceCloseSequence sequence = Started();
            for (float t = 0f; t < Hold - 0.05f; t += Frame)
            {
                sequence.Step(Frame);
                Assert.AreEqual(0f, sequence.Dark01, 1e-5f);
                Assert.IsFalse(sequence.SwapNow);
            }
        }

        [Test]
        public void TheCameraIsSwapped_Once_AtFullDark()
        {
            FaceCloseSequence sequence = Started();
            int swaps = 0;
            float darkAtSwap = -1f;
            for (float t = 0f; t < sequence.TotalSeconds + 0.5f; t += Frame)
            {
                sequence.Step(Frame);
                if (sequence.SwapNow)
                {
                    swaps++;
                    darkAtSwap = sequence.Dark01;
                }
            }

            Assert.AreEqual(1, swaps);
            Assert.AreEqual(1f, darkAtSwap, 1e-5f, "the swap is under full dark");
            Assert.IsFalse(sequence.IsActive);
            Assert.AreEqual(0f, sequence.Dark01, 1e-5f);
        }

        [Test]
        public void TheDarkRisesThenFalls_NeverJumping()
        {
            FaceCloseSequence sequence = Started();
            float last = 0f;
            float biggestStep = 0f;
            for (float t = 0f; t < sequence.TotalSeconds + 0.2f; t += Frame)
            {
                sequence.Step(Frame);
                biggestStep = UnityEngine.Mathf.Max(biggestStep, UnityEngine.Mathf.Abs(sequence.Dark01 - last));
                last = sequence.Dark01;
            }

            Assert.Less(biggestStep, 0.2f, "a few frames to go dark, a few to come back: no pop");
        }

        [Test]
        public void ItTakesAboutAHalfToAFullSecond()
        {
            Assert.That(Started().TotalSeconds, Is.InRange(0.7f, 1.1f));
        }

        [Test]
        public void Cancelled_TheViewIsClearAtOnce_AndNoSwapFollows()
        {
            FaceCloseSequence sequence = Started();
            for (float t = 0f; t < Hold + 0.1f; t += Frame)
            {
                sequence.Step(Frame);
            }

            Assert.Greater(sequence.Dark01, 0f);
            sequence.Cancel();
            sequence.Step(Frame);
            Assert.AreEqual(0f, sequence.Dark01, 1e-5f);
            Assert.IsFalse(sequence.SwapNow);
            Assert.IsFalse(sequence.IsActive);
        }
    }
}
