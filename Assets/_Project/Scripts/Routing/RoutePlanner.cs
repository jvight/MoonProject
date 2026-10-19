using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Routing
{
    /// <summary>
    /// A coarse path round obstacles on the ground plane, for a bot that cannot see a lander in the way: a grid over
    /// the start and the goal (plus a margin), A* over the cells the caller says are free, then the path pulled
    /// straight wherever the way between two of its points is clear. Plain maths: the caller supplies what is blocked.
    /// </summary>
    public static class RoutePlanner
    {
        private const int MaxCells = 400;
        private const float Sqrt2 = 1.41421356f;

        /// <summary>
        /// Fills <paramref name="waypoints"/> with the points to drive through, ending at <paramref name="goal"/>;
        /// a straight drive is the single point <paramref name="goal"/>. False (and just the goal) when no way round
        /// is found. Points within <paramref name="relax"/> metres of the start or the goal count as free, so a bot
        /// standing against a prop can leave it and a goal beside one can be reached.
        /// </summary>
        public static bool Plan(Vector2 start, Vector2 goal, Func<Vector2, bool> blocked, float cell, float margin,
            float relax, List<Vector2> waypoints)
        {
            waypoints.Clear();
            Func<Vector2, bool> solid = point => blocked(point) && (point - start).magnitude > relax &&
                                                  (point - goal).magnitude > relax;
            if (Clear(start, goal, solid, cell))
            {
                waypoints.Add(goal);
                return true;
            }

            Vector2 low = Vector2.Min(start, goal) - Vector2.one * margin;
            Vector2 high = Vector2.Max(start, goal) + Vector2.one * margin;
            int width = Mathf.Min(MaxCells, Mathf.CeilToInt((high.x - low.x) / cell) + 1);
            int depth = Mathf.Min(MaxCells, Mathf.CeilToInt((high.y - low.y) / cell) + 1);
            var states = new sbyte[width * depth];
            Vector2 CenterOf(int index)
            {
                return low + new Vector2(index % width, index / width) * cell;
            }

            bool Free(int index)
            {
                if (states[index] == 0)
                {
                    states[index] = (sbyte)(solid(CenterOf(index)) ? 2 : 1);
                }

                return states[index] == 1;
            }

            int startCell = CellOf(start, low, cell, width, depth);
            int goalCell = CellOf(goal, low, cell, width, depth);
            var best = new float[width * depth];
            var parent = new int[width * depth];
            for (int i = 0; i < best.Length; i++)
            {
                best[i] = float.MaxValue;
                parent[i] = -1;
            }

            var open = new List<(float Score, int Cell)>();
            best[startCell] = 0f;
            open.Add((Heuristic(startCell, goalCell, width), startCell));
            bool found = false;
            while (open.Count > 0)
            {
                int top = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    top = open[i].Score < open[top].Score ? i : top;
                }

                int current = open[top].Cell;
                open[top] = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                if (current == goalCell)
                {
                    found = true;
                    break;
                }

                int cx = current % width;
                int cz = current / width;
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx;
                        int nz = cz + dz;
                        if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= width || nz >= depth)
                        {
                            continue;
                        }

                        int next = nz * width + nx;
                        bool diagonal = dx != 0 && dz != 0;
                        if (!Free(next) || (diagonal && (!Free(cz * width + nx) || !Free(nz * width + cx))))
                        {
                            continue;
                        }

                        float cost = best[current] + (diagonal ? Sqrt2 : 1f);
                        if (cost < best[next])
                        {
                            best[next] = cost;
                            parent[next] = current;
                            open.Add((cost + Heuristic(next, goalCell, width), next));
                        }
                    }
                }
            }

            if (!found)
            {
                waypoints.Add(goal);
                return false;
            }

            var path = new List<Vector2> { start };
            var cells = new List<int>();
            for (int cellIndex = goalCell; cellIndex >= 0; cellIndex = parent[cellIndex])
            {
                cells.Add(cellIndex);
            }

            for (int i = cells.Count - 2; i >= 1; i--)
            {
                path.Add(CenterOf(cells[i]));
            }

            path.Add(goal);
            Pull(path, solid, cell, waypoints);
            return true;
        }

        /// <summary>True when the straight way from <paramref name="from"/> to <paramref name="to"/> is free.</summary>
        public static bool Clear(Vector2 from, Vector2 to, Func<Vector2, bool> blocked, float cell)
        {
            float length = (to - from).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (cell * 0.5f)));
            for (int i = 0; i <= steps; i++)
            {
                if (blocked(Vector2.Lerp(from, to, (float)i / steps)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Keeps only the corners: from each point the farthest one it can see in a straight line.</summary>
        private static void Pull(List<Vector2> path, Func<Vector2, bool> solid, float cell, List<Vector2> waypoints)
        {
            int at = 0;
            while (at < path.Count - 1)
            {
                int reach = at + 1;
                for (int i = path.Count - 1; i > at + 1; i--)
                {
                    if (Clear(path[at], path[i], solid, cell))
                    {
                        reach = i;
                        break;
                    }
                }

                waypoints.Add(path[reach]);
                at = reach;
            }
        }

        private static int CellOf(Vector2 point, Vector2 low, float cell, int width, int depth)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((point.x - low.x) / cell), 0, width - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((point.y - low.y) / cell), 0, depth - 1);
            return z * width + x;
        }

        private static float Heuristic(int cell, int goal, int width)
        {
            float dx = Mathf.Abs(cell % width - goal % width);
            float dz = Mathf.Abs(cell / width - goal / width);
            return Mathf.Max(dx, dz) + (Sqrt2 - 1f) * Mathf.Min(dx, dz);
        }
    }
}
