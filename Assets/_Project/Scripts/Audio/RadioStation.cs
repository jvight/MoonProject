using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The base's lofi radio. Silent until <see cref="RoverAwoke"/>: then it crackles on with a dial-tuning swish and
    /// static that resolves into the music (quicker when the player woke 07). What it plays comes from
    /// <see cref="IRadioProgram"/> (re-read on <see cref="RadioProgramChanged"/>): Lumen After Dark shuffles the base
    /// tracks plus every owned tape (no immediate repeats, a swish-and-static crossfade between tracks), Tape Deck
    /// loops the chosen cassette, Quiet Hours lets the music and static go and leaves the moon's ambience. Turning
    /// Bell's dial while the radio is on crossfades through a short static swish (Bell crackles along; the detent
    /// click is hers); changes at load or before the radio comes on just set the state, without a sound. New tracks
    /// on Lumen After Dark go to the ticker as "now playing", once per track per session. Clarity follows the rover's
    /// distance from the base through <see cref="RadioSignal"/> (low-pass, static, wow/flutter), and thins further
    /// inside Whispering Canyon (<see cref="CanyonAmbience"/>); <see cref="SignalRadiusChanged"/> widens the clear
    /// zone. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioStation : MonoBehaviour
    {
        private const float MinPitch = 0.01f;
        private const int SubscriptionCount = 3;

        [Tooltip("Assets/_Project/Data/Audio/RadioTuning.asset.")]
        [SerializeField] private RadioTuning _tuning;

        [Tooltip("Assets/_Project/Data/Audio/RadioPlaylist.asset (built from tools/music/playlist.json).")]
        [SerializeField] private RadioPlaylist _playlist;

        [Tooltip("Assets/_Project/Data/Audio/RadioTapes.asset (built from tools/music/tapes.json).")]
        [SerializeField] private RadioTapeLibrary _tapes;

        private readonly AudioSource[] _decks = new AudioSource[RadioDeckMixer.DeckCount];
        private readonly AudioLowPassFilter[] _filters = new AudioLowPassFilter[RadioDeckMixer.DeckCount];
        private readonly RadioTrack[] _deckTracks = new RadioTrack[RadioDeckMixer.DeckCount];
        private readonly RadioDeckMixer _mixer = new RadioDeckMixer();
        private readonly WowFlutter _wowFlutter = new WowFlutter();
        private readonly RadioWakeUp _wake = new RadioWakeUp();
        private readonly NowPlayingLog _nowPlaying = new NowPlayingLog();
        private readonly EasedValue _staticPresence = new EasedValue(1f);
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly Dictionary<string, RadioTrack> _tapeTracks = new Dictionary<string, RadioTrack>(
            StringComparer.Ordinal);
        private IRoverState _listener;
        private IRadioProgram _program;
        private EventBus _events;
        private Vector3 _basePosition;
        private AudioDirector _director;
        private CanyonAmbience _canyon;
        private RadioSignal _signal;
        private RadioProgramModel _model;
        private PlaylistShuffler _shuffler;
        private RadioTrack[] _pool = Array.Empty<RadioTrack>();
        private int _poolCount;
        private AudioSource _static;
        private AudioSource _swish;
        private float _staticCueVolume;
        private float _swishCueVolume;

        /// <summary>True once 07 has started waking and the radio has come on.</summary>
        public bool IsOn => _wake.IsAwake;

        /// <summary>True while any music plays (after the wake-up static; false on Quiet Hours).</summary>
        public bool MusicStarted
        {
            get
            {
                for (int i = 0; i < _decks.Length; i++)
                {
                    if (_decks[i] != null && _decks[i].isPlaying)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>The station heard (Lumen After Dark until Bell's dial is unlocked).</summary>
        public RadioChannel Station => _model != null ? _model.Station : RadioChannel.LumenAfterDark;

        /// <summary>The track on the live deck (null when no music plays).</summary>
        public RadioTrack CurrentTrack => _mixer.Live >= 0 ? _deckTracks[_mixer.Live] : null;

        /// <summary>Tracks in the Lumen After Dark shuffle (base tracks plus owned tapes).</summary>
        public int ShufflePoolCount => _poolCount;

        /// <summary>Smoothed signal clarity the player hears, 0 (lost) .. 1 (clear).</summary>
        public float Clarity => _signal != null ? _signal.Clarity : 0f;

        /// <summary>The clear-signal radius currently in effect (metres, easing towards the target).</summary>
        public float SignalRadius => _signal != null ? _signal.Radius : 0f;

        /// <summary>The clear-signal radius the station is easing towards (metres).</summary>
        public float TargetSignalRadius => _signal != null ? _signal.TargetRadius : 0f;

        internal AudioSource GetDeck(int index) => _decks[index];

        internal RadioDeckMixer Mixer => _mixer;

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
            _canyon = director.Canyon;
            _events = context.Events;
            _listener = context.Get<IRoverState>();
            _program = context.Get<IRadioProgram>();
            _basePosition = context.Get<IWorldLayout>().BasePosition;
            _signal = new RadioSignal(_tuning);
            _signal.Snap(SignalField.HorizontalDistance(_listener.Position, _basePosition));
            BuildTrackTables();

            for (int i = 0; i < _decks.Length; i++)
            {
                _decks[i] = director.CreateLoopSource(transform, $"Deck{i}", default, 0f);
                _decks[i].loop = false;
                _filters[i] = _decks[i].gameObject.AddComponent<AudioLowPassFilter>();
            }

            _static = director.CreateLoopSource(transform, "Static", staticCue, 0f);
            _staticCueVolume = director.Library.GetCue(staticCue).VolumeMax;
            _swish = director.CreateLoopSource(transform, "TuningSwish", swishCue, 0f);
            _swish.loop = false;
            _swishCueVolume = director.Library.GetCue(swishCue).VolumeMax;

            _model = new RadioProgramModel(_program.TotalTapeCount);
            _model.Apply(_program, false, out _);
            RebuildPool();
            _staticPresence.Snap(StaticPresenceTarget());
            _subscriptions[0] = context.Events.Subscribe<SignalRadiusChanged>(OnSignalRadiusChanged);
            _subscriptions[1] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[2] = context.Events.Subscribe<RadioProgramChanged>(OnRadioProgramChanged);
        }

        internal void Wire(RadioTuning tuning, RadioPlaylist playlist, RadioTapeLibrary tapes)
        {
            _tuning = tuning;
            _playlist = playlist;
            _tapes = tapes;
        }

        private void Update()
        {
            if (_signal == null || !_wake.IsAwake)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _signal.Step(SignalField.HorizontalDistance(_listener.Position, _basePosition), dt);
            RadioMix mix = _signal.Mix;
            float wobble = _wowFlutter.Step(dt, _tuning.WowRate, _tuning.FlutterRate, _tuning.FlutterShare);
            float pitch = Mathf.Max(MinPitch, WowFlutter.PitchFactor(wobble, mix.WobbleCents));
            if (_wake.Step(dt))
            {
                StartStationNow();
            }

            AdvanceShow(pitch);
            if (_mixer.Step(dt, out int startDeck))
            {
                StartOn(startDeck);
            }

            float cabin = _director.CabinBlend;
            float level = _director.Buses.Effective(AudioBus.Music) * _wake.Power;
            float music = mix.MusicVolume * level * _wake.MusicGain * Mathf.Lerp(1f, _tuning.CabinMusicGain, cabin) *
                          _canyon.RadioMusicGain;
            float cabinCutoff = _tuning.MaxCutoff * Mathf.Pow(_tuning.CabinCutoff / _tuning.MaxCutoff, cabin);
            float cutoff = _canyon.RadioCutoff(Mathf.Min(mix.CutoffHz, cabinCutoff));
            for (int i = 0; i < _decks.Length; i++)
            {
                AudioSource deck = _decks[i];
                deck.volume = music * _mixer.Level(i);
                deck.pitch = pitch;
                _filters[i].cutoffFrequency = cutoff;
                _filters[i].lowpassResonanceQ = _tuning.LowpassResonance;
                if (deck.isPlaying && _mixer.IsIdle(i))
                {
                    deck.Stop();
                    _deckTracks[i] = null;
                }
            }

            float presence = _staticPresence.Step(StaticPresenceTarget(), dt, _tuning.QuietStaticFade);
            float staticVolume = (mix.StaticVolume + _tuning.WakeStaticBoost * _wake.CrackleBoost) * presence
                                 + _tuning.TuneStaticBoost * _mixer.Swell;
            _static.volume = Mathf.Clamp01(staticVolume) * _staticCueVolume * level *
                             Mathf.Lerp(1f, _tuning.CabinStaticGain, cabin);
            _swish.volume = _tuning.TuneSwishVolume * _swishCueVolume * level;
        }

        private float StaticPresenceTarget()
        {
            return _model.Station == RadioChannel.QuietHours ? _tuning.QuietStaticGain : 1f;
        }

        /// <summary>On Lumen After Dark, moves to the next track a moment before this one ends.</summary>
        private void AdvanceShow(float pitch)
        {
            int live = _mixer.Live;
            if (live < 0 || _mixer.StartPending || _model.Station != RadioChannel.LumenAfterDark ||
                _decks[live].loop || !_decks[live].isPlaying)
            {
                return;
            }

            if (SecondsLeft(_decks[live], pitch) <= _tuning.CrossfadeLead)
            {
                float duration = _tuning.CrossfadeDuration;
                _mixer.Change(true, duration * _tuning.OutgoingFadeEnd, duration * _tuning.IncomingStart,
                    duration * (1f - _tuning.IncomingStart), duration);
                PlaySwish();
            }
        }

        private void OnRadioProgramChanged(RadioProgramChanged changed)
        {
            RadioProgramUpdate update = _model.Apply(_program, _wake.MusicStarted, out bool announce);
            if ((update & RadioProgramUpdate.Pool) != 0)
            {
                RebuildPool();
            }

            if ((update & RadioProgramUpdate.Station) == 0)
            {
                return;
            }

            if (!_wake.MusicStarted)
            {
                // Set before the radio comes on (a loaded save): the static starts where the station wants it.
                _staticPresence.Snap(StaticPresenceTarget());
                return;
            }

            if (announce)
            {
                float duration = _tuning.StationSwitchDuration;
                _mixer.Change(HasMusic(), duration * _tuning.OutgoingFadeEnd, duration * _tuning.IncomingStart,
                    duration * (1f - _tuning.IncomingStart), duration);
                PlaySwish();
                _director.NotifyStationSwitched();
                return;
            }

            float fade = _tuning.SilentSwitchFade;
            _mixer.Change(HasMusic(), fade, 0f, fade, 0f);
        }

        private void StartStationNow()
        {
            if (HasMusic())
            {
                StartOn(_mixer.StartNow(0f));
            }
        }

        private bool HasMusic()
        {
            switch (_model.Station)
            {
                case RadioChannel.LumenAfterDark:
                    return _poolCount > 0;
                case RadioChannel.TapeDeck:
                    return FindTape(_model.SelectedTape) != null;
                default:
                    return false;
            }
        }

        /// <summary>Starts the station's next music on <paramref name="deck"/> (it is the live deck now).</summary>
        private void StartOn(int deck)
        {
            RadioTrack track;
            bool loop;
            if (_model.Station == RadioChannel.TapeDeck)
            {
                track = FindTape(_model.SelectedTape);
                loop = true;
            }
            else if (_model.Station == RadioChannel.LumenAfterDark && _poolCount > 0)
            {
                track = _pool[_shuffler.Next()];
                loop = _poolCount == 1;
            }
            else
            {
                return;
            }

            if (track == null)
            {
                return;
            }

            AudioSource source = _decks[deck];
            source.Stop();
            source.clip = track.Clip;
            source.loop = loop;
            source.timeSamples = 0;
            source.volume = 0f;
            source.Play();
            _deckTracks[deck] = track;
            if (_model.Station == RadioChannel.LumenAfterDark && _nowPlaying.TryAnnounce(track.Id, out string titleKey))
            {
                _events.Publish(new TickerLine(NowPlayingLog.TickerKey, titleKey, true));
            }
        }

        private RadioTrack FindTape(string cassetteId)
        {
            if (string.IsNullOrEmpty(cassetteId))
            {
                return null;
            }

            if (_tapeTracks.TryGetValue(cassetteId, out RadioTrack track))
            {
                return track;
            }

            Debug.LogError($"{nameof(RadioStation)}: no radio track for cassette '{cassetteId}'. Render the music " +
                           "(tools/music) and run MoonProject/Build/Audio/Radio Tapes.", this);
            return null;
        }

        /// <summary>Lumen After Dark's shuffle: the base tracks, then owned tapes in collection order.</summary>
        private void RebuildPool()
        {
            _poolCount = 0;
            for (int i = 0; i < _playlist.Count; i++)
            {
                _pool[_poolCount++] = _playlist.GetTrack(i);
            }

            for (int i = 0; i < _model.OwnedCount; i++)
            {
                RadioTrack tape = FindTape(_model.GetOwned(i));
                if (tape != null && _poolCount < _pool.Length)
                {
                    _pool[_poolCount++] = tape;
                }
            }

            _shuffler.Resize(_poolCount);
            int live = _mixer.Live;
            if (live >= 0 && _model.Station == RadioChannel.LumenAfterDark && _decks[live] != null)
            {
                // A lone track loops; once a tape joins the show it moves on to the next track again.
                _decks[live].loop = _poolCount == 1;
            }
        }

        private void BuildTrackTables()
        {
            int capacity = _playlist.Count + _tapes.Count;
            _pool = new RadioTrack[capacity];
            _shuffler = new PlaylistShuffler(capacity, new AudioRandom(unchecked((uint)Environment.TickCount)));
            for (int i = 0; i < _playlist.Count; i++)
            {
                string id = _playlist.GetTrack(i).Id;
                _nowPlaying.Register(id, NowPlayingLog.TrackTitleKey(id));
            }

            for (int i = 0; i < _tapes.Count; i++)
            {
                RadioTrack tape = _tapes.GetTrack(i);
                _tapeTracks[tape.TapeId] = tape;
                _nowPlaying.Register(tape.Id, NowPlayingLog.CassetteTitleKey(tape.TapeId));
            }
        }

        private void PlaySwish()
        {
            _swish.Stop();
            _swish.Play();
        }

        private void OnSignalRadiusChanged(SignalRadiusChanged changed)
        {
            SetSignalRadius(changed.Radius);
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            if (_wake.IsAwake)
            {
                return;
            }

            _wake.Begin(awoke.WokenByPlayer ? _tuning.PlayerWakeMusicDelay : _tuning.WakeMusicDelay,
                awoke.WokenByPlayer ? _tuning.PlayerWakeMusicFade : _tuning.WakeMusicFade, _tuning.WakePowerTime);
            _static.volume = 0f;
            _static.Play();
            if (_model.Station != RadioChannel.QuietHours)
            {
                // Waking on Quiet Hours stays quiet: no dial swish, and the wake crackle is held down with the static.
                _swish.Play();
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

            if (_playlist == null || _tapes == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: the playlist or the tape library is not assigned.", this);
                return false;
            }

            string problem = _playlist.FindProblem() ?? _tapes.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {problem}.", this);
                ok = false;
            }

            return ok;
        }
    }
}
