using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Reusable scratch geometry for one primitive: shared vertices, index triples and a tag per triangle
    /// (<see cref="SideTag"/>, <see cref="CapTag"/> or a lathe band index). Generators emit triangles whose
    /// cross(b - a, c - a) points outward (Unity's clockwise front faces); shared vertices keep displacement
    /// watertight.
    /// </summary>
    internal sealed class IndexedShape
    {
        public const int SideTag = 0;
        public const int CapTag = 1;

        private const float GoldenRatio = 1.6180339887f;

        private readonly List<Vector3> _positions = new List<Vector3>(256);
        private readonly List<int> _triangles = new List<int>(768);
        private readonly List<int> _tags = new List<int>(256);
        private readonly List<Vector3> _smoothNormals = new List<Vector3>(256);
        private readonly List<int> _scratchTriangles = new List<int>(768);
        private readonly List<Vector2> _polygon = new List<Vector2>(32);
        private readonly List<int> _polygonTriangles = new List<int>(96);
        private readonly List<int> _polygonWork = new List<int>(32);
        private readonly Dictionary<long, int> _midpoints = new Dictionary<long, int>(256);

        public List<Vector3> Positions => _positions;

        public List<int> Triangles => _triangles;

        public List<int> Tags => _tags;

        public int TriangleCount => _tags.Count;

        public void Clear()
        {
            _positions.Clear();
            _triangles.Clear();
            _tags.Clear();
        }

        public void BuildBox(Vector3 size, float chamfer)
        {
            Vector3 half = size * 0.5f;
            if (chamfer <= 0f)
            {
                BuildSharpBox(half);
            }
            else
            {
                BuildChamferedBox(half, chamfer);
            }

            OrientOutward(Vector3.zero);
        }

        public void BuildFrustum(float bottomRadius, float topRadius, float height, int sides, bool caps)
        {
            float half = height * 0.5f;
            int bottom = AddRing(bottomRadius, -half, sides);
            int top = AddRing(topRadius, half, sides);
            bool bottomIsRing = bottomRadius > 0f;
            bool topIsRing = topRadius > 0f;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                int b0 = bottomIsRing ? bottom + i : bottom;
                int b1 = bottomIsRing ? bottom + j : bottom;
                int t0 = topIsRing ? top + i : top;
                int t1 = topIsRing ? top + j : top;
                AddQuad(b0, b1, t1, t0, SideTag);
            }

            if (caps && bottomIsRing)
            {
                int centre = AddVertex(new Vector3(0f, -half, 0f));
                for (int i = 0; i < sides; i++)
                {
                    AddTriangle(centre, bottom + (i + 1) % sides, bottom + i, CapTag);
                }
            }

            if (caps && topIsRing)
            {
                int centre = AddVertex(new Vector3(0f, half, 0f));
                for (int i = 0; i < sides; i++)
                {
                    AddTriangle(centre, top + i, top + (i + 1) % sides, CapTag);
                }
            }

            OrientOutward(Vector3.zero);
        }

        public void BuildIcosphere(float radius, int subdivisions)
        {
            float t = GoldenRatio;
            AddVertex(new Vector3(-1f, t, 0f));
            AddVertex(new Vector3(1f, t, 0f));
            AddVertex(new Vector3(-1f, -t, 0f));
            AddVertex(new Vector3(1f, -t, 0f));
            AddVertex(new Vector3(0f, -1f, t));
            AddVertex(new Vector3(0f, 1f, t));
            AddVertex(new Vector3(0f, -1f, -t));
            AddVertex(new Vector3(0f, 1f, -t));
            AddVertex(new Vector3(t, 0f, -1f));
            AddVertex(new Vector3(t, 0f, 1f));
            AddVertex(new Vector3(-t, 0f, -1f));
            AddVertex(new Vector3(-t, 0f, 1f));

            AddTriangleIndices(0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11);
            AddTriangleIndices(1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8);
            AddTriangleIndices(3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9);
            AddTriangleIndices(4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1);

            for (int level = 0; level < subdivisions; level++)
            {
                Subdivide();
            }

            for (int i = 0; i < _positions.Count; i++)
            {
                _positions[i] = _positions[i].normalized * radius;
            }

            OrientOutward(Vector3.zero);
        }

        public void BuildTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            for (int j = 0; j < majorSegments; j++)
            {
                float phi = (j + 0.5f) / majorSegments * Mathf.PI * 2f;
                for (int k = 0; k < minorSegments; k++)
                {
                    float psi = (k + 0.5f) / minorSegments * Mathf.PI * 2f;
                    float ring = majorRadius + minorRadius * Mathf.Cos(psi);
                    AddVertex(new Vector3(ring * Mathf.Sin(phi), minorRadius * Mathf.Sin(psi), ring * Mathf.Cos(phi)));
                }
            }

            for (int j = 0; j < majorSegments; j++)
            {
                int jNext = (j + 1) % majorSegments;
                for (int k = 0; k < minorSegments; k++)
                {
                    int kNext = (k + 1) % minorSegments;
                    AddQuad(
                        j * minorSegments + k,
                        jNext * minorSegments + k,
                        jNext * minorSegments + kNext,
                        j * minorSegments + kNext,
                        SideTag);
                }
            }
        }

        public void BuildWedge(Vector3 size)
        {
            Vector3 h = size * 0.5f;
            AddVertex(new Vector3(-h.x, -h.y, -h.z));
            AddVertex(new Vector3(h.x, -h.y, -h.z));
            AddVertex(new Vector3(h.x, -h.y, h.z));
            AddVertex(new Vector3(-h.x, -h.y, h.z));
            AddVertex(new Vector3(-h.x, h.y, -h.z));
            AddVertex(new Vector3(h.x, h.y, -h.z));

            AddQuad(0, 1, 2, 3, SideTag);
            AddQuad(0, 1, 5, 4, SideTag);
            AddQuad(4, 5, 2, 3, SideTag);
            AddTriangle(1, 2, 5, CapTag);
            AddTriangle(0, 3, 4, CapTag);
            OrientOutward(new Vector3(0f, -h.y / 3f, -h.z / 3f));
        }

        /// <summary>Profile points are (radius, height); the surface faces right of the travel direction.</summary>
        public void BuildLathe(ReadOnlySpan<Vector2> profile, int segments)
        {
            int previous = AddRing(profile[0].x, profile[0].y, segments);
            for (int band = 0; band < profile.Length - 1; band++)
            {
                Vector2 from = profile[band];
                Vector2 to = profile[band + 1];
                int next = AddRing(to.x, to.y, segments);
                bool fromRing = from.x > 0f;
                bool toRing = to.x > 0f;
                for (int s = 0; s < segments; s++)
                {
                    int sNext = (s + 1) % segments;
                    AddQuad(
                        fromRing ? previous + s : previous,
                        fromRing ? previous + sNext : previous,
                        toRing ? next + sNext : next,
                        toRing ? next + s : next,
                        band);
                }

                previous = next;
            }
        }

        public void BuildExtrusion(ReadOnlySpan<Vector2> polygon, float depth)
        {
            _polygon.Clear();
            for (int i = 0; i < polygon.Length; i++)
            {
                _polygon.Add(polygon[i]);
            }

            float doubleArea = PolygonTriangulator.SignedDoubleArea(_polygon);
            if (Mathf.Abs(doubleArea) < 1e-10f || !PolygonTriangulator.IsSimple(_polygon))
            {
                throw new ArgumentException("Extrusion polygon must be simple (no crossing edges) with non-zero area.");
            }

            if (doubleArea < 0f)
            {
                _polygon.Reverse();
            }

            int count = _polygon.Count;
            float half = depth * 0.5f;
            for (int i = 0; i < count; i++)
            {
                AddVertex(new Vector3(_polygon[i].x, _polygon[i].y, -half));
            }

            for (int i = 0; i < count; i++)
            {
                AddVertex(new Vector3(_polygon[i].x, _polygon[i].y, half));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                AddQuad(i, j, count + j, count + i, SideTag);
            }

            _polygonTriangles.Clear();
            PolygonTriangulator.Triangulate(_polygon, _polygonTriangles, _polygonWork);
            for (int t = 0; t < _polygonTriangles.Count; t += 3)
            {
                int a = _polygonTriangles[t];
                int b = _polygonTriangles[t + 1];
                int c = _polygonTriangles[t + 2];
                AddTriangle(count + a, count + b, count + c, CapTag);
                AddTriangle(a, c, b, CapTag);
            }
        }

        /// <summary>Moves every shared vertex along its area-weighted smooth normal by seeded fractal noise.</summary>
        public void Displace(Displacement displacement)
        {
            _smoothNormals.Clear();
            for (int i = 0; i < _positions.Count; i++)
            {
                _smoothNormals.Add(Vector3.zero);
            }

            for (int t = 0; t < _triangles.Count; t += 3)
            {
                int a = _triangles[t];
                int b = _triangles[t + 1];
                int c = _triangles[t + 2];
                Vector3 weighted = Vector3.Cross(_positions[b] - _positions[a], _positions[c] - _positions[a]);
                _smoothNormals[a] += weighted;
                _smoothNormals[b] += weighted;
                _smoothNormals[c] += weighted;
            }

            for (int i = 0; i < _positions.Count; i++)
            {
                Vector3 position = _positions[i];
                Vector3 direction = _smoothNormals[i];
                if (direction.sqrMagnitude < 1e-20f)
                {
                    direction = position;
                }

                if (direction.sqrMagnitude < 1e-20f)
                {
                    continue;
                }

                float noise = SeededNoise.Fractal(
                    position * displacement.Frequency, displacement.Seed, displacement.Octaves);
                _positions[i] = position + direction.normalized * (noise * displacement.Amplitude);
            }
        }

        private void BuildSharpBox(Vector3 half)
        {
            for (int i = 0; i < 8; i++)
            {
                AddVertex(new Vector3(
                    (i & 1) != 0 ? half.x : -half.x,
                    (i & 2) != 0 ? half.y : -half.y,
                    (i & 4) != 0 ? half.z : -half.z));
            }

            AddQuad(1, 3, 7, 5, SideTag);
            AddQuad(0, 4, 6, 2, SideTag);
            AddQuad(2, 3, 7, 6, CapTag);
            AddQuad(0, 1, 5, 4, CapTag);
            AddQuad(4, 5, 7, 6, SideTag);
            AddQuad(0, 1, 3, 2, SideTag);
        }

        private void BuildChamferedBox(Vector3 half, float chamfer)
        {
            // Corner k (bit 0 = +x, bit 1 = +y, bit 2 = +z) owns three vertices, one on each adjacent face:
            // 3k on the x face, 3k + 1 on the y face, 3k + 2 on the z face.
            Vector3 inner = half - new Vector3(chamfer, chamfer, chamfer);
            for (int k = 0; k < 8; k++)
            {
                float sx = (k & 1) != 0 ? 1f : -1f;
                float sy = (k & 2) != 0 ? 1f : -1f;
                float sz = (k & 4) != 0 ? 1f : -1f;
                AddVertex(new Vector3(sx * half.x, sy * inner.y, sz * inner.z));
                AddVertex(new Vector3(sx * inner.x, sy * half.y, sz * inner.z));
                AddVertex(new Vector3(sx * inner.x, sy * inner.y, sz * half.z));
            }

            AddQuad(3 * 1, 3 * 3, 3 * 7, 3 * 5, SideTag);
            AddQuad(3 * 0, 3 * 4, 3 * 6, 3 * 2, SideTag);
            AddQuad(3 * 2 + 1, 3 * 3 + 1, 3 * 7 + 1, 3 * 6 + 1, CapTag);
            AddQuad(3 * 0 + 1, 3 * 1 + 1, 3 * 5 + 1, 3 * 4 + 1, CapTag);
            AddQuad(3 * 4 + 2, 3 * 5 + 2, 3 * 7 + 2, 3 * 6 + 2, SideTag);
            AddQuad(3 * 0 + 2, 3 * 1 + 2, 3 * 3 + 2, 3 * 2 + 2, SideTag);

            for (int k = 0; k < 8; k += 2)
            {
                int k1 = k | 1;
                AddQuad(3 * k + 1, 3 * k1 + 1, 3 * k1 + 2, 3 * k + 2, SideTag);
            }

            for (int k = 0; k < 8; k++)
            {
                if ((k & 2) != 0)
                {
                    continue;
                }

                int k1 = k | 2;
                AddQuad(3 * k, 3 * k1, 3 * k1 + 2, 3 * k + 2, SideTag);
            }

            for (int k = 0; k < 4; k++)
            {
                int k1 = k | 4;
                AddQuad(3 * k, 3 * k1, 3 * k1 + 1, 3 * k + 1, SideTag);
            }

            for (int k = 0; k < 8; k++)
            {
                AddTriangle(3 * k, 3 * k + 1, 3 * k + 2, SideTag);
            }
        }

        /// <summary>A ring of <paramref name="sides"/> vertices, or one axis vertex when the radius is zero.</summary>
        private int AddRing(float radius, float y, int sides)
        {
            int first = _positions.Count;
            if (radius <= 0f)
            {
                AddVertex(new Vector3(0f, y, 0f));
                return first;
            }

            for (int i = 0; i < sides; i++)
            {
                // Half-step offset so a flat side (not a corner) faces +Z.
                float angle = (i + 0.5f) / sides * Mathf.PI * 2f;
                AddVertex(new Vector3(radius * Mathf.Sin(angle), y, radius * Mathf.Cos(angle)));
            }

            return first;
        }

        private void Subdivide()
        {
            _midpoints.Clear();
            _scratchTriangles.Clear();
            _scratchTriangles.AddRange(_triangles);
            _triangles.Clear();
            _tags.Clear();
            for (int t = 0; t < _scratchTriangles.Count; t += 3)
            {
                int a = _scratchTriangles[t];
                int b = _scratchTriangles[t + 1];
                int c = _scratchTriangles[t + 2];
                int ab = Midpoint(a, b);
                int bc = Midpoint(b, c);
                int ca = Midpoint(c, a);
                AddTriangle(a, ab, ca, SideTag);
                AddTriangle(b, bc, ab, SideTag);
                AddTriangle(c, ca, bc, SideTag);
                AddTriangle(ab, bc, ca, SideTag);
            }
        }

        private int Midpoint(int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (_midpoints.TryGetValue(key, out int index))
            {
                return index;
            }

            Vector3 mid = (_positions[a] + _positions[b]) * 0.5f;
            index = AddVertex(mid.normalized * _positions[a].magnitude);
            _midpoints.Add(key, index);
            return index;
        }

        private void OrientOutward(Vector3 interior)
        {
            for (int t = 0; t < _triangles.Count; t += 3)
            {
                Vector3 a = _positions[_triangles[t]];
                Vector3 b = _positions[_triangles[t + 1]];
                Vector3 c = _positions[_triangles[t + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Vector3 centre = (a + b + c) * (1f / 3f);
                if (Vector3.Dot(normal, centre - interior) < 0f)
                {
                    int swap = _triangles[t + 1];
                    _triangles[t + 1] = _triangles[t + 2];
                    _triangles[t + 2] = swap;
                }
            }
        }

        private int AddVertex(Vector3 position)
        {
            _positions.Add(position);
            return _positions.Count - 1;
        }

        private void AddTriangle(int a, int b, int c, int tag)
        {
            if (a == b || b == c || c == a)
            {
                return;
            }

            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
            _tags.Add(tag);
        }

        private void AddQuad(int a, int b, int c, int d, int tag)
        {
            AddTriangle(a, b, c, tag);
            AddTriangle(a, c, d, tag);
        }

        private void AddTriangleIndices(int a0, int b0, int c0, int a1, int b1, int c1, int a2, int b2, int c2,
            int a3, int b3, int c3, int a4, int b4, int c4)
        {
            AddTriangle(a0, b0, c0, SideTag);
            AddTriangle(a1, b1, c1, SideTag);
            AddTriangle(a2, b2, c2, SideTag);
            AddTriangle(a3, b3, c3, SideTag);
            AddTriangle(a4, b4, c4, SideTag);
        }
    }
}
