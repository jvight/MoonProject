using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The sound of solitude (feel pillar 6, M3-10): one <see cref="SoundscapeModel"/> fed every frame with 07's
    /// distance from the base, the radio's signal edge and station, Whispering Canyon and 07's stillness
    /// (<see cref="StillnessTracker"/>, counted once 07 is awake). The radio, the basin bed, the canyon beds and 07's
    /// small sounds read their gains here, so Quiet Hours, the canyon, distance and stillness make one coherent mix.
    /// Plays the room tone bed that takes over as everything else recedes. Neutral (all gains 1) until initialised.
    /// Initialised by <see cref="AudioDirector"/> after the radio and the canyon.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Soundscape : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/SoundscapeTuning.asset.")]
        [SerializeField] private SoundscapeTuning _tuning;

        private readonly EasedValue _fadeIn = new EasedValue(0f);
        private AudioDirector _director;
        private IRoverState _rover;
        private RadioStation _radio;
        private CanyonAmbience _canyon;
        private Vector3 _basePosition;
        private StillnessTracker _stillness;
        private SoundscapeModel _model;
        private AudioSource _roomTone;
        private IDisposable _awokeSubscription;
        private float _roomToneCueVolume;
        private float _fadeInTime;
        private bool _awake;

        /// <summary>0 moving .. 1 settled into stillness (eased; 0 while 07 sleeps).</summary>
        public float Stillness => _stillness != null ? _stillness.Amount : 0f;

        /// <summary>Seconds 07 has been still since it last moved (0 while 07 sleeps).</summary>
        public float StillSeconds => _stillness != null ? _stillness.StillSeconds : 0f;

        public float Solitude => _model != null ? _model.Solitude : 0f;

        public float Farness => _model != null ? _model.Farness : 0f;

        public float QuietHours => _model != null ? _model.QuietHours : 0f;

        public float RadioGain => _model != null ? _model.RadioGain : 1f;

        public float BasinBedGain => _model != null ? _model.BasinBedGain : 1f;

        public float CanyonBedGain => _model != null ? _model.CanyonBedGain : 1f;

        public float SmallSoundsGain => _model != null ? _model.SmallSoundsGain : 1f;

        internal AudioSource RoomToneSource => _roomTone;

        /// <summary>The radio's low-pass from its open <paramref name="cutoffHz"/> (thinner in the canyon).</summary>
        public float RadioCutoff(float cutoffHz)
        {
            return _model != null ? _model.RadioCutoff(cutoffHz) : cutoffHz;
        }

        internal void Initialize(GameContext context, AudioDirector director, RadioStation radio,
            CanyonAmbience canyon, float fadeInSeconds)
        {
            if (_tuning == null || canyon.Tuning == null)
            {
                Debug.LogError($"{nameof(Soundscape)}: {nameof(_tuning)} or the canyon's tuning is not assigned.",
                    this);
                enabled = false;
                return;
            }

            CueHandle roomTone = director.Resolve(AudioCueIds.RoomTone);
            if (!roomTone.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _radio = radio;
            _canyon = canyon;
            _fadeInTime = fadeInSeconds;
            _rover = context.Get<IRoverState>();
            _basePosition = context.Get<IWorldLayout>().BasePosition;
            _stillness = new StillnessTracker(_tuning);
            _model = new SoundscapeModel(_tuning, canyon.Tuning);
            _roomToneCueVolume = director.Library.GetCue(roomTone).VolumeMax;
            _roomTone = director.CreateLoopSource(transform, "RoomTone", roomTone, 0f);
            _awokeSubscription = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
        }

        internal void Wire(SoundscapeTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_model == null)
            {
                return;
            }

            if (_awake)
            {
                _stillness.Step(_rover.Speed, _rover.DriveInput, _rover.IsGrounded, Time.deltaTime);
            }

            float dt = Time.unscaledDeltaTime;
            float distance = SignalField.HorizontalDistance(_rover.Position, _basePosition);
            _model.Step(distance, _radio.SignalEdge, _stillness.Amount, _canyon.Inside,
                _radio.Station == RadioChannel.QuietHours, dt);

            float fade = _fadeIn.Step(1f, dt, _fadeInTime);
            float volume = Mathf.Clamp01(fade * _model.RoomToneGain * _roomToneCueVolume) *
                           _director.Buses.Effective(AudioBus.Ambience);
            _roomTone.volume = volume;
            if (volume > 0f && !_roomTone.isPlaying)
            {
                _roomTone.Play();
            }
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _awake = true;
        }

        private void OnDestroy()
        {
            _awokeSubscription?.Dispose();
            _awokeSubscription = null;
        }
    }
}
