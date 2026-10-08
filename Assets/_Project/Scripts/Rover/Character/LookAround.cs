using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's "where am I" look around after a radio-hop lands: after a short delay the head turns left, then right, then
    /// settles back ahead, lifting a little as it scans the horizon. One smooth wave (no stops, no snaps); the gaze
    /// springs in <see cref="RoverBodyLanguage"/> soften it further.
    /// </summary>
    public sealed class LookAround
    {
        private readonly RoverCharacterTuning _tuning;
        private float _elapsed = -1f;

        public LookAround(RoverCharacterTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True from <see cref="Start"/> until the look around has settled (the delay included).</summary>
        public bool IsActive => _elapsed >= 0f;

        /// <summary>Head yaw (deg, + = right) and extra pitch (deg, + = up) to aim at right now.</summary>
        public Vector2 Aim { get; private set; }

        /// <summary>Starts (or restarts) the look around.</summary>
        public void Start()
        {
            _elapsed = 0f;
            Aim = Vector2.zero;
        }

        public void Step(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return;
            }

            _elapsed += deltaTime;
            float progress = (_elapsed - _tuning.LookAroundDelay) / _tuning.LookAroundSeconds;
            if (progress >= 1f)
            {
                _elapsed = -1f;
                Aim = Vector2.zero;
                return;
            }

            Aim = Offset(_tuning, progress);
        }

        /// <summary>
        /// The look at <paramref name="progress"/> (0..1): yaw follows one full wave, left first, lingering a little at
        /// each side; the lift swells and fades once. Both are 0 at the start and the end.
        /// </summary>
        public static Vector2 Offset(RoverCharacterTuning tuning, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float wave = Mathf.Sin(2f * Mathf.PI * t);
            float lingering = wave * (2f - Mathf.Abs(wave));
            float lift = Mathf.Sin(Mathf.PI * t);
            return new Vector2(-tuning.LookAroundYaw * lingering, tuning.LookAroundLift * lift * lift);
        }
    }
}
