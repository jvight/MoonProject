using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's Rover Bay beside the lander (docs/features/M3-14, VISION ruling 14: 07 has no hands): the station that
    /// sells rover kit. 07 drives in and parks on its turntable, where a ring of light is the shop, like the radio
    /// tower's pad: it offers the first piece not yet bought, breathes softly, glows inviting when that is affordable,
    /// brightly while 07 is parked on it and dims once the bay has nothing left. A purchase flares the ring; then 07,
    /// held still and looking at the hopper by the entrance, feeds it the recipe's materials along its beam
    /// (<see cref="HopperFeed"/>), and once they are in, the bay starts fitting the piece
    /// (<see cref="RoverBayFitting"/>: the rover plays the install moment with the bay's arms): its work lamps flare
    /// and weld sparks fly from the arms' tips. A purchase made while another is being fed waits its turn. The lamps
    /// stay on as a warm welcome and lean brighter while 07 is parked; the bay's sign glows as a landmark.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Workshop : MonoBehaviour, IUpgradeStation
    {
        [Tooltip("Workshop tuning (Assets/_Project/Data/Tuning/Gameplay/WorkshopTuning.asset).")]
        [SerializeField] private WorkshopTuning _tuning;

        [Tooltip("The kit sold at the bay, in the order it offers it (Assets/_Project/Data/Content/Upgrades).")]
        [SerializeField] private UpgradeDefinition[] _definitions = Array.Empty<UpgradeDefinition>();

        [Tooltip("The RoverBay instance on the lander's WorkshopAnchor (+Z is its open front).")]
        [SerializeField] private Transform _bay;

        [Tooltip("The bay's Turntable: 07 parks on it and the pad lies on it.")]
        [SerializeField] private Transform _turntable;

        [Tooltip("The bay's HopperMouth: 07's beam feeds it (+Z out of the mouth).")]
        [SerializeField] private Transform _hopperMouth;

        [Tooltip("The bay's Lamp_0 and Lamp_1 glow renderers (the work lamps' glasses).")]
        [SerializeField] private Renderer[] _lamps = Array.Empty<Renderer>();

        [Tooltip("The bay's BaySign glow renderer.")]
        [SerializeField] private Renderer _sign;

        [Tooltip("Each gantry arm's SparkSocket (Arm_n/Upper/Lower/Tip/SparkSocket), in order.")]
        [SerializeField] private Transform[] _sparkSockets = Array.Empty<Transform>();

        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _rig;
        private UpgradeService _upgrades;
        private IDisposable _purchases;
        private StationPad _pad;
        private EmissionGlow[] _lampGlows = Array.Empty<EmissionGlow>();
        private EmissionGlow _signGlow;
        private BenchSparks[] _sparks = Array.Empty<BenchSparks>();
        private HopperFeed _feed;
        private PurchaseQueue _waiting;
        private string _feeding;
        private float _lampLevel;
        private bool _holding;
        private bool _initialized;

        /// <summary>What the bay offers now: its first piece not yet bought (its last once all are).</summary>
        public UpgradeDefinition Definition => _definitions.Length == 0 ? null
            : _definitions[_upgrades == null ? 0 : OfferIndex(_definitions, _upgrades)];

        /// <summary>True while 07 is parked on the turntable.</summary>
        public bool Occupied => _pad != null && _pad.Occupied;

        /// <summary>The turntable's centre, on its top.</summary>
        public Vector3 PadCentre => _pad != null ? _pad.Centre : Vector3.zero;

        /// <summary>Where the bay stands (its origin on the ground).</summary>
        public Vector3 BayPosition => _bay != null ? _bay.position : Vector3.zero;

        /// <summary>Which way the bay's open front faces: 07 drives in against it.</summary>
        public Vector3 BayForward => _bay != null ? _bay.forward : Vector3.forward;

        /// <summary>The hopper's mouth, where 07's beam feeds the materials in.</summary>
        public Vector3 HopperMouth => _hopperMouth != null ? _hopperMouth.position : Vector3.zero;

        /// <summary>True while 07 is feeding the hopper.</summary>
        public bool Feeding => _feed != null && _feed.Feeding;

        /// <summary>Current feed beam brightness (tests and debugging views).</summary>
        public float FeedBeamLevel => _feed != null ? _feed.BeamLevel : 0f;

        /// <summary>Bundles flying into the hopper right now (tests and debugging views).</summary>
        public int BundlesInFlight => _feed != null ? _feed.BundlesInFlight : 0;

        /// <summary>Current pad brightness (tests and debugging views).</summary>
        public float PadLevel => _pad != null ? _pad.Level : 0f;

        /// <summary>Current work lamp brightness (tests and debugging views).</summary>
        public float LampLevel => _lampLevel;

        /// <summary>Current sign brightness (tests and debugging views).</summary>
        public float SignLevel => _signGlow != null ? _signGlow.Intensity : 0f;

        /// <summary>Weld sparks in the air, all arms together (tests and debugging views).</summary>
        public int SparkCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _sparks.Length; i++)
                {
                    count += _sparks[i].ParticleCount;
                }

                return count;
            }
        }

        public WorkshopTuning Tuning => _tuning;

        public bool Sells(UpgradeDefinition definition)
        {
            return definition != null && Sells(definition.Id);
        }

        public int UpgradeCount => _definitions.Length;

        public UpgradeDefinition UpgradeAt(int index)
        {
            return _definitions[index];
        }

        internal void Wire(WorkshopTuning tuning, UpgradeDefinition[] definitions, Transform bay, Transform turntable,
            Transform hopperMouth, Renderer[] lamps, Renderer sign, Transform[] sparkSockets)
        {
            _tuning = tuning;
            _definitions = definitions;
            _bay = bay;
            _turntable = turntable;
            _hopperMouth = hopperMouth;
            _lamps = lamps;
            _sign = sign;
            _sparkSockets = sparkSockets;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades, SalvageCatalog bundles)
        {
            if (upgrades == null)
            {
                throw new ArgumentNullException(nameof(upgrades));
            }

            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }

            string problem = WiringProblem(upgrades);
            if (problem != null)
            {
                Debug.LogError($"{nameof(Workshop)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _rig = services.Rig;
            _upgrades = upgrades;
            _pad = StationPad.OnDeck("BayPad", transform, _turntable.position, _tuning.PadLook,
                services.Visuals.WarmRing);
            _lampGlows = new EmissionGlow[_lamps.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lampGlows[i] = new EmissionGlow(_lamps[i]);
            }

            _signGlow = new EmissionGlow(_sign);
            _lampLevel = _tuning.LampIdle;
            ApplyLamps();
            _sparks = new BenchSparks[_sparkSockets.Length];
            for (int i = 0; i < _sparks.Length; i++)
            {
                _sparks[i] = new BenchSparks(_sparkSockets[i], _tuning, services.Visuals.Spark);
            }

            _feed = new HopperFeed("BayFeed", transform, services.Visuals.TetherBeam, bundles, _tuning.FeedLook);
            _waiting = new PurchaseQueue(PurchaseQueue.CapacityFor(_definitions));
            _purchases = services.Events.Subscribe<UpgradePurchased>(OnPurchased);
            _initialized = true;
            return true;
        }

        private string WiringProblem(UpgradeService upgrades)
        {
            if (_tuning == null)
            {
                return "WorkshopTuning is not assigned.";
            }

            if (_bay == null || _turntable == null || _hopperMouth == null)
            {
                return "the Rover Bay, its Turntable or its HopperMouth is not assigned.";
            }

            if (_lamps == null || _lamps.Length == 0 || Array.IndexOf(_lamps, null) >= 0 || _sign == null)
            {
                return "the bay's work lamps (Lamp_n) or its sign (BaySign) are not assigned.";
            }

            if (_sparkSockets == null || _sparkSockets.Length == 0 || Array.IndexOf(_sparkSockets, null) >= 0)
            {
                return "the bay's arm SparkSockets are not assigned.";
            }

            if (_definitions == null || _definitions.Length == 0)
            {
                return "it sells nothing (no upgrade definitions).";
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] == null)
                {
                    return $"upgrade {i} is not assigned.";
                }

                if (_definitions[i].Station != UpgradeStationKind.Workshop)
                {
                    return $"'{_definitions[i].Id}' is sold at the {_definitions[i].Station}, not the workshop.";
                }

                if (upgrades.Find(_definitions[i].Id) == null)
                {
                    return $"'{_definitions[i].Id}' is not one of the GameplaySystem's upgrades.";
                }
            }

            return null;
        }

        private bool Sells(string upgradeId)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (string.Equals(_definitions[i].Id, upgradeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Index of what a station selling <paramref name="definitions"/> (in order) offers now: the first not yet
        /// fully bought, or the last once all are.
        /// </summary>
        internal static int OfferIndex(UpgradeDefinition[] definitions, UpgradeService upgrades)
        {
            int last = definitions.Length - 1;
            for (int i = 0; i < last; i++)
            {
                if (upgrades.LevelOf(definitions[i].Id) < definitions[i].MaxLevel)
                {
                    return i;
                }
            }

            return last;
        }

        private void OnPurchased(UpgradePurchased purchase)
        {
            if (!Sells(purchase.UpgradeId))
            {
                return;
            }

            _pad.Flare(_tuning.PadFlare);
            _waiting.Enqueue(purchase.UpgradeId, purchase.Level);
            if (!_feed.Feeding)
            {
                FeedNext(Time.time);
            }
        }

        /// <summary>07 starts feeding the hopper the recipe of the oldest purchase still waiting.</summary>
        private void FeedNext(float now)
        {
            _waiting.TryDequeue(out _feeding, out int level);
            Recipe recipe = _upgrades.Find(_feeding).Levels[level - 1].Recipe;
            _feed.Begin(recipe, now);
            Vector3 mouth = _hopperMouth.position;
            _rig.SetGazeTarget(this, mouth, GazePriorities.Focus);
            Hold(true);
            _events.Publish(new StationCued(StationCue.FeedStarted, _feeding, mouth));
        }

        /// <summary>The materials are in: the bay starts fitting the piece.</summary>
        private void Fit()
        {
            _events.Publish(new StationCued(StationCue.Fed, _feeding, _hopperMouth.position));
            _events.Publish(new RoverBayFitting(_feeding, _bay));
            _lampLevel = Mathf.Max(_lampLevel, _tuning.LampFlare);
            ApplyLamps();
            for (int i = 0; i < _sparks.Length; i++)
            {
                _sparks[i].Burst();
            }

            _feeding = null;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            StepFeed(now, deltaTime);
            UpgradeDefinition offer = Definition;
            _upgrades.TryGetOffer(offer.Id, out UpgradeOffer current);
            _pad.Tick(_rover.Position, current.IsMaxed, current.CanAfford, now, deltaTime);
            float lamp = _pad.Occupied ? _tuning.LampOccupied : _tuning.LampIdle;
            _lampLevel = Damp.Toward(_lampLevel, lamp, _tuning.LampEase, deltaTime);
            ApplyLamps();
        }

        private void StepFeed(float now, float deltaTime)
        {
            if (_feed.Step(_rig.TetherOrigin.position, _rig.CargoSocket.position, _hopperMouth.position, now,
                    deltaTime))
            {
                Fit();
            }

            if (_feed.Feeding)
            {
                return;
            }

            if (_waiting.Count > 0)
            {
                FeedNext(now);
            }
            else if (_holding)
            {
                _rig.ClearGazeTarget(this);
                Hold(false);
            }
        }

        private void ApplyLamps()
        {
            for (int i = 0; i < _lampGlows.Length; i++)
            {
                _lampGlows[i].Apply(_lampLevel);
            }

            _signGlow.Apply(Mathf.Max(_tuning.SignGlow, _lampLevel));
        }

        private void Hold(bool hold)
        {
            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void OnDisable()
        {
            if (_holding)
            {
                _rig.ClearGazeTarget(this);
                Hold(false);
            }
        }

        private void OnDestroy()
        {
            _purchases?.Dispose();
            _pad?.Dispose();
        }
    }
}
