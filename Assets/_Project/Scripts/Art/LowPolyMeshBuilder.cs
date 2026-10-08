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
    /// the output meshes. A builder made <see cref="WithVertexColours"/> also gives every vertex an sRGB colour
    /// (weather skins), starting at its swatch's <see cref="Palette.GetSurface"/> colour. Not thread-safe.
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
        private readonly List<Color32> _colours;
        private readonly List<int> _indices = new List<int>();
        private readonly IndexedShape _shape = new IndexedShape();
        private Vector3 _min;
        private Vector3 _max;

        public LowPolyMeshBuilder(int triangleCapacity = 256)
            : this(triangleCapacity, false)
        {
        }

        private LowPolyMeshBuilder(int triangleCapacity, bool vertexColours)
        {
            int vertexCapacity = Math.Max(3, triangleCapacity * 3);
            _positions = new List<Vector3>(vertexCapacity);
            _normals = new List<Vector3>(vertexCapacity);
            _uvs = new List<Vector2>(vertexCapacity);
            _colours = vertexColours ? new List<Color32>(vertexCapacity) : null;
        }

        /// <summary>
        /// A builder whose vertices also carry an sRGB colour, written as the mesh's vertex colours: the weather skins
        /// drawn by the LofiWeather shader, which shows that colour instead of the palette cell. Every face starts in
        /// its swatch's <see cref="Palette.GetSurface"/> colour (appended skins keep theirs) and can be shaded from
        /// there (<see cref="Shade"/>, <see cref="ShadeByHeight"/>).
        /// </summary>
        public static LowPolyMeshBuilder WithVertexColours(int triangleCapacity = 256)
        {
            return new LowPolyMeshBuilder(triangleCapacity, true);
        }

        public int TriangleCount => _positions.Count / 3;

        public int VertexCount => _positions.Count;

        /// <summary>Flattened vertex positions; triangle t uses vertices 3t, 3t + 1, 3t + 2.</summary>
        public IReadOnlyList<Vector3> Positions => _positions;

        /// <summary>Face normal per vertex (identical on the three vertices of a triangle).</summary>
        public IReadOnlyList<Vector3> Normals => _normals;

        /// <summary>Palette UV per vertex (identical on the three vertices of a triangle).</summary>
        public IReadOnlyList<Vector2> Uvs => _uvs;

        /// <summary>True when every vertex also carries a colour (see <see cref="WithVertexColours"/>).</summary>
        public bool HasVertexColours => _colours != null;

        /// <summary>The sRGB colour per vertex (empty unless <see cref="HasVertexColours"/>).</summary>
        public IReadOnlyList<Color32> Colours => _colours ?? (IReadOnlyList<Color32>)Array.Empty<Color32>();

        /// <summary>Axis-aligned bounds of everything added so far (zero-size at the origin when empty).</summary>
        public Bounds Bounds => VertexCount == 0 ? new Bounds(Vector3.zero, Vector3.zero) : FromMinMax(_min, _max);

        public void Clear()
        {
            _positions.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colours?.Clear();
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

        /// <summary>
        /// Copies every triangle of <paramref name="source"/> (with its swatches, and its vertex colours into a
        /// coloured builder) through a placement. A coloured source cannot go into a plain builder: its colours would
        /// be lost.
        /// </summary>
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

            if (source.HasVertexColours && !HasVertexColours)
            {
                throw new InvalidOperationException("A coloured skin cannot be appended to a plain builder.");
            }

            int first = TriangleCount;
            bool mirrored = Determinant3x3(placement) < 0f;
            for (int v = 0; v < source.VertexCount; v += 3)
            {
                Vector3 a = placement.MultiplyPoint3x4(source._positions[v]);
                Vector3 b = placement.MultiplyPoint3x4(source._positions[v + 1]);
                Vector3 c = placement.MultiplyPoint3x4(source._positions[v + 2]);
                Color32 ca = HasVertexColours ? source.ColourAt(v) : default;
                Color32 cb = HasVertexColours ? source.ColourAt(v + 1) : default;
                Color32 cc = HasVertexColours ? source.ColourAt(v + 2) : default;
                if (mirrored)
                {
                    (b, c) = (c, b);
                    (cb, cc) = (cc, cb);
                }

                AddFlatTriangle(a, b, c, source._uvs[v], ca, cb, cc);
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>
        /// Copies the triangles of <paramref name="source"/> that <paramref name="keepTriangle"/> picks (by triangle
        /// index), each pushed <paramref name="lift"/> out along its own normal and keeping its paint: a skin over
        /// part of another mesh, ready to be shaded. Glowing faces are skipped.
        /// </summary>
        public MeshRange AppendWhere(LowPolyMeshBuilder source, Predicate<int> keepTriangle, float lift)
        {
            RequireOverlaySource(source);
            if (keepTriangle == null)
            {
                throw new ArgumentNullException(nameof(keepTriangle));
            }

            int first = TriangleCount;
            for (int t = 0; t < source.TriangleCount; t++)
            {
                int v = t * 3;
                if (!IsGlowing(source._uvs[v]) && keepTriangle(t))
                {
                    Vector3 offset = source._normals[v] * lift;
                    AddFlatTriangle(source._positions[v] + offset, source._positions[v + 1] + offset,
                        source._positions[v + 2] + offset, source._uvs[v], source.ColourAt(v), source.ColourAt(v + 1),
                        source.ColourAt(v + 2));
                }
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>
        /// Copies the triangles of <paramref name="source"/> whose normal n satisfies dot(n, direction) &gt;=
        /// <paramref name="minDot"/>, each pushed <paramref name="lift"/> out along its own normal and painted
        /// <paramref name="swatch"/>: a skin over part of another mesh (dust settled on its top faces), built as its
        /// own mesh so it can be shown or removed on its own. Glowing faces are skipped.
        /// </summary>
        public MeshRange AppendFacing(LowPolyMeshBuilder source, Vector3 direction, float minDot, float lift,
            PaletteSwatch swatch)
        {
            RequireOverlaySource(source);
            Vector3 axis = direction.normalized;
            Vector2 uv = Palette.Uv(swatch);
            int first = TriangleCount;
            for (int v = 0; v < source.VertexCount; v += 3)
            {
                if (Vector3.Dot(source._normals[v], axis) >= minDot && !IsGlowing(source._uvs[v]))
                {
                    AddLifted(source._positions[v], source._positions[v + 1], source._positions[v + 2],
                        source._normals[v], lift, uv);
                }
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>
        /// Copies the triangles of <paramref name="source"/> painted <paramref name="from"/>, each pushed
        /// <paramref name="lift"/> out along its own normal and repainted <paramref name="to"/>: a coat over that paint
        /// (sun-bleached enamel over the clean enamel), its own mesh so the clean paint can come back.
        /// </summary>
        public MeshRange AppendRepainted(LowPolyMeshBuilder source, PaletteSwatch from, PaletteSwatch to, float lift)
        {
            RequireOverlaySource(source);
            Vector2 match = Palette.Uv(from);
            Vector2 uv = Palette.Uv(to);
            int first = TriangleCount;
            for (int v = 0; v < source.VertexCount; v += 3)
            {
                if (source._uvs[v] == match)
                {
                    AddLifted(source._positions[v], source._positions[v + 1], source._positions[v + 2],
                        source._normals[v], lift, uv);
                }
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>
        /// Copies the part of <paramref name="source"/> below <paramref name="height"/> (triangles crossing it are
        /// cut at that level), pushed <paramref name="lift"/> out along each face's normal and painted
        /// <paramref name="swatch"/>: a crisp tide line of dust round the foot of anything standing in it. Glowing
        /// faces are skipped.
        /// </summary>
        public MeshRange AppendBelow(LowPolyMeshBuilder source, float height, float lift, PaletteSwatch swatch)
        {
            RequireOverlaySource(source);
            return ClipBelow(source, height, lift, Palette.Uv(swatch));
        }

        /// <summary>
        /// As the painting overload, but the copy keeps the source's paint (and colours, which are interpolated where
        /// a triangle is cut): the lower part of a model as a skin, ready to be shaded towards the dust.
        /// </summary>
        public MeshRange AppendBelow(LowPolyMeshBuilder source, float height, float lift)
        {
            RequireOverlaySource(source);
            return ClipBelow(source, height, lift, null);
        }

        /// <summary>
        /// A flat convex polygon (a fan from its first point), each triangle wound to face along
        /// <paramref name="facing"/>: pieces of a panel cut out of another mesh's face, a stain laid on it.
        /// </summary>
        public MeshRange Polygon(IReadOnlyList<Vector3> points, Vector3 facing, PaletteSwatch swatch)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            Vector2 uv = Palette.Uv(swatch);
            int first = TriangleCount;
            for (int i = 2; i < points.Count; i++)
            {
                Vector3 a = points[0];
                Vector3 b = points[i - 1];
                Vector3 c = points[i];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f)
                {
                    (b, c) = (c, b);
                }

                AddFlatTriangle(a, b, c, uv);
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>The swatch triangle <paramref name="triangle"/> is painted with.</summary>
        public PaletteSwatch SwatchOf(int triangle)
        {
            if (triangle < 0 || triangle >= TriangleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(triangle));
            }

            return SwatchAt(_uvs[triangle * 3]);
        }

        /// <summary>
        /// Moves the colours of the range <paramref name="amount"/> (0..1) of the way towards
        /// <paramref name="toward"/> (sRGB): bleaching, darkening, staining.
        /// </summary>
        public void Shade(MeshRange range, Color32 toward, float amount)
        {
            RequireColouredRange(range);
            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                _colours[v] = Color32.Lerp(_colours[v], toward, amount);
            }
        }

        /// <summary>
        /// Shades the triangles of the range whose normal n satisfies dot(n, direction) &gt;= minDot towards
        /// <paramref name="toward"/> by <paramref name="amount"/>: dust caked on the faces turned to the sky.
        /// </summary>
        public void ShadeFacing(MeshRange range, Vector3 direction, float minDot, Color32 toward, float amount)
        {
            RequireColouredRange(range);
            Vector3 axis = direction.normalized;
            for (int t = range.FirstTriangle; t < range.EndTriangle; t++)
            {
                int v = t * 3;
                if (Vector3.Dot(_normals[v], axis) < minDot)
                {
                    continue;
                }

                for (int corner = 0; corner < 3; corner++)
                {
                    _colours[v + corner] = Color32.Lerp(_colours[v + corner], toward, amount);
                }
            }
        }

        /// <summary>
        /// Shades each vertex of the range towards <paramref name="toward"/> by <paramref name="amount"/> at or below
        /// <paramref name="bottom"/>, fading to nothing at <paramref name="top"/>: dust rising from the ground, rust
        /// creeping up a leg, a stain running out down a panel.
        /// </summary>
        public void ShadeByHeight(MeshRange range, Color32 toward, float bottom, float top, float amount)
        {
            RequireColouredRange(range);
            if (!(top > bottom))
            {
                throw new ArgumentException("The fade's top must lie above its bottom.", nameof(top));
            }

            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                float fade = Mathf.InverseLerp(top, bottom, _positions[v].y);
                _colours[v] = Color32.Lerp(_colours[v], toward, amount * fade);
            }
        }

        /// <summary>Recolours every triangle in <paramref name="range"/>.</summary>
        public void Repaint(MeshRange range, PaletteSwatch swatch)
        {
            RequireRange(range);
            Vector2 uv = Palette.Uv(swatch);
            Color32 colour = Palette.GetSurface(swatch);
            for (int v = range.FirstTriangle * 3; v < range.EndTriangle * 3; v++)
            {
                _uvs[v] = uv;
                if (_colours != null)
                {
                    _colours[v] = colour;
                }
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
            Color32 colour = Palette.GetSurface(swatch);
            for (int t = range.FirstTriangle; t < range.EndTriangle; t++)
            {
                int v = t * 3;
                if (Vector3.Dot(_normals[v], axis) >= minDot)
                {
                    for (int corner = 0; corner < 3; corner++)
                    {
                        _uvs[v + corner] = uv;
                        if (_colours != null)
                        {
                            _colours[v + corner] = colour;
                        }
                    }
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

                // Manual normalisation: Vector3.normalized returns zero below a 1e-5 length, which tiny faces reach.
                Vector3 cross = Vector3.Cross(b - a, c - a);
                float sqr = cross.sqrMagnitude;
                Vector3 normal = sqr > 0f ? cross / Mathf.Sqrt(sqr) : matrix.MultiplyVector(_normals[v]).normalized;
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
                for (int corner = 0; corner < 3; corner++)
                {
                    _normals[write + corner] = normal;
                    _uvs[write + corner] = uv;
                    if (_colours != null)
                    {
                        _colours[write + corner] = _colours[v + corner];
                    }
                }

                write += 3;
            }

            int removed = VertexCount - write;
            _positions.RemoveRange(write, removed);
            _normals.RemoveRange(write, removed);
            _uvs.RemoveRange(write, removed);
            _colours?.RemoveRange(write, removed);
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

        /// <summary>
        /// Centre of mass of the enclosed volume (uniform density), from signed tetrahedra against the origin.
        /// Exact for one closed shape; for overlapping closed parts the overlaps count twice, which is close
        /// enough for a pivot. Falls back to the bounds centre when the shape encloses no volume.
        /// </summary>
        public Vector3 VolumeCentroid()
        {
            double volume = 0d;
            double x = 0d;
            double y = 0d;
            double z = 0d;
            for (int v = 0; v < VertexCount; v += 3)
            {
                Vector3 a = _positions[v];
                Vector3 b = _positions[v + 1];
                Vector3 c = _positions[v + 2];
                double tetrahedron = Vector3.Dot(a, Vector3.Cross(b, c)) / 6d;
                volume += tetrahedron;
                x += tetrahedron * (a.x + b.x + c.x) / 4d;
                y += tetrahedron * (a.y + b.y + c.y) / 4d;
                z += tetrahedron * (a.z + b.z + c.z) / 4d;
            }

            if (Math.Abs(volume) < 1e-12d)
            {
                return Bounds.center;
            }

            return new Vector3((float)(x / volume), (float)(y / volume), (float)(z / volume));
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
            if (_colours != null)
            {
                mesh.SetColors(_colours);
            }

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

        private void AddLifted(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float lift, Vector2 uv)
        {
            Vector3 offset = normal * lift;
            AddFlatTriangle(a + offset, b + offset, c + offset, uv);
        }

        /// <summary>
        /// The part of every non-glowing source triangle below <paramref name="height"/>, lifted along its normal; in
        /// <paramref name="paint"/>'s swatch when given, otherwise in the source's own paint (colours interpolated
        /// along the cut edges).
        /// </summary>
        private MeshRange ClipBelow(LowPolyMeshBuilder source, float height, float lift, Vector2? paint)
        {
            int first = TriangleCount;
            var polygon = new List<Vector3>(4);
            var colours = new List<Color32>(4);
            for (int v = 0; v < source.VertexCount; v += 3)
            {
                if (IsGlowing(source._uvs[v]))
                {
                    continue;
                }

                polygon.Clear();
                colours.Clear();
                for (int e = 0; e < 3; e++)
                {
                    Vector3 from = source._positions[v + e];
                    Vector3 to = source._positions[v + (e + 1) % 3];
                    Color32 fromColour = source.ColourAt(v + e);
                    bool fromBelow = from.y <= height;
                    if (fromBelow)
                    {
                        polygon.Add(from);
                        colours.Add(fromColour);
                    }

                    if (fromBelow != (to.y <= height))
                    {
                        float cut = (height - from.y) / (to.y - from.y);
                        polygon.Add(Vector3.Lerp(from, to, cut));
                        colours.Add(Color32.Lerp(fromColour, source.ColourAt(v + (e + 1) % 3), cut));
                    }
                }

                Vector3 offset = source._normals[v] * lift;
                Vector2 uv = paint ?? source._uvs[v];
                for (int i = 2; i < polygon.Count; i++)
                {
                    if (paint.HasValue)
                    {
                        AddFlatTriangle(polygon[0] + offset, polygon[i - 1] + offset, polygon[i] + offset, uv);
                    }
                    else
                    {
                        AddFlatTriangle(polygon[0] + offset, polygon[i - 1] + offset, polygon[i] + offset, uv,
                            colours[0], colours[i - 1], colours[i]);
                    }
                }
            }

            return new MeshRange(first, TriangleCount - first);
        }

        /// <summary>Glowing glass is never dusted over: a lamp under dust would read as a dead one.</summary>
        private static bool IsGlowing(Vector2 uv)
        {
            return Palette.IsEmissive(SwatchAt(uv));
        }

        /// <summary>The swatch whose cell <paramref name="uv"/> points at (faces carry their cell's centre).</summary>
        private static PaletteSwatch SwatchAt(Vector2 uv)
        {
            int column = Mathf.FloorToInt(uv.x * Palette.Columns);
            int row = Mathf.FloorToInt(uv.y * Palette.Rows);
            return (PaletteSwatch)(row * Palette.Columns + column);
        }

        /// <summary>A vertex's colour: its own in a coloured builder, otherwise its swatch's surface colour.</summary>
        private Color32 ColourAt(int vertex)
        {
            return _colours != null ? _colours[vertex] : Palette.GetSurface(SwatchAt(_uvs[vertex]));
        }

        private void RequireColouredRange(MeshRange range)
        {
            RequireRange(range);
            if (_colours == null)
            {
                throw new InvalidOperationException("Only a builder made WithVertexColours can be shaded.");
            }
        }

        private void RequireOverlaySource(LowPolyMeshBuilder source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(source, this))
            {
                throw new ArgumentException("A builder cannot overlay itself.", nameof(source));
            }
        }

        private void AddFlatTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
        {
            Color32 colour = _colours != null ? Palette.GetSurface(SwatchAt(uv)) : default;
            AddFlatTriangle(a, b, c, uv, colour, colour, colour);
        }

        private void AddFlatTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uv, Color32 ca, Color32 cb, Color32 cc)
        {
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float sqr = cross.sqrMagnitude;
            if (sqr <= DegenerateCrossSqr)
            {
                return;
            }

            AddVertices(a, b, c, cross / Mathf.Sqrt(sqr), uv, ca, cb, cc);
        }

        private void AddVertices(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 uv)
        {
            Color32 colour = _colours != null ? Palette.GetSurface(SwatchAt(uv)) : default;
            AddVertices(a, b, c, normal, uv, colour, colour, colour);
        }

        private void AddVertices(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 uv, Color32 ca, Color32 cb,
            Color32 cc)
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
            if (_colours != null)
            {
                _colours.Add(ca);
                _colours.Add(cb);
                _colours.Add(cc);
            }
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
