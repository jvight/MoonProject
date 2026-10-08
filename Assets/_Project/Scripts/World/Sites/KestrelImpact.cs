using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The ground Kestrel-3 left when it came down (<see cref="KestrelSettings"/>): a shallow crater with a flat
    /// floor and a low rim, and a furrow trailing back along the fall line, deepest at the crater and fading toward
    /// its tail, with low berms and a few soft gouges. Adds to the basin floor's height (C1 everywhere), quiets the
    /// dunes around it and reports how scorched the dust is. Immutable and thread-safe.
    /// </summary>
    public sealed class KestrelImpact
    {
        private const uint GougeSalt = 0x5F356495u;

        // Gouges sit along the furrow's deeper stretch, at most this fraction of its half-width off its centre line.
        private const float GougeSpread = 0.4f;
        private const float GougeStretch = 0.8f;

        // The furrow's tail scorch keeps this share of the darkness it has at the crater.
        private const float TailScorch = 0.4f;

        // The berms rise only outside the crater, over this length past the rim crest.
        private const float BermRise = 6f;

        private readonly Vector2 _back;
        private readonly Vector2 _across;
        private readonly float _floorRadius;
        private readonly float _rimRadius;
        private readonly float _outerRadius;
        private readonly float _depth;
        private readonly float _rimHeight;
        private readonly float _wallEase;
        private readonly float _furrowEnd;
        private readonly float _headHalfWidth;
        private readonly float _tailHalfWidth;
        private readonly float _furrowDepth;
        private readonly float _bermHeight;
        private readonly float _bermHalfWidth;
        private readonly float[] _gougeArcs;
        private readonly float[] _gougeOffsets;
        private readonly float _gougeDepth;
        private readonly float _gougeHalfLength;
        private readonly float _gougeHalfWidth;
        private readonly float _scorchReach;
        private readonly float _clearMargin;
        private readonly float _calmSpan;
        private readonly float _trailReach;
        private readonly float _reach;

        /// <param name="calmSpan">Distance over which the dunes return around the impact, metres.</param>
        public KestrelImpact(KestrelSettings settings, uint seed, float calmSpan)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Center = MoonSurface.BearingToDirection(settings.Bearing) * settings.Distance;
            Fall = MoonSurface.BearingToDirection(settings.FallBearing);
            _back = -Fall;
            _across = new Vector2(_back.y, -_back.x);
            _floorRadius = settings.FloorRadius;
            _rimRadius = settings.RimRadius;
            _outerRadius = settings.RimRadius + settings.RimFlank;
            _depth = settings.Depth;
            _rimHeight = settings.RimHeight;
            _wallEase = settings.WallEase;
            _furrowEnd = settings.RimRadius + settings.FurrowLength;
            _headHalfWidth = settings.FurrowHalfWidth;
            _tailHalfWidth = settings.FurrowTailHalfWidth;
            _furrowDepth = settings.FurrowDepth;
            _bermHeight = settings.BermHeight;
            _bermHalfWidth = settings.BermHalfWidth;
            _gougeDepth = settings.GougeDepth;
            _gougeHalfLength = settings.GougeHalfLength;
            _gougeHalfWidth = settings.GougeHalfWidth;
            _scorchReach = settings.ScorchReach;
            _clearMargin = settings.ClearMargin;
            _calmSpan = calmSpan;
            TrailEnd = Center + _back * _furrowEnd;

            var random = new SeededRandom(Hashing.Mix(seed ^ GougeSalt));
            int gouges = settings.GougeCount;
            _gougeArcs = new float[gouges];
            _gougeOffsets = new float[gouges];
            float first = _rimRadius + _gougeHalfLength;
            float span = (_furrowEnd - first) * GougeStretch;
            for (int i = 0; i < gouges; i++)
            {
                _gougeArcs[i] = first + span * (i + random.Range(0.2f, 0.8f)) / gouges;
                _gougeOffsets[i] = random.Range(-GougeSpread, GougeSpread) * HalfWidthAt(_gougeArcs[i]);
            }

            _trailReach = _headHalfWidth + Mathf.Max(_bermHalfWidth, _scorchReach);
            _reach = Mathf.Max(Mathf.Max(_outerRadius, _rimRadius + _scorchReach), _trailReach) + calmSpan;
        }

        /// <summary>World XZ of the crater's centre.</summary>
        public Vector2 Center { get; }

        /// <summary>Unit XZ direction Kestrel-3 was travelling: from the furrow's tail toward the crater.</summary>
        public Vector2 Fall { get; }

        /// <summary>World XZ of the furrow's far, faint end.</summary>
        public Vector2 TrailEnd { get; }

        /// <summary>Radius of the crater's flat floor.</summary>
        public float FloorRadius => _floorRadius;

        /// <summary>Radius of the crater's rim crest.</summary>
        public float RimRadius => _rimRadius;

        /// <summary>Radius of the crater's outer flank, where it meets the ground around it.</summary>
        public float OuterRadius => _outerRadius;

        /// <summary>Half-width of the furrow where it meets the crater.</summary>
        public float FurrowHalfWidth => _headHalfWidth;

        /// <summary>
        /// True when a disc at <paramref name="centre"/> of <paramref name="radius"/> reaches the crater, the furrow
        /// or the margin kept clear around them.
        /// </summary>
        public bool Overlaps(Vector2 centre, float radius)
        {
            return TrailDistance(centre) < _trailReach + _clearMargin + radius
                || Vector2.Distance(centre, Center) < _outerRadius + _clearMargin + radius;
        }

        /// <summary>
        /// The height the impact adds at (x, z). Multiplies <paramref name="calm"/> down to quiet the dunes over the
        /// impact and returns how scorched the dust is there (0..1) in <paramref name="scorch"/>.
        /// </summary>
        public float Sample(float x, float z, ref float calm, out float scorch)
        {
            var offset = new Vector2(x - Center.x, z - Center.y);
            float trail = TrailDistance(new Vector2(x, z));
            if (trail >= _reach)
            {
                scorch = 0f;
                return 0f;
            }

            float distance = offset.magnitude;
            float along = Vector2.Dot(offset, _back);
            float side = Vector2.Dot(offset, _across);
            float height = Crater(distance);

            float halfWidth = HalfWidthAt(along);
            float fade = 1f - SmoothMath.Smootherstep(_rimRadius, _furrowEnd, along);
            float into = SmoothMath.Smootherstep(_floorRadius, _rimRadius, along);
            if (into > 0f && fade > 0f)
            {
                float trough = -_furrowDepth * SmoothMath.Bump(side / halfWidth);
                float berms = _bermHeight * SmoothMath.Smootherstep(_rimRadius, _rimRadius + BermRise, along)
                    * SmoothMath.Bump((Mathf.Abs(side) - halfWidth) / _bermHalfWidth);
                height += into * fade * (trough + berms);
            }

            for (int i = 0; i < _gougeArcs.Length; i++)
            {
                height -= _gougeDepth * SmoothMath.Bump((along - _gougeArcs[i]) / _gougeHalfLength)
                    * SmoothMath.Bump((side - _gougeOffsets[i]) / _gougeHalfWidth);
            }

            calm *= SmoothMath.Smootherstep(_outerRadius, _outerRadius + _calmSpan, distance)
                * SmoothMath.Smootherstep(_headHalfWidth + _bermHalfWidth,
                    _headHalfWidth + _bermHalfWidth + _calmSpan, trail);

            float craterScorch = 1f - SmoothMath.Smootherstep(_rimRadius, _rimRadius + _scorchReach, distance);
            float trailScorch = SmoothMath.Smootherstep(0f, _rimRadius, along)
                * (1f - (1f - TailScorch) * SmoothMath.Smootherstep(_rimRadius, _furrowEnd, along))
                * (1f - SmoothMath.Smootherstep(_furrowEnd, _furrowEnd + _scorchReach, along))
                * (1f - SmoothMath.Smootherstep(halfWidth, halfWidth + _scorchReach, Mathf.Abs(side)));
            scorch = Mathf.Max(craterScorch, trailScorch);
            return height;
        }

        private float Crater(float distance)
        {
            if (distance < _rimRadius)
            {
                return -_depth + (_depth + _rimHeight)
                    * SmoothMath.SmoothRamp(distance - _floorRadius, _rimRadius - _floorRadius, _wallEase);
            }

            return _rimHeight * (1f - SmoothMath.Smootherstep(_rimRadius, _outerRadius, distance));
        }

        private float HalfWidthAt(float along)
        {
            return _headHalfWidth
                + (_tailHalfWidth - _headHalfWidth) * SmoothMath.Smootherstep(_rimRadius, _furrowEnd, along);
        }

        /// <summary>
        /// Distance from <paramref name="point"/> to the furrow's centre line, from the crater's centre to its tail.
        /// </summary>
        private float TrailDistance(Vector2 point)
        {
            Vector2 offset = point - Center;
            float along = Mathf.Clamp(Vector2.Dot(offset, _back), 0f, _furrowEnd);
            return Vector2.Distance(point, Center + _back * along);
        }
    }
}
