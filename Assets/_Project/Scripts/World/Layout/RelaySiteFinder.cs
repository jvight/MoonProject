using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Finds a relay mast's site on the ground as it is: within a sector around the base, the point that stands
    /// highest above its surroundings while its whole pad is drivable and gently flat, so the mast reads as a high
    /// point from the base. Deterministic (a fixed lattice, ties keep the first found) and allocation-light: the rise
    /// is scored everywhere, the costlier pad check only runs on the best few.
    /// </summary>
    public static class RelaySiteFinder
    {
        private const float BearingStep = 1f;
        private const float DistanceStep = 4f;
        private const float SurroundRadius = 30f;
        private const int SurroundSamples = 12;
        private const int Shortlist = 16;
        private const int PadDirections = 12;
        private const int PadRings = 3;

        /// <summary>Steepest ground (degrees) allowed anywhere on a pad.</summary>
        public const float MaxPadSlope = 8f;

        /// <returns>The site (XZ). Throws when the sector holds no flat, drivable pad.</returns>
        public static Vector2 Find(MoonSurface surface, float bearing, float bearingSpread, float distance,
            float distanceSpread, float padRadius)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            var best = new Vector2[Shortlist];
            var bestRise = new float[Shortlist];
            for (int i = 0; i < Shortlist; i++)
            {
                bestRise[i] = float.MinValue;
            }

            for (float b = bearing - bearingSpread; b <= bearing + bearingSpread; b += BearingStep)
            {
                Vector2 along = MoonSurface.BearingToDirection(b);
                for (float d = distance - distanceSpread; d <= distance + distanceSpread; d += DistanceStep)
                {
                    Vector2 p = along * d;
                    if (!surface.IsDrivable(p.x, p.y))
                    {
                        continue;
                    }

                    Insert(best, bestRise, p, Rise(surface, p));
                }
            }

            for (int i = 0; i < Shortlist; i++)
            {
                if (bestRise[i] > float.MinValue && PadIsGood(surface, best[i], padRadius))
                {
                    return best[i];
                }
            }

            throw new InvalidOperationException(
                $"No flat, drivable relay pad of radius {padRadius} m around bearing {bearing}, distance {distance}.");
        }

        /// <summary>Height of <paramref name="p"/> above the mean of a ring around it.</summary>
        private static float Rise(MoonSurface surface, Vector2 p)
        {
            float ring = 0f;
            for (int k = 0; k < SurroundSamples; k++)
            {
                float angle = k * Mathf.PI * 2f / SurroundSamples;
                Vector2 q = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SurroundRadius;
                ring += surface.SampleHeight(q.x, q.y);
            }

            return surface.SampleHeight(p.x, p.y) - ring / SurroundSamples;
        }

        private static bool PadIsGood(MoonSurface surface, Vector2 centre, float padRadius)
        {
            float minNormalY = Mathf.Cos(MaxPadSlope * Mathf.Deg2Rad);
            for (int ring = 0; ring <= PadRings; ring++)
            {
                float radius = padRadius * ring / PadRings;
                for (int k = 0; k < PadDirections; k++)
                {
                    float angle = k * Mathf.PI * 2f / PadDirections;
                    Vector2 q = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (!surface.IsDrivable(q.x, q.y) || surface.SampleNormal(q.x, q.y).y < minNormalY)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Keeps the shortlist sorted, highest rise first.</summary>
        private static void Insert(Vector2[] points, float[] rises, Vector2 point, float rise)
        {
            int slot = points.Length;
            while (slot > 0 && rises[slot - 1] < rise)
            {
                slot--;
            }

            if (slot >= points.Length)
            {
                return;
            }

            for (int i = points.Length - 1; i > slot; i--)
            {
                points[i] = points[i - 1];
                rises[i] = rises[i - 1];
            }

            points[slot] = point;
            rises[slot] = rise;
        }
    }
}
