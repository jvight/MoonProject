using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Turns one <see cref="TerrainChunkPlan"/> into smooth-shaded, indexed painted mesh data plus a matching collider.
    /// Heights and normals come from the analytic surface; only the XZ lattice is jittered for an organic layout.
    /// Chunk borders are shared exactly with the neighbour: border vertices only slide along the border (seeded by
    /// world position, so both chunks agree), corners never move, and where a finer chunk meets a coarser one its
    /// in-between border vertices interpolate both position and normal along the coarse edge, so the seam is
    /// watertight and shading-continuous. Immutable and thread-safe.
    /// </summary>
    public sealed class TerrainChunkMesher
    {
        private const uint JitterXSalt = 0x3C6EF372u;
        private const uint JitterZSalt = 0xA54FF53Au;
        private const uint BorderSalt = 0x510E527Fu;
        private const uint DiagonalSalt = 0x9B05688Cu;

        // Lattice coordinates are whole multiples of the cell size; this absorbs float rounding when snapping.
        private const float LatticeEpsilon = 1e-3f;

        private readonly MoonSurface _surface;
        private readonly TerrainPainter _painter;
        private readonly GradientNoise _pocketNoise;
        private readonly float _jitter;
        private readonly float _chunkSize;
        private readonly uint _seed;

        public TerrainChunkMesher(MoonSurface surface, TerrainPainter painter, TerrainMeshSettings settings)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _painter = painter ?? throw new ArgumentNullException(nameof(painter));
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _jitter = settings.Jitter;
            _chunkSize = settings.ChunkSize;
            _seed = (uint)surface.Seed;
            _pocketNoise = new GradientNoise(Hashing.Mix(_seed ^ 0x4B1D7E31u));
        }

        public TerrainMeshData Build(TerrainChunkPlan plan)
        {
            int cells = plan.Cells;
            int stride = cells + 1;
            int vertexCount = stride * stride;
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var regions = new SurfaceSample[vertexCount];
            for (int j = 0; j <= cells; j++)
            {
                for (int i = 0; i <= cells; i++)
                {
                    int idx = j * stride + i;
                    PlaceVertex(plan, i, j, out positions[idx], out normals[idx], out regions[idx]);
                }
            }

            var origin = new Vector3(plan.Origin.x, 0f, plan.Origin.y);
            var vertices = new TerrainVertex[vertexCount];
            var colliderVertices = new Vector3[vertexCount];
            Vector3 min = positions[0] - origin;
            Vector3 max = min;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 local = positions[i] - origin;
                colliderVertices[i] = local;
                Color32 color = _painter.PaintVertex(positions[i], normals[i], regions[i]);
                vertices[i] = new TerrainVertex(local, normals[i], color);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            var indices = new int[cells * cells * 6];
            uint cellKey = CellKey(plan.CellSize);
            int written = 0;
            for (int j = 0; j < cells; j++)
            {
                for (int i = 0; i < cells; i++)
                {
                    int a = j * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    int gi = Mathf.RoundToInt(plan.Origin.x / plan.CellSize) + i;
                    int gj = Mathf.RoundToInt(plan.Origin.y / plan.CellSize) + j;
                    uint quadHash = Hashing.Hash(gi, gj, _seed ^ DiagonalSalt ^ cellKey);

                    // Each quad picks one of its two diagonals at random (no grid-aligned stripes),
                    // unless jitter made the quad non-convex and that diagonal would fold a triangle over.
                    bool mainDiagonal = (quadHash & 1u) == 0u;
                    if (mainDiagonal && !(FacesUp(positions, a, c, d) && FacesUp(positions, a, d, b)))
                    {
                        mainDiagonal = false;
                    }
                    else if (!mainDiagonal && !(FacesUp(positions, a, c, b) && FacesUp(positions, b, c, d)))
                    {
                        mainDiagonal = true;
                    }

                    if (mainDiagonal)
                    {
                        indices[written++] = a;
                        indices[written++] = c;
                        indices[written++] = d;
                        indices[written++] = a;
                        indices[written++] = d;
                        indices[written++] = b;
                    }
                    else
                    {
                        indices[written++] = a;
                        indices[written++] = c;
                        indices[written++] = b;
                        indices[written++] = b;
                        indices[written++] = c;
                        indices[written++] = d;
                    }
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return new TerrainMeshData($"TerrainChunk_{plan.Column}_{plan.Row}", origin, vertices, colliderVertices,
                indices, bounds, indices);
        }

        /// <summary>True when the triangle's XZ projection keeps the upward winding (it is not folded).</summary>
        private static bool FacesUp(Vector3[] positions, int i0, int i1, int i2)
        {
            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];
            return (p1.z - p0.z) * (p2.x - p0.x) - (p1.x - p0.x) * (p2.z - p0.z) > 0f;
        }

        private void PlaceVertex(TerrainChunkPlan plan, int i, int j, out Vector3 position, out Vector3 normal,
            out SurfaceSample region)
        {
            int cells = plan.Cells;
            float cell = plan.CellSize;
            float x = plan.Origin.x + i * cell;
            float z = plan.Origin.y + j * cell;
            bool onVerticalBorder = i == 0 || i == cells;
            bool onHorizontalBorder = j == 0 || j == cells;

            if (onVerticalBorder && onHorizontalBorder)
            {
                Sample(x, z, out position, out normal, out region);
            }
            else if (onVerticalBorder)
            {
                float neighbour = i == 0 ? plan.WestCellSize : plan.EastCellSize;
                BorderVertex(x, z, cell, neighbour, true, out position, out normal, out region);
            }
            else if (onHorizontalBorder)
            {
                float neighbour = j == 0 ? plan.SouthCellSize : plan.NorthCellSize;
                BorderVertex(z, x, cell, neighbour, false, out position, out normal, out region);
            }
            else
            {
                int gi = Mathf.RoundToInt(x / cell);
                int gj = Mathf.RoundToInt(z / cell);
                uint key = _seed ^ CellKey(cell);
                float amplitude = _jitter * cell;
                float jx = Hashing.ToSigned(Hashing.Hash(gi, gj, key ^ JitterXSalt)) * amplitude;
                float jz = Hashing.ToSigned(Hashing.Hash(gi, gj, key ^ JitterZSalt)) * amplitude;
                Sample(x + jx, z + jz, out position, out normal, out region);
            }
        }

        /// <summary>
        /// A vertex on a chunk border line (<paramref name="line"/> = the border's X for a vertical border, Z for a
        /// horizontal one; <paramref name="along"/> = the coordinate along the border).
        /// </summary>
        private void BorderVertex(float line, float along, float ownCell, float neighbourCell, bool vertical,
            out Vector3 position, out Vector3 normal, out SurfaceSample region)
        {
            float anchorCell = Mathf.Max(ownCell, neighbourCell);
            float fineCell = Mathf.Min(ownCell, neighbourCell);
            float steps = along / anchorCell;
            int k = Mathf.FloorToInt(steps + LatticeEpsilon);
            float t = steps - k;
            Anchor(line, k, anchorCell, fineCell, vertical, out position, out normal, out region);
            if (t <= LatticeEpsilon)
            {
                return;
            }

            Anchor(line, k + 1, anchorCell, fineCell, vertical, out Vector3 next, out Vector3 nextNormal,
                out SurfaceSample nextRegion);
            position = Vector3.Lerp(position, next, t);
            normal = Vector3.Lerp(normal, nextNormal, t).normalized;
            region = new SurfaceSample(position.y,
                Mathf.Lerp(region.CraterBowl, nextRegion.CraterBowl, t),
                Mathf.Lerp(region.CraterRim, nextRegion.CraterRim, t),
                Mathf.Lerp(region.RimZone, nextRegion.RimZone, t),
                Mathf.Lerp(region.CanyonFloor, nextRegion.CanyonFloor, t),
                Mathf.Lerp(region.Chasm, nextRegion.Chasm, t),
                Mathf.Lerp(region.Scorch, nextRegion.Scorch, t));
        }

        private void Anchor(float line, int k, float anchorCell, float fineCell, bool vertical, out Vector3 position,
            out Vector3 normal, out SurfaceSample region)
        {
            float along = k * anchorCell;
            float corner = along / _chunkSize;
            float slide = 0f;
            if (Mathf.Abs(corner - Mathf.Round(corner)) > LatticeEpsilon)
            {
                int lineIndex = Mathf.RoundToInt(line / _chunkSize);
                uint key = _seed ^ BorderSalt ^ CellKey(anchorCell) ^ (vertical ? 0x55555555u : 0xAAAAAAAAu);
                slide = Hashing.ToSigned(Hashing.Hash(lineIndex, k, key)) * _jitter * fineCell;
            }

            if (vertical)
            {
                Sample(line, along + slide, out position, out normal, out region);
            }
            else
            {
                Sample(along + slide, line, out position, out normal, out region);
            }
        }

        private void Sample(float x, float z, out Vector3 position, out Vector3 normal, out SurfaceSample region)
        {
            region = _surface.Sample(x, z);
            position = new Vector3(x, region.Height, z);
            float r = Mathf.Sqrt(x * x + z * z);
            if (region.RimZone <= 1e-4f || r <= 1f)
            {
                normal = _surface.SampleNormal(x, z);
                return;
            }

            float tRim = SmoothMath.Smootherstep(0f, 0.2f, region.RimZone);
            float pocket = _pocketNoise.Fractal(x / 68f, z / 68f, 2, 2f, 0.5f);
            float slumpPocket = SmoothMath.Smootherstep(-0.05f, 0.35f, pocket);
            float stepR = Mathf.Lerp(0.25f, Mathf.Lerp(4.8f, 1.35f, slumpPocket), tRim);
            float stepT = Mathf.Lerp(0.25f, 1.1f, tRim);
            float urx = x / r;
            float urz = z / r;
            float utx = -urz;
            float utz = urx;
            float gr = (_surface.SampleHeight(x + stepR * urx, z + stepR * urz)
                - _surface.SampleHeight(x - stepR * urx, z - stepR * urz)) / (2f * stepR);
            float gt = (_surface.SampleHeight(x + stepT * utx, z + stepT * utz)
                - _surface.SampleHeight(x - stepT * utx, z - stepT * utz)) / (2f * stepT);
            normal = new Vector3(-(gr * urx + gt * utx), 1f, -(gr * urz + gt * utz)).normalized;
        }

        private static uint CellKey(float cellSize)
        {
            return Hashing.Mix((uint)Mathf.RoundToInt(cellSize * 64f));
        }
    }
}
