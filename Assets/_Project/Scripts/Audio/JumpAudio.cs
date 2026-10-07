using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The Hover-Jump's sounds, on a voice that follows 07: the charge hum climbing D major pentatonic with each
    /// <see cref="RoverJumpCharged"/> step (<see cref="JumpChargeModel"/>); on <see cref="RoverJumped"/> a soft boing +
    /// whoosh (a hop for taps, the leap for strong charges) and the coils' twang; rushing air while airborne
    /// (<see cref="AirWindModel"/>); and a cushioned landing instead of the hard thump after a leap (the director asks
    /// <see cref="TryCushionLanding"/> first). A charge that ends without a leap (<see cref="RoverJumpCancelled"/>)
    /// fades away softly.
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JumpAudio : MonoBehaviour
    {
        private const int HopVariant = 0;
        private const int LeapVariant = 1;
        private const int SubscriptionCount = 3;

        [Tooltip("Assets/_Project/Data/Audio/JumpAudioTuning.asset.")]
        [SerializeField] private JumpAudioTuning _tuning;

        private readonly JumpChargeModel _charge = new JumpChargeModel();
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private IRoverState _rover;
        private AirWindModel _wind;
        private AudioSource _voice;
        private AudioSource _chargeLoop;
        private AudioSource _windLoop;
        private CueHandle _leap;
        private CueHandle _twang;
        private CueHandle _land;
        private float _chargeCueVolume;
        private float _windCueVolume;
        private bool _leaping;

        /// <summary>True from a leap until its landing has been voiced.</summary>
        public bool Leaping => _leaping;

        internal AudioSource ChargeSource => _chargeLoop;

        internal AudioSource WindSource => _windLoop;

        internal JumpChargeModel Charge => _charge;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(JumpAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            _wind = new AirWindModel(_tuning);
            CueHandle charge = director.Resolve(AudioCueIds.JumpCharge);
            CueHandle wind = director.Resolve(AudioCueIds.AirWind);
            _leap = director.Resolve(AudioCueIds.JumpLeap);
            _twang = director.Resolve(AudioCueIds.CoilTwang);
            _land = director.Resolve(AudioCueIds.JumpLand);
            if (!charge.IsValid || !wind.IsValid || !_leap.IsValid || !_twang.IsValid || !_land.IsValid)
            {
                enabled = false;
                return;
            }

            transform.position = _rover.Position;
            _voice = director.CreateLoopSource(transform, "Voice", default, 1f);
            _voice.loop = false;
            _chargeLoop = director.CreateLoopSource(transform, "ChargeHum", charge, 1f);
            _windLoop = director.CreateLoopSource(transform, "AirWind", wind, 1f);
            _chargeCueVolume = director.Library.GetCue(charge).VolumeMax;
            _windCueVolume = director.Library.GetCue(wind).VolumeMax;
            _subscriptions[0] = context.Events.Subscribe<RoverJumpCharged>(OnCharged);
            _subscriptions[1] = context.Events.Subscribe<RoverJumped>(OnJumped);
            _subscriptions[2] = context.Events.Subscribe<RoverJumpCancelled>(OnCancelled);
        }

        internal void Wire(JumpAudioTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>Voices a landing that ends a leap with the soft cushion (scaled by impact) and returns true; any
        /// other landing returns false so the usual thump plays.</summary>
        internal bool TryCushionLanding(RoverLanded landed)
        {
            if (!_leaping || _director == null)
            {
                return false;
            }

            _leaping = false;
            if (landed.ImpactSpeed >= _tuning.CushionMinImpact)
            {
                float t = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(_tuning.CushionMinImpact, _tuning.CushionFullImpact, landed.ImpactSpeed));
                _director.PlayAt(_land, landed.Position,
                    Mathf.Lerp(_tuning.CushionSoftVolume, _tuning.CushionHardVolume, t));
            }

            return true;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            transform.position = _rover.Position;
            _charge.Step(dt, _tuning.ChargeFadeIn, _tuning.ChargeGlide);
            _wind.Step(dt, _rover.IsGrounded, _rover.AirTime, _rover.Velocity.magnitude);
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            Drive(_chargeLoop, _charge.Gain(_tuning.ChargeStartVolume) * _chargeCueVolume * sfx, _charge.Pitch);
            Drive(_windLoop, _wind.Gain * _windCueVolume * sfx, _wind.Pitch);
        }

        private static void Drive(AudioSource loop, float volume, float pitch)
        {
            loop.volume = volume;
            loop.pitch = pitch;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
        }

        private void OnCharged(RoverJumpCharged charged)
        {
            _charge.Charge(charged.Strength);
        }

        private void OnCancelled(RoverJumpCancelled cancelled)
        {
            _charge.Cancel(_tuning.ChargeCancelFade);
        }

        private void OnJumped(RoverJumped jumped)
        {
            float strength = Mathf.Clamp01(jumped.Strength);
            _charge.Leap(_tuning.ChargeLeapFade);
            _leaping = true;
            int variant = strength < _tuning.HopThreshold ? HopVariant : LeapVariant;
            _director.PlayOn(_voice, _leap, Mathf.Lerp(_tuning.LeapMinVolume, 1f, strength), variant);
            _director.PlayOn(_voice, _twang, Mathf.Lerp(_tuning.TwangMinVolume, 1f, strength));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
