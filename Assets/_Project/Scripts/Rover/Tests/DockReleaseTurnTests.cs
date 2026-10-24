using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>07 turns on the pad the short way round, eased, and is simply freed when the headings already match.</summary>
    public sealed class DockReleaseTurnTests
    {
        private const float Seconds = 1.2f;
        private const float Frame = 0.02f;

        [Test]
        public void Matching_Headings_NeedNoTurn()
        {
            var turn = new DockReleaseTurn();
            Assert.IsFalse(turn.Begin(90f, 92f, 3f));
            Assert.IsFalse(turn.Active);
        }

        [Test]
        public void TheTurn_TakesTheShortWayRound_AndEndsOnTheReleaseHeading()
        {
            var turn = new DockReleaseTurn();
            Assert.IsTrue(turn.Begin(350f, 70f, 3f));
            float heading = 0f;
            float last = 350f;
            float widest = 0f;
            for (float t = 0f; turn.Active && t < 5f; t += Frame)
            {
                heading = turn.Step(Frame, Seconds);
                widest = Mathf.Max(widest, Mathf.Abs(Mathf.DeltaAngle(last, heading)));
                last = heading;
            }

            Assert.AreEqual(70f, heading, 1e-3f);
            Assert.Less(widest, 5f, "an eased turn never jumps more than a few degrees a frame");
        }

        [Test]
        public void TheTurn_StartsGently_AndCanBeCancelled()
        {
            var turn = new DockReleaseTurn();
            turn.Begin(0f, 80f, 3f);
            float first = turn.Step(Frame, Seconds);
            Assert.Less(first, 0.5f, "it starts gently");
            turn.Cancel();
            Assert.IsFalse(turn.Active);
        }
    }
}
