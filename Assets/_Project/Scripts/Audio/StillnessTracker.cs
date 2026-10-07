using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The soundscape's response to 07 resting (feel pillar 6), on top of the Rover's shared
    /// <see cref="MoonProject.Core.IRoverStillness"/>: <see cref="Amount"/> eases 0 -> 1 once 07 has been still for
    /// the still delay (settling ~6 s after it stopped) and falls back within about a second the moment it moves.
    /// Allocation-free.
    /// </summary>
    public sealed class StillnessTracker
    {
        private readonly SoundscapeTuning _tuning;
        private readonly EasedValue _amount = new EasedValue(0f);

        public StillnessTracker(SoundscapeTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Seconds 07 has been still, as last fed (0 while moving).</summary>
        public float StillSeconds { get; private set; }

        /// <summary>0 moving .. 1 settled into stillness (eased).</summary>
        public float Amount => _amount.Value;

        /// <summary>Feeds the Rover's seconds of stillness for one frame; returns <see cref="Amount"/>.</summary>
        public float Step(float stillSeconds, float deltaTime)
        {
            StillSeconds = Mathf.Max(0f, stillSeconds);
            float target = StillSeconds > 0f && StillSeconds >= _tuning.StillDelay ? 1f : 0f;
            return _amount.Step(target, Mathf.Max(0f, deltaTime), _tuning.StillRiseTime, _tuning.StillReleaseTime);
        }
    }
}
