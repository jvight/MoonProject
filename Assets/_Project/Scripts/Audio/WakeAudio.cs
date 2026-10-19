using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's wake as heard from inside it (docs/design/cutscenes.md §1, docs/features/M3-09 Part A):
    /// <list type="bullet">
    /// <item>the radio's Asleep profile: quiet out-of-tune static comes up under the fresh title, swells with the
    /// crackle in the black, heard muffled from inside 07, and gives way as the self-test's checks begin, or at the
    /// chase frame;</item>
    /// <item>each <see cref="RoverWakeBeatReached"/> plays its cues from <see cref="WakeCueSheet"/>: the thump, the
    /// ghost chord D-A-E surfacing in the black, the lid ticks as the picture and the eyes open, the short wake's
    /// tick;</item>
    /// <item>the New Game self-test, heard from 07: a check made of one motion plays it when it starts
    /// (<see cref="BootCheckStarted"/>: the iris, the cells' charge whine, the assembly's query); a check made of
    /// several plays each on its <see cref="BootCheckPulse"/> (the lamps' flickers and ramp, the head's sweep and nod,
    /// each wheel, the motor's shudder and creep, the cutter's sparks, the wing's jerk, stall and fold, the antenna's
    /// rise and signal, and the ready performance's glance and headlamps clicked off and on), once per pulse
    /// even where Rover repeats one as progress; 07's fault voice when a check comes out
    /// <see cref="BootCheckResult.Fault"/> (<see cref="BootCheckCompleted"/>). When the player fast-forwards the
    /// readout (<see cref="BootCheckFastForwarded"/>) one soft rush plays instead of the remaining lines' sounds, which
    /// would otherwise chatter, and the ready performance after <see cref="BootCheckFinished"/> sounds as usual. The
    /// lines of information (Carrier, Resume, Agenda) have no motion of their own, and the cells' ticks are the
    /// battery's (<see cref="BatteryAudio"/>).</item>
    /// <item>07's face booting: the tube's degauss thunk and whine (<see cref="CrtPowerOn"/>), the maker's mark traced
    /// stroke by stroke up the pentatonic (<see cref="LogoStrokeDrawn"/>) and resolving as its wordmark settles
    /// (<see cref="BootLineCompleted"/> of line 0), each typed character pair's soft counter tick, softened in a run
    /// (<see cref="BootTextTyped"/>), each settled line's heavier tick, and the load bar's steps, the fifth resolving
    /// (<see cref="LoadStep"/>); silent too while a fast-forward's rush stands in.</item>
    /// </list>
    /// The pause menu ducks all of it with the world (<see cref="AudioDirector.WorldGain"/>), and delayed cues wait.
    /// Initialises after the <see cref="AudioDirector"/>, whose library it resolves its cues from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WakeAudio : MonoBehaviour, IGameSystem
    {
        private const int PendingCapacity = 4;

        /// <summary>Most pulse indices one check uses (Rover's: the cutter's sparks 0..2 and beacon blinks 3..5).</summary>
        private const int MaxPulseIndex = 8;

        /// <summary>The checks of a single motion, heard as they start.</summary>
        private static readonly (BootCheckStep Step, string CueId)[] StartCueIds =
        {
            (BootCheckStep.Lens, AudioCueIds.BootLens), (BootCheckStep.Battery, AudioCueIds.BootBattery),
            (BootCheckStep.Assembly, AudioCueIds.BootAssembly),
        };

        /// <summary>
        /// The checks' motions on their pulses (Rover's indices), each with its variant and its share of the check
        /// level: the flickers are uneven, the cutter's attempts weaker each time. The beacon's blinks (Cutter 3..5) are
        /// the lander's light, not 07's, and have no sound.
        /// </summary>
        private static readonly PulseSound[] PulseSounds =
        {
            new PulseSound(BootCheckStep.Lamps, 0, AudioCueIds.BootLampFlicker, 0, 1f),
            new PulseSound(BootCheckStep.Lamps, 1, AudioCueIds.BootLampFlicker, 1, 0.7f),
            new PulseSound(BootCheckStep.Lamps, 2, AudioCueIds.BootLampFlicker, 2, 0.85f),
            new PulseSound(BootCheckStep.Lamps, 3, AudioCueIds.BootLampRamp, 0, 1f),
            new PulseSound(BootCheckStep.Head, 0, AudioCueIds.BootHeadSweep, 0, 1f),
            new PulseSound(BootCheckStep.Head, 1, AudioCueIds.BootHeadNod, 0, 1f),
            new PulseSound(BootCheckStep.Wheels, 0, AudioCueIds.BootWheel, 0, 1f),
            new PulseSound(BootCheckStep.Wheels, 1, AudioCueIds.BootWheel, 1, 1f),
            new PulseSound(BootCheckStep.Wheels, 2, AudioCueIds.BootWheel, 2, 1f),
            new PulseSound(BootCheckStep.Wheels, 3, AudioCueIds.BootWheel, 3, 1f),
            new PulseSound(BootCheckStep.Motor, 0, AudioCueIds.BootMotorShudder, 0, 1f),
            new PulseSound(BootCheckStep.Motor, 1, AudioCueIds.BootMotorCreep, 0, 1f),
            new PulseSound(BootCheckStep.Cutter, 0, AudioCueIds.BootCutterSpark, 0, 1f),
            new PulseSound(BootCheckStep.Cutter, 1, AudioCueIds.BootCutterSpark, 1, 0.8f),
            new PulseSound(BootCheckStep.Cutter, 2, AudioCueIds.BootCutterSpark, 2, 0.55f),
            new PulseSound(BootCheckStep.SolarWing, 0, AudioCueIds.BootWingJerk, 0, 1f),
            new PulseSound(BootCheckStep.SolarWing, 1, AudioCueIds.BootWingStall, 0, 1f),
            new PulseSound(BootCheckStep.SolarWing, 2, AudioCueIds.BootWingFold, 0, 1f),
            new PulseSound(BootCheckStep.Antenna, 0, AudioCueIds.BootAntennaRise, 0, 1f),
            new PulseSound(BootCheckStep.Antenna, 1, AudioCueIds.BootAntennaSignal, 0, 1f),
            new PulseSound(BootCheckStep.Begin, 0, AudioCueIds.BootBeginTurn, 0, 1f),
            new PulseSound(BootCheckStep.Begin, 2, AudioCueIds.BootBeginLamps, 0, 1f),
            new PulseSound(BootCheckStep.Begin, 3, AudioCueIds.BootBeginLamps, 0, 0.6f),
        };

        [Tooltip("The audio director (library, voices, buses).")]
        [SerializeField] private AudioDirector _director;

        [Tooltip("Assets/_Project/Data/Audio/RoverAudioTuning.asset; the wake is its Wake block.")]
        [SerializeField] private RoverAudioTuning _tuning;

        private readonly WakeCueCall[] _calls = new WakeCueCall[WakeCueSheet.MaxCalls];
        private readonly WakeCueCall[] _pending = new WakeCueCall[PendingCapacity];
        private readonly float[] _pendingLeft = new float[PendingCapacity];
        private readonly CueHandle[] _cues = new CueHandle[(int)WakeCueKind.Tick + 1];
        private readonly CueHandle[] _checks = new CueHandle[(int)BootCheckStep.Begin + 1];
        private readonly CueHandle[] _pulseCues = new CueHandle[PulseSounds.Length];
        private readonly bool[] _pulseHeard = new bool[MaxPulseIndex];
        private IRoverState _rover;
        private CueHandle _fault;
        private CueHandle _rush;
        private CueHandle _powerOn;
        private CueHandle _stroke;
        private CueHandle _resolve;
        private CueHandle _textTick;
        private CueHandle _lineDone;
        private CueHandle _loadStep;
        private readonly RepeatSoftener _textRepeats = new RepeatSoftener();
        private int _strokeNotes;
        private WakeCueSheet _sheet;
        private WakeAudioSettings _settings;
        private IDisposable[] _subscriptions;
        private AudioSource _static;
        private AudioLowPassFilter _muffle;
        private float _staticCueVolume;
        private float _level;
        private float _target;
        private float _fadeSeconds = 1f;
        private bool _paused;
        private int _pendingCount;
        private bool _rushed;

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
            _fault = _director.Resolve(AudioCueIds.BootFault);
            _rush = _director.Resolve(AudioCueIds.BootRush);
            _powerOn = _director.Resolve(AudioCueIds.FacePowerOn);
            _stroke = _director.Resolve(AudioCueIds.FaceLogoStroke);
            _resolve = _director.Resolve(AudioCueIds.FaceLogoResolve);
            _textTick = _director.Resolve(AudioCueIds.FaceTextTick);
            _lineDone = _director.Resolve(AudioCueIds.FaceLineDone);
            _loadStep = _director.Resolve(AudioCueIds.FaceLoadStep);
            bool resolved = asleep.IsValid && _fault.IsValid && _rush.IsValid && _powerOn.IsValid && _stroke.IsValid &&
                            _resolve.IsValid && _textTick.IsValid && _lineDone.IsValid && _loadStep.IsValid;
            foreach (CueHandle cue in _cues)
            {
                resolved &= cue.IsValid;
            }

            for (int i = 0; i < StartCueIds.Length; i++)
            {
                _checks[(int)StartCueIds[i].Step] = _director.Resolve(StartCueIds[i].CueId);
                resolved &= _checks[(int)StartCueIds[i].Step].IsValid;
            }

            for (int i = 0; i < PulseSounds.Length; i++)
            {
                _pulseCues[i] = _director.Resolve(PulseSounds[i].CueId);
                resolved &= _pulseCues[i].IsValid;
            }

            if (!resolved)
            {
                enabled = false;
                return;
            }

            _settings = _tuning.Wake;
            _strokeNotes = _director.Library.GetCue(_stroke).ClipCount;
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
                events.Subscribe<BootCheckPulse>(OnCheckPulse),
                events.Subscribe<BootCheckFastForwarded>(OnFastForwarded),
                events.Subscribe<BootCheckFinished>(OnChecksFinished),
                events.Subscribe<CrtPowerOn>(OnCrtPowerOn),
                events.Subscribe<LogoStrokeDrawn>(OnLogoStrokeDrawn),
                events.Subscribe<BootTextTyped>(OnBootTextTyped),
                events.Subscribe<BootLineCompleted>(OnBootLineCompleted),
                events.Subscribe<LoadStep>(OnLoadStep),
            };
        }

        /// <summary>A self-test check begins: a single motion sounds now, from 07; its pulses start fresh.</summary>
        private void OnCheckStarted(BootCheckStarted started)
        {
            for (int i = 0; i < _pulseHeard.Length; i++)
            {
                _pulseHeard[i] = false;
            }

            CueHandle cue = _checks[(int)started.Step];
            if (cue.IsValid && !_rushed)
            {
                _director.PlayAt(cue, _rover.Position, _settings.Check * _director.WorldGain);
            }
        }

        /// <summary>One of a check's motions: its sound once, the first time Rover marks that motion.</summary>
        private void OnCheckPulse(BootCheckPulse pulse)
        {
            if (_rushed || pulse.Index < 0 || pulse.Index >= _pulseHeard.Length || _pulseHeard[pulse.Index])
            {
                return;
            }

            for (int i = 0; i < PulseSounds.Length; i++)
            {
                PulseSound sound = PulseSounds[i];
                if (sound.Step == pulse.Step && sound.Index == pulse.Index)
                {
                    _pulseHeard[pulse.Index] = true;
                    _director.PlayVariantAt(_pulseCues[i], sound.Variant, _rover.Position,
                        _settings.Check * sound.Share * _director.WorldGain);
                    return;
                }
            }
        }

        /// <summary>A check came out: only a fault speaks (the jam, the receive-only hiss are in the motions).</summary>
        private void OnCheckCompleted(BootCheckCompleted completed)
        {
            if (completed.Result == BootCheckResult.Fault && !_rushed)
            {
                _director.PlayAt(_fault, _rover.Position, _settings.Fault * _director.WorldGain);
            }
        }

        /// <summary>
        /// The player hurried the readout: one rush now, and the remaining lines run silent (at their faster pace
        /// their motions would chatter) until the checks finish.
        /// </summary>
        private void OnFastForwarded(BootCheckFastForwarded forwarded)
        {
            if (_rushed)
            {
                return;
            }

            _rushed = true;
            _director.PlayAt(_rush, _rover.Position, _settings.Rush * _director.WorldGain);
        }

        private void OnCrtPowerOn(CrtPowerOn powerOn)
        {
            _director.PlayAt(_powerOn, _rover.Position, _settings.FacePowerOn * _director.WorldGain);
        }

        /// <summary>A stroke of the mark: a step up the pentatonic every few strokes, held at the top.</summary>
        private void OnLogoStrokeDrawn(LogoStrokeDrawn drawn)
        {
            if (_rushed)
            {
                return;
            }

            int note = Mathf.Clamp(drawn.Index / Mathf.Max(1, _settings.StrokesPerNote), 0, _strokeNotes - 1);
            _director.PlayVariantAt(_stroke, note, _rover.Position, _settings.FaceStroke * _director.WorldGain);
        }

        /// <summary>A character pair typed: a random counter tick, skipped when too close and softer in a run.</summary>
        private void OnBootTextTyped(BootTextTyped typed)
        {
            if (_rushed || !_textRepeats.TryTrigger(Time.unscaledTime, _settings.TextMinInterval,
                    _settings.TextStreakWindow, _settings.TextStreakDecay, _settings.TextStreakFloor, out float gain))
            {
                return;
            }

            _director.PlayAt(_textTick, _rover.Position, _settings.FaceText * gain * _director.WorldGain);
        }

        /// <summary>A line settled: the heavier tick; the wordmark's (line 0) is the mark whole, so it resolves.</summary>
        private void OnBootLineCompleted(BootLineCompleted completed)
        {
            if (_rushed)
            {
                return;
            }

            CueHandle cue = completed.LineIndex == 0 ? _resolve : _lineDone;
            float level = completed.LineIndex == 0 ? _settings.FaceResolve : _settings.FaceLine;
            _director.PlayAt(cue, _rover.Position, level * _director.WorldGain);
        }

        private void OnLoadStep(LoadStep step)
        {
            if (_rushed)
            {
                return;
            }

            _director.PlayVariantAt(_loadStep, step.Index, _rover.Position, _settings.FaceLoad * _director.WorldGain);
        }

        /// <summary>The readout is done: the ready performance that follows sounds as usual.</summary>
        private void OnChecksFinished(BootCheckFinished finished)
        {
            _rushed = false;
        }

        /// <summary>A loaded game wakes the radio its own way; the Asleep static, if it was up, gives way.</summary>
        private void OnSessionStarted(SessionStarted started)
        {
            _rushed = false;
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
                    FadeStatic(_settings.OpeningStatic, _settings.SwellSeconds);
                    break;
                case WakeBeat.ChecksBegin:
                case WakeBeat.ChaseFrame:
                case WakeBeat.Stirred:
                    FadeStatic(0f, _settings.HandOverSeconds);
                    break;
            }

            int count = _sheet.CuesFor(reached.Beat, _calls);
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
            _muffle.cutoffFrequency = _settings.InsideCutoff;
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

        /// <summary>One motion's sound in a self-test check.</summary>
        private readonly struct PulseSound
        {
            public PulseSound(BootCheckStep step, int index, string cueId, int variant, float share)
            {
                Step = step;
                Index = index;
                CueId = cueId;
                Variant = variant;
                Share = share;
            }

            public BootCheckStep Step { get; }

            public int Index { get; }

            public string CueId { get; }

            public int Variant { get; }

            public float Share { get; }
        }
    }
}
