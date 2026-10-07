using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World.Tests
{
    public sealed class TerrainMeshTests
    {
        private const float SeamTolerance = 1e-3f;

        // Mesh vertices must sit on the surface within this (m, times one plus the steepest slope nearby, measured
        // over the probe step).
        private const float HeightTolerance = 1e-3f;
        private const float HeightProbe = 0.01f;

        // Floor tone (sRGB luminance, 0..1): its p10..p90 spread, and the most it may change across one facet on
        // the open floor and anywhere drivable (crater rims and walls are forms, so they may turn a little faster).
        private const float MinFloorSpread = 0.08f;
        private const float MaxOpenFacetStep = 0.06f;
        private const float MaxFacetStep = 0.25f;

        private static readonly PaletteSwatch[] TerrainSwatches =
        {
            PaletteSwatch.DustLight, PaletteSwatch.DustMid, PaletteSwatch.DustShadow, PaletteSwatch.RockLight,
            PaletteSwatch.RockDark, PaletteSwatch.Charcoal,
        };

        private MoonSurface _surface;
        private TerrainMeshSettings _settings;
        private TerrainPainter _painter;
        private TerrainChunkPlan[] _plans;
        private TerrainMeshData[] _chunks;
        private double _meshingMs;

        [OneTimeSetUp]
        public void BuildAllChunks()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new TerrainMeshSettings();
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(new AtmosphereSettings(), new SkySettings());
            _painter = new TerrainPainter(new TerrainPaintSettings(), WorldSettings.DefaultSeed, toLight);
            var mesher = new TerrainChunkMesher(_surface, _painter, _settings);
            _plans = TerrainChunkPlanner.Plan(_settings, _surface.Canyon.Touches);
            _chunks = new TerrainMeshData[_plans.Length];
            var watch = Stopwatch.StartNew();
            Parallel.For(0, _plans.Length, i => _chunks[i] = mesher.Build(_plans[i]));
            _meshingMs = watch.Elapsed.TotalMilliseconds;
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Validate());
        }

        [Test]
        public void Planner_CoversTheGrid_AndNeighboursAgreeOnCellSizes()
        {
            int perSide = Mathf.RoundToInt(_settings.GridHalfExtent * 2f / _settings.ChunkSize);
            Assert.AreEqual(perSide * perSide, _plans.Length);
            var bySlot = new Dictionary<Vector2Int, TerrainChunkPlan>();
            foreach (TerrainChunkPlan plan in _plans)
            {
                bySlot.Add(new Vector2Int(plan.Column, plan.Row), plan);
            }

            foreach (TerrainChunkPlan plan in _plans)
            {
                AssertNeighbour(bySlot, plan, -1, 0, plan.WestCellSize);
                AssertNeighbour(bySlot, plan, 1, 0, plan.EastCellSize);
                AssertNeighbour(bySlot, plan, 0, -1, plan.SouthCellSize);
                AssertNeighbour(bySlot, plan, 0, 1, plan.NorthCellSize);
            }
        }

        [Test]
        public void FineTier_CoversEverythingDrivable()
        {
            for (float z = -500f; z <= 500f; z += 4f)
            {
                for (float x = -500f; x <= 500f; x += 4f)
                {
                    if (_surface.IsDrivable(x, z))
                    {
                        Assert.AreEqual(_settings.FineCellSize, PlanAt(x, z).CellSize, $"({x}, {z}) is not fine");
                    }
                }
            }
        }

        [Test]
        public void Chunks_HaveUpFacingFacets_AndStayInTheTerrainPalette()
        {
            Color32 low = Palette.Get(TerrainSwatches[0]);
            Color32 high = low;
            foreach (PaletteSwatch swatch in TerrainSwatches)
            {
                Color32 c = Palette.Get(swatch);
                low = new Color32((byte)Mathf.Min(low.r, c.r), (byte)Mathf.Min(low.g, c.g), (byte)Mathf.Min(low.b, c.b),
                    255);
                high = new Color32((byte)Mathf.Max(high.r, c.r), (byte)Mathf.Max(high.g, c.g),
                    (byte)Mathf.Max(high.b, c.b), 255);
            }

            string firstProblem = null;
            foreach (TerrainMeshData chunk in _chunks)
            {
                for (int i = 0; i < chunk.Vertices.Length && firstProblem == null; i++)
                {
                    TerrainVertex vertex = chunk.Vertices[i];
                    Color32 c = vertex.Color;
                    if (!(vertex.Normal.y > 0f) || Mathf.Abs(vertex.Normal.magnitude - 1f) > 1e-4f)
                    {
                        firstProblem = $"{chunk.Name}: folded or bad normal on triangle {i / 3}: {vertex.Normal}";
                    }
                    else if (c.r < low.r || c.g < low.g || c.b < low.b || c.r > high.r || c.g > high.g || c.b > high.b)
                    {
                        firstProblem = $"{chunk.Name}: colour {c} outside the terrain palette's range";
                    }
                }
            }

            Assert.IsNull(firstProblem);
        }

        [Test]
        public void FloorDust_ChangesValueGently_InPatches_NeverAsConfetti()
        {
            // Design ruling 9 and pillar 6: the floor varies in broad patches, and a facet never jumps out of its
            // surroundings (no paper-cut shapes, no confetti), so across any one facet the tone barely changes.
            const float floorRadius = 280f;
            var luminances = new List<float>();
            float steepest = 0f;
            float steepestOpen = 0f;
            for (int c = 0; c < _chunks.Length; c++)
            {
                TerrainMeshData chunk = _chunks[c];
                if (_plans[c].CellSize != _settings.FineCellSize)
                {
                    continue;
                }

                for (int t = 0; t < chunk.TriangleCount; t++)
                {
                    Vector3 world = chunk.Vertices[t * 3].Position + chunk.Origin;
                    if (new Vector2(world.x, world.z).magnitude > floorRadius
                        || !_surface.IsDrivable(world.x, world.z))
                    {
                        continue;
                    }

                    float l0 = Luminance(chunk.Vertices[t * 3].Color);
                    float l1 = Luminance(chunk.Vertices[t * 3 + 1].Color);
                    float l2 = Luminance(chunk.Vertices[t * 3 + 2].Color);
                    float step = Mathf.Max(Mathf.Abs(l0 - l1), Mathf.Max(Mathf.Abs(l1 - l2), Mathf.Abs(l2 - l0)));
                    steepest = Mathf.Max(steepest, step);
                    if (OnOpenFloor(chunk, t))
                    {
                        steepestOpen = Mathf.Max(steepestOpen, step);
                    }

                    luminances.Add(l0);
                }
            }

            luminances.Sort();
            float spread = luminances[luminances.Count * 9 / 10] - luminances[luminances.Count / 10];
            TestContext.WriteLine($"Floor tone spread (p10..p90): {spread:F3}, largest change across a facet: " +
                                  $"{steepestOpen:F3} on the open floor, {steepest:F3} anywhere drivable");
            Assert.Greater(spread, MinFloorSpread, "the floor should still vary in patches");
            Assert.LessOrEqual(steepestOpen, MaxOpenFacetStep, "an open-floor facet changes tone sharply (paper cut)");
            Assert.LessOrEqual(steepest, MaxFacetStep, "a facet changes tone too sharply");
        }

        private bool OnOpenFloor(TerrainMeshData chunk, int triangle)
        {
            for (int k = 0; k < 3; k++)
            {
                Vector3 p = chunk.Vertices[triangle * 3 + k].Position + chunk.Origin;
                SurfaceSample sample = _surface.Sample(p.x, p.z);
                if (sample.CraterBowl > 0f || sample.CraterRim > 0f || sample.CanyonFloor > 0f)
                {
                    return false;
                }
            }

            return true;
        }

        private static float Luminance(Color32 c)
        {
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
        }

        [Test]
        public void RenderAndColliderMeshes_DescribeTheSameSurface()
        {
            foreach (TerrainMeshData chunk in _chunks)
            {
                Assert.IsTrue(chunk.HasCollider);
                Assert.AreEqual(chunk.Vertices.Length, chunk.ColliderIndices.Length);
                int mismatches = 0;
                for (int i = 0; i < chunk.Vertices.Length; i++)
                {
                    if (chunk.ColliderVertices[chunk.ColliderIndices[i]] != chunk.Vertices[i].Position)
                    {
                        mismatches++;
                    }
                }

                Assert.AreEqual(0, mismatches, $"{chunk.Name}: render and collider vertices differ");
            }
        }

        [Test]
        public void InteriorHeights_ComeFromTheSurfaceFunction()
        {
            for (int c = 0; c < _chunks.Length; c++)
            {
                TerrainMeshData chunk = _chunks[c];
                float size = _plans[c].Size;
                foreach (Vector3 local in chunk.ColliderVertices)
                {
                    if (local.x <= 0f || local.x >= size || local.z <= 0f || local.z >= size)
                    {
                        continue;
                    }

                    // Out at the rim the height function is only float-accurate to a few 1e-4 m (its noise terms
                    // sum large magnitudes), and storing positions chunk-local moves them by about as much, which
                    // steep canyon walls (and the creases at their feet) multiply by their slope.
                    Vector3 world = local + chunk.Origin;
                    float height = _surface.SampleHeight(world.x, world.z);
                    float slope = SteepestRise(world.x, world.z, height) / HeightProbe;
                    Assert.AreEqual(height, world.y, HeightTolerance * (1f + slope), chunk.Name);
                }
            }
        }

        /// <summary>Largest height change one <see cref="HeightProbe"/> step away along either axis.</summary>
        private float SteepestRise(float x, float z, float height)
        {
            float rise = Mathf.Abs(_surface.SampleHeight(x + HeightProbe, z) - height);
            rise = Mathf.Max(rise, Mathf.Abs(_surface.SampleHeight(x - HeightProbe, z) - height));
            rise = Mathf.Max(rise, Mathf.Abs(_surface.SampleHeight(x, z + HeightProbe) - height));
            return Mathf.Max(rise, Mathf.Abs(_surface.SampleHeight(x, z - HeightProbe) - height));
        }

        [Test]
        public void ChunkBorders_AreWatertight_EvenBetweenTiers()
        {
            var bySlot = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < _plans.Length; i++)
            {
                bySlot.Add(new Vector2Int(_plans[i].Column, _plans[i].Row), i);
            }

            int seams = 0;
            for (int i = 0; i < _plans.Length; i++)
            {
                TerrainChunkPlan plan = _plans[i];
                if (bySlot.TryGetValue(new Vector2Int(plan.Column + 1, plan.Row), out int east))
                {
                    AssertSeam(BorderLine(i, true, plan.Size), BorderLine(east, true, 0f), _chunks[i].Name);
                    seams++;
                }

                if (bySlot.TryGetValue(new Vector2Int(plan.Column, plan.Row + 1), out int north))
                {
                    AssertSeam(BorderLine(i, false, plan.Size), BorderLine(north, false, 0f), _chunks[i].Name);
                    seams++;
                }
            }

            Assert.Greater(seams, 0);
        }

        [Test]
        public void Backdrop_StaysBelowTheChunks_AndFacesUp()
        {
            TerrainMeshData backdrop = BackdropMesher.Build(_surface, _painter, _settings);
            Assert.IsFalse(backdrop.HasCollider);
            float inner = _settings.GridHalfExtent - 20f;
            foreach (TerrainVertex vertex in backdrop.Vertices)
            {
                Assert.Greater(vertex.Normal.y, 0f);
                Vector3 p = vertex.Position;
                if (Mathf.Abs(p.x) < inner && Mathf.Abs(p.z) < inner)
                {
                    Assert.Less(p.y, _surface.SampleHeight(p.x, p.z) - _settings.BackdropSink * 0.9f);
                }
            }
        }

        [Test]
        public void Meshing_TheWholeGrid_FitsTheBootBudget()
        {
            int triangles = 0;
            foreach (TerrainMeshData chunk in _chunks)
            {
                triangles += chunk.TriangleCount;
            }

            TestContext.WriteLine($"Meshed {_chunks.Length} chunks, {triangles} triangles in {_meshingMs:0} ms");
            Assert.LessOrEqual(triangles, 300000, "terrain triangle budget");
            Assert.Less(_meshingMs, 1000.0, "parallel chunk meshing is too slow for boot-time generation");
        }

        /// <summary>Key of edge <paramref name="k"/> of triangle <paramref name="t"/>, equal from both sides.</summary>
        private static long Edge(TerrainMeshData chunk, int t, int k)
        {
            int a = chunk.ColliderIndices[t * 3 + k];
            int b = chunk.ColliderIndices[t * 3 + (k + 1) % 3];
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }

        private TerrainChunkPlan PlanAt(float x, float z)
        {
            foreach (TerrainChunkPlan plan in _plans)
            {
                if (x >= plan.Origin.x && x < plan.Origin.x + plan.Size && z >= plan.Origin.y
                    && z < plan.Origin.y + plan.Size)
                {
                    return plan;
                }
            }

            Assert.Fail($"No chunk covers ({x}, {z}).");
            return default;
        }

        /// <summary>World positions of a chunk's vertices on one local border line, sorted along the border.</summary>
        private List<Vector3> BorderLine(int chunkIndex, bool vertical, float localLine)
        {
            TerrainMeshData chunk = _chunks[chunkIndex];
            var line = new List<Vector3>();
            foreach (Vector3 local in chunk.ColliderVertices)
            {
                if (Mathf.Abs((vertical ? local.x : local.z) - localLine) < SeamTolerance)
                {
                    line.Add(local + chunk.Origin);
                }
            }

            line.Sort((a, b) => vertical ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x));
            return line;
        }

        private static void AssertSeam(List<Vector3> a, List<Vector3> b, string name)
        {
            Assert.GreaterOrEqual(a.Count, 2, name);
            Assert.GreaterOrEqual(b.Count, 2, name);
            AssertOnPolyline(a, b, name);
            AssertOnPolyline(b, a, name);
        }

        private static void AssertOnPolyline(List<Vector3> points, List<Vector3> polyline, string name)
        {
            foreach (Vector3 point in points)
            {
                float best = float.MaxValue;
                for (int i = 0; i + 1 < polyline.Count; i++)
                {
                    best = Mathf.Min(best, DistanceToSegment(point, polyline[i], polyline[i + 1]));
                }

                Assert.Less(best, SeamTolerance * 10f, $"{name}: crack at {point}");
            }
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        private static void AssertNeighbour(Dictionary<Vector2Int, TerrainChunkPlan> bySlot, TerrainChunkPlan plan,
            int dx, int dz, float reported)
        {
            float expected = bySlot.TryGetValue(new Vector2Int(plan.Column + dx, plan.Row + dz),
                out TerrainChunkPlan neighbour)
                ? neighbour.CellSize
                : plan.CellSize;
            Assert.AreEqual(expected, reported, $"chunk {plan.Column},{plan.Row} neighbour {dx},{dz}");
        }
    }
}
