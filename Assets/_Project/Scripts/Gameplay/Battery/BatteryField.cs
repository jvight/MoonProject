using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's battery in the world (docs/features/M3-20, VISION ruling 15): each frame it measures what 07 did (metres
    /// on the ground and the slope under them, a tow, the boost, beam work), finds the charger 07 stands on (the dock,
    /// or a lit relay mast's 4 m ring, once stopped) and steps <see cref="BatteryModel"/>, which other domains read
    /// as
    /// <see cref="IRoverBattery"/>. Leaps are paid as they happen. While 07 crawls, new beam work sleeps. The Low
    /// warning raises the nearest charger's pillar (lit or dormant: a dormant mast's pillar is the "masts charge"
    /// promise) and, the first few times, a ticker line naming home or a lit charger only. Once Tilly is home her
    /// repaired solar cell trickles a still 07 back up, and a few seconds into a crawl she flies out to lead toward
    /// the nearest lit charger until 07 charges. A finished charge keeps the day (saves). Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatteryField : MonoBehaviour, IChargerReach
    {
        private const string LowDockLine = "battery.low.dock";
        private const string LowMastLine = "battery.low.mast";
        private const string NodeNamePrefix = "hop.node.";

        /// <summary>No charger found (the dock is always one, so only before the first frame).</summary>
        private const int NoCharger = -1;

        /// <summary>The dock in the charger index: masts use their reach node (1..).</summary>
        private const int Dock = StationReach.Home;

        [Tooltip("What draws charge, the crawl, the chargers and the warnings " +
                 "(Assets/_Project/Data/Tuning/Gameplay/BatteryTuning.asset).")]
        [SerializeField] private BatteryTuning _tuning;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private EventBus _events;
        private IRoverState _rover;
        private IRoverAbilities _abilities;
        private ISaveService _save;
        private BeamWork _beam;
        private StationReach _reach;
        private TetherSystem _tether;
        private ChargingDock _dock;
        private BellTuning _pillarLook;
        private SignalPillar _pillar;
        private Friend _leader;
        private DroneBody _leaderBody;
        private Vector3 _nearestLit;
        private int _nearestLitNode = Dock;
        private string[] _nodeNames = Array.Empty<string>();
        private Vector3 _lastPosition;
        private float _pillarUntil;
        private bool _docked;
        private bool _boosting;
        private bool _placed;
        private bool _wasAtMast;
        private bool _mastFull;
        private int _mastCells;
        private bool _wasLow;
        private bool _initialized;

        /// <summary>07's battery: the rules and the state other domains read.</summary>
        public BatteryModel Model { get; private set; }

        public BatteryTuning Tuning => _tuning;

        /// <summary>The dock counts from the start: home always charges 07 (docs/design/first-hour-qa.md #8).</summary>
        public bool HasCharger => _initialized;

        public Vector3 NearestPosition => _nearestLit;

        public string NearestNameKey => _nodeNames[_nearestLitNode];

        public float DistanceToNearest => Mathf.Sqrt(SurfaceRules.HorizontalDistanceSquared(_rover.Position, _nearestLit));

        /// <summary>
        /// The share of a full battery the way to the nearest lit charger takes, with the Low warning's margins (its
        /// route factor and range margin), so the dial's tick and the warning agree.
        /// </summary>
        public float ChargeToReach
        {
            get
            {
                float metres = DistanceToNearest * _tuning.LowRouteFactor * _tuning.LowRangeMargin;
                return Mathf.Clamp01(metres / (_tuning.MetresPerCell * Mathf.Max(1, Model.Cells)));
            }
        }

        /// <summary>The Low warning's pillar stands (or is still rising).</summary>
        public bool PillarShown => _pillar != null && _pillar.Shown;

        internal void Wire(BatteryTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, StationReach reach, TetherSystem tether, HomeBase home,
            IRoverAbilities abilities, FriendField friends, SonarTuning sonar)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (_tuning == null)
            {
                Debug.LogError($"{nameof(BatteryField)}: BatteryTuning is not assigned (run Gameplay/Tuning).", this);
                enabled = false;
                return false;
            }

            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _tether = tether != null ? tether : throw new ArgumentNullException(nameof(tether));
            _dock = home != null ? home.Dock : throw new ArgumentNullException(nameof(home));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _pillarLook = friends != null ? friends.BellTuning : throw new ArgumentNullException(nameof(friends));
            _leader = friends.Find(_tuning.LeadFriend);
            _leaderBody = _leader != null ? _leader.Body as DroneBody : null;
            if (_leaderBody == null)
            {
                Debug.LogError($"{nameof(BatteryField)}: no flying friend '{_tuning.LeadFriend}' to lead a crawl " +
                               "and give the solar cell (BatteryTuning's lead friend).", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _save = services.Save;
            _beam = services.Beam;
            Model = new BatteryModel(_tuning, _events);
            _pillar = new SignalPillar("ChargerPillar", transform, sonar, services.Terrain,
                services.Visuals.FriendPillar, services.Meshes.Pillar);
            _nodeNames = new string[_reach.NodeCount];
            for (int node = 0; node < _nodeNames.Length; node++)
            {
                _nodeNames[node] = NodeNamePrefix + _reach.Id(node);
            }

            _lastPosition = _rover.Position;
            _subscriptions.Add(_events.Subscribe<RoverDockChanged>(OnDockChanged));
            _subscriptions.Add(_events.Subscribe<RoverJumped>(OnJumped));
            _subscriptions.Add(_events.Subscribe<RoverBoostChanged>(OnBoostChanged));
            _subscriptions.Add(_events.Subscribe<RoverPlaced>(OnPlaced));
            _subscriptions.Add(_events.Subscribe<BatteryWarned>(OnWarned));
            _subscriptions.Add(_events.Subscribe<ChargingChanged>(OnChargingChanged));
            _subscriptions.Add(_events.Subscribe<PartFitted>(OnPartFitted));
            _subscriptions.Add(_events.Subscribe<PartRemoved>(OnPartRemoved));
            _initialized = true;
            return true;
        }

        internal BatterySaveData Capture()
        {
            return Model.Capture();
        }

        internal void Restore(BatterySaveData data)
        {
            Model.Restore(data);
        }

        /// <summary>The save is loaded (Start runs after it): everyone starts from the real battery.</summary>
        private void Start()
        {
            if (_initialized)
            {
                Model.Announce();
                _wasLow = Model.IsLowPower;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Model.Step(Sample(), deltaTime);
            StepFeel();
            _beam.SetAsleep(Model.IsLowPower);
            _leaderBody.Lead(Model.FriendLeads && !Model.IsCharging, _nearestLit);
            StepPillar(Time.time, deltaTime);
        }

        /// <summary>
        /// The battery's FeelBeats (feel-pass §2.7, §2.13). At a lit mast (Kind MastCharge): Anticipate as charging
        /// starts, Act per whole cell gained (strength = charge), Reward when full there, Cancel when 07 leaves it
        /// first. Low power (Kind LowPower): Impact as 07 drops onto its reserve cell, Reward when it is back above.
        /// </summary>
        private void StepFeel()
        {
            Vector3 at = _rover.Position;
            bool atMast = Model.IsCharging && Model.Charger == ChargerKind.Mast;
            int cells = Mathf.FloorToInt(Model.Charge);
            bool full = Model.Charge >= Model.Cells;
            if (atMast && !_wasAtMast)
            {
                _mastCells = cells;
                Feel(FeelKind.MastCharge, FeelPhase.Anticipate, at, Model.Charge01);
            }
            else if (!atMast && _wasAtMast && !_mastFull)
            {
                Feel(FeelKind.MastCharge, FeelPhase.Cancel, at, Model.Charge01);
            }

            if (atMast)
            {
                while (_mastCells < cells)
                {
                    _mastCells++;
                    Feel(FeelKind.MastCharge, FeelPhase.Act, at, Model.Charge01);
                }

                if (full && !_mastFull)
                {
                    Feel(FeelKind.MastCharge, FeelPhase.Reward, at, 1f);
                }
            }

            _mastFull = atMast && full;
            _wasAtMast = atMast;
            if (Model.IsLowPower != _wasLow)
            {
                Feel(FeelKind.LowPower, Model.IsLowPower ? FeelPhase.Impact : FeelPhase.Reward, at,
                    Model.IsLowPower ? 1f : Model.Charge01);
                _wasLow = Model.IsLowPower;
            }
        }

        private void Feel(FeelKind kind, FeelPhase phase, Vector3 at, float strength)
        {
            _events.Publish(new FeelBeat(kind, phase, at, Vector3.up, strength, 0f));
        }

        /// <summary>This frame's battery step: 07's travel, its verbs, the charger under it.</summary>
        private BatteryStep Sample()
        {
            Vector3 position = _rover.Position;
            Vector3 travel = position - _lastPosition;
            travel.y = 0f;
            bool counted = !_placed && _rover.IsGrounded;
            _placed = false;
            _lastPosition = position;

            int charger = ChargerUnder(position);
            _nearestLitNode = NearestCharger(position, true);
            _nearestLit = ChargerPosition(_nearestLitNode);
            bool friendHome = _leader.Progress.State == FriendState.Awake;
            return new BatteryStep
            {
                GroundDistance = counted ? travel.magnitude : 0f,
                Speed = _rover.Speed,
                SlopeDegrees = SlopeAlong(_rover.GroundNormal, travel),
                Towing = _tether.State == TetherAimState.Towing,
                Boosting = _boosting,
                BeamSeconds = _beam.Take(),
                Still = _rover.Speed <= _tuning.StillSpeed,
                SolarFitted = friendHome,
                DrumsFitted = _abilities.Has(RoverAbility.BoostCoils),
                FriendCanLead = friendHome,
                OnCharger = charger != NoCharger,
                Charger = charger == Dock ? ChargerKind.Dock : ChargerKind.Mast,
                ChargerPosition = charger != NoCharger ? ChargerPosition(charger) : Vector3.zero,
                Position = position,
                NearestLitCharger = _nearestLit,
            };
        }

        /// <summary>
        /// The charger 07 is on now: the dock while docked, else a lit mast's ring (the model waits for a stop).
        /// </summary>
        private int ChargerUnder(Vector3 position)
        {
            if (_docked)
            {
                return Dock;
            }

            float ring = _tuning.RingRadius * _tuning.RingRadius;
            for (int node = StationReach.Home + 1; node < _reach.NodeCount; node++)
            {
                if (_reach.IsLit(node) &&
                    SurfaceRules.HorizontalDistanceSquared(position, _reach.Position(node)) <= ring)
                {
                    return node;
                }
            }

            return NoCharger;
        }

        /// <summary>
        /// The nearest charger to <paramref name="position"/>: the dock or a mast, only lit masts when
        /// <paramref name="litOnly"/>.
        /// </summary>
        private int NearestCharger(Vector3 position, bool litOnly)
        {
            int nearest = Dock;
            float best = SurfaceRules.HorizontalDistanceSquared(position, _dock.Position);
            for (int node = StationReach.Home + 1; node < _reach.NodeCount; node++)
            {
                if (litOnly && !_reach.IsLit(node))
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistanceSquared(position, _reach.Position(node));
                if (distance < best)
                {
                    best = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        private Vector3 ChargerPosition(int charger)
        {
            return charger == Dock ? _dock.Position : _reach.Position(charger);
        }

        /// <summary>Slope (degrees) of the ground along <paramref name="travel"/>: + uphill, - downhill.</summary>
        private static float SlopeAlong(Vector3 normal, Vector3 travel)
        {
            float length = travel.magnitude;
            if (length <= 0f || normal.y <= 0f)
            {
                return 0f;
            }

            float rise = -(normal.x * travel.x + normal.z * travel.z) / (normal.y * length);
            return Mathf.Atan(rise) * Mathf.Rad2Deg;
        }

        private void StepPillar(float now, float deltaTime)
        {
            if (_pillar.Shown && now >= _pillarUntil)
            {
                _pillar.Hide();
            }

            _pillar.Tick(now, deltaTime, _pillarLook);
        }

        private void OnWarned(BatteryWarned warned)
        {
            if (warned.Warning != BatteryWarning.Low)
            {
                return;
            }

            Vector3 position = _rover.Position;
            _pillar.Show(ChargerPosition(NearestCharger(position, false)));
            _pillarUntil = Time.time + _tuning.PillarSeconds;
            if (!Model.TakeLowLine())
            {
                return;
            }

            int lit = NearestCharger(position, true);
            _events.Publish(lit == Dock
                ? new TickerLine(LowDockLine)
                : new TickerLine(LowMastLine, _nodeNames[lit], true));
        }

        /// <summary>A finished charge keeps the day; a charge beginning lets the pillar go.</summary>
        private void OnChargingChanged(ChargingChanged changed)
        {
            if (changed.Charging)
            {
                _pillar.Hide();
                return;
            }

            if (Model.FullCells == Model.Cells)
            {
                _save.SaveNow();
            }
        }

        /// <summary>Cell Pack II's spine is on 07: two more cells, arriving full (the install moment's rise).</summary>
        private void OnPartFitted(PartFitted fitted)
        {
            if (fitted.Part == RoverKitPiece.BatterySpine)
            {
                Model.GrowTo(_tuning.CellPackCells);
            }
        }

        /// <summary>The spine came off at the bay: back to 07's own cells until it is refitted.</summary>
        private void OnPartRemoved(PartRemoved removed)
        {
            if (removed.Part == RoverKitPiece.BatterySpine)
            {
                Model.ShrinkTo(_tuning.StartingCells);
            }
        }

        private void OnDockChanged(RoverDockChanged dock)
        {
            _docked = dock.Docked;
        }

        private void OnJumped(RoverJumped jumped)
        {
            Model.PayLeap(jumped.Strength);
        }

        private void OnBoostChanged(RoverBoostChanged boost)
        {
            _boosting = boost.Boosting;
        }

        private void OnPlaced(RoverPlaced placed)
        {
            _placed = true;
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
