using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's metal warming while it drives and ticking as it cools: heat eases towards the motor's effort while
    /// moving and cools once stopped; a few seconds after stopping, while still warm, a tick now and then - closer
    /// together and louder when hot, sparser and softer as it cools, none once it has settled. Moving again stops
    /// the ticking. Allocation-free.
    /// </summary>
    public sealed class MotorCooling
    {
        // Cooler ticks are softer, down to this share of the full tick volume.
        private const float CoolTickShare = 0.4f;

        private readonly SoundscapeTuning _tuning;
        private readonly AudioRandom _random;
        private readonly EasedValue _heat = new EasedValue(0f);
        private float _untilTick;
        private bool _wasMoving = true;

        public MotorCooling(SoundscapeTuning tuning, AudioRandom random)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>0 cold .. 1 hot after a long drive at full effort.</summary>
        public float Heat => _heat.Value;

        /// <summary>
        /// Advances one frame: <paramref name="effort"/> is the motor's work 0..1 (e.g. normalized speed),
        /// <paramref name="moving"/> whether 07 is driving. Returns true when a tick is due, with its volume scale.
        /// </summary>
        public bool Step(float deltaTime, float effort, bool moving, out float volume)
        {
            volume = 0f;
            float dt = Mathf.Max(0f, deltaTime);
            _heat.Step(moving ? Mathf.Clamp01(effort) : 0f, dt, _tuning.HeatTime, _tuning.CoolTime);
            if (moving)
            {
                _wasMoving = true;
                return false;
            }

            if (_wasMoving)
            {
                _wasMoving = false;
                _untilTick = _tuning.TickDelay;
            }

            _untilTick -= dt;
            if (_untilTick > 0f || Heat < _tuning.TickMinHeat)
            {
                return false;
            }

            float warmth = Mathf.InverseLerp(_tuning.TickMinHeat, 1f, Heat);
            float gap = Mathf.Lerp(_tuning.TickMaxGap, _tuning.TickMinGap, warmth);
            float jitter = _tuning.TickGapJitter;
            _untilTick = gap * _random.Range(1f - jitter, 1f + jitter);
            volume = _tuning.TickVolume * Mathf.Lerp(CoolTickShare, 1f, warmth);
            return true;
        }
    }
}
