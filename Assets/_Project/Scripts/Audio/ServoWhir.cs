using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One of 07's little servos: how much it whirs from how fast it moves (silent below a dead rate so idle sway
    /// stays quiet, full at the full rate), spinning up quickly and down a touch slower. Allocation-free.
    /// </summary>
    public sealed class ServoWhir
    {
        private readonly EasedValue _rate = new EasedValue(0f);

        /// <summary>0 still .. 1 at full speed.</summary>
        public float Amount { get; private set; }

        /// <summary>Feeds the servo's speed this frame (any unit, matching the dead and full rates).</summary>
        public float Step(float rate, float deltaTime, float deadRate, float fullRate, float attack, float release)
        {
            float eased = _rate.Step(Mathf.Max(0f, rate), Mathf.Max(0f, deltaTime), attack, release);
            float t = fullRate > deadRate ? Mathf.Clamp01((eased - deadRate) / (fullRate - deadRate)) : 0f;
            Amount = t * t * (3f - 2f * t);
            return Amount;
        }

        /// <summary>
        /// Degrees per second a direction turned between two frames <paramref name="deltaTime"/> apart. Uses atan2 of
        /// the cross and dot products, which stays exact for the tiny per-frame angles of a high frame rate (an
        /// acos-based angle rounds those to zero).
        /// </summary>
        public static float AngularRate(Vector3 previous, Vector3 current, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            float angle = Mathf.Atan2(Vector3.Cross(previous, current).magnitude, Vector3.Dot(previous, current));
            return angle * Mathf.Rad2Deg / deltaTime;
        }
    }
}
