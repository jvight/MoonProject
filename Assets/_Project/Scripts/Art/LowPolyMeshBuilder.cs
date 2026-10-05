using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Art
{
    /// <summary>
    /// Composes flat-shaded, palette-coloured low-poly meshes from primitives. Each primitive is generated with
    /// shared vertices in its own local frame, optionally displaced by seeded noise (still watertight), placed by a
    /// matrix, then flattened: three unique vertices per triangle, the face normal on all three, and the
    /// <see cref="Palette.Uv"/> of the face's swatch as UV0. Primitives are centred on their local origin; round ones
    /// use local +Y as their axis (see <see cref="Place.AlongX"/> / <see cref="Place.AlongZ"/>).
    /// Buffers are reused across <see cref="Clear"/>, so one builder can produce many meshes without garbage beyond
    /// the output meshes. Not thread-safe.
    /// </summary>
    public sealed class LowPolyMeshBuilder
    {
        public const int MaxIcosphereSubdivisions = 3;
        public const int MinSides = 3;

        private const float DegenerateCrossSqr = 1e-14f;
        private const int MaxUInt16Vertices = 65535;

        private readonly List<Vector3> _positions;
        private readonly List<Vector3> _normals;
        private readonly List<Vector2> _uvs;
        private readonly List<int> _indices = new List<int>();
        private readonly IndexedShape _shape = new IndexedShape();
        private Vector3 _min;
        private Vector3 _max;

        public LowPolyMeshBuilder(int triangleCapacity = 256)
        {
            int vertexCapacity = Math.Max(3, triangleCapacity * 3);
            _positions = new List<Vector3>(vertexCapacity);
            _normals = new List<Vector3>(vertexCapacity);
            _uvs = new List<Vector2>(vertexCapacity);
        }

        public int TriangleCount => _positions.Count / 3;

        public int VertexCount => _positions.Count;

        /// <summary>Flattened vertex positions; triangle t uses vertices 3t, 3t + 1, 3t + 2.</summary>
        public IReadOnlyList<Vector3> Positions => _positions;

        /// <summary>Face normal per vertex (identical on the three vertices of a triangle).</summary>
        public IReadOnlyList<Vector3> Normals => _normals;

        /// <summary>Palette UV per vertex (identical on the three vertices of a triangle).</summary>
        public IReadOnlyList<Vector2> Uvs => _uvs;

        /// <summary>Axis-aligned bounds of everything added so far (zero-size at the origin when empty).</summary>
        public Bounds Bounds => VertexCount == 0 ? new Bounds(Vector3.zero, Vector3.zero) : FromMinMax(_min, _max);

        public void Clear()
        {
            _positions.Clear();
            _normals.Clear();
            _uvs.Clear();
        }

        /// <summary>The range from <paramref name="firstTriangle"/> to the current end (groups primitives).</summary>
        public MeshRange RangeFrom(int firstTriangle)
        {
            if (firstTriangle < 0 || firstTriangle > TriangleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(firstTriangle));
            }

            return new MeshRange(firstTriangle, TriangleCount - firstTriangle);
        }

        /// <summary>Box of <paramref name="size"/> centred on the origin; the +Y/-Y faces are caps. A positive
        /// <paramref name="chamfer"/> bevels every edge.</summary>
        public MeshRange Box(Matrix4x4 placement, Vector3 size, Paint paint, float chamfer = 0f,
            Displacement displacement = default)
        {
            RequirePositive(size.x, nameof(size));
            RequirePositive(size.y, nameof(size));
            RequirePositive(size.z, nameof(size));
            float maxChamfer = 0.49f * Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (chamfer < 0f || chamfer > maxChamfer)
            {
                throw new ArgumentOutOfRangeException(nameof(chamfer), chamfer, $"Chamfer must be 0..{maxChamfer}.");
            }

            _shape.Clear();
            _shape.BuildBox(size, chamfer);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>Regular n-sided prism (a cylinder when n is large) along local Y, centred on the origin.</summary>
        public MeshRange Prism(Matrix4x4 placement, float radius, float height, int sides, Paint paint,
            bool caps = true, Displacement displacement = default)
        {
            return Frustum(placement, radius, radius, height, sides, paint, caps, displacement);
        }

        /// <summary>
        /// Truncated cone along local Y, centred on the origin (bottom at -height/2). A zero radius collapses that end
        /// to an apex. Caps are the flat ends.
        /// </summary>
        public MeshRange Frustum(Matrix4x4 placement, float bottomRadius, float topRadius, float height, int sides,
            Paint paint, bool caps = true, Displacement displacement = default)
        {
            RequireSides(sides);
            RequirePositive(height, nameof(height));
            if (bottomRadius < 0f || topRadius < 0f || (bottomRadius <= 0f && topRadius <= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(bottomRadius), "Radii must be >= 0 and not both zero.");
            }

            _shape.Clear();
            _shape.BuildFrustum(bottomRadius, topRadius, height, sides, caps);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>Cone along local Y, centred on the origin, apex at +height/2.</summary>
        public MeshRange Cone(Matrix4x4 placement, float radius, float height, int sides, Paint paint,
            bool cap = true, Displacement displacement = default)
        {
            RequirePositive(radius, nameof(radius));
            return Frustum(placement, radius, 0f, height, sides, paint, cap, displacement);
        }

        /// <summary>Geodesic sphere: 20 * 4^subdivisions faces (subdivisions 0..3).</summary>
        public MeshRange Icosphere(Matrix4x4 placement, float radius, int subdivisions, Paint paint,
            Displacement displacement = default)
        {
            RequirePositive(radius, nameof(radius));
            if (subdivisions < 0 || subdivisions > MaxIcosphereSubdivisions)
            {
                throw new ArgumentOutOfRangeException(nameof(subdivisions), subdivisions,
                    $"Subdivisions must be 0..{MaxIcosphereSubdivisions}.");
            }

            _shape.Clear();
            _shape.BuildIcosphere(radius, subdivisions);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>Torus around local Y (ring in the XZ plane).</summary>
        public MeshRange Torus(Matrix4x4 placement, float majorRadius, float minorRadius, int majorSegments,
            int minorSegments, Paint paint, Displacement displacement = default)
        {
            RequirePositive(minorRadius, nameof(minorRadius));
            if (majorRadius <= minorRadius)
            {
                throw new ArgumentOutOfRangeException(nameof(majorRadius), "Major radius must exceed minor radius.");
            }

            RequireSides(majorSegments);
            RequireSides(minorSegments);
            _shape.Clear();
            _shape.BuildTorus(majorRadius, minorRadius, majorSegments, minorSegments);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>
        /// Ramp inside a box of <paramref name="size"/> centred on the origin: full height at -Z, sloping down to the
        /// bottom edge at +Z. The two triangular ends (±X) are caps.
        /// </summary>
        public MeshRange Wedge(Matrix4x4 placement, Vector3 size, Paint paint, Displacement displacement = default)
        {
            RequirePositive(size.x, nameof(size));
            RequirePositive(size.y, nameof(size));
            RequirePositive(size.z, nameof(size));
            _shape.Clear();
            _shape.BuildWedge(size);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>
        /// Revolves a (radius, height) profile around local Y. The surface faces to the right of the travel direction
        /// (a profile walked upwards at positive radius faces outward). Points on the axis (radius 0) become poles;
        /// start and end on the axis for a closed solid. No caps are added.
        /// </summary>
        public MeshRange Lathe(Matrix4x4 placement, ReadOnlySpan<Vector2> profile, int segments, Paint paint,
            Displacement displacement = default)
        {
            return LatheInternal(placement, profile, segments, paint, default, displacement);
        }

        /// <summary>As <see cref="Lathe(Matrix4x4, ReadOnlySpan{Vector2}, int, Paint, Displacement)"/> with one swatch
        /// per band: band i spans profile points i and i + 1.</summary>
        public MeshRange Lathe(Matrix4x4 placement, ReadOnlySpan<Vector2> profile, int segments,
            ReadOnlySpan<PaletteSwatch> bands, Displacement displacement = default)
        {
            if (bands.Length != profile.Length - 1)
            {
                throw new ArgumentException("Lathe needs exactly one swatch per band (profile length - 1).",
                    nameof(bands));
            }

            return LatheInternal(placement, profile, segments, default, bands, displacement);
        }

        /// <summary>
        /// Extrudes a simple (possibly concave) polygon in the XY plane along Z, centred on the origin. Either winding
        /// is accepted. The front (+Z) and back (-Z) faces are caps.
        /// </summary>
        public MeshRange Extrude(Matrix4x4 placement, ReadOnlySpan<Vector2> polygon, float depth, Paint paint,
            Displacement displacement = default)
        {
            if (polygon.Length < 3)
            {
                throw new ArgumentException("Extrude needs at least three polygon points.", nameof(polygon));
            }

            RequirePositive(depth, nameof(depth));
            _shape.Clear();
            _shape.BuildExtrusion(polygon, depth);
            return Emit(placement, paint, default, displacement, true);
        }

        /// <summary>Copies every triangle of <paramref name="source"/> (with colours) through a placement.</summary>
        public MeshRange Append(LowPolyMeshBuilder source, Matrix4x4 placement)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(source, this))
            {
                throw new ArgumentException("Cannot append a builder to itself.", nameof(source));
            }

            int first = TriangleCount;
            bool mirrored = Determinant3x3(placement) < 0f;
            for (int v = 0; v < source.VertexCount; v += 3)
            {
                Vector3 a = placement.MultiplyPoint3x4(source._positions[v]);
                Vector3 b = placement.MultiplyPoint3x4(source._positions[v + 1]);
                Vector3 c = placement.MultiplyPoint3x4(source._positions[v + 2]);
                if (mirrored)
                {
                    (b, c) = (c, b);
                }

                AddFlatTriangle(a, b, c, source._uvs[v]);
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>Recolours every triangle in <paramref name="range"/>.</summary>
        public void Repaint(MeshRange range, PaletteSwatch swatch)
        {
            RequireRange(range);
            Vector2 uv = Palette.Uv(swatch);
            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                _uvs[v] = uv;
            }
        }

        /// <summary>
        /// Recolours the triangles in <paramref name="range"/> whose normal n satisfies dot(n, direction) &gt;= minDot.
        /// </summary>
        public void RepaintFacing(MeshRange range, Vector3 direction, float minDot, PaletteSwatch swatch)
        {
            RequireRange(range);
            Vector3 axis = direction.normalized;
            Vector2 uv = Palette.Uv(swatch);
            for (int t = range.FirstTriangle; t < range.EndTriangle; t++)
            {
                int v = t * 3;
                if (Vector3.Dot(_normals[v], axis) >= minDot)
                {
                    _uvs[v] = uv;
                    _uvs[v + 1] = uv;
                    _uvs[v + 2] = uv;
                }
            }
        }

        /// <summary>Moves the triangles of <paramref name="range"/> by a matrix (normals and winding follow).</summary>
        public void Transform(MeshRange range, Matrix4x4 matrix)
        {
            RequireRange(range);
            bool mirrored = Determinant3x3(matrix) < 0f;
            for (int t = range.FirstTriangle; t < range.EndTriangle; t++)
            {
                int v = t * 3;
                Vector3 a = matrix.MultiplyPoint3x4(_positions[v]);
                Vector3 b = matrix.MultiplyPoint3x4(_positions[v + 1]);
                Vector3 c = matrix.MultiplyPoint3x4(_positions[v + 2]);
                if (mirrored)
                {
                    (b, c) = (c, b);
                }

                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                _positions[v] = a;
                _positions[v + 1] = b;
                _positions[v + 2] = c;
                _normals[v] = normal;
                _normals[v + 1] = normal;
                _normals[v + 2] = normal;
            }

            RecalculateBounds();
        }

        /// <summary>
        /// Slices the shape flat: every vertex of <paramref name="range"/> beyond the plane
        /// dot(<paramref name="planeNormal"/>, p) = <paramref name="planeDistance"/> is projected back onto it (chipped
        /// rock facets, worn edges). Shared positions move identically, so closed shapes stay closed; triangles that
        /// collapse are dropped, so the range must be the last one in the builder. Returns the updated range.
        /// Colours are kept; repaint afterwards if they depend on the new normals.
        /// </summary>
        public MeshRange Shave(MeshRange range, Vector3 planeNormal, float planeDistance)
        {
            RequireRange(range);
            if (range.EndTriangle != TriangleCount)
            {
                throw new ArgumentException("Only the last range of a builder can be shaved.", nameof(range));
            }

            if (planeNormal.sqrMagnitude < 1e-12f)
            {
                throw new ArgumentException("Plane normal must be non-zero.", nameof(planeNormal));
            }

            Vector3 axis = planeNormal.normalized;
            int write = range.FirstTriangle * 3;
            for (int v = write; v < VertexCount; v += 3)
            {
                Vector3 a = ShaveVertex(_positions[v], axis, planeDistance);
                Vector3 b = ShaveVertex(_positions[v + 1], axis, planeDistance);
                Vector3 c = ShaveVertex(_positions[v + 2], axis, planeDistance);
                Vector3 cross = Vector3.Cross(b - a, c - a);
                float sqr = cross.sqrMagnitude;
                if (sqr <= DegenerateCrossSqr)
                {
                    continue;
                }

                Vector3 normal = cross / Mathf.Sqrt(sqr);
                Vector2 uv = _uvs[v];
                _positions[write] = a;
                _positions[write + 1] = b;
                _positions[write + 2] = c;
                _normals[write] = normal;
                _normals[write + 1] = normal;
                _normals[write + 2] = normal;
                _uvs[write] = uv;
                _uvs[write + 1] = uv;
                _uvs[write + 2] = uv;
                write += 3;
            }

            int removed = VertexCount - write;
            _positions.RemoveRange(write, removed);
            _normals.RemoveRange(write, removed);
            _uvs.RemoveRange(write, removed);
            RecalculateBounds();
            return new MeshRange(range.FirstTriangle, TriangleCount - range.FirstTriangle);
        }

        /// <summary>
        /// Largest dot(p, <paramref name="direction"/>) over the range's vertices: how far the shape reaches along a
        /// direction (pair with <see cref="Shave"/> to cut a fixed depth into a shape).
        /// </summary>
        public float Support(MeshRange range, Vector3 direction)
        {
            RequireRange(range);
            if (range.TriangleCount == 0)
            {
                throw new ArgumentException("Range is empty.", nameof(range));
            }

            float support = float.MinValue;
            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                support = Mathf.Max(support, Vector3.Dot(_positions[v], direction));
            }

            return support;
        }

        /// <summary>Axis-aligned bounds of the triangles in <paramref name="range"/>.</summary>
        public Bounds GetBounds(MeshRange range)
        {
            RequireRange(range);
            if (range.TriangleCount == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Vector3 min = _positions[range.FirstTriangle * 3];
            Vector3 max = min;
            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                min = Vector3.Min(min, _positions[v]);
                max = Vector3.Max(max, _positions[v]);
            }

            return FromMinMax(min, max);
        }

        /// <summary>Creates a new mesh holding everything added so far.</summary>
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            WriteTo(mesh);
            return mesh;
        }

        /// <summary>
        /// Replaces the contents of <paramref name="mesh"/> (keeps its name and identity, so assets and runtime pools
        /// can be rebuilt in place). Uses 32-bit indices only above 65535 vertices.
        /// </summary>
        public void WriteTo(Mesh mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            for (int i = _indices.Count; i < VertexCount; i++)
            {
                _indices.Add(i);
            }

            mesh.Clear();
            mesh.indexFormat = VertexCount > MaxUInt16Vertices ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetIndices(_indices, 0, VertexCount, MeshTopology.Triangles, 0, false);
            mesh.bounds = Bounds;
        }

        private MeshRange LatheInternal(Matrix4x4 placement, ReadOnlySpan<Vector2> profile, int segments, Paint paint,
            ReadOnlySpan<PaletteSwatch> bands, Displacement displacement)
        {
            RequireSides(segments);
            if (profile.Length < 2)
            {
                throw new ArgumentException("Lathe needs at least two profile points.", nameof(profile));
            }

            for (int i = 0; i < profile.Length; i++)
            {
                if (profile[i].x < 0f)
                {
                    throw new ArgumentException("Lathe profile radii must be >= 0.", nameof(profile));
                }

                if (i > 0 && profile[i].x <= 0f && profile[i - 1].x <= 0f)
                {
                    throw new ArgumentException("Two consecutive lathe points lie on the axis.", nameof(profile));
                }
            }

            _shape.Clear();
            _shape.BuildLathe(profile, segments);
            return Emit(placement, paint, bands, displacement, false);
        }

        /// <param name="tagsMarkCaps">False for lathes, whose triangle tags are band indices, not side/cap.</param>
        private MeshRange Emit(Matrix4x4 placement, Paint paint, ReadOnlySpan<PaletteSwatch> bands,
            Displacement displacement, bool tagsMarkCaps)
        {
            if (!displacement.IsNone)
            {
                _shape.Displace(displacement);
            }

            List<Vector3> points = _shape.Positions;
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = placement.MultiplyPoint3x4(points[i]);
            }

            bool mirrored = Determinant3x3(placement) < 0f;
            List<int> triangles = _shape.Triangles;
            List<int> tags = _shape.Tags;
            int first = TriangleCount;
            for (int t = 0; t < tags.Count; t++)
            {
                Vector3 a = points[triangles[3 * t]];
                Vector3 b = points[triangles[3 * t + 1]];
                Vector3 c = points[triangles[3 * t + 2]];
                if (mirrored)
                {
                    (b, c) = (c, b);
                }

                Vector3 cross = Vector3.Cross(b - a, c - a);
                float sqr = cross.sqrMagnitude;
                if (sqr <= DegenerateCrossSqr)
                {
                    continue;
                }

                Vector3 normal = cross / Mathf.Sqrt(sqr);
                int tag = tags[t];
                PaletteSwatch swatch = bands.Length > 0
                    ? bands[tag]
                    : paint.Resolve(tagsMarkCaps && tag == IndexedShape.CapTag, normal);
                AddVertices(a, b, c, normal, Palette.Uv(swatch));
            }

            return new MeshRange(first, TriangleCount - first);
        }

        private void AddFlatTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
        {
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float sqr = cross.sqrMagnitude;
            if (sqr <= DegenerateCrossSqr)
            {
                return;
            }

            AddVertices(a, b, c, cross / Mathf.Sqrt(sqr), uv);
        }

        private void AddVertices(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 uv)
        {
            if (VertexCount == 0)
            {
                _min = a;
                _max = a;
            }

            _min = Vector3.Min(_min, Vector3.Min(a, Vector3.Min(b, c)));
            _max = Vector3.Max(_max, Vector3.Max(a, Vector3.Max(b, c)));
            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);
            _uvs.Add(uv);
            _uvs.Add(uv);
            _uvs.Add(uv);
        }

        private static Vector3 ShaveVertex(Vector3 position, Vector3 axis, float distance)
        {
            float beyond = Vector3.Dot(position, axis) - distance;
            return beyond > 0f ? position - axis * beyond : position;
        }

        private void RecalculateBounds()
        {
            if (VertexCount == 0)
            {
                return;
            }

            _min = _positions[0];
            _max = _positions[0];
            for (int v = 1; v < VertexCount; v++)
            {
                _min = Vector3.Min(_min, _positions[v]);
                _max = Vector3.Max(_max, _positions[v]);
            }
        }

        private void RequireRange(MeshRange range)
        {
            if (range.FirstTriangle < 0 || range.TriangleCount < 0 || range.EndTriangle > TriangleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "Range lies outside this builder.");
            }
        }

        private static Bounds FromMinMax(Vector3 min, Vector3 max)
        {
            return new Bounds((min + max) * 0.5f, max - min);
        }

        private static float Determinant3x3(Matrix4x4 m)
        {
            return m.m00 * (m.m11 * m.m22 - m.m12 * m.m21)
                - m.m01 * (m.m10 * m.m22 - m.m12 * m.m20)
                + m.m02 * (m.m10 * m.m21 - m.m11 * m.m20);
        }

        private static void RequirePositive(float value, string name)
        {
            if (!(value > 0f))
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be positive.");
            }
        }

        private static void RequireSides(int sides)
        {
            if (sides < MinSides)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), sides, $"Need at least {MinSides} sides.");
            }
        }
    }
}
