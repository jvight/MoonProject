using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Map;
using MoonProject.Gameplay.Map;
using MoonProject.UI.Editor;
using MoonProject.UI.Map;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace MoonProject.UI.Tests.Map
{
    /// <summary>
    /// M3-07's map view: the paper maps the basin north up; strokes are tessellated once, deterministically, and
    /// chunked for 16-bit indices; opening the view shows ink only where the station both remembers a visit and
    /// reaches, pencil where it was visited out of reach, a "?" where it was only seen and nothing where it was never
    /// seen; a cued place inks and then names itself; the tally counts without a total; Ines's names and notes exist
    /// in both languages; and nothing on the paper points.
    /// </summary>
    public sealed class MapViewTests
    {
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";

        private EventBus _events;
        private InputActionAsset _asset;
        private InputReader _reader;
        private LocalizationService _localization;
        private MapSheet _sheet;
        private MapViewTuning _tuning;
        private Reach _reach;
        private Overlay _overlay;
        private ScanSites _scanSites;
        private MapKnowledge _knowledge;
        private VisualElement _pause;
        private MapView _view;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath));
            _reader = new InputReader(_asset);
            TextAsset[] assets = StringTableAssets.Load();
            var tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
            }

            _localization = new LocalizationService(_events, tables);
            _sheet = ScriptableObject.CreateInstance<MapSheet>();
            _sheet.Write(7, Vector2.zero, 500f, 5f,
                new[] { new Vector2(-400f, 0f), new Vector2(0f, 400f), new Vector2(400f, 0f) },
                new[] { new MapLine(MapLineKind.RimCrest, 0, 3, true, 40f) }, new string[0], new Vector2[0]);
            _tuning = ScriptableObject.CreateInstance<MapViewTuning>();
            _reach = new Reach();
            _overlay = new Overlay();
            _scanSites = new ScanSites();
            var places = new List<MapPlace>
            {
                new MapPlace(MapPlaces.HomeId, Vector3.zero, MapPlaceKind.Home, 20f),
                new MapPlace("site.depot", new Vector3(50f, 0f, 0f), MapPlaceKind.Site, 5f),
                new MapPlace("site.drill", new Vector3(0f, 0f, 200f), MapPlaceKind.Site, 5f),
                new MapPlace("relay.1", new Vector3(-150f, 0f, 0f), MapPlaceKind.Relay, 3f),
                new MapPlace("site.garage", new Vector3(0f, 0f, -120f), MapPlaceKind.Site, 5f),
                new MapPlace(WorldAnchorIds.RegionPeak, new Vector3(0f, 0f, 430f), MapPlaceKind.Region, 8f),
            };
            var probes = new List<MapPlaceProbe>();
            foreach (MapPlace place in places)
            {
                probes.Add(new MapPlaceProbe(null, null, null, null, place.Kind != MapPlaceKind.Region));
            }

            _knowledge = new MapKnowledge(_events, _reach, places, probes);
            _knowledge.Poll(Vector3.zero, 0f, false, false);
            _knowledge.MarkVisited(1);
            _knowledge.MarkVisited(2);
            _knowledge.MarkSeen(3);
            var root = new VisualElement();
            _pause = new VisualElement();
            root.Add(_pause);
            _view = new MapView(_pause, _sheet, _tuning, _knowledge, _reach, _overlay, _localization, _events,
                _reader, _scanSites);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Dispose();
            _reader.Dispose();
            Object.DestroyImmediate(_asset);
            Object.DestroyImmediate(_sheet);
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Paper_HasNorthUpAndTheBasinCentreInTheMiddle()
        {
            var space = new PaperSpace(new Vector2(10f, 20f), 100f, 1000f);
            Assert.AreEqual(new Vector2(500f, 500f), space.ToPaper(new Vector2(10f, 20f)));
            Assert.Less(space.ToPaper(new Vector2(10f, 70f)).y, 500f, "north is up the page");
            Assert.Greater(space.ToPaper(new Vector2(60f, 20f)).x, 500f, "east is right");
            Assert.AreEqual(5f, space.UnitsPerMetre, 1e-5f);
        }

        [Test]
        public void Strokes_AreAQuadPerSpan_Deterministic_AndChunkedUnderTheIndexLimit()
        {
            var points = new List<Vector2> { Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f) };
            var open = new StrokeBuffer();
            open.AddLine(points, false, 2f, Color.black, 1f, 5f, 0.3f, 3u);
            Assert.AreEqual(2, open.QuadCount);
            var closed = new StrokeBuffer();
            closed.AddLine(points, true, 2f, Color.black, 1f, 5f, 0.3f, 3u);
            Assert.AreEqual(3, closed.QuadCount);

            var many = new StrokeBuffer();
            var line = new List<Vector2>();
            for (int i = 0; i <= StrokeBuffer.ChunkVertices / 4 + 10; i++)
            {
                line.Add(new Vector2(i, i % 2));
            }

            many.AddLine(line, false, 1f, Color.black, 0f, 1f, 0f, 0u);
            many.Finish();
            Assert.AreEqual(2, many.ChunkCount);

            for (float x = 0f; x < 20f; x += 0.37f)
            {
                float noise = StrokeBuffer.Noise01(9u, x);
                Assert.That(noise, Is.InRange(0f, 1f));
                Assert.AreEqual(noise, StrokeBuffer.Noise01(9u, x));
            }
        }

        [Test]
        public void Opening_InksOnlyVisitedInReach_PencilsTheRest_AndShowsNothingUnseen()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, true));
            Assert.IsTrue(_view.IsOpen);

            MapMark home = Mark(MapPlaces.HomeId);
            MapMark depot = Mark("site.depot");
            MapMark drill = Mark("site.drill");
            MapMark mast = Mark("relay.1");
            MapMark garage = Mark("site.garage");
            Assert.IsFalse(home.Drawn, "the pen has not drawn anything yet: the first look inks it");
            Assert.IsTrue(home.PencilName);
            Assert.IsTrue(depot.PencilName, "visited in reach, waiting for the pen");
            Assert.IsTrue(drill.PencilName, "visited out of reach stays pencil");
            Assert.AreEqual(DisplayStyle.Flex, mast.Question.style.display.value, "seen: a pencil ?");
            Assert.AreEqual(DisplayStyle.None, garage.Name.style.display.value, "never seen: nothing");
            Assert.AreEqual(DisplayStyle.None, garage.Question.style.display.value);
        }

        [Test]
        public void ACuedPlace_InksThenWritesItsName()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, true));
            int drawn = _knowledge.InkedSinceLastLook;
            Assert.AreEqual(2, drawn, "home and the depot; the drill lies out of reach");
            var pen = new ChartPen(_events, _knowledge);
            pen.Begin(0f, 0f);
            pen.Tick(0f, 1f);

            MapMark home = Mark(MapPlaces.HomeId);
            Assert.IsTrue(home.Animating);
            _view.Tick(_tuning.InkSeconds * 0.5f, false);
            Assert.AreEqual(0.5f, home.Reveal, 0.01f);
            _view.Tick(_tuning.InkSeconds + _tuning.NameFadeSeconds, false);
            Assert.IsTrue(home.Drawn);
            Assert.AreEqual(DisplayStyle.Flex, home.Name.style.display.value);
            Assert.AreEqual(_tuning.Ink, home.Name.style.color.value);
            Assert.IsFalse(Mark("site.drill").Drawn);
        }

        [Test]
        public void TheTally_CountsFoundPlaces_WithNoTotal()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            Assert.AreEqual(2, _view.TallyStrokes, "the depot and the drill");
            Assert.IsFalse(_view.ShowsComplete);
        }

        [Test]
        public void TheFocus_RestsOnThePlaceAtTheViewsCentre()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            _view.Tick(0.1f, false);
            Assert.AreSame(Mark(MapPlaces.HomeId), _view.Focus);
        }

        [Test]
        public void AnOpenMap_AllocatesNothingPerFrame()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            var pen = new ChartPen(_events, _knowledge);
            pen.Begin(0f, 0f);
            pen.Tick(0f, 1f);
            for (int i = 0; i < 4; i++)
            {
                _view.Tick(0.016f, false);
            }

            TestDelegate frame = () => _view.Tick(0.016f, false);
            Assert.That(frame, Is.Not.AllocatingGCMemory(), "while the pen inks");
            _view.Tick(_tuning.InkSeconds + _tuning.NameFadeSeconds, false);
            frame();
            Assert.That(frame, Is.Not.AllocatingGCMemory(), "while it rests");
        }

        [Test]
        public void NothingOnThePaper_PointsAnywhere()
        {
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            var names = new List<string>();
            _view.Root.Query<VisualElement>().ForEach(element => names.Add(element.name ?? string.Empty));
            foreach (string name in names)
            {
                StringAssert.DoesNotContain("arrow", name);
                StringAssert.DoesNotContain("you-are-here", name);
                StringAssert.DoesNotContain("objective", name);
            }
        }

        [Test]
        public void EveryPlace_HasItsNameAndNote_InBothLanguages()
        {
            foreach (string language in new[] { "en", "vi" })
            {
                _localization.SetLanguage(language);
                foreach (MapMark mark in _view.Marks)
                {
                    string id = mark.Place.Id;
                    Assert.IsTrue(_localization.TryGet(MapKeys.Prefix + id + MapKeys.NameSuffix, out _), id);
                    Assert.IsTrue(_localization.TryGet(MapKeys.Prefix + id + MapKeys.NoteSuffix, out _), id);
                }
            }
        }

        private MapMark Mark(string id)
        {
            foreach (MapMark mark in _view.Marks)
            {
                if (mark.Place.Id == id)
                {
                    return mark;
                }
            }

            Assert.Fail($"no mark for {id}");
            return null;
        }

        [Test]
        public void OpeningTheView_CirclesEachCharger_AndDrawsTheLinkFromHomeTowardThePeak()
        {
            Assert.AreEqual(0, _overlay.ChargersRead, "nothing is read before the view opens");
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            Assert.AreEqual(_overlay.ChargerCount, _overlay.ChargersRead, "the dock and the lit mast are circled");
            Assert.AreEqual(_overlay.LinkPointCount, _overlay.LinkPointsRead, "the Link runs home, then the lit mast");
            _view.Tick(0.1f, false);
            Assert.IsTrue(_view.IsOpen);
        }

        [Test]
        public void ScanBrackets_DrawOnlyForKnownSitesWithFragmentsLeft_IncludingMicro_AndVanishWhenEmptied()
        {
            var space = new PaperSpace(_sheet.Centre, _sheet.Radius, _tuning.PaperSize);
            var posA = new Vector3(120f, 8f, -90f);
            var posB = new Vector3(0f, 0f, 200f);
            var posC = new Vector3(-200f, 0f, 150f);
            _scanSites.Add("micro.06", posA, known: true, left: 2, total: 2);
            _scanSites.Add("site.drill", posB, known: true, left: 0, total: 2);
            _scanSites.Add("site.kestrel", posC, known: false, left: 3, total: 3);
            int marksBefore = _view.Marks.Count;

            _events.Publish(new MapLookStarted(Vector3.zero, false));
            Assert.AreEqual(marksBefore, _view.Marks.Count, "micro.* adds no crew-map place or label");
            Assert.AreEqual(1, _view.BracketPositions.Count, "only known sites with Left > 0 get a pencil bracket (A)");
            Assert.AreEqual(space.ToPaper(posA), _view.BracketPositions[0]);
            Assert.AreEqual(6, _view.BracketStrokes, "two 3-segment bracket halves at A");

            _view.Tick(0.1f, false);
            int readsAfterOpen = _scanSites.Reads;
            TestDelegate frame = () => _view.Tick(0.1f, false);
            frame();
            Assert.That(frame, Is.Not.AllocatingGCMemory(), "no per-frame allocation in the look");
            Assert.AreEqual(readsAfterOpen, _scanSites.Reads, "nothing read per frame during the look");

            _scanSites.SetLeft("micro.06", 0);
            _events.Publish(new ScanSiteEmptied("micro.06", posA));
            Assert.AreEqual(0, _view.BracketPositions.Count, "ScanSiteEmptied during a look removes the bracket");
            Assert.AreEqual(0, _view.BracketStrokes);

            _events.Publish(new MapLookEnded());
            _events.Publish(new MapLookStarted(Vector3.zero, false));
            Assert.AreEqual(0, _view.BracketPositions.Count, "the next look draws none");
            Assert.AreEqual(0, _view.BracketStrokes);
        }

        private sealed class ScanSites : IScanSites
        {
            private readonly List<Site> _sites = new List<Site>();

            public int Reads { get; private set; }

            public int Count
            {
                get
                {
                    Reads++;
                    return _sites.Count;
                }
            }

            public void Add(string placeId, Vector3 position, bool known, int left, int total)
            {
                _sites.Add(new Site { PlaceId = placeId, Position = position, Known = known, Left = left, Total = total });
            }

            public void SetLeft(string placeId, int left)
            {
                for (int i = 0; i < _sites.Count; i++)
                {
                    if (_sites[i].PlaceId == placeId)
                    {
                        Site site = _sites[i];
                        site.Left = left;
                        _sites[i] = site;
                    }
                }
            }

            public string PlaceId(int index) => _sites[index].PlaceId;

            public Vector3 Position(int index) => _sites[index].Position;

            public bool IsKnown(int index) => _sites[index].Known;

            public int Left(int index) => _sites[index].Left;

            public int FragmentCount(int index) => _sites[index].Total;

            public int IndexOf(string placeId)
            {
                for (int i = 0; i < _sites.Count; i++)
                {
                    if (_sites[i].PlaceId == placeId)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public int ScannedOf(string kitId) => 0;

            public int SetSize(string kitId) => 0;

            private struct Site
            {
                public string PlaceId;
                public Vector3 Position;
                public bool Known;
                public int Left;
                public int Total;
            }
        }

        /// <summary>The dock and one lit chain mast west of home, the Peak to the north (M3-21).</summary>
        private sealed class Overlay : IChartOverlay
        {
            private static readonly Vector3 Mast = new Vector3(-150f, 0f, 0f);

            public int ChargersRead { get; private set; }

            public int LinkPointsRead { get; private set; }

            public int ChargerCount => 2;

            public float ChargerRadius => 140f;

            public int LinkPointCount => 2;

            public Vector3 Peak => new Vector3(0f, 0f, 430f);

            public Vector3 ChargerPosition(int index)
            {
                ChargersRead++;
                return index == 0 ? Vector3.zero : Mast;
            }

            public Vector3 LinkPoint(int index)
            {
                LinkPointsRead++;
                return index == 0 ? Vector3.zero : Mast;
            }
        }

        /// <summary>Home's circle of reach, 100 m.</summary>
        private sealed class Reach : IStationReach
        {
            public int LitCount => 1;

            public int NodeCount => 1;

            public bool IsInReach(Vector3 position)
            {
                return new Vector2(position.x, position.z).magnitude <= 100f;
            }

            public float DistanceToNearestNode(Vector3 position)
            {
                return new Vector2(position.x, position.z).magnitude;
            }

            public RelayNode GetNode(int index)
            {
                return new RelayNode("home", Vector3.zero, true, 100f, Vector3.zero);
            }
        }
    }
}
