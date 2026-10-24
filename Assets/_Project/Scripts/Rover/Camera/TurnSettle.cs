using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Whether 07 has stopped turning (pure): its heading's turn rate must stay under a threshold for a moment. The
    /// next-action nudge waits for it, so a turn 07 makes itself (the in-place face-the-array turn on the pad) is not
    /// stacked with a camera pan, and re-checks where the array is once the turn has settled.
    /// </summary>
    public sealed class TurnSettle
    {
        private float _lastYaw;
        private float _calmSeconds;
        private bool _started;

        /// <summary>True once the heading has been calm for the settle time (and from the start).</summary>
        public bool IsSettled { get; private set; } = true;

        /// <param name="yawDegrees">07's heading now.</param>
        /// <param name="deltaTime">Seconds since the last step.</param>
        /// <param name="maxRate">Degrees per second under which the heading counts as still.</param>
        /// <param name="settleSeconds">How long it must stay still.</param>
        public void Step(float yawDegrees, float deltaTime, float maxRate, float settleSeconds)
        {
            if (!_started)
            {
                _started = true;
                _lastYaw = yawDegrees;
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            float rate = Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yawDegrees)) / deltaTime;
            _lastYaw = yawDegrees;
            _calmSeconds = rate <= maxRate ? _calmSeconds + deltaTime : 0f;
            IsSettled = _calmSeconds >= settleSeconds;
        }
    }
}
