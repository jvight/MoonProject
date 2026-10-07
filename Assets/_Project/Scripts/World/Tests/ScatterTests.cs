using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    public sealed class ScatterTests
    {
        private MoonSurface _surface;
        private ScatterSettings _settings;
        private ScatterPlanner _planner;
        private List<ScatterInstance> _plan;
        private double _planMs;

        [OneTimeSetUp]
        public void PlanOnce()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new ScatterSettings();
            _planner = new ScatterPlanner(_surface, _settings);
            var watch = Stopwatch.StartNew();
            _plan = _planner.Plan();
            _planMs = watch.Elapsed.TotalMilliseconds;
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Validate());
        }

        [Test]
        public void PoissonSampler_KeepsItsMinimumDistance_InsideTheDisc()
        {
            List<Vector2> points = PoissonDiskSampler.SampleDisc(60f, 4f, 11u);
            Assert.Greater(points.Count, 400, "the disc should be filled");
            for (int i = 0; i < points.Count; i++)
            {
                Assert.LessOrEqual(points[i].magnitude, 60f);
                for (int j = i + 1; j < points.Count; j++)
                {
                    Assert.GreaterOrEqual(Vector2.Distance(points[i], points[j]), 4f - 1e-4f);
                }
            }
        }

        [Test]
        public void Plan_IsDeterministic()
        {
            List<ScatterInstance> again = new ScatterPlanner(_surface, _settings).Plan();
            Assert.AreEqual(_plan.Count, again.Count);
            for (int i = 0; i < _plan.Count; i++)
            {
                Assert.AreEqual(_plan[i].Position, again[i].Position);
                Assert.AreEqual(_plan[i].Size, again[i].Size);
                Assert.AreEqual(_plan[i].Variant, again[i].Variant);
            }
        }

        [Test]
        public void Counts_StayWithinTheRenderingBudget()
        {
            int pebbles = Count(ScatterKind.Pebble);
            int boulders = Count(ScatterKind.Boulder);
            TestContext.WriteLine($"Scatter plan: {pebbles} pebbles, {boulders} boulders in {_planMs:0} ms");
            Assert.That(pebbles, Is.InRange(2000, 12000));
            Assert.That(boulders, Is.InRange(80, 600));
        }

        [Test]
        public void Rocks_SitOnTheSurface_InsideTheExtent_WithinTheirSizes()
        {
            foreach (ScatterInstance rock in _plan)
            {
                var flat = new Vector2(rock.Position.x, rock.Position.z);
                Assert.LessOrEqual(flat.magnitude, _settings.ExtentRadius);
                Assert.AreEqual(_surface.SampleHeight(flat.x, flat.y), rock.Position.y, 1e-4f);
                Vector2 range = rock.Kind == ScatterKind.Boulder ? _settings.BoulderSize : _settings.PebbleSize;
                Assert.That(rock.Size, Is.InRange(range.x, range.y));
                Assert.GreaterOrEqual(rock.Variant, 0);
            }
        }

        [Test]
        public void BasePad_IsClearOfRocks()
        {
            float clear = _surface.PadRadius + _settings.PadClearance;
            foreach (ScatterInstance rock in _plan)
            {
                Assert.Greater(new Vector2(rock.Position.x, rock.Position.z).magnitude, clear);
            }
        }

        [Test]
        public void Boulders_KeepLanesFeaturesCratersAndSteepGroundClear()
        {
            float maxSlopeCos = Mathf.Cos(_settings.BoulderMaxSlope * Mathf.Deg2Rad);
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind != ScatterKind.Boulder)
                {
                    continue;
                }

                var flat = new Vector2(rock.Position.x, rock.Position.z);
                Assert.GreaterOrEqual(_planner.DistanceToLanes(flat), _settings.LaneHalfWidth,
                    $"boulder in a lane at {flat}");
                Assert.AreEqual(0f, _surface.Sample(flat.x, flat.y).CraterBowl, $"boulder inside a crater at {flat}");
                Assert.GreaterOrEqual(rock.Normal.y, maxSlopeCos, $"boulder on a steep face at {flat}");
                foreach (Ramp ramp in _surface.Ramps)
                {
                    Assert.Greater(Vector2.Distance(flat, ramp.Crest), ramp.BoundingRadius,
                        $"boulder on a ramp at {flat}");
                }
            }
        }

        [Test]
        public void Canyon_KeepsItsFloorsClearOfBoulders_AndItsDrivingLineClearOfPebbles()
        {
            Canyon canyon = _surface.Canyon;
            foreach (ScatterInstance rock in _plan)
            {
                SurfaceSample sample = _surface.Sample(rock.Position.x, rock.Position.z);
                if (rock.Kind == ScatterKind.Boulder)
                {
                    Assert.AreEqual(0f, sample.CanyonFloor, $"boulder on a canyon floor at {rock.Position}");
                    continue;
                }

                Assert.AreEqual(0f, sample.Chasm, $"pebble in the chasm at {rock.Position}");
                if (canyon.TryFloor(rock.Position.x, rock.Position.z, out bool _, out float _, out float centre))
                {
                    Assert.GreaterOrEqual(centre, _settings.CanyonPebbleClear,
                        $"pebble on the canyon's driving line at {rock.Position}");
                }
            }
        }

        [Test]
        public void SameClassRocks_KeepTheirPoissonSpacing()
        {
            AssertSpacing(ScatterKind.Boulder, _settings.BoulderSpacing);
            AssertSpacing(ScatterKind.Pebble, _settings.PebbleSpacing);
        }

        [Test]
        public void Boulders_GatherAtTheFootOfTheRim_AndOnCraterRims()
        {
            int talus = 0;
            int ejecta = 0;
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind != ScatterKind.Boulder)
                {
                    continue;
                }

                if (_surface.FloorEdgeDistance(rock.Position.x, rock.Position.z) < 0f)
                {
                    talus++;
                }

                if (_surface.Sample(rock.Position.x, rock.Position.z).CraterRim > 0.3f)
                {
                    ejecta++;
                }
            }

            Assert.Greater(talus, 40, "the foot of the rim should carry a talus of boulders");
            Assert.Greater(ejecta, 10, "raised crater rims should carry ejecta boulders");
        }

        private void AssertSpacing(ScatterKind kind, float spacing)
        {
            var points = new List<Vector2>();
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind == kind)
                {
                    points.Add(new Vector2(rock.Position.x, rock.Position.z));
                }
            }

            // Cell hashing keeps the all-pairs check linear: only neighbouring cells can be closer than the spacing.
            var cells = new Dictionary<Vector2Int, List<Vector2>>();
            foreach (Vector2 point in points)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(point.x / spacing), Mathf.FloorToInt(point.y / spacing));
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (cells.TryGetValue(cell + new Vector2Int(dx, dz), out List<Vector2> near))
                        {
                            foreach (Vector2 other in near)
                            {
                                Assert.GreaterOrEqual(Vector2.Distance(point, other), spacing - 1e-3f, kind.ToString());
                            }
                        }
                    }
                }

                if (!cells.TryGetValue(cell, out List<Vector2> list))
                {
                    list = new List<Vector2>();
                    cells.Add(cell, list);
                }

                list.Add(point);
            }
        }

        private int Count(ScatterKind kind)
        {
            int count = 0;
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
