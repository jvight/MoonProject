using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Notices when 07 is trying to drive but getting nowhere. While the player is trying (throttle held), an anchor
    /// marks where the attempt started; moving more than ProgressRadius away (on the ground plane) moves the anchor and
    /// resets the clock. If the clock reaches StuckTime, <see cref="Step"/> reports it once and re-arms. Letting go of
    /// the throttle resets everything, so parking, holding still or pausing to look never counts.
    /// </summary>
    public sealed class StuckDetector
    {
        private readonly RecoverySettings _settings;
        private Vector3 _anchor;
        private bool _hasAnchor;

        public StuckDetector(RecoverySettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Seconds of trying without progress so far.</summary>
        public float StuckFor { get; private set; }

        public void Reset()
        {
            _hasAnchor = false;
            StuckFor = 0f;
        }

        /// <summary>Advances one physics step; true when 07 has just been found stuck.</summary>
        public bool Step(float throttle, Vector3 position, float deltaTime)
        {
            if (Mathf.Abs(throttle) < _settings.TryingInput)
            {
                Reset();
                return false;
            }

            Vector3 moved = position - _anchor;
            moved.y = 0f;
            if (!_hasAnchor || moved.sqrMagnitude > _settings.ProgressRadius * _settings.ProgressRadius)
            {
                _anchor = position;
                _hasAnchor = true;
                StuckFor = 0f;
                return false;
            }

            StuckFor += deltaTime;
            if (StuckFor < _settings.StuckTime)
            {
                return false;
            }

            Reset();
            return true;
        }
    }
}
