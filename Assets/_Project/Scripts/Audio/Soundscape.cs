using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The sound of solitude (feel pillar 6, M3-10): one <see cref="SoundscapeModel"/> fed every frame with 07's
    /// place in the relay network (<see cref="RadioStation.ReachDistance"/>: home and every lit mast keep it close),
    /// the radio's signal edge and station, Whispering Canyon, 07's stillness (the Rover's
    /// <see cref="IRoverStillness"/> through <see cref="StillnessTracker"/>, counted once 07 is awake) and the
    /// camera's wide shot (<see cref="RoverWideShotChanged"/>: the mix breathes out over the frame's opening and back
    /// in with the hand-back). The radio, the basin bed, the canyon beds and 07's small sounds read their gains here,
    /// so Quiet Hours, the canyon, distance, stillness and the wide shot make one coherent mix.
    /// Plays the room tone bed that takes over as everything else recedes. Neutral (all gains 1) until initialised.
    /// Initialised by <see cref="AudioDirector"/> after the radio and the canyon.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Soundscape : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/SoundscapeTuning.asset.")]
        [SerializeField] private SoundscapeTuning _tuning;

        private const int SubscriptionCount = 3;

        private readonly EasedValue _fadeIn = new EasedValue(0f);
        private readonly LoopFader _wide = new LoopFader();
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private IRoverState _rover;
        private IRoverStillness _rest;
        private RadioStation _radio;
        private CanyonAmbience _canyon;
        private StillnessTracker _stillness;
        private SoundscapeModel _model;
        private AudioSource _roomTone;
        private float _roomToneCueVolume;
        private float _fadeInTime;
        private bool _awake;

        /// <summary>0 moving .. 1 settled into stillness (eased; 0 while 07 sleeps).</summary>
        public float Stillness => _stillness != null ? _stillness.Amount : 0f;

        /// <summary>The camera's wide shot as the mix hears it, 0 closed .. 1 fully open (eased).</summary>
        public float Wide => _wide.Gain;

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
            _rest = context.Get<IRoverStillness>();
            _stillness = new StillnessTracker(_tuning);
            _model = new SoundscapeModel(_tuning, canyon.Tuning);
            _roomToneCueVolume = director.Library.GetCue(roomTone).VolumeMax;
            _roomTone = director.CreateLoopSource(transform, "RoomTone", roomTone, 0f);
            _subscriptions[0] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[1] = context.Events.Subscribe<RoverWideShotChanged>(OnWideShotChanged);
            _subscriptions[2] = context.Events.Subscribe<RoverPlaced>(OnRoverPlaced);
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

            _stillness.Step(_awake ? _rest.StillSeconds : 0f, Time.deltaTime);
            float dt = Time.unscaledDeltaTime;
            _wide.Step(dt, _tuning.WideOpenTime, _tuning.WideReleaseTime);
            float distance = _radio.ReachDistance(_rover.Position);
            _model.Step(distance, _radio.SignalEdge, _stillness.Amount, _wide.Gain, _canyon.Inside,
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

        private void OnRoverPlaced(RoverPlaced placed)
        {
            // A radio-hop sets 07 down at full dark: the new place's distance holds at once, not eased in.
            _model.SettleAt(_radio.ReachDistance(placed.Position), _radio.SignalEdge);
        }

        private void OnWideShotChanged(RoverWideShotChanged changed)
        {
            if (changed.Wide)
            {
                _wide.FadeIn();
            }
            else
            {
                _wide.FadeOut();
            }
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
