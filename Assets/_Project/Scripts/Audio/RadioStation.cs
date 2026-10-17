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
    /// place in the relay network through <see cref="RadioSignal"/> (low-pass, static, wow/flutter): clear anywhere
    /// home or a lit mast reaches (<see cref="StationDistance"/>), falling off past the nearest edge; beyond, the whole
    /// set fades towards near-silence, it thins in Whispering Canyon and pulls back when 07 is still (the
    /// <see cref="Soundscape"/>'s mix). <see cref="SignalRadiusChanged"/> widens home's clear zone. A radio-hop
    /// (<see cref="RadioHopStarted"/>) eases the set into static as the screen fades, holds through the dark and
    /// resolves into the target node's clarity as the view eases in (<see cref="RadioHop"/>). Music is earned
    /// (<see cref="RadioGate"/>, docs/features/M3-24): a fresh game hears only static, which tower level 1 clears into
    /// the Earth signal's carrier (no music); 07's tape deck (<see cref="RoverSocket.Deck"/>) plays the first
    /// tape softly on repeat, eased in at the fit and rounded like an old cassette; Bell's dial brings the station
    /// with a dial turn. A voiced recording ducks the music (<see cref="RadioVoice"/>). Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioStation : MonoBehaviour
    {
        private const float MinPitch = 0.01f;
        private const int SubscriptionCount = 13;

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
        private bool _openingHoldsRadio;
        private readonly NowPlayingLog _nowPlaying = new NowPlayingLog();
        private readonly EasedValue _staticPresence = new EasedValue(1f);
        private readonly RadioHop _hop = new RadioHop();
        private readonly RadioGate _gate = new RadioGate();
        private readonly EasedValue _deckShare = new EasedValue(0f);
        private readonly EasedValue _carrierPresence = new EasedValue(0f);
        private readonly EasedValue _eveningDuck = new EasedValue(1f);
        private readonly EasedValue _buildDuck = new EasedValue(1f);
        private bool _building;
        private float _buildLeft;
        private float _eveningLeft;
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly Dictionary<string, RadioTrack> _tapeTracks = new Dictionary<string, RadioTrack>(
            StringComparer.Ordinal);
        private IRoverState _listener;
        private IStationReach _reach;
        private IRadioProgram _program;
        private EventBus _events;
        private AudioDirector _director;
        private Soundscape _soundscape;
        private RadioSignal _signal;
        private bool _crawling;
        private RadioProgramModel _model;
        private PlaylistShuffler _shuffler;
        private RadioTrack[] _pool = Array.Empty<RadioTrack>();
        private int _poolCount;
        private AudioSource _static;
        private AudioSource _swish;
        private AudioSource _carrier;
        private RadioVoice _voice;
        private CueHandle _hopOut;
        private CueHandle _hopIn;
        private float _staticCueVolume;
        private float _swishCueVolume;
        private float _carrierCueVolume;

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

        /// <summary>What the radio may play yet (static, 07's deck, or the station).</summary>
        public RadioGateStage GateStage => _gate.Stage;

        /// <summary>The Earth signal's carrier level, 0 (not heard) .. 1 (fully in).</summary>
        public float CarrierPresence => _carrierPresence.Value;

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

        /// <summary>Where the signal is lost entirely: the clear radius in effect plus the falloff (metres).</summary>
        public float SignalEdge => _signal != null ? _signal.Radius + _tuning.FalloffWidth : 0f;

        /// <summary>0 .. 1 how far into a radio-hop's static the set is.</summary>
        public float HopAmount => _hop.Amount;

        /// <summary>The home-relative distance giving the radio's signal at <paramref name="position"/>: home's
        /// clear radius plus how far it is past the nearest lit node's reach (see <see cref="StationDistance"/>).
        /// </summary>
        public float ReachDistance(Vector3 position)
        {
            return StationDistance.Equivalent(_reach, position, SignalRadius);
        }

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
            CueHandle carrierCue = director.Resolve(AudioCueIds.RadioCarrier);
            _hopOut = director.Resolve(AudioCueIds.RadioHopOut);
            _hopIn = director.Resolve(AudioCueIds.RadioHopIn);
            if (!staticCue.IsValid || !swishCue.IsValid || !carrierCue.IsValid || !_hopOut.IsValid || !_hopIn.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _voice = director.Voice;
            _soundscape = director.Soundscape;
            _events = context.Events;
            _listener = context.Get<IRoverState>();
            _program = context.Get<IRadioProgram>();
            _reach = context.Get<IStationReach>();
            _signal = new RadioSignal(_tuning);
            _signal.Snap(ReachDistance(_listener.Position));
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
            _carrier = director.CreateLoopSource(transform, "Carrier", carrierCue, 0f);
            _carrierCueVolume = director.Library.GetCue(carrierCue).VolumeMax;

            _model = new RadioProgramModel(_program.TotalTapeCount);
            _model.Apply(_program, false, out _);
            _gate.SetDialUnlocked(_model.DialUnlocked);
            if (context.Get<IRoverEquipment>().TryGetFitted(RoverSocket.Deck, out _))
            {
                // A loaded game's deck is already on 07 (nothing is published on load): its tape simply plays.
                _gate.FitDeck();
                _deckShare.Snap(_gate.Stage == RadioGateStage.Deck ? 1f : 0f);
            }
            RebuildPool();
            _staticPresence.Snap(StaticPresenceTarget());
            _subscriptions[0] = context.Events.Subscribe<SignalRadiusChanged>(OnSignalRadiusChanged);
            _subscriptions[1] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[2] = context.Events.Subscribe<RadioProgramChanged>(OnRadioProgramChanged);
            _subscriptions[3] = context.Events.Subscribe<RadioHopStarted>(OnRadioHopStarted);
            _subscriptions[4] = context.Events.Subscribe<RadioHopFinished>(OnRadioHopFinished);
            _subscriptions[5] = context.Events.Subscribe<RoverPlaced>(OnRoverPlaced);
            _subscriptions[6] = context.Events.Subscribe<RoverWakeBeatReached>(OnWakeBeat);
            _subscriptions[7] = context.Events.Subscribe<LowPowerChanged>(OnLowPowerChanged);
            _subscriptions[8] = context.Events.Subscribe<PartFitted>(OnPartFitted);
            _subscriptions[9] = context.Events.Subscribe<PartRemoved>(OnPartRemoved);
            _subscriptions[10] = context.Events.Subscribe<BasePowerChanged>(OnBasePowerChanged);
            _subscriptions[11] = context.Events.Subscribe<DockEveningOpened>(OnDockEveningOpened);
            _subscriptions[12] = context.Events.Subscribe<FeelBeat>(OnFeelBeat);
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
            _signal.Step(ReachDistance(_listener.Position) + (_crawling ? _tuning.CrawlDistance : 0f), dt);
            float hop = _hop.Step(dt, _tuning.HopOutTime, _tuning.HopDarkTime, _tuning.HopInTime, out bool landing);
            if (landing)
            {
                _director.Play2D(_hopIn);
            }

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
            _buildLeft -= dt;
            _building &= _buildLeft > 0f;
            float build = _buildDuck.Step(_building ? _tuning.BuildDuckGain : 1f, dt,
                _building ? _tuning.BuildDuckTime : _tuning.BuildReturnTime);
            float level = _director.Buses.Effective(AudioBus.Music) * _wake.Power * build;
            float deckTarget = _gate.Stage == RadioGateStage.Deck ? 1f : 0f;
            float deckShare = _deckShare.Step(deckTarget, dt, _tuning.DeckBlendTime);
            float music = Mathf.Lerp(mix.MusicVolume, _tuning.DeckMusicGain, deckShare) * level * _wake.MusicGain *
                          Mathf.Lerp(1f, _tuning.CabinMusicGain, cabin) * _soundscape.RadioGain *
                          _soundscape.StargazeMusicGain * Mathf.Lerp(1f, _tuning.HopMusicGain, hop) *
                          _voice.MusicDuck * EveningDuck(dt);
            float cabinCutoff = _tuning.MaxCutoff * Mathf.Pow(_tuning.CabinCutoff / _tuning.MaxCutoff, cabin);
            float band = Mathf.Lerp(mix.CutoffHz, _tuning.DeckCutoff, deckShare);
            float cutoff = _soundscape.RadioCutoff(Mathf.Min(band, cabinCutoff));
            cutoff *= Mathf.Pow(Mathf.Min(1f, _tuning.HopCutoff / cutoff), hop);
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
            bool uncleared = _gate.Stage == RadioGateStage.Static && !_gate.CarrierClear;
            float floor = uncleared ? _tuning.UnclearedStaticFloor : 0f;
            float staticVolume = (Mathf.Max(mix.StaticVolume, floor) + _tuning.WakeStaticBoost * _wake.CrackleBoost) *
                                 presence
                                 + _tuning.TuneStaticBoost * _mixer.Swell + _tuning.HopStaticBoost * hop;
            _static.volume = Mathf.Clamp01(staticVolume) * _staticCueVolume * level * _soundscape.RadioGain *
                             Mathf.Lerp(1f, _tuning.CabinStaticGain, cabin);
            _swish.volume = _tuning.TuneSwishVolume * _swishCueVolume * level * _soundscape.RadioGain;
            float carrier = _carrierPresence.Step(_gate.CarrierHeard ? 1f : 0f, dt, _tuning.CarrierFadeTime);
            _carrier.volume = carrier * _tuning.CarrierVolume * _carrierCueVolume * mix.MusicVolume * level *
                              _soundscape.RadioGain * Mathf.Lerp(1f, _tuning.CabinStaticGain, cabin) *
                              _voice.MusicDuck;
        }

        private float StaticPresenceTarget()
        {
            if (_gate.CarrierHeard)
            {
                return _tuning.CarrierStaticGain;
            }

            return _gate.Stage == RadioGateStage.Station && _model.Station == RadioChannel.QuietHours
                ? _tuning.QuietStaticGain
                : 1f;
        }

        /// <summary>On Lumen After Dark, moves to the next track a moment before this one ends.</summary>
        private void AdvanceShow(float pitch)
        {
            int live = _mixer.Live;
            if (live < 0 || _mixer.StartPending || _gate.Stage != RadioGateStage.Station ||
                _model.Station != RadioChannel.LumenAfterDark ||
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
            bool opened = _gate.SetDialUnlocked(_model.DialUnlocked);
            if ((update & RadioProgramUpdate.Pool) != 0)
            {
                RebuildPool();
            }

            if (((update & RadioProgramUpdate.Station) == 0 && !opened) || _gate.Stage != RadioGateStage.Station)
            {
                return;
            }

            // Bell's dial bringing the station in is heard as a dial turn, like any change of station.
            announce |= opened;

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
            switch (_gate.Stage)
            {
                case RadioGateStage.Static:
                    return false;
                case RadioGateStage.Deck:
                    return _playlist.Count > 0;
            }

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
            if (_gate.Stage == RadioGateStage.Static)
            {
                return;
            }

            if (_gate.Stage == RadioGateStage.Deck)
            {
                track = _playlist.GetTrack(0);
                loop = true;
            }
            else if (_model.Station == RadioChannel.TapeDeck)
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
            if (_gate.Stage == RadioGateStage.Station && _model.Station == RadioChannel.LumenAfterDark &&
                _nowPlaying.TryAnnounce(track.Id, out string titleKey))
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
            if (live >= 0 && _gate.Stage == RadioGateStage.Station && _model.Station == RadioChannel.LumenAfterDark &&
                _decks[live] != null)
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

        private void OnRadioHopStarted(RadioHopStarted started)
        {
            _hop.Begin();
            _director.Play2D(_hopOut);
        }

        private void OnRadioHopFinished(RadioHopFinished finished)
        {
            _hop.End();
        }

        /// <summary>07 crawls home on its reserve: the radio thins as if a little farther from home, eased.</summary>
        private void OnLowPowerChanged(LowPowerChanged lowPower)
        {
            _crawling = lowPower.LowPower;
        }

        private void OnRoverPlaced(RoverPlaced placed)
        {
            // Set down unseen at full dark: the signal is the new place's at once, revealed as the hop resolves.
            _signal.Snap(ReachDistance(placed.Position));
        }

        private void OnSignalRadiusChanged(SignalRadiusChanged changed)
        {
            SetSignalRadius(changed.Radius);
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            if (!_openingHoldsRadio)
            {
                WakeUp(awoke.WokenByPlayer);
            }
        }

        /// <summary>
        /// 07's opening (docs/design/cutscenes.md §1) keeps the set asleep: the Asleep static and the ghost chord are
        /// heard from inside 07 instead, and the radio wakes as the camera turns over 07's shoulder (O12), or at the
        /// chase frame when the player hurried it.
        /// </summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            switch (reached.Beat)
            {
                case WakeBeat.Crackle:
                    _openingHoldsRadio = true;
                    break;
                case WakeBeat.OverShoulder:
                case WakeBeat.ChaseFrame:
                    if (_openingHoldsRadio)
                    {
                        _openingHoldsRadio = false;
                        WakeUp(reached.Hurried);
                    }

                    break;
            }
        }

        private void WakeUp(bool byPlayer)
        {
            if (_wake.IsAwake)
            {
                return;
            }

            _wake.Begin(byPlayer ? _tuning.PlayerWakeMusicDelay : _tuning.WakeMusicDelay,
                byPlayer ? _tuning.PlayerWakeMusicFade : _tuning.WakeMusicFade, _tuning.WakePowerTime);
            StartBeds();
            if (_model.Station != RadioChannel.QuietHours)
            {
                // Waking on Quiet Hours stays quiet: no dial swish, and the wake crackle is held down with the static.
                _swish.Play();
            }
        }

        /// <summary>The static and the carrier loop from the moment the set comes on; levels do the rest.</summary>
        private void StartBeds()
        {
            _static.volume = 0f;
            _static.Play();
            _carrier.volume = 0f;
            _carrier.Play();
        }

        /// <summary>07's tape deck is fitted (a live fit: a load publishes nothing): the first tape eases in softly,
        /// without a dial swish.</summary>
        private void OnPartFitted(PartFitted fitted)
        {
            if (fitted.Socket == RoverSocket.Deck && _gate.FitDeck())
            {
                OnGateOpened(_tuning.DeckFitFade);
            }
        }

        /// <summary>The deck taken off pauses its tape (the station, once Bell's dial is open, plays on).</summary>
        private void OnPartRemoved(PartRemoved removed)
        {
            if (removed.Socket == RoverSocket.Deck && _gate.RemoveDeck() && _wake.MusicStarted)
            {
                _mixer.Change(HasMusic(), _tuning.SilentSwitchFade, 0f, _tuning.SilentSwitchFade, 0f);
            }
        }

        private void OnGateOpened(float fadeIn)
        {
            if (!_wake.MusicStarted)
            {
                // Before the radio comes on (a load, the title): the wake-up starts whatever the gate allows.
                _deckShare.Snap(_gate.Stage == RadioGateStage.Deck ? 1f : 0f);
                _staticPresence.Snap(StaticPresenceTarget());
                return;
            }

            _mixer.Change(HasMusic(), _tuning.SilentSwitchFade, 0f, fadeIn, 0f);
        }

        /// <summary>
        /// The tower comes online (feel-pass section 8.2, Kind Build): the set steps back while it powers up, then at
        /// Follow 0 the radio crackles (the dial swish) and whatever the gate allows eases back in.
        /// </summary>
        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Build)
            {
                return;
            }

            if (beat.Phase == FeelPhase.Anticipate)
            {
                _building = true;
                _buildLeft = _tuning.BuildDuckMaxSeconds;
            }
            else if (beat.Phase == FeelPhase.Follow && beat.Strength <= 0f && _building)
            {
                _building = false;
                if (_wake.IsAwake)
                {
                    PlaySwish();
                }
            }
        }

        /// <summary>The dock evening's swell: the music steps back about 3 dB under it for a moment.</summary>
        private void OnDockEveningOpened(DockEveningOpened opened)
        {
            _eveningLeft = _tuning.EveningDuckSeconds;
        }

        private float EveningDuck(float deltaTime)
        {
            _eveningLeft = Mathf.Max(0f, _eveningLeft - deltaTime);
            float target = _eveningLeft > 0f ? _tuning.EveningDuckGain : 1f;
            return _eveningDuck.Step(target, deltaTime, _tuning.VoiceDuckTime);
        }

        /// <summary>Tower level 1 clears the static into the Earth signal's carrier (no music).</summary>
        private void OnBasePowerChanged(BasePowerChanged change)
        {
            if (change.Stage < BasePowerStage.Home || !_gate.ClearCarrier())
            {
                return;
            }

            if (change.Restored || !_wake.IsAwake)
            {
                _carrierPresence.Snap(_gate.CarrierHeard ? 1f : 0f);
                _staticPresence.Snap(StaticPresenceTarget());
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
