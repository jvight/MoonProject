using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The tow rope's shape (opening v3 3.2b): taut it runs straight, slack it sags, and never past its cap.</summary>
    public sealed class TowCableShapeTests
    {
        private const float MaxSag = 2.5f;

        [Test]
        public void ATautRope_HasNoSag()
        {
            Assert.AreEqual(0f, TowCableShape.Sag(6f, 6f, MaxSag));
            Assert.AreEqual(0f, TowCableShape.Sag(7f, 6f, MaxSag), "stretched past its length it only runs straight");
        }

        [Test]
        public void ASlackRope_SagsMore_TheMoreSlackItHas()
        {
            float little = TowCableShape.Sag(5.8f, 6f, MaxSag);
            float more = TowCableShape.Sag(5f, 6f, MaxSag);
            float most = TowCableShape.Sag(2f, 6f, MaxSag);
            Assert.Greater(little, 0f);
            Assert.Greater(more, little);
            Assert.Greater(most, more);
        }

        [Test]
        public void TheSag_IsTheRopesOwnLength_AndNeverMoreThanItsCap()
        {
            float sag = TowCableShape.Sag(5f, 6f, 10f);
            // A shallow parabola's length is the span plus 8 sag^2 / (3 span): the rope is as long as it is.
            Assert.AreEqual(6f, 5f + 8f * sag * sag / (3f * 5f), 1e-3f);
            Assert.AreEqual(MaxSag, TowCableShape.Sag(1f, 60f, MaxSag), "capped");
        }

        [Test]
        public void TheRope_RunsFromEndToEnd_SaggedInTheMiddleOnly()
        {
            var points = new Vector3[11];
            var from = new Vector3(0f, 1f, 0f);
            var to = new Vector3(0f, 1f, -4f);
            TowCableShape.Fill(from, to, 1f, points);
            Assert.AreEqual(from, points[0]);
            Assert.AreEqual(to, points[10]);
            Assert.AreEqual(0f, points[5].y, 1e-4f, "the middle hangs a whole sag below the line (1 m down from 1 m)");
            Assert.AreEqual(-2f, points[5].z, 1e-4f);
            for (int i = 1; i <= 5; i++)
            {
                Assert.LessOrEqual(points[i].y, points[i - 1].y + 1e-5f, "down to the middle");
            }
        }
    }
}
