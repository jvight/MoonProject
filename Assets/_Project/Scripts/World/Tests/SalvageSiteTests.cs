using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The salvage site (docs/features/M3-13): Kestrel-3's crater where its story asks, with a clear way in from home;
    /// it never traps (ruling 10), reads from the base, trails a long scorched furrow and crowds no other place; the
    /// Lumen ruins stand flat and clear beyond the lander.
    /// </summary>
    public sealed class SalvageSiteTests
    {
        public const string Kestrel = "kestrel";

        private const float MinFootprint = 8f;
        private const float MaxFootprint = 14f;
        private const float MaxDriveSlope = 20f;
        private const string FallenArrayRuin = "FallenSolarArray";

        // A clear way in: no boulder this close to the straight line from the base pad to a floor site.
        private const float ApproachClearance = 3f;
        private const float ApproachStep = 0.5f;

        // Kestrel-3 in the spawn first frame (yaw 355, about +-45 degrees), apart from The Peak.
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

        // Clear ground kept between a site's whole shaped reach and any other place.
        private const float SiteMargin = 5f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private WorldAnchors _anchors;

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
        public void Kestrel_StandsWhereItsStoryAsks()
        {
            WorldAnchor site = Site(Kestrel);
            Assert.That(new Vector2(site.Position.x, site.Position.z).magnitude, Is.InRange(150f, 260f),
                "distance from home");
            Assert.That(site.Radius, Is.InRange(MinFootprint, MaxFootprint), "footprint");
        }

        [Test]
        public void Kestrel_HasAClearApproachFromHome()
        {
            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), _anchors).Plan();
            WorldAnchor site = Site(Kestrel);
            var end = new Vector2(site.Position.x, site.Position.z);
            Vector2 way = end.normalized;
            float length = end.magnitude - site.Radius;
            for (float d = _surface.PadRadius; d <= length; d += ApproachStep)
            {
                Vector2 p = way * d;
                Assert.IsTrue(_surface.IsDrivable(p.x, p.y), $"the way in is blocked at {p}");
                Assert.LessOrEqual(Slope(p), MaxDriveSlope, $"the way in is too steep at {p}");
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
                    $"a boulder at {at} blocks the way in");
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
        public void Kestrel_ReadsFromTheBase_InTheSpawnFrame_ApartFromThePeak()
        {
            WorldAnchor site = Site(Kestrel);
            float bearing = Bearing(site.Position);
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(SpawnYaw, bearing)), SpawnHalfView, "outside the spawn view");
            Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(Bearing(_surface.PeakSummit), bearing)),
                MinLandmarkSeparation, "crowds The Peak");

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
        public void KestrelTrail_MarksTheFurrowsHomeSideEnd_FacingAlongTheFallLine()
        {
            Assert.IsTrue(_anchors.TryGet(WorldAnchorIds.KestrelTrail, out WorldAnchor trail));
            WorldAnchor site = Site(Kestrel);
            KestrelImpact kestrel = _surface.Kestrel;
            Assert.AreEqual(0f, Vector2.Distance(new Vector2(trail.Position.x, trail.Position.z), kestrel.TrailEnd),
                1e-4f, "trail.kestrel should stand at the furrow's far end");
            Assert.Greater(Vector3.Dot(trail.Forward, site.Forward), 0.999f, "trail.kestrel faces along the fall line");
            Vector3 toCrater = site.Position - trail.Position;
            var along = new Vector2(toCrater.x, toCrater.z);
            Assert.Less(Vector2.Angle(along, new Vector2(trail.Forward.x, trail.Forward.z)), 0.1f,
                "the crater lies straight ahead of trail.kestrel");
            Assert.That(along.magnitude - kestrel.RimRadius, Is.InRange(MinFurrow, MaxFurrow), "furrow length");
            Assert.AreEqual(kestrel.TailHalfWidth, trail.Radius, 1e-4f, "trail.kestrel spans the furrow's tail");
            Assert.Less(Vector2.Distance(Vector2.zero, new Vector2(trail.Position.x, trail.Position.z)),
                new Vector2(site.Position.x, site.Position.z).magnitude, "the trail's end lies on the home side");
        }

        [Test]
        public void Kestrel_ScorchedDust_ReadsDarkerThanTheFloorAround()
        {
            var painter = new TerrainPainter(new TerrainPaintSettings(), WorldSettings.DefaultSeed, PainterLight,
                new SurfaceSettings().PadRadius);
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
        public void Kestrel_KeepsClearOfTheBase_OtherPlaces_AndThePlayFeatures()
        {
            WorldAnchor site = Site(Kestrel);
            var centre = new Vector2(site.Position.x, site.Position.z);
            float reach = _surface.Kestrel.OuterRadius;
            Assert.Greater(centre.magnitude - reach, _settings.PadRadius + SiteMargin, "crowds the base");

            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor other = _anchors.Get(i);
                if (other.Id == site.Id || other.Id == WorldAnchorIds.KestrelTrail
                    || other.Id == WorldAnchorIds.PlaceMidway)
                {
                    continue;
                }

                var at = new Vector2(other.Position.x, other.Position.z);
                Assert.Greater(Vector2.Distance(centre, at), reach + other.Radius + SiteMargin, $"crowds {other.Id}");
                Assert.Greater(FurrowDistance(at), _surface.Kestrel.FurrowHalfWidth + other.Radius + SiteMargin,
                    $"Kestrel-3's furrow runs over {other.Id}");
            }

            foreach (Ramp ramp in _surface.Ramps)
            {
                Assert.Greater(Vector2.Distance(centre, ramp.Crest), reach + ramp.BoundingRadius + SiteMargin,
                    $"crowds the ramp at {ramp.Crest}");
            }

            foreach (Crater crater in _surface.Craters)
            {
                if (crater.IsPlayBowl)
                {
                    Assert.Greater(Vector2.Distance(centre, crater.Center), reach + crater.OuterRadius + SiteMargin,
                        $"crowds the bowl at {crater.Center}");
                }
            }
        }

        [Test]
        public void RuinsCluster_IsFlatBeyondTheLanderInSpawnView_WithDrivableLanesFreeOfScatter()
        {
            GroundShape ruins = _surface.Shaping.Ruins;
            Assert.That(Vector2.Distance(ruins.Center, _surface.Shaping.Lander.Center), Is.InRange(50f, 80f),
                "the ruins stand about 65 m east of the lander (opening v2 §4)");
            float bearing = Mathf.Atan2(ruins.Center.x, ruins.Center.y) * Mathf.Rad2Deg;
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(SpawnYaw, bearing)), SpawnHalfView,
                "ruins cluster should stand in the spawn view");
            var target = new Vector3(ruins.Center.x, _surface.SampleHeight(ruins.Center.x, ruins.Center.y) + WreckHeight,
                ruins.Center.y);
            Assert.IsTrue(InSight(Eye(), target, SightTargetClearance), "the base cannot see the ruins cluster");

            RuinPlacement[] pieces = new LandmarkSettings().Ruins;
            Assert.AreEqual(5, pieces.Length);
            GroundShape array = _surface.Shaping.FallenArray;
            for (int i = 0; i < pieces.Length; i++)
            {
                Vector2 spot = ruins.Center + pieces[i].Offset;
                GroundShape footing = pieces[i].Id == FallenArrayRuin ? array : ruins;
                Assert.LessOrEqual(Vector2.Distance(spot, footing.Center), footing.Radius,
                    $"{pieces[i].Id} sits outside its flattened footprint");
                Assert.AreEqual(footing.Plateau, _surface.SampleHeight(spot.x, spot.y), 1e-3f,
                    $"{pieces[i].Id} footing is not flat");
                for (int j = i + 1; j < pieces.Length; j++)
                {
                    float gap = Vector2.Distance(pieces[i].Offset, pieces[j].Offset)
                        - pieces[i].ClearanceRadius - pieces[j].ClearanceRadius;
                    Assert.GreaterOrEqual(gap, -2.5f, $"{pieces[i].Id} and {pieces[j].Id} overlap");
                }
            }

            List<ScatterInstance> plan = new ScatterPlanner(_surface, new ScatterSettings(), _anchors).Plan();
            WorldSystem.AddHomeApronStones(plan, _surface);
            GroundShape lander = _surface.Shaping.Lander;
            GroundShape apron = _surface.Shaping.Apron;
            foreach (ScatterInstance rock in plan)
            {
                var at = new Vector2(rock.Position.x, rock.Position.z);
                Assert.GreaterOrEqual(Vector2.Distance(at, ruins.Center), ruins.Radius,
                    $"{rock.Kind} at {at} encroaches on the ruins cluster lanes");
                Assert.GreaterOrEqual(Vector2.Distance(at, lander.Center), lander.Radius,
                    $"{rock.Kind} at {at} lies on the lander's footprint");
                Assert.GreaterOrEqual(Vector2.Distance(at, apron.Center), apron.Radius,
                    $"{rock.Kind} at {at} lies on the build apron");
                Assert.GreaterOrEqual(Vector2.Distance(at, array.Center), array.Radius,
                    $"{rock.Kind} at {at} lies on the fallen array's footprint");
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
