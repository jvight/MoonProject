using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Pure rover sound logic: a hum that stays silent while 07 sleeps and powers up when it wakes, then eases its
    /// pitch/volume with speed and throttle; eased dust crunch from speed while
    /// grounded, and creak triggers from landings (slightly delayed, after the thump) and from fast changes of the
    /// ground normal (bumps, ridges), rate-limited by a cooldown.
    /// </summary>
    public sealed class RoverAudioModel
    {
        private readonly RoverAudioTuning _tuning;
        private readonly EasedValue _humPitch;
        private readonly EasedValue _humVolume;
        private readonly EasedValue _crunchVolume;
        private readonly EasedValue _wakeGain;
        private Vector3 _lastNormal = Vector3.up;
        private bool _wasGrounded;
        private float _cooldown;
        private float _pendingDelay;
        private float _pendingStrength;

        public RoverAudioModel(RoverAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _humPitch = new EasedValue(tuning.IdlePitch);
            _humVolume = new EasedValue(0f);
            _crunchVolume = new EasedValue(0f);
            _wakeGain = new EasedValue(0f);
        }

        /// <summary>False until <see cref="NotifyAwoke"/>: a sleeping 07 makes no motor sound.</summary>
        public bool IsAwake { get; private set; }

        public float HumPitch => _humPitch.Value;

        /// <summary>0..1 scale of the hum cue's volume (silent until awake, then eases in: no pop).</summary>
        public float HumVolume => _humVolume.Value * _wakeGain.Value;

        /// <summary>07 starts waking: the motor powers up from the wake pitch.</summary>
        public void NotifyAwoke()
        {
            if (IsAwake)
            {
                return;
            }

            IsAwake = true;
            _humPitch.Snap(_tuning.WakeStartPitch);
        }

        public float CrunchVolume => _crunchVolume.Value;

        public float CrunchPitch { get; private set; } = 1f;

        /// <summary>Schedules a creak after a landing at <paramref name="impactSpeed"/> m/s (soft ones are
        /// ignored).</summary>
        public void NotifyLanding(float impactSpeed)
        {
            if (impactSpeed < _tuning.LandingCreakMinImpact)
            {
                return;
            }

            float t = Mathf.InverseLerp(_tuning.LandingCreakMinImpact, _tuning.LandingCreakFullImpact, impactSpeed);
            float strength = Mathf.Lerp(_tuning.CreakMinStrength, 1f, t);
            if (strength > _pendingStrength)
            {
                _pendingStrength = strength;
                _pendingDelay = _tuning.LandingCreakDelay;
            }
        }

        /// <summary>Advances one frame; returns the strength (0..1) of a creak to play now, or 0.</summary>
        public float Step(float deltaTime, in RoverAudioInput input)
        {
            float speed = Mathf.Clamp01(input.NormalizedSpeed);
            float throttle = Mathf.Clamp01(input.Throttle);
            UpdateHum(deltaTime, speed, throttle, input.IsGrounded);
            UpdateCrunch(deltaTime, speed, input.IsGrounded);
            return UpdateCreaks(deltaTime, input);
        }

        private void UpdateHum(float deltaTime, float speed, float throttle, bool grounded)
        {
            float curve = Mathf.Pow(speed, _tuning.SpeedPitchCurve);
            float pitch = Mathf.Lerp(_tuning.IdlePitch, _tuning.TopSpeedPitch, curve)
                          + _tuning.ThrottlePitchBonus * throttle
                          + (grounded ? 0f : _tuning.AirbornePitchBonus * throttle);
            float volume = Mathf.Clamp01(Mathf.Lerp(_tuning.IdleVolume, _tuning.TopSpeedVolume, speed)
                                         + _tuning.ThrottleVolumeBonus * throttle);
            _humPitch.Step(pitch, deltaTime, _tuning.PitchSmoothing);
            _humVolume.Step(volume, deltaTime, _tuning.VolumeSmoothing);
            _wakeGain.Step(IsAwake ? 1f : 0f, deltaTime, _tuning.WakeHumTime);
        }

        private void UpdateCrunch(float deltaTime, float speed, bool grounded)
        {
            float target = grounded
                ? _tuning.CrunchMaxVolume * Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(_tuning.CrunchSpeedStart, _tuning.CrunchSpeedFull, speed))
                : 0f;
            _crunchVolume.Step(target, deltaTime, _tuning.CrunchRiseTime, _tuning.CrunchFallTime);
            CrunchPitch = Mathf.Lerp(_tuning.CrunchPitchSlow, _tuning.CrunchPitchFast, speed);
        }

        private float UpdateCreaks(float deltaTime, in RoverAudioInput input)
        {
            _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
            float creak = 0f;
            if (_pendingStrength > 0f)
            {
                _pendingDelay -= deltaTime;
                if (_pendingDelay <= 0f)
                {
                    creak = _pendingStrength;
                    _pendingStrength = 0f;
                }
            }

            if (creak <= 0f && input.IsGrounded && _wasGrounded && deltaTime > 0f && _cooldown <= 0f)
            {
                float rate = Vector3.Angle(_lastNormal, input.GroundNormal) / deltaTime;
                if (rate >= _tuning.CreakTurnRateMin)
                {
                    float t = Mathf.InverseLerp(_tuning.CreakTurnRateMin, _tuning.CreakTurnRateFull, rate);
                    creak = Mathf.Lerp(_tuning.CreakMinStrength, 1f, t);
                }
            }

            if (creak > 0f)
            {
                _cooldown = _tuning.CreakCooldown;
            }

            _wasGrounded = input.IsGrounded;
            _lastNormal = input.GroundNormal;
            return creak;
        }
    }
}
