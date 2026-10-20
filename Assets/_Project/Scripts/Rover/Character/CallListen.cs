using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's part in the call from the sky (<see cref="CallListenSettings"/>), as pure logic fed the call's state each
    /// frame: how far the head turns toward the call's point of light (<see cref="HeadWeight"/>), the antenna lamp's
    /// glow (<see cref="LampGlow"/>: a stutter while the call is broken, the pattern keyed back once it locks) and
    /// the reach toward it (<see cref="Reach"/>). A call that is already locked when 07 first looks is not keyed back
    /// until the next repeat: only a change seen live plays at once.
    /// </summary>
    public sealed class CallListen
    {
        private const int StutterSeed = 7919;

        private readonly CallListenSettings _settings;
        private CallState _state;
        private bool _seen;
        private float _idleSeconds;
        private float _reachAt = float.PositiveInfinity;
        private float _dejectedFor = float.PositiveInfinity;
        private float _darkFor = float.PositiveInfinity;
        private float _keying;
        private int _slot = -1;
        private float _slotLeft;
        private bool _slotLit;

        public CallListen(CallListenSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>How far the head turns from where it would look to the call's point, 0 to 1.</summary>
        public float HeadWeight { get; private set; }

        /// <summary>Glow the antenna lamp adds now (linear fraction of its authored glow).</summary>
        public float LampGlow { get; private set; }

        /// <summary>The reach toward the call now, 0 (none) to 1 (the top of a reach).</summary>
        public float Reach { get; private set; }

        /// <summary>How far 07 hangs its head after its reply failed, 0 (not at all) to 1 (fully).</summary>
        public float Dejection { get; private set; }

        /// <summary>07 tried to answer the call and could not: its head lowers and the lamp goes dark for a while.</summary>
        public void ReplyFailed()
        {
            _dejectedFor = 0f;
            _darkFor = 0f;
            _reachAt = float.PositiveInfinity;
        }

        /// <param name="state">How clearly 07 hears the call now.</param>
        /// <param name="listening">07 idles or charges: it turns toward the call.</param>
        /// <param name="keying">How strongly the call's sign keys now, 0..1 (Audio's carrier, in phase with the sound
        /// and the point of light); 0 when there is no such signal, so the locked lamp stays dark.</param>
        public void Step(float deltaTime, CallState state, bool listening, float keying)
        {
            if (!_seen)
            {
                _seen = true;
            }

            _state = state;
            _keying = Mathf.Clamp01(keying);

            HeadWeight = Smoothing.Damp(HeadWeight, listening ? 1f : 0f, _settings.HeadHalfLife, deltaTime);
            _idleSeconds = listening ? _idleSeconds + deltaTime : 0f;
            _dejectedFor += deltaTime;
            _darkFor += deltaTime;
            Dejection = Smoothing.Damp(Dejection, _dejectedFor < _settings.DejectedHoldSeconds ? 1f : 0f,
                _settings.DejectedHalfLife, deltaTime);
            StepReach(deltaTime, listening);
            StepLamp(deltaTime);
        }

        private void StepReach(float deltaTime, bool listening)
        {
            bool broken = _state == CallState.Broken;
            if (broken && listening && float.IsPositiveInfinity(_reachAt) && _idleSeconds >= _settings.ReachFirstSeconds)
            {
                _reachAt = 0f;
            }

            if (!float.IsPositiveInfinity(_reachAt))
            {
                _reachAt += deltaTime;
                Reach = ReachShape(_reachAt);
                if (_reachAt >= _settings.ReachRise + _settings.ReachHold + _settings.ReachFall)
                {
                    _reachAt = float.PositiveInfinity;
                    _idleSeconds = _settings.ReachFirstSeconds - _settings.ReachInterval;
                }
            }
            else
            {
                Reach = 0f;
            }
        }

        private float ReachShape(float time)
        {
            float rise = _settings.ReachRise;
            if (time < rise)
            {
                return Smoothing.SmoothStep(0f, rise, time);
            }

            float fall = rise + _settings.ReachHold;
            return 1f - Smoothing.SmoothStep(fall, fall + _settings.ReachFall, time);
        }

        private void StepLamp(float deltaTime)
        {
            float glow = 0f;
            if (_state == CallState.Broken)
            {
                glow = _settings.StutterBase + (StepStutter(deltaTime) ? _settings.StutterGlow : 0f);
            }
            else if (_state == CallState.Locked)
            {
                glow = _keying * _settings.KeyGlow;
            }
            else if (_state == CallState.Clear)
            {
                glow = _settings.ClearGlow;
            }

            if (_darkFor < _settings.LampDarkSeconds)
            {
                glow = 0f;
            }

            LampGlow = Smoothing.Damp(LampGlow, glow, _settings.LampHalfLife, deltaTime);
        }

        /// <summary>
        /// The stutter's lit bursts and dark gaps: each slot has a length between the tuned bounds and is lit or dark by
        /// a fixed hash of its number, so the stutter never repeats evenly and never changes between runs.
        /// </summary>
        private bool StepStutter(float deltaTime)
        {
            _slotLeft -= deltaTime;
            while (_slotLeft <= 0f)
            {
                _slot++;
                _slotLeft += Mathf.Lerp(_settings.StutterMinSeconds, _settings.StutterMaxSeconds,
                    Hash(_slot + StutterSeed));
                _slotLit = Hash(_slot) < _settings.StutterLitShare;
            }

            return _slotLit;
        }

        private static float Hash(int n)
        {
            unchecked
            {
                uint x = (uint)(n * 374761393 + StutterSeed * 668265263);
                x = (x ^ (x >> 13)) * 1274126177u;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
