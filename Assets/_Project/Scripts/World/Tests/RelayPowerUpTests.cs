using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// A relay's power-up as its props see it (<see cref="RelayPowerUp"/>): nothing has begun before its stages, a
    /// motion runs from its stage over its own seconds, a loaded game stands every stage up to the replayed one at its
    /// end, and the settle eases from rest past home and back to exactly home.
    /// </summary>
    public sealed class RelayPowerUpTests
    {
        private const float Seconds = 1.5f;
        private const float Overshoot = 0.08f;
        private const float SettleFrom = 0.8f;

        [Test]
        public void BeforeItsStage_AMotionHasNotBegun_ThenRunsOverItsSeconds()
        {
            var powerUp = new RelayPowerUp();
            Assert.IsFalse(powerUp.Reached(RelayStage.Align));
            Assert.AreEqual(0f, powerUp.Progress(RelayStage.Align, Seconds));

            powerUp.OnStage(RelayStage.Align, false);
            powerUp.Step(Seconds * 0.5f);
            Assert.AreEqual(0.5f, powerUp.Progress(RelayStage.Align, Seconds), 1e-4f);
            powerUp.Step(Seconds);
            Assert.AreEqual(1f, powerUp.Progress(RelayStage.Align, Seconds));
            Assert.IsFalse(powerUp.Reached(RelayStage.Live), "later stages wait for their own event");
        }

        [Test]
        public void ALoadedLiveRelay_StandsEveryStageAtItsEnd()
        {
            var powerUp = new RelayPowerUp();
            powerUp.OnStage(RelayStage.Live, true);
            foreach (RelayStage stage in new[]
                {
                    RelayStage.Latch, RelayStage.Wake, RelayStage.Align, RelayStage.Reply, RelayStage.Live,
                })
            {
                Assert.IsTrue(powerUp.Reached(stage), $"{stage} is long done");
                Assert.AreEqual(1f, powerUp.Progress(stage, Seconds), $"{stage}'s motion is at its end");
            }
        }

        [Test]
        public void TheSettle_RunsPastHome_ThenComesBackToExactlyHome()
        {
            Assert.AreEqual(0f, RelayPowerUp.Settle(0f, Overshoot, SettleFrom), 1e-5f);
            Assert.AreEqual(1f + Overshoot, RelayPowerUp.Settle(SettleFrom, Overshoot, SettleFrom), 1e-5f);
            Assert.AreEqual(1f, RelayPowerUp.Settle(1f, Overshoot, SettleFrom), 1e-5f);
        }
    }
}
