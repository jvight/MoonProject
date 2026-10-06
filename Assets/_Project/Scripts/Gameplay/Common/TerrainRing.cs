using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A flat band of light that hugs the ground: a ring of quads whose vertices are dropped onto the surface through
    /// <see cref="ITerrainQuery"/> (world-space vertices; the owning object stays at the origin). The mesh and its
    /// buffers are allocated once; <see cref="Rebuild"/> rewrites them in place without allocating.
    /// uv.x runs around the ring, uv.y across the band (0 inside, 1 outside).
    /// </summary>
    public sealed class TerrainRing
    {
        private const float MinBandWidth = 0.01f;
        private const float MinBoundsHeight = 0.1f;

        private readonly Vector3[] _vertices;
        private readonly int _segments;

        public TerrainRing(int segments)
        {
            if (segments < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), segments, "A ring needs at least 3 segments.");
            }

            _segments = segments;
            int rowLength = segments + 1;
            _vertices = new Vector3[rowLength * 2];
            var normals = new Vector3[_vertices.Length];
            var uvs = new Vector2[_vertices.Length];
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i <= segments; i++)
                {
                    int index = row * rowLength + i;
                    normals[index] = Vector3.up;
                    uvs[index] = new Vector2((float)i / segments, row);
                }
            }

            var triangles = new int[segments * 6];
            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int inner = i;
                int outer = i + rowLength;
                triangles[t++] = inner;
                triangles[t++] = outer;
                triangles[t++] = inner + 1;
                triangles[t++] = inner + 1;
                triangles[t++] = outer;
                triangles[t++] = outer + 1;
            }

            Mesh = new Mesh { name = "TerrainRing" };
            Mesh.MarkDynamic();
            Mesh.SetVertices(_vertices);
            Mesh.SetNormals(normals);
            Mesh.SetUVs(0, uvs);
            Mesh.SetTriangles(triangles, 0);
        }

        public Mesh Mesh { get; }

        /// <summary>World-space vertex positions after the last <see cref="Rebuild"/> (inner row, then outer row).</summary>
        public Vector3[] Vertices => _vertices;

        /// <summary>
        /// Lays the band between <paramref name="innerRadius"/> and <paramref name="outerRadius"/> around
        /// <paramref name="centre"/> (XZ), each vertex <paramref name="lift"/> metres above the surface.
        /// </summary>
        public void Rebuild(ITerrainQuery terrain, Vector3 centre, float innerRadius, float outerRadius, float lift)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            innerRadius = Mathf.Max(0f, innerRadius);
            outerRadius = Mathf.Max(innerRadius + MinBandWidth, outerRadius);
            int rowLength = _segments + 1;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i <= _segments; i++)
            {
                float angle = 2f * Mathf.PI * i / _segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                for (int row = 0; row < 2; row++)
                {
                    float radius = row == 0 ? innerRadius : outerRadius;
                    float x = centre.x + cos * radius;
                    float z = centre.z + sin * radius;
                    float y = terrain.SampleHeight(x, z) + lift;
                    _vertices[row * rowLength + i] = new Vector3(x, y, z);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            Mesh.SetVertices(_vertices);
            float extent = outerRadius * 2f;
            Mesh.bounds = new Bounds(new Vector3(centre.x, (minY + maxY) * 0.5f, centre.z),
                new Vector3(extent, Mathf.Max(MinBoundsHeight, maxY - minY), extent));
        }
    }
}
