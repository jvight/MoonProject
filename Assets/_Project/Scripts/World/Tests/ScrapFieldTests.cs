using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Kestrel-3's scrap field (0.5.5 §2): the hull fragments lie along the furrow either side of its clear lane,
    /// never in it; scenery sinks deeper and tilts, the hosts stand upright at the lane's edge; deterministic.
    /// </summary>
    public sealed class ScrapFieldTests
    {
        private const int Variants = 4;

        private MoonSurface _surface;
        private ScrapFieldSettings _settings;
        private List<ScrapPlate> _pieces;

        [OneTimeSetUp]
        public void Plan()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new ScrapFieldSettings();
            _pieces = ScrapFieldPlanner.Plan(_surface, _settings, Variants, WorldSettings.DefaultSeed);
        }

        [Test]
        public void Field_HasItsPieces_AndIsDeterministic()
        {
            Assert.AreEqual(_settings.Pieces, _pieces.Count);
            List<ScrapPlate> again = ScrapFieldPlanner.Plan(_surface, _settings, Variants, WorldSettings.DefaultSeed);
            for (int i = 0; i < _pieces.Count; i++)
            {
                Assert.AreEqual(_pieces[i].Position, again[i].Position, $"piece {i}");
                Assert.AreEqual(_pieces[i].Variant, again[i].Variant, $"piece {i}");
            }
        }

        [Test]
        public void Pieces_LieBesideTheFurrowsLane_OnTheGround()
        {
            KestrelImpact kestrel = _surface.Kestrel;
            Vector2 along = (kestrel.Center - kestrel.TrailEnd).normalized;
            var across = new Vector2(along.y, -along.x);
            foreach (ScrapPlate piece in _pieces)
            {
                Vector2 offset = new Vector2(piece.Position.x, piece.Position.z) - kestrel.TrailEnd;
                float side = Mathf.Abs(Vector2.Dot(offset, across));
                Assert.That(side, Is.InRange(_settings.LaneHalfWidth,
                    _settings.LaneHalfWidth + _settings.HostOffset + _settings.Spread), "beside the lane, never in it");
                Assert.That(Vector2.Dot(offset, along), Is.GreaterThanOrEqualTo(0f), "on the furrow, not behind it");
                float ground = _surface.SampleHeight(piece.Position.x, piece.Position.z);
                if (piece.IsHost)
                {
                    Assert.AreEqual(ground, piece.Position.y, 1e-4f, "a host stands on its dust line");
                    Assert.AreEqual(1f, Vector3.Dot(piece.Rotation * Vector3.up, Vector3.up), 1e-4f, "upright");
                    Assert.LessOrEqual(side, _settings.LaneHalfWidth + _settings.HostOffset + 1e-3f,
                        "at the lane's edge, in 07's reach");
                }
                else
                {
                    Assert.AreEqual(ground - _settings.Sink, piece.Position.y, 1e-4f, "scenery sinks deeper");
                }
            }
        }

        [Test]
        public void Field_HasItsHosts_FirstInOrderFromTheFarEnd()
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                Assert.AreEqual(i < _settings.Hosts, _pieces[i].IsHost, $"piece {i}");
            }
        }

        [Test]
        public void Variants_AreAllInRange_AndMixed()
        {
            var used = new HashSet<int>();
            foreach (ScrapPlate piece in _pieces)
            {
                Assert.That(piece.Variant, Is.InRange(0, Variants - 1));
                used.Add(piece.Variant);
            }

            Assert.Greater(used.Count, 1, "more than one kind of fragment");
        }
    }
}
