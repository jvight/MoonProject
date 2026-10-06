using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The rover's own sounds, following <see cref="IRoverState"/> every frame: an electric hum (silent until
    /// <see cref="RoverAwoke"/>, then pitch/volume from speed and throttle), dust crunch under the wheels while
    /// grounded, soft suspension creaks on landings and bumps, and the servo lift + settle of a
    /// <see cref="RoverRecovering"/>. Logic lives in <see cref="RoverAudioModel"/> and <see cref="RecoveryLift"/>;
    /// this component only moves the emitter and applies values. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverAudio : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/RoverAudioTuning.asset.")]
        [SerializeField] private RoverAudioTuning _tuning;

        private IRoverState _rover;
        private AudioDirector _director;
        private RoverAudioModel _model;
        private readonly RecoveryLift _lift = new RecoveryLift();
        private AudioSource _liftLoop;
        private CueHandle _settle;
        private float _liftCueVolume;
        private AudioSource _hum;
        private AudioSource _crunch;
        private CueHandle _creak;
        private float _humCueVolume;
        private float _crunchCueVolume;
        private IDisposable _landedSubscription;
        private IDisposable _awokeSubscription;
        private IDisposable _recoveringSubscription;

        /// <summary>True while 07 is awake and its motor hum can play.</summary>
        public bool IsAwake => _model != null && _model.IsAwake;

        /// <summary>True while a recovery lift is sounding.</summary>
        public bool Lifting => _lift.Active;

        internal AudioSource HumSource => _hum;

        internal AudioSource LiftSource => _liftLoop;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(RoverAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _rover = context.Get<IRoverState>();
            _director = director;
            _model = new RoverAudioModel(_tuning);
            CueHandle hum = director.Resolve(AudioCueIds.RoverHum);
            CueHandle crunch = director.Resolve(AudioCueIds.DustCrunch);
            _creak = director.Resolve(AudioCueIds.SuspensionCreak);
            CueHandle lift = director.Resolve(AudioCueIds.RecoveryLift);
            _settle = director.Resolve(AudioCueIds.RecoverySettle);
            if (!hum.IsValid || !crunch.IsValid || !_creak.IsValid || !lift.IsValid || !_settle.IsValid)
            {
                enabled = false;
                return;
            }

            _humCueVolume = director.Library.GetCue(hum).VolumeMax;
            _crunchCueVolume = director.Library.GetCue(crunch).VolumeMax;
            transform.SetPositionAndRotation(_rover.Position, _rover.Rotation);
            _hum = director.CreateLoopSource(transform, "Hum", hum, _tuning.LoopSpatialBlend);
            _crunch = director.CreateLoopSource(transform, "DustCrunch", crunch, _tuning.LoopSpatialBlend);
            _liftLoop = director.CreateLoopSource(transform, "RecoveryLift", lift, _tuning.LoopSpatialBlend);
            _liftCueVolume = director.Library.GetCue(lift).VolumeMax;
            // Start the loops at unrelated points so their textures never line up.
            _crunch.timeSamples = _crunch.clip.samples / 2;
            _hum.Play();
            _crunch.Play();
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnRoverLanded);
            _awokeSubscription = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _recoveringSubscription = context.Events.Subscribe<RoverRecovering>(OnRoverRecovering);
        }

        internal void Wire(RoverAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_model == null)
            {
                return;
            }

            transform.SetPositionAndRotation(_rover.Position, _rover.Rotation);
            var input = new RoverAudioInput(_rover.NormalizedSpeed, Mathf.Abs(_rover.DriveInput.y), _rover.IsGrounded,
                _rover.GroundNormal);
            float creak = _model.Step(Time.deltaTime, input);
            float sfx = _director.Buses.Effective(AudioBus.Sfx);
            _hum.pitch = _model.HumPitch;
            _hum.volume = _model.HumVolume * _humCueVolume * sfx;
            _crunch.pitch = _model.CrunchPitch;
            _crunch.volume = _model.CrunchVolume * _crunchCueVolume * sfx;
            if (creak > 0f)
            {
                _director.PlayAt(_creak, _rover.Position, creak);
            }

            UpdateLift(sfx);
        }

        private void UpdateLift(float sfx)
        {
            if (!_lift.Active)
            {
                return;
            }

            bool landed = _lift.Step(Time.deltaTime);
            _liftLoop.volume = _lift.Gain * _liftCueVolume * _tuning.LiftVolume * sfx;
            _liftLoop.pitch = _lift.PitchFactor(_tuning.LiftRiseSemitones);
            if (landed)
            {
                _liftLoop.Stop();
                _director.PlayAt(_settle, _rover.Position, _tuning.SettleVolume);
            }
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _model?.NotifyAwoke();
        }

        private void OnRoverRecovering(RoverRecovering recovering)
        {
            if (_model == null)
            {
                return;
            }

            _lift.Begin(recovering.Duration, _tuning.LiftFadeIn, _tuning.LiftFadeOut);
            _liftLoop.volume = 0f;
            _liftLoop.pitch = 1f;
            _liftLoop.timeSamples = 0;
            _liftLoop.Play();
        }

        private void OnRoverLanded(RoverLanded landed)
        {
            _model?.NotifyLanding(landed.ImpactSpeed);
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
            _landedSubscription = null;
            _awokeSubscription?.Dispose();
            _awokeSubscription = null;
            _recoveringSubscription?.Dispose();
            _recoveringSubscription = null;
        }
    }
}
