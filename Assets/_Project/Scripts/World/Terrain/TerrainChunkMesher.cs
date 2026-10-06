using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Turns one <see cref="TerrainChunkPlan"/> into flat-shaded, palette-painted mesh data plus a matching collider.
    /// Heights always come from the analytic surface; only the XZ lattice is jittered for a handmade look.
    /// Chunk borders are shared exactly with the neighbour: border vertices only slide along the border (seeded by
    /// world position, so both chunks agree), corners never move, and where a finer chunk meets a coarser one its
    /// in-between border vertices sit on the coarse edge, so the seam is watertight. Immutable and thread-safe.
    /// </summary>
    public sealed class TerrainChunkMesher
    {
        private const uint JitterXSalt = 0x3C6EF372u;
        private const uint JitterZSalt = 0xA54FF53Au;
        private const uint BorderSalt = 0x510E527Fu;
        private const uint DiagonalSalt = 0x9B05688Cu;
        private const uint PaintSalt = 0x1F83D9ABu;

        // Lattice coordinates are whole multiples of the cell size; this absorbs float rounding when snapping.
        private const float LatticeEpsilon = 1e-3f;

        private readonly MoonSurface _surface;
        private readonly TerrainPainter _painter;
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
        }

        public TerrainMeshData Build(TerrainChunkPlan plan)
        {
            int cells = plan.Cells;
            int stride = cells + 1;
            var positions = new Vector3[stride * stride];
            var regions = new SurfaceSample[stride * stride];
            for (int j = 0; j <= cells; j++)
            {
                for (int i = 0; i <= cells; i++)
                {
                    PlaceVertex(plan, i, j, out positions[j * stride + i], out regions[j * stride + i]);
                }
            }

            var origin = new Vector3(plan.Origin.x, 0f, plan.Origin.y);
            var vertices = new TerrainVertex[cells * cells * 6];
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
                    uint paintHash = Hashing.Hash(gi, gj, _seed ^ PaintSalt ^ cellKey);

                    // Each quad picks one of its two diagonals at random (no grid-aligned stripes in the facets),
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
                        Emit(positions, regions, a, c, d, origin, paintHash, vertices, indices, ref written);
                        Emit(positions, regions, a, d, b, origin, Hashing.Mix(paintHash), vertices, indices,
                            ref written);
                    }
                    else
                    {
                        Emit(positions, regions, a, c, b, origin, paintHash, vertices, indices, ref written);
                        Emit(positions, regions, b, c, d, origin, Hashing.Mix(paintHash), vertices, indices,
                            ref written);
                    }
                }
            }

            var colliderVertices = new Vector3[positions.Length];
            Vector3 min = positions[0] - origin;
            Vector3 max = min;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 local = positions[i] - origin;
                colliderVertices[i] = local;
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return new TerrainMeshData($"TerrainChunk_{plan.Column}_{plan.Row}", origin, vertices, colliderVertices,
                indices, bounds);
        }

        private void Emit(Vector3[] positions, SurfaceSample[] regions, int i0, int i1, int i2, Vector3 origin,
            uint hash, TerrainVertex[] vertices, int[] indices, ref int written)
        {
            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            SurfaceSample r0 = regions[i0];
            SurfaceSample r1 = regions[i1];
            SurfaceSample r2 = regions[i2];
            var region = new SurfaceSample(0f, (r0.CraterBowl + r1.CraterBowl + r2.CraterBowl) / 3f,
                (r0.CraterRim + r1.CraterRim + r2.CraterRim) / 3f, (r0.RimZone + r1.RimZone + r2.RimZone) / 3f);
            Vector2 uv = Palette.Uv(_painter.Pick(normal, (p0 + p1 + p2) / 3f, region, hash));

            indices[written] = i0;
            vertices[written++] = new TerrainVertex(p0 - origin, normal, uv);
            indices[written] = i1;
            vertices[written++] = new TerrainVertex(p1 - origin, normal, uv);
            indices[written] = i2;
            vertices[written++] = new TerrainVertex(p2 - origin, normal, uv);
        }

        /// <summary>True when the triangle's XZ projection keeps the upward winding (it is not folded).</summary>
        private static bool FacesUp(Vector3[] positions, int i0, int i1, int i2)
        {
            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];
            return (p1.z - p0.z) * (p2.x - p0.x) - (p1.x - p0.x) * (p2.z - p0.z) > 0f;
        }

        private void PlaceVertex(TerrainChunkPlan plan, int i, int j, out Vector3 position, out SurfaceSample region)
        {
            int cells = plan.Cells;
            float cell = plan.CellSize;
            float x = plan.Origin.x + i * cell;
            float z = plan.Origin.y + j * cell;
            bool onVerticalBorder = i == 0 || i == cells;
            bool onHorizontalBorder = j == 0 || j == cells;

            if (onVerticalBorder && onHorizontalBorder)
            {
                Sample(x, z, out position, out region);
            }
            else if (onVerticalBorder)
            {
                float neighbour = i == 0 ? plan.WestCellSize : plan.EastCellSize;
                BorderVertex(x, z, cell, neighbour, true, out position, out region);
            }
            else if (onHorizontalBorder)
            {
                float neighbour = j == 0 ? plan.SouthCellSize : plan.NorthCellSize;
                BorderVertex(z, x, cell, neighbour, false, out position, out region);
            }
            else
            {
                int gi = Mathf.RoundToInt(x / cell);
                int gj = Mathf.RoundToInt(z / cell);
                uint key = _seed ^ CellKey(cell);
                float amplitude = _jitter * cell;
                float jx = Hashing.ToSigned(Hashing.Hash(gi, gj, key ^ JitterXSalt)) * amplitude;
                float jz = Hashing.ToSigned(Hashing.Hash(gi, gj, key ^ JitterZSalt)) * amplitude;
                Sample(x + jx, z + jz, out position, out region);
            }
        }

        /// <summary>
        /// A vertex on a chunk border line (<paramref name="line"/> = the border's X for a vertical border, Z for a
        /// horizontal one; <paramref name="along"/> = the coordinate along the border).
        /// </summary>
        private void BorderVertex(float line, float along, float ownCell, float neighbourCell, bool vertical,
            out Vector3 position, out SurfaceSample region)
        {
            float anchorCell = Mathf.Max(ownCell, neighbourCell);
            float fineCell = Mathf.Min(ownCell, neighbourCell);
            float steps = along / anchorCell;
            int k = Mathf.FloorToInt(steps + LatticeEpsilon);
            float t = steps - k;
            Anchor(line, k, anchorCell, fineCell, vertical, out position, out region);
            if (t <= LatticeEpsilon)
            {
                return;
            }

            Anchor(line, k + 1, anchorCell, fineCell, vertical, out Vector3 next, out SurfaceSample nextRegion);
            position = Vector3.Lerp(position, next, t);
            region = new SurfaceSample(position.y,
                Mathf.Lerp(region.CraterBowl, nextRegion.CraterBowl, t),
                Mathf.Lerp(region.CraterRim, nextRegion.CraterRim, t),
                Mathf.Lerp(region.RimZone, nextRegion.RimZone, t));
        }

        private void Anchor(float line, int k, float anchorCell, float fineCell, bool vertical, out Vector3 position,
            out SurfaceSample region)
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
                Sample(line, along + slide, out position, out region);
            }
            else
            {
                Sample(along + slide, line, out position, out region);
            }
        }

        private void Sample(float x, float z, out Vector3 position, out SurfaceSample region)
        {
            region = _surface.Sample(x, z);
            position = new Vector3(x, region.Height, z);
        }

        private static uint CellKey(float cellSize)
        {
            return Hashing.Mix((uint)Mathf.RoundToInt(cellSize * 64f));
        }
    }
}
