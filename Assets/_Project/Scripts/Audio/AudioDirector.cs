using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The Audio domain's game system. Owns the volume buses, pooled 3D/2D one-shot voices and the cue library, plays
    /// the landing thump on <see cref="RoverLanded"/>, and initialises the rover sounds, radio and ambience bed.
    /// Initialise it after the World and Rover systems (it reads <see cref="IRoverState"/> and
    /// <see cref="IWorldLayout"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour, IGameSystem
    {
        private const float MinPitch = 0.05f;

        [Tooltip("Assets/_Project/Data/Audio/AudioLibrary.asset (built from tools/audio/sfx_manifest.json).")]
        [SerializeField] private AudioLibrary _library;

        [Tooltip("Assets/_Project/Data/Audio/AudioMixTuning.asset.")]
        [SerializeField] private AudioMixTuning _mixTuning;

        [Tooltip("Rover hum, dust crunch and suspension creaks.")]
        [SerializeField] private RoverAudio _roverAudio;

        [Tooltip("The radio station (music, static, clarity).")]
        [SerializeField] private RadioStation _radio;

        [Tooltip("The lunar ambience bed.")]
        [SerializeField] private AmbienceBed _ambience;

        private readonly AudioBusMixer _buses = new AudioBusMixer();
        private AudioRandom _random;
        private VoicePool _spatialPool;
        private VoicePool _flatPool;
        private AudioSource[] _spatialVoices;
        private AudioSource[] _flatVoices;
        private int[] _lastClip;
        private CueHandle _landingThump;
        private IDisposable _landedSubscription;

        public AudioBusMixer Buses => _buses;

        public AudioLibrary Library => _library;

        public bool IsInitialized { get; private set; }

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
            _random = new AudioRandom(unchecked((uint)Environment.TickCount));
            _lastClip = new int[_library.CueCount];
            for (int i = 0; i < _lastClip.Length; i++)
            {
                _lastClip[i] = -1;
            }

            _spatialVoices = CreateVoices("Voice3D", _mixTuning.SpatialVoices, true);
            _flatVoices = CreateVoices("Voice2D", _mixTuning.FlatVoices, false);
            _spatialPool = new VoicePool(_spatialVoices.Length);
            _flatPool = new VoicePool(_flatVoices.Length);
            _landingThump = Resolve(AudioCueIds.LandingThump);
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnRoverLanded);
            IsInitialized = true;

            _roverAudio.Initialize(context, this);
            _radio.Initialize(context, this);
            _ambience.Initialize(this, _mixTuning.AmbienceFadeIn);
        }

        /// <summary>Resolves a cue id once (call at initialisation); logs and returns an invalid handle if unknown.</summary>
        public CueHandle Resolve(string id)
        {
            if (!_library.TryResolve(id, out CueHandle handle))
            {
                Debug.LogError($"{nameof(AudioDirector)}: no cue '{id}' in {_library.name}. " +
                               "Re-run tools/audio/build_sfx.py and MoonProject/Build/Audio/Library.", this);
            }

            return handle;
        }

        /// <summary>Plays a random variant of <paramref name="cue"/> as a 3D sound at <paramref name="position"/>.</summary>
        public void PlayAt(CueHandle cue, Vector3 position, float volumeScale = 1f, float pitchScale = 1f)
        {
            Play(cue, true, position, volumeScale, pitchScale);
        }

        /// <summary>Plays a random variant of <paramref name="cue"/> flat (UI, stingers).</summary>
        public void Play2D(CueHandle cue, float volumeScale = 1f, float pitchScale = 1f)
        {
            Play(cue, false, Vector3.zero, volumeScale, pitchScale);
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
        }

        private void Play(CueHandle cue, bool spatial, Vector3 position, float volumeScale, float pitchScale)
        {
            if (!IsInitialized || !cue.IsValid)
            {
                Debug.LogError($"{nameof(AudioDirector)}: play request before initialisation or with an " +
                               "unresolved cue.", this);
                return;
            }

            AudioCue entry = _library.GetCue(cue);
            int clipIndex = _random.PickAvoiding(entry.ClipCount, _lastClip[cue.Index]);
            _lastClip[cue.Index] = clipIndex;
            AudioClip clip = entry.GetClip(clipIndex);
            float pitch = Mathf.Max(MinPitch, _random.Range(entry.PitchMin, entry.PitchMax) * pitchScale);
            float volume = _random.Range(entry.VolumeMin, entry.VolumeMax) * volumeScale * _buses.Effective(entry.Bus);

            VoicePool pool = spatial ? _spatialPool : _flatPool;
            AudioSource[] voices = spatial ? _spatialVoices : _flatVoices;
            AudioSource voice = voices[pool.Acquire(Time.unscaledTime, clip.length / pitch)];
            voice.Stop();
            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;
            if (spatial)
            {
                voice.transform.position = position;
            }

            voice.Play();
        }

        private void OnRoverLanded(RoverLanded landed)
        {
            ImpactSound thump = ImpactSound.ForLanding(landed.ImpactSpeed, _mixTuning);
            if (thump.Audible && _landingThump.IsValid)
            {
                PlayAt(_landingThump, landed.Position, thump.Volume, thump.Pitch);
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
            ok &= Require(_radio, nameof(_radio));
            ok &= Require(_ambience, nameof(_ambience));
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

        internal void Wire(AudioLibrary library, AudioMixTuning mixTuning, RoverAudio roverAudio, RadioStation radio,
            AmbienceBed ambience)
        {
            _library = library;
            _mixTuning = mixTuning;
            _roverAudio = roverAudio;
            _radio = radio;
            _ambience = ambience;
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
            _landedSubscription = null;
        }
    }
}
