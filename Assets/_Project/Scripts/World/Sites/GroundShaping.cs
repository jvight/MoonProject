using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The shaped ground of <see cref="SiteSettings"/>: flat footprints on the basin floor under the opening's
    /// places. Each plateau's height is read once from the natural ground at its centre, so the shapes sit level with
    /// their surroundings. Immutable and thread-safe.
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
            Ruins = Flat(naturalHeight, settings.RuinsCenter, settings.RuinsRadius, settings.RuinsBlend);
            Lander = Flat(naturalHeight, settings.LanderCenter, settings.LanderRadius, blend);
            Apron = Flat(naturalHeight, settings.ApronCenter, settings.ApronRadius, blend);
            MidwayCenter = settings.MidwayCenter;
            MidwayRadius = settings.MidwayRadius;
            WakePost = settings.WakePost;
            Blast = new BlastZone(Lander.Center, settings.BlastZone);

            _shapes = new[] { Ruins, Lander, Apron };
            _reachSq = new float[_shapes.Length];
            for (int i = 0; i < _shapes.Length; i++)
            {
                float reach = _shapes[i].Radius + _shapes[i].Run;
                _reachSq[i] = reach * reach;
            }
        }

        /// <summary>The flat footprint under the Lumen station ruins cluster.</summary>
        public GroundShape Ruins { get; }

        /// <summary>Opening v2: the flat footprint the landed cargo lander stands on.</summary>
        public GroundShape Lander { get; }

        /// <summary>Opening v2: the build apron beside the lander, flat for the player's structures.</summary>
        public GroundShape Apron { get; }

        /// <summary>
        /// Opening v2: the centre of the first drive's point of interest in the Kestrel furrow (not shaped: its debris
        /// lies in the furrow as it is).
        /// </summary>
        public Vector2 MidwayCenter { get; }

        /// <summary>Opening v2: the midway point's radius, metres.</summary>
        public float MidwayRadius { get; }

        /// <summary>Opening v2: where 07's dead charging post stands on the old pad (already flat, not shaped).</summary>
        public Vector2 WakePost { get; }

        /// <summary>Opening v2: the lander's blast-swept zone round its footprint.</summary>
        public BlastZone Blast { get; }

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
