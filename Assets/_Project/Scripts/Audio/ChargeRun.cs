using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The cutter's charge run in time (docs/features/M3-26 section 2; pure, EditMode-tested): the lamps on 07's back
    /// light one after another from the battery to the emitter over the Anticipate's seconds, accelerating (ease-in),
    /// and each sounds the next pentatonic step. A resume starts part-way (the start fraction), so fewer lamps play
    /// in its shorter time and the last one always lands on the ignition. Allocation-free.
    /// </summary>
    public sealed class ChargeRun
    {
        private int _count;
        private int _first;
        private int _next;
        private float _seconds;
        private float _elapsed;

        /// <summary>True while lamps are still to light.</summary>
        public bool IsRunning => _next < _count;

        /// <summary>
        /// Starts a run of <paramref name="lampCount"/> lamps over <paramref name="seconds"/>, from lamp
        /// <c>floor(startFraction × count)</c> (0 fresh, 0.5 on a resume).
        /// </summary>
        public void Begin(int lampCount, float seconds, float startFraction)
        {
            _count = Mathf.Max(0, lampCount);
            _first = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(startFraction) * _count), 0, Mathf.Max(0, _count - 1));
            _next = _first;
            _seconds = Mathf.Max(0f, seconds);
            _elapsed = 0f;
        }

        /// <summary>Stops the run (a cancel, or the ignition arrived early).</summary>
        public void Stop()
        {
            _next = _count;
        }

        /// <summary>Advances the run's clock by <paramref name="deltaTime"/> seconds.</summary>
        public void Advance(float deltaTime)
        {
            _elapsed += deltaTime;
        }

        /// <summary>
        /// True with the next lamp to sound when its onset has passed; call until it returns false, since a long
        /// frame may pass several onsets.
        /// </summary>
        public bool TryNext(out int lamp)
        {
            lamp = _next;
            if (_next >= _count || _elapsed < OnsetOf(_next))
            {
                return false;
            }

            _next++;
            return true;
        }

        /// <summary>
        /// Seconds from the run's start to lamp <paramref name="lamp"/>: the run's progress eases in (progress =
        /// (t / T)²), so the first lamp sounds at once and the last lands on the ignition.
        /// </summary>
        public float OnsetOf(int lamp)
        {
            int span = _count - 1 - _first;
            if (span <= 0)
            {
                return 0f;
            }

            float fraction = (lamp - _first) / (float)span;
            return _seconds * Mathf.Sqrt(Mathf.Clamp01(fraction));
        }
    }
}
