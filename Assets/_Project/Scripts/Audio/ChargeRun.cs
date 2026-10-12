using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The cutter's charge run in time (docs/features/M3-26 section 2; pure, EditMode-tested), on the same clock as
    /// Rover's lamps: with N lamps, the Anticipate's seconds T and its strength s (0 fresh, 0.5 on a resume), the run's
    /// eased share is s + (1 - s)(t / T)², so lamp i (0 at the battery) lights at
    /// t_i = T·sqrt(((i + 1) / N - s) / (1 - s)) and the last lands on the ignition. Lamps with (i + 1) / N ≤ s are
    /// already lit on a resume and sound no note. Each lit lamp sounds the next pentatonic step. Allocation-free.
    /// </summary>
    public sealed class ChargeRun
    {
        private int _count;
        private int _next;
        private float _seconds;
        private float _start;
        private float _elapsed;

        /// <summary>True while lamps are still to light.</summary>
        public bool IsRunning => _next < _count;

        /// <summary>Starts a run of <paramref name="lampCount"/> lamps over <paramref name="seconds"/> from share
        /// <paramref name="startShare"/> (the Anticipate's strength).</summary>
        public void Begin(int lampCount, float seconds, float startShare)
        {
            _count = Mathf.Max(0, lampCount);
            _seconds = Mathf.Max(0f, seconds);
            _start = Mathf.Clamp01(startShare);
            _elapsed = 0f;
            _next = FirstUnlit(_count, _start);
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
            if (_next >= _count || _elapsed < Onset(_next, _count, _seconds, _start))
            {
                return false;
            }

            _next++;
            return true;
        }

        /// <summary>The first lamp a run from <paramref name="startShare"/> still has to light.</summary>
        public static int FirstUnlit(int lampCount, float startShare)
        {
            int lamp = 0;
            while (lamp < lampCount && (lamp + 1) / (float)lampCount <= startShare)
            {
                lamp++;
            }

            return lamp;
        }

        /// <summary>Seconds from the run's start to lamp <paramref name="lamp"/> lighting (Rover's rule).</summary>
        public static float Onset(int lamp, int lampCount, float seconds, float startShare)
        {
            if (lampCount <= 0 || startShare >= 1f)
            {
                return 0f;
            }

            float share = ((lamp + 1) / (float)lampCount - startShare) / (1f - startShare);
            return seconds * Mathf.Sqrt(Mathf.Clamp01(share));
        }
    }
}
