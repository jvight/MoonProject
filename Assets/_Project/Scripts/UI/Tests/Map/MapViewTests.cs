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
            _view = new MapView(_pause, _sheet, _tuning, _knowledge, _reach, _localization, _events, _reader);
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
