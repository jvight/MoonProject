using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Gives every gameplay event its sound: sonar ping, relic answers (each relic on its own note, softer and darker
    /// with distance), scrap chimes
    /// climbing the pentatonic by combo step, tether pluck / hum (follows the beam emitter while attached) / release
    /// or sighing snap, excavation rumble while the beam lifts plus the surfacing sparkle, the shelf "placed" cue and
    /// the upgrade arpeggio. Loops fade with <see cref="LoopFader"/> and stop when silent. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 10;

        [Tooltip("Assets/_Project/Data/Audio/GameplayAudioTuning.asset.")]
        [SerializeField] private GameplayAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly LoopFader _tetherFader = new LoopFader();
        private readonly LoopFader _rumbleFader = new LoopFader();
        private readonly Dictionary<string, int> _relicVoices = new Dictionary<string, int>(StringComparer.Ordinal);
        private AudioDirector _director;
        private Transform _tetherOrigin;
        private CueHandle _sonarPing;
        private CueHandle _relicAnswer;
        private CueHandle _scrapChime;
        private CueHandle _tetherAttach;
        private CueHandle _tetherRelease;
        private CueHandle _tetherSnap;
        private CueHandle _surfacingSparkle;
        private CueHandle _relicPlaced;
        private CueHandle _upgradeArpeggio;
        private AudioSource _tetherHum;
        private AudioSource _rumble;
        private float _tetherHumCueVolume;
        private float _rumbleCueVolume;
        private int _scrapNotes;

        /// <summary>True while the tether hum can be heard (including its fade-out).</summary>
        public bool TetherHumAudible => _tetherFader.IsAudible;

        /// <summary>True while the excavation rumble can be heard (including its fade-out).</summary>
        public bool RumbleAudible => _rumbleFader.IsAudible;

        internal AudioSource TetherHumSource => _tetherHum;

        internal AudioSource RumbleSource => _rumble;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(GameplayAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _director = director;
            _tetherOrigin = context.Get<IRoverRig>().TetherOrigin;
            if (_tetherOrigin == null)
            {
                Debug.LogError($"{nameof(GameplayAudio)}: {nameof(IRoverRig)}.TetherOrigin is null.", this);
                enabled = false;
                return;
            }

            _sonarPing = director.Resolve(AudioCueIds.SonarPing);
            _relicAnswer = director.Resolve(AudioCueIds.RelicAnswer);
            _scrapChime = director.Resolve(AudioCueIds.ScrapChime);
            _tetherAttach = director.Resolve(AudioCueIds.TetherAttach);
            _tetherRelease = director.Resolve(AudioCueIds.TetherRelease);
            _tetherSnap = director.Resolve(AudioCueIds.TetherSnap);
            _surfacingSparkle = director.Resolve(AudioCueIds.SurfacingSparkle);
            _relicPlaced = director.Resolve(AudioCueIds.RelicPlaced);
            _upgradeArpeggio = director.Resolve(AudioCueIds.UpgradeArpeggio);
            CueHandle hum = director.Resolve(AudioCueIds.TetherHum);
            CueHandle rumble = director.Resolve(AudioCueIds.ExcavationRumble);
            if (!(_sonarPing.IsValid && _relicAnswer.IsValid && _scrapChime.IsValid && _tetherAttach.IsValid &&
                  _tetherRelease.IsValid && _tetherSnap.IsValid && _surfacingSparkle.IsValid && _relicPlaced.IsValid &&
                  _upgradeArpeggio.IsValid && hum.IsValid && rumble.IsValid))
            {
                enabled = false;
                return;
            }

            _scrapNotes = director.Library.GetCue(_scrapChime).ClipCount;
            AudioCue answers = director.Library.GetCue(_relicAnswer);
            for (int i = 0; i < answers.ClipCount; i++)
            {
                _relicVoices[answers.GetVariantLabel(i)] = i;
            }

            _tetherHumCueVolume = director.Library.GetCue(hum).VolumeMax;
            _rumbleCueVolume = director.Library.GetCue(rumble).VolumeMax;
            _tetherHum = director.CreateLoopSource(transform, "TetherHum", hum, _tuning.TetherHumSpatialBlend);
            _rumble = director.CreateLoopSource(transform, "ExcavationRumble", rumble, 1f);

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<SonarPinged>(OnSonarPinged);
            _subscriptions[1] = events.Subscribe<RelicAnswered>(OnRelicAnswered);
            _subscriptions[2] = events.Subscribe<ScrapCollected>(OnScrapCollected);
            _subscriptions[3] = events.Subscribe<TetherAttached>(OnTetherAttached);
            _subscriptions[4] = events.Subscribe<TetherReleased>(OnTetherReleased);
            _subscriptions[5] = events.Subscribe<ExcavationStarted>(OnExcavationStarted);
            _subscriptions[6] = events.Subscribe<ExcavationStopped>(OnExcavationStopped);
            _subscriptions[7] = events.Subscribe<RelicSurfaced>(OnRelicSurfaced);
            _subscriptions[8] = events.Subscribe<RelicDeposited>(OnRelicDeposited);
            _subscriptions[9] = events.Subscribe<UpgradePurchased>(OnUpgradePurchased);
        }

        internal void Wire(GameplayAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx);
            if (_tetherFader.IsAudible || _tetherFader.IsOn)
            {
                _tetherHum.transform.position = _tetherOrigin.position;
            }

            StepLoop(_tetherHum, _tetherFader, dt, _tuning.TetherHumFadeIn, _tuning.TetherHumFadeOut,
                _tetherHumCueVolume * _tuning.TetherHumVolume * sfx);
            StepLoop(_rumble, _rumbleFader, dt, _tuning.RumbleFadeIn, _tuning.RumbleFadeOut,
                _rumbleCueVolume * _tuning.RumbleVolume * sfx);
        }

        private static void StepLoop(AudioSource source, LoopFader fader, float dt, float fadeIn, float fadeOut,
            float volume)
        {
            fader.Step(dt, fadeIn, fadeOut);
            source.volume = fader.Gain * volume;
            if (fader.IsAudible && !source.isPlaying)
            {
                source.Play();
            }
            else if (!fader.IsAudible && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void OnSonarPinged(SonarPinged ping)
        {
            _director.PlayAt(_sonarPing, ping.Origin);
        }

        private void OnRelicAnswered(RelicAnswered answer)
        {
            if (answer.RelicId == null || !_relicVoices.TryGetValue(answer.RelicId, out int voice))
            {
                Debug.LogError($"{nameof(GameplayAudio)}: no answer voice for relic '{answer.RelicId}'. Re-run " +
                               "tools/audio/build_sfx.py (it reads Data/Content/Relics) and the Audio/Library builder.",
                    this);
                return;
            }

            RelicAnswerTone tone = RelicAnswerTone.ForDistance(answer.Distance, _tuning);
            _director.PlayFilteredAt(_relicAnswer, voice, answer.Position, tone.Volume, tone.CutoffHz,
                _tuning.AnswerMinDistance);
        }

        private void OnScrapCollected(ScrapCollected scrap)
        {
            int note = ScrapMelody.NoteIndex(scrap.ComboStep, _scrapNotes, _tuning.ScrapTopWindow);
            _director.PlayVariantAt(_scrapChime, note, scrap.Position);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            float heft = Mathf.InverseLerp(_tuning.TetherLightMass, _tuning.TetherHeavyMass, attached.Mass);
            _director.PlayAt(_tetherAttach, attached.Position,
                Mathf.Lerp(_tuning.TetherLightVolume, _tuning.TetherHeavyVolume, heft));
            _tetherHum.transform.position = _tetherOrigin.position;
            _tetherFader.FadeIn();
        }

        private void OnTetherReleased(TetherReleased released)
        {
            _director.PlayAt(released.Snapped ? _tetherSnap : _tetherRelease, released.Position);
            _tetherFader.FadeOut();
        }

        private void OnExcavationStarted(ExcavationStarted started)
        {
            _rumble.transform.position = started.Position;
            _rumbleFader.FadeIn();
        }

        private void OnExcavationStopped(ExcavationStopped stopped)
        {
            _rumbleFader.FadeOut();
        }

        private void OnRelicSurfaced(RelicSurfaced surfaced)
        {
            _director.PlayAt(_surfacingSparkle, surfaced.Position);
        }

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            _director.PlayAt(_relicPlaced, deposited.Position);
        }

        private void OnUpgradePurchased(UpgradePurchased upgrade)
        {
            _director.Play2D(_upgradeArpeggio);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
