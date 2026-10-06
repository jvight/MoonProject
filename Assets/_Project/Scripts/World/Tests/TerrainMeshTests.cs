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

        private static readonly PaletteSwatch[] TerrainSwatches =
        {
            PaletteSwatch.DustLight, PaletteSwatch.DustMid, PaletteSwatch.DustShadow, PaletteSwatch.RockLight,
            PaletteSwatch.RockDark,
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
            _plans = TerrainChunkPlanner.Plan(_settings);
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
            for (float z = -400f; z <= 400f; z += 4f)
            {
                for (float x = -400f; x <= 400f; x += 4f)
                {
                    if (_surface.IsDrivable(x, z))
                    {
                        Assert.AreEqual(_settings.FineCellSize, PlanAt(x, z).CellSize, $"({x}, {z}) is not fine");
                    }
                }
            }
        }

        [Test]
        public void Chunks_HaveUpFacingFacets_AndOnlyTerrainSwatches()
        {
            var allowed = new HashSet<Vector2>();
            foreach (PaletteSwatch swatch in TerrainSwatches)
            {
                allowed.Add(Palette.Uv(swatch));
            }

            string firstProblem = null;
            foreach (TerrainMeshData chunk in _chunks)
            {
                for (int i = 0; i < chunk.Vertices.Length && firstProblem == null; i++)
                {
                    TerrainVertex vertex = chunk.Vertices[i];
                    if (!(vertex.Normal.y > 0f) || Mathf.Abs(vertex.Normal.magnitude - 1f) > 1e-4f)
                    {
                        firstProblem = $"{chunk.Name}: folded or bad normal on triangle {i / 3}: {vertex.Normal}";
                    }
                    else if (!allowed.Contains(vertex.Uv))
                    {
                        firstProblem = $"{chunk.Name}: unexpected swatch UV {vertex.Uv}";
                    }
                }
            }

            Assert.IsNull(firstProblem);
        }

        [Test]
        public void FloorDust_ComesInPatches_NeverConfetti()
        {
            // Design ruling 9: a bright facet with no bright neighbour reads as a paper scrap on the ground.
            const float floorRadius = 280f;
            const float maxIsolatedShare = 0.02f;
            Vector2 light = Palette.Uv(PaletteSwatch.DustLight);
            int lightFacets = 0;
            int isolated = 0;
            for (int c = 0; c < _chunks.Length; c++)
            {
                TerrainMeshData chunk = _chunks[c];
                if (_plans[c].CellSize != _settings.FineCellSize)
                {
                    continue;
                }

                var byEdge = new Dictionary<long, List<int>>();
                int triangles = chunk.TriangleCount;
                for (int t = 0; t < triangles; t++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        long key = Edge(chunk, t, k);
                        if (!byEdge.TryGetValue(key, out List<int> owners))
                        {
                            owners = new List<int>(2);
                            byEdge.Add(key, owners);
                        }

                        owners.Add(t);
                    }
                }

                for (int t = 0; t < triangles; t++)
                {
                    Vector3 world = chunk.Vertices[t * 3].Position + chunk.Origin;
                    if (chunk.Vertices[t * 3].Uv != light || new Vector2(world.x, world.z).magnitude > floorRadius)
                    {
                        continue;
                    }

                    lightFacets++;
                    bool hasLightNeighbour = false;
                    for (int k = 0; k < 3 && !hasLightNeighbour; k++)
                    {
                        long key = Edge(chunk, t, k);
                        foreach (int other in byEdge[key])
                        {
                            hasLightNeighbour |= other != t && chunk.Vertices[other * 3].Uv == light;
                        }
                    }

                    if (!hasLightNeighbour)
                    {
                        isolated++;
                    }
                }
            }

            TestContext.WriteLine($"Floor DustLight facets: {lightFacets}, isolated: {isolated}");
            Assert.Greater(lightFacets, 1000, "the floor should still carry light dust patches");
            Assert.LessOrEqual(isolated, lightFacets * maxIsolatedShare, "isolated bright facets (confetti)");
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

                    Vector3 world = local + chunk.Origin;
                    Assert.AreEqual(_surface.SampleHeight(world.x, world.z), world.y, 1e-4f, chunk.Name);
                }
            }
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
