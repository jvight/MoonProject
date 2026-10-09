using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The music while 07 looks up at the stars (M3-15 §5, <see cref="Core.Events.StargazingChanged"/>): it thins to
    /// a few soft notes over the wind (quieter and low-passed, eased out over the thin time and back over the return
    /// time), and the beat's start asks for one soft airy swell, never again within the swell's rest, so a beat that
    /// ends and restarts soon after stays quiet. Real time; allocation-free.
    /// </summary>
    public sealed class StargazeMix
    {
        private readonly SoundscapeTuning _tuning;
        private readonly LoopFader _gaze = new LoopFader();
        private float _sinceSwell = float.PositiveInfinity;

        public StargazeMix(SoundscapeTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True while 07 is stargazing (the beat's own state, not the eased mix).</summary>
        public bool Gazing => _gaze.IsOn;

        /// <summary>0 normal music .. 1 fully thinned (eased).</summary>
        public float Amount => _gaze.Gain;

        /// <summary>The music's gain: down to the stargazing level as the beat settles in.</summary>
        public float MusicGain => SoundscapeModel.FromDb(Amount * _tuning.StargazeMusicDb);

        /// <summary>The music's low-pass from its open <paramref name="cutoffHz"/>, eased geometrically down to the
        /// stargazing cutoff.</summary>
        public float MusicCutoff(float cutoffHz)
        {
            if (Amount <= 0f || cutoffHz <= 0f)
            {
                return cutoffHz;
            }

            return cutoffHz * Mathf.Pow(Mathf.Min(1f, _tuning.StargazeCutoff / cutoffHz), Amount);
        }

        /// <summary>
        /// The beat starts or ends. Returns true when its start should play the swell: not for a repeat of the same
        /// state, and not within the swell's rest after the last one.
        /// </summary>
        public bool SetGazing(bool gazing)
        {
            if (gazing == _gaze.IsOn)
            {
                return false;
            }

            if (!gazing)
            {
                _gaze.FadeOut();
                return false;
            }

            _gaze.FadeIn();
            if (_sinceSwell < _tuning.StargazeSwellRest)
            {
                return false;
            }

            _sinceSwell = 0f;
            return true;
        }

        public void Step(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            _gaze.Step(dt, _tuning.StargazeThinTime, _tuning.StargazeReturnTime);
            _sinceSwell += dt;
        }
    }
}
