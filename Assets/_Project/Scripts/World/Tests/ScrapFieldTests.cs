using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Kestrel-3's scrap field (0.5.5 §2): the cuttable slots lie in the furrow's clear lane, evenly down it,
    /// shallow, never the anchor skin.
    /// </summary>
    public sealed class ScrapFieldTests
    {
        private const int Variants = 4;

        private MoonSurface _surface;
        private ScrapFieldSettings _settings;

        [OneTimeSetUp]
        public void SetUp()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new ScrapFieldSettings();
        }

        [Test]
        public void Cuttables_LieInTheClearLane_EvenlyDownTheFurrow_Shallow()
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
                Assert.AreNotEqual(0, slot.Variant, "never the anchor skin");
                Assert.AreEqual(1f, slot.Scale);
            }
        }
    }
}
