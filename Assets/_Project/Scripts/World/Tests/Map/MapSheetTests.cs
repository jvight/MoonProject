using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Map;
using MoonProject.World.Map;

namespace MoonProject.World.Tests.Map
{
    /// <summary>
    /// M3-07's sheet and sightline: the marching squares trace closed and open iso-lines where the field crosses, the
    /// thinning keeps corners and drops straight runs, the shipped world's sheet holds every kind of line inside the
    /// paper within the draw budget, the two places beyond the rim are anchored, and the lookout's sightline sees
    /// the mound's relay from the deck but not into the canyon's terminus.
    /// </summary>
    public sealed class MapSheetTests
    {
        // The map view draws every point as a quad at 60 fps: keep the sheet well under the UI mesh budget.
        private const int MaxSheetPoints = 60000;
        private const float DeckEyeHeight = 18f;
        private const float LightHeight = 2f;
        private const float SightStep = 2f;
        private const float SightClearance = 0.5f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private WorldAnchors _anchors;
        private MapSheetTuning _tuning;
        private MapSheet _sheet;

        [OneTimeSetUp]
        public void TraceWorld()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, _settings.Canyon, new RelaySettings());
            _surface.AttachCraterField(new CraterField(_settings.CraterField, WorldSettings.DefaultSeed,
                _settings.FloorRadius - _settings.RimWarpAmplitude, _settings.PadRadius + _settings.PadBlend, _anchors,
                _surface.Canyon.Bounds));
            _tuning = ScriptableObject.CreateInstance<MapSheetTuning>();
            _sheet = ScriptableObject.CreateInstance<MapSheet>();
            MapSheetTracer.Trace(_sheet, _surface, _anchors, _tuning);
        }

        [OneTimeTearDown]
        public void Release()
        {
            UnityEngine.Object.DestroyImmediate(_sheet);
            UnityEngine.Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void IsoGrid_TracesACircleAsOneClosedLoopOnTheRadius()
        {
            const float radius = 10f;
            var grid = new IsoGrid(new Rect(-20f, -20f, 40f, 40f), 1f, (x, z) => Mathf.Sqrt(x * x + z * z));
            var lines = new List<List<Vector2>>();
            var closed = new List<bool>();
            grid.Trace(radius, lines, closed);

            Assert.AreEqual(1, lines.Count);
            Assert.IsTrue(closed[0]);
            foreach (Vector2 point in lines[0])
            {
                Assert.AreEqual(radius, point.magnitude, 0.1f);
            }
        }

        [Test]
        public void IsoGrid_TracesALevelCrossingTheAreaAsOneOpenLine()
        {
            var grid = new IsoGrid(new Rect(0f, 0f, 20f, 20f), 1f, (x, z) => x);
            var lines = new List<List<Vector2>>();
            var closed = new List<bool>();
            grid.Trace(7.5f, lines, closed);

            Assert.AreEqual(1, lines.Count);
            Assert.IsFalse(closed[0]);
            Assert.AreEqual(21, lines[0].Count);
            foreach (Vector2 point in lines[0])
            {
                Assert.AreEqual(7.5f, point.x, 1e-4f);
            }
        }

        [Test]
        public void Simplifier_DropsStraightRunsAndKeepsCorners()
        {
            var straight = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                straight.Add(new Vector2(i, 0f));
            }

            Assert.AreEqual(2, PolylineSimplifier.Simplify(straight, false, 0.1f).Count);

            var square = new List<Vector2>();
            Vector2[] corners = { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f), new Vector2(0f, 10f) };
            for (int c = 0; c < corners.Length; c++)
            {
                for (int s = 0; s < 5; s++)
                {
                    square.Add(Vector2.Lerp(corners[c], corners[(c + 1) % corners.Length], s / 5f));
                }
            }

            List<Vector2> thin = PolylineSimplifier.Simplify(square, true, 0.1f);
            Assert.AreEqual(4, thin.Count);
            CollectionAssert.AreEquivalent(corners, thin);
        }

        [Test]
        public void Sheet_IsValidAndHoldsEveryKindOfLineInsideThePaper()
        {
            Assert.IsNull(_sheet.Validate());
            var counts = new Dictionary<MapLineKind, int>();
            foreach (MapLineKind kind in Enum.GetValues(typeof(MapLineKind)))
            {
                counts[kind] = 0;
            }

            for (int i = 0; i < _sheet.LineCount; i++)
            {
                counts[_sheet.GetLine(i).Kind]++;
            }

            Assert.Greater(counts[MapLineKind.Contour], 10, "contours every 5 m up the rim and The Peak");
            Assert.AreEqual(1, counts[MapLineKind.RimCrest]);
            Assert.GreaterOrEqual(counts[MapLineKind.CanyonWall], 2, "the walls, cut open at the basin mouths");
            Assert.GreaterOrEqual(counts[MapLineKind.Chasm], 1);
            Assert.Greater(counts[MapLineKind.CraterRim], 0);
            Assert.Less(_sheet.PointCount, MaxSheetPoints);

            float reach = _sheet.Radius * Mathf.Sqrt(2f) + _tuning.GridStep;
            for (int i = 0; i < _sheet.PointCount; i++)
            {
                Assert.LessOrEqual((_sheet.GetPoint(i) - _sheet.Centre).magnitude, reach);
            }
        }

        [Test]
        public void Sheet_ContoursSitOnTheirLevel()
        {
            for (int i = 0; i < _sheet.LineCount; i++)
            {
                MapLine line = _sheet.GetLine(i);
                if (line.Kind != MapLineKind.Contour)
                {
                    continue;
                }

                Assert.AreEqual(0f, Mathf.Repeat(line.Height, _sheet.ContourInterval), 1e-3f);
                Vector2 point = _sheet.GetPoint(line.Start);
                Assert.AreEqual(line.Height, _surface.SampleHeight(point.x, point.y), _tuning.GridStep);
            }
        }

        [Test]
        public void Sheet_CarriesEveryAnchorIncludingTheRegions()
        {
            Assert.AreEqual(_anchors.Count, _sheet.AnchorCount);
            var ids = new List<string>();
            for (int i = 0; i < _sheet.AnchorCount; i++)
            {
                ids.Add(_sheet.GetAnchorId(i));
            }

            CollectionAssert.Contains(ids, WorldAnchorIds.RegionPeak);
            CollectionAssert.Contains(ids, WorldAnchorIds.RegionTerraces);
        }

        [Test]
        public void Regions_PeakOnTheSummitTerracesAtTheFootOfTheSouthWestWall()
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.RegionPeak, out WorldAnchor peak));
            Assert.AreEqual(0f, Vector3.Distance(peak.Position, _surface.PeakSummit), 1f);

            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.RegionTerraces, out WorldAnchor terraces));
            Vector3 foot = terraces.Position;
            Assert.IsTrue(_surface.IsDrivable(foot.x, foot.z));
            Assert.Greater(new Vector2(foot.x, foot.z).magnitude, _surface.FloorRadius * 0.7f);
            Assert.Less(foot.x, 0f, "west");
            Assert.Less(foot.z, 0f, "south");
            Vector3 beyond = foot + terraces.Forward * (terraces.Radius * 3f);
            Assert.IsFalse(_surface.IsDrivable(beyond.x, beyond.z), "the wall rises just past the foot");
        }

        [Test]
        public void Sightline_FromTheDeckSeesTheMoundRelayButNotIntoTheTerminus()
        {
            var sightline = new Sightline(_surface);
            var eye = new Vector3(0f, _surface.SampleHeight(0f, 0f) + DeckEyeHeight, 0f);
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.RelayPrefix + "0", out WorldAnchor mound));
            Assert.IsTrue(sightline.IsClear(eye, mound.Position + Vector3.up * LightHeight, mound.Radius, SightStep,
                SightClearance));

            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonTerminus, out WorldAnchor terminus));
            Assert.IsFalse(sightline.IsClear(eye, terminus.Position + Vector3.up * LightHeight, terminus.Radius,
                SightStep, SightClearance));
        }

        [Test]
        public void Sightline_IsBlockedByARidgeBetween()
        {
            var flat = new HeightOnly((x, z) => 0f);
            var ridge = new HeightOnly((x, z) => Mathf.Abs(x - 50f) < 2f ? 10f : 0f);
            var eye = new Vector3(0f, 3f, 0f);
            var target = new Vector3(100f, 3f, 0f);

            Assert.IsTrue(new Sightline(flat).IsClear(eye, target, 0f, 1f, 0f));
            Assert.IsFalse(new Sightline(ridge).IsClear(eye, target, 0f, 1f, 0f));
        }

        private sealed class HeightOnly : ITerrainQuery
        {
            private readonly Func<float, float, float> _height;

            public HeightOnly(Func<float, float, float> height)
            {
                _height = height;
            }

            public Rect PlayableArea => new Rect(-1000f, -1000f, 2000f, 2000f);

            public bool IsDrivable(float x, float z)
            {
                return true;
            }

            public float SampleHeight(float x, float z)
            {
                return _height(x, z);
            }

            public Vector3 SampleNormal(float x, float z)
            {
                return Vector3.up;
            }
        }
    }
}
