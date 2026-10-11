using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.App.Tests
{
    /// <summary>New Game over a save reloads inside O1's black (cutscenes §1.7).</summary>
    public sealed class PushedInGateTests
    {
        private const float Timeout = 6f;
        private const float Frame = 1f / 60f;

        [Test]
        public void Armed_OpensOnceOnThePushIntoTheLens()
        {
            var gate = new PushedInGate();
            Assert.IsFalse(gate.Notify(WakeBeat.PushedIn), "not armed: nothing to reload");

            gate.Arm();
            Assert.IsFalse(gate.Notify(WakeBeat.Crackle));
            Assert.IsFalse(gate.Step(Frame, Timeout));
            Assert.IsTrue(gate.Notify(WakeBeat.PushedIn), "the reload hides in the black");
            Assert.IsFalse(gate.Notify(WakeBeat.PushedIn), "once");
            Assert.IsFalse(gate.TimedOut);
        }

        [Test]
        public void WithoutTheBeat_GivesUpWaitingAfterTheTimeout()
        {
            var gate = new PushedInGate();
            gate.Arm();
            bool opened = false;
            float waited = 0f;
            while (!opened && waited < 2f * Timeout)
            {
                opened = gate.Step(Frame, Timeout);
                waited += Frame;
            }

            Assert.IsTrue(opened, "the reload is never lost");
            Assert.AreEqual(Timeout, waited, 2f * Frame);
            Assert.IsTrue(gate.TimedOut);
            Assert.IsFalse(gate.IsWaiting);
        }
    }
}
