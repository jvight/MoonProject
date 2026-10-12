using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The 0.5.2 feel beats in sound (docs/design/feel-pass.md, polish-pass.md section 1.1), at most three layers per
    /// beat and mezzo-piano peaks:
    /// <list type="bullet">
    /// <item>the cut and the dig (<see cref="FeelBeat"/>): a glassy charge arpeggio over <c>Seconds</c> (two notes on a
    /// resume) and a falling A-F# on Cancel; ignition's zing and chassis thump (Act, strength 0); the sizzle through
    /// 07's body following progress (later Acts); the crack (Impact); a strap tuk at the fold (Follow, strength 1).
    /// The beam's singing climb and the collect melody stay <see cref="SalvageAudio"/>'s;</item>
    /// <item>the dock (Kind Dock): the walls rising (Anticipate), the clamp (Impact), the walls settling (Reward; the
    /// full chord and the per-cell notes are <see cref="StationAudio"/>'s and <see cref="BatteryAudio"/>'s), the quick
    /// drop (Cancel);</item>
    /// <item>the first hour's small moments: a tech node lighting, the decode's cable ticks, a component found, the
    /// drums' boost whine and release, a relic strapped into the cradle, a new step's page tick, one save tick per
    /// burst, the dock evening's swell, the stock count rolling up (one rising tick per unit, rate-capped and topping
    /// out on the highest note for a big haul) and home's windows waking (one warm click per step, rising).</item>
    /// </list>
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeelAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 11;
        private const int ChargeFresh = 0;
        private const int ChargeResume = 1;
        private const float SilentVolume = 0.001f;
        private const float SemitonesPerOctave = 12f;

        /// <summary>D major pentatonic above the root, in semitones: the windows' clicks climb it.</summary>
        private static readonly float[] PentatonicSemitones = { 0f, 2f, 4f, 7f, 9f, 12f };

        [Tooltip("Assets/_Project/Data/Audio/FeelAudioTuning.asset.")]
        [SerializeField] private FeelAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly EasedValue _sizzle = new EasedValue(0f);
        private readonly EasedValue _boost = new EasedValue(0f);
        private AudioDirector _director;
        private IRoverState _rover;
        private AudioSource _sizzleSource;
        private AudioSource _boostSource;
        private float _sizzleCueVolume;
        private float _boostCueVolume;
        private float _sizzleTarget;
        private bool _boosting;
        private float _sinceSave = float.MaxValue;
        private string _lastStep = string.Empty;
        private CueHandle _charge;
        private CueHandle _cancel;
        private CueHandle _ignite;
        private CueHandle _crack;
        private CueHandle _strap;
        private CueHandle _wallsRise;
        private CueHandle _clamp;
        private CueHandle _wallsSettle;
        private CueHandle _wallsDrop;
        private CueHandle _nodeLight;
        private CueHandle _cableTicks;
        private CueHandle _found;
        private CueHandle _boostRelease;
        private CueHandle _pageTick;
        private CueHandle _saveTick;
        private CueHandle _eveningSwell;
        private CueHandle _countTick;
        private CueHandle _windowClick;
        private int _countNotes;
        private float _sinceCount = float.MaxValue;
        private int _nodeVariants;

        /// <summary>The cut's sizzle level now (0 when the beam is off).</summary>
        public float SizzleLevel => _sizzle.Value;

        internal void Wire(FeelAudioTuning tuning)
        {
            _tuning = tuning;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(FeelAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _charge = director.Resolve(AudioCueIds.CutCharge);
            _cancel = director.Resolve(AudioCueIds.CutCancel);
            _ignite = director.Resolve(AudioCueIds.CutIgnite);
            CueHandle sizzle = director.Resolve(AudioCueIds.CutSizzle);
            _crack = director.Resolve(AudioCueIds.CutCrack);
            _strap = director.Resolve(AudioCueIds.StrapTuk);
            _wallsRise = director.Resolve(AudioCueIds.DockWallsRise);
            _clamp = director.Resolve(AudioCueIds.DockClamp);
            _wallsSettle = director.Resolve(AudioCueIds.DockWallsSettle);
            _wallsDrop = director.Resolve(AudioCueIds.DockWallsDrop);
            _nodeLight = director.Resolve(AudioCueIds.NodeLight);
            _cableTicks = director.Resolve(AudioCueIds.CableTicks);
            _found = director.Resolve(AudioCueIds.ComponentFound);
            CueHandle boost = director.Resolve(AudioCueIds.BoostWhine);
            _boostRelease = director.Resolve(AudioCueIds.BoostRelease);
            _pageTick = director.Resolve(AudioCueIds.PageTick);
            _saveTick = director.Resolve(AudioCueIds.SaveTick);
            _eveningSwell = director.Resolve(AudioCueIds.DockEveningSwell);
            _countTick = director.Resolve(AudioCueIds.CountTick);
            _windowClick = director.Resolve(AudioCueIds.WindowClick);
            if (!(_charge.IsValid && _cancel.IsValid && _ignite.IsValid && sizzle.IsValid && _crack.IsValid &&
                  _strap.IsValid && _wallsRise.IsValid && _clamp.IsValid && _wallsSettle.IsValid &&
                  _wallsDrop.IsValid && _nodeLight.IsValid && _cableTicks.IsValid && _found.IsValid &&
                  boost.IsValid && _boostRelease.IsValid && _pageTick.IsValid && _saveTick.IsValid &&
                  _eveningSwell.IsValid && _countTick.IsValid && _windowClick.IsValid))
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            _nodeVariants = director.Library.GetCue(_nodeLight).ClipCount;
            _countNotes = director.Library.GetCue(_countTick).ClipCount;
            _sizzleSource = director.CreateLoopSource(transform, "CutSizzle", sizzle, 1f);
            _sizzleCueVolume = director.Library.GetCue(sizzle).VolumeMax;
            _boostSource = director.CreateLoopSource(transform, "BoostWhine", boost, 1f);
            _boostCueVolume = director.Library.GetCue(boost).VolumeMax;

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<FeelBeat>(OnFeelBeat);
            _subscriptions[1] = events.Subscribe<TechNodeChanged>(OnTechNodeChanged);
            _subscriptions[2] = events.Subscribe<DecodeHandOff>(OnDecodeHandOff);
            _subscriptions[3] = events.Subscribe<ComponentFound>(OnComponentFound);
            _subscriptions[4] = events.Subscribe<RoverBoostChanged>(OnBoostChanged);
            _subscriptions[5] = events.Subscribe<RelicStowed>(OnRelicStowed);
            _subscriptions[6] = events.Subscribe<NextStepChanged>(OnNextStepChanged);
            _subscriptions[7] = events.Subscribe<GameSaved>(OnGameSaved);
            _subscriptions[8] = events.Subscribe<DockEveningOpened>(OnDockEveningOpened);
            _subscriptions[9] = events.Subscribe<StockCountTicked>(OnStockCountTicked);
            _subscriptions[10] = events.Subscribe<HomeWindowLit>(OnHomeWindowLit);
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _sinceSave += Time.unscaledDeltaTime;
            _sinceCount += Time.unscaledDeltaTime;
            float world = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            float sizzle = _sizzle.Step(_sizzleTarget, dt, _tuning.SizzleEase);
            Loop(_sizzleSource, sizzle * _sizzleCueVolume * world);
            float boost = _boost.Step(_boosting ? 1f : 0f, dt, _tuning.BoostEase);
            _boostSource.transform.position = _rover.Position;
            Loop(_boostSource, boost * _tuning.BoostVolume * _boostCueVolume * world);
        }

        /// <summary>A loop plays only while it is heard.</summary>
        private static void Loop(AudioSource source, float volume)
        {
            source.volume = volume;
            if (volume > SilentVolume && !source.isPlaying)
            {
                source.Play();
            }
            else if (volume <= SilentVolume && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            switch (beat.Kind)
            {
                case FeelKind.Cut:
                case FeelKind.Dig:
                    OnCutBeat(beat);
                    break;
                case FeelKind.Dock:
                    OnDockBeat(beat);
                    break;
            }
        }

        private void OnCutBeat(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    int variant = beat.Seconds >= _tuning.FreshChargeSeconds ? ChargeFresh : ChargeResume;
                    _director.PlayVariantAt(_charge, variant, beat.Position);
                    break;
                case FeelPhase.Cancel:
                    _sizzleTarget = 0f;
                    _director.PlayAt(_cancel, beat.Position);
                    break;
                case FeelPhase.Act:
                    _sizzleSource.transform.position = beat.Position;
                    _sizzleTarget = Mathf.Lerp(_tuning.SizzleMin, _tuning.SizzleMax, beat.Strength);
                    if (beat.Strength <= 0f)
                    {
                        _director.PlayAt(_ignite, beat.Position);
                    }

                    break;
                case FeelPhase.Impact:
                    _sizzleTarget = 0f;
                    _director.PlayAt(_crack, beat.Position);
                    break;
                case FeelPhase.Follow:
                    if (beat.Strength >= 1f)
                    {
                        _director.PlayAt(_strap, beat.Position);
                    }

                    break;
            }
        }

        private void OnDockBeat(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _director.PlayAt(_wallsRise, beat.Position);
                    break;
                case FeelPhase.Impact:
                    _director.PlayAt(_clamp, beat.Position);
                    break;
                case FeelPhase.Reward:
                    _director.PlayAt(_wallsSettle, beat.Position);
                    break;
                case FeelPhase.Cancel:
                    _director.PlayAt(_wallsDrop, beat.Position);
                    break;
            }
        }

        private void OnTechNodeChanged(TechNodeChanged changed)
        {
            if (!changed.Restored && changed.State != TechNodeState.Unknown)
            {
                _director.PlayVariant2D(_nodeLight, (int)changed.Node % _nodeVariants);
            }
        }

        private void OnDecodeHandOff(DecodeHandOff handOff)
        {
            _director.Play2D(_cableTicks);
        }

        private void OnComponentFound(ComponentFound found)
        {
            if (!found.Restored)
            {
                _director.PlayAt(_found, found.Position);
            }
        }

        private void OnBoostChanged(RoverBoostChanged changed)
        {
            if (_boosting && !changed.Boosting)
            {
                _director.PlayAt(_boostRelease, _rover.Position);
            }

            _boosting = changed.Boosting;
        }

        private void OnRelicStowed(RelicStowed stowed)
        {
            _director.PlayAt(_strap, stowed.Position);
        }

        /// <summary>Only a new step's line turns the page, never a re-show of the same one.</summary>
        private void OnNextStepChanged(NextStepChanged changed)
        {
            string key = changed.LineKey ?? string.Empty;
            if (key.Length > 0 && !string.Equals(key, _lastStep, StringComparison.Ordinal))
            {
                _director.Play2D(_pageTick);
            }

            _lastStep = key;
        }

        /// <summary>One soft tick per burst of saves.</summary>
        private void OnGameSaved(GameSaved saved)
        {
            if (_sinceSave >= _tuning.SaveTickInterval)
            {
                _director.Play2D(_saveTick);
            }

            _sinceSave = 0f;
        }

        /// <summary>
        /// One rising tick per unit; past the top note it stays there, and ticks closer than the tuning's interval are
        /// skipped, except the roll's last, so a big haul rolls on softly.
        /// </summary>
        private void OnStockCountTicked(StockCountTicked ticked)
        {
            if (ticked.Remaining > 0 && _sinceCount < _tuning.CountTickInterval)
            {
                return;
            }

            _sinceCount = 0f;
            _director.PlayVariant2D(_countTick, Mathf.Clamp(ticked.Index, 0, _countNotes - 1));
        }

        /// <summary>A warm click per step of home's windows waking, a pentatonic step higher each time.</summary>
        private void OnHomeWindowLit(HomeWindowLit lit)
        {
            int step = Mathf.Clamp(lit.Step - 1, 0, PentatonicSemitones.Length - 1);
            _director.Play2D(_windowClick, 1f, Mathf.Pow(2f, PentatonicSemitones[step] / SemitonesPerOctave));
        }

        private void OnDockEveningOpened(DockEveningOpened opened)
        {
            _director.Play2D(_eveningSwell);
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
