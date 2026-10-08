using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The salvage sites (docs/features/M3-13): five site anchors where their stories ask, each a flat footprint with
    /// a clear way in from home; Kestrel-3's crater never traps (ruling 10), reads from the base and trails a long
    /// scorched furrow; the drill rig stands on a crater rim; the lander lies past the gate; and no site crowds
    /// another place.
    /// </summary>
    public sealed class SalvageSiteTests
    {
        public const string Depot = "depot";
        public const string Kestrel = "kestrel";
        public const string Drill = "drill";
        public const string Garage = "garage";
        public const string Lander = "lander";

        private const float MinFootprint = 8f;
        private const float MaxFootprint = 14f;
        private const float MaxDriveSlope = 20f;

        // A clear way in: no boulder this close to the straight line from the base pad to a floor site.
        private const float ApproachClearance = 3f;
        private const float ApproachStep = 0.5f;

        // Kestrel-3 in the spawn first frame (yaw 355, about +-45 degrees), apart from The Peak and relay.0.
        private const float SpawnYaw = 355f;
        private const float SpawnHalfView = 40f;
        private const float MinLandmarkSeparation = 15f;
        private const float BaseEyeHeight = 3f;
        private const float WreckHeight = 2f;
        private const float SightTargetClearance = 2f;

        // Kestrel-3's furrow: 60-120 m long, darker all along, and in the base's sight over most of its length (its
        // centre line seen this far above the ground).
        private const float MinFurrow = 60f;
        private const float MaxFurrow = 120f;
        private const float FurrowScorch = 0.35f;
        private const float FloorScorch = 0.95f;
        private const float FurrowSightLift = 0.25f;
        private const float MinFurrowInSight = 0.5f;
        private const float FarFromTheImpact = 40f;

        // The scorched crater floor paints at least this much darker (luminance) than the floor on a ring this far
        // out; the light direction only steers rock paint.
        private const float MinScorchContrast = 0.1f;
        private const float OpenFloorRing = 70f;
        private static readonly Vector3 PainterLight = new Vector3(0.3f, 0.35f, 0.9f);

        // The drill rig's footprint edge stands within this far of the crest of a crater at least this large.
        private const float RimReach = 6f;
        private const float MinRigCraterRadius = 15f;

        // Clear ground kept between a site's whole shaped reach and any other place.
        private const float SiteMargin = 5f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private WorldAnchors _anchors;

        public static IReadOnlyList<string> Sites { get; } = new[] { Depot, Kestrel, Drill, Garage, Lander };

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, _settings.Canyon, new RelaySettings());
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Sites.Validate());
        }

        [Test]
        public void Sites_StandWhereTheirStoriesAsk()
        {
            AssertDistance(Depot, 40f, 70f);
            AssertDistance(Kestrel, 150f, 260f);
            AssertDistance(Drill, 120f, 250f);
            AssertDistance(Garage, 80f, 200f);
            foreach (string name in Sites)
            {
                Assert.That(Site(name).Radius, Is.InRange(MinFootprint, MaxFootprint), $"{name}'s footprint");
            }
        }

        [Test]
        public void FloorSites_HaveAClearApproachFromHome()
        {
            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), _anchors).Plan();
            foreach (string name in Sites)
            {
                if (name == Lander)
                {
                    continue;
                }

                WorldAnchor site = Site(name);
                var end = new Vector2(site.Position.x, site.Position.z);
                Vector2 way = end.normalized;
                float length = end.magnitude - site.Radius;
                for (float d = _surface.PadRadius; d <= length; d += ApproachStep)
                {
                    Vector2 p = way * d;
                    Assert.IsTrue(_surface.IsDrivable(p.x, p.y), $"the way to {name} is blocked at {p}");
                    Assert.LessOrEqual(Slope(p), MaxDriveSlope, $"the way to {name} is too steep at {p}");
                }

                foreach (ScatterInstance rock in plan)
                {
                    if (rock.Kind != ScatterKind.Boulder)
                    {
                        continue;
                    }

                    var at = new Vector2(rock.Position.x, rock.Position.z);
                    float along = Mathf.Clamp(Vector2.Dot(at, way), 0f, length);
                    Assert.Greater(Vector2.Distance(at, way * along), ApproachClearance + rock.Size * 0.5f,
                        $"a boulder at {at} blocks the way to {name}");
                }
            }
        }

        [Test]
        public void Kestrel_CraterNeverTraps_EveryWayOutIsAGentleDrive()
        {
            KestrelImpact kestrel = _surface.Kestrel;
            const int rays = 72;
            for (int i = 0; i < rays; i++)
            {
                Vector2 direction = MoonSurface.BearingToDirection(i * 360f / rays);
                float steepest = 0f;
                for (float d = 0f; d <= kestrel.OuterRadius + 2f; d += 0.25f)
                {
                    Vector2 p = kestrel.Center + direction * d;
                    Vector2 q = p + direction * 0.25f;
                    float rise = _surface.SampleHeight(q.x, q.y) - _surface.SampleHeight(p.x, p.y);
                    steepest = Mathf.Max(steepest, Mathf.Atan2(rise, 0.25f) * Mathf.Rad2Deg);
                }

                Assert.LessOrEqual(steepest, MaxDriveSlope, $"the way out at bearing {i * 360f / rays} is too steep");
            }
        }

        [Test]
        public void Kestrel_ReadsFromTheBase_InTheSpawnFrame_ApartFromThePeakAndRelay0()
        {
            WorldAnchor site = Site(Kestrel);
            float bearing = Bearing(site.Position);
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(SpawnYaw, bearing)), SpawnHalfView, "outside the spawn view");
            Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(Bearing(_surface.PeakSummit), bearing)),
                MinLandmarkSeparation, "crowds The Peak");
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.RelayPrefix + 0, out WorldAnchor relay));
            Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(Bearing(relay.Position), bearing)),
                MinLandmarkSeparation, "crowds relay.0");

            Vector3 eye = Eye();
            Assert.IsTrue(InSight(eye, site.Position + Vector3.up * WreckHeight, SightTargetClearance),
                "the base cannot see Kestrel-3's wreck");

            KestrelImpact kestrel = _surface.Kestrel;
            int seen = 0;
            int samples = 0;
            for (float d = kestrel.RimRadius; d <= Vector2.Distance(kestrel.Center, kestrel.TrailEnd); d += 2f)
            {
                Vector2 p = kestrel.Center - kestrel.Fall * d;
                var ground = new Vector3(p.x, _surface.SampleHeight(p.x, p.y) + FurrowSightLift, p.y);
                samples++;
                if (InSight(eye, ground, 0f))
                {
                    seen++;
                }
            }

            Assert.GreaterOrEqual((float)seen / samples, MinFurrowInSight, "the furrow hides from the base");
        }

        [Test]
        public void Kestrel_ScorchesItsCraterAndTrailsAFurrowBackAlongItsFallLine()
        {
            KestrelImpact kestrel = _surface.Kestrel;
            WorldAnchor site = Site(Kestrel);
            Assert.Greater(Vector3.Dot(site.Forward, new Vector3(kestrel.Fall.x, 0f, kestrel.Fall.y)), 0.999f,
                "site.kestrel should face along the fall line, the way its trail leads in");
            Vector2 trail = kestrel.TrailEnd - kestrel.Center;
            Assert.Less(Vector2.Angle(trail, -kestrel.Fall), 0.1f, "the furrow trails back along the fall line");
            Assert.That(trail.magnitude - kestrel.RimRadius, Is.InRange(MinFurrow, MaxFurrow), "furrow length");

            for (int i = 0; i < 12; i++)
            {
                Vector2 p = kestrel.Center + MoonSurface.BearingToDirection(i * 30f) * kestrel.FloorRadius;
                Assert.GreaterOrEqual(_surface.Sample(p.x, p.y).Scorch, FloorScorch, $"crater floor at {p}");
            }

            for (float d = kestrel.RimRadius; d <= trail.magnitude; d += 2f)
            {
                Vector2 p = kestrel.Center - kestrel.Fall * d;
                Assert.GreaterOrEqual(_surface.Sample(p.x, p.y).Scorch, FurrowScorch, $"furrow at {p}");
            }

            Vector2 side = new Vector2(kestrel.Fall.y, -kestrel.Fall.x) * FarFromTheImpact;
            foreach (Vector2 p in new[] { kestrel.Center + side, kestrel.Center - side,
                         kestrel.TrailEnd - kestrel.Fall * FarFromTheImpact,
                         kestrel.Center + kestrel.Fall * FarFromTheImpact })
            {
                Assert.AreEqual(0f, _surface.Sample(p.x, p.y).Scorch, $"scorch reaches {p}");
            }
        }

        [Test]
        public void Kestrel_ScorchedDust_ReadsDarkerThanTheFloorAround()
        {
            var painter = new TerrainPainter(new TerrainPaintSettings(), WorldSettings.DefaultSeed, PainterLight);
            KestrelImpact kestrel = _surface.Kestrel;
            float scorched = 0f;
            float around = 0f;
            const int samples = 24;
            for (int i = 0; i < samples; i++)
            {
                Vector2 direction = MoonSurface.BearingToDirection(i * 360f / samples);
                scorched += Luminance(painter, kestrel.Center + direction * kestrel.FloorRadius * 0.5f);
                around += Luminance(painter, kestrel.Center + direction * OpenFloorRing);
            }

            Assert.Greater((around - scorched) / samples, MinScorchContrast,
                "Kestrel-3's crater should read as a dark scar on the floor");
        }

        [Test]
        public void Drill_StandsOnACraterRim()
        {
            WorldAnchor site = Site(Drill);
            var centre = new Vector2(site.Position.x, site.Position.z);
            bool onRim = false;
            foreach (Crater crater in _surface.Craters)
            {
                float fromCrest = Vector2.Distance(centre, crater.Center) - crater.Radius;
                onRim |= crater.Radius >= MinRigCraterRadius && fromCrest >= 0f
                    && fromCrest <= site.Radius + RimReach;
            }

            Assert.IsTrue(onRim, "the drill rig should stand just past a large crater's rim crest");
        }

        [Test]
        public void Sites_KeepClearOfEachOther_TheBase_TheRelays_TheCanyonsPlaces_AndThePlayFeatures()
        {
            foreach (string name in Sites)
            {
                WorldAnchor site = Site(name);
                var centre = new Vector2(site.Position.x, site.Position.z);
                float reach = Reach(name, site);
                Assert.Greater(centre.magnitude - reach, _settings.PadRadius + SiteMargin, $"{name} crowds the base");

                for (int i = 0; i < _anchors.Count; i++)
                {
                    WorldAnchor other = _anchors.Get(i);
                    if (other.Id == site.Id)
                    {
                        continue;
                    }

                    var at = new Vector2(other.Position.x, other.Position.z);
                    Assert.Greater(Vector2.Distance(centre, at), reach + other.Radius + SiteMargin,
                        $"{name} crowds {other.Id}");
                    if (name == Kestrel)
                    {
                        Assert.Greater(FurrowDistance(at), _surface.Kestrel.FurrowHalfWidth + other.Radius + SiteMargin,
                            $"Kestrel-3's furrow runs over {other.Id}");
                    }
                }

                if (name == Lander)
                {
                    continue;
                }

                foreach (Ramp ramp in _surface.Ramps)
                {
                    Assert.Greater(Vector2.Distance(centre, ramp.Crest), reach + ramp.BoundingRadius + SiteMargin,
                        $"{name} crowds the ramp at {ramp.Crest}");
                }

                foreach (Crater crater in _surface.Craters)
                {
                    if (crater.IsPlayBowl)
                    {
                        Assert.Greater(Vector2.Distance(centre, crater.Center),
                            reach + crater.OuterRadius + SiteMargin, $"{name} crowds the bowl at {crater.Center}");
                    }
                }
            }
        }

        [Test]
        public void Lander_OpensOffTheApron_PastTheTouchdownZone()
        {
            Canyon canyon = _surface.Canyon;
            WorldAnchor site = Site(Lander);
            Assert.IsTrue(canyon.MainPath.TryProject(site.Position.x, site.Position.z, out float arc,
                out float lateral));
            Assert.That(arc, Is.InRange(canyon.FarFaceArc + Canyon.SlabHalfDepth, canyon.ApronEndArc),
                "the lander lies on the apron past the far face");
            Assert.Greater(lateral * canyon.LanderSide, 0f, "the lander's bay opens opposite the ledge and the exit");
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.CanyonLanding, out WorldAnchor landing));
            Assert.Greater(Vector3.Distance(site.Position, landing.Position), site.Radius + landing.Radius + SiteMargin,
                "the lander crowds the touchdown zone");
        }

        private void AssertDistance(string name, float min, float max)
        {
            WorldAnchor site = Site(name);
            Assert.That(new Vector2(site.Position.x, site.Position.z).magnitude, Is.InRange(min, max),
                $"{name}'s distance from home");
        }

        /// <summary>How far a site's shaped ground reaches from its centre.</summary>
        private float Reach(string name, WorldAnchor site)
        {
            switch (name)
            {
                case Kestrel:
                    return _surface.Kestrel.OuterRadius;
                case Lander:
                    return site.Radius;
                default:
                    return site.Radius + _settings.Sites.FootprintBlend;
            }
        }

        private float FurrowDistance(Vector2 point)
        {
            KestrelImpact kestrel = _surface.Kestrel;
            Vector2 back = -kestrel.Fall;
            float length = Vector2.Distance(kestrel.Center, kestrel.TrailEnd);
            float along = Mathf.Clamp(Vector2.Dot(point - kestrel.Center, back), 0f, length);
            return Vector2.Distance(point, kestrel.Center + back * along);
        }

        private Vector3 Eye()
        {
            return new Vector3(0f, _surface.SampleHeight(0f, 0f) + BaseEyeHeight, 0f);
        }

        private bool InSight(Vector3 eye, Vector3 target, float clearance)
        {
            float length = Vector3.Distance(eye, target);
            for (float d = 0f; d < length - clearance; d += 1f)
            {
                Vector3 p = Vector3.Lerp(eye, target, d / length);
                if (p.y <= _surface.SampleHeight(p.x, p.z))
                {
                    return false;
                }
            }

            return true;
        }

        private float Luminance(TerrainPainter painter, Vector2 p)
        {
            SurfaceSample sample = _surface.Sample(p.x, p.y);
            Color32 c = painter.Ground(new Vector3(p.x, sample.Height, p.y), sample);
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
        }

        private float Slope(Vector2 p)
        {
            return Vector3.Angle(Vector3.up, _surface.SampleNormal(p.x, p.y));
        }

        private static float Bearing(Vector3 position)
        {
            return Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg;
        }

        private WorldAnchor Site(string name)
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.SitePrefix + name, out WorldAnchor anchor), $"missing {name}");
            return anchor;
        }
    }
}
