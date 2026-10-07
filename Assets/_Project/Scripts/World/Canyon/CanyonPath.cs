using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A densely sampled centre line in the XZ plane, read as the C1 Catmull-Rom curve through its samples, with a
    /// bucketed nearest-point query: for any point within reach it returns the arc length s of the closest point on
    /// the curve (clamped to its ends, so the ends become round caps) and the signed lateral distance (positive to
    /// the right of the travel direction). The polyline finds the neighbourhood and a few Newton steps on the curve
    /// finish the job, so s and the distance vary smoothly (no creases at the samples). Samples must be evenly
    /// spaced. Immutable and thread-safe once built.
    /// </summary>
    public sealed class CanyonPath
    {
        private const float BucketSize = 8f;
        private const int NewtonSteps = 4;

        private readonly Vector2[] _points;
        private readonly float[] _arc;
        private readonly float _reach;
        private readonly Vector2 _gridMin;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int[][] _buckets;

        /// <param name="points">Centre-line samples in travel order (two or more, spaced well under the reach).</param>
        /// <param name="startArc">Arc length of the first sample (negative before the mouth).</param>
        /// <param name="reach">Queries farther than this from the line report nothing.</param>
        public CanyonPath(IReadOnlyList<Vector2> points, float startArc, float reach)
        {
            if (points == null || points.Count < 2)
            {
                throw new ArgumentException("A path needs at least two points.", nameof(points));
            }

            _reach = reach;
            _points = new Vector2[points.Count];
            _arc = new float[points.Count];
            Vector2 min = points[0];
            Vector2 max = points[0];
            float arc = startArc;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    arc += Vector2.Distance(points[i - 1], points[i]);
                }

                _points[i] = points[i];
                _arc[i] = arc;
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            _gridMin = min - Vector2.one * (reach + BucketSize);
            Vector2 size = max - min + Vector2.one * 2f * (reach + BucketSize);
            _gridWidth = Mathf.CeilToInt(size.x / BucketSize);
            _gridHeight = Mathf.CeilToInt(size.y / BucketSize);
            var lists = new List<int>[_gridWidth * _gridHeight];
            for (int i = 0; i + 1 < _points.Length; i++)
            {
                Vector2 low = Vector2.Min(_points[i], _points[i + 1]) - Vector2.one * reach;
                Vector2 high = Vector2.Max(_points[i], _points[i + 1]) + Vector2.one * reach;
                int x0 = Mathf.FloorToInt((low.x - _gridMin.x) / BucketSize);
                int x1 = Mathf.FloorToInt((high.x - _gridMin.x) / BucketSize);
                int y0 = Mathf.FloorToInt((low.y - _gridMin.y) / BucketSize);
                int y1 = Mathf.FloorToInt((high.y - _gridMin.y) / BucketSize);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int bucket = y * _gridWidth + x;
                        lists[bucket] = lists[bucket] ?? new List<int>();
                        lists[bucket].Add(i);
                    }
                }
            }

            _buckets = new int[lists.Length][];
            for (int i = 0; i < lists.Length; i++)
            {
                _buckets[i] = lists[i]?.ToArray();
            }

            Bounds = new Rect(_gridMin, size);
        }

        /// <summary>Everything a query can report on lies inside this rectangle.</summary>
        public Rect Bounds { get; }

        public float StartArc => _arc[0];

        public float EndArc => _arc[_arc.Length - 1];

        /// <summary>Closest point on the line to (x, z); false when nothing lies within reach.</summary>
        public bool TryProject(float x, float z, out float arc, out float lateral)
        {
            arc = 0f;
            lateral = 0f;
            int bx = Mathf.FloorToInt((x - _gridMin.x) / BucketSize);
            int by = Mathf.FloorToInt((z - _gridMin.y) / BucketSize);
            if (bx < 0 || by < 0 || bx >= _gridWidth || by >= _gridHeight)
            {
                return false;
            }

            int[] segments = _buckets[by * _gridWidth + bx];
            if (segments == null)
            {
                return false;
            }

            var p = new Vector2(x, z);
            float best = _reach * _reach;
            int segment = -1;
            float u = 0f;
            for (int k = 0; k < segments.Length; k++)
            {
                int i = segments[k];
                Vector2 a = _points[i];
                Vector2 ab = _points[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float distanceSq = (p - (a + ab * t)).sqrMagnitude;
                if (distanceSq < best)
                {
                    best = distanceSq;
                    segment = i;
                    u = t;
                }
            }

            if (segment < 0)
            {
                return false;
            }

            Refine(p, ref segment, ref u);
            int last = _points.Length - 2;
            bool beyondStart = segment == 0 && u <= 0f;
            bool beyondEnd = segment == last && u >= 1f;
            u = Mathf.Clamp01(u);
            Curve(segment, u, out Vector2 foot, out Vector2 tangent, out Vector2 _);
            float distance = Vector2.Distance(p, foot);
            if (distance >= _reach)
            {
                return false;
            }

            arc = Mathf.Lerp(_arc[segment], _arc[segment + 1], u);
            float side = tangent.x * (p.y - foot.y) - tangent.y * (p.x - foot.x);

            // Past either end the curve is a round cap: distance is radial and has no side.
            lateral = beyondStart || beyondEnd ? distance : (side > 0f ? -distance : distance);
            return true;
        }

        /// <summary>
        /// Newton steps on (C(u) - p) . C'(u) = 0, stepping into the neighbouring segment when u leaves [0, 1].
        /// Leaves u below 0 or above 1 only past the curve's ends.
        /// </summary>
        private void Refine(Vector2 p, ref int segment, ref float u)
        {
            int last = _points.Length - 2;
            for (int step = 0; step < NewtonSteps; step++)
            {
                Curve(segment, u, out Vector2 position, out Vector2 first, out Vector2 second);
                Vector2 offset = position - p;
                float slope = Vector2.Dot(first, first) + Vector2.Dot(offset, second);
                if (slope <= 1e-6f)
                {
                    return;
                }

                u -= Vector2.Dot(offset, first) / slope;
                if (u > 1f && segment < last)
                {
                    segment++;
                    u -= 1f;
                }
                else if (u < 0f && segment > 0)
                {
                    segment--;
                    u += 1f;
                }
            }
        }

        /// <summary>Uniform Catmull-Rom position and first two derivatives on segment i at u in [0, 1].</summary>
        private void Curve(int i, float u, out Vector2 position, out Vector2 first, out Vector2 second)
        {
            Vector2 p1 = _points[i];
            Vector2 p2 = _points[i + 1];
            Vector2 p0 = i > 0 ? _points[i - 1] : 2f * p1 - p2;
            Vector2 p3 = i + 2 < _points.Length ? _points[i + 2] : 2f * p2 - p1;
            Vector2 a = 2f * p1;
            Vector2 b = p2 - p0;
            Vector2 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector2 d = -p0 + 3f * p1 - 3f * p2 + p3;
            float uu = u * u;
            position = 0.5f * (a + b * u + c * uu + d * uu * u);
            first = 0.5f * (b + 2f * c * u + 3f * d * uu);
            second = 0.5f * (2f * c + 6f * d * u);
        }

        /// <summary>Centre-line point at arc length <paramref name="arc"/> (clamped to the line).</summary>
        public Vector2 PointAt(float arc)
        {
            int i = Segment(arc, out float t);
            return Vector2.Lerp(_points[i], _points[i + 1], t);
        }

        /// <summary>Unit travel direction at arc length <paramref name="arc"/>.</summary>
        public Vector2 TangentAt(float arc)
        {
            int i = Segment(arc, out float _);
            return (_points[i + 1] - _points[i]).normalized;
        }

        /// <summary>Unit vector to the right of the travel direction (the positive lateral side).</summary>
        public Vector2 RightAt(float arc)
        {
            Vector2 tangent = TangentAt(arc);
            return new Vector2(tangent.y, -tangent.x);
        }

        private int Segment(float arc, out float t)
        {
            if (arc <= _arc[0])
            {
                t = 0f;
                return 0;
            }

            for (int i = 0; i + 1 < _arc.Length; i++)
            {
                if (arc <= _arc[i + 1])
                {
                    t = (arc - _arc[i]) / Mathf.Max(_arc[i + 1] - _arc[i], 1e-6f);
                    return i;
                }
            }

            t = 1f;
            return _arc.Length - 2;
        }
    }
}
