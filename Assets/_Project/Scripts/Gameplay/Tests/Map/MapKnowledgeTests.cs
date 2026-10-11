using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;
using MoonProject.Gameplay.Map;

namespace MoonProject.Gameplay.Tests.Map
{
    /// <summary>
    /// M3-07's acceptance in logic: the first look inks home then every visited place in reach, in order; a newly lit
    /// mast inks only the places that just entered reach; nothing is inked that the save has not visited; seen and
    /// visited are published once per save, never for what a loaded save already shows; places beyond the rim are
    /// never visited; and the record survives a save.
    /// </summary>
    public sealed class MapKnowledgeTests
    {
        private const float Stroke = 0.6f;
        private const float Lead = 1f;

        private EventBus _events;
        private FakeReach _reach;
        private List<MapPlace> _places;
        private List<MapPlaceProbe> _probes;
        private List<string> _inked;
        private List<int> _orders;
        private List<string> _seen;
        private List<string> _visited;
        private bool _depotAnswered;
        private bool _drillCut;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _reach = new FakeReach();
            _reach.Add(Vector3.zero, 90f, true);
            _depotAnswered = false;
            _drillCut = false;
            _places = new List<MapPlace>
            {
                new MapPlace(MapPlaces.HomeId, Vector3.zero, MapPlaceKind.Home, 20f),
                new MapPlace("site.depot", new Vector3(50f, 0f, 0f), MapPlaceKind.Site, 5f),
                new MapPlace("site.drill", new Vector3(0f, 0f, 80f), MapPlaceKind.Site, 5f),
                new MapPlace("relay.1", new Vector3(-150f, 0f, 0f), MapPlaceKind.Relay, 3f),
                new MapPlace(WorldAnchorIds.RegionTerraces, new Vector3(0f, 0f, -300f), MapPlaceKind.Region, 6f),
            };
            _probes = new List<MapPlaceProbe>
            {
                new MapPlaceProbe(null, () => true, null, null),
                new MapPlaceProbe(() => _depotAnswered, null, null, null),
                new MapPlaceProbe(null, () => _drillCut, null, null),
                new MapPlaceProbe(null, null, null, null),
                new MapPlaceProbe(null, null, null, null, false),
            };
            _inked = new List<string>();
            _orders = new List<int>();
            _seen = new List<string>();
            _visited = new List<string>();
            _events.Subscribe<MapInkCued>(cue =>
            {
                _inked.Add(cue.PlaceId);
                _orders.Add(cue.Order);
            });
            _events.Subscribe<MapPlaceSeen>(seen => _seen.Add(seen.PlaceId));
            _events.Subscribe<MapPlaceVisited>(visited => _visited.Add(visited.PlaceId));
        }

