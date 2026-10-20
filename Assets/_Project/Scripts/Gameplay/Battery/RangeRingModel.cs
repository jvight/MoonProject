using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring's math (docs/design/opening-v3-purpose.md "Range", plan slice 4.2), pure and EditMode-tested.
    /// A ground point is inside the ring when 07 can drive out to it and back to a charge point on what charge it has:
    /// the way out and the way back are each costed in cells, on the real ground (the battery's metres per cell and its
    /// slope multipliers, uphill dearer and downhill cheaper), straight line by straight line, and the way back runs to
    /// the cheapest charge point, so another pad or a lit relay nearer to a place pulls the ring out toward it. With no
    /// charge point at all, 07's own place is the only way home. The ring is the polygon of the furthest point on each
    /// bearing round 07 that is still inside, found going outward until the first that is not.
    /// </summary>
    public sealed class RangeRingModel
    {
        private readonly BatteryTuning _battery;
        private readonly RangeRingSettings _settings;
        private readonly ITerrainQuery _terrain;

        public RangeRingModel(BatteryTuning battery, RangeRingSettings settings, ITerrainQuery terrain)
        {
            _battery = battery != null ? battery : throw new ArgumentNullException(nameof(battery));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        }

        /// <summary>Cells it costs 07 to drive straight from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public float PathCost(Vector3 from, Vector3 to, float stepMetres)
        {
            Vector3 flat = to - from;
            flat.y = 0f;
            float distance = flat.magnitude;
            if (distance < 1e-3f)
            {
                return 0f;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.5f, stepMetres)));
            float length = distance / steps;
            Vector3 direction = flat / distance;
            float cost = 0f;
            float height = _terrain.SampleHeight(from.x, from.z);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 at = from + direction * (length * i);
                float next = _terrain.SampleHeight(at.x, at.z);
                cost += SegmentCost(height, next, length);
                height = next;
            }

            return cost;
        }

        /// <summary>
        /// Works the ring out into <paramref name="polygon"/>, one point per bearing, starting due +Z and turning toward
        /// +X.
        /// </summary>
        /// <param name="origin">Where 07 is.</param>
        /// <param name="charge">07's charge now (cells).</param>
        /// <param name="chargePoints">Where 07 can recharge (built pads; later lit relays and hex contacts).</param>
        public void Compute(Vector3 origin, float charge, IReadOnlyList<Vector3> chargePoints, Vector3[] polygon)
        {
            if (chargePoints == null)
            {
                throw new ArgumentNullException(nameof(chargePoints));
            }

            if (polygon == null || polygon.Length < 3)
            {
                throw new ArgumentException("The ring needs at least three points.", nameof(polygon));
            }

            for (int k = 0; k < polygon.Length; k++)
            {
                float angle = 2f * Mathf.PI * k / polygon.Length;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float reach = Reach(origin, direction, charge, chargePoints);
                Vector3 edge = origin + direction * reach;
                edge.y = _terrain.SampleHeight(edge.x, edge.z);
                polygon[k] = edge;
            }
        }

        /// <summary>The furthest distance along <paramref name="direction"/> still inside, 0 when none is.</summary>
        private float Reach(Vector3 origin, Vector3 direction, float charge, IReadOnlyList<Vector3> chargePoints)
        {
            float step = _settings.StepMetres;
            float outCost = 0f;
            float height = _terrain.SampleHeight(origin.x, origin.z);
            float last = 0f;
            for (float distance = step; distance <= _settings.MaxRangeMetres; distance += step)
            {
                Vector3 at = origin + direction * distance;
                float next = _terrain.SampleHeight(at.x, at.z);
                outCost += SegmentCost(height, next, step);
                height = next;
                at.y = next;
                float back = WayBack(at, origin, chargePoints);
                if (outCost + back > charge)
                {
                    break;
                }

                last = distance;
            }

            return last;
        }

        /// <summary>Cells to the cheapest way back to a charge point (07's own place when there is none).</summary>
        private float WayBack(Vector3 from, Vector3 origin, IReadOnlyList<Vector3> chargePoints)
        {
            if (chargePoints.Count == 0)
            {
                return PathCost(from, origin, _settings.PathStepMetres);
            }

            float best = float.MaxValue;
            for (int i = 0; i < chargePoints.Count; i++)
            {
                best = Mathf.Min(best, PathCost(from, chargePoints[i], _settings.PathStepMetres));
            }

            return best;
        }

        private float SegmentCost(float heightFrom, float heightTo, float length)
        {
            float slope = Mathf.Atan2(heightTo - heightFrom, length) * Mathf.Rad2Deg;
            return length / _battery.MetresPerCell * _battery.SlopeMultiplier(slope);
        }
    }
}
