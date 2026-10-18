using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's wake as heard from inside it (docs/design/cutscenes.md §1, docs/features/M3-09 Part A):
    /// <list type="bullet">
    /// <item>the radio's Asleep profile: quiet out-of-tune static comes up under the fresh title, swells with the O1
    /// crackle, is heard muffled from inside 07 while the camera is in the lens and moves out beside it as the camera
    /// leaves (O10); it gives way to the radio's own wake over the shoulder (O12), or at the chase frame;</item>
    /// <item>each <see cref="RoverWakeBeatReached"/> plays its cues from <see cref="WakeCueSheet"/>: the thump, the
    /// ghost chord D-A-E surfacing at O1, the lid ticks, the lens whine, the focus landing, the short wake's tick;</item>
    /// <item>the New Game self-test: each check's motion from 07 when it starts (<see cref="BootCheckStarted"/>, the
    /// iris, the lamps, the head, the wheels, the motor, the cutter's failed ignition, the stuck wing, the antenna,
    /// the cells, the assembly's query, the recovery's ready chord), and 07's fault voice when a check comes out
    /// <see cref="BootCheckResult.Fault"/> (<see cref="BootCheckCompleted"/>). The black, the lid and the lens hunt
    /// (Carrier, Resume, Agenda) are already the wake's own beats.</item>
    /// </list>
    /// The pause menu ducks all of it with the world (<see cref="AudioDirector.WorldGain"/>), and delayed cues wait.
    /// Initialises after the <see cref="AudioDirector"/>, whose library it resolves its cues from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WakeAudio : MonoBehaviour, IGameSystem
    {
        private const int PendingCapacity = 4;

        /// <summary>The checks with a sound of their own (the black's Carrier, Resume and Agenda are the wake's beats).
        /// </summary>
        private static readonly (BootCheckStep Step, string CueId)[] CheckCueIds =
        {
            (BootCheckStep.Lens, AudioCueIds.BootLens), (BootCheckStep.Lamps, AudioCueIds.BootLamps),
            (BootCheckStep.Head, AudioCueIds.BootHead), (BootCheckStep.Wheels, AudioCueIds.BootWheels),
            (BootCheckStep.Motor, AudioCueIds.BootMotor), (BootCheckStep.Cutter, AudioCueIds.BootCutter),
            (BootCheckStep.SolarWing, AudioCueIds.BootWingJam), (BootCheckStep.Antenna, AudioCueIds.BootAntenna),
            (BootCheckStep.Battery, AudioCueIds.BootBattery), (BootCheckStep.Assembly, AudioCueIds.BootAssembly),
            (BootCheckStep.Begin, AudioCueIds.BootBegin),
        };

        [Tooltip("The audio director (library, voices, buses).")]
        [SerializeField] private AudioDirector _director;

        [Tooltip("Assets/_Project/Data/Audio/RoverAudioTuning.asset; the wake is its Wake block.")]
        [SerializeField] private RoverAudioTuning _tuning;

        private readonly WakeCueCall[] _calls = new WakeCueCall[WakeCueSheet.MaxCalls];
        private readonly WakeCueCall[] _pending = new WakeCueCall[PendingCapacity];
        private readonly float[] _pendingLeft = new float[PendingCapacity];
        private readonly CueHandle[] _cues = new CueHandle[(int)WakeCueKind.Whine + 1];
        private readonly CueHandle[] _checks = new CueHandle[(int)BootCheckStep.Begin + 1];
        private IRoverState _rover;
        private CueHandle _fault;
        private WakeCueSheet _sheet;
        private WakeAudioSettings _settings;
        private IDisposable[] _subscriptions;
        private AudioSource _static;
        private AudioLowPassFilter _muffle;
        private float _staticCueVolume;
        private float _level;
        private float _target;
        private float _fadeSeconds = 1f;
        private float _beside;
        private bool _narrowing;
        private bool _paused;
        private int _pendingCount;

        /// <summary>The Asleep static's source (for tests and tooling).</summary>
        internal AudioSource StaticSource => _static;

        internal void Wire(AudioDirector director, RoverAudioTuning tuning)
        {
            _director = director;
            _tuning = tuning;
        }

        public void Initialize(GameContext context)
        {
            if (_director == null || _tuning == null)
            {
                Debug.LogError($"{nameof(WakeAudio)}: the audio director or the rover audio tuning is not assigned.",
                    this);
                enabled = false;
                return;
            }

            CueHandle asleep = _director.Resolve(AudioCueIds.RadioCarrier);
            _cues[(int)WakeCueKind.Thump] = _director.Resolve(AudioCueIds.WakePowerThump);
            _cues[(int)WakeCueKind.Ghost] = _director.Resolve(AudioCueIds.GhostChord);
            _cues[(int)WakeCueKind.Tick] = _director.Resolve(AudioCueIds.LidTick);
            _cues[(int)WakeCueKind.Whine] = _director.Resolve(AudioCueIds.LensWhine);
            _fault = _director.Resolve(AudioCueIds.BootFault);
            bool resolved = asleep.IsValid && _fault.IsValid;
            foreach (CueHandle cue in _cues)
            {
                resolved &= cue.IsValid;
            }

            for (int i = 0; i < CheckCueIds.Length; i++)
            {
                _checks[(int)CheckCueIds[i].Step] = _director.Resolve(CheckCueIds[i].CueId);
                resolved &= _checks[(int)CheckCueIds[i].Step].IsValid;
            }

            if (!resolved)
            {
                enabled = false;
                return;
            }

            _settings = _tuning.Wake;
            _sheet = new WakeCueSheet(_settings);
            _staticCueVolume = _director.Library.GetCue(asleep).VolumeMax;
            _static = _director.CreateLoopSource(transform, "AsleepStatic", asleep, 0f);
            _muffle = _static.gameObject.AddComponent<AudioLowPassFilter>();
            _muffle.cutoffFrequency = _settings.InsideCutoff;
            _rover = context.Get<IRoverState>();
            EventBus events = context.Events;
            _subscriptions = new[]
            {
                events.Subscribe<SessionStarted>(OnSessionStarted),
                events.Subscribe<RoverWakeBeatReached>(OnBeat),
                events.Subscribe<PauseChanged>(OnPauseChanged),
                events.Subscribe<BootCheckStarted>(OnCheckStarted),
                events.Subscribe<BootCheckCompleted>(OnCheckCompleted),
            };
        }

        /// <summary>A self-test check's motion begins: its sound, from 07 (the black's checks have none here).</summary>
        private void OnCheckStarted(BootCheckStarted started)
        {
            CueHandle cue = _checks[(int)started.Step];
            if (cue.IsValid)
            {
                _director.PlayAt(cue, _rover.Position, _settings.Check * _director.WorldGain);
            }
        }

        /// <summary>A check came out: only a fault speaks (the jam, the receive-only hiss are in the motions).</summary>
        private void OnCheckCompleted(BootCheckCompleted completed)
        {
            if (completed.Result == BootCheckResult.Fault)
            {
                _director.PlayAt(_fault, _rover.Position, _settings.Fault * _director.WorldGain);
            }
        }

        /// <summary>A loaded game wakes the radio its own way; the Asleep static, if it was up, gives way.</summary>
        private void OnSessionStarted(SessionStarted started)
        {
            if (!started.FreshGame)
            {
                FadeStatic(0f, _settings.HandOverSeconds);
            }
        }

        private void OnPauseChanged(PauseChanged pause)
        {
            _paused = pause.Paused;
        }

        private void OnBeat(RoverWakeBeatReached reached)
        {
            switch (reached.Beat)
            {
                case WakeBeat.Crackle:
                    _beside = 0f;
                    _narrowing = false;
                    FadeStatic(_settings.OpeningStatic, _settings.SwellSeconds);
                    break;
                case WakeBeat.PullingOut:
                    _narrowing = true;
                    break;
                case WakeBeat.PulledOut:
                case WakeBeat.OverShoulder:
                case WakeBeat.ChaseFrame:
                case WakeBeat.Stirred:
                    FadeStatic(0f, _settings.HandOverSeconds);
                    break;
            }

            int count = _sheet.CuesFor(reached.Beat, reached.Hurried, _calls);
            for (int i = 0; i < count; i++)
            {
                if (_calls[i].Delay > 0f)
                {
                    Queue(_calls[i]);
                }
                else
                {
                    Play(_calls[i]);
                }
            }
        }

        private void FadeStatic(float target, float seconds)
        {
            _target = target;
            _fadeSeconds = Mathf.Max(seconds, Mathf.Epsilon);
            if (target > 0f && !_static.isPlaying)
            {
                _static.Play();
            }
        }

        private void Queue(WakeCueCall call)
        {
            if (_pendingCount == PendingCapacity)
            {
                Play(_pending[0]);
                RemovePending(0);
            }

            _pending[_pendingCount] = call;
            _pendingLeft[_pendingCount] = call.Delay;
            _pendingCount++;
        }

        private void RemovePending(int index)
        {
            for (int i = index; i < _pendingCount - 1; i++)
            {
                _pending[i] = _pending[i + 1];
                _pendingLeft[i] = _pendingLeft[i + 1];
            }

            _pendingCount--;
        }

        private void Play(WakeCueCall call)
        {
            _director.PlayVariant2D(_cues[(int)call.Kind], call.Variant, call.Volume * _director.WorldGain);
        }

        private void Update()
        {
            if (_static == null)
            {
                return;
            }

            float deltaTime = _paused ? 0f : Time.deltaTime;
            for (int i = _pendingCount - 1; i >= 0; i--)
            {
                _pendingLeft[i] -= deltaTime;
                if (_pendingLeft[i] <= 0f)
                {
                    Play(_pending[i]);
                    RemovePending(i);
                }
            }

            _level = Mathf.MoveTowards(_level, _target, Time.unscaledDeltaTime / _fadeSeconds);
            if (_narrowing)
            {
                _beside = Mathf.MoveTowards(_beside, 1f, deltaTime / _settings.NarrowSeconds);
            }

            float beside = _beside * _beside * (3f - 2f * _beside);
            _muffle.cutoffFrequency = Mathf.Lerp(_settings.InsideCutoff, _settings.BesideCutoff, beside);
            _static.panStereo = _settings.BesidePan * beside;
            _static.volume = Mathf.Clamp01(_level * _staticCueVolume) * _director.Buses.Effective(AudioBus.Music)
                * _director.WorldGain;
            if (_level <= 0f && _target <= 0f && _static.isPlaying)
            {
                _static.Stop();
            }
        }

        private void OnDestroy()
        {
            if (_subscriptions == null)
            {
                return;
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
