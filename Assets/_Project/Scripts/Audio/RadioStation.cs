using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's radio (docs/design/opening-v2.md B0). Silent until <see cref="RoverAwoke"/>: then it crackles on with a
    /// tuning swish into a faint signal hiss, the only thing it hears while the base has no power. When the base powers
    /// on (<see cref="OpeningMilestoneReached"/> <see cref="OpeningMilestone.PowerOn"/>) the hiss clears, and the set
    /// stays quiet: a build is not a milestone, so no music. Music comes only as moments, one track each: the dial
    /// finds the station with a swish and a swell of static, the track fades in low and warm, plays through, fades out,
    /// and the set falls back to silence (only a faint static presence). The opening's milestone, relay.1 answering
    /// 07's call (<see cref="CallStateChanged"/> <see cref="CallState.Answered"/>, beat R8), is one such moment (a load
    /// already past it stays silent); later milestones, regions and danger ask for theirs with
    /// <see cref="MusicMomentRequested"/>. Tracks come from the shuffled playlist (no immediate repeats) and each is
    /// named on the ticker once per session. Clarity follows 07's distance from the base through
    /// <see cref="RadioSignal"/> (low-pass, static, wow/flutter); beyond the clear radius the set thins towards
    /// near-silence, and it pulls back when 07 is still (the <see cref="Soundscape"/>'s mix). Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioStation : MonoBehaviour
    {
        private const float MinPitch = 0.01f;
        private const int SubscriptionCount = 7;
        private const float MaxFadeOutShareOfTrack = 0.5f;

        [Tooltip("Assets/_Project/Data/Audio/RadioTuning.asset.")]
        [SerializeField] private RadioTuning _tuning;

        [Tooltip("Assets/_Project/Data/Audio/RadioPlaylist.asset (built from tools/music/playlist.json).")]
        [SerializeField] private RadioPlaylist _playlist;

        private readonly AudioSource[] _decks = new AudioSource[RadioDeckMixer.DeckCount];
        private readonly AudioLowPassFilter[] _filters = new AudioLowPassFilter[RadioDeckMixer.DeckCount];
        private readonly RadioTrack[] _deckTracks = new RadioTrack[RadioDeckMixer.DeckCount];
        private readonly RadioDeckMixer _mixer = new RadioDeckMixer();
        private readonly WowFlutter _wowFlutter = new WowFlutter();
        private readonly RadioWakeUp _wake = new RadioWakeUp();
        private readonly NowPlayingLog _nowPlaying = new NowPlayingLog();
        private readonly EasedValue _hissFloor = new EasedValue(0f);
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private bool _openingHoldsRadio;
        private bool _powered;
        private bool _answered;
        private bool _momentPending;
        private IRoverState _listener;
        private IWorldLayout _layout;
        private EventBus _events;
        private AudioDirector _director;
        private Soundscape _soundscape;
        private RadioSignal _signal;
        private bool _crawling;
        private PlaylistShuffler _shuffler;
        private AudioSource _static;
        private AudioSource _swish;
        private float _staticCueVolume;
        private float _swishCueVolume;

        /// <summary>True once 07 has started waking and the radio has come on.</summary>
        public bool IsOn => _wake.IsAwake;

        /// <summary>True once the base has power: the signal hiss has cleared (still no music).</summary>
        public bool Powered => _powered;

        /// <summary>True once relay.1 has answered 07's call (its music moment, unless loaded already answered).
        /// </summary>
        public bool Answered => _answered;

        /// <summary>True while any music plays.</summary>
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

        /// <summary>The track on the live deck (null when no music plays).</summary>
        public RadioTrack CurrentTrack => _mixer.Live >= 0 ? _deckTracks[_mixer.Live] : null;

        /// <summary>True while a music moment waits for the radio to come on.</summary>
        public bool MomentPending => _momentPending;

        /// <summary>Tracks in the shuffle.</summary>
        public int ShufflePoolCount => _playlist != null ? _playlist.Count : 0;

        /// <summary>Smoothed signal clarity the player hears, 0 (lost) .. 1 (clear).</summary>
        public float Clarity => _signal != null ? _signal.Clarity : 0f;

        /// <summary>Where the signal is lost entirely: the clear radius plus the falloff (metres from the base).</summary>
        public float SignalEdge => _signal != null ? _signal.Radius + _tuning.FalloffWidth : 0f;

        /// <summary>The hiss level the unpowered set holds above the clarity static (0 once the hiss has cleared).
        /// </summary>
        public float HissFloor => _hissFloor.Value;

        /// <summary>07's horizontal distance from the base at <paramref name="position"/> (metres).</summary>
        public float ReachDistance(Vector3 position)
        {
            return SignalField.HorizontalDistance(position, _layout.BasePosition);
        }

        internal AudioSource GetDeck(int index) => _decks[index];

        internal RadioDeckMixer Mixer => _mixer;

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
            _soundscape = director.Soundscape;
            _events = context.Events;
            _listener = context.Get<IRoverState>();
            _layout = context.Get<IWorldLayout>();
            _signal = new RadioSignal(_tuning);
            _signal.Snap(ReachDistance(_listener.Position));
            _shuffler = new PlaylistShuffler(_playlist.Count, new AudioRandom(unchecked((uint)Environment.TickCount)));
            for (int i = 0; i < _playlist.Count; i++)
            {
                string id = _playlist.GetTrack(i).Id;
                _nowPlaying.Register(id, NowPlayingLog.TrackTitleKey(id));
            }

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
            _hissFloor.Snap(_tuning.UnpoweredStaticFloor);

            _subscriptions[0] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[1] = context.Events.Subscribe<RoverWakeBeatReached>(OnWakeBeat);
            _subscriptions[2] = context.Events.Subscribe<RoverPlaced>(OnRoverPlaced);
            _subscriptions[3] = context.Events.Subscribe<LowPowerChanged>(OnLowPowerChanged);
            _subscriptions[4] = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestone);
            _subscriptions[5] = context.Events.Subscribe<CallStateChanged>(OnCallStateChanged);
            _subscriptions[6] = context.Events.Subscribe<MusicMomentRequested>(OnMusicMomentRequested);
        }

        internal void Wire(RadioTuning tuning, RadioPlaylist playlist)
        {
            _tuning = tuning;
            _playlist = playlist;
        }

        private void Update()
        {
            if (_signal == null || !_wake.IsAwake)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _signal.Step(ReachDistance(_listener.Position) + (_crawling ? _tuning.CrawlDistance : 0f), dt);
            RadioMix mix = _signal.Mix;
            float wobble = _wowFlutter.Step(dt, _tuning.WowRate, _tuning.FlutterRate, _tuning.FlutterShare);
            float pitch = Mathf.Max(MinPitch, WowFlutter.PitchFactor(wobble, mix.WobbleCents));
            if (_wake.Step(dt) && _momentPending)
            {
                _momentPending = false;
                StartOn(_mixer.StartNow(0f));
            }

            FadeOutNearTheEnd(pitch);
            if (_mixer.Step(dt, out int startDeck))
            {
                StartOn(startDeck);
            }

            float cabin = _director.CabinBlend;
            float level = _director.Buses.Effective(AudioBus.Music) * _wake.Power;
            float music = mix.MusicVolume * level * _wake.MusicGain * Mathf.Lerp(1f, _tuning.CabinMusicGain, cabin) *
                          _soundscape.RadioGain * _soundscape.StargazeMusicGain;
            float cabinCutoff = _tuning.MaxCutoff * Mathf.Pow(_tuning.CabinCutoff / _tuning.MaxCutoff, cabin);
            float cutoff = Mathf.Min(mix.CutoffHz, cabinCutoff);
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

            float floor = _hissFloor.Step(_powered ? 0f : _tuning.UnpoweredStaticFloor, dt, _tuning.PowerOnClearTime);
            float presence = Mathf.Lerp(_tuning.IdleStaticGain, 1f, MusicPresence());
            float staticVolume = Mathf.Max(mix.StaticVolume * presence, floor) + _tuning.WakeStaticBoost * _wake.CrackleBoost +
                                 _tuning.TuneStaticBoost * _mixer.Swell;
            _static.volume = Mathf.Clamp01(staticVolume) * _staticCueVolume * level * _soundscape.RadioGain *
                             Mathf.Lerp(1f, _tuning.CabinStaticGain, cabin);
            _swish.volume = _tuning.TuneSwishVolume * _swishCueVolume * level * _soundscape.RadioGain;
        }

        /// <summary>
        /// The moment's track fades out before it ends (over at most half of a short track); the set then falls back
        /// to silence.
        /// </summary>
        private void FadeOutNearTheEnd(float pitch)
        {
            int live = _mixer.Live;
            if (live < 0)
            {
                return;
            }

            AudioSource deck = _decks[live];
            float fadeOut = deck.clip != null
                ? Mathf.Min(_tuning.MomentFadeOut, deck.clip.length / pitch * MaxFadeOutShareOfTrack)
                : 0f;
            if (SecondsLeft(deck, pitch) <= fadeOut)
            {
                _mixer.Change(false, fadeOut, 0f, 0f, 0f);
            }
        }

        /// <summary>How much music the decks carry, 0 (silent) .. 1 (a track at full level).</summary>
        private float MusicPresence()
        {
            float presence = 0f;
            for (int i = 0; i < RadioDeckMixer.DeckCount; i++)
            {
                presence = Mathf.Max(presence, _mixer.Level(i));
            }

            return presence;
        }

        /// <summary>Starts the playlist's next track on <paramref name="deck"/> (it is the live deck now).</summary>
        private void StartOn(int deck)
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            RadioTrack track = _playlist.GetTrack(_shuffler.Next());
            AudioSource source = _decks[deck];
            source.Stop();
            source.clip = track.Clip;
            source.loop = false;
            source.timeSamples = 0;
            source.volume = 0f;
            source.Play();
            _deckTracks[deck] = track;
            if (_nowPlaying.TryAnnounce(track.Id, out string titleKey))
            {
                _events.Publish(new TickerLine(NowPlayingLog.TickerKey, titleKey, true));
            }
        }

        private void PlaySwish()
        {
            _swish.Stop();
            _swish.Play();
        }

        /// <summary>The base has power: the hiss clears (at once for a load). No music: the build is not a milestone.
        /// </summary>
        private void OnMilestone(OpeningMilestoneReached reached)
        {
            if (reached.Milestone != OpeningMilestone.PowerOn || _powered)
            {
                return;
            }

            _powered = true;
            if (reached.Restored)
            {
                _hissFloor.Snap(0f);
            }
        }

        /// <summary>Relay.1 answered: the opening's milestone gets its music moment (a load past it stays silent).
        /// </summary>
        private void OnCallStateChanged(CallStateChanged changed)
        {
            if (changed.State != CallState.Answered || _answered)
            {
                return;
            }

            _answered = true;
            if (!changed.Restored)
            {
                BeginMoment();
            }
        }

        private void OnMusicMomentRequested(MusicMomentRequested requested)
        {
            BeginMoment();
        }

        /// <summary>
        /// One track: the dial swish and a swell of static, then the track fades in. Ignored while one is already
        /// starting or playing; before the radio is on it waits and the wake-up starts it.
        /// </summary>
        private void BeginMoment()
        {
            if (_momentPending || _mixer.Live >= 0 || _mixer.StartPending)
            {
                return;
            }

            if (!_wake.MusicStarted)
            {
                _momentPending = true;
                return;
            }

            _mixer.Change(true, 0f, _tuning.MomentStartDelay, _tuning.MomentFadeIn, _tuning.TuneSwellDuration);
            PlaySwish();
        }

        /// <summary>07 crawls home on its reserve: the radio thins as if a little farther from the base, eased.</summary>
        private void OnLowPowerChanged(LowPowerChanged lowPower)
        {
            _crawling = lowPower.LowPower;
        }

        private void OnRoverPlaced(RoverPlaced placed)
        {
            // Set down unseen (a recovery): the signal is the new place's at once.
            _signal.Snap(ReachDistance(placed.Position));
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            if (!_openingHoldsRadio)
            {
                WakeUp(awoke.WokenByPlayer);
            }
        }

        /// <summary>
        /// 07's opening keeps the set asleep: the Asleep static is heard from inside 07 instead, and the radio wakes
        /// at the chase frame.
        /// </summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            switch (reached.Beat)
            {
                case WakeBeat.FadedOut:
                case WakeBeat.Crackle:
                    _openingHoldsRadio = true;
                    break;
                case WakeBeat.ChaseFrame:
                    if (_openingHoldsRadio)
                    {
                        _openingHoldsRadio = false;
                        WakeUp(false);
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
            _static.volume = 0f;
            _static.Play();
            _swish.Play();
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
            if (_tuning == null || _playlist == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: the radio tuning or the playlist is not assigned.", this);
                return false;
            }

            string problem = _playlist.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {problem}.", this);
                return false;
            }

            return true;
        }
    }
}
