using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class GazePanTests
    {
        private const float Frame = 1f / 60f;
        private const float Travel = 1.8f;
        private const float Hold = 2.2f;
        private const float Sweep = 22f;
        private const float Rise = 8f;
        private const float HalfLife = 0.5f;
        private const float Slack = 15f;
        private const float StartBearing = 150f;

        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Relay = At(-134f, 200f, 8f);

        private static Vector3 At(float bearing, float distance, float height)
        {
            float rad = bearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * distance, height, Mathf.Cos(rad) * distance);
        }

        private static GazePan Started(float startBearing)
        {
            var pan = new GazePan();
            pan.Begin(Origin, startBearing, Relay, Travel, Hold, Sweep, Rise, HalfLife, Slack);
            return pan;
        }

        private static float Bearing(Vector3 point)
        {
            return Mathf.Repeat(Mathf.Atan2(point.x, point.z) * Mathf.Rad2Deg, 360f);
        }

        [Test]
        public void ItStartsWhereTheCameraAlreadyLooks()
        {
            GazePan pan = Started(StartBearing);

            Assert.IsTrue(pan.IsActive);
            Assert.AreEqual(StartBearing, Bearing(pan.Subject), 0.5f, "no jump from the chase view");
        }

        [Test]
        public void ItNeverTurnsFasterThanItsRate_AndNeverCuts()
        {
            GazePan pan = Started(StartBearing);
            float last = pan.Bearing;
            float fastest = 0f;
            for (float t = 0f; t < 6f; t += Frame)
            {
                pan.Step(Frame);
                fastest = Mathf.Max(fastest, Mathf.Abs(Mathf.DeltaAngle(last, pan.Bearing)) / Frame);
                last = pan.Bearing;
            }

            Assert.LessOrEqual(fastest, Sweep + 0.5f, "the pan never whips round");
        }

        [Test]
        public void ItTurnsTheShortWay_ToWithinItsSlackOfTheLamp()
        {
            GazePan pan = Started(-100f);
            for (float t = 0f; t < Travel + Hold; t += Frame)
            {
                pan.Step(Frame);
            }

            float gap = Mathf.Abs(Mathf.DeltaAngle(pan.Bearing, Bearing(Relay)));
            Assert.LessOrEqual(gap, Slack + 3f, "it settles a third of the frame off the lamp, not past it");
            Assert.GreaterOrEqual(gap, Slack - 8f, "and not on top of it");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(-100f, pan.Bearing)), 40f, "from -100 to -134 is a short way");
        }

        [Test]
        public void ItHoldsThroughItsTravelAndItsHold_ThenHasArrived()
        {
            GazePan pan = Started(StartBearing);
            for (float t = 0f; t < Travel + Hold - 0.2f; t += Frame)
            {
                pan.Step(Frame);
            }

            Assert.IsFalse(pan.Arrived, "still holding the lamp");
            for (float t = 0f; t < 0.5f; t += Frame)
            {
                pan.Step(Frame);
            }

            Assert.IsTrue(pan.Arrived, "the camera may let go");
        }

        [Test]
        public void AnEndedPanStaysStill()
        {
            GazePan pan = Started(StartBearing);
            pan.Step(1f);
            pan.End();
            Vector3 where = pan.Subject;
            pan.Step(1f);

            Assert.IsFalse(pan.IsActive);
            Assert.AreEqual(where, pan.Subject);
        }

        [Test]
        public void TheBearingTo_IsTheCompassBearing()
        {
            Assert.AreEqual(90f, GazePan.BearingTo(Vector3.zero, new Vector3(5f, 3f, 0f)), 0.01f);
            Assert.AreEqual(180f, GazePan.BearingTo(new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -4f)), 0.01f);
        }
    }
}
