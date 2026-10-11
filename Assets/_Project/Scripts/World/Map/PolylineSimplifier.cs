using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World.Map
{
    /// <summary>Douglas-Peucker thinning of a traced polyline: keeps its shape within a tolerance, drops the rest.</summary>
    public static class PolylineSimplifier
    {
        /// <summary>
        /// The points of <paramref name="points"/> that keep it within <paramref name="tolerance"/> metres, in order. A
        /// closed loop keeps its first point and still does not repeat it at the end.
        /// </summary>
        public static List<Vector2> Simplify(List<Vector2> points, bool closed, float tolerance)
        {
            var result = new List<Vector2>();
            if (points.Count < 3)
            {
                result.AddRange(points);
                return result;
            }

            var run = new List<Vector2>(points);
            if (closed)
            {
                run.Add(points[0]);
            }

            var keep = new bool[run.Count];
            keep[0] = true;
            keep[run.Count - 1] = true;
            var spans = new Stack<Vector2Int>();
            spans.Push(new Vector2Int(0, run.Count - 1));
            while (spans.Count > 0)
            {
                Vector2Int span = spans.Pop();
                int farthest = -1;
                float worst = tolerance;
                for (int i = span.x + 1; i < span.y; i++)
                {
                    float distance = DistanceToSegment(run[i], run[span.x], run[span.y]);
                    if (distance > worst)
                    {
                        worst = distance;
                        farthest = i;
                    }
                }

                if (farthest >= 0)
                {
                    keep[farthest] = true;
                    spans.Push(new Vector2Int(span.x, farthest));
                    spans.Push(new Vector2Int(farthest, span.y));
                }
            }

            int end = closed ? run.Count - 1 : run.Count;
            for (int i = 0; i < end; i++)
            {
                if (keep[i])
                {
                    result.Add(run[i]);
                }
            }

            return result;
        }

        /// <summary>The polyline's length in metres (a closed loop includes its closing span).</summary>
        public static float Length(List<Vector2> points, bool closed)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }

            return closed && points.Count > 1 ? length + Vector2.Distance(points[points.Count - 1], points[0]) : length;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
            return Vector2.Distance(point, a + ab * t);
        }
    }
}
