using System.Collections.Generic;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Weathered wreck parts and the low-poly weathering of VISION ruling 12, shared by the salvage site recipes:
    /// corrugated sheets, bars, cargo pods, cable drums, solar panels, crates, junction boxes and cables, and the
    /// decades on them as flat facets (rust patches, rust streaks running down from bolts, caked dust on top faces,
    /// dust drifts piled against bases). Big readable shapes only: everything must still read at 30 m.
    /// A face frame (<see cref="Face"/>) has its surface in the XY plane, +Z out of the surface and +Y up along it.
    /// </summary>
    internal static class SiteKit
    {
        // Faces turned at most about 37 degrees from the sky hold caked dust.
        private const float DustFacing = 0.8f;

        // Only the flat crest of a drift takes the lighter dust; lumps keep it from reading as a dome.
        private const float DriftTop = 0.97f;
        private const float DriftLumps = 0.09f;
        private const float SheetThickness = 0.04f;
        private const float RidgePitch = 0.45f;
        private const int CableSides = 5;
        private const float StreakWidth = 0.05f;

        // A shallow dish walked so the bowl faces up (radius, height); scaled to the dish's radius.
        private static readonly Vector2[] DishProfile =
        {
            new Vector2(0f, -0.18f), new Vector2(0.4f, -0.12f), new Vector2(0.75f, 0.1f), new Vector2(0.69f, 0.12f),
            new Vector2(0.36f, -0.04f), new Vector2(0f, -0.08f),
        };

        /// <summary>
        /// A face frame at <paramref name="origin"/>: +Z along <paramref name="normal"/>, +Y as close to
        /// <paramref name="up"/> as the surface allows.
        /// </summary>
        public static Matrix4x4 Face(Vector3 origin, Vector3 normal, Vector3 up)
        {
            Vector3 z = normal.normalized;
            Vector3 y = (up - Vector3.Dot(up, z) * z).normalized;
            Vector3 x = Vector3.Cross(y, z);
            return new Matrix4x4(x, y, z, new Vector4(origin.x, origin.y, origin.z, 1f));
        }

        /// <summary>
        /// A square bar from <paramref name="from"/> to <paramref name="to"/> (posts, beams, lattice).
        /// </summary>
        public static MeshRange Bar(LowPolyMeshBuilder b, Vector3 from, Vector3 to, float thickness,
            PaletteSwatch swatch)
        {
            float length = Vector3.Distance(from, to) + thickness;
            return b.Box(Along(from, to), new Vector3(thickness, thickness, length), swatch);
        }

        /// <summary>
        /// A corrugated sheet on a face frame: <paramref name="width"/> across the ridges, <paramref name="height"/>
        /// along them (+Y), its low edge rusted through.
        /// </summary>
        public static void Sheet(LowPolyMeshBuilder b, Matrix4x4 face, float width, float height, PaletteSwatch paint)
        {
            b.Box(face, new Vector3(width, height, SheetThickness), paint);
            int ridges = Mathf.Max(2, Mathf.RoundToInt(width / RidgePitch));
            for (int i = 0; i < ridges; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * width / ridges;
                b.Box(face * At(x, 0f, 0.035f), new Vector3(0.07f, height, 0.03f), paint);
            }

            b.Box(face * At(0f, -height * 0.5f + 0.14f, 0.024f), new Vector3(width * 0.85f, 0.2f, 0.008f),
                PaletteSwatch.Rust);
        }

        /// <summary>
        /// A curved sheet on an arc of <paramref name="radius"/> around <paramref name="frame"/>'s Z axis, from
        /// <paramref name="fromDegrees"/> to <paramref name="toDegrees"/> (measured from +X towards +Y) in flat
        /// segments, <paramref name="length"/> along Z.
        /// </summary>
        public static MeshRange ArcSheet(LowPolyMeshBuilder b, Matrix4x4 frame, float radius, float fromDegrees,
            float toDegrees, int segments, float length, PaletteSwatch paint)
        {
            int first = b.TriangleCount;
            float step = (toDegrees - fromDegrees) / segments;
            float chord = 2f * radius * Mathf.Sin(Mathf.Abs(step) * 0.5f * Mathf.Deg2Rad) + SheetThickness;
            for (int i = 0; i < segments; i++)
            {
                float angle = (fromDegrees + (i + 0.5f) * step) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var tangent = new Vector3(-radial.y, radial.x, 0f);
                b.Box(frame * Face(radial * radius, radial, tangent), new Vector3(length, chord, SheetThickness),
                    paint);
            }

            return b.RangeFrom(first);
        }

        /// <summary>A cable along <paramref name="points"/> (slack loops, snapped ends).</summary>
        public static void Cable(LowPolyMeshBuilder b, Vector3[] points, float radius, PaletteSwatch swatch)
        {
            for (int i = 1; i < points.Length; i++)
            {
                RecipeKit.Rod(b, points[i - 1], points[i], radius, CableSides, swatch);
            }
        }

        /// <summary>
        /// Points of a cable hanging slack from <paramref name="from"/> to <paramref name="to"/>, dipping
        /// <paramref name="sag"/> below the straight line at its middle.
        /// </summary>
        public static Vector3[] Sag(Vector3 from, Vector3 to, float sag, int segments)
        {
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points[i] = Vector3.Lerp(from, to, t) + Vector3.down * (4f * sag * t * (1f - t));
            }

            return points;
        }

        /// <summary>A rust streak running down a face from a bolt at (x, y) on the face frame.</summary>
        public static void RustStreak(LowPolyMeshBuilder b, Matrix4x4 face, float x, float y, float length)
        {
            RustStreak(b, face, x, y, length, StreakWidth);
        }

        /// <summary>
        /// As the other overload, the streak <paramref name="width"/> wide (the bolt and drip scale with it).
        /// </summary>
        public static void RustStreak(LowPolyMeshBuilder b, Matrix4x4 face, float x, float y, float length, float width)
        {
            float scale = width / StreakWidth;
            b.Prism(face * At(new Vector3(x, y, 0.008f * scale), AlongZ), 0.03f * scale, 0.016f * scale, 6,
                PaletteSwatch.Metal);
            b.Box(face * At(x, y - length * 0.5f, 0.004f), new Vector3(width, length, 0.008f), PaletteSwatch.Rust);
            b.Box(face * At(x, y - length - 0.03f * scale, 0.004f), new Vector3(width * 0.6f, 0.08f * scale, 0.008f),
                PaletteSwatch.Rust);
        }

        /// <summary>
        /// Rust along one seam of a face frame (M3-15 §3): a few streaks from bolts and the seam at height
        /// <paramref name="seam"/> between <paramref name="left"/> and <paramref name="right"/>, planned by
        /// <see cref="RustRunPlan"/> so neighbours never share a length or a spacing; some only a stain at the bolt.
        /// </summary>
        public static void RustSeam(LowPolyMeshBuilder b, Matrix4x4 face, float left, float right, float seam,
            float drop, float perMetre, int seed)
        {
            RustSeam(b, face, left, right, seam, drop, perMetre, seed, 1f);
        }

        /// <summary>As the other overload, every stain <paramref name="widthScale"/> as wide (finer on 07).</summary>
        public static void RustSeam(LowPolyMeshBuilder b, Matrix4x4 face, float left, float right, float seam,
            float drop, float perMetre, int seed, float widthScale)
        {
            var runs = new List<RustRunPlan.Run>();
            RustRunPlan.Plan(left, right, seam, drop, perMetre, seed, runs);
            foreach (RustRunPlan.Run run in runs)
            {
                RustStreak(b, face, run.X, run.Top, run.Length, run.Width * widthScale);
            }
        }

        /// <summary>An irregular rust patch on a face frame, centred at (x, y).</summary>
        public static void RustPatch(LowPolyMeshBuilder b, Matrix4x4 face, float x, float y, float size)
        {
            Vector2[] outline =
            {
                new Vector2(0.5f, 0.05f), new Vector2(0.3f, 0.42f), new Vector2(-0.1f, 0.5f), new Vector2(-0.48f, 0.2f),
                new Vector2(-0.4f, -0.3f), new Vector2(0f, -0.5f), new Vector2(0.42f, -0.35f),
            };
            for (int i = 0; i < outline.Length; i++)
            {
                outline[i] *= size;
            }

            b.Extrude(face * At(x, y, 0.004f), outline, 0.008f, PaletteSwatch.Rust);
        }

        /// <summary>Caked dust on whatever in <paramref name="range"/> faces the sky.</summary>
        public static void DustCap(LowPolyMeshBuilder b, MeshRange range)
        {
            b.RepaintFacing(range, Vector3.up, DustFacing, PaletteSwatch.CakedDust);
        }

        /// <summary>
        /// A soft dust drift on the ground at (<paramref name="at"/>.x, <paramref name="at"/>.z), half buried:
        /// <paramref name="length"/> along its yaw, rising to <paramref name="height"/>. Painted in the ground's own
        /// dust, lighter on top, so it reads as the moon piling up against the wreck rather than a separate thing.
        /// </summary>
        public static void Drift(LowPolyMeshBuilder b, Vector3 at, float length, float width, float height, float yaw,
            int seed)
        {
            MeshRange drift = b.Icosphere(At(new Vector3(at.x, 0f, at.z), new Vector3(0f, yaw, 0f),
                new Vector3(width, height * 2f, length)), 0.5f, 2,
                Paint.Facing(PaletteSwatch.DustLight, PaletteSwatch.DustMid, Vector3.up, DriftTop),
                new Displacement(seed, DriftLumps, 2.2f, 2));
            b.Shave(drift, Vector3.down, height * 0.4f);
        }

        /// <summary>
        /// A crew cargo pod along <paramref name="frame"/>'s Z: chalky body, metal end caps, caked dust on top and
        /// rust streaks down its +X side. An <paramref name="emptied"/> pod gapes dark at its +Z end.
        /// </summary>
        public static void Pod(LowPolyMeshBuilder b, Matrix4x4 frame, float radius, float length, bool emptied)
        {
            int first = b.TriangleCount;
            Matrix4x4 axis = frame * Matrix4x4.Rotate(Rotation(AlongZ));
            b.Prism(axis, radius, length, 10, PaletteSwatch.FadedPaint);
            b.Frustum(axis * At(0f, -length * 0.5f - 0.06f, 0f), radius * 0.7f, radius * 1.02f, 0.12f, 10,
                PaletteSwatch.Metal);
            if (emptied)
            {
                b.Torus(axis * At(0f, length * 0.5f, 0f), radius * 0.94f, 0.05f, 10, 4, PaletteSwatch.Metal);
                b.Prism(axis * At(0f, length * 0.5f + 0.004f, 0f), radius * 0.9f, 0.012f, 10, PaletteSwatch.Charcoal);
            }
            else
            {
                b.Frustum(axis * At(0f, length * 0.5f + 0.06f, 0f), radius * 1.02f, radius * 0.7f, 0.12f, 10,
                    PaletteSwatch.Metal);
            }

            DustCap(b, b.RangeFrom(first));
            Matrix4x4 side = frame * Face(new Vector3(radius * 0.96f, 0f, 0f), Vector3.right, Vector3.up);
            RustStreak(b, side, -length * 0.25f, radius * 0.1f, radius * 0.45f);
            RustStreak(b, side, length * 0.3f, radius * 0.05f, radius * 0.4f);
        }

        /// <summary>
        /// A dish of <paramref name="radius"/> opening along <paramref name="frame"/>'s +Y (frame origin at the
        /// bowl's back), its feed on a short rod.
        /// </summary>
        public static void Dish(LowPolyMeshBuilder b, Matrix4x4 frame, float radius, PaletteSwatch bowl)
        {
            float s = radius / DishProfile[2].x;
            var profile = new Vector2[DishProfile.Length];
            for (int i = 0; i < profile.Length; i++)
            {
                profile[i] = DishProfile[i] * s;
            }

            b.Lathe(frame * At(0f, -profile[0].y, 0f), profile, 12, bowl);
            b.Torus(frame * At(0f, -profile[0].y + profile[2].y, 0f), radius * 0.96f, radius * 0.035f, 12, 3,
                PaletteSwatch.Metal);
            RecipeKit.Rod(b, frame.MultiplyPoint3x4(new Vector3(0f, 0.05f, 0f)),
                frame.MultiplyPoint3x4(new Vector3(0f, radius * 0.75f, 0f)), radius * 0.04f, 5, PaletteSwatch.Metal);
            b.Prism(frame * At(0f, radius * 0.78f, 0f), radius * 0.09f, radius * 0.14f, 6, PaletteSwatch.Charcoal);
        }

        /// <summary>A cable drum on its side, axis along <paramref name="frame"/>'s X, a loose end trailing.</summary>
        public static void Drum(LowPolyMeshBuilder b, Matrix4x4 frame, float radius, float width, PaletteSwatch cable)
        {
            Matrix4x4 axis = frame * Matrix4x4.Rotate(Rotation(AlongX));
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(axis * At(0f, side * width * 0.5f, 0f), radius, 0.05f, 10, PaletteSwatch.Metal);
            }

            b.Prism(axis, radius * 0.75f, width - 0.04f, 10, cable);
            b.Prism(axis, radius * 0.22f, width + 0.12f, 6, PaletteSwatch.Charcoal);
            Cable(b, new[]
            {
                frame.MultiplyPoint3x4(new Vector3(0.1f, radius * 0.72f, -radius * 0.1f)),
                frame.MultiplyPoint3x4(new Vector3(0.18f, radius * 0.3f, -radius * 0.95f)),
                frame.MultiplyPoint3x4(new Vector3(0.12f, -radius + 0.03f, -radius * 1.5f)),
                frame.MultiplyPoint3x4(new Vector3(-0.1f, -radius + 0.03f, -radius * 2.3f)),
            }, 0.03f, cable);
        }

        /// <summary>
        /// A solar panel on a face frame (cells on +Z): a metal frame and a grid of deep violet cells, one cell lost
        /// when <paramref name="cracked"/>.
        /// </summary>
        public static void SolarPanel(LowPolyMeshBuilder b, Matrix4x4 face, float width, float height, bool cracked)
        {
            b.Box(face, new Vector3(width, height, 0.05f), PaletteSwatch.Metal);
            int across = Mathf.Max(1, Mathf.RoundToInt(width / 0.5f));
            int along = Mathf.Max(1, Mathf.RoundToInt(height / 0.5f));
            float cellX = width / across;
            float cellY = height / along;
            for (int i = 0; i < across; i++)
            {
                for (int j = 0; j < along; j++)
                {
                    bool lost = cracked && i == across - 1 && j == along / 2;
                    var centre = new Vector3(-width * 0.5f + (i + 0.5f) * cellX, -height * 0.5f + (j + 0.5f) * cellY,
                        0.028f);
                    b.Box(face * At(centre), new Vector3(cellX - 0.06f, cellY - 0.06f, 0.008f),
                        lost ? PaletteSwatch.Charcoal : PaletteSwatch.SkyHorizon);
                }
            }
        }

        /// <summary>
        /// A cargo crate: chalky faces, metal end bands, caked dust on top, a rust streak on its +Z face.
        /// </summary>
        public static void Crate(LowPolyMeshBuilder b, Matrix4x4 frame, Vector3 size)
        {
            int first = b.TriangleCount;
            b.Box(frame, size, PaletteSwatch.FadedPaint, 0.02f);
            for (int end = -1; end <= 1; end += 2)
            {
                b.Box(frame * At(end * (size.x * 0.5f - 0.06f), 0f, 0f),
                    new Vector3(0.06f, size.y + 0.02f, size.z + 0.02f), PaletteSwatch.Metal);
            }

            DustCap(b, b.RangeFrom(first));
            RustStreak(b, frame * At(0f, 0f, size.z * 0.5f), size.x * 0.25f, size.y * 0.3f, size.y * 0.5f);
        }

        /// <summary>
        /// A sage junction box with its back on a face frame, its door facing +Z, three cables hanging from its foot.
        /// </summary>
        public static void JunctionBox(LowPolyMeshBuilder b, Matrix4x4 face, Vector3 size)
        {
            Matrix4x4 body = face * At(0f, 0f, size.z * 0.5f);
            b.Box(body, size, PaletteSwatch.Sage, 0.02f);
            b.Box(body * At(0f, 0f, size.z * 0.5f + 0.004f), new Vector3(size.x - 0.1f, size.y - 0.1f, 0.01f),
                PaletteSwatch.Charcoal);
            b.Box(body * At(0f, 0.01f, size.z * 0.5f + 0.01f), new Vector3(size.x - 0.16f, size.y - 0.18f, 0.01f),
                PaletteSwatch.Sage);
            RustPatch(b, body * At(0f, 0f, size.z * 0.5f + 0.014f), size.x * 0.15f, -size.y * 0.2f, size.y * 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * size.x * 0.25f;
                Cable(b, new[]
                {
                    face.MultiplyPoint3x4(new Vector3(x, -size.y * 0.5f + 0.02f, size.z * 0.5f)),
                    face.MultiplyPoint3x4(new Vector3(x * 1.3f, -size.y * 0.5f - 0.3f - i * 0.08f, size.z + 0.12f)),
                    face.MultiplyPoint3x4(new Vector3(x * 1.8f, -size.y * 0.5f - 0.5f - i * 0.12f, size.z + 0.4f)),
                }, 0.022f, i == 1 ? PaletteSwatch.WarmAccent : PaletteSwatch.Charcoal);
            }
        }
    }
}
