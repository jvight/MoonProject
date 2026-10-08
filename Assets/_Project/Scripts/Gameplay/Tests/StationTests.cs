using System;
using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The logic of how 07 works with the base's machines (docs/features/M3-14): feeding a hopper, the tower's service
    /// port moment, purchases waiting their turn and resting on the charging dock.
    /// </summary>
    public sealed class StationTests
    {
        private const float Frame = 1f / 60f;

        private static readonly FeedLook Feed = new FeedLook(0.15f, 0.55f, 0.15f, 0.45f, 0.25f);

        [Test]
        public void HopperFeed_TakesALeadThenOneFlightPerMaterialABeatApart()
        {
            Assert.AreEqual(0.15f + 0.55f, HopperFeed.DurationFor(0, Feed), 1e-5f, "a free recipe still beams briefly");
            Assert.AreEqual(0.15f + 0.55f, HopperFeed.DurationFor(1, Feed), 1e-5f);
            Assert.AreEqual(0.15f + 2f * 0.15f + 0.55f, HopperFeed.DurationFor(3, Feed), 1e-5f);
            Assert.AreEqual(1f, HopperFeed.DurationFor(3, Feed), 1e-5f, "about a second for a full recipe");
        }

        [Test]
        public void HopperFeed_BundlesPopOutOf07_FlyFullSize_AndShrinkIntoTheMouth()
        {
            Assert.AreEqual(0f, HopperFeed.FlightProgress(-0.1f, Feed));
            Assert.AreEqual(0.5f, HopperFeed.FlightProgress(0.275f, Feed), 1e-5f);
            Assert.AreEqual(1f, HopperFeed.FlightProgress(2f, Feed));
            Assert.AreEqual(0f, HopperFeed.BundleSize(0f, Feed), 1e-5f, "never appears at full size");
            Assert.AreEqual(1f, HopperFeed.BundleSize(0.5f, Feed), 1e-5f);
            Assert.AreEqual(0f, HopperFeed.BundleSize(1f, Feed), 1e-5f, "never vanishes at full size");
            Assert.Less(HopperFeed.BundleSize(0.9f, Feed), HopperFeed.BundleSize(0.8f, Feed), "it shrinks in");
            Assert.Greater(HopperFeed.BundleSize(0.1f, Feed), HopperFeed.BundleSize(0.05f, Feed), "it pops out");
        }

        [Test]
        public void TowerPort_HatchOpens_BeamStitchesUpTheTower_ThenTheHatchShuts()
        {
            const float hatch = 0.45f;
            float stitch = TowerPortMoment.StitchDuration(0.7f, 1.3f);
            Assert.AreEqual(2f, stitch, 1e-5f, "the stitch lasts as long as the flare and the growth");
            Assert.AreEqual(2.9f, TowerPortMoment.Duration(hatch, stitch), 1e-5f);
            Assert.AreEqual(0f, TowerPortMoment.Openness(-1f, hatch, stitch), "shut until the hopper is fed");
            Assert.AreEqual(0f, TowerPortMoment.Openness(0f, hatch, stitch), 1e-5f);
            Assert.AreEqual(0.5f, TowerPortMoment.Openness(hatch * 0.5f, hatch, stitch), 1e-5f, "it eases open");
            Assert.AreEqual(1f, TowerPortMoment.Openness(hatch + stitch * 0.5f, hatch, stitch), 1e-5f);
            Assert.AreEqual(0.5f, TowerPortMoment.Openness(hatch * 1.5f + stitch, hatch, stitch), 1e-5f);
            Assert.AreEqual(0f, TowerPortMoment.Openness(2f * hatch + stitch, hatch, stitch), 1e-5f, "shut again");
            Assert.IsFalse(TowerPortMoment.Stitching(hatch * 0.9f, hatch, stitch), "not before it is open");
            Assert.IsTrue(TowerPortMoment.Stitching(hatch, hatch, stitch));
            Assert.IsFalse(TowerPortMoment.Stitching(hatch + stitch, hatch, stitch), "not once it closes");

            float previous = -1f;
            for (float t = 0f; t <= TowerPortMoment.Duration(hatch, stitch); t += Frame)
            {
                float climb = TowerPortMoment.Climb(t, hatch, stitch);
                Assert.GreaterOrEqual(climb, previous, "the stitch only climbs");
                previous = climb;
            }

            Assert.AreEqual(0f, TowerPortMoment.Climb(hatch, hatch, stitch), 1e-5f, "from the hatch");
            Assert.AreEqual(1f, TowerPortMoment.Climb(hatch + stitch, hatch, stitch), 1e-5f, "to the beacon");
        }

        [Test]
        public void PurchaseQueue_KeepsPurchasesInOrder_RoundAndRound()
        {
            var queue = new PurchaseQueue(2);
            queue.Enqueue("rover.hover_jump", 1);
            queue.Enqueue("rover.cargo_cradle", 1);
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue("rover.boost_coils", 1),
                "never more than the station sells");
            Assert.IsTrue(queue.TryDequeue(out string id, out int level));
            Assert.AreEqual("rover.hover_jump", id);
            Assert.AreEqual(1, level);
            queue.Enqueue("radio_tower", 2);
            Assert.IsTrue(queue.TryDequeue(out id, out _));
            Assert.AreEqual("rover.cargo_cradle", id);
            Assert.IsTrue(queue.TryDequeue(out id, out level));
            Assert.AreEqual("radio_tower", id);
            Assert.AreEqual(2, level);
            Assert.IsFalse(queue.TryDequeue(out id, out _));
            Assert.IsNull(id);
            queue.Enqueue("radio_tower", 3);
            queue.Clear();
            Assert.AreEqual(0, queue.Count, "a load forgets what was waiting");
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PurchaseQueue(0));
        }

        [Test]
        public void DockRest_StoppedOnTheDockWithNoInput_RestsAfterAMoment()
        {
            var rest = new DockRest(1.3f, 0.3f, 1.2f, 0.08f);
            int changes = Run(rest, 1.1f, 0.5f, 0.2f, 0f);
            Assert.AreEqual(0, changes, "not before the moment has passed");
            Assert.IsFalse(rest.Docked);
            changes = Run(rest, 0.2f, 0.5f, 0.2f, 0f);
            Assert.AreEqual(1, changes);
            Assert.IsTrue(rest.Docked);
            Assert.AreEqual(0f, rest.StillFor);
            Assert.AreEqual(0, Run(rest, 5f, 0.5f, 0f, 0.05f), "input inside the dead zone keeps it resting");
            Assert.IsTrue(rest.Docked);
        }

        [Test]
        public void DockRest_IsNeverForced_AnyInputOrMovingOffEndsIt()
        {
            var rest = new DockRest(1.3f, 0.3f, 1.2f, 0.08f);
            Assert.AreEqual(0, Run(rest, 5f, 0.5f, 2f, 0f), "rolling across the dock never rests");
            Assert.AreEqual(0, Run(rest, 5f, 0.5f, 0f, 0.5f), "nor holding the stick");
            Assert.AreEqual(0, Run(rest, 5f, 2f, 0f, 0f), "nor stopping beside it");
            Run(rest, 1.3f, 0.5f, 0f, 0f);
            Assert.IsTrue(rest.Docked);
            Assert.IsTrue(rest.Step(0.5f, 0f, 0.3f, Frame), "any drive input leaves at once");
            Assert.IsFalse(rest.Docked);
            Run(rest, 0.6f, 0.5f, 0f, 0f);
            Assert.IsFalse(rest.Docked, "the moment starts over after leaving");
            Run(rest, 0.7f, 0.5f, 0f, 0f);
            Assert.IsTrue(rest.Docked);
            Assert.IsTrue(rest.Step(1.5f, 0f, 0f, Frame), "moved off the dock: the rest ends");
            Assert.IsFalse(rest.Docked);
        }

        /// <summary>Steps the rest for <paramref name="seconds"/> at 60 fps; returns how often it changed.</summary>
        private static int Run(DockRest rest, float seconds, float distance, float speed, float input)
        {
            int changes = 0;
            for (float t = 0f; t < seconds - Frame * 0.5f; t += Frame)
            {
                changes += rest.Step(distance, speed, input, Frame) ? 1 : 0;
            }

            return changes;
        }
    }
}
