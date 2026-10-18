using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    public sealed class BootGateTests
    {
        private const float Steady = 0.016f;

        private static BootGate Gate()
        {
            return new BootGate(steadyMilliseconds: 40f, steadyFrames: 12, minSeconds: 0.5f, maxSeconds: 8f);
        }

        [Test]
        public void HoldsThroughTheLongFirstFrames_AndLiftsOnlyAfterSteadyFramesFollow()
        {
            BootGate gate = Gate();
            Assert.IsFalse(gate.Tick(4.2f), "The first, long frame never lifts the dark.");
            Assert.IsFalse(gate.Tick(1.9f), "Nor does the second.");
            for (int frame = 0; frame < 11; frame++)
            {
                Assert.IsFalse(gate.Tick(Steady), "Eleven steady frames are not yet twelve.");
            }

            Assert.IsTrue(gate.Tick(Steady));
        }

        [Test]
        public void ALateHitchRestartsTheCount()
        {
            BootGate gate = Gate();
            for (int frame = 0; frame < 11; frame++)
            {
                gate.Tick(Steady);
            }

            Assert.IsFalse(gate.Tick(0.3f));
            for (int frame = 0; frame < 11; frame++)
            {
                Assert.IsFalse(gate.Tick(Steady));
            }

            Assert.IsTrue(gate.Tick(Steady));
        }

        [Test]
        public void KeepsTheDarkAtLeastItsMinimum_EvenWhenFramesAreSteadyFromTheStart()
        {
            BootGate gate = Gate();
            float elapsed = 0f;
            bool lifted = false;
            while (!lifted && elapsed < 2f)
            {
                lifted = gate.Tick(Steady);
                elapsed += Steady;
            }

            Assert.IsTrue(lifted);
            Assert.GreaterOrEqual(elapsed, 0.5f);
        }

        [Test]
        public void GivesUpAtTheMaximum_WhenFramesNeverSettle()
        {
            BootGate gate = Gate();
            float elapsed = 0f;
            while (!gate.Tick(0.1f))
            {
                elapsed += 0.1f;
                Assert.Less(elapsed, 9f, "The dark must lift by its maximum.");
            }

            Assert.GreaterOrEqual(elapsed, 7.8f);
        }
    }
}
