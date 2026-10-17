using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The feel beats in sound (docs/design/feel-pass.md, docs/design/opening-v2.md), at most three layers per beat
    /// and mezzo-piano peaks:
    /// <list type="bullet">
    /// <item>the cut (<see cref="FeelBeat"/> Kind Cut, M3-26, 0.5.5 §5.5): the weighted charge-up on
    /// <see cref="CutChargeStaged"/>, all through the chassis: the brace's servo chunk and wheel clack (a fresh
    /// charge), a capacitor whine gliding up onto D, A and D′ at each stage's detent click over a low hum on D, the
    /// clunk-click of the full lock with the whine held through the breath (<see cref="ChargeVoice"/>), and on a
    /// release (Cancel) the whine gliding back down; ignition's zing and thump, brighter per tier (Act, strength 0);
    /// the sizzle through 07's body following progress, with a deeper undertone per tier (later Acts); the tick-slide
    /// onto the next cut line (Follow 0.5); the crack (Impact); a strap tuk at the fold (Follow 1). The beam's singing
    /// climb and the collect melody stay <see cref="SalvageAudio"/>'s;</item>
    /// <item>charging on the pad (opening v2 B6): the DockLock walls rising (Kind Dock Anticipate), the clamp
    /// (Impact), the walls settling (Reward) and the quick drop when 07 drives off early (Cancel); while a charger fills
    /// the cells a soft hum breathes, and a full D major chord rings when the last cell fills
    /// (<see cref="ChargingChanged"/>, <see cref="BatteryChanged"/>; the per-cell notes are
    /// <see cref="BatteryAudio"/>'s);</item>
    /// <item>scanning (Kind Scan, 0.5.5 §1): the sweep tone stepping up per quarter, a glass note each quarter, the
    /// lift-off chime at the dissolve, and a warm arpeggio when a blueprint's sheet completes; containers (0.5.5 §2):
    /// the seal cut through, the container opening by kind, a crew crate's latch and note, the tok of a hold too full;
    /// the hold's first boot (0.5.5 §3): the power-on, a rising tick per slot, the starter items sliding in;</item>
    /// <item>a part picked up (Kind Collect): the lift, then the clunk on 07's back;</item>
    /// <item>small moments: the drums' boost whine and release, a new objective line's page tick, one save tick per
    /// burst, the hold's count rolling up (one rising tick per unit, rate-capped, topping out on the highest note).
    /// </item>
    /// </list>
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeelAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 16;
        private const float SegmentFollow = 0.5f;
        private const float SilentVolume = 0.001f;
        private const int ScanQuarters = 4;

        /// <summary>The scan sweep's pitch at each quarter drawn, on its D5 loop: D, E, F#, A.</summary>
        private static readonly float[] ScanPitches = { 1f, 1.1225f, 1.2599f, 1.4983f };

        [Tooltip("Assets/_Project/Data/Audio/FeelAudioTuning.asset.")]
        [SerializeField] private FeelAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly EasedValue _sizzle = new EasedValue(0f);
        private readonly EasedValue _boost = new EasedValue(0f);
        private readonly EasedValue _scan = new EasedValue(0f);
        private readonly ChargeVoice _charge = new ChargeVoice();
        private ChargeModel _charger;
        private AudioDirector _director;
        private IRoverState _rover;
        private ICutter _cutter;
        private Vector3 _chargeAt;
        private Vector3 _chargerAt;
        private bool _full;
        private AudioSource _whineSource;
        private AudioSource _humSource;
        private AudioSource _undertoneSource;
        private AudioSource _sizzleSource;
        private AudioSource _boostSource;
        private AudioSource _scanSource;
        private AudioSource _chargerSource;
        private float _whineCueVolume;
        private float _humCueVolume;
        private float _undertoneCueVolume;
        private float _sizzleCueVolume;
        private float _boostCueVolume;
        private float _scanCueVolume;
        private float _chargerCueVolume;
        private float _sizzleTarget;
        private float _scanTarget;
        private bool _boosting;
        private int _segment;
        private int _segmentNotes;
        private int _slotNotes;
        private int _containerKinds;
        private int _countNotes;
        private float _sheetIn = -1f;
        private float _sinceSave = float.MaxValue;
        private float _sinceCount = float.MaxValue;
        private string _lastStep = string.Empty;
        private CueHandle _brace;
        private CueHandle _detent;
        private CueHandle _lock;
        private CueHandle _segmentSlide;
        private CueHandle _ignite;
        private CueHandle _crack;
        private CueHandle _strap;
        private CueHandle _wallsRise;
        private CueHandle _clamp;
        private CueHandle _wallsSettle;
        private CueHandle _wallsDrop;
        private CueHandle _chargeFull;
        private CueHandle _cargoTok;
        private CueHandle _crateLatch;
        private CueHandle _holdBoot;
        private CueHandle _holdSlot;
        private CueHandle _itemSlide;
        private CueHandle _sealCut;
        private CueHandle _containerOpen;
        private CueHandle _scanStep;
        private CueHandle _scanLiftoff;
        private CueHandle _sheetComplete;
        private CueHandle _boostRelease;
        private CueHandle _pageTick;
        private CueHandle _saveTick;
        private CueHandle _countTick;
        private CueHandle _pickupLift;
        private CueHandle _pickupClunk;

        /// <summary>The cut's sizzle level now (0 when the beam is off).</summary>
        public float SizzleLevel => _sizzle.Value;

        /// <summary>The pad's charging hum level now (0 when no charger fills the cells).</summary>
        public float ChargerHum => _charger != null ? _charger.Hum : 0f;

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

            _brace = director.Resolve(AudioCueIds.ChargeBrace);
            _detent = director.Resolve(AudioCueIds.ChargeDetent);
            _lock = director.Resolve(AudioCueIds.ChargeLock);
            CueHandle whine = director.Resolve(AudioCueIds.ChargeWhine);
            CueHandle hum = director.Resolve(AudioCueIds.ChargeHum);
            CueHandle undertone = director.Resolve(AudioCueIds.CutUndertone);
            CueHandle sizzle = director.Resolve(AudioCueIds.CutSizzle);
            CueHandle boost = director.Resolve(AudioCueIds.BoostWhine);
            CueHandle sweep = director.Resolve(AudioCueIds.ScanSweep);
            CueHandle charger = director.Resolve(AudioCueIds.DockChargeHum);
            _segmentSlide = director.Resolve(AudioCueIds.SegmentSlide);
            _ignite = director.Resolve(AudioCueIds.CutIgniteTier);
            _crack = director.Resolve(AudioCueIds.CutCrack);
            _strap = director.Resolve(AudioCueIds.StrapTuk);
            _wallsRise = director.Resolve(AudioCueIds.DockWallsRise);
            _clamp = director.Resolve(AudioCueIds.DockClamp);
            _wallsSettle = director.Resolve(AudioCueIds.DockWallsSettle);
            _wallsDrop = director.Resolve(AudioCueIds.DockWallsDrop);
            _chargeFull = director.Resolve(AudioCueIds.DockFull);
            _cargoTok = director.Resolve(AudioCueIds.CargoTok);
            _crateLatch = director.Resolve(AudioCueIds.CrateLatch);
            _holdBoot = director.Resolve(AudioCueIds.HoldBoot);
            _holdSlot = director.Resolve(AudioCueIds.HoldSlot);
            _itemSlide = director.Resolve(AudioCueIds.ItemSlide);
            _sealCut = director.Resolve(AudioCueIds.SealCut);
            _containerOpen = director.Resolve(AudioCueIds.ContainerOpen);
            _scanStep = director.Resolve(AudioCueIds.ScanStep);
            _scanLiftoff = director.Resolve(AudioCueIds.ScanLiftoff);
            _sheetComplete = director.Resolve(AudioCueIds.SheetComplete);
            _boostRelease = director.Resolve(AudioCueIds.BoostRelease);
            _pageTick = director.Resolve(AudioCueIds.PageTick);
            _saveTick = director.Resolve(AudioCueIds.SaveTick);
            _countTick = director.Resolve(AudioCueIds.CountTick);
            _pickupLift = director.Resolve(AudioCueIds.PickupLift);
            _pickupClunk = director.Resolve(AudioCueIds.PickupClunk);
            if (!(_brace.IsValid && _detent.IsValid && _lock.IsValid && whine.IsValid && hum.IsValid &&
                  undertone.IsValid && sizzle.IsValid && boost.IsValid && sweep.IsValid && charger.IsValid &&
                  _segmentSlide.IsValid && _ignite.IsValid && _crack.IsValid && _strap.IsValid &&
                  _wallsRise.IsValid && _clamp.IsValid && _wallsSettle.IsValid && _wallsDrop.IsValid &&
                  _chargeFull.IsValid && _cargoTok.IsValid && _crateLatch.IsValid && _holdBoot.IsValid &&
                  _holdSlot.IsValid && _itemSlide.IsValid && _sealCut.IsValid && _containerOpen.IsValid &&
                  _scanStep.IsValid && _scanLiftoff.IsValid && _sheetComplete.IsValid && _boostRelease.IsValid &&
                  _pageTick.IsValid && _saveTick.IsValid && _countTick.IsValid && _pickupLift.IsValid &&
                  _pickupClunk.IsValid))
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            _cutter = context.Get<ICutter>();
            _charger = new ChargeModel(_tuning);
            AudioLibrary library = director.Library;
            _whineSource = Loop(director, "ChargeWhine", whine, out _whineCueVolume);
            _humSource = Loop(director, "ChargeHum", hum, out _humCueVolume);
            _undertoneSource = Loop(director, "CutUndertone", undertone, out _undertoneCueVolume);
            _sizzleSource = Loop(director, "CutSizzle", sizzle, out _sizzleCueVolume);
            _boostSource = Loop(director, "BoostWhine", boost, out _boostCueVolume);
            _scanSource = Loop(director, "ScanSweep", sweep, out _scanCueVolume);
            _chargerSource = Loop(director, "ChargerHum", charger, out _chargerCueVolume);
            _segmentNotes = library.GetCue(_segmentSlide).ClipCount;
            _slotNotes = library.GetCue(_holdSlot).ClipCount;
            _containerKinds = library.GetCue(_containerOpen).ClipCount;
            _countNotes = library.GetCue(_countTick).ClipCount;

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<FeelBeat>(OnFeelBeat);
            _subscriptions[1] = events.Subscribe<CutChargeStaged>(OnCutChargeStaged);
            _subscriptions[2] = events.Subscribe<ChargingChanged>(OnChargingChanged);
            _subscriptions[3] = events.Subscribe<BatteryChanged>(OnBatteryChanged);
            _subscriptions[4] = events.Subscribe<RoverBoostChanged>(OnBoostChanged);
            _subscriptions[5] = events.Subscribe<NextStepChanged>(OnNextStepChanged);
            _subscriptions[6] = events.Subscribe<GameSaved>(OnGameSaved);
            _subscriptions[7] = events.Subscribe<StockCountTicked>(OnStockCountTicked);
            _subscriptions[8] = events.Subscribe<CargoOverflowed>(OnCargoOverflowed);
            _subscriptions[9] = events.Subscribe<CrewCrateOpened>(OnCrewCrateOpened);
            _subscriptions[10] = events.Subscribe<HoldBootStarted>(OnHoldBootStarted);
            _subscriptions[11] = events.Subscribe<HoldSlotLit>(OnHoldSlotLit);
            _subscriptions[12] = events.Subscribe<HoldItemArrived>(OnHoldItemArrived);
            _subscriptions[13] = events.Subscribe<ContainerOpened>(OnContainerOpened);
            _subscriptions[14] = events.Subscribe<BlueprintCompleted>(OnBlueprintCompleted);
            _subscriptions[15] = events.Subscribe<ContainerSealCut>(OnContainerSealCut);
        }

        private AudioSource Loop(AudioDirector director, string name, CueHandle cue, out float cueVolume)
        {
            cueVolume = director.Library.GetCue(cue).VolumeMax;
            return director.CreateLoopSource(transform, name, cue, 1f);
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
            Drive(_sizzleSource, sizzle * _sizzleCueVolume * world);
            _undertoneSource.transform.position = _sizzleSource.transform.position;
            Drive(_undertoneSource, sizzle * _tuning.UndertoneAt(_cutter.Tier) * _undertoneCueVolume * world);
            _charge.Step(dt, _tuning.WhineLevel);
            _whineSource.transform.position = _chargeAt;
            _humSource.transform.position = _chargeAt;
            _whineSource.pitch = _charge.Pitch;
            Drive(_whineSource, _charge.WhineLevel * _whineCueVolume * world);
            Drive(_humSource, _charge.HumLevel * _humCueVolume * world);
            _charger.Step(dt);
            Drive(_chargerSource, _charger.Hum * _tuning.ChargeVolume * _chargerCueVolume * world);
            if (_charger.TakeFull())
            {
                _director.PlayAt(_chargeFull, _chargerAt);
            }

            if (_sheetIn >= 0f)
            {
                _sheetIn -= dt;
                if (_sheetIn < 0f)
                {
                    _director.Play2D(_sheetComplete);
                }
            }

            Drive(_scanSource, _scan.Step(_scanTarget, dt, _tuning.ScanEase) * _tuning.ScanVolume * _scanCueVolume *
                               world);
            float boost = _boost.Step(_boosting ? 1f : 0f, dt, _tuning.BoostEase);
            _boostSource.transform.position = _rover.Position;
            Drive(_boostSource, boost * _tuning.BoostVolume * _boostCueVolume * world);
        }

        /// <summary>A loop plays only while it is heard.</summary>
        private static void Drive(AudioSource source, float volume)
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
                    OnCutBeat(beat);
                    break;
                case FeelKind.Dock:
                    OnDockBeat(beat);
                    break;
                case FeelKind.Scan:
                    OnScanBeat(beat);
                    break;
                case FeelKind.Collect:
                    OnCollectBeat(beat);
                    break;
            }
        }

        /// <summary>A part picked up with E: the lift, then the clunk on 07's back (Reward: the melody).</summary>
        private void OnCollectBeat(FeelBeat beat)
        {
            if (beat.Phase == FeelPhase.Act)
            {
                _director.PlayAt(_pickupLift, beat.Position);
            }
            else if (beat.Phase == FeelPhase.Impact)
            {
                _director.PlayAt(_pickupClunk, beat.Position);
            }
        }

        /// <summary>
        /// A charge stage (0.5.5 §5.1): stage 0 starts the charge (the brace's chunk and clack unless the emitter is
        /// warm); each lock clicks its detent and steps the whine onto its note; the full lock is the clunk-click.
        /// </summary>
        private void OnCutChargeStaged(CutChargeStaged staged)
        {
            if (staged.Stage == 0)
            {
                _charge.Begin(staged.Warm, _tuning.BraceSeconds, staged.StageSeconds, _tuning.HumAt(0));
                if (!staged.Warm)
                {
                    _director.PlayAt(_brace, _chargeAt);
                }

                return;
            }

            _charge.Lock(staged.Stage, staged.StagesNeeded, _tuning.HumAt(staged.Stage));
            _director.PlayAt(staged.IsFullLock ? _lock : _detent, _chargeAt);
        }

        private void OnCutBeat(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _chargeAt = beat.Position;
                    _segment = 0;
                    break;
                case FeelPhase.Cancel:
                    _charge.Drain(_tuning.DrainSeconds);
                    _sizzleTarget = 0f;
                    break;
                case FeelPhase.Act:
                    _sizzleSource.transform.position = beat.Position;
                    _sizzleTarget = Mathf.Lerp(_tuning.SizzleMin, _tuning.SizzleMax, beat.Strength);
                    if (beat.Strength <= 0f)
                    {
                        _charge.Fire();
                        int tiers = _director.Library.GetCue(_ignite).ClipCount;
                        _director.PlayVariantAt(_ignite, Mathf.Clamp(_cutter.Tier, 0, tiers - 1), beat.Position);
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
                    else if (Mathf.Approximately(beat.Strength, SegmentFollow))
                    {
                        _segment = Mathf.Min(_segment + 1, _segmentNotes - 1);
                        _director.PlayVariantAt(_segmentSlide, _segment, beat.Position);
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

        /// <summary>A charger started or stopped filling the cells: the hum follows it, the chord if they are full.
        /// </summary>
        private void OnChargingChanged(ChargingChanged changed)
        {
            _chargerAt = changed.Position;
            _chargerSource.transform.position = changed.Position;
            if (changed.Charging)
            {
                _charger.Begin();
            }
            else
            {
                _charger.End(_full);
            }
        }

        private void OnBatteryChanged(BatteryChanged changed)
        {
            _full = changed.FullCells >= changed.Cells;
        }

        /// <summary>
        /// Scanning (0.5.5 §1): the lens's sweep tone while it draws (Anticipate), stepping up D, E, F#, A with a
        /// glass note at each quarter (Act 0.25 .. 1); letting go fades it (Cancel, progress kept); the dissolve into
        /// the lens is the lift-off chime (Reward).
        /// </summary>
        private void OnScanBeat(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _scanSource.transform.position = beat.Position;
                    _scanSource.pitch = ScanPitches[QuarterOf(beat.Strength)];
                    _scanTarget = 1f;
                    break;
                case FeelPhase.Act:
                    int quarter = Mathf.Clamp(Mathf.RoundToInt(beat.Strength * ScanQuarters) - 1, 0, ScanQuarters - 1);
                    _scanSource.pitch = ScanPitches[quarter];
                    _director.PlayVariantAt(_scanStep, quarter, beat.Position);
                    break;
                case FeelPhase.Cancel:
                    _scanTarget = 0f;
                    break;
                case FeelPhase.Reward:
                    _scanTarget = 0f;
                    _director.PlayAt(_scanLiftoff, beat.Position);
                    break;
            }
        }

        /// <summary>The quarter a resumed scan is in (its sweep starts on that step).</summary>
        private static int QuarterOf(float progress)
        {
            return Mathf.Clamp(Mathf.FloorToInt(progress * ScanQuarters), 0, ScanQuarters - 1);
        }

        /// <summary>
        /// A blueprint's last fragment is in: the sheet completes with a soft rising D major arpeggio, a moment after
        /// the dissolve so 07's Proud chirp leads.
        /// </summary>
        private void OnBlueprintCompleted(BlueprintCompleted completed)
        {
            _sheetIn = _tuning.SheetCompleteDelay;
        }

        private void OnContainerSealCut(ContainerSealCut cut)
        {
            _director.PlayAt(_sealCut, cut.Position);
        }

        /// <summary>A container opening and its find sliding out; the hinge's note says its kind.</summary>
        private void OnContainerOpened(ContainerOpened opened)
        {
            _director.PlayVariantAt(_containerOpen, Mathf.Clamp((int)opened.Kind, 0, _containerKinds - 1),
                opened.Position);
        }

        private void OnCargoOverflowed(CargoOverflowed overflowed)
        {
            _director.PlayAt(_cargoTok, overflowed.Position);
        }

        private void OnCrewCrateOpened(CrewCrateOpened opened)
        {
            _director.PlayAt(_crateLatch, opened.Position);
        }

        /// <summary>The first Tab press (0.5.5 B): 07's hold powers on in the dark.</summary>
        private void OnHoldBootStarted(HoldBootStarted started)
        {
            _director.Play2D(_holdBoot);
        }

        /// <summary>Each slot outline lighting, left to right: a tick a pentatonic step higher.</summary>
        private void OnHoldSlotLit(HoldSlotLit lit)
        {
            _director.PlayVariant2D(_holdSlot, Mathf.Clamp(lit.Index, 0, _slotNotes - 1));
        }

        private void OnHoldItemArrived(HoldItemArrived arrived)
        {
            _director.Play2D(_itemSlide);
        }

        private void OnBoostChanged(RoverBoostChanged changed)
        {
            if (_boosting && !changed.Boosting)
            {
                _director.PlayAt(_boostRelease, _rover.Position);
            }

            _boosting = changed.Boosting;
        }

        /// <summary>Only a new objective line turns the page, never a re-show of the same one.</summary>
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
