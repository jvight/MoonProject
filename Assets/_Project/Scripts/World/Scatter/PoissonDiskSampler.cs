using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Bridson's Poisson-disk sampling inside a disc: points no closer than a minimum distance, filling the disc
    /// evenly without grid artefacts. Deterministic for a seed (single-threaded, seeded PCG32).
    /// </summary>
    public static class PoissonDiskSampler
    {
        private const int CandidatesPerPoint = 30;
        private const float TwoPi = 6.28318531f;

        /// <summary>Samples the disc of <paramref name="radius"/> around the origin.</summary>
        public static List<Vector2> SampleDisc(float radius, float minDistance, uint seed)
        {
            if (!(radius > 0f) || !(minDistance > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(minDistance), "Radius and distance must be positive.");
            }

            float cell = minDistance / Mathf.Sqrt(2f);
            int size = Mathf.CeilToInt(radius * 2f / cell) + 1;
            var grid = new int[size * size];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = -1;
            }

            var points = new List<Vector2>();
            var active = new List<int>();
            var random = new SeededRandom(seed);
            float radiusSq = radius * radius;
            float minSq = minDistance * minDistance;

            float startAngle = random.Range(0f, TwoPi);
            float startDistance = radius * Mathf.Sqrt(random.NextFloat());
            Add(new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * startDistance);

            while (active.Count > 0)
            {
                int slot = random.Range(0, active.Count);
                Vector2 origin = points[active[slot]];
                bool placed = false;
                for (int attempt = 0; attempt < CandidatesPerPoint; attempt++)
                {
                    float angle = random.Range(0f, TwoPi);
                    float distance = random.Range(minDistance, minDistance * 2f);
                    Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (candidate.sqrMagnitude <= radiusSq && Fits(candidate))
                    {
                        Add(candidate);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    active[slot] = active[active.Count - 1];
                    active.RemoveAt(active.Count - 1);
                }
            }

            return points;

            void Add(Vector2 point)
            {
                grid[CellIndex(point)] = points.Count;
                active.Add(points.Count);
                points.Add(point);
            }

            bool Fits(Vector2 candidate)
            {
                int cx = Mathf.FloorToInt((candidate.x + radius) / cell);
                int cy = Mathf.FloorToInt((candidate.y + radius) / cell);
                for (int y = Mathf.Max(0, cy - 2); y <= Mathf.Min(size - 1, cy + 2); y++)
                {
                    for (int x = Mathf.Max(0, cx - 2); x <= Mathf.Min(size - 1, cx + 2); x++)
                    {
                        int neighbour = grid[y * size + x];
                        if (neighbour >= 0 && (points[neighbour] - candidate).sqrMagnitude < minSq)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            int CellIndex(Vector2 point)
            {
                int x = Mathf.FloorToInt((point.x + radius) / cell);
                int y = Mathf.FloorToInt((point.y + radius) / cell);
                return y * size + x;
            }
        }
    }
}
