using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's blueprint line drawing of a low-poly mesh (0.5.5 §1): a thin bar along every feature edge, the edges
    /// where the faces meeting turn by more than a crease angle, or where a face ends, so the drawing traces the
    /// object's outline and folds and skips the flat panels' diagonals. It draws like a hand, not a bounding box: a
    /// rod's or a bevel's run of close parallel edges is drawn once, and the longer a straight run, the finer its line,
    /// so the part's own small features (coils, flanges, seams) carry the drawing.
    /// </summary>
    internal static class LineDrawing
    {
        // Faces meeting at more than this read as a fold worth drawing.
        private const float CreaseDegrees = 28f;

        // Positions closer than this are one vertex when edges are matched.
        private const float Weld = 1e-4f;

        // Parallel edges closer than this (and overlapping) are one stroke: a rod's facets, a chamfer's two folds.
        private const float StrokeMerge = 0.045f;
        private const float StrokeParallel = 0.985f;

        // A straight run this long or longer is drawn at the finest weight; a short one at the full weight.
        private const float ShortRun = 0.12f;
        private const float LongRun = 0.7f;
        private const float FinestWeight = 0.45f;

        public static LowPolyMeshBuilder FromMesh(LowPolyMeshBuilder source, float thickness)
        {
            var edges = new Dictionary<(Vector3Int, Vector3Int), List<Vector3>>();
            var ends = new Dictionary<(Vector3Int, Vector3Int), (Vector3, Vector3)>();
            IReadOnlyList<Vector3> positions = source.Positions;
            for (int t = 0; t < source.TriangleCount; t++)
            {
                Vector3 a = positions[t * 3];
                Vector3 b = positions[t * 3 + 1];
                Vector3 c = positions[t * 3 + 2];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-12f)
                {
                    continue;
                }

                normal.Normalize();
                foreach ((Vector3 from, Vector3 to) in new[] { (a, b), (b, c), (c, a) })
                {
                    (Vector3Int, Vector3Int) key = Key(from, to);
                    if (!edges.TryGetValue(key, out List<Vector3> faces))
                    {
                        faces = new List<Vector3>(2);
                        edges.Add(key, faces);
                        ends.Add(key, (from, to));
                    }

                    faces.Add(normal);
                }
            }

            float crease = Mathf.Cos(CreaseDegrees * Mathf.Deg2Rad);
            var features = new List<(Vector3 From, Vector3 To)>();
            foreach (KeyValuePair<(Vector3Int, Vector3Int), List<Vector3>> edge in edges)
            {
                List<Vector3> faces = edge.Value;
                (Vector3 from, Vector3 to) = ends[edge.Key];
                if ((faces.Count == 2 && Vector3.Dot(faces[0], faces[1]) > crease)
                    || (to - from).sqrMagnitude < thickness * thickness)
                {
                    continue;
                }

                features.Add((from, to));
            }

            features.Sort((a, b) => (b.To - b.From).sqrMagnitude.CompareTo((a.To - a.From).sqrMagnitude));
            var strokes = new List<(Vector3 From, Vector3 To)>(features.Count);
            var lines = new LowPolyMeshBuilder(features.Count * 12);
            foreach ((Vector3 from, Vector3 to) in features)
            {
                if (strokes.Exists(stroke => SameStroke(stroke.From, stroke.To, from, to)))
                {
                    continue;
                }

                strokes.Add((from, to));
                float length = Vector3.Distance(from, to);
                float weight = Mathf.Lerp(1f, FinestWeight, Mathf.InverseLerp(ShortRun, LongRun, length)) * thickness;
                lines.Box(Place.Along(from, to), new Vector3(weight, weight, length + weight), PaletteSwatch.Cream);
            }

            return lines;
        }

        /// <summary>Whether an edge runs alongside a longer stroke already drawn: parallel, close and
        /// overlapping.</summary>
        private static bool SameStroke(Vector3 strokeFrom, Vector3 strokeTo, Vector3 from, Vector3 to)
        {
            Vector3 axis = (strokeTo - strokeFrom).normalized;
            if (Mathf.Abs(Vector3.Dot(axis, (to - from).normalized)) < StrokeParallel)
            {
                return false;
            }

            Vector3 middle = (from + to) * 0.5f;
            float along = Vector3.Dot(middle - strokeFrom, axis);
            if (along < 0f || along > Vector3.Distance(strokeFrom, strokeTo))
            {
                return false;
            }

            return Vector3.Distance(middle, strokeFrom + axis * along) < StrokeMerge;
        }

        private static (Vector3Int, Vector3Int) Key(Vector3 from, Vector3 to)
        {
            Vector3Int a = Vector3Int.RoundToInt(from / Weld);
            Vector3Int b = Vector3Int.RoundToInt(to / Weld);
            bool ordered = a.x < b.x || (a.x == b.x && (a.y < b.y || (a.y == b.y && a.z <= b.z)));
            return ordered ? (a, b) : (b, a);
        }
    }
}
