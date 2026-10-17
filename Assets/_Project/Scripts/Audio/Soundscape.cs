using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The soundscape of solitude (feel pillar 6): reads 07's distance from the base against the radio's signal
    /// edge, 07's stillness (the Rover's <see cref="IRoverStillness"/>) and the camera's wide shot
    /// (<see cref="RoverWideShotChanged"/>), and turns them into one coherent mix through
    /// <see cref="SoundscapeModel"/>. The radio, the basin bed and 07's small sounds read their gains here; the room
    /// tone, the space around 07, is played here. While 07 stargazes (<see cref="StargazingChanged"/>) the music thins
    /// and a soft swell plays (<see cref="StargazeMix"/>). Initialised by <see cref="AudioDirector"/> after the radio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Soundscape : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/SoundscapeTuning.asset.")]
        [SerializeField] private SoundscapeTuning _tuning;

        private const int SubscriptionCount = 4;

        private readonly EasedValue _fadeIn = new EasedValue(0f);
        private readonly LoopFader _wide = new LoopFader();
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private IRoverState _rover;
        private IRoverStillness _rest;
        private RadioStation _radio;
        private StillnessTracker _stillness;
        private SoundscapeModel _model;
        private StargazeMix _stargaze;
        private CueHandle _swell;
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

        public float RadioGain => _model != null ? _model.RadioGain : 1f;

        /// <summary>The music's own gain on top of <see cref="RadioGain"/>: thinned while 07 stargazes.</summary>
        public float StargazeMusicGain => _stargaze != null ? _stargaze.MusicGain : 1f;

        /// <summary>0 normal .. 1 the music fully thinned for stargazing (eased).</summary>
        public float Stargaze => _stargaze != null ? _stargaze.Amount : 0f;

        public float BasinBedGain => _model != null ? _model.BasinBedGain : 1f;

        public float SmallSoundsGain => _model != null ? _model.SmallSoundsGain : 1f;

        internal AudioSource RoomToneSource => _roomTone;

        /// <summary>The radio's low-pass from its open <paramref name="cutoffHz"/> (thinner while 07 stargazes).
        /// </summary>
        public float RadioCutoff(float cutoffHz)
        {
            return _stargaze != null ? _stargaze.MusicCutoff(cutoffHz) : cutoffHz;
        }

        internal void Initialize(GameContext context, AudioDirector director, RadioStation radio, float fadeInSeconds)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(Soundscape)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CueHandle roomTone = director.Resolve(AudioCueIds.RoomTone);
            _swell = director.Resolve(AudioCueIds.StargazeSwell);
            if (!roomTone.IsValid || !_swell.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _radio = radio;
            _fadeInTime = fadeInSeconds;
            _rover = context.Get<IRoverState>();
            _rest = context.Get<IRoverStillness>();
            _stillness = new StillnessTracker(_tuning);
            _model = new SoundscapeModel(_tuning);
            _stargaze = new StargazeMix(_tuning);
            _roomToneCueVolume = director.Library.GetCue(roomTone).VolumeMax;
            _roomTone = director.CreateLoopSource(transform, "RoomTone", roomTone, 0f);
            _subscriptions[0] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[1] = context.Events.Subscribe<RoverWideShotChanged>(OnWideShotChanged);
            _subscriptions[2] = context.Events.Subscribe<RoverPlaced>(OnRoverPlaced);
            _subscriptions[3] = context.Events.Subscribe<StargazingChanged>(OnStargazingChanged);
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
            _stargaze.Step(dt);
            float distance = _radio.ReachDistance(_rover.Position);
            _model.Step(distance, _radio.SignalEdge, _stillness.Amount, _wide.Gain, dt);

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
            // A recovery sets 07 down unseen: the new place's distance holds at once, not eased in.
            _model.SettleAt(_radio.ReachDistance(placed.Position), _radio.SignalEdge);
        }

        private void OnStargazingChanged(StargazingChanged changed)
        {
            if (_stargaze.SetGazing(changed.IsStargazing))
            {
                _director.Play2D(_swell, _tuning.StargazeSwellVolume);
            }
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
