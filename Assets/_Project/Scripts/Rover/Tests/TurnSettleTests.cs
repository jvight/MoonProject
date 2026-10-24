using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class TurnSettleTests
    {
        private const float Frame = 1f / 60f;
        private const float MaxRate = 8f;
        private const float Settle = 0.3f;

        [Test]
        public void ItStartsSettled_AndStaysSettledWhileTheHeadingIsStill()
        {
            var turn = new TurnSettle();
            Assert.IsTrue(turn.IsSettled);
            for (int i = 0; i < 60; i++)
            {
                turn.Step(90f, Frame, MaxRate, Settle);
            }

            Assert.IsTrue(turn.IsSettled);
        }

        [Test]
        public void ATurn_UnsettlesIt_AndItSettlesAgainOnceTheHeadingHasBeenStillForTheSettleTime()
        {
            var turn = new TurnSettle();
            turn.Step(0f, Frame, MaxRate, Settle);
            float yaw = 0f;
            for (int i = 0; i < 30; i++)
            {
                yaw += 40f * Frame;
                turn.Step(yaw, Frame, MaxRate, Settle);
                Assert.IsFalse(turn.IsSettled, "turning at 40 deg/s");
            }

            int frames = 0;
            while (!turn.IsSettled && frames < 600)
            {
                turn.Step(yaw, Frame, MaxRate, Settle);
                frames++;
            }

            Assert.IsTrue(turn.IsSettled);
            Assert.GreaterOrEqual(frames * Frame, Settle - Frame, "not before the settle time");
            Assert.LessOrEqual(frames * Frame, Settle + 2f * Frame, "and then at once");
        }

        [Test]
        public void TheHeadingWrappingRoundNorth_IsNotATurn()
        {
            var turn = new TurnSettle();
            turn.Step(359.9f, Frame, MaxRate, Settle);
            for (int i = 0; i < 40; i++)
            {
                turn.Step(i % 2 == 0 ? 0.1f : 359.9f, Frame, MaxRate * 200f, Settle);
            }

            Assert.IsTrue(turn.IsSettled);
        }
    }
}
