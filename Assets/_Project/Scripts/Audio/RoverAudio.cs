using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The rover's own sounds, following <see cref="IRoverState"/> every frame: an electric hum (pitch/volume from
    /// speed and throttle), dust crunch under the wheels while grounded, and soft suspension creaks on landings and
    /// bumps. All logic lives in <see cref="RoverAudioModel"/>; this component only moves the emitter and applies
    /// values. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverAudio : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/RoverAudioTuning.asset.")]
        [SerializeField] private RoverAudioTuning _tuning;

        private IRoverState _rover;
        private AudioDirector _director;
        private RoverAudioModel _model;
        private AudioSource _hum;
        private AudioSource _crunch;
        private CueHandle _creak;
        private float _humCueVolume;
        private float _crunchCueVolume;
        private IDisposable _landedSubscription;

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
            if (!hum.IsValid || !crunch.IsValid || !_creak.IsValid)
            {
                enabled = false;
                return;
            }

            _humCueVolume = director.Library.GetCue(hum).VolumeMax;
            _crunchCueVolume = director.Library.GetCue(crunch).VolumeMax;
            transform.SetPositionAndRotation(_rover.Position, _rover.Rotation);
            _hum = director.CreateLoopSource(transform, "Hum", hum, _tuning.LoopSpatialBlend);
            _crunch = director.CreateLoopSource(transform, "DustCrunch", crunch, _tuning.LoopSpatialBlend);
            // Start the loops at unrelated points so their textures never line up.
            _crunch.timeSamples = _crunch.clip.samples / 2;
            _hum.Play();
            _crunch.Play();
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnRoverLanded);
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
        }

        private void OnRoverLanded(RoverLanded landed)
        {
            _model?.NotifyLanding(landed.ImpactSpeed);
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
            _landedSubscription = null;
        }
    }
}
