using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's inner state, stepped once per frame:
    /// <list type="bullet">
    /// <item>A session can open with 07 asleep (<see cref="WakeUpSequence"/>): lid shut, eye dark, head bowed, until
    /// it wakes on its own or the player drives. Daydreaming only starts once it is awake.</item>
    /// <item>Active -> Daydreaming after standing still for IdleDelay: <see cref="Idle"/> eases 0 -> 1 (head drifts up
    /// to Earth, lid droops, the wing opens a little, the eye breathes deeper and dimmer).</item>
    /// <item>The sigh waits for the camera: once 07 is daydreaming and the lonely wide shot is opening
    /// (<see cref="SetWideShot"/>), whichever comes last, 07 sighs (the wing opens further than its rest and settles
    /// back) as the frame breathes out. Once per opening.</item>
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
        private readonly WakeUpSequence _wake;
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
        private DampedSpring _nod;
        private float _effortTarget;
        private bool _wideShot;
        private bool _sighedIntoWideShot;
        private bool _wingMended;

        /// <param name="startAsleep">Open the session with 07 asleep (first boot).</param>
        public RoverMood(RoverCharacterTuning tuning, uint seed, bool startAsleep)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _wake = new WakeUpSequence(tuning, startAsleep);
            _random = seed == 0u ? 1u : seed;
            _blinkCountdown = NextBlinkInterval();
            ComposeFace();
        }

        /// <summary>The first-boot wake-up (phase, sleep and Earth-glance weights).</summary>
        public WakeUpSequence Wake => _wake;

        /// <summary>How much 07 looks up toward Earth right now: daydreaming or the waking glance, 0..1.</summary>
        public float EarthGaze => Mathf.Max(Idle, _wake.EarthLook);

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

        /// <summary>Eye emission as a linear multiplier (1 = authored glow while active and calm).</summary>
        public float EyeGlow { get; private set; } = 1f;

        /// <summary>Solar wing opening, 0 (folded) .. 1 (fully open).</summary>
        public float WingOpen => Mathf.Clamp01(_wing.Value);

        /// <summary>Antenna tip emission as a linear multiplier.</summary>
        public float TipGlow { get; private set; }

        /// <summary>Effort while charging a Hover-Jump, 0..1 (eased): a squint and a gathered head.</summary>
        public float Effort { get; private set; }

        /// <summary>Contented-nod swell, 0 .. ~1.</summary>
        public float Nodding => Mathf.Clamp(_nod.Value, 0f, MaxSwell);

        /// <summary>Sigh swell (head droops, lid lowers, wing opens a little), 0 .. ~1.</summary>
        public float Sighing => Mathf.Clamp(_sigh.Value, 0f, MaxSwell);

        /// <summary>Extra head pitch (deg, + = up) from perk-up, "oof", nods, sighs and sleep.</summary>
        public float HeadPitchOffset => _tuning.PerkHeadLift * Perk
            - _tuning.OofHeadDip * Oof
            - _tuning.NodDepth * Nodding
            - _tuning.SighHeadDrop * Sighing
            - _tuning.SleepHeadBow * _wake.Sleep
            - _tuning.EffortHeadDip * Effort;

        /// <summary>
        /// Advances the mood. <paramref name="driveInput"/> is the magnitude of the drive stick/keys (0..1).
        /// Returns what changed this frame: the first-boot wake starting or finishing, or a wake from a daydream.
        /// </summary>
        public MoodTransition Step(float speed, float driveInput, float deltaTime)
        {
            bool active = driveInput >= _tuning.ActivityInput;
            MoodTransition waking = _wake.Step(active, deltaTime);
            if (waking == MoodTransition.FinishedWaking)
            {
                BlinkNow();
            }

            bool wasDaydreaming = IsDaydreaming;
            float idleBefore = Idle;
            bool still = speed < _tuning.StillSpeed && !active && _wake.Phase == WakePhase.Awake;
            _stillTime = still ? _stillTime + deltaTime : 0f;
            bool daydreaming = IsDaydreaming;

            float halfLife = daydreaming ? _tuning.IdleRiseHalfLife : _tuning.IdleFallHalfLife;
            Idle = Smoothing.Damp(Idle, daydreaming ? 1f : 0f, halfLife, deltaTime);

            if (daydreaming && _wideShot && !_sighedIntoWideShot)
            {
                Sigh(_tuning.DaydreamSigh);
                _sighedIntoWideShot = true;
            }

            _sigh.Step(0f, _tuning.SighFrequency, 1f, deltaTime);
            _nod.Step(0f, _tuning.NodFrequency, 1f, deltaTime);
            float idleOpen = _wingMended ? _tuning.WingIdleOpenMended : _tuning.WingIdleOpen;
            float wingTarget = Idle * idleOpen + _tuning.WingSighAmount * Sighing;
            _wing.Step(wingTarget, _tuning.WingFrequency, _tuning.WingDamping, deltaTime);
            _wing.Clamp(0f, 1f);
            _perk.Step(0f, _tuning.PerkFrequency, 1f, deltaTime);
            _oof.Step(0f, _tuning.OofFrequency, 1f, deltaTime);

            Effort = Smoothing.Damp(Effort, _effortTarget, _tuning.EffortHalfLife, deltaTime);
            StepBreath(deltaTime);
            StepBlink(deltaTime);
            StepTip(deltaTime);
            ComposeFace();

            if (waking != MoodTransition.None)
            {
                return waking;
            }

            bool wokeFromDaydream = wasDaydreaming && !daydreaming && idleBefore >= _tuning.WakeThreshold;
            return wokeFromDaydream ? MoodTransition.WokeFromDaydream : MoodTransition.None;
        }

        /// <summary>
        /// The camera began opening to the lonely wide shot (true) or handed back (false): 07's daydream sigh lands as
        /// the frame opens.
        /// </summary>
        public void SetWideShot(bool open)
        {
            _wideShot = open;
            _sighedIntoWideShot &= open;
        }

        /// <summary>
        /// Tilly replaced the solar wing's missing cell: from now on the wing settles open wider while daydreaming.
        /// </summary>
        public void SetWingMended(bool mended)
        {
            _wingMended = mended;
        }

        /// <summary>
        /// Something new needs 07's attention (it landed from a radio-hop): any daydream ends and the rest starts
        /// over, easing back with the usual fall half-life.
        /// </summary>
        public void Rouse()
        {
            _stillTime = 0f;
        }

        /// <summary>How hard 07 is gathering itself for a Hover-Jump right now (the charge, 0..1).</summary>
        public void SetEffort(float effort)
        {
            _effortTarget = Mathf.Clamp01(effort);
        }

        /// <summary>Joy (perk strength) for touching down after <paramref name="airTime"/> s in the air.</summary>
        public float LandingJoy(float airTime)
        {
            float flight = Mathf.InverseLerp(_tuning.JoyAirTimeFrom, _tuning.JoyAirTimeFull, airTime);
            return _tuning.FlightLandingJoy * flight;
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

        /// <summary>A slow, contented nod peaking at <paramref name="strength"/> (0..1), with a soft blink.</summary>
        public void NodContentedly(float strength)
        {
            Swell(ref _nod, strength, _tuning.NodFrequency);
            BlinkNow();
        }

        /// <summary>A whole-body sigh peaking at <paramref name="strength"/> (0..1).</summary>
        public void Sigh(float strength)
        {
            Swell(ref _sigh, strength, _tuning.SighFrequency);
        }

        /// <summary>Starts a slow blink now unless one is already running.</summary>
        public void BlinkNow()
        {
            if (_blinkElapsed < 0f)
            {
                _blinkElapsed = 0f;
            }
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
            TipGlow = Mathf.Lerp(_tuning.TipRestGlow, _tuning.TipPeakGlow, pulse);
        }

        private void ComposeFace()
        {
            float lid = Mathf.Lerp(_tuning.ActiveLid, _tuning.IdleLid, Idle)
                - _tuning.PerkWiden * Perk
                + _tuning.OofSquint * Oof
                + _tuning.SighLidDroop * Sighing
                + _tuning.EffortSquint * Effort;
            lid = Mathf.Lerp(Mathf.Clamp01(lid), 1f, _wake.Sleep);
            LidClosure = lid + (1f - lid) * Blink;

            float depth = Mathf.Lerp(_tuning.ActiveBreathDepth, _tuning.IdleBreathDepth, EarthGaze);
            float glow = 1f + depth * (2f * Breath - 1f) - _tuning.IdleGlowDim * Idle + _tuning.PerkGlowBoost * Perk;
            float lidDim = 1f - _tuning.LidGlowDim * LidClosure;
            float restingLidDim = 1f - _tuning.LidGlowDim * _tuning.ActiveLid;
            EyeGlow = Mathf.Max(0f, glow * lidDim / restingLidDim) * (1f - _wake.Sleep);
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
