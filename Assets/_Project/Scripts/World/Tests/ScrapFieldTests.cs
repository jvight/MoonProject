using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Kestrel-3's scrap field (0.5.5 §2): the hull fragments lie along the furrow either side of its clear lane,
    /// never in it, sunk deep and tilted, crowding the impact end; deterministic.
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
                    _settings.LaneHalfWidth + _settings.LaneMargin + _settings.Spread + _settings.AnchorScale.y),
                    "beside the lane, never in it");
                Assert.That(Vector2.Dot(offset, along), Is.GreaterThanOrEqualTo(0f), "on the furrow, not behind it");
                float ground = _surface.SampleHeight(piece.Position.x, piece.Position.z);
                Assert.AreEqual(ground - _settings.Sink * piece.Scale, piece.Position.y, 1e-4f,
                    "sunk deep, in proportion to its size");
            }
        }

        [Test]
        public void Field_CrowdsTheImpactEnd_AroundBigAnchorSkins()
        {
            KestrelImpact kestrel = _surface.Kestrel;
            Vector2 toCrater = kestrel.Center - kestrel.TrailEnd;
            float half = (toCrater.magnitude - kestrel.RimRadius) * 0.5f;
            int nearImpact = 0;
            int anchors = 0;
            foreach (ScrapPlate piece in _pieces)
            {
                Vector2 offset = new Vector2(piece.Position.x, piece.Position.z) - kestrel.TrailEnd;
                nearImpact += Vector2.Dot(offset, toCrater.normalized) < half ? 1 : 0;
                anchors += piece.Scale >= _settings.AnchorScale.x ? 1 : 0;
            }

            Assert.Greater(nearImpact, _pieces.Count / 2, "most of the wreck lies near where Kestrel-3 first hit");
            Assert.GreaterOrEqual(anchors, _settings.Anchors, "a few big skins anchor the field");
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

        [Test]
        public void Cuttables_LieInTheClearLane_EvenlyDownTheFurrow_ShallowerThanTheScenery()
        {
            List<ScrapPlate> slots = ScrapFieldPlanner.PlanCuttables(_surface, _settings, Variants,
                WorldSettings.DefaultSeed);
            Assert.AreEqual(_settings.Cuttables, slots.Count);
            KestrelImpact kestrel = _surface.Kestrel;
            Vector2 along = (kestrel.Center - kestrel.TrailEnd).normalized;
            var across = new Vector2(along.y, -along.x);
            float previous = float.NegativeInfinity;
            foreach (ScrapPlate slot in slots)
            {
                Vector2 offset = new Vector2(slot.Position.x, slot.Position.z) - kestrel.TrailEnd;
                Assert.LessOrEqual(Mathf.Abs(Vector2.Dot(offset, across)), _settings.LaneHalfWidth,
                    "a cuttable slot lies in the lane");
                float down = Vector2.Dot(offset, along);
                Assert.Greater(down, previous, "the slots run down the furrow in order");
                previous = down;
                float ground = _surface.SampleHeight(slot.Position.x, slot.Position.z);
                Assert.AreEqual(ground - _settings.CuttableSink, slot.Position.y, 1e-3f, "shallow, its seam showing");
                Assert.Less(_settings.CuttableSink, _settings.Sink, "shallower than the scenery");
                Assert.AreNotEqual(0, slot.Variant, "never the anchor skin");
                Assert.AreEqual(1f, slot.Scale);
            }
        }
    }
}
