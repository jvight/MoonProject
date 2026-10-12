using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The charge run as plain logic (docs/features/M3-26 §2.2): light travels from 07's battery along its back to
    /// the emitter in the charge-up's time, accelerating, so the last lamp lights exactly at ignition. Lamp 0 is
    /// nearest the battery. Each lamp flares as the run reaches it, then holds a charged glow; ignition spikes them
    /// all once, each cut step pulses them, a release drains them back toward the battery and the break fades them.
    /// A resume starts part-way (the line is still warm). Fixed memory, no allocations.
    /// </summary>
    public sealed class ChargeRunLights
    {
        private enum State
        {
            Rest,
            Running,
            Charged,
            Draining,
            Fading,
        }

        private readonly ChargeRunSettings _settings;
        private readonly float[] _onsets;
        private State _state;
        private float _elapsed;
        private float _spike;
        private float _pulse;

        public ChargeRunLights(ChargeRunSettings settings, int lamps)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _onsets = new float[Mathf.Max(1, lamps)];
        }

        public int Count => _onsets.Length;

        /// <summary>
        /// When lamp <paramref name="index"/> lights, in seconds from the charge-up's start (the audio's note onset):
        /// the run's eased share is <c>s + (1 - s)(t / T)²</c>, and lamp i lights when it reaches <c>(i + 1) / N</c>,
        /// so <c>t = T √(((i + 1) / N − s) / (1 − s))</c>; lamps at or under the start share are lit at once (0).
        /// </summary>
        public static float Onset(int index, int count, float seconds, float start)
        {
            float share = (index + 1f) / Mathf.Max(1, count);
            float from = Mathf.Clamp01(start);
            if (share <= from || from >= 1f)
            {
                return 0f;
            }

            return seconds * Mathf.Sqrt((share - from) / (1f - from));
        }

        /// <summary>The charge-up begins: the run takes <paramref name="seconds"/>, from <paramref name="start"/> (0 fresh, >0 resume).</summary>
        public void Begin(float seconds, float start)
        {
            for (int i = 0; i < _onsets.Length; i++)
            {
                _onsets[i] = Onset(i, _onsets.Length, seconds, start);
            }

            _state = State.Running;
            _elapsed = 0f;
        }

        /// <summary>Ignition: every lamp spikes once, then holds charged.</summary>
        public void Ignite()
        {
            _state = State.Charged;
            _spike = _settings.SpikeSeconds;
        }

        /// <summary>A cut step: the charged lamps pulse.</summary>
        public void Pulse()
        {
            if (_state == State.Charged)
            {
                _pulse = _settings.PulseSeconds;
            }
        }

        /// <summary>A release: the lamps drain back toward the battery.</summary>
        public void Drain()
        {
            if (_state == State.Running || _state == State.Charged)
            {
                _state = State.Draining;
                _elapsed = 0f;
            }
        }

        /// <summary>The break: the lamps fade as the emitter vents.</summary>
        public void Fade()
        {
            if (_state != State.Rest)
            {
                _state = State.Fading;
                _elapsed = 0f;
            }
        }

        public void Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            _elapsed += step;
            _spike -= step;
            _pulse -= step;
            if ((_state == State.Draining && _elapsed >= _settings.DrainSeconds)
                || (_state == State.Fading && _elapsed >= _settings.FadeSeconds))
            {
                _state = State.Rest;
            }
        }

        /// <summary>Lamp <paramref name="index"/>'s glow now.</summary>
        public float Glow(int index)
        {
            float rest = _settings.RestGlow;
            float charged = _settings.ChargedGlow;
            switch (_state)
            {
                case State.Running:
                    float since = _elapsed - _onsets[index];
                    if (since < 0f)
                    {
                        return rest;
                    }

                    float light = _settings.LightSeconds;
                    float peak = charged * (1f + _settings.Flare);
                    if (since < light)
                    {
                        return Mathf.Lerp(rest, peak, Smoothing.SmoothStep(0f, 1f, since / light));
                    }

                    float settle = Smoothing.SmoothStep(0f, 1f, (since - light) / _settings.FlareSeconds);
                    return Mathf.Lerp(peak, charged, settle);
                case State.Charged:
                    float lift = (_spike > 0f ? _settings.IgnitionSpike : 0f) + (_pulse > 0f ? _settings.StepPulse : 0f);
                    return charged * (1f + lift);
                case State.Draining:
                    int last = _onsets.Length - 1;
                    float offAt = last > 0 ? _settings.DrainSeconds * (last - index) / last : 0f;
                    return _elapsed >= offAt ? rest : charged;
                case State.Fading:
                    return Mathf.Lerp(charged, rest, _elapsed / _settings.FadeSeconds);
                default:
                    return rest;
            }
        }
    }
}
