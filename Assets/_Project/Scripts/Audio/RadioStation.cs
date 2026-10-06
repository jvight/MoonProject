using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The base's lofi radio. Plays the <see cref="RadioPlaylist"/> in shuffled order (no immediate repeats), moving
    /// between tracks with a short "turning the dial" crossfade full of static. Its clarity follows the rover's
    /// distance from the base (<see cref="IWorldLayout.BasePosition"/>) through <see cref="RadioSignal"/>: low clarity
    /// closes a low-pass filter, raises the static and deepens a tape wow/flutter. <see cref="SignalRadiusChanged"/>
    /// (radio tower upgrades, loading a save) widens the clear zone through <see cref="SetSignalRadius"/>.
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioStation : MonoBehaviour
    {
        private const int DeckCount = 2;
        private const float MinPitch = 0.01f;

        [Tooltip("Assets/_Project/Data/Audio/RadioTuning.asset.")]
        [SerializeField] private RadioTuning _tuning;

        [Tooltip("Assets/_Project/Data/Audio/RadioPlaylist.asset (built from tools/music/playlist.json).")]
        [SerializeField] private RadioPlaylist _playlist;

        private readonly AudioSource[] _decks = new AudioSource[DeckCount];
        private readonly AudioLowPassFilter[] _filters = new AudioLowPassFilter[DeckCount];
        private readonly RadioCrossfade _crossfade = new RadioCrossfade();
        private readonly WowFlutter _wowFlutter = new WowFlutter();
        private IRoverState _listener;
        private Vector3 _basePosition;
        private AudioDirector _director;
        private RadioSignal _signal;
        private PlaylistShuffler _shuffler;
        private EasedValue _startFade;
        private AudioSource _static;
        private AudioSource _swish;
        private float _staticCueVolume;
        private float _swishCueVolume;
        private int _live;
        private IDisposable _radiusSubscription;

        /// <summary>Smoothed signal clarity the player hears, 0 (lost) .. 1 (clear).</summary>
        public float Clarity => _signal != null ? _signal.Clarity : 0f;

        /// <summary>The clear-signal radius currently in effect (metres, easing towards the target).</summary>
        public float SignalRadius => _signal != null ? _signal.Radius : 0f;

        /// <summary>The clear-signal radius the station is easing towards (metres).</summary>
        public float TargetSignalRadius => _signal != null ? _signal.TargetRadius : 0f;

        /// <summary>Sets a new clear-signal radius (e.g. after a radio tower upgrade); it blooms in smoothly.</summary>
        public void SetSignalRadius(float radius)
        {
            if (_signal == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {nameof(SetSignalRadius)} called before initialisation.",
                    this);
                return;
            }

            _signal.SetTargetRadius(radius);
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (!HasValidReferences())
            {
                enabled = false;
                return;
            }

            CueHandle staticCue = director.Resolve(AudioCueIds.RadioStatic);
            CueHandle swishCue = director.Resolve(AudioCueIds.RadioTune);
            if (!staticCue.IsValid || !swishCue.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _listener = context.Get<IRoverState>();
            _basePosition = context.Get<IWorldLayout>().BasePosition;
            _signal = new RadioSignal(_tuning);
            _signal.Snap(SignalField.HorizontalDistance(_listener.Position, _basePosition));
            _shuffler = new PlaylistShuffler(_playlist.Count, new AudioRandom(unchecked((uint)Environment.TickCount)));
            _startFade = new EasedValue(0f);

            for (int i = 0; i < DeckCount; i++)
            {
                _decks[i] = director.CreateLoopSource(transform, i == 0 ? "DeckA" : "DeckB", default, 0f);
                _decks[i].loop = false;
                _filters[i] = _decks[i].gameObject.AddComponent<AudioLowPassFilter>();
            }

            _static = director.CreateLoopSource(transform, "Static", staticCue, 0f);
            _staticCueVolume = director.Library.GetCue(staticCue).VolumeMax;
            _swish = director.CreateLoopSource(transform, "TuningSwish", swishCue, 0f);
            _swish.loop = false;
            _swishCueVolume = director.Library.GetCue(swishCue).VolumeMax;

            _live = 0;
            StartTrack(_decks[_live], _shuffler.Next());
            // A single track simply loops; the crossfade needs a different track to move to.
            _decks[_live].loop = _playlist.Count == 1;
            _static.Play();
            _radiusSubscription = context.Events.Subscribe<SignalRadiusChanged>(OnSignalRadiusChanged);
        }

        internal void Wire(RadioTuning tuning, RadioPlaylist playlist)
        {
            _tuning = tuning;
            _playlist = playlist;
        }

        private void Update()
        {
            if (_signal == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _signal.Step(SignalField.HorizontalDistance(_listener.Position, _basePosition), dt);
            RadioMix mix = _signal.Mix;
            float wobble = _wowFlutter.Step(dt, _tuning.WowRate, _tuning.FlutterRate, _tuning.FlutterShare);
            float pitch = Mathf.Max(MinPitch, WowFlutter.PitchFactor(wobble, mix.WobbleCents));
            float level = _director.Buses.Effective(AudioBus.Music) * _startFade.Step(1f, dt, _tuning.StartFadeIn);

            AdvancePlaylist(dt, pitch);

            AudioSource live = _decks[_live];
            AudioSource other = _decks[1 - _live];
            float music = mix.MusicVolume * level;
            if (_crossfade.Active)
            {
                live.volume = music * _crossfade.OutgoingGain;
                other.volume = music * _crossfade.IncomingGain;
            }
            else
            {
                live.volume = music;
                other.volume = 0f;
            }

            for (int i = 0; i < DeckCount; i++)
            {
                _decks[i].pitch = pitch;
                _filters[i].cutoffFrequency = mix.CutoffHz;
                _filters[i].lowpassResonanceQ = _tuning.LowpassResonance;
            }

            float staticVolume = mix.StaticVolume + _tuning.TuneStaticBoost * _crossfade.StaticSwell;
            _static.volume = Mathf.Clamp01(staticVolume) * _staticCueVolume * level;
            _swish.volume = _tuning.TuneSwishVolume * _swishCueVolume * level;
        }

        private void OnSignalRadiusChanged(SignalRadiusChanged changed)
        {
            SetSignalRadius(changed.Radius);
        }

        private void OnDestroy()
        {
            _radiusSubscription?.Dispose();
            _radiusSubscription = null;
        }

        private void AdvancePlaylist(float dt, float pitch)
        {
            if (_playlist.Count < 2)
            {
                return;
            }

            if (!_crossfade.Active && SecondsLeft(_decks[_live], pitch) <= _tuning.CrossfadeLead)
            {
                _crossfade.Begin(_tuning.CrossfadeDuration, _tuning.OutgoingFadeEnd, _tuning.IncomingStart);
                _swish.Stop();
                _swish.Play();
            }

            if (!_crossfade.Active)
            {
                return;
            }

            if (_crossfade.Step(dt))
            {
                StartTrack(_decks[1 - _live], _shuffler.Next());
            }

            if (!_crossfade.Active)
            {
                _decks[_live].Stop();
                _live = 1 - _live;
            }
        }

        private void StartTrack(AudioSource deck, int track)
        {
            deck.clip = _playlist.GetTrack(track).Clip;
            deck.timeSamples = 0;
            deck.volume = 0f;
            deck.Play();
        }

        private static float SecondsLeft(AudioSource deck, float pitch)
        {
            AudioClip clip = deck.clip;
            if (clip == null || !deck.isPlaying)
            {
                return 0f;
            }

            return (clip.samples - deck.timeSamples) / (float)clip.frequency / pitch;
        }

        private bool HasValidReferences()
        {
            bool ok = true;
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {nameof(_tuning)} is not assigned.", this);
                ok = false;
            }

            if (_playlist == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {nameof(_playlist)} is not assigned.", this);
                return false;
            }

            string problem = _playlist.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {problem}.", _playlist);
                ok = false;
            }

            return ok;
        }
    }
}
