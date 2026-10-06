using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>Relic sites and the scrap field: deterministic, on gentle drivable floor, generous enough.</summary>
    public sealed class PlacementTests
    {
        private static readonly RelicPlacementBand[] Bands =
        {
            RelicPlacementBand.Onboarding, RelicPlacementBand.Onboarding, RelicPlacementBand.Wanderer,
            RelicPlacementBand.Wanderer, RelicPlacementBand.Wanderer, RelicPlacementBand.RimView,
        };

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void RelicSites_AreDeterministic()
        {
            TestWorld world = TestWorld.Basin();
            var tuning = Create<RelicPlacementTuning>();
            RelicSite[] a = RelicSitePlanner.Plan(world, world, tuning, Bands);
            RelicSite[] b = RelicSitePlanner.Plan(world, world, tuning, Bands);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].Position, b[i].Position, $"site {i}");
            }
        }

        [Test]
        public void RelicSites_RespectTheirBands_AndAreFlatSpacedAndDrivable()
        {
            TestWorld world = TestWorld.Basin();
            var tuning = Create<RelicPlacementTuning>();
            RelicSite[] sites = RelicSitePlanner.Plan(world, world, tuning, Bands);
            float minNormalY = SurfaceRules.MinNormalY(tuning.MaxSlopeDegrees);

            for (int i = 0; i < sites.Length; i++)
            {
                Vector3 site = sites[i].Position;
                float fromBase = SurfaceRules.HorizontalDistance(site, world.BasePosition);
                Assert.AreEqual(world.SampleHeight(site.x, site.z), site.y, 1e-4f, $"site {i} lies on the surface");
                Assert.IsTrue(SurfaceRules.InsideDrivable(world, site.x, site.z, tuning.EdgeMargin), $"site {i}");
                Assert.IsTrue(SurfaceRules.IsFlat(world, site.x, site.z, minNormalY, tuning.FlatnessProbeRadius),
                    $"site {i} is flat enough to park over");
                Assert.Greater(Vector3.Distance(new Vector3(site.x, 0f, site.z), TestWorld.MoundCentre), 12f,
                    $"site {i} avoids the steep mound");
                switch (Bands[i])
                {
                    case RelicPlacementBand.Onboarding:
                        Assert.That(fromBase, Is.InRange(tuning.OnboardingDistance.x, tuning.OnboardingDistance.y));
                        Assert.Less(Mathf.Abs(SurfaceRules.Bearing(site - world.BasePosition)),
                            tuning.OnboardingSpread + 1f, "onboarding sites lie ahead of 07's spawn heading");
                        break;
                    case RelicPlacementBand.Wanderer:
                        Assert.That(fromBase, Is.InRange(tuning.WandererDistance.x, tuning.WandererDistance.y));
                        break;
                    case RelicPlacementBand.RimView:
                        Assert.Greater(fromBase, 180f, "the rim site is far out");
                        break;
                }

                for (int j = 0; j < i; j++)
                {
                    Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site, sites[j].Position),
                        tuning.MinSiteSpacing * 0.6f - 1e-3f, $"sites {j} and {i} are apart");
                }
            }
        }

        [Test]
        public void RimSite_FacesThePeak_WithAClearView()
        {
            TestWorld world = TestWorld.Basin();
            var tuning = Create<RelicPlacementTuning>();
            Vector3 site = RelicSitePlanner.Plan(world, world, tuning, Bands)[5].Position;
            float bearingToPeak = SurfaceRules.Bearing(world.PeakPosition - world.BasePosition);
            float bearingToSite = SurfaceRules.Bearing(site - world.BasePosition);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(bearingToPeak, bearingToSite)), tuning.RimBearingSpread + 1f);

            Vector3 eye = site + Vector3.up * tuning.RimEyeHeight;
            for (int i = 1; i <= 50; i++)
            {
                Vector3 point = Vector3.Lerp(eye, world.PeakPosition, tuning.RimSightReach * i / 50f);
                Assert.Greater(point.y, world.SampleHeight(point.x, point.z), "nothing hides The Peak");
            }
        }

        [Test]
        public void ScrapField_IsDeterministic_AndHoldsAtLeastTheMinimumValue()
        {
            TestWorld world = TestWorld.Basin();
            var placement = Create<RelicPlacementTuning>();
            var tuning = Create<ScrapTuning>();
            RelicSite[] sites = RelicSitePlanner.Plan(world, world, placement, Bands);
            ScrapVariant[] variants = Variants();

            List<ScrapSpawn> a = ScrapFieldPlanner.Plan(world, world, tuning, sites, variants);
            List<ScrapSpawn> b = ScrapFieldPlanner.Plan(world, world, tuning, sites, variants);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Variant, b[i].Variant);
            }

            int total = ScrapFieldPlanner.TotalValue(a, variants);
            Assert.GreaterOrEqual(total, tuning.MinTotalValue);
            Assert.GreaterOrEqual(total, 270, "design ruling 5: at least twice the tower's 135 scrap");
        }

        [Test]
        public void ScrapPieces_LieOnGentleDrivableFloor_AwayFromHomeAndDigSpots()
        {
            TestWorld world = TestWorld.Basin();
            var placement = Create<RelicPlacementTuning>();
            var tuning = Create<ScrapTuning>();
            RelicSite[] sites = RelicSitePlanner.Plan(world, world, placement, Bands);
            List<ScrapSpawn> spawns = ScrapFieldPlanner.Plan(world, world, tuning, sites, Variants());
            float minNormalY = SurfaceRules.MinNormalY(tuning.MaxSlopeDegrees);

            foreach (ScrapSpawn spawn in spawns)
            {
                Vector3 p = spawn.Position;
                Assert.IsTrue(world.IsDrivable(p.x, p.z));
                Assert.GreaterOrEqual(world.SampleNormal(p.x, p.z).y, minNormalY, "never on steep slopes");
                Assert.AreEqual(world.SampleHeight(p.x, p.z) + tuning.HoverHeight, p.y, 1e-4f);
                Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(p, world.BasePosition),
                    tuning.BaseClearRadius);
                foreach (RelicSite site in sites)
                {
                    Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(p, site.Position), tuning.SiteClearRadius);
                }
            }
        }

        [Test]
        public void ScrapTrails_LeadFromHomeTowardEveryDistantRelic()
        {
            TestWorld world = TestWorld.Flat();
            var placement = Create<RelicPlacementTuning>();
            var tuning = Create<ScrapTuning>();
            RelicSite[] sites = RelicSitePlanner.Plan(world, world, placement, Bands);
            List<ScrapSpawn> spawns = ScrapFieldPlanner.Plan(world, world, tuning, sites, Variants());

            foreach (RelicSite site in sites)
            {
                Vector3 direction = (site.Position - world.BasePosition).normalized;
                float length = SurfaceRules.HorizontalDistance(site.Position, world.BasePosition);
                if (length < 80f)
                {
                    continue;
                }

                int nearPath = 0;
                foreach (ScrapSpawn spawn in spawns)
                {
                    Vector3 offset = spawn.Position - world.BasePosition;
                    float along = Vector3.Dot(new Vector3(offset.x, 0f, offset.z), direction);
                    float across = Vector3.Cross(direction, new Vector3(offset.x, 0f, offset.z)).magnitude;
                    if (along > tuning.TrailStart - 1f && along < length && across < tuning.TrailJitter + 3f)
                    {
                        nearPath++;
                    }
                }

                Assert.GreaterOrEqual(nearPath, 2, $"a trail of scrap points toward the relic at {site.Position}");
            }
        }

        private ScrapVariant[] Variants()
        {
            var prefab = new GameObject("ScrapFixture");
            _created.Add(prefab);
            return new[]
            {
                new ScrapVariant(prefab, 1, 3f), new ScrapVariant(prefab, 2, 2f), new ScrapVariant(prefab, 3, 1f),
                new ScrapVariant(prefab, 2, 2f),
            };
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }
    }
}
