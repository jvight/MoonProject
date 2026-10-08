using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>Easing, flight and glide curves: start where they say, end where they say, never jump.</summary>
    public sealed class MotionTests
    {
        [Test]
        public void Eases_AreAnchoredAtZeroAndOne()
        {
            Assert.AreEqual(0f, Ease.InOutSine(0f), 1e-6f);
            Assert.AreEqual(1f, Ease.InOutSine(1f), 1e-6f);
            Assert.AreEqual(1f, Ease.OutCubic(1f), 1e-6f);
            Assert.AreEqual(1f, Ease.InOutCubic(1f), 1e-6f);
            Assert.AreEqual(1f, Ease.OutBack(1f, 1.5f), 1e-6f);
            Assert.AreEqual(0f, Ease.Hump(0f), 1e-6f);
            Assert.AreEqual(1f, Ease.Hump(0.5f), 1e-6f);
            Assert.AreEqual(0f, Ease.Hump(1f), 1e-6f);
        }

        [Test]
        public void OutBack_OvershootsThenSettles()
        {
            float peak = 0f;
            for (int i = 0; i <= 100; i++)
            {
                peak = Mathf.Max(peak, Ease.OutBack(i / 100f, 1.4f));
            }

            Assert.Greater(peak, 1.02f);
            Assert.Less(peak, 1.2f, "a soft overshoot, not a bounce");
        }

        [TestCase(0f)]
        [TestCase(0.3f)]
        [TestCase(0.75f)]
        [TestCase(1f)]
        public void InverseOutQuad_UndoesOutQuad(float value)
        {
            Assert.AreEqual(value, Ease.OutQuad(Ease.InverseOutQuad(value)), 1e-5f);
        }

        [Test]
        public void Damp_IsFrameRateIndependent()
        {
            float coarse = 0f;
            for (int i = 0; i < 10; i++)
            {
                coarse = Damp.Toward(coarse, 1f, 0.5f, 0.1f);
            }

            float fine = 0f;
            for (int i = 0; i < 100; i++)
            {
                fine = Damp.Toward(fine, 1f, 0.5f, 0.01f);
            }

            Assert.AreEqual(coarse, fine, 1e-4f);
            Assert.AreEqual(1f - Mathf.Exp(-2f), fine, 1e-4f);
        }

        [Test]
        public void PickupFlight_LeavesTheRestPoseAndMeetsTheSocketSmoothly()
        {
            var start = new Vector3(2f, 0.5f, 3f);
            var target = new Vector3(0f, 1.1f, -0.4f);
            Vector3 first = PickupFlight.Evaluate(start, target, 0f, 0.7f, 1f, 0.9f, 0.6f, 1.25f);
            Vector3 last = PickupFlight.Evaluate(start, target, 1f, 0.7f, 1f, 0.9f, 0.6f, 1.25f);
            Assert.Less(Vector3.Distance(first, start), 1e-4f);
            Assert.Less(Vector3.Distance(last, target), 1e-4f);

            Vector3 previous = first;
            float maxStep = 0f;
            for (int i = 1; i <= 200; i++)
            {
                Vector3 point = PickupFlight.Evaluate(start, target, i / 200f, 0.7f, 1f, 0.9f, 0.6f, 1.25f);
                maxStep = Mathf.Max(maxStep, Vector3.Distance(point, previous));
                previous = point;
            }

            Assert.Less(maxStep, 0.1f, "no jumps along the path (200 steps over ~4 m)");
            Assert.Less(Vector3.Distance(PickupFlight.Evaluate(start, target, 0.01f, 0.7f, 1f, 0.9f, 0.6f, 1.25f),
                start), 0.01f, "eased out of the rest pose");
        }

        [Test]
        public void PickupFlight_LiftsAndSpiralsMidFlight()
        {
            var start = Vector3.zero;
            var target = new Vector3(4f, 0f, 0f);
            Vector3 middle = PickupFlight.Evaluate(start, target, 0.45f, 0f, 1f, 0.9f, 0.6f, 1.25f);
            Vector3 straight = Vector3.Lerp(start, target, Ease.InOutCubic(0.45f));
            Assert.Greater(Vector3.Distance(middle, straight), 0.5f, "it does not fly in a straight line");
            Assert.AreEqual(0.6f, PickupFlight.Duration(0f, 0.6f, 0.06f), 1e-6f);
            Assert.AreEqual(0.9f, PickupFlight.Duration(5f, 0.6f, 0.06f), 1e-6f);
        }

        [Test]
        public void GlidePath_ArrivesWithASoftSettleBelowAndBack()
        {
            var start = new Vector3(5f, 1f, 0f);
            var end = new Vector3(0f, 2f, 0f);
            Assert.Less(Vector3.Distance(GlidePath.Position(start, end, 0f, 0.9f, 0.25f, 1.4f), start), 1e-4f);
            Assert.Less(Vector3.Distance(GlidePath.Position(start, end, 1f, 0.9f, 0.25f, 1.4f), end), 1e-4f);
            Vector3 glideEnd = GlidePath.Position(start, end, GlidePath.GlideShare, 0.9f, 0.25f, 1.4f);
            Assert.AreEqual(end.y + 0.25f, glideEnd.y, 1e-3f, "it glides to a point above the slot first");

            float lowest = float.MaxValue;
            for (int i = 0; i <= 100; i++)
            {
                float p = Mathf.Lerp(GlidePath.GlideShare, 1f, i / 100f);
                lowest = Mathf.Min(lowest, GlidePath.Position(start, end, p, 0.9f, 0.25f, 1.4f).y);
            }

            Assert.Less(lowest, end.y, "the settle dips a touch past the slot");
            Assert.Greater(lowest, end.y - 0.06f, "only a touch");
        }

        [Test]
        public void MarkerPillar_RisesHoldsAndFadesWithinItsLifetime()
        {
            Assert.AreEqual(0f, MarkerEnvelope.Pillar(0f, 20f, 0.6f, 6f), 1e-6f);
            Assert.AreEqual(1f, MarkerEnvelope.Pillar(5f, 20f, 0.6f, 6f), 1e-6f);
            Assert.Greater(MarkerEnvelope.Pillar(16f, 20f, 0.6f, 6f), 0.1f, "still visible near the end");
            Assert.Less(MarkerEnvelope.Pillar(19.5f, 20f, 0.6f, 6f), 0.05f);
            Assert.AreEqual(0f, MarkerEnvelope.Pillar(20f, 20f, 0.6f, 6f), 1e-6f);
            Assert.AreEqual(0f, MarkerEnvelope.Pulse(5f, 4f), 1e-6f);
            Assert.Greater(MarkerEnvelope.Pulse(0.4f, 4f), 0.9f);
        }

        [Test]
        public void SurfaceRules_BearingsRoundTrip()
        {
            Vector3 east = SurfaceRules.BearingDirection(90f);
            Assert.AreEqual(1f, east.x, 1e-5f);
            Assert.AreEqual(0f, east.z, 1e-5f);
            Assert.AreEqual(-40f, SurfaceRules.Bearing(SurfaceRules.BearingDirection(-40f)), 1e-3f);
        }
    }
}
