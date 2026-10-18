using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The old cable's tear and its one bend (opening v2 §4): the Begin wave dies at the tear in a fading stutter and
    /// one cold spark ticks once; the bend lands where the cable runs its set length straight into the port.
    /// </summary>
    public sealed class CableTearTests
    {
        private const float Run = 4f;
        private const float Seconds = 1.2f;
        private const int Flickers = 3;
        private const float Tolerance = 1e-3f;

        [Test]
        public void TheLastClip_StuttersOnlyAfterTheWaveArrives_EachFlickerFainter()
        {
            Assert.AreEqual(0f, TearCue.Stutter(Run - 0.01f, Run, Seconds, Flickers), "nothing before it arrives");
            Assert.AreEqual(1f, TearCue.Stutter(Run, Run, Seconds, Flickers), Tolerance, "lit as it arrives");
            float flicker = Seconds / Flickers;
            Assert.Less(TearCue.Stutter(Run + flicker * 0.5f, Run, Seconds, Flickers), 0.05f, "dips between flickers");
            float second = TearCue.Stutter(Run + flicker, Run, Seconds, Flickers);
            float third = TearCue.Stutter(Run + flicker * 2f, Run, Seconds, Flickers);
            Assert.That(second, Is.LessThan(1f).And.GreaterThan(third), "each flicker fainter");
            Assert.AreEqual(0f, TearCue.Stutter(Run + Seconds, Run, Seconds, Flickers), Tolerance,
                "then out for good");
        }

        [Test]
        public void TheSpark_TicksOnceAfterItsDelay()
        {
            const float delay = 0.45f;
            const float lit = 0.1f;
            Assert.AreEqual(0f, TearCue.Spark(Run + delay - 0.01f, Run, delay, lit), "dark before it");
            Assert.AreEqual(1f, TearCue.Spark(Run + delay + 1e-4f, Run, delay, lit), Tolerance, "lit at once");
            Assert.That(TearCue.Spark(Run + delay + lit * 0.5f, Run, delay, lit), Is.InRange(0.4f, 0.6f), "fading");
            Assert.AreEqual(0f, TearCue.Spark(Run + delay + lit, Run, delay, lit), Tolerance, "and gone: one tick");
            Assert.AreEqual(0f, TearCue.Spark(Run + 30f, Run, delay, lit), "never again");
        }

        [Test]
        public void TheBend_LandsSoTheCableRunsItsApproachStraightIntoThePort()
        {
            var post = new Vector2(-2.4f, -4f);
            var socket = new Vector2(33.7f, 159f);
            const float turn = 45f;
            const float chord = 1.17f;
            const float reach = 0.94f;
            const float approach = 8f;
            CableCorner.Solve(post, socket, turn, chord, reach, approach, out Vector2 bendStart, out Vector2 heading);
            Assert.AreEqual(1f, heading.magnitude, Tolerance);
            Assert.AreEqual(0f, Vector2.Angle(heading, bendStart - post), 0.05f, "the long run heads into the bend");
            Vector2 into = CableCorner.Rotate(heading, turn);
            Vector2 bendEnd = bendStart + CableCorner.Rotate(heading, turn * 0.5f) * chord;
            Vector2 plugged = bendEnd + into * (approach + reach);
            Assert.AreEqual(0f, Vector2.Distance(plugged, socket), 0.01f, "the approach and plug end in the socket");
            Assert.Less(Vector2.SignedAngle(heading, into), 0f, "it turns right, clockwise seen from above");
        }
    }
}
