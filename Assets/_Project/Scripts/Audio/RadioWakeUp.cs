using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio coming to life with 07: silent while asleep, then the set powers on into a crackle of static that
    /// holds for a short delay and settles over the settle time into the plain signal hiss. Times are seconds from
    /// <see cref="Begin"/>; a quicker profile is used when the player woke 07 by driving.
    /// </summary>
    public sealed class RadioWakeUp
    {
        private float _elapsed;
        private float _delay;
        private float _settleTime;
        private float _powerTime;

        /// <summary>False until <see cref="Begin"/>: the radio is completely silent.</summary>
        public bool IsAwake { get; private set; }

        /// <summary>The set's power-on ramp 0..1 (everything the radio plays is scaled by it).</summary>
        public float Power => IsAwake ? Smooth(_elapsed / _powerTime) : 0f;

        /// <summary>How far the wake-up crackle has settled, 0..1: zero until the delay, then a smooth rise.</summary>
        public float Settled => IsAwake ? Smooth((_elapsed - _delay) / _settleTime) : 0f;

        /// <summary>Extra wake-up static 0..1: full until the delay, then gone by the end of the settle time.</summary>
        public float CrackleBoost => IsAwake ? 1f - Settled : 0f;

        /// <summary>Starts waking (ignored when already awake: 07 wakes once per session).</summary>
        public void Begin(float delay, float settleTime, float powerTime)
        {
            if (IsAwake)
            {
                return;
            }

            IsAwake = true;
            _elapsed = 0f;
            _delay = Mathf.Max(0f, delay);
            _settleTime = Mathf.Max(settleTime, Mathf.Epsilon);
            _powerTime = Mathf.Max(powerTime, Mathf.Epsilon);
        }

        /// <summary>Advances time (nothing happens while asleep).</summary>
        public void Step(float deltaTime)
        {
            if (IsAwake)
            {
                _elapsed += Mathf.Max(0f, deltaTime);
            }
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
