using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07 speaks machine (docs/design/feel-pass.md section 7.2): plays the chirp 07's character asks for
    /// (<see cref="RoverChirpRequested"/>) from 07 itself, one at a time, never over a voice line or a letter (it waits
    /// a moment, then lets the chirp go: <see cref="ChirpScheduler"/>), a little under the radio's music, and
    /// publishes <see cref="RoverChirpSyllable"/> at each syllable's onset so 07's eye and antenna move in time.
    /// Zero allocation per frame. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChirpVoice : MonoBehaviour
    {
        private const uint SeedSalt = 0x3C6EF372u;

        [Tooltip("Assets/_Project/Data/Audio/FeelAudioTuning.asset.")]
        [SerializeField] private FeelAudioTuning _tuning;

        [Tooltip("Assets/_Project/Data/Audio/ChirpBank.asset (built from tools/audio/chirp_syllables.json).")]
        [SerializeField] private ChirpBank _bank;

        private readonly CueHandle[] _cues = new CueHandle[ChirpBank.MoodCount];
        private AudioDirector _director;
        private EventBus _events;
        private IRoverState _rover;
        private ChirpScheduler _scheduler;
        private AudioSource _source;
        private IDisposable _subscription;
        private ChirpVariant _speaking;
        private ChirpMood _speakingMood;
        private float _elapsed;
        private int _nextSyllable;

        /// <summary>True from a chirp's first syllable until its last has ended.</summary>
        public bool IsSpeaking => _speaking != null;

        /// <summary>The scheduler (tests read its waiting and dropped requests).</summary>
        internal ChirpScheduler Scheduler => _scheduler;

        internal void Wire(FeelAudioTuning tuning, ChirpBank bank)
        {
            _tuning = tuning;
            _bank = bank;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null || _bank == null)
            {
                Debug.LogError($"{nameof(ChirpVoice)}: the feel tuning or the chirp bank is not assigned.", this);
                enabled = false;
                return;
            }

            string problem = _bank.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(ChirpVoice)}: {problem}.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _cues.Length; i++)
            {
                _cues[i] = director.Resolve(_bank.CueId((ChirpMood)i));
                if (!_cues[i].IsValid)
                {
                    enabled = false;
                    return;
                }
            }

            _director = director;
            _events = context.Events;
            _rover = context.Get<IRoverState>();
            _scheduler = new ChirpScheduler(new AudioRandom(unchecked((uint)Environment.TickCount) ^ SeedSalt));
            _source = director.CreateLoopSource(transform, "Chirp", default, 1f);
            _source.loop = false;
            _subscription = context.Events.Subscribe<RoverChirpRequested>(OnChirpRequested);
        }

        private void Update()
        {
            if (_scheduler == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _source.transform.position = _rover.Position;
            if (_speaking != null)
            {
                Advance(dt);
            }

            bool busy = _speaking != null || _director.Voice.IsSpeaking;
            if (_scheduler.Step(dt, busy, _tuning.ChirpHoldSeconds, out ChirpMood mood))
            {
                Speak(mood);
            }
        }

        private void Speak(ChirpMood mood)
        {
            int variant = _scheduler.PickVariant(mood, _bank.VariantCount(mood));
            float level = _tuning.ChirpVolume * (_director.Radio.MusicStarted ? _tuning.ChirpUnderRadio : 1f);
            _director.PlayOn(_source, _cues[(int)mood], level, variant);
            _speaking = _bank.GetVariant(mood, variant);
            _speakingMood = mood;
            _elapsed = 0f;
            _nextSyllable = 0;
            Advance(0f);
        }

        /// <summary>Publishes every syllable whose onset has passed; ends the chirp after its last syllable.</summary>
        private void Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            ChirpVariant chirp = _speaking;
            while (_nextSyllable < chirp.SyllableCount && chirp.Onset(_nextSyllable) <= _elapsed)
            {
                _events.Publish(new RoverChirpSyllable(_speakingMood, _nextSyllable, chirp.SyllableCount,
                    chirp.Pitch(_nextSyllable), chirp.Length(_nextSyllable)));
                _nextSyllable++;
            }

            if (_elapsed >= chirp.Seconds)
            {
                _speaking = null;
            }
        }

        private void OnChirpRequested(RoverChirpRequested requested)
        {
            _scheduler.Request(requested.Mood);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
