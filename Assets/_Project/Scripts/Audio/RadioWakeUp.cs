using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio coming to life with 07: silent while asleep, then the set powers on into a crackle of static, the
    /// music starts after a short delay and resolves in over the fade while the extra static melts away. Times are
    /// seconds from <see cref="Begin"/>; a quicker profile is used when the player woke 07 by driving.
    /// </summary>
    public sealed class RadioWakeUp
    {
        private float _elapsed;
        private float _delay;
        private float _fade;
        private float _powerTime;
        private bool _musicStarted;

        /// <summary>False until <see cref="Begin"/>: the radio is completely silent.</summary>
        public bool IsAwake { get; private set; }

        /// <summary>The set's power-on ramp 0..1 (everything the radio plays is scaled by it).</summary>
        public float Power => IsAwake ? Smooth(_elapsed / _powerTime) : 0f;

        /// <summary>Music level 0..1: zero until the delay, then a smooth rise over the fade.</summary>
        public float MusicGain => _musicStarted ? Smooth((_elapsed - _delay) / _fade) : 0f;

        /// <summary>Extra wake-up static 0..1: full until the music starts, then gone by the end of the fade.</summary>
        public float CrackleBoost => IsAwake ? 1f - MusicGain : 0f;

        /// <summary>Starts waking (ignored when already awake: 07 wakes once per session).</summary>
        public void Begin(float delay, float fade, float powerTime)
        {
            if (IsAwake)
            {
                return;
            }

            IsAwake = true;
            _elapsed = 0f;
            _delay = Mathf.Max(0f, delay);
            _fade = Mathf.Max(fade, Mathf.Epsilon);
            _powerTime = Mathf.Max(powerTime, Mathf.Epsilon);
        }

        /// <summary>Advances time; returns true on the single step at which the music should start playing.</summary>
        public bool Step(float deltaTime)
        {
            if (!IsAwake)
            {
                return false;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_musicStarted || _elapsed < _delay)
            {
                return false;
            }

            _musicStarted = true;
            return true;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
