using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Verifies <see cref="IWorldRegions"/> (<see cref="WorldRegions"/>), <see cref="RegionVolume"/>,
    /// <see cref="WorldAtmosphereBlender"/>, and <see cref="WorldAnchor.RegionId"/> assignment (0.6-S1).
    /// </summary>
    public sealed class WorldRegionTests
    {
        private const int GridSteps = 32;
        private const float GridExtent = 1600f;
        private const float SeamStep = 0.5f;
        private const float MaxWeightDeltaPerHalfMetre = 0.01f;
        private const float WeightTolerance = 1e-5f;

        private SurfaceSettings _surfaceSettings;
        private MoonSurface _surface;
        private WorldAnchors _anchors;
        private RegionSettings _regionSettings;
        private WorldRegions _regions;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _surfaceSettings = new SurfaceSettings();
            _surface = new MoonSurface(_surfaceSettings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, _surfaceSettings.Canyon, new RelaySettings());
            _regionSettings = new RegionSettings();
            _regions = new WorldRegions(_anchors, _regionSettings);
        }

        [Test]
        public void Weights_SumToOne_AcrossGridAndExtremePositions()
        {
            var buffer = new RegionWeight[4];
            for (int iz = 0; iz < GridSteps; iz++)
            {
                float z = Mathf.Lerp(-GridExtent, GridExtent, iz / (float)(GridSteps - 1));
                for (int ix = 0; ix < GridSteps; ix++)
                {
                    float x = Mathf.Lerp(-GridExtent, GridExtent, ix / (float)(GridSteps - 1));
                    AssertWeightsAt(new Vector3(x, 0f, z), buffer);
                }
            }

            Vector3[] extremes =
            {
                new Vector3(-5000f, 0f, -5000f),
                new Vector3(5000f, 0f, -5000f),
                new Vector3(-5000f, 0f, 5000f),
                new Vector3(5000f, 0f, 5000f),
                new Vector3(0f, 500f, 0f),
            };
            foreach (Vector3 extreme in extremes)
            {
                AssertWeightsAt(extreme, buffer);
            }
        }

        [Test]
        public void Weights_AreContinuous_AcrossEveryBasinCanyonSeam()
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonMouth, out WorldAnchor mouth));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLedge, out WorldAnchor ledge));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonTerminus, out WorldAnchor terminus));

            // 1. From deep in the basin through the canyon mouth to the landing apron.
            AssertContinuityAlongLine(Vector3.zero, landing.Position);

            // 2. Across the canyon laterally at the landing apron (+-120 m perpendicular to the corridor).
            var landingRight = new Vector3(landing.Forward.z, 0f, -landing.Forward.x);
            AssertContinuityAlongLine(landing.Position - landingRight * 120f, landing.Position + landingRight * 120f);

            // 3. Across the glinting ledge laterally.
            AssertContinuityAlongLine(ledge.Position - ledge.Forward * 120f, ledge.Position + ledge.Forward * 120f);

            // 4. Past the terminus chamber out into the rim beyond.
            AssertContinuityAlongLine(mouth.Position, terminus.Position + terminus.Forward * 120f);
        }

        [Test]
        public void Hysteresis_PreventsFlapping_AndPublishesRegionEnteredOncePerRealChange()
        {
            var bus = new EventBus();
            var regions = new WorldRegions(_anchors, _regionSettings, bus);
            var received = new List<RegionEntered>();
            using var sub = bus.Subscribe<RegionEntered>(e => received.Add(e));

            Assert.AreEqual(RegionIds.Basin, regions.CurrentRegionId);
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonMouth, out WorldAnchor mouth));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonTerminus, out WorldAnchor terminus));

            Vector3 lowCanyon = FindWeightPoint(regions, Vector3.zero, landing.Position, 0.46f);
            Vector3 highCanyon = FindWeightPoint(regions, Vector3.zero, landing.Position, 0.54f);
            Assert.That(regions.SampleWeight(RegionIds.Canyon, lowCanyon), Is.InRange(0.45f, 0.47f));
            Assert.That(regions.SampleWeight(RegionIds.Canyon, highCanyon), Is.InRange(0.53f, 0.549f));

            // Oscillating across 0.5 within [0.46, 0.54] must not flip CurrentRegionId or fire RegionEntered.
            regions.Step(Vector3.zero);
            for (int i = 0; i < 20; i++)
            {
                regions.Step(i % 2 == 0 ? highCanyon : lowCanyon);
            }

            Assert.AreEqual(RegionIds.Basin, regions.CurrentRegionId);
            Assert.AreEqual(0, received.Count);

            // Driving into the canyon past the 0.55 hysteresis threshold fires RegionEntered once.
            regions.Step(mouth.Position);
            regions.Step(landing.Position);
            regions.Step(terminus.Position);
            Assert.AreEqual(RegionIds.Canyon, regions.CurrentRegionId);
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(RegionIds.Canyon, received[0].RegionId);
            Assert.AreEqual(RegionIds.Basin, received[0].PreviousRegionId);
            Assert.IsFalse(received[0].Placed);

            // Oscillating back in [0.46, 0.54] canyon weight (basin weight [0.46, 0.54] < 0.55) keeps Canyon active.
            for (int i = 0; i < 20; i++)
            {
                regions.Step(i % 2 == 0 ? lowCanyon : highCanyon);
            }

            Assert.AreEqual(RegionIds.Canyon, regions.CurrentRegionId);
            Assert.AreEqual(1, received.Count);

            // Driving back out into the basin past 0.55 basin weight switches back to Basin once.
            regions.Step(Vector3.zero);
            Assert.AreEqual(RegionIds.Basin, regions.CurrentRegionId);
            Assert.AreEqual(2, received.Count);
            Assert.AreEqual(RegionIds.Basin, received[1].RegionId);
            Assert.AreEqual(RegionIds.Canyon, received[1].PreviousRegionId);
            Assert.IsFalse(received[1].Placed);

            // PlaceAt snaps immediately and marks Placed = true.
            regions.PlaceAt(terminus.Position);
            Assert.AreEqual(RegionIds.Canyon, regions.CurrentRegionId);
            Assert.AreEqual(3, received.Count);
            Assert.AreEqual(RegionIds.Canyon, received[2].RegionId);
            Assert.AreEqual(RegionIds.Basin, received[2].PreviousRegionId);
            Assert.IsTrue(received[2].Placed);
        }

        [Test]
        public void Anchors_HaveValidRegionIds_MatchingSampledRegions()
        {
            Assert.IsTrue(_regions.TryGetRegion(RegionIds.Basin, out RegionInfo basinInfo));
            Assert.AreEqual(RegionIds.Basin, basinInfo.Id);
            Assert.IsTrue(_regions.TryGetRegion(RegionIds.Canyon, out RegionInfo canyonInfo));
            Assert.AreEqual(RegionIds.Canyon, canyonInfo.Id);
            Assert.IsFalse(_regions.TryGetRegion("nonexistent", out _));

            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor anchor = _anchors.Get(i);
                Assert.IsFalse(string.IsNullOrEmpty(anchor.RegionId), $"{anchor.Id} has an empty RegionId");
                Assert.IsTrue(_regions.TryGetRegion(anchor.RegionId, out RegionInfo info),
                    $"{anchor.Id} references unknown region '{anchor.RegionId}'");
                Assert.AreEqual(anchor.RegionId, info.Id);

                bool expectedCanyon = anchor.Id.StartsWith("canyon.", System.StringComparison.Ordinal)
                    || anchor.Id == WorldAnchorIds.RelayPrefix + 3
                    || anchor.Id == WorldAnchorIds.SitePrefix + "lander";
                Assert.AreEqual(expectedCanyon ? RegionIds.Canyon : RegionIds.Basin, anchor.RegionId,
                    $"{anchor.Id} has unexpected RegionId");
                Assert.AreEqual(anchor.RegionId, _regions.SampleRegionId(anchor.Position),
                    $"SampleRegionId disagrees with anchor.RegionId at {anchor.Id}");
            }
        }

        [Test]
        public void AtmosphereBlender_MatchesLookAByDefault_AndBlendsProfilesAcrossSeam()
        {
            var atmosphere = new AtmosphereSettings();
            var sky = new SkySettings();
            BlendedAtmosphere expectedLookA = BlendedAtmosphere.FromSettings(atmosphere, sky);
            var blender = new WorldAtmosphereBlender(_regions);

            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonTerminus, out WorldAnchor terminus));
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing));

            BlendedAtmosphere atBase = blender.BlendAt(Vector3.zero);
            BlendedAtmosphere atTerminus = blender.BlendAt(terminus.Position);
            Assert.IsTrue(atBase.ApproximatelyEquals(in expectedLookA), "basin default profile must match Look A");
            Assert.IsTrue(atTerminus.ApproximatelyEquals(in expectedLookA), "canyon default profile must match Look A");
            Assert.AreEqual(RegionAtmosphereProfile.DefaultFarClipPlane, _regions.CurrentFarClipPlane, 1e-4f);

            // Verify interpolation when profiles differ.
            var customBasin = new RegionAtmosphereProfile(
                RegionIds.Basin,
                RegionAtmosphereProfile.DefaultBasinOuterRadius,
                RegionAtmosphereProfile.DefaultBasinBlendBand,
                farClipPlane: 3200f,
                ambientDrainMultiplier: 1f,
                blocksSkyAndRadio: false,
                fogDensity: 0.0005f,
                lightIntensity: 2.0f);
            var customCanyon = new RegionAtmosphereProfile(
                RegionIds.Canyon,
                RegionAtmosphereProfile.DefaultCanyonHalfWidth,
                RegionAtmosphereProfile.DefaultCanyonBlendBand,
                farClipPlane: 2200f,
                ambientDrainMultiplier: 1.5f,
                blocksSkyAndRadio: true,
                fogDensity: 0.0015f,
                lightIntensity: 1.0f);
            var customRegions = new WorldRegions(_anchors, new RegionSettings(customBasin, customCanyon));
            var customBlender = new WorldAtmosphereBlender(customRegions);

            Vector3 midSeam = FindWeightPoint(customRegions, Vector3.zero, landing.Position, 0.5f);
            float w = customRegions.SampleWeight(RegionIds.Canyon, midSeam);
            Assert.AreEqual(0.5f, w, 1e-3f);

            customRegions.Step(midSeam);
            BlendedAtmosphere midBlend = customBlender.BlendAt(midSeam);
            Assert.AreEqual(Mathf.Lerp(3200f, 2200f, w), customRegions.CurrentFarClipPlane, 1e-2f);
            Assert.AreEqual(Mathf.Lerp(0.0005f, 0.0015f, w), midBlend.FogDensity, 1e-6f);
            Assert.AreEqual(Mathf.Lerp(2.0f, 1.0f, w), midBlend.LightIntensity, 1e-4f);
            Assert.AreEqual(Mathf.Lerp(1f, 1.5f, w), customRegions.SampleDrainMultiplier(midSeam), 1e-4f);
            Assert.AreEqual(w, customRegions.SampleSkyOcclusion(midSeam), 1e-4f);
        }

        private void AssertWeightsAt(Vector3 position, RegionWeight[] buffer)
        {
            int count = _regions.GetWeights(position, buffer);
            Assert.That(count, Is.InRange(1, 2), $"unexpected active weight count at {position}");
            float sum = 0f;
            for (int i = 0; i < count; i++)
            {
                Assert.That(buffer[i].Weight, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
                sum += buffer[i].Weight;
            }

            Assert.AreEqual(1f, sum, WeightTolerance, $"weights do not sum to 1 at {position}");
            float sampledSum = _regions.SampleWeight(RegionIds.Basin, position)
                + _regions.SampleWeight(RegionIds.Canyon, position);
            Assert.AreEqual(1f, sampledSum, WeightTolerance);
            Assert.AreEqual(1f, _regions.SampleDrainMultiplier(position), WeightTolerance);
            Assert.AreEqual(0f, _regions.SampleSkyOcclusion(position), WeightTolerance);
        }

        private void AssertContinuityAlongLine(Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            int steps = Mathf.CeilToInt(length / SeamStep);
            float prevCanyon = _regions.SampleWeight(RegionIds.Canyon, from);
            float prevBasin = _regions.SampleWeight(RegionIds.Basin, from);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)steps);
                float canyon = _regions.SampleWeight(RegionIds.Canyon, p);
                float basin = _regions.SampleWeight(RegionIds.Basin, p);
                Assert.Less(Mathf.Abs(canyon - prevCanyon), MaxWeightDeltaPerHalfMetre,
                    $"canyon weight jump at {p}");
                Assert.Less(Mathf.Abs(basin - prevBasin), MaxWeightDeltaPerHalfMetre,
                    $"basin weight jump at {p}");
                prevCanyon = canyon;
                prevBasin = basin;
            }
        }

        private static Vector3 FindWeightPoint(WorldRegions regions, Vector3 outside, Vector3 inside, float target)
        {
            Vector3 lo = outside;
            Vector3 hi = inside;
            for (int i = 0; i < 32; i++)
            {
                Vector3 mid = (lo + hi) * 0.5f;
                if (regions.SampleWeight(RegionIds.Canyon, mid) < target)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return (lo + hi) * 0.5f;
        }
    }
}