        [Test]
        public void FirstLook_InksHomeThenEveryVisitedPlaceInReach_InOrder()
        {
            MapKnowledge map = NewMap();
            map.Poll(Vector3.zero, 0f, true, false);
            Drive(map, _places[1].Position);
            Drive(map, _places[3].Position);
            Assert.AreEqual(2, map.InkedSinceLastLook, "home and the depot; relay.1 lies out of reach");

            Look(map);

            CollectionAssert.AreEqual(new[] { MapPlaces.HomeId, "site.depot" }, _inked);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _orders);
            Assert.IsTrue(map.IsInkDrawn("site.depot"));
            Assert.IsFalse(map.IsInked("relay.1"));
            Assert.AreEqual(0, map.InkedSinceLastLook);
        }

        [Test]
        public void ANewlyLitMast_InksOnlyThePlacesThatJustEnteredReach()
        {
            MapKnowledge map = NewMap();
            map.Poll(Vector3.zero, 0f, true, false);
            Drive(map, _places[1].Position);
            Drive(map, _places[3].Position);
            Look(map);
            _inked.Clear();
            _orders.Clear();

            _reach.Add(new Vector3(-150f, 0f, 0f), 40f, true);
            Assert.AreEqual(1, map.InkedSinceLastLook);
            Look(map);

            CollectionAssert.AreEqual(new[] { "relay.1" }, _inked);
            CollectionAssert.AreEqual(new[] { 0 }, _orders, "each look's pen counts its strokes from the bottom note");
        }

        [Test]
        public void NothingUnvisited_IsEverInked()
        {
            MapKnowledge map = NewMap();
            map.Poll(Vector3.zero, 0f, true, false);
            _depotAnswered = true;
            map.Poll(Vector3.zero, 0f, true, true);
            Assert.AreEqual(MapPlaceState.Seen, map.GetState("site.depot"));
            Assert.AreEqual(MapPlaceState.Sketched, map.GetState("site.drill"));

            Look(map);

            CollectionAssert.AreEqual(new[] { MapPlaces.HomeId }, _inked);
            Assert.IsFalse(map.IsInked("site.depot"), "seen in reach is still only pencil");
            Assert.IsFalse(map.IsInked("site.drill"));
        }

        [Test]
        public void SeenAndVisited_ArePublishedOnce_AndNeverForWhatALoadedSaveShows()
        {
            MapKnowledge map = NewMap();
            _drillCut = true;
            map.Poll(Vector3.zero, 0f, true, false);
            Assert.AreEqual(MapPlaceState.Visited, map.GetState("site.drill"), "an older save's cut drill is visited");
            Assert.AreEqual(MapPlaceState.Visited, map.GetState(MapPlaces.HomeId));
            CollectionAssert.IsEmpty(_visited, "catching up with the save is not news");

            _depotAnswered = true;
            map.Poll(Vector3.zero, 0f, true, true);
            map.Poll(Vector3.zero, 0f, true, true);
            Drive(map, _places[1].Position);
            Drive(map, _places[1].Position);

            CollectionAssert.AreEqual(new[] { "site.depot" }, _seen);
            CollectionAssert.AreEqual(new[] { "site.depot" }, _visited);
        }

        [Test]
        public void PlacesBeyondTheRim_AreNeverVisited()
        {
            MapKnowledge map = NewMap();
            Drive(map, _places[4].Position);
            Assert.AreEqual(MapPlaceState.Sketched, map.GetState(WorldAnchorIds.RegionTerraces));

            Assert.IsTrue(map.MarkSeen(4), "the evening pillar can still ring it");
            Assert.IsFalse(map.MarkVisited(4));
            Assert.AreEqual(MapPlaceState.Seen, map.GetState(WorldAnchorIds.RegionTerraces));
        }

        [Test]
        public void TheRecord_SurvivesASave_AndUnknownIdsAreIgnored()
        {
            MapKnowledge map = NewMap();
            map.Poll(Vector3.zero, 0f, true, false);
            Drive(map, _places[1].Position);
            _depotAnswered = true;
            Look(map);
            map.MarkSeen(2);
            MapSaveData data = map.Capture(true);
            data.visited = new List<string>(data.visited) { "site.gone" }.ToArray();

            MapKnowledge loaded = NewMap();
            loaded.Restore(data);

            for (int i = 0; i < _places.Count; i++)
            {
                Assert.AreEqual(map.GetState(i), loaded.GetState(i), _places[i].Id);
                Assert.AreEqual(map.IsInkDrawn(_places[i].Id), loaded.IsInkDrawn(_places[i].Id), _places[i].Id);
            }

            Assert.IsTrue(data.hatchOpen);
            Assert.AreEqual(0, loaded.InkedSinceLastLook);
        }

        [Test]
        public void Queries_DoNotKnowPlacesOffTheMap()
        {
            MapKnowledge map = NewMap();
            Assert.AreEqual(MapPlaceState.Sketched, map.GetState("site.nowhere"));
            Assert.IsFalse(map.IsInked("site.nowhere"));
            Assert.AreEqual(MapPlaceDeeds.None, map.GetDeeds("site.nowhere"));
            Assert.IsNull(map.GetStateLineKey(null));
            Assert.Throws<ArgumentException>(() => new MapKnowledge(_events, _reach,
                new[] { _places[0], _places[0] }, new[] { _probes[0], _probes[0] }));
        }

        private MapKnowledge NewMap()
        {
            return new MapKnowledge(_events, _reach, _places, _probes);
        }

        private static void Drive(MapKnowledge map, Vector3 rover)
        {
            map.Poll(rover, 0f, false, true);
        }

        /// <summary>A whole look: the pen draws until it rests.</summary>
        private void Look(MapKnowledge map)
        {
            var pen = new ChartPen(_events, map);
            float now = 0f;
            pen.Begin(now, Lead);
            for (int frame = 0; frame < 1000 && (pen.IsDrawing || now < Lead); frame++)
            {
                now += Stroke * 0.25f;
                pen.Tick(now, Stroke);
            }

            pen.Stop();
        }

        /// <summary>Home and masts as circles of reach, lit or not.</summary>
        internal sealed class FakeReach : IStationReach
        {
            private readonly List<Vector3> _positions = new List<Vector3>();
            private readonly List<float> _radii = new List<float>();
            private readonly List<bool> _lit = new List<bool>();

            public int LitCount => _lit.FindAll(lit => lit).Count;

            public int NodeCount => _positions.Count;

            public void Add(Vector3 position, float radius, bool lit)
            {
                _positions.Add(position);
                _radii.Add(radius);
                _lit.Add(lit);
            }

            public bool IsInReach(Vector3 position)
            {
                for (int i = 0; i < _positions.Count; i++)
                {
                    Vector3 d = position - _positions[i];
                    d.y = 0f;
                    if (_lit[i] && d.magnitude <= _radii[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            public float DistanceToNearestNode(Vector3 position)
            {
                return 0f;
            }

            public RelayNode GetNode(int index)
            {
                return new RelayNode(index == 0 ? "home" : "relay." + index, _positions[index], _lit[index],
                    _radii[index], _positions[index]);
            }
        }
    }
}
