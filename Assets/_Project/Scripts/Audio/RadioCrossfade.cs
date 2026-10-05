using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The "turning the dial" transition between two tracks: the outgoing track fades out (equal power) over the
    /// first part, static swells in the middle, and the incoming track starts and fades in over the last part.
    /// Times are fractions of the crossfade so the shape scales with its duration.
    /// </summary>
    public sealed class RadioCrossfade
    {
        private float _elapsed;
        private float _duration;
        private float _outgoingEnd;
        private float _incomingStart;
        private bool _incomingStarted;

        public bool Active { get; private set; }

        /// <summary>0..1 progress of the active crossfade (1 when idle).</summary>
        public float Progress => Active ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        public float OutgoingGain => Active ? Mathf.Cos(0.5f * Mathf.PI * Mathf.Clamp01(Progress / _outgoingEnd)) : 0f;

        public float IncomingGain
        {
            get
            {
                if (!Active)
                {
                    return 1f;
                }

                float u = Mathf.Clamp01((Progress - _incomingStart) / (1f - _incomingStart));
                return Mathf.Sin(0.5f * Mathf.PI * u);
            }
        }

        /// <summary>0 at both ends, 1 at the middle of the crossfade.</summary>
        public float StaticSwell => Active ? Mathf.Sin(Mathf.PI * Progress) : 0f;

        public void Begin(float duration, float outgoingEnd, float incomingStart)
        {
            _duration = Mathf.Max(duration, Mathf.Epsilon);
            _outgoingEnd = Mathf.Clamp(outgoingEnd, Mathf.Epsilon, 1f);
            _incomingStart = Mathf.Clamp(incomingStart, 0f, 1f - Mathf.Epsilon);
            _elapsed = 0f;
            _incomingStarted = false;
            Active = true;
        }

        /// <summary>Advances time; returns true on the single step at which the incoming track should start.</summary>
        public bool Step(float deltaTime)
        {
            if (!Active)
            {
                return false;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            bool startNow = !_incomingStarted && Progress >= _incomingStart;
            _incomingStarted |= startNow;
            if (_elapsed >= _duration)
            {
                Active = false;
            }

            return startNow;
        }
    }
}
