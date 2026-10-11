using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World.Map
{
    /// <summary>
    /// A scalar field sampled on a square grid over world XZ, traced into iso-lines by marching squares: the crew
    /// map's contours and the canyon's outlines (docs/features/M3-07). Built at edit time by the map sheet builder,
    /// so it allocates freely. Saddles are resolved by the cell's centre value.
    /// </summary>
    public sealed class IsoGrid
    {
        private readonly float[] _values;
        private readonly int _columns;
        private readonly int _rows;
        private readonly float _step;
        private readonly Vector2 _origin;
        private readonly int _horizontalEdges;

        /// <param name="area">World XZ rectangle to cover (rounded up to whole steps).</param>
        /// <param name="step">Metres between two samples.</param>
        /// <param name="sample">The field at world (x, z).</param>
        public IsoGrid(Rect area, float step, Func<float, float, float> sample)
        {
            if (step <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "The grid step must be positive.");
            }

            if (sample == null)
            {
                throw new ArgumentNullException(nameof(sample));
            }

            _step = step;
            _origin = area.min;
            _columns = Mathf.Max(1, Mathf.CeilToInt(area.width / step));
            _rows = Mathf.Max(1, Mathf.CeilToInt(area.height / step));
            _horizontalEdges = _columns * (_rows + 1);
            _values = new float[(_columns + 1) * (_rows + 1)];
            for (int j = 0; j <= _rows; j++)
            {
                for (int i = 0; i <= _columns; i++)
                {
                    _values[Node(i, j)] = sample(_origin.x + i * step, _origin.y + j * step);
                }
            }
        }

        public float Min
        {
            get
            {
                float min = float.PositiveInfinity;
                foreach (float value in _values)
                {
                    min = Mathf.Min(min, value);
                }

                return min;
            }
        }

        public float Max
        {
            get
            {
                float max = float.NegativeInfinity;
                foreach (float value in _values)
                {
                    max = Mathf.Max(max, value);
                }

                return max;
            }
        }

        /// <summary>
        /// Appends every iso-line at <paramref name="level"/> to <paramref name="lines"/> as a run of points, and
        /// whether it is a closed loop to <paramref name="closed"/>; a loop does not repeat its first point.
        /// </summary>
        public void Trace(float level, List<List<Vector2>> lines, List<bool> closed)
        {
            int edgeCount = _horizontalEdges + (_columns + 1) * _rows;
            var first = new int[edgeCount];
            var second = new int[edgeCount];
            for (int e = 0; e < edgeCount; e++)
            {
                first[e] = -1;
                second[e] = -1;
            }

            for (int j = 0; j < _rows; j++)
            {
                for (int i = 0; i < _columns; i++)
                {
                    Cell(i, j, level, first, second);
                }
            }

            var visited = new bool[edgeCount];
            for (int e = 0; e < edgeCount; e++)
            {
                if (!visited[e] && first[e] >= 0 && second[e] < 0)
                {
                    lines.Add(Walk(e, level, first, second, visited));
                    closed.Add(false);
                }
            }

            for (int e = 0; e < edgeCount; e++)
            {
                if (!visited[e] && first[e] >= 0)
                {
                    lines.Add(Walk(e, level, first, second, visited));
                    closed.Add(true);
                }
            }
        }

        private void Cell(int i, int j, float level, int[] first, int[] second)
        {
            float a = _values[Node(i, j)];
            float b = _values[Node(i + 1, j)];
            float c = _values[Node(i + 1, j + 1)];
            float d = _values[Node(i, j + 1)];
            int bottom = j * _columns + i;
            int top = (j + 1) * _columns + i;
            int left = _horizontalEdges + j * (_columns + 1) + i;
            int right = left + 1;
            int index = (a >= level ? 1 : 0) | (b >= level ? 2 : 0) | (c >= level ? 4 : 0) | (d >= level ? 8 : 0);
            bool centre = (a + b + c + d) * 0.25f >= level;
            switch (index)
            {
                case 1:
                case 14:
                    Link(left, bottom, first, second);
                    break;
                case 2:
                case 13:
                    Link(bottom, right, first, second);
                    break;
                case 3:
                case 12:
                    Link(left, right, first, second);
                    break;
                case 4:
                case 11:
                    Link(right, top, first, second);
                    break;
                case 6:
                case 9:
                    Link(bottom, top, first, second);
                    break;
                case 7:
                case 8:
                    Link(left, top, first, second);
                    break;
                case 5:
                    // a and c are high: joined through a high centre (cut b and d off), apart otherwise.
                    Link(centre ? bottom : left, centre ? right : bottom, first, second);
                    Link(centre ? top : right, centre ? left : top, first, second);
                    break;
                case 10:
                    // b and d are high: joined through a high centre (cut a and c off), apart otherwise.
                    Link(centre ? left : bottom, centre ? bottom : right, first, second);
                    Link(centre ? right : top, centre ? top : left, first, second);
                    break;
            }
        }

        private static void Link(int from, int to, int[] first, int[] second)
        {
            Attach(from, to, first, second);
            Attach(to, from, first, second);
        }

        private static void Attach(int edge, int other, int[] first, int[] second)
        {
            if (first[edge] < 0)
            {
                first[edge] = other;
            }
            else
            {
                second[edge] = other;
            }
        }

        private List<Vector2> Walk(int start, float level, int[] first, int[] second, bool[] visited)
        {
            var points = new List<Vector2>();
            int previous = -1;
            int edge = start;
            while (edge >= 0 && !visited[edge])
            {
                visited[edge] = true;
                points.Add(Crossing(edge, level));
                int next = first[edge] != previous ? first[edge] : second[edge];
                previous = edge;
                edge = next;
            }

            return points;
        }

        /// <summary>Where the field crosses <paramref name="level"/> along a grid edge (linear between its nodes).</summary>
        private Vector2 Crossing(int edge, float level)
        {
            int i0;
            int j0;
            int i1;
            int j1;
            if (edge < _horizontalEdges)
            {
                j0 = edge / _columns;
                i0 = edge - j0 * _columns;
                i1 = i0 + 1;
                j1 = j0;
            }
            else
            {
                int vertical = edge - _horizontalEdges;
                j0 = vertical / (_columns + 1);
                i0 = vertical - j0 * (_columns + 1);
                i1 = i0;
                j1 = j0 + 1;
            }

            float v0 = _values[Node(i0, j0)];
            float v1 = _values[Node(i1, j1)];
            float t = Mathf.Approximately(v0, v1) ? 0.5f : Mathf.Clamp01((level - v0) / (v1 - v0));
            return _origin + new Vector2(Mathf.Lerp(i0, i1, t), Mathf.Lerp(j0, j1, t)) * _step;
        }

        private int Node(int i, int j)
        {
            return j * (_columns + 1) + i;
        }
    }
}
