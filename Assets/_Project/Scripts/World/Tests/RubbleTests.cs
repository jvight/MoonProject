using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The near-field rubble (owner's Apollo 11 reference, 2026-10-19): small clods and stones lie thick in patches
    /// with thinner stretches between, gather in aprons round the boulders, and keep off the opening's sites and the
    /// driving lines; on the sites and in the lanes every pebble also stays below the cobble cutoff, so the drive there
    /// stays smooth.
    /// </summary>
    public sealed class RubbleTests
    {
        // Patches: of the plain's tiles this wide, the thickest holds at least this many times the thinnest quarter's
        // average.
        private const float TileSize = 8f;
        private const float MinPatchContrast = 4f;

        // Tiles this close to the floor's edge are left out: the rim's slopes thin the rubble there.
        private const float FloorMargin = 40f;

        // An apron: round a boulder, within its reach, rubble lies at least this many times thicker than on average.
        private const float MinApronContrast = 2f;

        private SurfaceSettings _surfaceSettings;
        private ScatterSettings _settings;
        private MoonSurface _surface;
        private ScatterPlanner _planner;
        private List<ScatterInstance> _plan;

        [OneTimeSetUp]
        public void SetUp()
        {
            _surfaceSettings = new SurfaceSettings();
            _settings = new ScatterSettings();
            _surface = new MoonSurface(_surfaceSettings, WorldSettings.DefaultSeed);
            var anchors = new WorldAnchors(_surface, _surfaceSettings.Canyon, new RelaySettings());
            _planner = new ScatterPlanner(_surface, _settings, anchors);
            _plan = _planner.Plan();
        }

        [Test]
        public void Rubble_KeepsOffTheOpeningsSitesAndLanes()
        {
            int rubble = 0;
            foreach (ScatterInstance stone in _plan)
            {
                if (stone.Kind != ScatterKind.Rubble)
                {
                    continue;
                }

                rubble++;
                var point = new Vector2(stone.Position.x, stone.Position.z);
                Assert.IsFalse(_surface.Relief.OnOpeningSite(point), $"rubble on an opening site at {point}");
                Assert.GreaterOrEqual(Lanes(point), _settings.RubbleLaneHalfWidth, $"rubble in a lane at {point}");
                Assert.Less(stone.Size, _settings.CobbleColliderMinSize, "rubble never collides");
            }

            Assert.Greater(rubble, 0, "the plain has rubble");
        }

        [Test]
        public void PebblesOnTheOpeningsGround_StayBelowTheCobbleCutoff()
        {
            foreach (ScatterInstance pebble in _plan)
            {
                var point = new Vector2(pebble.Position.x, pebble.Position.z);
                bool smooth = _surface.Relief.OnOpeningSite(point) || Lanes(point) < _settings.LaneHalfWidth;
                if (pebble.Kind == ScatterKind.Pebble && smooth)
                {
                    Assert.Less(pebble.Size, _settings.CobbleColliderMinSize, $"a solid cobble at {point}");
                }
            }
        }

        [Test]
        public void Rubble_LiesThickInPatches_WithThinnerStretchesBetween()
        {
            var tiles = new Dictionary<Vector2Int, int>();
            float floor = _surface.DrivableRadius - FloorMargin;
            for (float x = -floor; x < floor; x += TileSize)
            {
                for (float z = -floor; z < floor; z += TileSize)
                {
                    var middle = new Vector2(x + TileSize * 0.5f, z + TileSize * 0.5f);
                    if (middle.magnitude < floor && !_surface.Relief.OnOpeningSite(middle)
                        && Lanes(middle) > _settings.RubbleLaneHalfWidth + TileSize)
                    {
                        tiles[Tile(middle.x, middle.y)] = 0;
                    }
                }
            }

            foreach (ScatterInstance stone in _plan)
            {
                Vector2Int tile = Tile(stone.Position.x, stone.Position.z);
                if (stone.Kind == ScatterKind.Rubble && tiles.ContainsKey(tile))
                {
                    tiles[tile]++;
                }
            }

            var counts = new List<int>(tiles.Values);
            counts.Sort();
            int quarter = Mathf.Max(1, counts.Count / 4);
            float thin = 0f;
            for (int i = 0; i < quarter; i++)
            {
                thin += counts[i];
            }

            thin /= quarter;
            int thickest = counts[counts.Count - 1];
            TestContext.WriteLine($"{counts.Count} open tiles: the thinnest quarter {thin:F1} a tile, the " +
                                  $"thickest {thickest}");
            Assert.GreaterOrEqual(thickest, thin * MinPatchContrast, "patches stand out from the stretches between");
        }

        [Test]
        public void Rubble_GathersInApronsRoundTheBoulders()
        {
            int stones = 0;
            var rubble = new List<Vector2>();
            var boulders = new List<ScatterInstance>();
            foreach (ScatterInstance instance in _plan)
            {
                if (instance.Kind == ScatterKind.Rubble)
                {
                    stones++;
                    rubble.Add(new Vector2(instance.Position.x, instance.Position.z));
                }
                else if (instance.Kind == ScatterKind.Boulder)
                {
                    boulders.Add(instance);
                }
            }

            Assert.Greater(boulders.Count, 0, "the plain has boulders");
            float floor = Mathf.PI * _surface.DrivableRadius * _surface.DrivableRadius;
            float average = stones / floor;
            int near = 0;
            float area = 0f;
            foreach (ScatterInstance boulder in boulders)
            {
                var middle = new Vector2(boulder.Position.x, boulder.Position.z);
                float inner = boulder.Size * 0.5f;
                float outer = boulder.Size * _settings.RubbleApronReach;
                area += Mathf.PI * (outer * outer - inner * inner);
                foreach (Vector2 stone in rubble)
                {
                    float distance = Vector2.Distance(stone, middle);
                    near += distance >= inner && distance <= outer ? 1 : 0;
                }
            }

            float apron = near / area;
            TestContext.WriteLine($"{stones} rubble stones, {average:F3} per m2 on average, {apron:F3} in the aprons");
            Assert.GreaterOrEqual(apron, average * MinApronContrast, "rubble gathers round the boulders");
        }

        private float Lanes(Vector2 point)
        {
            return Mathf.Min(_planner.DistanceToLanes(point), _surface.Relief.DistanceToProtectedLanes(point));
        }

        private static Vector2Int Tile(float x, float z)
        {
            return new Vector2Int(Mathf.FloorToInt(x / TileSize), Mathf.FloorToInt(z / TileSize));
        }
    }
}
