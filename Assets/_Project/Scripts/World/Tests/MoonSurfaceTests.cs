using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    public sealed class MoonSurfaceTests
    {
        private const float MaxDrivableSlopeDegrees = 20f;
        private const float RimClearanceAboveFloor = 40f;

        // The dish's blinking light on the summit plateau: what the player must see from the base.
        private const float BeaconHeightAboveSummit = 4f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Validate());
        }

        [Test]
        public void SameSeed_GivesIdenticalSurface_AndOtherSeedsDiffer()
        {
            var twin = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            var other = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed + 1);
            int differences = 0;
            for (float z = -600f; z <= 600f; z += 37f)
            {
                for (float x = -600f; x <= 600f; x += 41f)
                {
                    Assert.AreEqual(_surface.SampleHeight(x, z), twin.SampleHeight(x, z), 0f, $"at ({x}, {z})");
                    if (Mathf.Abs(_surface.SampleHeight(x, z) - other.SampleHeight(x, z)) > 0.01f)
                    {
                        differences++;
                    }
                }
            }

            Assert.Greater(differences, 100, "A different seed should produce a different surface.");
        }

        [Test]
        public void BasePad_IsExactlyFlatAtZero_AndFallsOffSmoothly()
        {
            float pad = _settings.PadRadius;
            for (float z = -pad; z <= pad; z += 1f)
            {
                for (float x = -pad; x <= pad; x += 1f)
                {
                    if (x * x + z * z > pad * pad)
                    {
                        continue;
                    }

                    Assert.AreEqual(0f, _surface.SampleHeight(x, z), 0f, $"pad height at ({x}, {z})");
                }
            }

            Assert.AreEqual(1f, _surface.SampleNormal(0f, 0f).y, 1e-6f);

            // Just outside the pad the ground must still be nearly level: no lip, no step.
            for (int i = 0; i < 36; i++)
            {
                Vector2 direction = MoonSurface.BearingToDirection(i * 10f);
                Vector2 p = direction * (pad + 2f);
                Assert.Less(Mathf.Abs(_surface.SampleHeight(p.x, p.y)), 0.05f, $"pad edge at bearing {i * 10}");
            }
        }

        [Test]
        public void DrivableFloor_NeverExceedsTheSlopeLimit()
        {
            float extent = _settings.FloorRadius + _settings.RimWarpAmplitude;
            float worst = 0f;
            Vector2 worstAt = Vector2.zero;
            int samples = 0;
            for (float z = -extent; z <= extent; z += 2f)
            {
                for (float x = -extent; x <= extent; x += 2f)
                {
                    if (!_surface.IsDrivable(x, z))
                    {
                        continue;
                    }

                    samples++;
                    float slope = SlopeDegrees(x, z);
                    if (slope > worst)
                    {
                        worst = slope;
                        worstAt = new Vector2(x, z);
                    }
                }
            }

            Assert.Greater(samples, 50000, "The drivable floor should cover most of the basin.");
            Assert.LessOrEqual(worst, MaxDrivableSlopeDegrees, $"steepest drivable point at {worstAt}");
        }

        [Test]
        public void PlayableArea_IsEntirelyDrivableFloor()
        {
            Rect area = _surface.PlayableArea;
            Assert.Greater(area.width, 400f);
            Assert.AreEqual(Vector2.zero, area.center);
            for (float z = area.yMin; z <= area.yMax; z += 4f)
            {
                for (float x = area.xMin; x <= area.xMax; x += 4f)
                {
                    Assert.IsTrue(_surface.IsDrivable(x, z), $"({x}, {z}) is outside the drivable floor");
                }
            }
        }

        [Test]
        public void Rim_RisesFarAboveTheFloor_InEveryDirection()
        {
            float highestFloor = float.MinValue;
            for (float z = -_surface.DrivableRadius; z <= _surface.DrivableRadius; z += 5f)
            {
                for (float x = -_surface.DrivableRadius; x <= _surface.DrivableRadius; x += 5f)
                {
                    if (x * x + z * z <= _surface.DrivableRadius * _surface.DrivableRadius)
                    {
                        highestFloor = Mathf.Max(highestFloor, _surface.SampleHeight(x, z));
                    }
                }
            }

            for (int bearing = 0; bearing < 360; bearing += 5)
            {
                Vector2 direction = MoonSurface.BearingToDirection(bearing);
                float highestRim = float.MinValue;
                for (float r = _settings.FloorRadius; r <= _settings.CrestRadius + 60f; r += 2f)
                {
                    highestRim = Mathf.Max(highestRim, _surface.SampleHeight(direction.x * r, direction.y * r));
                }

                Assert.Greater(highestRim, highestFloor + RimClearanceAboveFloor, $"rim too low at bearing {bearing}");
            }
        }

        [Test]
        public void ThePeak_IsTheGlobalMaximum_WithAFlatSummit()
        {
            Vector3 summit = _surface.PeakSummit;
            Assert.AreEqual(_settings.PeakHeight, summit.y);
            Assert.AreEqual(summit.y, _surface.SampleHeight(summit.x, summit.z), 1e-4f);

            float highest = float.MinValue;
            const float nearExtent = 800f;
            for (float z = -nearExtent; z <= nearExtent; z += 3f)
            {
                for (float x = -nearExtent; x <= nearExtent; x += 3f)
                {
                    highest = Mathf.Max(highest, _surface.SampleHeight(x, z));
                }
            }

            for (float z = -3000f; z <= 3000f; z += 20f)
            {
                for (float x = -3000f; x <= 3000f; x += 20f)
                {
                    highest = Mathf.Max(highest, _surface.SampleHeight(x, z));
                }
            }

            Assert.LessOrEqual(highest, summit.y + 1e-3f);

            float plateau = _surface.SummitRadius * 0.9f;
            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = MoonSurface.BearingToDirection(i * 22.5f) * plateau;
                Assert.AreEqual(summit.y, _surface.SampleHeight(summit.x + offset.x, summit.z + offset.y), 1e-3f,
                    "summit plateau must be flat for the satellite dish");
            }
        }

        [Test]
        public void ThePeak_IsVisibleFromTheBase()
        {
            var eye = new Vector3(0f, 2.5f, 0f);
            Vector3 target = _surface.PeakSummit + Vector3.up * BeaconHeightAboveSummit;
            Vector3 toTarget = target - eye;
            for (float t = 0.02f; t < 0.98f; t += 0.002f)
            {
                Vector3 p = eye + toTarget * t;
                Assert.Greater(p.y, _surface.SampleHeight(p.x, p.z), $"line of sight blocked at {p}");
            }

            float bearing = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;
            Assert.AreEqual(_settings.PeakBearing, bearing, 0.01f);
        }

        [Test]
        public void Craters_AreAllPlaced_ClearOfThePadAndInsideTheFloor()
        {
            Assert.AreEqual(_settings.CraterCount + _settings.Bowls.Length, _surface.Craters.Count);
            int bowls = 0;
            for (int i = 0; i < _surface.Craters.Count; i++)
            {
                Crater crater = _surface.Craters[i];
                float distance = crater.Center.magnitude;
                Assert.Greater(distance - crater.OuterRadius, _settings.PadRadius + _settings.PadBlend * 0.5f,
                    $"crater {i} intrudes on the base");
                Assert.Less(distance + crater.OuterRadius, _surface.DrivableRadius + 1f,
                    $"crater {i} leaves the floor");
                Assert.Less(_surface.SampleHeight(crater.Center.x, crater.Center.y), MeanRimHeight(crater),
                    $"crater {i} has no dip");
                if (crater.IsPlayBowl)
                {
                    bowls++;
                }
            }

            Assert.AreEqual(_settings.Bowls.Length, bowls);
        }

        [Test]
        public void Ramps_RiseAboveTheirSurroundings()
        {
            Assert.AreEqual(_settings.Ramps.Length, _surface.Ramps.Count);
            foreach (Ramp ramp in _surface.Ramps)
            {
                Vector2 before = ramp.Crest - ramp.Direction * (ramp.RiseLength + 2f);
                float crest = _surface.SampleHeight(ramp.Crest.x, ramp.Crest.y);
                Assert.Greater(crest - _surface.SampleHeight(before.x, before.y), ramp.Height * 0.5f);
                Assert.IsTrue(_surface.IsDrivable(ramp.Crest.x, ramp.Crest.y));
            }
        }

        [Test]
        public void Normals_HaveNoKinks_AndGentleCurvatureOnTheFloor()
        {
            // C1 means the normal turns in proportion to the distance travelled once the step is small: halving the
            // step must roughly halve the turn. Across a crease the turn would stay the same at any step size.
            var random = new SeededRandom(7u);
            const float step = 0.4f;
            const float maxTurnPerStep = 6f;
            const float smallStep = 0.05f;

            // Vector3.Angle is acos-based: angles below ~0.03 degrees are float noise.
            const float angleNoiseFloor = 0.05f;
            for (int i = 0; i < 20000; i++)
            {
                float x = random.Range(-320f, 320f);
                float z = random.Range(-320f, 320f);
                if (!_surface.IsDrivable(x, z))
                {
                    continue;
                }

                Vector3 normal = _surface.SampleNormal(x, z);
                float coarse = Vector3.Angle(normal, _surface.SampleNormal(x + step, z + step * 0.5f));
                float small = Vector3.Angle(normal, _surface.SampleNormal(x + smallStep, z + smallStep * 0.5f));
                float half = Vector3.Angle(normal, _surface.SampleNormal(x + smallStep * 0.5f, z + smallStep * 0.25f));
                Assert.Less(coarse, maxTurnPerStep, $"surface too tightly curved at ({x}, {z})");
                Assert.Less(half, small * 0.7f + angleNoiseFloor, $"crease at ({x}, {z})");
            }
        }

        [Test]
        public void Sampling_IsFastEnoughForBootTimeMeshing()
        {
            const int count = 200000;
            float sink = 0f;
            var random = new SeededRandom(3u);
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                sink += _surface.SampleHeight(random.Range(-500f, 500f), random.Range(-500f, 500f));
            }

            watch.Stop();
            double microseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / count;
            TestContext.WriteLine($"MoonSurface.SampleHeight: {microseconds:F3} us/sample (checksum {sink:F1})");
            Assert.Less(microseconds, 2.0, "height sampling is too slow for boot-time terrain generation");
        }

        /// <summary>Mean height around a crater's rim: robust to the hill the crater sits on.</summary>
        private float MeanRimHeight(Crater crater)
        {
            const int samples = 16;
            float sum = 0f;
            for (int k = 0; k < samples; k++)
            {
                Vector2 p = crater.Center + MoonSurface.BearingToDirection(k * 360f / samples) * crater.Radius;
                sum += _surface.SampleHeight(p.x, p.y);
            }

            return sum / samples;
        }

        private float SlopeDegrees(float x, float z)
        {
            return Mathf.Acos(Mathf.Clamp(_surface.SampleNormal(x, z).y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
