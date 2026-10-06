using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Voices every <see cref="UiCue"/> with a soft 2D cue in D major pentatonic. Focus steps and slider ticks are
    /// rate-limited and softened under rapid repeats (<see cref="RepeatSoftener"/>); the hold-to-buy swell plays on
    /// its own source so it hands over to the chord when the hold completes, releases with a soft tail when the
    /// hold is let go (<see cref="UiCueKind.HoldRelease"/>) or the menu opens, and restarts on a re-press. Runs on
    /// unscaled time (UI works while paused). Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiAudio : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/UiAudioTuning.asset.")]
        [SerializeField] private UiAudioTuning _tuning;

        private readonly RepeatSoftener _focusRepeats = new RepeatSoftener();
        private readonly RepeatSoftener _sliderRepeats = new RepeatSoftener();
        private AudioDirector _director;
        private CueHandle _menuOpen;
        private CueHandle _menuClose;
        private CueHandle _focus;
        private CueHandle _confirm;
        private CueHandle _back;
        private CueHandle _card;
        private CueHandle _holdComplete;
        private CueHandle _prompt;
        private CueHandle _slider;
        private AudioSource _holdSwell;
        private float _holdCueVolume;
        private bool _holdStopping;
        private float _holdFadeTime;
        private IDisposable _subscription;

        /// <summary>True while the hold swell is sounding (including its short let-go fade).</summary>
        public bool HoldSwellPlaying => _holdSwell != null && _holdSwell.isPlaying;

        internal AudioSource HoldSwellSource => _holdSwell;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(UiAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _director = director;
            _menuOpen = director.Resolve(AudioCueIds.UiMenuOpen);
            _menuClose = director.Resolve(AudioCueIds.UiMenuClose);
            _focus = director.Resolve(AudioCueIds.UiFocus);
            _confirm = director.Resolve(AudioCueIds.UiConfirm);
            _back = director.Resolve(AudioCueIds.UiBack);
            _card = director.Resolve(AudioCueIds.UiCard);
            _holdComplete = director.Resolve(AudioCueIds.UiHoldComplete);
            _prompt = director.Resolve(AudioCueIds.UiPrompt);
            _slider = director.Resolve(AudioCueIds.UiSlider);
            CueHandle holdFill = director.Resolve(AudioCueIds.UiHoldFill);
            if (!(_menuOpen.IsValid && _menuClose.IsValid && _focus.IsValid && _confirm.IsValid && _back.IsValid &&
                  _card.IsValid && _holdComplete.IsValid && _prompt.IsValid && _slider.IsValid && holdFill.IsValid))
            {
                enabled = false;
                return;
            }

            _holdSwell = director.CreateLoopSource(transform, "HoldSwell", holdFill, 0f);
            _holdSwell.loop = false;
            _holdCueVolume = director.Library.GetCue(holdFill).VolumeMax;
            _subscription = context.Events.Subscribe<UiCue>(OnUiCue);
        }

        internal void Wire(UiAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_holdSwell == null || !_holdSwell.isPlaying)
            {
                return;
            }

            float full = _holdCueVolume * _director.Buses.Effective(AudioBus.Sfx);
            if (!_holdStopping)
            {
                _holdSwell.volume = full;
                return;
            }

            float step = full * Time.unscaledDeltaTime / _holdFadeTime;
            _holdSwell.volume = Mathf.Max(0f, _holdSwell.volume - step);
            if (_holdSwell.volume <= 0f)
            {
                _holdSwell.Stop();
                _holdStopping = false;
            }
        }

        private void OnUiCue(UiCue cue)
        {
            switch (cue.Kind)
            {
                case UiCueKind.MenuOpen:
                    LetGoOfHold(_tuning.HoldReleaseFade);
                    _director.Play2D(_menuOpen);
                    break;
                case UiCueKind.MenuClose:
                    _director.Play2D(_menuClose);
                    break;
                case UiCueKind.FocusMove:
                    PlaySoftened(_focus, _focusRepeats, _tuning.FocusMinInterval);
                    break;
                case UiCueKind.Confirm:
                    _director.Play2D(_confirm);
                    break;
                case UiCueKind.Back:
                    _director.Play2D(_back);
                    break;
                case UiCueKind.CardShown:
                    _director.Play2D(_card);
                    break;
                case UiCueKind.HoldFill:
                    StartHold();
                    break;
                case UiCueKind.HoldComplete:
                    LetGoOfHold(_tuning.HoldCompleteFade);
                    _director.Play2D(_holdComplete);
                    break;
                case UiCueKind.HoldRelease:
                    LetGoOfHold(_tuning.HoldReleaseFade);
                    break;
                case UiCueKind.PromptShown:
                    _director.Play2D(_prompt);
                    break;
                case UiCueKind.SliderStep:
                    PlaySoftened(_slider, _sliderRepeats, _tuning.SliderMinInterval);
                    break;
                default:
                    Debug.LogError($"{nameof(UiAudio)}: no sound for UI cue {cue.Kind}.", this);
                    break;
            }
        }

        private void PlaySoftened(CueHandle cue, RepeatSoftener repeats, float minInterval)
        {
            if (repeats.TryTrigger(Time.unscaledTime, minInterval, _tuning.StreakWindow, _tuning.StreakDecay,
                    _tuning.StreakFloor, out float gain))
            {
                _director.Play2D(cue, gain);
            }
        }

        private void StartHold()
        {
            _holdStopping = false;
            _holdSwell.Stop();
            _holdSwell.timeSamples = 0;
            _holdSwell.volume = _holdCueVolume * _director.Buses.Effective(AudioBus.Sfx);
            _holdSwell.Play();
        }

        private void LetGoOfHold(float fadeTime)
        {
            if (_holdSwell.isPlaying)
            {
                _holdStopping = true;
                _holdFadeTime = fadeTime;
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
