using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's battery in the world (docs/features/M3-20, VISION ruling 15, opening-v2 §10): each frame it measures what
    /// 07 did (metres on the ground and the slope under them, the boost, beam work, scanning, welding), finds the pad
    /// 07 built that it stands on (powered by the base, or by the lander's reserve, which it draws as it fills 07) and
    /// steps <see cref="BatteryModel"/>, which other domains read as <see cref="IRoverBattery"/>. Leaps are paid as
    /// they happen. While 07 crawls, new beam work sleeps. The Low warning shows, the first few times, a ticker line.
    /// The stand-still trickle runs only while 07's solar wing works. A finished charge keeps the day (saves).
    /// Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatteryField : MonoBehaviour, IBuildEnergy
    {
        private const string LowLine = "battery.low";
        private const string LowPadLine = "battery.low.pad";

        [Tooltip("What draws charge, the crawl, the chargers and the warnings " +
                 "(Assets/_Project/Data/Tuning/Gameplay/BatteryTuning.asset).")]
        [SerializeField] private BatteryTuning _tuning;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private EventBus _events;
        private IRoverState _rover;
        private IRoverAbilities _abilities;
        private ISaveService _save;
        private BeamWork _beam;
        private OpeningField _pads;
        private IRoverSystems _systems;
        private StructureCatalog _structures;
        private bool _padFromReserve;
        private bool _chargingOnReserve;
        private bool _hasCharger;
        private Vector3 _nearestCharger;
        private Vector3 _lastPosition;
        private bool _boosting;
        private bool _placed;
        private bool _wasLow;
        private bool _initialized;

        /// <summary>07's battery: the rules and the state other domains read.</summary>
        public BatteryModel Model { get; private set; }

        public BatteryTuning Tuning => _tuning;

        /// <summary>
        /// The share of 07's battery (0..1 of its present cells) welding <paramref name="structureId"/> costs: the
        /// build menu shows it next to the materials (opening-v2 §10).
        /// </summary>
        public float WeldShare01(string structureId)
        {
            if (!_structures.TryGet(structureId, out StructureDefinition definition))
            {
                throw new ArgumentException($"No structure '{structureId}' in the catalog.", nameof(structureId));
            }

            return _tuning.WeldCells(definition.BuildSeconds) / Model.Cells;
        }

        internal void Wire(BatteryTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, IRoverAbilities abilities)
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

            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _events = services.Events;
            _rover = services.Rover;
            _save = services.Save;
            _beam = services.Beam;
            Model = new BatteryModel(_tuning, _events);
            _lastPosition = _rover.Position;
            _subscriptions.Add(_events.Subscribe<RoverJumped>(OnJumped));
            _subscriptions.Add(_events.Subscribe<RoverBoostChanged>(OnBoostChanged));
            _subscriptions.Add(_events.Subscribe<RoverPlaced>(OnPlaced));
            _subscriptions.Add(_events.Subscribe<BatteryWarned>(OnWarned));
            _subscriptions.Add(_events.Subscribe<ChargingChanged>(OnChargingChanged));
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

        /// <summary>
        /// The opening's built pads are the chargers (opening-v2 B6): fed by the base, or by the lander's reserve,
        /// which they draw as they fill 07. Connected before the first frame.
        /// </summary>
        internal void ConnectPads(OpeningField opening)
        {
            _pads = opening ?? throw new ArgumentNullException(nameof(opening));
        }

        /// <summary>The structures 07 can build: what each weld costs the battery.</summary>
        internal void ConnectStructures(StructureCatalog structures)
        {
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
        }

        /// <summary>07's systems: the solar wing's stand-still trickle only runs while the wing works.</summary>
        internal void ConnectSystems(IRoverSystems systems)
        {
            _systems = systems ?? throw new ArgumentNullException(nameof(systems));
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
            float before = Model.Charge;
            Model.Step(Sample(), deltaTime);
            bool onReserve = _padFromReserve && Model.IsCharging && Model.Charger == ChargerKind.Pad;
            if (onReserve && Model.Charge > before)
            {
                _pads.Port.Draw(Model.Charge - before);
            }

            if (_chargingOnReserve && !onReserve)
            {
                _pads.Port.GiveOut();
            }

            _chargingOnReserve = onReserve;
            _beam.SetAsleep(Model.IsLowPower);
            if (Model.IsLowPower != _wasLow)
            {
                _events.Publish(new FeelBeat(FeelKind.LowPower,
                    Model.IsLowPower ? FeelPhase.Impact : FeelPhase.Reward, _rover.Position, Vector3.up,
                    Model.IsLowPower ? 1f : Model.Charge01, 0f));
                _wasLow = Model.IsLowPower;
            }
        }

        /// <summary>This frame's battery step: 07's travel, its functions, the pad under it.</summary>
        private BatteryStep Sample()
        {
            Vector3 position = _rover.Position;
            Vector3 travel = position - _lastPosition;
            travel.y = 0f;
            bool counted = !_placed && _rover.IsGrounded;
            _placed = false;
            _lastPosition = position;

            bool onPad = _pads.Pads.TryFindUnder(position, out Vector3 padPosition, out _padFromReserve);
            _hasCharger = _pads.Pads.TryFindNearest(position, out _nearestCharger);
            return new BatteryStep
            {
                GroundDistance = counted ? travel.magnitude : 0f,
                Speed = _rover.Speed,
                SlopeDegrees = SlopeAlong(_rover.GroundNormal, travel),
                Boosting = _boosting,
                BeamSeconds = _beam.Take(out float scanSeconds, out float weldSeconds),
                ScanSeconds = scanSeconds,
                WeldSeconds = weldSeconds,
                Still = _rover.Speed <= _tuning.StillSpeed,
                SolarFitted = _systems.StateOf(RoverSystem.SolarWing) == RoverSystemState.Ok,
                DrumsFitted = _abilities.Has(RoverAbility.BoostCoils),
                OnCharger = onPad,
                Charger = ChargerKind.Pad,
                ChargerPosition = padPosition,
                Position = position,
                NearestLitCharger = _hasCharger ? _nearestCharger : position,
            };
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

        private void OnWarned(BatteryWarned warned)
        {
            if (warned.Warning == BatteryWarning.Low && Model.TakeLowLine())
            {
                _events.Publish(new TickerLine(_hasCharger ? LowPadLine : LowLine));
            }
        }

        /// <summary>A finished charge keeps the day.</summary>
        private void OnChargingChanged(ChargingChanged changed)
        {
            if (!changed.Charging && Model.FullCells == Model.Cells)
            {
                _save.SaveNow();
            }
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
