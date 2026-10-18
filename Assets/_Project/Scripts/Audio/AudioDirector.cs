using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The Audio domain's game system. Owns the volume buses (registered as <see cref="IAudioSettings"/>), pooled
    /// 3D/2D one-shot voices and the cue library, plays the landing thump on <see cref="RoverLanded"/>, eases the
    /// pause mix on <see cref="PauseChanged"/> (world loops duck, the radio moves into the cabin), and initialises
    /// the rover, building, battery, feel and UI sounds, 07's chirps, the radio, the title music, the
    /// ambience bed and the soundscape of solitude (with 07's small sounds). 2D sources bypass reverb zones. Initialise it after the World, Rover and
    /// Gameplay systems (it reads <see cref="IRoverState"/>, <see cref="IRoverRig"/> and
    /// <see cref="IWorldLayout"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour, IGameSystem
    {
        private const float MinPitch = 0.05f;
        private const int RecentClipCapacity = 16;

        [Tooltip("Assets/_Project/Data/Audio/AudioLibrary.asset (built from tools/audio/sfx_manifest.json).")]
        [SerializeField] private AudioLibrary _library;

        [Tooltip("Assets/_Project/Data/Audio/AudioMixTuning.asset.")]
        [SerializeField] private AudioMixTuning _mixTuning;

        [Tooltip("Rover hum, dust crunch and suspension creaks.")]
        [SerializeField] private RoverAudio _roverAudio;

        [Tooltip("Building: the placement ghost, snap, weld, structure complete and power-on.")]
        [SerializeField] private GameplayAudio _gameplay;

        [SerializeField] private BatteryAudio _battery;


        [Tooltip("UI touches (menus, focus, sliders, cards, prompts, hold-to-buy).")]
        [SerializeField] private UiAudio _ui;

        [Tooltip("The radio: the signal hiss, then the playlist once the base powers on.")]
        [SerializeField] private RadioStation _radio;


        [Tooltip("The feel beats: the cut, the charge on the pad, scans, containers and the hold's boot.")]
        [SerializeField] private FeelAudio _feel;

        [Tooltip("07's machine voice: the chirps and their syllables.")]
        [SerializeField] private RoverChirpAudio _chirps;

        [Tooltip("The lunar ambience bed.")]
        [SerializeField] private AmbienceBed _ambience;


        [Tooltip("The mix of solitude: distance from the base, stillness and the wide shot; the room tone.")]
        [SerializeField] private Soundscape _soundscape;

        [Tooltip("07's lamp hum, servos and cooling ticks.")]
        [SerializeField] private RoverSmallSounds _smallSounds;


        [Tooltip("Salvage: the cutting beam, the break-off and the salvage melody.")]
        [SerializeField] private SalvageAudio _salvage;


        [Tooltip("The title screen's dedicated procedural theme.")]
        [SerializeField] private TitleMusic _titleMusic;

        private readonly AudioBusMixer _buses = new AudioBusMixer();
        private readonly LoopFader _pause = new LoopFader();
        private readonly AudioClip[] _recentClips = new AudioClip[RecentClipCapacity];
        private int _playCount;
        private AudioRandom _random;
        private VoiceBank _spatial;
        private VoiceBank _flat;
        private int[] _lastClip;
        private int _appliedBusVersion;
        private CueHandle _landingThump;
        private IDisposable _landedSubscription;
        private IDisposable _pauseSubscription;

        public AudioBusMixer Buses => _buses;

        public AudioLibrary Library => _library;

        /// <summary>The base radio (the chirps sit under its music).</summary>
        internal RadioStation Radio => _radio;

        /// <summary>The dedicated title screen theme.</summary>
        internal TitleMusic TitleMusic => _titleMusic;

        public bool IsInitialized { get; private set; }

        /// <summary>True between PauseChanged(true) and PauseChanged(false).</summary>
        public bool IsPaused => _pause.IsOn;

        /// <summary>Volume scale for loops that run on game time (1 playing, ducked while paused, eased).</summary>
        public float WorldGain => Mathf.Lerp(1f, _mixTuning.PausedWorldGain, _pause.Gain);

        /// <summary>0..1 eased "listening in the cabin" amount while paused (the radio uses it).</summary>
        public float CabinBlend => _pause.Gain;

        /// <summary>The mix of solitude every layer reads its level from.</summary>
        internal Soundscape Soundscape => _soundscape;

        /// <summary>The voice that started most recently (diagnostics and tests).</summary>
        internal AudioSource LastVoice { get; private set; }

        /// <summary>The clip that started most recently (one-shots on a following voice keep their source's clip
        /// unchanged, so read this instead of <see cref="LastVoice"/>.clip).</summary>
        internal AudioClip LastClip => _playCount > 0 ? RecentClip(0) : null;

        /// <summary>Number of one-shots started so far (diagnostics and tests).</summary>
        internal int PlayCount => _playCount;

        /// <summary>The clip started <paramref name="playsAgo"/> plays ago (0 = last), within the last 16.</summary>
        internal AudioClip RecentClip(int playsAgo)
        {
            if (playsAgo < 0 || playsAgo >= Mathf.Min(_playCount, RecentClipCapacity))
            {
                return null;
            }

            return _recentClips[(_playCount - 1 - playsAgo) % RecentClipCapacity];
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!HasValidReferences())
            {
                enabled = false;
                return;
            }

            _buses.SetVolume(AudioBus.Master, _mixTuning.MasterVolume);
            _buses.SetVolume(AudioBus.Music, _mixTuning.MusicVolume);
            _buses.SetVolume(AudioBus.Sfx, _mixTuning.SfxVolume);
            _buses.SetVolume(AudioBus.Ambience, _mixTuning.AmbienceVolume);
            _appliedBusVersion = _buses.Version;
            context.Register<IAudioSettings>(_buses);

            _random = new AudioRandom(unchecked((uint)Environment.TickCount));
            _lastClip = new int[_library.CueCount];
            for (int i = 0; i < _lastClip.Length; i++)
            {
                _lastClip[i] = -1;
            }

            _spatial = new VoiceBank(CreateVoices("Voice3D", _mixTuning.SpatialVoices, true), true);
            _flat = new VoiceBank(CreateVoices("Voice2D", _mixTuning.FlatVoices, false), false);
            _landingThump = Resolve(AudioCueIds.LandingThump);
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnRoverLanded);
            _pauseSubscription = context.Events.Subscribe<PauseChanged>(OnPauseChanged);
            IsInitialized = true;

            _roverAudio.Initialize(context, this);
            _gameplay.Initialize(context, this);
            _ui.Initialize(context, this);
            _radio.Initialize(context, this);
            if (_titleMusic == null && _mixTuning.TitleMusic != null)
            {
                var titleHost = new GameObject("TitleMusic");
                titleHost.transform.SetParent(transform, false);
                _titleMusic = titleHost.AddComponent<TitleMusic>();
                _titleMusic.Wire(_mixTuning.TitleMusic);
            }

            if (_titleMusic != null)
            {
                _titleMusic.Initialize(context, this);
            }

            _feel.Initialize(context, this);
            _chirps.Initialize(context, this);
            _soundscape.Initialize(context, this, _radio, _mixTuning.AmbienceFadeIn);
            _ambience.Initialize(context, this, _soundscape, _mixTuning.AmbienceFadeIn, _mixTuning.AmbienceCrossfade);
            _smallSounds.Initialize(context, this, _soundscape);
            _salvage.Initialize(context, this);
            _battery.Initialize(context, this);
        }

        /// <summary>Resolves a cue id once (call at initialisation); logs and returns an invalid handle if
        /// unknown.</summary>
        public CueHandle Resolve(string id)
        {
            if (!_library.TryResolve(id, out CueHandle handle))
            {
                Debug.LogError($"{nameof(AudioDirector)}: no cue '{id}' in {_library.name}. " +
                               "Re-run tools/audio/build_sfx.py and MoonProject/Build/Audio/Library.", this);
            }

            return handle;
        }

        /// <summary>Plays a random variant of <paramref name="cue"/> as a 3D sound at
        /// <paramref name="position"/>.</summary>
        public void PlayAt(CueHandle cue, Vector3 position, float volumeScale = 1f, float pitchScale = 1f)
        {
            Play(cue, _spatial, position, volumeScale, pitchScale, -1);
        }

        /// <summary>Plays variant <paramref name="variant"/> (clamped to the cue's clips) of <paramref name="cue"/> in
        /// 3D, e.g. the chime note of a salvage combo step.</summary>
        public void PlayVariantAt(CueHandle cue, int variant, Vector3 position, float volumeScale = 1f)
        {
            Play(cue, _spatial, position, volumeScale, 1f, Mathf.Max(0, variant));
        }

        /// <summary>Plays <paramref name="cue"/> (variant <paramref name="variant"/>, or random without repeats when
        /// negative) as a one-shot on <paramref name="source"/>, a voice that follows its owner (e.g. 07's chirps
        /// from its body). Overlapping one-shots are fine; the source's pitch takes the cue's pitch variance.</summary>
        internal void PlayOn(AudioSource source, CueHandle cue, float volumeScale, int variant = -1)
        {
            if (!IsInitialized || !cue.IsValid)
            {
                Debug.LogError($"{nameof(AudioDirector)}: play request before initialisation or with an " +
                               "unresolved cue.", this);
                return;
            }

            AudioCue entry = _library.GetCue(cue);
            int clipIndex = variant >= 0
                ? Mathf.Min(variant, entry.ClipCount - 1)
                : _random.PickAvoiding(entry.ClipCount, _lastClip[cue.Index]);
            _lastClip[cue.Index] = clipIndex;
            source.pitch = Mathf.Max(MinPitch, _random.Range(entry.PitchMin, entry.PitchMax));
            source.volume = 1f;
            float volume = _random.Range(entry.VolumeMin, entry.VolumeMax) * volumeScale * _buses.Effective(entry.Bus);
            source.PlayOneShot(entry.GetClip(clipIndex), volume);
            LastVoice = source;
            Remember(entry.GetClip(clipIndex));
        }

        /// <summary>Plays variant <paramref name="variant"/> (clamped to the cue's clips) of <paramref name="cue"/>
        /// flat, e.g. the wake phrase of the place that woke.</summary>
        public void PlayVariant2D(CueHandle cue, int variant, float volumeScale = 1f)
        {
            Play(cue, _flat, Vector3.zero, volumeScale, 1f, Mathf.Max(0, variant));
        }

        /// <summary>Plays a random variant of <paramref name="cue"/> flat (UI, stingers).</summary>
        public void Play2D(CueHandle cue, float volumeScale = 1f, float pitchScale = 1f)
        {
            Play(cue, _flat, Vector3.zero, volumeScale, pitchScale, -1);
        }

        public void SetBusVolume(AudioBus bus, float volume)
        {
            _buses.SetVolume(bus, volume);
        }

        /// <summary>A looping source for <paramref name="cue"/> (first clip) on a new child of
        /// <paramref name="parent"/>, silent until its owner sets a volume. Initialisation-time only.</summary>
        internal AudioSource CreateLoopSource(Transform parent, string objectName, CueHandle cue, float spatialBlend)
        {
            var host = new GameObject(objectName);
            host.transform.SetParent(parent, false);
            AudioSource source = host.AddComponent<AudioSource>();
            Configure(source, spatialBlend);
            source.loop = true;
            source.volume = 0f;
            if (cue.IsValid)
            {
                source.clip = _library.GetCue(cue).GetClip(0);
            }

            return source;
        }

        /// <summary>Applies the project's playback defaults (and 3D rolloff when spatial) to a source.</summary>
        internal void Configure(AudioSource source, float spatialBlend)
        {
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = _mixTuning.RolloffMode;
            source.minDistance = _mixTuning.MinDistance;
            source.maxDistance = _mixTuning.MaxDistance;
            source.dopplerLevel = _mixTuning.DopplerLevel;
            source.spread = _mixTuning.Spread;

            // Reverb zones are for sounds in the world; the radio, UI and ambience beds stay dry.
            source.bypassReverbZones = spatialBlend <= 0f;
        }

        /// <summary>07's battery sounds (docs/features/M3-20).</summary>
        internal void WireBattery(BatteryAudio battery)
        {
            _battery = battery;
        }

        /// <summary>The 0.5.2 feel beats and 07's machine voice (docs/design/feel-pass.md).</summary>
        internal void WireFeel(FeelAudio feel, RoverChirpAudio chirps)
        {
            _feel = feel;
            _chirps = chirps;
        }

        internal void WireTitleMusic(TitleMusic titleMusic)
        {
            _titleMusic = titleMusic;
        }

        internal void Wire(AudioLibrary library, AudioMixTuning mixTuning, RoverAudio roverAudio,
            GameplayAudio gameplay, UiAudio ui, RadioStation radio, AmbienceBed ambience,
            Soundscape soundscape, RoverSmallSounds smallSounds, SalvageAudio salvage)
        {
            _ui = ui;
            _library = library;
            _mixTuning = mixTuning;
            _roverAudio = roverAudio;
            _gameplay = gameplay;
            _radio = radio;
            _ambience = ambience;
            _soundscape = soundscape;
            _smallSounds = smallSounds;
            _salvage = salvage;
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            _pause.Step(Time.unscaledDeltaTime, _mixTuning.PauseDuckTime, _mixTuning.ResumeTime);
            if (_buses.Version == _appliedBusVersion)
            {
                return;
            }

            // A volume slider moved: re-level the one-shots that are still ringing (loops read the buses per frame).
            _appliedBusVersion = _buses.Version;
            float now = Time.unscaledTime;
            _spatial.Relevel(_buses, now);
            _flat.Relevel(_buses, now);
        }

        private void Play(CueHandle cue, VoiceBank bank, Vector3 position, float volumeScale, float pitchScale,
            int variant)
        {
            if (!IsInitialized || !cue.IsValid)
            {
                Debug.LogError($"{nameof(AudioDirector)}: play request before initialisation or with an " +
                               "unresolved cue.", this);
                return;
            }

            AudioCue entry = _library.GetCue(cue);
            int clipIndex = variant >= 0
                ? Mathf.Min(variant, entry.ClipCount - 1)
                : _random.PickAvoiding(entry.ClipCount, _lastClip[cue.Index]);
            _lastClip[cue.Index] = clipIndex;
            AudioClip clip = entry.GetClip(clipIndex);
            float pitch = Mathf.Max(MinPitch, _random.Range(entry.PitchMin, entry.PitchMax) * pitchScale);
            float baseVolume = _random.Range(entry.VolumeMin, entry.VolumeMax) * volumeScale;

            int index = bank.Pool.Acquire(Time.unscaledTime, clip.length / pitch);
            AudioSource voice = bank.Sources[index];
            voice.Stop();
            voice.clip = clip;
            voice.pitch = pitch;
            bank.BaseVolumes[index] = baseVolume;
            bank.Buses[index] = entry.Bus;
            voice.volume = baseVolume * _buses.Effective(entry.Bus);
            if (bank.Spatial)
            {
                voice.transform.position = position;
            }

            voice.Play();
            LastVoice = voice;
            Remember(clip);
        }

        private void OnRoverLanded(RoverLanded landed)
        {
            ImpactSound thump = ImpactSound.ForLanding(landed.ImpactSpeed, _mixTuning);
            if (thump.Audible && _landingThump.IsValid)
            {
                PlayAt(_landingThump, landed.Position, thump.Volume, thump.Pitch);
            }
        }

        private void Remember(AudioClip clip)
        {
            _recentClips[_playCount % RecentClipCapacity] = clip;
            _playCount++;
        }

        private void OnPauseChanged(PauseChanged changed)
        {
            if (changed.Paused)
            {
                _pause.FadeIn();
            }
            else
            {
                _pause.FadeOut();
            }
        }

        private AudioSource[] CreateVoices(string prefix, int count, bool spatial)
        {
            var voices = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                var host = new GameObject($"{prefix}_{i:00}");
                host.transform.SetParent(transform, false);
                voices[i] = host.AddComponent<AudioSource>();
                Configure(voices[i], spatial ? 1f : 0f);
            }

            return voices;
        }

        private bool HasValidReferences()
        {
            bool ok = true;
            ok &= Require(_library, nameof(_library));
            ok &= Require(_mixTuning, nameof(_mixTuning));
            ok &= Require(_roverAudio, nameof(_roverAudio));
            ok &= Require(_gameplay, nameof(_gameplay));
            ok &= Require(_ui, nameof(_ui));
            ok &= Require(_radio, nameof(_radio));
            ok &= Require(_feel, nameof(_feel));
            ok &= Require(_chirps, nameof(_chirps));
            ok &= Require(_ambience, nameof(_ambience));
            ok &= Require(_soundscape, nameof(_soundscape));
            ok &= Require(_smallSounds, nameof(_smallSounds));
            ok &= Require(_salvage, nameof(_salvage));
            ok &= Require(_battery, nameof(_battery));
            if (_library != null)
            {
                string problem = _library.FindProblem();
                if (problem != null)
                {
                    Debug.LogError($"{nameof(AudioDirector)}: {problem}.", _library);
                    ok = false;
                }
            }

            return ok;
        }

        private bool Require(UnityEngine.Object reference, string field)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(AudioDirector)}: {field} is not assigned.", this);
            return false;
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
            _landedSubscription = null;
            _pauseSubscription?.Dispose();
            _pauseSubscription = null;
        }

        /// <summary>One pool of one-shot voices with the per-voice state needed to re-level them.</summary>
        private sealed class VoiceBank
        {
            public VoiceBank(AudioSource[] sources, bool spatial)
            {
                Sources = sources;
                Spatial = spatial;
                Pool = new VoicePool(sources.Length);
                BaseVolumes = new float[sources.Length];
                Buses = new AudioBus[sources.Length];
            }

            public AudioSource[] Sources { get; }

            public bool Spatial { get; }

            public VoicePool Pool { get; }

            public float[] BaseVolumes { get; }

            public AudioBus[] Buses { get; }

            public void Relevel(AudioBusMixer buses, float now)
            {
                for (int i = 0; i < Sources.Length; i++)
                {
                    if (Pool.IsBusy(i, now))
                    {
                        Sources[i].volume = BaseVolumes[i] * buses.Effective(Buses[i]);
                    }
                }
            }
        }
    }
}
