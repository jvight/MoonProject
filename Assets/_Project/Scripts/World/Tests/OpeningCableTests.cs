using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Opening v2's old cables (§4): the route's joints run end to end on the ground about a segment apart, wandering
    /// only within their tuned sway; the marker wave runs from the first marker to the last, once per repeat, and
    /// leaves the rest dark.
    /// </summary>
    public sealed class OpeningCableTests
    {
        private const float Segment = 8f;
        private const float Wander = 1.6f;
        private const float Bends = 3f;
        private const float Tolerance = 1e-3f;

        [Test]
        public void Route_RunsEndToEnd_OnTheGround_ASegmentApart()
        {
            var from = new Vector2(-2f, -4f);
            var to = new Vector2(30f, 150f);
            Vector3[] joints = CableRoute.Joints(from, to, Segment, Wander, Bends, (x, z) => 0.1f * x);
            Assert.AreEqual(new Vector2(from.x, from.y), new Vector2(joints[0].x, joints[0].z), "starts at the post");
            Vector3 last = joints[joints.Length - 1];
            Assert.AreEqual(to.x, last.x, Tolerance);
            Assert.AreEqual(to.y, last.z, Tolerance, "ends under the socket");
            Vector2 along = (to - from).normalized;
            var side = new Vector2(along.y, -along.x);
            for (int i = 0; i < joints.Length; i++)
            {
                Assert.AreEqual(0.1f * joints[i].x, joints[i].y, Tolerance, "on the ground");
                float sway = Vector2.Dot(new Vector2(joints[i].x, joints[i].z) - from, side);
                Assert.LessOrEqual(Mathf.Abs(sway), Wander + Tolerance, "within its sway");
                if (i > 0)
                {
                    float spacing = Vector3.Distance(joints[i], joints[i - 1]);
                    Assert.That(spacing, Is.InRange(Segment * 0.8f, Segment * 1.25f), "about a segment apart");
                }
            }
        }

        [Test]
        public void MarkerWave_RunsFromFirstToLast_OncePerRepeat()
        {
            const int Count = 20;
            const float Run = 4f;
            const float Repeat = 9f;
            const float Width = 0.18f;
            Assert.AreEqual(1f, CableMarkerWave.Lift(0f, 0, Count, Run, Repeat, Width), Tolerance, "starts at the post");
            Assert.AreEqual(0f, CableMarkerWave.Lift(0f, Count - 1, Count, Run, Repeat, Width), Tolerance);
            Assert.AreEqual(1f, CableMarkerWave.Lift(Run, Count - 1, Count, Run, Repeat, Width), Tolerance,
                "reaches the lander after its run");
            Assert.AreEqual(0f, CableMarkerWave.Lift(Run + 2f, Count / 2, Count, Run, Repeat, Width), Tolerance,
                "dark between waves");
            Assert.AreEqual(1f, CableMarkerWave.Lift(Repeat, 0, Count, Run, Repeat, Width), Tolerance, "and again");
        }
    }
}
