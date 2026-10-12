using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Plays a voiced recording when the radio ticker starts showing its line (<see cref="TickerLineShown"/>): Ro's
    /// letters on old tape, the automated station on a narrow broadcast band (tools/voice). The ticker line is the
    /// subtitle; <see cref="VoiceLineStarted"/> tells the ticker how long to keep it up. While a voice speaks the
    /// radio's music ducks (<see cref="MusicDuck"/>). Voices sit on the Sfx bus, so turning the music down never
    /// silences the story; like the radio they keep playing under the pause menu. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioVoice : MonoBehaviour
    {
        private const int SubscriptionCount = 1;

        [Tooltip("Assets/_Project/Data/Audio/RadioTuning.asset.")]
        [SerializeField] private RadioTuning _tuning;

        [Tooltip("Assets/_Project/Data/Audio/VoiceLibrary.asset (built from tools/voice/voice_manifest.json).")]
        [SerializeField] private VoiceLibrary _library;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly EasedValue _duck = new EasedValue(1f);
        private AudioDirector _director;
        private EventBus _events;
        private AudioSource _source;

        /// <summary>Gain for the radio's music: 1 when no voice speaks, the tuning's duck while one does.</summary>
        public float MusicDuck => _duck.Value;

        /// <summary>True while a recording plays.</summary>
        public bool IsSpeaking => _source != null && _source.isPlaying;

        /// <summary>The recording playing now (null when none).</summary>
        public AudioClip CurrentClip => IsSpeaking ? _source.clip : null;

        internal void Wire(RadioTuning tuning, VoiceLibrary library)
        {
            _tuning = tuning;
            _library = library;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (!HasValidReferences())
            {
                enabled = false;
                return;
            }

            _director = director;
            _events = context.Events;
            _source = director.CreateLoopSource(transform, "Voice", default, 0f);
            _source.loop = false;
            _subscriptions[0] = context.Events.Subscribe<TickerLineShown>(OnTickerLineShown);
        }

        private void Update()
        {
            if (_source == null)
            {
                return;
            }

            _source.volume = _tuning.VoiceVolume * _director.Buses.Effective(AudioBus.Sfx);
            float target = IsSpeaking ? _tuning.VoiceMusicDuck : 1f;
            _duck.Step(target, Time.unscaledDeltaTime, _tuning.VoiceDuckTime);
        }

        private void OnTickerLineShown(TickerLineShown shown)
        {
            if (!_library.TryGet(shown.Key, out VoiceLine line))
            {
                return;
            }

            _source.Stop();
            _source.clip = line.Clip;
            _source.timeSamples = 0;
            _source.Play();

            _events.Publish(new VoiceLineStarted(line.Key, line.Clip.length));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }

        private bool HasValidReferences()
        {
            if (_tuning == null || _library == null)
            {
                Debug.LogError($"{nameof(RadioVoice)}: the radio tuning or the voice library is not assigned.", this);
                return false;
            }

            string problem = _library.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioVoice)}: {problem}.", this);
                return false;
            }

            return true;
        }
    }
}
