using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The basin's small craters (M3-18 V4): crisp shallow dents with a raised rim, at most one per cell of a jittered
    /// grid, gathered in patchy fields on the open floor. Built once the world anchors are known and kept off the base
    /// pad, every anchor's ground and Whispering Canyon, so no anchor moves and every pad stays flat. The profile is C1
    /// (smooth to drive over); flat shading makes it read crisp. A sample looks at its own cell and the eight around
    /// it. Immutable and thread-safe.
    /// </summary>
    public sealed class CraterField
    {
        private const uint PlaceSalt = 0x5BD1E995u;
        private const uint FieldSalt = 0x27D4EB2Fu;

        // A crater's centre stays this far inside its cell (as a fraction), so its reach is always within one cell.
        private const float CellMargin = 0.15f;

        private readonly CraterFieldSettings _settings;
        private readonly float _invCell;
        private readonly int _cells;
        private readonly int _offset;
        private readonly float[] _x;
        private readonly float[] _z;
        private readonly float[] _radius;

        /// <param name="settings">The field's tuning.</param>
        /// <param name="seed">World seed.</param>
        /// <param name="floorRadius">Craters stay inside this radius from the base (the drivable floor).</param>
        /// <param name="padRadius">The base pad's radius: craters stay clear of it.</param>
        /// <param name="anchors">World anchors whose ground stays untouched.</param>
        /// <param name="keepOut">Area left alone (Whispering Canyon's bounds, XZ).</param>
        public CraterField(CraterFieldSettings settings, int seed, float floorRadius, float padRadius,
            IWorldAnchors anchors, Rect keepOut)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            string problem = settings.Validate();
            if (problem != null)
            {
                throw new ArgumentException(problem, nameof(settings));
            }

            float cell = settings.CellSize;
            _invCell = 1f / cell;
            _offset = Mathf.CeilToInt(floorRadius * _invCell) + 1;
            _cells = _offset * 2;
            _x = new float[_cells * _cells];
            _z = new float[_cells * _cells];
            _radius = new float[_cells * _cells];
            var fieldNoise = new GradientNoise(Hashing.Mix((uint)seed ^ FieldSalt));
            uint placeSeed = Hashing.Mix((uint)seed ^ PlaceSalt);
            float invField = 1f / settings.FieldWavelength;
            float outerShare = 1f + settings.RimWidth;
            int count = 0;
            for (int j = 0; j < _cells; j++)
            {
                for (int i = 0; i < _cells; i++)
                {
                    int ci = i - _offset;
                    int cj = j - _offset;
                    float cx = (ci + 0.5f) * cell;
                    float cz = (cj + 0.5f) * cell;
                    float field = SmoothMath.Smootherstep(settings.FieldThreshold - 0.2f,
                        settings.FieldThreshold + 0.2f, fieldNoise.Fractal(cx * invField, cz * invField, 2, 2f, 0.5f));
                    if (Hashing.ToUnit(Hashing.Hash(ci, cj, 0, placeSeed)) >= settings.Density * field)
                    {
                        continue;
                    }

                    float x = (ci + Jitter(Hashing.Hash(ci, cj, 1, placeSeed))) * cell;
                    float z = (cj + Jitter(Hashing.Hash(ci, cj, 2, placeSeed))) * cell;
                    float size = Mathf.Pow(Hashing.ToUnit(Hashing.Hash(ci, cj, 3, placeSeed)), settings.SizePower);
                    float radius = Mathf.Lerp(settings.Radius.x, settings.Radius.y, size);
                    float reach = radius * outerShare + settings.Clearance;
                    if (!IsOpenFloor(x, z, reach, floorRadius, padRadius, anchors, keepOut))
                    {
                        continue;
                    }

                    int index = j * _cells + i;
                    _x[index] = x;
                    _z[index] = z;
                    _radius[index] = radius;
                    count++;
                }
            }

            Count = count;
        }

        /// <summary>How many small craters the field holds.</summary>
        public int Count { get; }

        /// <summary>
        /// The craters' height change at (x, z), and their paint weights there: <paramref name="bowl"/> and
        /// <paramref name="rim"/> rise to the strongest crater's (never lowered).
        /// </summary>
        public float Sample(float x, float z, ref float bowl, ref float rim)
        {
            int ci = SmoothMath.FastFloor(x * _invCell) + _offset;
            int cj = SmoothMath.FastFloor(z * _invCell) + _offset;
            float height = 0f;
            for (int j = cj - 1; j <= cj + 1; j++)
            {
                if (j < 0 || j >= _cells)
                {
                    continue;
                }

                for (int i = ci - 1; i <= ci + 1; i++)
                {
                    if (i < 0 || i >= _cells)
                    {
                        continue;
                    }

                    int index = j * _cells + i;
                    float radius = _radius[index];
                    if (radius <= 0f)
                    {
                        continue;
                    }

                    float dx = x - _x[index];
                    float dz = z - _z[index];
                    float t = (float)Math.Sqrt(dx * dx + dz * dz) / radius;
                    if (t >= 1f + _settings.RimWidth)
                    {
                        continue;
                    }

                    float lip = SmoothMath.Bump((t - 1f) / _settings.RimWidth);
                    height += radius * _settings.RimHeight * lip;
                    rim = Mathf.Max(rim, lip);
                    if (t < 1f)
                    {
                        // A cosine bowl spreads its curvature evenly: the smallest craters stay smooth to drive.
                        height -= radius * _settings.Depth * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * t));
                        bowl = Mathf.Max(bowl, _settings.BowlShade * (1f - t));
                    }
                }
            }

            return height;
        }

        private static float Jitter(uint hash)
        {
            return CellMargin + (1f - 2f * CellMargin) * Hashing.ToUnit(hash);
        }

        private static bool IsOpenFloor(float x, float z, float reach, float floorRadius, float padRadius,
            IWorldAnchors anchors, Rect keepOut)
        {
            float fromBase = Mathf.Sqrt(x * x + z * z);
            if (fromBase - reach < padRadius || fromBase + reach > floorRadius)
            {
                return false;
            }

            var area = Rect.MinMaxRect(x - reach, z - reach, x + reach, z + reach);
            if (area.Overlaps(keepOut))
            {
                return false;
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                float dx = x - anchor.Position.x;
                float dz = z - anchor.Position.z;
                float apart = reach + anchor.Radius;
                if (dx * dx + dz * dz < apart * apart)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
