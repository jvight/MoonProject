using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Finds a walking line over drivable ground between two points (a friend's way out of a winding canyon): an A*
    /// search on a square grid over the box around both ends (diagonal steps allowed, never between two cells whose
    /// ground differs by more than a gentle climb), then string-pulled so the line cuts straight wherever every cell
    /// under it is drivable and gentle. Each grid cell is sampled at most once (the analytic surface is costly).
    /// Deterministic; runs once at initialisation (allocates). Null when no such line exists in the box.
    /// </summary>
    public static class WalkPathPlanner
    {
        private const float Diagonal = 1.41421356f;

        /// <summary>Samples per cell along a straight line when checking it.</summary>
        private const int LineSamplesPerCell = 2;

        private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] StepZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// The waypoints from <paramref name="from"/> to <paramref name="to"/> (both included, on the surface), or
        /// null when the drivable ground does not connect them inside the search box.
        /// </summary>
        public static Vector3[] Plan(ITerrainQuery terrain, Vector3 from, Vector3 to, FriendTuning tuning)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            var grid = new Grid(terrain, from, to, tuning);
            List<int> cells = grid.Search();
            if (cells == null)
            {
                return null;
            }

            var points = new List<Vector3> { SurfaceRules.OnSurface(terrain, from.x, from.z) };
            int current = 0;
            while (current < cells.Count - 1)
            {
                int next = current + 1;
                for (int candidate = cells.Count - 1; candidate > current + 1; candidate--)
                {
                    if (grid.Straight(cells[current], cells[candidate]))
                    {
                        next = candidate;
                        break;
                    }
                }

                if (next < cells.Count - 1)
                {
                    points.Add(grid.Point(cells[next]));
                }

                current = next;
            }

            points.Add(SurfaceRules.OnSurface(terrain, to.x, to.z));
            return points.ToArray();
        }

        /// <summary>The search grid: cells sampled lazily (drivable, height), A* with a binary heap.</summary>
        private sealed class Grid
        {
            private readonly ITerrainQuery _terrain;
            private readonly float _cell;
            private readonly float _maxClimb;
            private readonly int _maxNodes;
            private readonly float _originX;
            private readonly float _originZ;
            private readonly int _width;
            private readonly int _depth;
            private readonly int _start;
            private readonly int _goal;
            private readonly sbyte[] _drivable;
            private readonly bool[] _heightKnown;
            private readonly float[] _height;
            private readonly float[] _cost;
            private readonly int[] _parent;
            private readonly bool[] _closed;
            private readonly List<int> _open = new List<int>();
            private readonly List<float> _openScore = new List<float>();

            public Grid(ITerrainQuery terrain, Vector3 from, Vector3 to, FriendTuning tuning)
            {
                _terrain = terrain;
                _cell = tuning.PathCell;
                _maxClimb = tuning.PathMaxClimb;
                _maxNodes = tuning.PathMaxNodes;
                float margin = tuning.PathMargin;
                _originX = Mathf.Min(from.x, to.x) - margin;
                _originZ = Mathf.Min(from.z, to.z) - margin;
                _width = Mathf.CeilToInt((Mathf.Max(from.x, to.x) + margin - _originX) / _cell) + 1;
                _depth = Mathf.CeilToInt((Mathf.Max(from.z, to.z) + margin - _originZ) / _cell) + 1;
                int count = _width * _depth;
                _drivable = new sbyte[count];
                _heightKnown = new bool[count];
                _height = new float[count];
                _cost = new float[count];
                _parent = new int[count];
                _closed = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    _cost[i] = float.MaxValue;
                    _parent[i] = -1;
                }

                _start = CellAt(from.x, from.z);
                _goal = CellAt(to.x, to.z);
            }

            public Vector3 Point(int cell)
            {
                Vector2 centre = Centre(cell);
                return new Vector3(centre.x, Height(cell), centre.y);
            }

            public List<int> Search()
            {
                _cost[_start] = 0f;
                Push(_start, Heuristic(_start));
                int expanded = 0;
                while (_open.Count > 0 && expanded < _maxNodes)
                {
                    int cell = Pop();
                    if (_closed[cell])
                    {
                        continue;
                    }

                    if (cell == _goal)
                    {
                        return Trace();
                    }

                    _closed[cell] = true;
                    expanded++;
                    int x = cell % _width;
                    int z = cell / _width;
                    for (int s = 0; s < StepX.Length; s++)
                    {
                        int nx = x + StepX[s];
                        int nz = z + StepZ[s];
                        if (nx < 0 || nz < 0 || nx >= _width || nz >= _depth)
                        {
                            continue;
                        }

                        int neighbour = nz * _width + nx;
                        float step = s < 4 ? 1f : Diagonal;
                        if (_closed[neighbour] || !Passable(cell, neighbour, step))
                        {
                            continue;
                        }

                        float cost = _cost[cell] + _cell * step;
                        if (cost < _cost[neighbour])
                        {
                            _cost[neighbour] = cost;
                            _parent[neighbour] = cell;
                            Push(neighbour, cost + Heuristic(neighbour));
                        }
                    }
                }

                return null;
            }

            /// <summary>Every cell under the straight line from <paramref name="a"/> to <paramref name="b"/> is
            /// passable from the one before it.</summary>
            public bool Straight(int a, int b)
            {
                Vector2 from = Centre(a);
                Vector2 to = Centre(b);
                float length = Vector2.Distance(from, to);
                int samples = Mathf.CeilToInt(length / _cell * LineSamplesPerCell);
                int previous = a;
                for (int i = 1; i <= samples; i++)
                {
                    Vector2 point = Vector2.Lerp(from, to, (float)i / samples);
                    int cell = CellAt(point.x, point.y);
                    if (cell == previous)
                    {
                        continue;
                    }

                    if (!Passable(previous, cell, Diagonal))
                    {
                        return false;
                    }

                    previous = cell;
                }

                return true;
            }

            private bool Passable(int from, int to, float step)
            {
                return (to == _goal || Drivable(to)) && Mathf.Abs(Height(to) - Height(from)) <= _maxClimb * step;
            }

            private List<int> Trace()
            {
                var cells = new List<int>();
                for (int cell = _goal; cell >= 0; cell = _parent[cell])
                {
                    cells.Add(cell);
                }

                cells.Reverse();
                return cells;
            }

            private int CellAt(float px, float pz)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt((px - _originX) / _cell), 0, _width - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt((pz - _originZ) / _cell), 0, _depth - 1);
                return z * _width + x;
            }

            private Vector2 Centre(int cell)
            {
                return new Vector2(_originX + (cell % _width) * _cell, _originZ + (cell / _width) * _cell);
            }

            private bool Drivable(int cell)
            {
                if (_drivable[cell] == 0)
                {
                    Vector2 centre = Centre(cell);
                    _drivable[cell] = _terrain.IsDrivable(centre.x, centre.y) ? (sbyte)1 : (sbyte)-1;
                }

                return _drivable[cell] > 0;
            }

            private float Height(int cell)
            {
                if (!_heightKnown[cell])
                {
                    Vector2 centre = Centre(cell);
                    _height[cell] = _terrain.SampleHeight(centre.x, centre.y);
                    _heightKnown[cell] = true;
                }

                return _height[cell];
            }

            private float Heuristic(int cell)
            {
                return Vector2.Distance(Centre(cell), Centre(_goal));
            }

            private void Push(int cell, float score)
            {
                _open.Add(cell);
                _openScore.Add(score);
                int child = _open.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (_openScore[parent] <= _openScore[child])
                    {
                        break;
                    }

                    Swap(parent, child);
                    child = parent;
                }
            }

            private int Pop()
            {
                int top = _open[0];
                int last = _open.Count - 1;
                Swap(0, last);
                _open.RemoveAt(last);
                _openScore.RemoveAt(last);
                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1;
                    if (left >= _open.Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int smallest = right < _open.Count && _openScore[right] < _openScore[left] ? right : left;
                    if (_openScore[parent] <= _openScore[smallest])
                    {
                        break;
                    }

                    Swap(parent, smallest);
                    parent = smallest;
                }

                return top;
            }

            private void Swap(int a, int b)
            {
                int cell = _open[a];
                _open[a] = _open[b];
                _open[b] = cell;
                float score = _openScore[a];
                _openScore[a] = _openScore[b];
                _openScore[b] = score;
            }
        }
    }
}
