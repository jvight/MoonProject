using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Kestrel-3's scrap field (0.5.5 §2): the plates lie along the furrow either side of its clear lane, half-buried
    /// in the dust (never resting on top), deterministic for a seed.
    /// </summary>
    public sealed class ScrapFieldTests
    {
        private MoonSurface _surface;
        private ScrapFieldSettings _settings;
        private List<ScrapPlate> _plates;

        [OneTimeSetUp]
        public void Plan()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new ScrapFieldSettings();
            _plates = ScrapFieldPlanner.Plan(_surface, _settings, WorldSettings.DefaultSeed);
        }

        [Test]
        public void Field_HasItsPlates_AndIsDeterministic()
        {
            Assert.AreEqual(_settings.Plates, _plates.Count);
            List<ScrapPlate> again = ScrapFieldPlanner.Plan(_surface, _settings, WorldSettings.DefaultSeed);
            for (int i = 0; i < _plates.Count; i++)
            {
                Assert.AreEqual(_plates[i].Centre, again[i].Centre, $"plate {i}");
            }
        }

        [Test]
        public void Plates_LieBesideTheFurrowsLane_NotInIt()
        {
            KestrelImpact kestrel = _surface.Kestrel;
            Vector2 along = (kestrel.Center - kestrel.TrailEnd).normalized;
            var across = new Vector2(along.y, -along.x);
            foreach (ScrapPlate plate in _plates)
            {
                Vector2 offset = new Vector2(plate.Centre.x, plate.Centre.z) - kestrel.TrailEnd;
                float side = Mathf.Abs(Vector2.Dot(offset, across));
                Assert.That(side, Is.InRange(_settings.LaneHalfWidth, _settings.LaneHalfWidth + _settings.Spread),
                    "beside the lane, within the field");
                Assert.That(Vector2.Dot(offset, along), Is.GreaterThanOrEqualTo(0f), "on the furrow, not behind it");
            }
        }

        [Test]
        public void Plates_AreHalfBuried_NeverRestingOnTop()
        {
            foreach (ScrapPlate plate in _plates)
            {
                float ground = _surface.SampleHeight(plate.Centre.x, plate.Centre.z);
                Vector3 across = plate.Rotation * Vector3.forward * (plate.Size.z * 0.5f);
                float low = Mathf.Min(plate.Centre.y + across.y, plate.Centre.y - across.y);
                float high = Mathf.Max(plate.Centre.y + across.y, plate.Centre.y - across.y);
                Assert.Less(low, ground, "a plate's lower edge is under the dust");
                Assert.Greater(high, ground, "and its upper edge sticks out");
            }
        }
    }
}
