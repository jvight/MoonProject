using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The generated part of the weather skins (VISION ruling 12, "left alone for a very long time"): a model's faces
    /// grouped into planes; every big flat face broken into tired panels of patchwork paint; rust running down from
    /// each panel's top edge and darkening bare metal towards the ground; and the dust rising from the ground and
    /// lying on every face turned to the sky. Big flat facets with smooth vertex-colour fades, never texture grime.
    /// </summary>
    internal static class WeatherSkins
    {
        // Faces whose normals agree within this step and whose planes lie within this distance share a plane.
        private const float NormalStep = 0.05f;
        private const float OffsetStep = 0.02f;

        // A plane at least this big, and this wide both ways, is broken into panels. Smaller facets (curved parts,
        // small fittings) take the tone of the panel-sized patch of space they sit in, so a dome weathers in patches
        // rather than facet by facet; long thin members (posts, rails, struts) take one tone per column of space, so
        // a post is one tired colour from foot to top rather than striped.
        private const float PanelPlaneArea = 0.2f;
        private const float MinPanelSide = 0.3f;
        private const float ThinMember = 3f;
        private const float MemberColumn = 1.2f;

        // Rust gathers along a share of the panels' bottom edges, rising this far up the panel at most.
        private const float EdgeRustShare = 0.45f;
        private const float MinEdgeRust = 0.08f;
        private const float MaxEdgeRust = 0.32f;
        private const float EdgeRustDepth = 0.9f;

        // Rust runs hang only on walls (|n.y| under this) at least this wide and tall.
        private const float WallSlope = 0.35f;
        private const float MinRunWall = 0.2f;
        private const float RunMargin = 0.05f;
        private const float RunTopGap = 0.012f;
        private const float MinRunWidth = 0.045f;
        private const float MaxRunWidth = 0.11f;
        private const float RunTaper = 0.25f;
        private const float MinRunLength = 0.35f;
        private const float MaxRunLength = 1f;

        // How far down its length a run fades into the paint it runs over.
        private const float RunFade = 0.85f;

        // Under the panels' own tones, decades of grime darken every face a little (scaled by the paint wear).
        private const float AgeVeil = 0.14f;

        // Faces turned at most ~37 degrees from the sky hold dust.
        private const float SkyFacing = 0.8f;

        // Faces smaller than this (rivets, slats, bolt heads) keep their clean paint: too small to weather visibly.
        private const float MinSkinFace = 0.0012f;

        // Faces starting above this share of the rust's or the dust's reach would barely shade: they are left out.
        private const float GradientReach = 0.85f;

        /// <summary>The faces of <paramref name="surface"/> (glowing glass left out) by plane and paint.</summary>
        public static List<SkinPlane> Planes(LowPolyMeshBuilder surface)
        {
            var planes = new List<SkinPlane>();
            var lookup = new Dictionary<long, SkinPlane>();
            IReadOnlyList<Vector3> positions = surface.Positions;
            IReadOnlyList<Vector3> normals = surface.Normals;
            for (int t = 0; t < surface.TriangleCount; t++)
            {
                PaletteSwatch swatch = surface.SwatchOf(t);
                if (Palette.IsEmissive(swatch))
                {
                    continue;
                }

                Vector3 normal = normals[t * 3];
                var step = new Vector3Int(Mathf.RoundToInt(normal.x / NormalStep),
                    Mathf.RoundToInt(normal.y / NormalStep), Mathf.RoundToInt(normal.z / NormalStep));
                int offset = Mathf.RoundToInt(Vector3.Dot(normal, positions[t * 3]) / OffsetStep);
                long key = Key(step, offset, swatch);
                if (!lookup.TryGetValue(key, out SkinPlane plane))
                {
                    plane = new SkinPlane(((Vector3)step).normalized, swatch, (int)(key ^ (key >> 32)));
                    lookup.Add(key, plane);
                    planes.Add(plane);
                }

                plane.Add(t, positions[t * 3], positions[t * 3 + 1], positions[t * 3 + 2]);
            }

            return planes;
        }

        /// <summary>
        /// Every face of <paramref name="surface"/> again, <paramref name="lift"/> proud of it, in tired paint: big
        /// flat faces cut into panels (each sun-bleached, grimy, yellowed, or a mismatched replacement), small facets
        /// toned by the patch of space they sit in.
        /// </summary>
        public static void Patchwork(LowPolyMeshBuilder skin, LowPolyMeshBuilder surface, List<SkinPlane> planes,
            WeatherProfile profile, float lift)
        {
            var corners = new Vector3[3];
            var flat = new List<Vector2>(3);
            var piece = new List<Vector2>(8);
            var scratch = new List<Vector2>(8);
            var points = new List<Vector3>(8);
            foreach (SkinPlane plane in planes)
            {
                PanelGrid grid = Panelled(plane) ? new PanelGrid(plane, profile) : null;
                bool member = IsMember(plane);
                foreach (int t in plane.Triangles)
                {
                    Corners(surface, t, corners);
                    if (Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).magnitude * 0.5f < MinSkinFace)
                    {
                        continue;
                    }

                    Vector3 normal = surface.Normals[t * 3];
                    if (grid == null)
                    {
                        Vector3 centre = (corners[0] + corners[1] + corners[2]) / 3f;
                        float roll = member
                            ? Roll((int)plane.Swatch, Mathf.FloorToInt(centre.x / MemberColumn), 0,
                                Mathf.FloorToInt(centre.z / MemberColumn), profile.Seed)
                            : Roll((int)plane.Swatch, Mathf.FloorToInt(centre.x / profile.PanelWidth),
                                Mathf.FloorToInt(centre.y / profile.PanelHeight),
                                Mathf.FloorToInt(centre.z / profile.PanelWidth), profile.Seed);
                        points.Clear();
                        points.Add(corners[0] + normal * lift);
                        points.Add(corners[1] + normal * lift);
                        points.Add(corners[2] + normal * lift);
                        Weathering.PanelTone(plane.Swatch, roll, false, out PaletteSwatch toward, out float amount);
                        MeshRange facet = skin.Polygon(points, normal, plane.Swatch);
                        skin.Shade(facet, Palette.GetSurface(toward), amount * profile.PaintWear);
                        skin.Shade(facet, Palette.GetSurface(PaletteSwatch.Charcoal), AgeVeil * profile.PaintWear);
                        continue;
                    }

                    flat.Clear();
                    for (int i = 0; i < 3; i++)
                    {
                        flat.Add(plane.Project(corners[i]));
                    }

                    grid.Cells(flat, out int i0, out int i1, out int j0, out int j1);
                    for (int i = i0; i <= i1; i++)
                    {
                        for (int j = j0; j <= j1; j++)
                        {
                            piece.Clear();
                            piece.AddRange(flat);
                            ClipConvex(piece, grid.Cell(i, j), scratch);
                            if (piece.Count < 3)
                            {
                                continue;
                            }

                            Lift(piece, flat, corners, normal * lift, points);
                            MeshRange range = skin.Polygon(points, normal, plane.Swatch);
                            Tone(skin, range, plane.Swatch, Roll(plane.Key, i, j, 0, profile.Seed), profile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rust runs hanging from the top edge of every panel on every wall of <paramref name="surface"/>, each with a
        /// dark bloom at its bolt, fading down its length into the panel's paint and cut to the wall's outline.
        /// </summary>
        public static void RustRuns(LowPolyMeshBuilder skin, LowPolyMeshBuilder surface, List<SkinPlane> planes,
            WeatherProfile profile, float lift)
        {
            Color32 dark = Weathering.DarkRust;
            var corners = new Vector3[3];
            var flat = new List<Vector2>(3);
            var run = new List<Vector2>(8);
            var scratch = new List<Vector2>(8);
            var points = new List<Vector3>(8);
            foreach (SkinPlane plane in planes)
            {
                Vector2 size = plane.Max - plane.Min;
                if (Mathf.Abs(plane.Normal.y) > WallSlope || !Panelled(plane) || size.x < MinRunWall
                    || size.y < MinRunWall)
                {
                    continue;
                }

                var grid = new PanelGrid(plane, profile);
                for (int i = 0; i < grid.Columns; i++)
                {
                    for (int j = 0; j < grid.Rows; j++)
                    {
                        Vector2[] cell = grid.Cell(i, j);
                        EdgeRust(skin, surface, plane, cell, i, j, profile, lift, corners, flat, run, scratch,
                            points);
                        float width = cell[1].x - cell[0].x;
                        int runs = Mathf.RoundToInt(width * profile.RunsPerMetre
                            * (0.4f + 1.2f * Roll(plane.Key, i, j, 1, profile.Seed)));
                        for (int k = 0; k < runs; k++)
                        {
                            float x = Mathf.Lerp(cell[0].x + RunMargin, cell[1].x - RunMargin,
                                Roll(plane.Key, i, j, 10 + 3 * k, profile.Seed));
                            float w = Mathf.Lerp(MinRunWidth, MaxRunWidth, Roll(plane.Key, i, j, 11 + 3 * k,
                                profile.Seed));
                            float top = cell[2].y - RunTopGap;
                            float length = Mathf.Lerp(MinRunLength, MaxRunLength, Roll(plane.Key, i, j, 12 + 3 * k,
                                profile.Seed)) * (top - plane.Min.y - RunTopGap);
                            int foot = grid.Row(top - length);
                            Color32 fadeTo = ToneColour(plane.Swatch, Roll(plane.Key, i, foot, 0, profile.Seed),
                                profile);
                            int first = skin.TriangleCount;
                            run.Clear();
                            run.Add(new Vector2(x - w * 0.5f, top));
                            run.Add(new Vector2(x - w * 0.5f * RunTaper, top - length));
                            run.Add(new Vector2(x + w * 0.5f * RunTaper, top - length));
                            run.Add(new Vector2(x + w * 0.5f, top));
                            Lay(skin, surface, plane, run, corners, flat, scratch, points, lift, PaletteSwatch.Rust);
                            MeshRange stain = skin.RangeFrom(first);
                            if (stain.TriangleCount > 0)
                            {
                                Bounds reach = skin.GetBounds(stain);
                                skin.ShadeByHeight(stain, fadeTo, reach.min.y, reach.max.y, RunFade);
                            }

                            int bolt = skin.TriangleCount;
                            run.Clear();
                            Bloom(run, new Vector2(x, top - w * 0.6f), w * 0.75f);
                            Lay(skin, surface, plane, run, corners, flat, scratch, points, lift + 0.001f,
                                PaletteSwatch.Rust);
                            skin.Shade(skin.RangeFrom(bolt), dark, 1f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The visible <paramref name="paint"/> skin's lower faces again, darkening to rust towards the ground and
        /// fading out at <see cref="WeatherProfile.RustHeight"/>: bare metal oxidised deep (legs, feet, lower
        /// structure), paint rusting through more lightly.
        /// </summary>
        public static void Oxidise(LowPolyMeshBuilder skin, LowPolyMeshBuilder paint, WeatherProfile profile,
            float lift)
        {
            if (profile.RustHeight <= 0f)
            {
                return;
            }

            float reach = profile.RustHeight * GradientReach;
            MeshRange metal = skin.AppendWhere(paint,
                t => paint.SwatchOf(t) == PaletteSwatch.Metal && MinY(paint, t) < reach, lift);
            skin.ShadeByHeight(metal, Weathering.DarkRust, 0f, profile.RustHeight, profile.MetalRust);
            if (profile.PaintRust <= 0f)
            {
                return;
            }

            MeshRange painted = skin.AppendWhere(paint,
                t => Rusts(paint.SwatchOf(t)) && MinY(paint, t) < reach, lift);
            skin.ShadeByHeight(painted, Weathering.Rust, 0f, profile.RustHeight, profile.PaintRust);
        }

        /// <summary>
        /// The dust over the visible <paramref name="layers"/>: their faces below the tide shaded towards caked dust
        /// more and more towards the ground, and every face turned to the sky caked over.
        /// </summary>
        public static void Dust(LowPolyMeshBuilder skin, WeatherProfile profile, float lift,
            params LowPolyMeshBuilder[] layers)
        {
            Color32 dust = Palette.GetSurface(PaletteSwatch.CakedDust);
            bool grounded = profile.Tide > 0f;
            float reach = profile.Tide * GradientReach;
            foreach (LowPolyMeshBuilder layer in layers)
            {
                MeshRange range = skin.AppendWhere(layer,
                    t => layer.Normals[t * 3].y >= SkyFacing || (grounded && MinY(layer, t) < reach), lift);
                if (grounded)
                {
                    skin.ShadeByHeight(range, dust, 0f, profile.Tide, profile.GroundDust);
                }

                skin.ShadeFacing(range, Vector3.up, SkyFacing, dust, profile.TopDust);
            }
        }

        /// <summary>
        /// On a share of the panels, rust gathered along the bottom edge: a band in the panel's tone turning to rust
        /// towards the edge.
        /// </summary>
        private static void EdgeRust(LowPolyMeshBuilder skin, LowPolyMeshBuilder surface, SkinPlane plane,
            Vector2[] cell, int i, int j, WeatherProfile profile, float lift, Vector3[] corners, List<Vector2> flat,
            List<Vector2> band, List<Vector2> scratch, List<Vector3> points)
        {
            if (Roll(plane.Key, i, j, 2, profile.Seed) >= EdgeRustShare)
            {
                return;
            }

            float bottom = cell[0].y;
            float height = Mathf.Lerp(MinEdgeRust, MaxEdgeRust, Roll(plane.Key, i, j, 3, profile.Seed))
                * (cell[2].y - bottom);
            band.Clear();
            band.Add(cell[0]);
            band.Add(cell[1]);
            band.Add(new Vector2(cell[1].x, bottom + height));
            band.Add(new Vector2(cell[0].x, bottom + height));
            int first = skin.TriangleCount;
            Lay(skin, surface, plane, band, corners, flat, scratch, points, lift * 0.9f, plane.Swatch);
            MeshRange range = skin.RangeFrom(first);
            if (range.TriangleCount == 0)
            {
                return;
            }

            skin.Shade(range, ToneColour(plane.Swatch, Roll(plane.Key, i, j, 0, profile.Seed), profile), 1f);
            Bounds reach = skin.GetBounds(range);
            skin.ShadeByHeight(range, Weathering.Rust, reach.min.y, reach.max.y, EdgeRustDepth);
        }

        /// <summary>Whether a plane is big and broad enough to break into panels.</summary>
        private static bool Panelled(SkinPlane plane)
        {
            Vector2 size = plane.Max - plane.Min;
            return plane.Area >= PanelPlaneArea && Mathf.Min(size.x, size.y) >= MinPanelSide;
        }

        /// <summary>Whether a plane is a long thin member (a post, a rail, a strut).</summary>
        private static bool IsMember(SkinPlane plane)
        {
            Vector2 size = plane.Max - plane.Min;
            return Mathf.Max(size.x, size.y) >= ThinMember * Mathf.Min(size.x, size.y);
        }

        /// <summary>Lays a 2D shape on a plane's faces (cut to each of them), lifted, in a swatch.</summary>
        private static void Lay(LowPolyMeshBuilder skin, LowPolyMeshBuilder surface, SkinPlane plane,
            List<Vector2> shape, Vector3[] corners, List<Vector2> flat, List<Vector2> scratch, List<Vector3> points,
            float lift, PaletteSwatch swatch)
        {
            var piece = new List<Vector2>(shape.Count + 3);
            var clipper = new Vector2[3];
            foreach (int t in plane.Triangles)
            {
                Corners(surface, t, corners);
                flat.Clear();
                for (int i = 0; i < 3; i++)
                {
                    flat.Add(plane.Project(corners[i]));
                }

                bool clockwise = Cross(flat[1] - flat[0], flat[2] - flat[0]) < 0f;
                clipper[0] = flat[0];
                clipper[1] = clockwise ? flat[2] : flat[1];
                clipper[2] = clockwise ? flat[1] : flat[2];
                piece.Clear();
                piece.AddRange(shape);
                ClipConvex(piece, clipper, scratch);
                if (piece.Count < 3)
                {
                    continue;
                }

                Vector3 normal = surface.Normals[t * 3];
                Lift(piece, flat, corners, normal * lift, points);
                skin.Polygon(points, normal, swatch);
            }
        }

        /// <summary>The 3D points of a 2D piece cut from triangle (corners, flat), each pushed by offset.</summary>
        private static void Lift(List<Vector2> piece, List<Vector2> flat, Vector3[] corners, Vector3 offset,
            List<Vector3> points)
        {
            points.Clear();
            float area = Cross(flat[1] - flat[0], flat[2] - flat[0]);
            foreach (Vector2 p in piece)
            {
                float wb = Cross(p - flat[0], flat[2] - flat[0]) / area;
                float wc = Cross(flat[1] - flat[0], p - flat[0]) / area;
                points.Add(corners[0] + (corners[1] - corners[0]) * wb + (corners[2] - corners[0]) * wc + offset);
            }
        }

        /// <summary>A rough rust bloom round a bolt: a hexagon with an uneven rim.</summary>
        private static void Bloom(List<Vector2> shape, Vector2 centre, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + 0.3f;
                float reach = radius * (i % 2 == 0 ? 1f : 0.78f);
                shape.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach);
            }
        }

        /// <summary>Paints a piece in the tone its panel weathered to.</summary>
        private static void Tone(LowPolyMeshBuilder skin, MeshRange range, PaletteSwatch swatch, float roll,
            WeatherProfile profile)
        {
            Weathering.PanelTone(swatch, roll, profile.Mismatched, out PaletteSwatch toward, out float amount);
            skin.Shade(range, Palette.GetSurface(toward), amount * profile.PaintWear);
            skin.Shade(range, Palette.GetSurface(PaletteSwatch.Charcoal), AgeVeil * profile.PaintWear);
        }

        private static Color32 ToneColour(PaletteSwatch swatch, float roll, WeatherProfile profile)
        {
            Weathering.PanelTone(swatch, roll, profile.Mismatched, out PaletteSwatch toward, out float amount);
            Color32 tone = Color32.Lerp(Palette.GetSurface(swatch), Palette.GetSurface(toward),
                amount * profile.PaintWear);
            return Color32.Lerp(tone, Palette.GetSurface(PaletteSwatch.Charcoal), AgeVeil * profile.PaintWear);
        }

        /// <summary>Paint and fittings that rust through (not glass, dust or rust itself).</summary>
        private static bool Rusts(PaletteSwatch swatch)
        {
            switch (swatch)
            {
                case PaletteSwatch.Metal:
                case PaletteSwatch.Rust:
                case PaletteSwatch.CakedDust:
                case PaletteSwatch.DustLight:
                case PaletteSwatch.DustMid:
                case PaletteSwatch.DustShadow:
                case PaletteSwatch.Wood:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Sutherland-Hodgman: cuts <paramref name="polygon"/> in place to a convex clipper (CCW).</summary>
        private static void ClipConvex(List<Vector2> polygon, IReadOnlyList<Vector2> clipper, List<Vector2> scratch)
        {
            for (int e = 0; e < clipper.Count && polygon.Count > 0; e++)
            {
                Vector2 a = clipper[e];
                Vector2 b = clipper[(e + 1) % clipper.Count];
                scratch.Clear();
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 from = polygon[i];
                    Vector2 to = polygon[(i + 1) % polygon.Count];
                    float fromSide = Cross(b - a, from - a);
                    float toSide = Cross(b - a, to - a);
                    if (fromSide >= 0f)
                    {
                        scratch.Add(from);
                    }

                    if ((fromSide >= 0f) != (toSide >= 0f))
                    {
                        scratch.Add(Vector2.Lerp(from, to, fromSide / (fromSide - toSide)));
                    }
                }

                polygon.Clear();
                polygon.AddRange(scratch);
            }
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static void Corners(LowPolyMeshBuilder surface, int triangle, Vector3[] corners)
        {
            for (int i = 0; i < 3; i++)
            {
                corners[i] = surface.Positions[triangle * 3 + i];
            }
        }

        private static float MinY(LowPolyMeshBuilder builder, int triangle)
        {
            IReadOnlyList<Vector3> p = builder.Positions;
            return Mathf.Min(p[triangle * 3].y, Mathf.Min(p[triangle * 3 + 1].y, p[triangle * 3 + 2].y));
        }

        private static long Key(Vector3Int normal, int offset, PaletteSwatch swatch)
        {
            unchecked
            {
                long key = normal.x & 0xFF;
                key = (key << 8) | (uint)(normal.y & 0xFF);
                key = (key << 8) | (uint)(normal.z & 0xFF);
                key = (key << 8) | (uint)((int)swatch & 0xFF);
                return (key << 24) | (uint)(offset & 0xFFFFFF);
            }
        }

        /// <summary>A repeatable roll in [0, 1) from a few integers (a small integer hash).</summary>
        private static float Roll(int a, int b, int c, int d, int seed)
        {
            unchecked
            {
                uint h = (uint)a * 0x9E3779B1u;
                h ^= (uint)b * 0x85EBCA77u + (h << 6) + (h >> 2);
                h ^= (uint)c * 0xC2B2AE3Du + (h << 6) + (h >> 2);
                h ^= (uint)d * 0x27D4EB2Fu + (h << 6) + (h >> 2);
                h ^= (uint)seed * 0x165667B1u + (h << 6) + (h >> 2);
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>The panels a plane's paint breaks into: a grid over it, cells close to the panel size.</summary>
        private sealed class PanelGrid
        {
            private readonly Vector2 _min;
            private readonly Vector2 _cell;

            public PanelGrid(SkinPlane plane, WeatherProfile profile)
            {
                Vector2 size = plane.Max - plane.Min;
                Columns = Math.Max(1, Mathf.RoundToInt(size.x / profile.PanelWidth));
                Rows = Math.Max(1, Mathf.RoundToInt(size.y / profile.PanelHeight));
                _min = plane.Min;
                _cell = new Vector2(size.x / Columns, size.y / Rows);
            }

            public int Columns { get; }

            public int Rows { get; }

            /// <summary>Cell (i, j) as a counter-clockwise rectangle.</summary>
            public Vector2[] Cell(int i, int j)
            {
                var low = new Vector2(_min.x + i * _cell.x, _min.y + j * _cell.y);
                Vector2 high = low + _cell;
                return new[] { low, new Vector2(high.x, low.y), high, new Vector2(low.x, high.y) };
            }

            /// <summary>The row holding height <paramref name="v"/> on the plane.</summary>
            public int Row(float v)
            {
                return Mathf.Clamp(Mathf.FloorToInt((v - _min.y) / _cell.y), 0, Rows - 1);
            }

            /// <summary>The range of cells a flat triangle overlaps.</summary>
            public void Cells(List<Vector2> flat, out int i0, out int i1, out int j0, out int j1)
            {
                Vector2 low = Vector2.Min(flat[0], Vector2.Min(flat[1], flat[2]));
                Vector2 high = Vector2.Max(flat[0], Vector2.Max(flat[1], flat[2]));
                i0 = Mathf.Clamp(Mathf.FloorToInt((low.x - _min.x) / _cell.x), 0, Columns - 1);
                i1 = Mathf.Clamp(Mathf.FloorToInt((high.x - _min.x) / _cell.x), 0, Columns - 1);
                j0 = Mathf.Clamp(Mathf.FloorToInt((low.y - _min.y) / _cell.y), 0, Rows - 1);
                j1 = Mathf.Clamp(Mathf.FloorToInt((high.y - _min.y) / _cell.y), 0, Rows - 1);
            }
        }
    }
}
