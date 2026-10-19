using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The basin floor's gentle relief (owner, 2026-10-19): the wrinkle ridges rise and taper out, the relief leaves the
    /// opening's sites and lanes level, the rolled boulders' furrows cut the big old crater's wall, that crater reads as
    /// a soft round valley, and the boulders gather in fields with long empty stretches between.
    /// </summary>
    public sealed class ReliefTests
    {
        // A ridge stands at least this high at its middle and has sunk back to level this close to its ends.
        private const float MinRidgeRise = 1f;
        private const float EndRise = 0.05f;

        // Over the protected sites and lanes the relief adds less than this, metres.
        private const float Level = 0.01f;

        // A furrow cuts at least this deep at its middle, metres.
        private const float MinFurrow = 0.1f;

        // The big old crater: at least this wide, its walls no steeper than this.
        private const float BigCraterRadius = 50f;
        private const float BigCraterWallSlope = 15f;
        private const float WallStep = 2f;

        // Boulder fields: of the plain's tiles this wide, at least this share hold no boulder, and some tile holds a
        // cluster of at least this many.
        private const float TileSize = 60f;
        private const float MinEmptyShare = 0.4f;
        private const int MinCluster = 3;

        private SurfaceSettings _settings;
        private MoonSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
        }

        [Test]
        public void WrinkleRidges_RiseAlongTheirLine_AndTaperOutAtTheirEnds()
        {
            WrinkleRidge[] ridges = _settings.Relief.Ridges;
            Assert.That(ridges.Length, Is.InRange(2, 3), "two or three wrinkle ridges");
            foreach (WrinkleRidge ridge in ridges)
            {
                Vector2 along = MoonSurface.BearingToDirection(ridge.Bearing);
                var across = new Vector2(along.y, -along.x);
                float best = 0f;
                for (float lateral = -ridge.Sinuosity; lateral <= ridge.Sinuosity; lateral += 0.5f)
                {
                    Vector2 middle = ridge.Start + along * (ridge.Length * 0.5f) + across * lateral;
                    best = Mathf.Max(best, _surface.Relief.Rise(middle.x, middle.y));
                }

                Assert.GreaterOrEqual(best, MinRidgeRise, $"the ridge from {ridge.Start} stands at its middle");
                Vector2 end = ridge.Start + along * (ridge.Length - 1f);
                float swell = _settings.Relief.SwellHeight;
                Assert.Less(Mathf.Abs(_surface.Relief.Rise(end.x, end.y)), swell + EndRise,
                    $"the ridge from {ridge.Start} has sunk back to the swells at its end");
            }
        }

        [Test]
        public void TheOpeningsSitesAndLanes_StayLevel()
        {
            SiteSettings sites = _settings.Sites;
            var spots = new List<Vector2>
            {
                Vector2.zero, sites.ApronCenter, sites.LanderCenter, sites.RuinsCenter, sites.ScoutSpot,
                sites.MidwayCenter,
            };
            foreach (ProtectedLane lane in _settings.Relief.ProtectedLanes)
            {
                for (float t = 0f; t <= 1f; t += 0.1f)
                {
                    spots.Add(Vector2.Lerp(lane.From, lane.To, t));
                }
            }

            foreach (Vector2 spot in spots)
            {
                Assert.Less(Mathf.Abs(_surface.Relief.Rise(spot.x, spot.y)), Level, $"relief at {spot}");
                Assert.Less(_surface.Relief.Furrow(spot.x, spot.y), Level, $"a furrow at {spot}");
            }
        }

        [Test]
        public void BoulderTracks_CutTheBigCratersWall()
        {
            BoulderTrack[] tracks = _settings.Relief.Tracks;
            Assert.That(tracks.Length, Is.InRange(2, 3), "two or three boulder tracks");
            Crater big = BigCrater();
            foreach (BoulderTrack track in tracks)
            {
                Vector2 middle = (track.Start + track.End) * 0.5f;
                Assert.GreaterOrEqual(_surface.Relief.Furrow(middle.x, middle.y), MinFurrow,
                    $"the furrow from {track.Start} cuts the ground");
                Assert.Less(Vector2.Distance(track.Start, big.Center), big.Radius, "it starts on the crater's wall");
                Assert.Less(Vector2.Distance(track.End, big.Center), Vector2.Distance(track.Start, big.Center),
                    "and runs down it");
                Assert.Less(_surface.SampleHeight(track.End.x, track.End.y),
                    _surface.SampleHeight(track.Start.x, track.Start.y), "the boulder rests below where it broke loose");
            }
        }

        [Test]
        public void TheBigOldCrater_ReadsAsASoftRoundValley()
        {
            Crater big = BigCrater();
            float steepest = 0f;
            for (float r = 0f; r <= big.OuterRadius; r += WallStep)
            {
                for (int k = 0; k < 16; k++)
                {
                    float angle = k * Mathf.PI / 8f;
                    float x = big.Center.x + Mathf.Sin(angle) * r;
                    float z = big.Center.y + Mathf.Cos(angle) * r;
                    steepest = Mathf.Max(steepest, Vector3.Angle(Vector3.up, _surface.SampleNormal(x, z)));
                }
            }

            TestContext.WriteLine($"the big old crater: r {big.Radius:F0} m, depth {big.Depth:F1} m, steepest " +
                                  $"{steepest:F1} deg");
            Assert.Less(steepest, BigCraterWallSlope, "soft walls");
        }

        [Test]
        public void Boulders_GatherInFields_WithLongEmptyStretchesBetween()
        {
            var anchors = new WorldAnchors(_surface, _settings.Canyon, new RelaySettings());
            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), anchors).Plan();
            var tiles = new Dictionary<Vector2Int, int>();
            float floor = _settings.FloorRadius - TileSize;
            for (float x = -floor; x < floor; x += TileSize)
            {
                for (float z = -floor; z < floor; z += TileSize)
                {
                    if (new Vector2(x, z).magnitude < floor)
                    {
                        tiles[Tile(x, z)] = 0;
                    }
                }
            }

            foreach (ScatterInstance rock in plan)
            {
                Vector2Int tile = Tile(rock.Position.x, rock.Position.z);
                if (rock.Kind == ScatterKind.Boulder && tiles.ContainsKey(tile))
                {
                    tiles[tile]++;
                }
            }

            int empty = 0;
            int densest = 0;
            foreach (int count in tiles.Values)
            {
                empty += count == 0 ? 1 : 0;
                densest = Mathf.Max(densest, count);
            }

            float share = empty / (float)tiles.Count;
            TestContext.WriteLine($"{empty} of {tiles.Count} plain tiles hold no boulder; the densest holds {densest}");
            Assert.GreaterOrEqual(share, MinEmptyShare, "long empty stretches between the fields");
            Assert.GreaterOrEqual(densest, MinCluster, "boulders gather in places");
        }

        private Crater BigCrater()
        {
            Crater big = default;
            foreach (Crater crater in _surface.Craters)
            {
                big = crater.Radius > big.Radius ? crater : big;
            }

            Assert.GreaterOrEqual(big.Radius, BigCraterRadius, "the floor has one big old crater");
            return big;
        }

        private static Vector2Int Tile(float x, float z)
        {
            return new Vector2Int(Mathf.FloorToInt(x / TileSize), Mathf.FloorToInt(z / TileSize));
        }
    }
}
