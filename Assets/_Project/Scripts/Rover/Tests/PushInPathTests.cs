using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The push into the lens sweeps round 07 and never crosses its hull.</summary>
    public sealed class PushInPathTests
    {
        private const float Clear = 1.8f;

        private static readonly Vector3 Center = new Vector3(10f, 0f, 20f);
        private static readonly Vector3 FaceEye = Center + new Vector3(0f, 1.1f, 0.6f);

        [Test]
        public void StartsAtTheTitleFrame_AndEndsAtTheLens()
        {
            Vector3 from = Center + new Vector3(0f, 4f, -7f);
            Assert.AreEqual(0f, Vector3.Distance(PushInPath.Position(Center, from, FaceEye, 0f, Clear), from), 1e-3f);
            Assert.AreEqual(0f, Vector3.Distance(PushInPath.Position(Center, from, FaceEye, 1f, Clear), FaceEye), 1e-3f);
        }

        [Test]
        public void FromBehindTo07sFace_TheCameraNeverCrossesTheHull()
        {
            Vector3 from = Center + new Vector3(0f, 4f, -7f);
            float nearest = float.MaxValue;
            for (float t = 0f; t <= 0.9f; t += 0.01f)
            {
                Vector3 p = PushInPath.Position(Center, from, FaceEye, t, Clear);
                float flat = Mathf.Sqrt((p.x - Center.x) * (p.x - Center.x) + (p.z - Center.z) * (p.z - Center.z));
                nearest = Mathf.Min(nearest, flat);
            }

            Assert.GreaterOrEqual(nearest, Clear * 0.95f, "The camera keeps the clear radius round 07 until it closes in.");
        }

        [Test]
        public void StartingNearerThanTheClearRadius_IsContinuousAtTheStart()
        {
            Vector3 from = Center + new Vector3(0f, 1.2f, 1.4f);
            Vector3 first = PushInPath.Position(Center, from, FaceEye, 0.01f, Clear);
            Assert.Less(Vector3.Distance(first, from), 0.1f, "No jump out to the clear radius at the first frame.");
        }

        [Test]
        public void TheCameraTurnsTheShortWayRound()
        {
            Vector3 from = Center + new Vector3(7f, 3f, -1f);
            Vector3 mid = PushInPath.Position(Center, from, FaceEye, 0.5f, Clear);
            Assert.Greater(mid.z - Center.z, -1f, "From the east to the front it sweeps through the north-east, not round the back.");
        }

        [Test]
        public void TheHeightEasesBetweenTheTwoEnds()
        {
            Vector3 from = Center + new Vector3(0f, 4f, -7f);
            Assert.AreEqual(Mathf.Lerp(4f, 1.1f, 0.5f), PushInPath.Position(Center, from, FaceEye, 0.5f, Clear).y - Center.y, 1e-3f);
        }
    }
}
