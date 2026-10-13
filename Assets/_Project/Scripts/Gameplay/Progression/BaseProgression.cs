using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Waking Lumen Station step by step (docs/features/M3-16, revised by M3-19 §7). Each frame it notes whether 07 has
    /// woken and gone out, greets 07 at each place the first time (<see cref="PlaceApproaches"/>; the lab once 07 is
    /// up in it), lets the sleeping tower and bay say "not yet" as 07 comes near (<see cref="DormantAnswers"/>), gives
    /// 07's first listen its one line, and keeps the soft "next" line in two tiers (<see cref="StepFlow"/>,
    /// docs/features/M3-21 §3): the teaching steps in order, then only the recovery targets' needs, Power before
    /// Link, naming only what the station has seen. Step 4 brings the first relic home, stitches the belly lift and
    /// rides it up to the keepsake shelf; step 5 wakes the bay with the tower's second level and fits a kit. The chasm
    /// comes before the bay (P2): once Tilly is home and the first memory is on the shelf, she spots Bell's light
    /// across the chasm (Bell's warm pillar rises); the first time 07 reaches the lip without Hover-Jump it holds still
    /// and looks across while the light pulses. Coming home to the dock with news opens the dock's evening
    /// (<see cref="DockEvening"/>, P6), whose listening sweep raises the next signal pillar. Saved as
    /// "gameplay.progression".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaseProgression : MonoBehaviour
    {
        private static readonly string[] Places =
        {
            ProgressionKeys.PlaceDock, ProgressionKeys.PlaceLander, ProgressionKeys.PlaceLadder,
            ProgressionKeys.PlaceLift, ProgressionKeys.PlaceTower, ProgressionKeys.PlaceLab, ProgressionKeys.PlaceBay,
            ProgressionKeys.PlaceRack,
        };

        private const int TowerSpot = 4;
        private const int StepTower = 0;
        private const int StepLiftStitch = 9;
        /// <summary>Step 12, "build tower level 2 to power the bay": the bay's place card and its not-yet wait for it.</summary>
        private const int StepBay = 12;

        [Tooltip("The wake-up's tuning (Assets/_Project/Data/Tuning/Gameplay/ProgressionTuning.asset).")]
        [SerializeField] private ProgressionTuning _tuning;

        [Tooltip("Where each place stands, in the order of the place ids (dock, lander, ladder, lift, tower, lab, " +
                 "bay, rack).")]
        [SerializeField] private Transform[] _places = Array.Empty<Transform>();

        private IRoverState _rover;
        private IRoverRig _rig;
        private SonarSystem _sonar;
        private Friend _signal;
        private Friend _leap;
        private int _leapIndex;
        private HomeBase _home;
        private UpgradeService _upgrades;
        private Vector3 _lip;
        private LiftStation _lift;
        private ISurveyChart _chart;
        private bool _bellLit;
        private bool _chasmSeen;
        private float _lookUntil = float.NegativeInfinity;
        private bool _looking;
        private PlaceApproaches _approaches;
        private DormantAnswers _answers;
        private DockEvening _evening;
        private RadioLetters _letters;
        private StepFlow _steps;
        private IDisposable _salvage;
        private IDisposable _listens;
        private IDisposable _answersHeard;
        private Func<float> _batteryShare;
        private bool _listenArmed;
        private Vector3 _dock;
        private bool _woke;
        private bool _salvaged;
        private bool _listened;
        private bool _towerSeen;
        private GaragePort _port;
        private TowerPartField _parts;
        private int _signalIndex;
        private float _sweepAt = float.PositiveInfinity;
        private IDisposable _powered;
        private bool _initialized;

        public ProgressionTuning Tuning => _tuning;

        /// <summary>The current "next" line key (empty once every step is done).</summary>
        public string NextStep => _steps != null ? _steps.Current : string.Empty;

        /// <summary>Every teaching step is done: the line speaks only of the recovery targets (M3-21 §3).</summary>
        public bool PastTeaching => _steps != null && _steps.PastTeaching;

        /// <summary>07 has looked across the chasm at Bell's light (P2): the bay's power may follow.</summary>
        public bool ChasmSeen => _chasmSeen;

        /// <summary>True while 07 holds still at the lip, looking across.</summary>
        public bool LookingAcross => _looking;

        /// <summary>The dock's evening (tests and debugging views).</summary>
        public DockEvening Evening => _evening;

        internal void Wire(ProgressionTuning tuning, Transform[] places)
        {
            _tuning = tuning;
            _places = places;
        }

        internal bool Initialize(GameplayServices services, BasePower power, HomeBase home, Workshop bay,
            SonarSystem sonar, FriendField friends, RelayField relays, UpgradeService upgrades, LiftStation lift,
            RadioTower tower, TowerParts components, Func<float> batteryShare)
        {
            _batteryShare = batteryShare ?? throw new ArgumentNullException(nameof(batteryShare));
            string problem = WiringProblem();
            WorldAnchor lip = default;
            problem = problem ?? (services.Anchors.TryGet(WorldAnchorIds.CanyonLip, out lip)
                ? null
                : $"no world anchor '{WorldAnchorIds.CanyonLip}' for {ProgressionKeys.NextChasm}.");
            Friend signal = problem == null ? FindFriend(friends, _tuning.SignalFriend) : null;
            Friend leap = problem == null ? FindFriend(friends, _tuning.LeapFriend) : null;
            problem = problem
                ?? (signal == null ? $"no friend '{_tuning.SignalFriend}' for {ProgressionKeys.NextSignal}."
                    : leap == null ? $"no friend '{_tuning.LeapFriend}' across the chasm."
                    : upgrades.Find(_tuning.LeapKit) == null
                        ? $"no kit '{_tuning.LeapKit}' for {ProgressionKeys.NextChasm}."
                        : null);
            if (problem != null)
            {
                Debug.LogError($"{nameof(BaseProgression)}: {problem}", this);
                enabled = false;
                return false;
            }

            _rover = services.Rover;
            _rig = services.Rig;
            _lift = lift != null ? lift : throw new ArgumentNullException(nameof(lift));
            _sonar = sonar;
            _signal = signal;
            _leap = leap;
            _leapIndex = IndexOf(friends, leap);
            _home = home;
            _upgrades = upgrades;
            _lip = lip.Position;
            _dock = _places[0].position;
            _approaches = new PlaceApproaches(services.Events, _tuning.PlaceLineGap);
            _steps = new StepFlow(services.Events);
            for (int i = 0; i < Places.Length; i++)
            {
                _approaches.Add(Places[i], _places[i].position, _tuning.RadiusOf(Places[i]),
                    Speaks(Places[i], leap));
            }

            _answers = new DormantAnswers(services.Events);
            Func<bool> homePowered = () => power.HasReached(BasePowerStage.Home);
            _answers.Add(ProgressionKeys.NotYetTower, _places[TowerSpot].position,
                _tuning.RadiusOf(ProgressionKeys.PlaceTower), () => !homePowered());
            // The bay's not-yet stays quiet while the next line already says what wakes it (text audit 0.5.5 §3.4).
            _answers.Add(ProgressionKeys.NotYetBay, bay.transform.position, _tuning.RadiusOf(ProgressionKeys.PlaceBay),
                () => homePowered() && !bay.Awake && _steps.CurrentIndex != StepBay);
            _salvage = services.Events.Subscribe<MaterialSalvaged>(_ => _salvaged = true);
            EventBus events = services.Events;
            _listens = events.Subscribe<SonarPinged>(OnListened);
            _answersHeard = events.Subscribe<SiteAnswered>(_ => OnSiteAnswered(events));
            _evening = new DockEvening(services.Events, _tuning.EveningSweepDelay, _tuning.EveningCompletionHold);
            _letters = new RadioLetters(services.Events, _tuning.SingleLetterMasts, _tuning.LettersAfter,
                _tuning.LetterCount);

            _signalIndex = IndexOf(friends, signal);
            _powered = events.Subscribe<BasePowerChanged>(OnPowerChanged);

            // First-hour v2 §4: one thing at a time, each line shown as soon as 07 stops.
            Func<bool> homeLit = () => power.HasReached(BasePowerStage.Home);
            Func<bool> tillyAwake = () => signal.Progress.State == FriendState.Awake;
            _steps.Add(ProgressionKeys.NextWake, () => _towerSeen || _salvaged || homeLit());
            _steps.Add(ProgressionKeys.NextSalvage, () => homeLit() || CoilOut(components));
            _steps.Add(() => TowerLine(components, tower), homeLit);
            _steps.Add(ProgressionKeys.NextSignal,
                () => tillyAwake() || signal.Progress.Collected >= signal.Progress.PartCount - 1);
            _steps.Add(ProgressionKeys.NextDig, () => tillyAwake() || signal.Progress.AllPartsGathered);
            _steps.Add(ProgressionKeys.NextFriendRepair, tillyAwake);
            _steps.Add(ProgressionKeys.NextGarage, () => _port != null && (_port.IsOpen || _port.RingFitted));
            _steps.Add(ProgressionKeys.NextPort, () => _port != null && _port.RingFitted);
            _steps.Add(ProgressionKeys.NextRelic, () => lift.Phase >= LiftPhase.Called || home.DisplayedCount > 0);
            _steps.Add(ProgressionKeys.NextLiftStitch, () => lift.Phase >= LiftPhase.Ready || home.DisplayedCount > 0);
            _steps.Add(ProgressionKeys.NextLiftRide, () => home.DisplayedCount > 0);
            _steps.Add(ProgressionKeys.NextChasm, () => _chasmSeen || upgrades.LevelOf(_tuning.LeapKit) > 0);
            _steps.Add(ProgressionKeys.NextBayWake, () => power.HasReached(BasePowerStage.Bay));
            _steps.Add(ProgressionKeys.NextBay, AnyKitFitted(upgrades));
            AddTargetNeeds(tower, components, relays);
            _initialized = true;
            return true;
        }

        /// <summary>
        /// After the teaching steps, only the recovery targets speak (M3-21 §3): Power first (the tower's next level:
        /// its component, then its materials), then Link (each seen chain mast still dark, nearest home first, else
        /// the chain as a whole). The Peak's foot is out of reach in this version, so the line ends empty.
        /// </summary>
        private void AddTargetNeeds(RadioTower tower, TowerParts components, RelayField relays)
        {
            Func<bool> always = () => true;
            Func<bool> powered = () => tower.ShownLevel >= tower.Definition.MaxLevel;
            _steps.AddNeed(ProgressionKeys.NextPowerPart,
                () => powered() || NextComponent(tower, components) != TowerPartState.Waiting, always);
            _steps.AddNeed(ProgressionKeys.NextPowerCarried,
                () => powered() || NextComponent(tower, components) == TowerPartState.Delivered, always);
            _steps.AddNeed(ProgressionKeys.NextPowerBuild, powered, always);
            for (int i = 0; i < relays.ChainMastCount; i++)
            {
                int mast = i;
                string id = relays.ChainMastId(mast);
                _steps.AddNeed(ProgressionKeys.NextLinkMastPrefix + id, () => relays.IsChainMastLit(mast),
                    () => _chart != null && _chart.HasSeen(id));
            }

            _steps.AddNeed(ProgressionKeys.NextLink, () => relays.LitChainMasts >= relays.ChainMastCount, always);
        }

        /// <summary>Where the component of the tower's next level is (Delivered when that level needs none).</summary>
        private static TowerPartState NextComponent(RadioTower tower, TowerParts components)
        {
            int level = tower.ShownLevel + 1;
            for (int i = 0; i < components.Count; i++)
            {
                if (components.PartAt(i).TowerLevel == level)
                {
                    return components.StateOf(i);
                }
            }

            return TowerPartState.Delivered;
        }

        /// <summary>True once the bay has fitted any of the tech board's kits (the bay step's verb, step 5).</summary>
        private Func<bool> AnyKitFitted(UpgradeService upgrades)
        {
            var kits = new string[_tuning.TechKits.Count];
            for (int i = 0; i < kits.Length; i++)
            {
                kits[i] = _tuning.TechKits[i].KitId;
            }

            return () =>
            {
                foreach (string kit in kits)
                {
                    if (upgrades.LevelOf(kit) > 0)
                    {
                        return true;
                    }
                }

                return false;
            };
        }

        /// <summary>The map system connects the survey chart once it is up (step 8's look waits on it).</summary>
        /// <summary>The pieces made after the wake-up: Kenji's garage port and the tower's components in the world.</summary>
        internal void ConnectFinds(GaragePort port, TowerPartField parts)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        internal void ConnectChart(ISurveyChart chart)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
        }

        internal ProgressionSaveData Capture()
        {
            return new ProgressionSaveData
            {
                woke = _woke, salvaged = _salvaged, chasmSeen = _chasmSeen, listened = _listened,
                places = _approaches.Capture(),
            };
        }

        internal void Restore(ProgressionSaveData data)
        {
            _woke = data.woke;
            _salvaged = data.salvaged;
            _chasmSeen = data.chasmSeen;
            _listened = data.listened;
            _approaches.Restore(data.places);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            Vector3 rover = _rover.Position;
            _woke |= SurfaceRules.HorizontalDistance(rover, _dock) > _tuning.WakeDistance;
            _towerSeen |= SurfaceRules.HorizontalDistance(rover, _places[TowerSpot].position) <=
                          _tuning.RadiusOf(ProgressionKeys.PlaceTower);
            if (now >= _sweepAt)
            {
                _sweepAt = float.PositiveInfinity;
                if (_signal.Progress.State == FriendState.Dormant)
                {
                    _sonar.RevealFriend(_signalIndex, _tuning.TowerSweepBrightness);
                }
            }
            _approaches.Tick(rover, now);
            _answers.Tick(rover);
            StepChasm(rover, now);
            _letters.Tick();
            if (_evening.Tick(now))
            {
                _sonar.RevealNextUnheard(_dock, _tuning.EveningPillarBrightness);
            }

            _steps.Tick();
        }

        /// <summary>
        /// Tower level 1 wakes home: its first sweep rolls out a moment later and raises the faint signal's pillar
        /// (first-hour v2 §4), never on a load.
        /// </summary>
        private void OnPowerChanged(BasePowerChanged changed)
        {
            if (!changed.Restored && changed.Stage == BasePowerStage.Home)
            {
                _sweepAt = Time.time + _tuning.TowerSweepDelay;
            }
        }

        /// <summary>
        /// The depot's last cut opened its heart: the coil's crate glows there to be cut (0.5.5 §2), or 07 already has
        /// the coil.
        /// </summary>
        private bool CoilOut(TowerParts components)
        {
            return components.StateOf(0) != TowerPartState.Waiting || (_parts != null && _parts.IsFound(0));
        }

        /// <summary>
        /// The tower step's exact sub-step (feel-pass §8.2): bring the coil while it is still out there, park on the
        /// tower's pad while 07 carries it, then no line once it is in the port: the tower's panel, its hold ring and
        /// the prompt say it (text audit 0.5.5 §3.1).
        /// </summary>
        private static string TowerLine(TowerParts components, RadioTower tower)
        {
            return !components.NeedsPart(tower.Definition.Id, 1) ? string.Empty
                : components.StateOf(0) == TowerPartState.Carried ? ProgressionKeys.NextTowerPad
                : ProgressionKeys.NextTowerPart;
        }

        /// <summary>Tilly spots Bell's light once the first memory is home; 07 looks across from the lip once.</summary>
        private void StepChasm(Vector3 rover, float now)
        {
            bool bellAway = _leap.Progress.State != FriendState.Awake;
            if (!_bellLit && bellAway && _signal.Progress.State == FriendState.Awake && _home.DisplayedCount > 0)
            {
                _bellLit = true;
                _sonar.RevealFriend(_leapIndex, _tuning.BellLightBrightness);
            }

            if (_bellLit && !_chasmSeen && bellAway && _upgrades.LevelOf(_tuning.LeapKit) == 0 &&
                SurfaceRules.HorizontalDistance(rover, _lip) <= _tuning.ChasmLookRadius)
            {
                _chasmSeen = true;
                _looking = true;
                _lookUntil = now + _tuning.ChasmLookSeconds;
                _sonar.RevealFriend(_leapIndex, _tuning.BellLightBrightness);
                _rig.SetGazeTarget(this, _leap.Site.Position, GazePriorities.Focus);
                _rig.SetHoldStill(this, true);
            }

            if (_looking && now >= _lookUntil)
            {
                EndLook();
            }
        }

        /// <summary>
        /// A listen away from home arms the first-listen line (text audit 0.5.5 §3.4: only at the first stop away
        /// from home); a listen at home disarms it.
        /// </summary>
        private void OnListened(SonarPinged pinged)
        {
            _listenArmed = !_listened &&
                           SurfaceRules.HorizontalDistance(pinged.Origin, _dock) > _tuning.ListenAwayFromHome;
        }

        /// <summary>
        /// A wreck answers that armed listen: the line says what just happened, once (saved), at the moment it shows.
        /// </summary>
        private void OnSiteAnswered(EventBus events)
        {
            if (!_listenArmed || _listened)
            {
                return;
            }

            _listened = true;
            _listenArmed = false;
            events.Publish(new TickerLine(ProgressionKeys.FirstListen));
        }

        private void EndLook()
        {
            _looking = false;
            _rig.ClearGazeTarget(this);
            _rig.SetHoldStill(this, false);
        }

        private static int IndexOf(FriendField friends, Friend friend)
        {
            for (int i = 0; i < friends.Friends.Count; i++)
            {
                if (friends.Friends[i] == friend)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Only the next thing speaks (M3-16 P4): a place greets 07 once the steps have come to it, the step it
        /// belongs to (0 wake: the dock; 2 tower: the lander and the tower; 4 relic: the ladder and the lift once it
        /// asks for the stitch, the lab the first time 07 is up in it; 5 bay: the bay; past the teaching steps: Bell's
        /// rack, once she is home).
        /// </summary>
        private Func<bool> Speaks(string placeId, Friend bell)
        {
            switch (placeId)
            {
                case ProgressionKeys.PlaceDock: return () => _batteryShare() < _tuning.DockCardBelow;
                case ProgressionKeys.PlaceLander:
                case ProgressionKeys.PlaceTower: return () => _steps.CurrentIndex >= StepTower;
                case ProgressionKeys.PlaceLab: return () => _lift.Lab.IsInside;
                case ProgressionKeys.PlaceBay: return () => _steps.CurrentIndex >= StepBay;
                case ProgressionKeys.PlaceRack:
                    return () => _steps.PastTeaching && bell.Progress.State == FriendState.Awake;
                default: return () => _steps.CurrentIndex >= StepLiftStitch;
            }
        }

        private static Friend FindFriend(FriendField friends, string id)
        {
            foreach (Friend friend in friends.Friends)
            {
                if (friend.Id == id)
                {
                    return friend;
                }
            }

            return null;
        }

        private string WiringProblem()
        {
            if (_tuning == null)
            {
                return "ProgressionTuning is not assigned.";
            }

            if (_places == null || _places.Length != Places.Length)
            {
                return $"expects {Places.Length} place spots ({string.Join(", ", Places)}).";
            }

            for (int i = 0; i < _places.Length; i++)
            {
                if (_places[i] == null)
                {
                    return $"the spot of {Places[i]} is not assigned.";
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            _salvage?.Dispose();
            _listens?.Dispose();
            _answersHeard?.Dispose();
            _powered?.Dispose();
            _evening?.Dispose();
            _letters?.Dispose();
            if (_looking)
            {
                EndLook();
            }
        }
    }
}
