using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Air rushing past while 07 is airborne: silent on the ground and through small bumps, then swelling with air time
    /// and speed (faster = louder and a little higher), and resolving quickly on touchdown.
    /// </summary>
    public sealed class AirWindModel
    {
        // -60 dB: below this a fading wind is inaudible, so it snaps to silence and its source can stop.
        private const float SilentGain = 1e-3f;

        private readonly JumpAudioTuning _tuning;
        private readonly EasedValue _gain = new EasedValue(0f);

        public AirWindModel(JumpAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        public float Gain => _gain.Value;

        public float Pitch { get; private set; } = 1f;

        public void Step(float deltaTime, bool grounded, float airTime, float speed)
        {
            float pace = Mathf.Clamp01(speed / _tuning.WindFullSpeed);
            float target = 0f;
            if (!grounded && airTime > _tuning.WindMinAirTime)
            {
                float swell = Mathf.SmoothStep(0f, 1f, (airTime - _tuning.WindMinAirTime) / _tuning.WindSwellTime);
                target = swell * Mathf.Lerp(_tuning.WindSlowGain, 1f, pace);
            }

            _gain.Step(target, deltaTime, _tuning.WindRiseTime, _tuning.WindFallTime);
            if (target <= 0f && _gain.Value < SilentGain)
            {
                _gain.Snap(0f);
            }

            Pitch = Mathf.Lerp(_tuning.WindSlowPitch, _tuning.WindFastPitch, pace);
        }
    }
}
