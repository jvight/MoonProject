using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The eased turn 07 makes on the pad when the dock lets go facing a new way (the first undock turns it toward the
    /// ruins): its heading glides from where it is to the release heading with a smoothstep, in place. Pure so the
    /// shape is testable.
    /// </summary>
    public sealed class DockReleaseTurn
    {
        private float _from;
        private float _delta;
        private float _elapsed;

        /// <summary>True while the heading is still turning.</summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Starts the turn from <paramref name="fromYaw"/> to <paramref name="toYaw"/> (degrees). False, and nothing to
        /// do, when they differ by no more than <paramref name="thresholdDegrees"/>.
        /// </summary>
        public bool Begin(float fromYaw, float toYaw, float thresholdDegrees)
        {
            float delta = Mathf.DeltaAngle(fromYaw, toYaw);
            Active = Mathf.Abs(delta) > thresholdDegrees;
            _from = fromYaw;
            _delta = delta;
            _elapsed = 0f;
            return Active;
        }

        /// <summary>Stops the turn where it is.</summary>
        public void Cancel()
        {
            Active = false;
        }

        /// <summary>The heading (degrees, 0..360) after <paramref name="deltaTime"/> more seconds of a turn lasting <paramref name="seconds"/>.</summary>
        public float Step(float deltaTime, float seconds)
        {
            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(seconds, 1e-3f));
            Active = t < 1f;
            return Mathf.Repeat(_from + _delta * Smoothing.SmoothStep(0f, 1f, t), 360f);
        }
    }
}
