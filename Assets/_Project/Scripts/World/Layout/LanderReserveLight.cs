using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How lit the lander's own lights are (opening v3 R4): fully while it holds a reserve, fading to dark over
    /// <see cref="LanderBeaconSettings.ReserveDarkSeconds"/> once the reserve is spent, and back if it were ever
    /// charged again. A restored game takes its state at once, with no fade. Plain logic, allocation-free.
    /// </summary>
    public sealed class LanderReserveLight
    {
        private float _target = 1f;

        /// <summary>0 dark .. 1 fully lit.</summary>
        public float Level { get; private set; } = 1f;

        /// <summary>
        /// The lander's reserve changed to <paramref name="reserve01"/> (from a restored game when
        /// <paramref name="restored"/>).
        /// </summary>
        public void OnReserve(float reserve01, bool restored)
        {
            _target = reserve01 > 0f ? 1f : 0f;
            if (restored)
            {
                Level = _target;
            }
        }

        public void Step(float deltaTime, float fadeSeconds)
        {
            Level = Mathf.MoveTowards(Level, _target, deltaTime / Mathf.Max(fadeSeconds, 1e-3f));
        }
    }
}
