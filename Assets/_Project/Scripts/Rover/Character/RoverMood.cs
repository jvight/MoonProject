using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's inner state, stepped once per frame:
    /// <list type="bullet">
    /// <item>Active -> Daydreaming after standing still for IdleDelay: <see cref="Idle"/> eases 0 -> 1 (head drifts up
    /// to Earth, lid droops, the wing sighs open further than its rest and settles back, the eye breathes deeper and
    /// dimmer).</item>
    /// <item>Daydreaming -> Active as soon as it drives: Idle eases back quickly, and if it was deep in the daydream
    /// <see cref="Step"/> reports a wake-up so the caller can perk up.</item>
    /// </list>
    /// Perk-up and "oof" are smooth swells (critically damped springs kicked from rest) so nothing ever snaps.
    /// Blinks are occasional and slow, spaced by a seeded generator (deterministic for tests).
    /// </summary>
    public sealed class RoverMood
    {
        /// <summary>A critically damped spring kicked with v0 peaks at v0 / (omega * e); this inverts that.</summary>
        private const float PeakToKick = 2.7182818f;

        private const float MaxSwell = 1.5f;

        private readonly RoverCharacterTuning _tuning;
        private uint _random;
        private float _stillTime;
        private float _breathPhase;
        private float _tipTime;
        private float _blinkCountdown;
        private float _blinkElapsed = -1f;
        private DampedSpring _perk;
        private DampedSpring _oof;
        private DampedSpring _wing;
        private DampedSpring _sigh;

        public RoverMood(RoverCharacterTuning tuning, uint seed)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _random = seed == 0u ? 1u : seed;
            _blinkCountdown = NextBlinkInterval();
        }

        /// <summary>Daydream weight, 0 (active) .. 1 (fully daydreaming).</summary>
        public float Idle { get; private set; }

        /// <summary>True once standing still for longer than the idle delay.</summary>
        public bool IsDaydreaming => _stillTime >= _tuning.IdleDelay;

        /// <summary>Perk-up swell, 0 .. ~1.</summary>
        public float Perk => Mathf.Clamp(_perk.Value, 0f, MaxSwell);

        /// <summary>"Oof" swell, 0 .. ~1.</summary>
        public float Oof => Mathf.Clamp(_oof.Value, 0f, MaxSwell);

        /// <summary>Blink closure, 0 (none) .. 1 (fully closed mid-blink).</summary>
        public float Blink { get; private set; }

        /// <summary>Breath cycle, 0 (exhaled) .. 1 (inhaled).</summary>
        public float Breath { get; private set; }

        /// <summary>Eyelid closure 0 (open) .. 1 (closed), all influences combined.</summary>
        public float LidClosure { get; private set; }

        /// <summary>Eye brightness multiplier (1 = authored glow while active and calm).</summary>
        public float EyeGlow { get; private set; } = 1f;

        /// <summary>Solar wing opening, 0 (folded) .. 1 (fully open).</summary>
        public float WingOpen => Mathf.Clamp01(_wing.Value);

        /// <summary>Antenna tip brightness multiplier.</summary>
        public float TipGlow { get; private set; }

        /// <summary>Extra head pitch (deg, + = up) from perk-up and "oof".</summary>
        public float HeadPitchOffset => _tuning.PerkHeadLift * Perk - _tuning.OofHeadDip * Oof;

        /// <summary>
        /// Advances the mood. <paramref name="driveInput"/> is the magnitude of the drive stick/keys (0..1).
        /// Returns true when 07 was woken from a deep daydream this frame.
        /// </summary>
        public bool Step(float speed, float driveInput, float deltaTime)
        {
            bool wasDaydreaming = IsDaydreaming;
            float idleBefore = Idle;
            bool still = speed < _tuning.StillSpeed && driveInput < _tuning.ActivityInput;
            _stillTime = still ? _stillTime + deltaTime : 0f;
            bool daydreaming = IsDaydreaming;

            float halfLife = daydreaming ? _tuning.IdleRiseHalfLife : _tuning.IdleFallHalfLife;
            Idle = Smoothing.Damp(Idle, daydreaming ? 1f : 0f, halfLife, deltaTime);

            if (daydreaming && !wasDaydreaming)
            {
                Swell(ref _sigh, _tuning.WingSighAmount, _tuning.WingSighFrequency);
            }

            _sigh.Step(0f, _tuning.WingSighFrequency, 1f, deltaTime);
            float wingTarget = Idle * _tuning.WingIdleOpen + Mathf.Max(0f, _sigh.Value);
            _wing.Step(wingTarget, _tuning.WingFrequency, _tuning.WingDamping, deltaTime);
            _wing.Clamp(0f, 1f);
            _perk.Step(0f, _tuning.PerkFrequency, 1f, deltaTime);
            _oof.Step(0f, _tuning.OofFrequency, 1f, deltaTime);

            StepBreath(deltaTime);
            StepBlink(deltaTime);
            StepTip(deltaTime);
            ComposeFace();

            return wasDaydreaming && !daydreaming && idleBefore >= _tuning.WakeThreshold;
        }

        /// <summary>Starts a perk-up swell peaking at <paramref name="strength"/> (0..1).</summary>
        public void PerkUp(float strength)
        {
            Swell(ref _perk, strength, _tuning.PerkFrequency);
        }

        /// <summary>Starts an "oof" swell peaking at <paramref name="strength"/> (0..1).</summary>
        public void FeelImpact(float strength)
        {
            Swell(ref _oof, strength, _tuning.OofFrequency);
        }

        /// <summary>Kicks a critically damped spring so it swells to <paramref name="peak"/> and fades.</summary>
        private static void Swell(ref DampedSpring spring, float peak, float frequency)
        {
            spring.AddVelocity(Mathf.Clamp01(peak) * 2f * Mathf.PI * frequency * PeakToKick);
        }

        /// <summary>"Oof" strength for a landing: 0 below the hard-landing threshold, else OofMinStrength..1.</summary>
        public float OofStrength(float impactSpeed)
        {
            if (impactSpeed < _tuning.OofImpactSpeed)
            {
                return 0f;
            }

            float t = Mathf.InverseLerp(_tuning.OofImpactSpeed, _tuning.OofFullImpact, impactSpeed);
            return Mathf.Lerp(_tuning.OofMinStrength, 1f, t);
        }

        private void StepBreath(float deltaTime)
        {
            _breathPhase = Mathf.Repeat(_breathPhase + deltaTime / _tuning.BreathPeriod, 1f);
            Breath = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * _breathPhase);
        }

        private void StepBlink(float deltaTime)
        {
            if (_blinkElapsed >= 0f)
            {
                _blinkElapsed += deltaTime;
                if (_blinkElapsed >= _tuning.BlinkDuration)
                {
                    _blinkElapsed = -1f;
                    _blinkCountdown = NextBlinkInterval();
                }
            }
            else
            {
                _blinkCountdown -= deltaTime;
                if (_blinkCountdown <= 0f)
                {
                    _blinkElapsed = 0f;
                }
            }

            Blink = _blinkElapsed >= 0f ? Pulse(_blinkElapsed / _tuning.BlinkDuration) : 0f;
        }

        private void StepTip(float deltaTime)
        {
            _tipTime = Mathf.Repeat(_tipTime + deltaTime, _tuning.TipBlinkPeriod);
            float pulse = _tipTime < _tuning.TipBlinkDuration ? Pulse(_tipTime / _tuning.TipBlinkDuration) : 0f;
            TipGlow = Mathf.Lerp(_tuning.TipRestGlow, 1f, pulse);
        }

        private void ComposeFace()
        {
            float lid = Mathf.Lerp(_tuning.ActiveLid, _tuning.IdleLid, Idle)
                - _tuning.PerkWiden * Perk
                + _tuning.OofSquint * Oof;
            lid = Mathf.Clamp01(lid);
            LidClosure = lid + (1f - lid) * Blink;

            float depth = Mathf.Lerp(_tuning.ActiveBreathDepth, _tuning.IdleBreathDepth, Idle);
            float glow = 1f + depth * (2f * Breath - 1f) - _tuning.IdleGlowDim * Idle + _tuning.PerkGlowBoost * Perk;
            float lidDim = 1f - _tuning.LidGlowDim * LidClosure;
            float restingLidDim = 1f - _tuning.LidGlowDim * _tuning.ActiveLid;
            EyeGlow = Mathf.Max(0f, glow * lidDim / restingLidDim);
        }

        /// <summary>Smooth 0 -> 1 -> 0 over t = 0..1 (sin squared): slow at both ends, never a pop.</summary>
        private static float Pulse(float t)
        {
            float s = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            return s * s;
        }

        private float NextBlinkInterval()
        {
            // xorshift32: tiny, allocation-free, deterministic per seed.
            _random ^= _random << 13;
            _random ^= _random >> 17;
            _random ^= _random << 5;
            float unit = (_random & 0xFFFFFF) / (float)0x1000000;
            return Mathf.Lerp(_tuning.BlinkMinInterval, _tuning.BlinkMaxInterval, unit);
        }
    }
}
