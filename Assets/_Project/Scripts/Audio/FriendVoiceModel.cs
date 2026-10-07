using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Pure sound logic of one friend: rotor loop pitch/volume eased from its effort (silent when stopped); the repair
    /// stitching, heard while the beam stitches (repairing with the rotors still) and rising over the stitch time;
    /// the boot, due the moment the rotors start turning during the repair (its eye flickers on then), unless the
    /// friend ends its stitching with a cue of its own (<see cref="FinishStitching"/>); and
    /// occasional chirps of its own by activity (curious when out, happy at home, sleepy when napping, never while
    /// dormant or being repaired).
    /// </summary>
    public sealed class FriendVoiceModel
    {
        private const float CentsPerSemitone = 100f;
        private const float CentsPerOctave = 1200f;

        // The rotor fades in over the first tenth of effort, so lifting off never pops in at the low volume.
        private const float LiftOffEffort = 0.1f;

        // Below this effort the winding-down rotor is inaudible (lift-off ramp x low volume < -40 dB): stop it.
        private const float SilentEffort = 0.01f;

        private readonly FriendAudioTuning _tuning;
        private readonly AudioRandom _random;
        private readonly EasedValue _rotorEffort = new EasedValue(0f);
        private readonly LoopFader _stitch = new LoopFader();
        private FriendActivity _activity = FriendActivity.Dormant;
        private float _ambientTimer;
        private float _stitchTime;
        private bool _booted;
        private bool _bootDue;
        private bool _stitchFinished;

        public FriendVoiceModel(FriendAudioTuning tuning, AudioRandom random)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public float RotorVolume => _rotorEffort.Value <= 0f
            ? 0f
            : Mathf.Lerp(_tuning.RotorLowVolume, _tuning.RotorHighVolume, _rotorEffort.Value) *
              Mathf.Clamp01(_rotorEffort.Value / LiftOffEffort);

        public float RotorPitch => Mathf.Lerp(_tuning.RotorIdlePitch, _tuning.RotorDashPitch, _rotorEffort.Value);

        public bool StitchAudible => _stitch.IsAudible;

        public float StitchGain => _stitch.Gain * Mathf.Lerp(_tuning.StitchStartGain, 1f, StitchRise);

        public float StitchPitch =>
            Mathf.Pow(2f, StitchRise * _tuning.StitchRiseSemitones * CentsPerSemitone / CentsPerOctave);

        private float StitchRise => Mathf.SmoothStep(0f, 1f, _stitchTime / _tuning.StitchRiseTime);

        /// <summary>True once when the boot is due (the rotors just started during a repair); clears it.</summary>
        public bool TakeBoot()
        {
            bool due = _bootDue;
            _bootDue = false;
            return due;
        }

        /// <summary>
        /// The repair moved on from the beam with a moment of the friend's own (Bell's tape sliding into its slot):
        /// the stitching fades now, and that moment stands in for the shared boot. Holds until the repair ends.
        /// </summary>
        public void FinishStitching()
        {
            _stitchFinished = true;
            _booted = true;
            _bootDue = false;
        }

        /// <summary>Advances one frame of game time; returns true when a chirp of <paramref name="mood"/> is
        /// due.</summary>
        public bool Step(float deltaTime, FriendActivity activity, float rotorSpeed, out FriendMood mood)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float effort = Mathf.Clamp01(rotorSpeed);
            _rotorEffort.Step(effort, dt, _tuning.RotorSpinUpTime, _tuning.RotorSpinDownTime);
            if (effort <= 0f && _rotorEffort.Value < SilentEffort)
            {
                _rotorEffort.Snap(0f);
            }

            if (activity != FriendActivity.Repairing)
            {
                _stitchFinished = false;
            }

            bool stitching = activity == FriendActivity.Repairing && effort <= 0f && !_stitchFinished;
            if (stitching)
            {
                if (!_stitch.IsOn)
                {
                    _stitchTime = 0f;
                    _booted = false;
                }

                _stitch.FadeIn();
                _stitchTime += dt;
            }
            else
            {
                _stitch.FadeOut();
                if (activity == FriendActivity.Repairing && !_booted)
                {
                    _booted = true;
                    _bootDue = true;
                }
            }

            _stitch.Step(dt, _tuning.StitchFadeIn, _tuning.StitchFadeOut);
            if (activity != _activity)
            {
                _activity = activity;
                _ambientTimer = NextGap(activity);
            }

            mood = FriendMood.Curious;
            if (!TryAmbientMood(activity, out FriendMood ambient))
            {
                return false;
            }

            _ambientTimer -= dt;
            if (_ambientTimer > 0f)
            {
                return false;
            }

            _ambientTimer = NextGap(activity);
            mood = ambient;
            return true;
        }

        private static bool TryAmbientMood(FriendActivity activity, out FriendMood mood)
        {
            switch (activity)
            {
                case FriendActivity.Following:
                case FriendActivity.Spotting:
                    mood = FriendMood.Curious;
                    return true;
                case FriendActivity.Home:
                    mood = FriendMood.Happy;
                    return true;
                case FriendActivity.Napping:
                    mood = FriendMood.Sleepy;
                    return true;
                default:
                    mood = FriendMood.Curious;
                    return false;
            }
        }

        private float NextGap(FriendActivity activity)
        {
            switch (activity)
            {
                case FriendActivity.Following:
                case FriendActivity.Spotting:
                    return _random.Range(_tuning.FollowingChirpMin, _tuning.FollowingChirpMax);
                case FriendActivity.Home:
                    return _random.Range(_tuning.HomeChirpMin, _tuning.HomeChirpMax);
                case FriendActivity.Napping:
                    return _random.Range(_tuning.NappingChirpMin, _tuning.NappingChirpMax);
                default:
                    return 0f;
            }
        }
    }
}
