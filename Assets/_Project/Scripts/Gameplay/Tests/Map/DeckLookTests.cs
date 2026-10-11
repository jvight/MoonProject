using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using MoonProject.Core;
using MoonProject.Core.Map;
using MoonProject.Gameplay.Map;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace MoonProject.Gameplay.Tests.Map
{
    /// <summary>
    /// 07's time on the deck (M3-07 steps 1 and 4, amendment 4; cutscenes §6): the first ride sweeps, swings the hatch
    /// open and lets the lookout breathe before the map; later rides open onto the map after the sweep; driving
    /// leaves at any point and the deck is not taken again until the lift has moved; and the sweep blinks the
    /// unvisited places in view clockwise from where 07 faces, never the places beyond the rim.
    /// </summary>
    public sealed class DeckLookTests
    {
        private MapTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<MapTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void FirstRide_Sweeps_OpensTheHatch_Breathes_ThenLooks()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(false);
            Assert.AreEqual(DeckLookStep.None, deck.Step(Input(false, false, 0f, 0f)));
            Assert.AreEqual(DeckLookStep.SweepStarted, deck.Step(Input(true, false, 0f, 1f)));
            Assert.IsTrue(deck.HoldsDeck, "look input belongs to the lookout now, not to the lift");
            Assert.AreEqual(DeckLookStep.None, deck.Step(Input(true, false, 5f, 2f)));
            Assert.AreEqual(DeckLookStep.HatchOpened, deck.Step(Input(true, true, 6f, 3f)));
            Assert.IsTrue(deck.HatchOpen);

            float breathe = _tuning.BreatheSeconds;
            Assert.AreEqual(DeckLookStep.None, deck.Step(Input(true, true, 60f, 3f + breathe * 0.5f)),
                "07 was already still through the sweep: the breath still takes its time");
            Assert.AreEqual(DeckLookStep.LookStarted, deck.Step(Input(true, true, 60f, 3f + breathe)));
            Assert.IsTrue(deck.FirstLook);
            Assert.AreEqual(DeckLookStep.LookEnded, deck.Step(Input(true, true, 0f, 20f, leave: true)));
            Assert.IsFalse(deck.HoldsDeck);
            Assert.IsFalse(deck.FirstLook);
        }

        [Test]
        public void FirstRide_LookingAtTheHatch_OpensTheMapAtOnce()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(false);
            deck.Step(Input(true, false, 0f, 0f));
            deck.Step(Input(true, true, 0f, 1f));
            Assert.AreEqual(DeckLookPhase.Breathing, deck.Phase);
            Assert.AreEqual(DeckLookStep.LookStarted, deck.Step(Input(true, true, 0f, 1.1f, atHatch: true)));
        }

        [Test]
        public void LaterRides_OpenOntoTheMapAfterTheSweep()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(true);
            Assert.AreEqual(DeckLookStep.SweepStarted, deck.Step(Input(true, false, 0f, 0f)));
            Assert.AreEqual(DeckLookStep.LookStarted, deck.Step(Input(true, true, 0f, 1f)));
            Assert.IsFalse(deck.FirstLook);
        }

        [Test]
        public void Driving_LeavesAtAnyPoint_AndTheDeckWaitsForTheLiftToMove()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(false);
            deck.Step(Input(true, false, 0f, 0f));
            Assert.AreEqual(DeckLookStep.Released, deck.Step(Input(true, false, 0f, 1f, leave: true)));
            Assert.IsFalse(deck.HatchOpen, "leaving mid-sweep keeps the hatch's first opening for next time");
            Assert.AreEqual(DeckLookStep.None, deck.Step(Input(true, false, 0f, 2f)), "still on the deck");
            Assert.AreEqual(DeckLookStep.None, deck.Step(Input(false, false, 0f, 3f)));
            Assert.AreEqual(DeckLookStep.SweepStarted, deck.Step(Input(true, false, 0f, 30f)), "the next ride");
        }

        [Test]
        public void TheLiftMoving_EndsALook()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(true);
            deck.Step(Input(true, false, 0f, 0f));
            deck.Step(Input(true, true, 0f, 1f));
            Assert.AreEqual(DeckLookStep.LookEnded, deck.Step(Input(false, true, 0f, 2f)));
        }

        [Test]
        public void Sweep_BlinksUnvisitedPlacesInView_ClockwiseFromTheFacing_NeverBeyondTheRim()
        {
            var events = new EventBus();
            var reach = new MapKnowledgeTests.FakeReach();
            reach.Add(Vector3.zero, 50f, true);
            var places = new List<MapPlace>
            {
                new MapPlace(MapPlaces.HomeId, Vector3.zero, MapPlaceKind.Home, 20f),
                new MapPlace("west", new Vector3(-100f, 0f, 0f), MapPlaceKind.Site, 5f),
                new MapPlace("east", new Vector3(100f, 0f, 0f), MapPlaceKind.Site, 5f),
                new MapPlace("south", new Vector3(0f, 0f, -100f), MapPlaceKind.Site, 5f),
                new MapPlace("hidden", new Vector3(0f, 0f, 100f), MapPlaceKind.Site, 5f),
                new MapPlace(WorldAnchorIds.RegionPeak, new Vector3(10f, 0f, 400f), MapPlaceKind.Region, 5f),
            };
            var probes = new List<MapPlaceProbe>();
            foreach (MapPlace place in places)
            {
                probes.Add(new MapPlaceProbe(null, null, null, null, place.Kind != MapPlaceKind.Region));
            }

            var map = new MapKnowledge(events, reach, places, probes);
            map.Poll(Vector3.zero, 0f, true, false);
            map.MarkSeen(3);
            var sweep = new LookoutSweep();
            var sightline = new BlockedAt(new Vector3(0f, 0f, 100f));

            sweep.Begin(map, sightline, Vector3.up * 14f, Vector3.right, true, _tuning, 0f, 1f);
            CollectionAssert.AreEqual(new[] { "east", "south", "west" }, Blinks(map, sweep));

            sweep.Begin(map, sightline, Vector3.up * 14f, Vector3.right, false, _tuning, 0f, 1f);
            CollectionAssert.AreEqual(new[] { "east", "west" }, Blinks(map, sweep), "later rides: unseen places only");
        }

        [Test]
        public void TheDeck_AllocatesNothingPerFrame()
        {
            var deck = new DeckLook(_tuning);
            deck.Restore(true);
            var sweep = new LookoutSweep();
            DeckLookInput input = Input(true, false, 0f, 0f);
            deck.Step(input);
            TestDelegate step = () => deck.Step(input);
            TestDelegate blink = () => sweep.Tick(1f, 1f);
            step();
            blink();
            Assert.That(step, Is.Not.AllocatingGCMemory());
            Assert.That(blink, Is.Not.AllocatingGCMemory());
        }

        private List<string> Blinks(MapKnowledge map, LookoutSweep sweep)
        {
            var ids = new List<string>();
            for (float now = 0f; now < 60f && !sweep.IsFinished(now); now += 0.1f)
            {
                int index = sweep.Tick(now, _tuning.SweepInterval);
                if (index >= 0)
                {
                    ids.Add(map.GetPlace(index).Id);
                }
            }

            return ids;
        }

        private static DeckLookInput Input(bool atDeck, bool sweepDone, float still, float now, bool leave = false,
            bool atHatch = false)
        {
            return new DeckLookInput(atDeck, sweepDone, atHatch, still, leave, now);
        }

        /// <summary>Sees everything but the one target behind a ridge.</summary>
        private sealed class BlockedAt : ISightline
        {
            private readonly Vector3 _hidden;

            public BlockedAt(Vector3 hidden)
            {
                _hidden = hidden;
            }

            public bool IsClear(Vector3 eye, Vector3 target, float targetRadius, float step, float clearance)
            {
                return new Vector2(target.x - _hidden.x, target.z - _hidden.z).sqrMagnitude > 1f;
            }
        }
    }
}
