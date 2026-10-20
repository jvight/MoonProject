using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class ReleasePanTests
    {
        private const float Frame = 1f / 60f;
        private const float Lead = 2.9f;
        private const float DishLeg = 3f;
        private const float KestrelLeg = 1.4f;
        private const float Sweep = 55f;
        private const float Slack = 18f;
        private const float StartBearing = 90f;
        private const float Rise = 10f;
        private const float HalfLife = 0.3f;

        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Relay = At(-134f, 200f, 8f);
        private static readonly Vector3 Dish = At(15f, 1500f, 120f);
        private static readonly Vector3 Kestrel = At(-32f, 900f, 20f);

        private static Vector3 At(float bearing, float distance, float height)
        {
            float rad = bearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * distance, height, Mathf.Cos(rad) * distance);
        }

        private static ReleasePan Started()
        {
            var pan = new ReleasePan();
            pan.Begin(Origin, StartBearing, Relay, Dish, Kestrel, Lead, DishLeg, KestrelLeg, Sweep, Rise, HalfLife, Slack);
            return pan;
        }

        private static float BearingTo(Vector3 point)
        {
            return Mathf.Atan2(point.x, point.z) * Mathf.Rad2Deg;
        }

        [Test]
        public void ItStartsWhereTheCameraAlreadyLooks_HeadingForTheRelayLamp()
        {
            ReleasePan pan = Started();

            Assert.IsTrue(pan.IsActive);
            Assert.AreEqual(0, pan.Target);
            Assert.AreEqual(StartBearing, Mathf.Repeat(BearingTo(pan.Subject), 360f), 0.5f, "no jump from the chase view");
        }

        [Test]
        public void ItNeverTurnsFasterThanItsRateAndNeverCuts()
        {
            ReleasePan pan = Started();
            float last = pan.Bearing;
            float fastest = 0f;
            for (float t = 0f; t < 9f; t += Frame)
            {
                pan.Step(Frame);
                fastest = Mathf.Max(fastest, Mathf.Abs(Mathf.DeltaAngle(last, pan.Bearing)) / Frame);
                last = pan.Bearing;
            }

            Assert.LessOrEqual(fastest, Sweep + 0.5f, "the pan never whips round");
        }

        [Test]
        public void ItReachesTheDishThenKestrelWithinTheViewsNineSeconds()
        {
            ReleasePan pan = Started();
            float atDish = -1f;
            float arrived = -1f;
            for (float t = 0f; t < 9f; t += Frame)
            {
                pan.Step(Frame);
                if (atDish < 0f && Mathf.Abs(Mathf.DeltaAngle(pan.Bearing, BearingTo(Dish))) < Slack + 7f)
                {
                    atDish = t;
                }

                if (arrived < 0f && pan.Arrived)
                {
                    arrived = t;
                }
            }

            Assert.Greater(atDish, 0f, "the pan came to the dish");
            Assert.LessOrEqual(atDish, 5.9f, "and was there when the dish glints");
            Assert.Greater(arrived, 0f, "the pan arrived at Kestrel");
            Assert.LessOrEqual(arrived, 7.4f, "leaving the camera room to ease back inside nine seconds");
        }

        [Test]
        public void ItIsOnTheRelayLampBeforeItsLeadEnds()
        {
            ReleasePan pan = Started();
            for (float t = 0f; t < Lead - 0.1f; t += Frame)
            {
                pan.Step(Frame);
            }

            Assert.AreEqual(0, pan.Target);
            Assert.AreEqual(0f, Mathf.DeltaAngle(pan.Bearing, BearingTo(Relay)), Slack + 7f, "the lamp is in frame");
        }

        [Test]
        public void AnEndedPanStaysStill()
        {
            ReleasePan pan = Started();
            pan.Step(1f);
            pan.End();
            Vector3 where = pan.Subject;
            pan.Step(1f);

            Assert.IsFalse(pan.IsActive);
            Assert.AreEqual(where, pan.Subject);
        }
    }
}
