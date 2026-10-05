using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>Ear-clipping triangulation of simple (possibly concave) polygons.</summary>
    internal static class PolygonTriangulator
    {
        private const float CollinearEpsilon = 1e-10f;

        /// <summary>Twice the signed area; positive when the polygon winds counter-clockwise (x right, y up).</summary>
        public static float SignedDoubleArea(List<Vector2> polygon)
        {
            float sum = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                sum += polygon[j].x * polygon[i].y - polygon[i].x * polygon[j].y;
            }

            return sum;
        }

        /// <summary>True when no two non-adjacent edges cross (the polygon does not self-intersect).</summary>
        public static bool IsSimple(List<Vector2> polygon)
        {
            int count = polygon.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 a0 = polygon[i];
                Vector2 a1 = polygon[(i + 1) % count];
                for (int j = i + 2; j < count; j++)
                {
                    if ((j + 1) % count == i)
                    {
                        continue;
                    }

                    if (SegmentsCross(a0, a1, polygon[j], polygon[(j + 1) % count]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Appends counter-clockwise index triples covering <paramref name="polygon"/> (which must wind
        /// counter-clockwise) to <paramref name="triangles"/>. Collinear vertices produce no triangle.
        /// </summary>
        public static void Triangulate(List<Vector2> polygon, List<int> triangles, List<int> work)
        {
            work.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                work.Add(i);
            }

            while (work.Count > 3)
            {
                if (!ClipOneEar(polygon, triangles, work))
                {
                    throw new ArgumentException("Polygon is self-intersecting or wound clockwise; cannot triangulate.");
                }
            }

            if (Cross(polygon[work[0]], polygon[work[1]], polygon[work[2]]) > CollinearEpsilon)
            {
                triangles.Add(work[0]);
                triangles.Add(work[1]);
                triangles.Add(work[2]);
            }
        }

        private static bool ClipOneEar(List<Vector2> polygon, List<int> triangles, List<int> work)
        {
            int count = work.Count;
            for (int i = 0; i < count; i++)
            {
                int previous = work[(i + count - 1) % count];
                int current = work[i];
                int next = work[(i + 1) % count];
                Vector2 a = polygon[previous];
                Vector2 b = polygon[current];
                Vector2 c = polygon[next];
                float turn = Cross(a, b, c);
                if (Mathf.Abs(turn) <= CollinearEpsilon)
                {
                    work.RemoveAt(i);
                    return true;
                }

                if (turn < 0f || ContainsOtherVertex(polygon, work, previous, current, next))
                {
                    continue;
                }

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                work.RemoveAt(i);
                return true;
            }

            return false;
        }

        private static bool ContainsOtherVertex(List<Vector2> polygon, List<int> work, int ia, int ib, int ic)
        {
            Vector2 a = polygon[ia];
            Vector2 b = polygon[ib];
            Vector2 c = polygon[ic];
            for (int k = 0; k < work.Count; k++)
            {
                int index = work[k];
                if (index == ia || index == ib || index == ic)
                {
                    continue;
                }

                Vector2 p = polygon[index];
                if (p == a || p == b || p == c)
                {
                    continue;
                }

                if (Cross(a, b, p) >= 0f && Cross(b, c, p) >= 0f && Cross(c, a, p) >= 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentsCross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            float d0 = Cross(b0, b1, a0);
            float d1 = Cross(b0, b1, a1);
            float d2 = Cross(a0, a1, b0);
            float d3 = Cross(a0, a1, b1);
            return ((d0 > 0f && d1 < 0f) || (d0 < 0f && d1 > 0f)) && ((d2 > 0f && d3 < 0f) || (d2 < 0f && d3 > 0f));
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }
    }
}
