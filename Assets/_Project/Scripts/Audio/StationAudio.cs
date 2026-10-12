using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The base's machines working for 07, who has no hands (M3-14):
    /// <list type="bullet">
    /// <item>the feeding beam hums at a station's hopper and each bundle clunks in, a little higher as the bin fills
    /// (<see cref="StationCued"/>, <see cref="BundleDrops"/>);</item>
    /// <item>the Rover Bay's old arms whir at their tips while they move and sigh as they come to rest, and the
    /// turntable rumbles while it turns 07 (both read from <see cref="IRoverBay"/>'s transforms, so they follow
    /// wherever the Rover drives them);</item>
    /// <item>a crafted piece set on 07 (<see cref="RoverKitFitted"/>) is welded at the nearest arm tip, then a
    /// resolved chime: done for 07; at Kenji's garage port (the tether ring, the tape deck) the folded arm unfolds as
    /// the install begins (<see cref="RoverKitInstalling"/>), the weld sparks on 07 itself, and the tether ring hums
    /// for the first time;</item>
    /// <item>the radio tower's hatch opens, the beam's stitching rises with the new section, the hatch shuts;</item>
    /// <item>the charging dock clunks as 07 settles on it and leaving it is a tiny release
    /// (<see cref="RoverDockChanged"/>); any charger (the dock, a lit mast's ring) hums very softly while it fills the
    /// cells and plays a gentle tone as the last one fills (<see cref="ChargingChanged"/>, <see cref="BatteryChanged"/>,
    /// docs/features/M3-20).</item>
    /// <item>power returning to home, the bay or the lab's chart plays that place's wake phrase
    /// (<see cref="BasePowerChanged"/>, never on a restore from a save), and a dormant station answers the sonar with
    /// a soft "not yet" where it stands (<see cref="StationNotYet"/>, published as the ping's ring reaches it).</item>
    /// <item>the crew's belly lift: the jammed platform creaks as it asks for the stitch, the hatch slides open on an A
    /// and shut, the ride ratchets and hums in D while it moves and clunks at each stop (<see cref="LiftCued"/>,
    /// <see cref="LiftSounds"/>);</item>
    /// <item>a found blueprint tube chimes where it was found (<see cref="BlueprintFound"/>); the lab's reader and
    /// Kenji's plotter are <see cref="LabAudio"/>'s.</item>
    /// </list>
    /// Game-time loops duck while paused. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 10;
        private const uint DropSeedSalt = 0x68E31DA4u;
        private const float CentsPerSemitone = 100f;
        private const float CentsPerOctave = 1200f;

        [Tooltip("Assets/_Project/Data/Audio/StationAudioTuning.asset.")]
        [SerializeField] private StationAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly LoopFader _feed = new LoopFader();
        private readonly LoopFader _stitch = new LoopFader();
        private readonly LoopFader _liftRide = new LoopFader();
        private readonly ServoWhir _turn = new ServoWhir();
        private CueHandle[] _beats = Array.Empty<CueHandle>();
        private int[] _wakeVariants = Array.Empty<int>();
        private CueHandle _wake;
        private CueHandle _notYet;
        private CueHandle[] _liftBeats = Array.Empty<CueHandle>();
        private CueHandle _found;
        private CueHandle _portArm;
        private CueHandle _ringWake;
        private AudioSource _liftLoop;
        private float _liftCueVolume;
        private AudioDirector _director;
        private IRoverState _rover;
        private BundleDrops _drops;
        private Transform[] _tips = Array.Empty<Transform>();
        private Vector3[] _lastTips = Array.Empty<Vector3>();
        private BayArmVoice[] _arms = Array.Empty<BayArmVoice>();
        private AudioSource[] _armSources = Array.Empty<AudioSource>();
        private Transform _turntable;
        private Vector3 _lastFacing;
        private ChargeModel _charge;
        private CueHandle _sigh;
        private CueHandle _weld;
        private CueHandle _chime;
        private CueHandle _dockConnect;
        private CueHandle _dockFull;
        private CueHandle _dockRelease;
        private AudioSource _feedHum;
        private AudioSource _turnRumble;
        private AudioSource _stitchLoop;
        private AudioSource _chargeHum;
        private Vector3 _chargerPosition;
        private bool _full;
        private float _feedCueVolume;
        private float _servoCueVolume;
        private float _turnCueVolume;
        private float _stitchCueVolume;
        private float _chargeCueVolume;
        private float _stitchTime;

        internal AudioSource FeedSource => _feedHum;

        internal AudioSource TurntableSource => _turnRumble;

        internal AudioSource StitchSource => _stitchLoop;

        internal AudioSource ChargeSource => _chargeHum;

        internal AudioSource LiftSource => _liftLoop;

        internal AudioSource ArmSource(int index) => _armSources[index];

        /// <summary>The bay's arm tips the servos follow: every arm's Tip, then the floor arm's.</summary>
        public int ArmVoiceCount => _arms.Length;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(StationAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CueHandle feed = director.Resolve(AudioCueIds.HopperFeedHum);
            CueHandle servo = director.Resolve(AudioCueIds.BayArmServo);
            CueHandle turn = director.Resolve(AudioCueIds.BayTurntable);
            CueHandle stitch = director.Resolve(AudioCueIds.FriendStitch);
            CueHandle charge = director.Resolve(AudioCueIds.DockChargeHum);
            _sigh = director.Resolve(AudioCueIds.BayArmSigh);
            _weld = director.Resolve(AudioCueIds.BayWeld);
            _chime = director.Resolve(AudioCueIds.BayFittedChime);
            _dockConnect = director.Resolve(AudioCueIds.DockConnect);
            _dockFull = director.Resolve(AudioCueIds.DockFull);
            _dockRelease = director.Resolve(AudioCueIds.DockRelease);
            _wake = director.Resolve(AudioCueIds.BaseWake);
            _notYet = director.Resolve(AudioCueIds.StationNotYet);
            CueHandle liftRide = director.Resolve(AudioCueIds.LiftRide);
            _found = director.Resolve(AudioCueIds.BlueprintFound);
            _portArm = director.Resolve(AudioCueIds.PortArm);
            _ringWake = director.Resolve(AudioCueIds.TetherRingWake);
            if (!feed.IsValid || !servo.IsValid || !turn.IsValid || !stitch.IsValid || !charge.IsValid ||
                !_sigh.IsValid || !_weld.IsValid || !_chime.IsValid || !_dockConnect.IsValid || !_dockFull.IsValid ||
                !_dockRelease.IsValid || !_wake.IsValid || !_notYet.IsValid || !liftRide.IsValid ||
                !_found.IsValid || !_portArm.IsValid || !_ringWake.IsValid || !TryResolveBeats(director) ||
                !TryResolveLiftBeats(director) ||
                !TryResolveWakes(director.Library.GetCue(_wake)))
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            IRoverBay bay = context.Get<IRoverBay>();
            _charge = new ChargeModel(_tuning);
            _drops = new BundleDrops(_tuning, new AudioRandom(unchecked((uint)Environment.TickCount) ^ DropSeedSalt));
            AudioLibrary library = director.Library;
            _feedCueVolume = library.GetCue(feed).VolumeMax;
            _servoCueVolume = library.GetCue(servo).VolumeMax;
            _turnCueVolume = library.GetCue(turn).VolumeMax;
            _stitchCueVolume = library.GetCue(stitch).VolumeMax;
            _chargeCueVolume = library.GetCue(charge).VolumeMax;
            _feedHum = director.CreateLoopSource(transform, "FeedHum", feed, 1f);
            _turnRumble = director.CreateLoopSource(transform, "Turntable", turn, 1f);
            _stitchLoop = director.CreateLoopSource(transform, "TowerStitch", stitch, 1f);
            _chargeHum = director.CreateLoopSource(transform, "ChargeHum", charge, 1f);
            _liftCueVolume = library.GetCue(liftRide).VolumeMax;
            _liftLoop = director.CreateLoopSource(transform, "LiftRide", liftRide, 1f);
            BuildArms(bay, servo);
            _turntable = bay.Turntable;
            _lastFacing = _turntable.forward;
            _turnRumble.transform.position = bay.TurntablePosition;

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<StationCued>(OnStationCued);
            _subscriptions[1] = events.Subscribe<RoverKitFitted>(OnKitFitted);
            _subscriptions[2] = events.Subscribe<RoverDockChanged>(OnDockChanged);
            _subscriptions[3] = events.Subscribe<BasePowerChanged>(OnBasePowerChanged);
            _subscriptions[4] = events.Subscribe<StationNotYet>(OnStationNotYet);
            _subscriptions[5] = events.Subscribe<LiftCued>(OnLiftCued);
            _subscriptions[6] = events.Subscribe<BlueprintFound>(OnBlueprintFound);
            _subscriptions[7] = events.Subscribe<ChargingChanged>(OnChargingChanged);
            _subscriptions[8] = events.Subscribe<BatteryChanged>(OnBatteryChanged);
            _subscriptions[9] = events.Subscribe<RoverKitInstalling>(OnKitInstalling);
        }

        internal void Wire(StationAudioTuning tuning)
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
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            StepArms(dt, sfx);

            float turn = _turn.Step(ServoWhir.AngularRate(_lastFacing, _turntable.forward, dt), dt,
                _tuning.TurnDeadRate, _tuning.TurnFullRate, _tuning.ArmAttack, _tuning.ArmRelease);
            _lastFacing = _turntable.forward;
            Drive(_turnRumble, turn * _tuning.TurnVolume * _turnCueVolume * sfx,
                Mathf.Lerp(_tuning.TurnMinPitch, _tuning.TurnMaxPitch, turn));

            _feed.Step(dt, _tuning.FeedFadeIn, _tuning.FeedFadeOut);
            Drive(_feedHum, _feed.Gain * _tuning.FeedVolume * _feedCueVolume * sfx, 1f);

            _stitchTime += dt;
            _stitch.Step(dt, _tuning.StitchFadeIn, _tuning.StitchFadeOut);
            float rise = Mathf.SmoothStep(0f, 1f, _stitchTime / _tuning.StitchRiseTime);
            Drive(_stitchLoop, _stitch.Gain * _tuning.StitchVolume * _stitchCueVolume * sfx,
                Mathf.Pow(2f, rise * _tuning.StitchRise * CentsPerSemitone / CentsPerOctave));

            _liftRide.Step(dt, _tuning.LiftRideFadeIn, _tuning.LiftRideFadeOut);
            Drive(_liftLoop, _liftRide.Gain * _tuning.LiftRideVolume * _liftCueVolume * sfx, 1f);

            _charge.Step(dt);
            Drive(_chargeHum, _charge.Hum * _tuning.ChargeVolume * _chargeCueVolume * sfx, 1f);
            if (_charge.TakeFull())
            {
                _director.PlayAt(_dockFull, _chargerPosition);
            }
        }

        private void StepArms(float dt, float sfx)
        {
            for (int i = 0; i < _arms.Length; i++)
            {
                Vector3 tip = _tips[i].position;
                float speed = dt > 0f ? Vector3.Distance(tip, _lastTips[i]) / dt : 0f;
                _lastTips[i] = tip;
                float amount = _arms[i].Step(speed, dt);
                _armSources[i].transform.position = tip;
                Drive(_armSources[i], amount * _tuning.ArmVolume * _servoCueVolume * sfx,
                    Mathf.Lerp(_tuning.ArmMinPitch, _tuning.ArmMaxPitch, amount));
                if (_arms[i].TakeSigh())
                {
                    _director.PlayAt(_sigh, tip);
                }
            }
        }

        private void OnStationCued(StationCued cued)
        {
            float volume = 1f;
            float pitch = 1f;
            switch (cued.Cue)
            {
                case StationCue.FeedStarted:
                    _feedHum.transform.position = cued.Position;
                    _drops.Restart();
                    _feed.FadeIn();
                    break;
                case StationCue.BundleDropped:
                    volume = _tuning.DropVolume;
                    pitch = _drops.NextPitch();
                    break;
                case StationCue.Fed:
                    _drops.Restart();
                    _feed.FadeOut();
                    break;
                case StationCue.StitchStarted:
                    _stitchLoop.transform.position = cued.Position;
                    _stitchTime = 0f;
                    _stitch.FadeIn();
                    break;
                case StationCue.HatchClosed:
                    _stitch.FadeOut();
                    break;
            }

            int beat = (int)cued.Cue;
            if (beat >= 0 && beat < _beats.Length && _beats[beat].IsValid)
            {
                _director.PlayAt(_beats[beat], cued.Position, volume, pitch);
            }
        }

        private void OnKitFitted(RoverKitFitted fitted)
        {
            if (fitted.Gift)
            {
                return;
            }

            bool atPort = AtGaragePort(fitted.Piece);
            _director.PlayAt(_weld, atPort ? _rover.Position : NearestTip(_rover.Position));
            _director.Play2D(_chime);
            if (fitted.Piece == RoverKitPiece.TetherRing)
            {
                _director.PlayAt(_ringWake, _rover.Position);
            }
        }

        /// <summary>Kenji's garage port begins an install: its folded arm unfolds towards 07.</summary>
        private void OnKitInstalling(RoverKitInstalling installing)
        {
            if (!installing.Gift && AtGaragePort(installing.Piece))
            {
                _director.PlayAt(_portArm, _rover.Position);
            }
        }

        /// <summary>The pieces Kenji's garage port fits (first-hour v2 section 3); the bay's arms the rest.</summary>
        private static bool AtGaragePort(RoverKitPiece piece)
        {
            return piece == RoverKitPiece.TetherRing || piece == RoverKitPiece.TapeDeck;
        }

        private void OnDockChanged(RoverDockChanged changed)
        {
            _director.PlayAt(changed.Docked ? _dockConnect : _dockRelease, changed.Position);
        }

        /// <summary>A charger started or stopped filling the cells: the hum follows it, "full" if the cells are.</summary>
        private void OnChargingChanged(ChargingChanged changed)
        {
            _chargerPosition = changed.Position;
            _chargeHum.transform.position = changed.Position;
            if (changed.Charging)
            {
                _charge.Begin();
            }
            else
            {
                _charge.End(_full);
            }
        }

        private void OnBatteryChanged(BatteryChanged changed)
        {
            _full = changed.FullCells >= changed.Cells;
        }

        private void OnBasePowerChanged(BasePowerChanged changed)
        {
            int stage = (int)changed.Stage;
            if (changed.Restored || stage < 0 || stage >= _wakeVariants.Length || _wakeVariants[stage] < 0)
            {
                return;
            }

            _director.PlayVariant2D(_wake, _wakeVariants[stage], _tuning.WakeVolume);
        }

        private void OnLiftCued(LiftCued cued)
        {
            switch (cued.Cue)
            {
                case LiftCue.RideStarted:
                    _liftLoop.transform.position = cued.Position;
                    _liftRide.FadeIn();
                    break;
                case LiftCue.RideStopped:
                    _liftLoop.transform.position = cued.Position;
                    _liftRide.FadeOut();
                    break;
            }

            int beat = (int)cued.Cue;
            if (beat >= 0 && beat < _liftBeats.Length && _liftBeats[beat].IsValid)
            {
                _director.PlayAt(_liftBeats[beat], cued.Position, _tuning.LiftBeatVolume);
            }
        }

        private void OnBlueprintFound(BlueprintFound found)
        {
            _director.PlayAt(_found, found.Position, _tuning.BlueprintFoundVolume);
        }

        private void OnStationNotYet(StationNotYet notYet)
        {
            _director.PlayAt(_notYet, notYet.Position, _tuning.NotYetVolume);
        }

        private Vector3 NearestTip(Vector3 from)
        {
            Vector3 nearest = from;
            float best = float.MaxValue;
            for (int i = 0; i < _tips.Length; i++)
            {
                float distance = (_tips[i].position - from).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = _tips[i].position;
                }
            }

            return nearest;
        }

        private void BuildArms(IRoverBay bay, CueHandle servo)
        {
            int count = bay.ArmCount + 1;
            _tips = new Transform[count];
            _lastTips = new Vector3[count];
            _arms = new BayArmVoice[count];
            _armSources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                _tips[i] = i < bay.ArmCount ? bay.GetArmJoint(i, RoverBayJoint.Tip) : bay.FloorTip;
                _lastTips[i] = _tips[i].position;
                _arms[i] = new BayArmVoice(_tuning);
                _armSources[i] = _director.CreateLoopSource(transform, $"ArmServo{i}", servo, 1f);
            }
        }

        private bool TryResolveBeats(AudioDirector director)
        {
            _beats = new CueHandle[StationSounds.TableSize()];
            foreach (StationCue cue in (StationCue[])Enum.GetValues(typeof(StationCue)))
            {
                if (!StationSounds.TryGetOneShot(cue, out string id))
                {
                    Debug.LogWarning($"{nameof(StationAudio)}: station beat {cue} has no sound in " +
                                     $"{nameof(StationSounds)} yet; it plays silently.", this);
                    continue;
                }

                if (id == null)
                {
                    continue;
                }

                _beats[(int)cue] = director.Resolve(id);
                if (!_beats[(int)cue].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryResolveLiftBeats(AudioDirector director)
        {
            int size = 0;
            foreach (LiftCue cue in (LiftCue[])Enum.GetValues(typeof(LiftCue)))
            {
                size = Math.Max(size, (int)cue + 1);
            }

            _liftBeats = new CueHandle[size];
            foreach (LiftCue cue in (LiftCue[])Enum.GetValues(typeof(LiftCue)))
            {
                if (!LiftSounds.TryGetOneShot(cue, out string id))
                {
                    Debug.LogWarning($"{nameof(StationAudio)}: lift beat {cue} has no sound in " +
                                     $"{nameof(LiftSounds)} yet; it plays silently.", this);
                    continue;
                }

                if (id == null)
                {
                    continue;
                }

                _liftBeats[(int)cue] = director.Resolve(id);
                if (!_liftBeats[(int)cue].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryResolveWakes(AudioCue wake)
        {
            int size = 0;
            foreach (BasePowerStage stage in (BasePowerStage[])Enum.GetValues(typeof(BasePowerStage)))
            {
                size = Math.Max(size, (int)stage + 1);
            }

            _wakeVariants = new int[size];
            foreach (BasePowerStage stage in (BasePowerStage[])Enum.GetValues(typeof(BasePowerStage)))
            {
                string label = BaseWakeSounds.LabelOf(stage);
                _wakeVariants[(int)stage] = label == null ? -1 : VariantOf(wake, label);
                if (label != null && _wakeVariants[(int)stage] < 0)
                {
                    Debug.LogError($"{nameof(StationAudio)}: cue '{wake.Id}' has no '{label}' variant for " +
                                   $"{stage}. Re-run tools/audio/build_sfx.py and the Audio builders.", this);
                    return false;
                }
            }

            return true;
        }

        private static int VariantOf(AudioCue cue, string label)
        {
            for (int i = 0; i < cue.ClipCount; i++)
            {
                if (cue.GetVariantLabel(i) == label)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Drive(AudioSource loop, float volume, float pitch)
        {
            loop.volume = volume;
            loop.pitch = pitch;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
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
