using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The shaped ground of <see cref="SiteSettings"/>: flat footprints for the salvage sites on the basin floor and
    /// the low mound under relay.0. Each plateau's height is read once from the natural ground at its centre (raised
    /// by the mound's rise), so the shapes sit level with their surroundings. Immutable and thread-safe.
    /// </summary>
    public sealed class GroundShaping
    {
        private readonly GroundShape[] _shapes;
        private readonly float[] _reachSq;

        /// <param name="naturalHeight">The ground's height before shaping.</param>
        public GroundShaping(SiteSettings settings, Func<float, float, float> naturalHeight)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (naturalHeight == null)
            {
                throw new ArgumentNullException(nameof(naturalHeight));
            }

            float blend = settings.FootprintBlend;
            Depot = Flat(naturalHeight, settings.DepotCenter, settings.DepotRadius, blend);
            Drill = Flat(naturalHeight, settings.DrillCenter, settings.DrillRadius, blend);
            Garage = Flat(naturalHeight, settings.GarageCenter, settings.GarageRadius, blend);
            Vector2 mound = settings.MoundCenter;
            RelayMound = new GroundShape(mound, settings.MoundRadius, settings.MoundRun,
                naturalHeight(mound.x, mound.y) + settings.MoundRise);

            _shapes = new[] { Depot, Drill, Garage, RelayMound };
            _reachSq = new float[_shapes.Length];
            for (int i = 0; i < _shapes.Length; i++)
            {
                float reach = _shapes[i].Radius + _shapes[i].Run;
                _reachSq[i] = reach * reach;
            }
        }

        public GroundShape Depot { get; }

        public GroundShape Drill { get; }

        public GroundShape Garage { get; }

        /// <summary>The low mound relay.0 stands on.</summary>
        public GroundShape RelayMound { get; }

        /// <summary>The ground at (x, z) after shaping, given its natural <paramref name="height"/> there.</summary>
        public float Apply(float x, float z, float height)
        {
            for (int i = 0; i < _shapes.Length; i++)
            {
                GroundShape shape = _shapes[i];
                float dx = x - shape.Center.x;
                float dz = z - shape.Center.y;
                float distanceSq = dx * dx + dz * dz;
                if (distanceSq < _reachSq[i])
                {
                    height = Mathf.Lerp(height, shape.Plateau, shape.Weight(Mathf.Sqrt(distanceSq)));
                }
            }

            return height;
        }

        private static GroundShape Flat(Func<float, float, float> naturalHeight, Vector2 center, float radius,
            float blend)
        {
            return new GroundShape(center, radius, blend, naturalHeight(center.x, center.y));
        }
    }
}
