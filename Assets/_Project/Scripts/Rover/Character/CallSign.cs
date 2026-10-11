using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The station's old call sign on 07's antenna tip, three short and one long (docs/design/cutscenes.md §1.5): it
    /// plays once every period while 07 rests, so the player knows the pattern by the time Earth answers with it.
    /// Each pulse has soft edges and the pulses repeat slower than 3 per second. <see cref="Glow"/> is 0 between
    /// pulses and 1 at a pulse's height.
    /// </summary>
    public sealed class CallSign
    {
        private const int ShortPulses = 3;

        private readonly RoverCharacterTuning _tuning;
        private float _time;

        public CallSign(RoverCharacterTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _time = tuning.CallSignPeriod;
        }

        public float Glow { get; private set; }

        /// <summary>Seconds one full pattern takes, from the first short pulse to the end of the long one.</summary>
        public float PatternSeconds => ShortPulses * (_tuning.CallSignShort + _tuning.CallSignGap)
            + _tuning.CallSignLong;

        /// <summary>Starts the pattern from its first pulse now (07's first blink, O11).</summary>
        public void Restart()
        {
            _time = 0f;
        }

        /// <param name="atRest">07 is resting; the pattern only starts while it is.</param>
        public void Step(bool atRest, float deltaTime)
        {
            float pattern = PatternSeconds;
            bool playing = _time < pattern;
            if (playing || atRest)
            {
                _time += deltaTime;
                if (_time >= Mathf.Max(_tuning.CallSignPeriod, pattern))
                {
                    _time = 0f;
                }
            }

            Glow = _time < pattern ? Pulse(_time) : 0f;
        }

        private float Pulse(float time)
        {
            float slot = _tuning.CallSignShort + _tuning.CallSignGap;
            int index = Mathf.Min((int)(time / slot), ShortPulses);
            float start = index * slot;
            float length = index < ShortPulses ? _tuning.CallSignShort : _tuning.CallSignLong;
            float t = time - start;
            if (t > length)
            {
                return 0f;
            }

            float edge = Mathf.Min(_tuning.CallSignEdge, 0.5f * length);
            return Smoothing.SmoothStep(0f, edge, t) * (1f - Smoothing.SmoothStep(length - edge, length, t));
        }
    }
}
