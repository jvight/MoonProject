using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where the Peak road's rockfall bared the Peak's flank (<see cref="RockfallScarSettings"/>): a weight, 1 on the
    /// fresh regolith up the road's bearing above its foot, easing to 0 across its wandering edges. Immutable and
    /// thread-safe.
    /// </summary>
    public sealed class RockfallScar
    {
        private const uint WanderSalt = 0x5BD1E995u;

        private readonly RockfallScarSettings _settings;
        private readonly Vector2 _foot;
        private readonly Vector2 _direction;
        private readonly GradientNoise _wander;

        public RockfallScar(RockfallScarSettings settings, Vector2 roadFoot, Vector2 roadDirection, uint seed)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _foot = roadFoot;
            _direction = roadDirection.normalized;
            _wander = new GradientNoise(Hashing.Mix(seed ^ WanderSalt));
        }

        /// <summary>How fresh the ground at (x, z) is: 1 on the scar, 0 off it.</summary>
        public float Weight(float x, float z)
        {
            RockfallScarSettings s = _settings;
            float dx = x - _foot.x;
            float dz = z - _foot.y;
            float along = dx * _direction.x + dz * _direction.y;
            float end = s.Start + s.Length;
            if (along <= s.Start - s.Edge || along >= end + s.Edge)
            {
                return 0f;
            }

            // Sampled across the scar too, so each edge wanders on its own and the two meet smoothly on the line.
            float side = dx * _direction.y - dz * _direction.x;
            float lateral = Mathf.Abs(side);
            float t = Mathf.Clamp01((along - s.Start) / s.Length);
            float halfWidth = Mathf.Lerp(s.FanHalfWidth, s.TopHalfWidth, t)
                * (1f + s.Wander * _wander.Sample(along / s.WanderWavelength, side / s.WanderWavelength));
            if (lateral >= halfWidth + s.Edge)
            {
                return 0f;
            }

            return (1f - SmoothMath.Smootherstep(halfWidth - s.Edge, halfWidth + s.Edge, lateral))
                * SmoothMath.Smootherstep(s.Start - s.Edge, s.Start + s.Edge, along)
                * (1f - SmoothMath.Smootherstep(end - s.Edge, end + s.Edge, along));
        }
    }
}
