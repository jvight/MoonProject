using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// "Stillness is a reward" (feel pillar 6): 07 is still while grounded, slower than the still speed and without
    /// drive input. <see cref="StillSeconds"/> counts how long; <see cref="Amount"/> eases 0 -> 1 once the stillness
    /// has lasted the still delay (over ~6 s in all) and falls back quickly the moment 07 moves. This is the shared
    /// definition the camera's wide shot should use too (proposed as a Core contract owned by Rover). Allocation-free.
    /// </summary>
    public sealed class StillnessTracker
    {
        private readonly SoundscapeTuning _tuning;
        private readonly EasedValue _amount = new EasedValue(0f);

        public StillnessTracker(SoundscapeTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Seconds 07 has been still (0 while moving).</summary>
        public float StillSeconds { get; private set; }

        /// <summary>0 moving .. 1 settled into stillness (eased).</summary>
        public float Amount => _amount.Value;

        /// <summary>Feeds one frame of 07's motion; returns <see cref="Amount"/>.</summary>
        public float Step(float speed, Vector2 driveInput, bool grounded, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float input = Mathf.Max(Mathf.Abs(driveInput.x), Mathf.Abs(driveInput.y));
            bool still = grounded && speed < _tuning.StillSpeed && input <= _tuning.StillInput;
            StillSeconds = still ? StillSeconds + dt : 0f;
            float target = still && StillSeconds >= _tuning.StillDelay ? 1f : 0f;
            return _amount.Step(target, dt, _tuning.StillRiseTime, _tuning.StillReleaseTime);
        }
    }
}
