using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The rille notch cut through the rim (<see cref="RilleSettings"/>): a straight pass along its bearing whose floor
    /// stays low through the wall, steps up a steep lava lip past the crest and rises onto the outer plain, with steep
    /// walls either side, joined to the natural rim by a smooth minimum so the surface stays C1; and the dark mare's
    /// weight on the plain it looks onto. Immutable and thread-safe.
    /// </summary>
    public sealed class RilleNotch
    {
        // How high the cut lifts out of the way beyond its ends: far above any rim, so the minimum leaves the world.
        private const float FarAbove = 400f;

        // How far round the lava lip the terrain needs fine mesh cells, metres: the lip's sheer run stays sheer.
        private const float LipMargin = 8f;

        // Rounding where the lip's top meets the rise onto the mare, metres (keeps the floor C1).
        private const float LipBlend = 2f;

        private readonly RilleSettings _settings;
        private readonly Vector2 _direction;
        private readonly float _crestRadius;
        private readonly float _outerPlainHeight;
        private readonly float _wallTan;
        private readonly float _cosMare;

        public RilleNotch(RilleSettings settings, float crestRadius, float outerPlainHeight)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _direction = MoonSurface.BearingToDirection(settings.Bearing);
            _crestRadius = crestRadius;
            _outerPlainHeight = outerPlainHeight;
            _wallTan = Mathf.Tan(settings.WallSlope * Mathf.Deg2Rad);
            _cosMare = Mathf.Cos(settings.MareHalfAngle * Mathf.Deg2Rad);
        }

        /// <summary>Unit XZ direction of the pass from the base.</summary>
        public Vector2 Direction => _direction;

        /// <summary>Distance from the base where the pass begins to cut into the rim, metres.</summary>
        public float MouthDistance => _settings.Start;

        /// <summary>Floor height of the pass <paramref name="along"/> metres out from the base on its bearing.</summary>
        public float Floor(float along)
        {
            RilleSettings s = _settings;
            float rise = SmoothMath.Smootherstep(_crestRadius + s.RiseStart, _crestRadius + s.RiseEnd, along);
            float lipFoot = _crestRadius + s.LipDistance;
            float lip = s.LipHeight * SmoothMath.Smootherstep(lipFoot, lipFoot + s.LipRun, along);
            float lift = FarAbove * (1f - SmoothMath.Smootherstep(s.Start, s.Start + s.StartBlend, along)
                + SmoothMath.Smootherstep(s.End, s.End + s.EndBlend, along));
            return SmoothMath.SmoothMax(s.PassHeight + lip, s.PassHeight + (_outerPlainHeight - s.PassHeight) * rise,
                LipBlend) + lift;
        }

        /// <summary>True when the pass's lava lip may cross <paramref name="area"/> (it needs fine mesh cells).</summary>
        public bool Touches(Rect area)
        {
            RilleSettings s = _settings;
            float foot = _crestRadius + s.LipDistance;
            float reach = s.HalfWidth + s.WallEase + s.LipHeight / _wallTan + LipMargin;
            for (float along = foot - LipMargin; along <= foot + s.LipRun + LipMargin; along += LipMargin)
            {
                Vector2 p = _direction * along;
                float dx = Mathf.Max(area.xMin - p.x, 0f, p.x - area.xMax);
                float dz = Mathf.Max(area.yMin - p.y, 0f, p.y - area.yMax);
                if (dx * dx + dz * dz <= reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary><paramref name="height"/> at (x, z) with the pass cut into it.</summary>
        public float Apply(float x, float z, float height)
        {
            float along = x * _direction.x + z * _direction.y;
            if (along <= _settings.Start || along >= _settings.End + _settings.EndBlend)
            {
                return height;
            }

            float lateral = Mathf.Abs(x * _direction.y - z * _direction.x);
            float cut = Floor(along) + _wallTan * SoftPlus(lateral - _settings.HalfWidth, _settings.WallEase);
            return SmoothMath.SmoothMin(height, cut, _settings.Rounding);
        }

        /// <summary>0 on the highlands, easing to 1 on the dark mare beyond the crest round the pass's bearing.</summary>
        public float MareWeight(float x, float z)
        {
            // Inside the sector and past the mare's start, the distance along the bearing is at least this: most of
            // the world fails it with a dot product and no square root.
            RilleSettings s = _settings;
            float start = _crestRadius + s.MareStart;
            float along = x * _direction.x + z * _direction.y;
            if (along <= start * _cosMare)
            {
                return 0f;
            }

            float r = Mathf.Sqrt(x * x + z * z);
            float cosine = along / r;
            float edge = 1f - (1f - _cosMare) * (1f - s.MareEdge);
            float sector = SmoothMath.Smootherstep(_cosMare, edge, cosine);
            return sector * SmoothMath.Smootherstep(start, start + s.MareBlend, r);
        }

        // 0 below -ease, x above +ease, a quadratic blend between: a C1 hinge where the floor turns into a wall.
        private static float SoftPlus(float x, float ease)
        {
            if (x <= -ease)
            {
                return 0f;
            }

            if (x >= ease)
            {
                return x;
            }

            float t = x + ease;
            return t * t / (4f * ease);
        }
    }
}
