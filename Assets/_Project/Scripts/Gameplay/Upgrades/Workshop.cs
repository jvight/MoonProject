using System;
using UnityEngine;
using UnityEngine.Rendering;
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
    /// (<see cref="RoverBayFitting"/>: the rover plays the install moment with the bay's arms). When the rover sets
    /// the piece on (<see cref="RoverKitFitted"/>), the work lamps flare and weld sparks fly from the tip of the arm
    /// that fitted it (the floor arm's for a belly piece). A purchase made while another is being fed waits its turn.
    /// While the bay works (feeding and fitting, and a moment after) a warm point light under each work lamp lights its
    /// interior and arms, easing down to a low glow when it is done, and while the floor arm rises through the
    /// turntable a warm light in the pit and one on its tip light the piece coming up under 07's belly. The lamps
    /// stay on as a warm welcome and lean brighter while 07 is parked. The bay's sign lights with the base's power
    /// (the radio tower's first level): it flickers on softly the first time, and stays lit.
    /// It is also the <see cref="IRoverBay"/> the Rover domain drives (registered by the <see cref="GameplaySystem"/>):
    /// the turntable, the gantry arms' joints and the floor arm, all from serialized references wired by the scene
    /// build, never looked up by name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Workshop : MonoBehaviour, IUpgradeStation, IRoverBay
    {
        /// <summary>Below this share of their glow the pit lights switch off instead of drawing nothing.</summary>
        private const float PitLightCutoff = 0.01f;

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

        [Tooltip("Each gantry arm's shoulder turn (Arm_n/Yaw), in order.")]
        [SerializeField] private Transform[] _armYaws = Array.Empty<Transform>();

        [Tooltip("Each gantry arm's shoulder pitch (Arm_n/Yaw/Upper), in order.")]
        [SerializeField] private Transform[] _armUppers = Array.Empty<Transform>();

        [Tooltip("Each gantry arm's elbow (Arm_n/Yaw/Upper/Lower), in order.")]
        [SerializeField] private Transform[] _armLowers = Array.Empty<Transform>();

        [Tooltip("Each gantry arm's wrist (Arm_n/Yaw/Upper/Lower/Tip), in order.")]
        [SerializeField] private Transform[] _armTips = Array.Empty<Transform>();

        [Tooltip("Each gantry arm's SparkSocket (Arm_n/Yaw/Upper/Lower/Tip/SparkSocket), in order.")]
        [SerializeField] private Transform[] _sparkSockets = Array.Empty<Transform>();

        [Tooltip("The floor arm's lift (FloorArm/FloorLift): rises along its local Y through the turntable.")]
        [SerializeField] private Transform _floorLift;

        [Tooltip("The floor arm's tip (FloorArm/FloorLift/FloorTip), where a belly piece rides up.")]
        [SerializeField] private Transform _floorTip;

        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _rig;
        private UpgradeService _upgrades;
        private IDisposable _purchases;
        private StationPad _pad;
        private EmissionGlow[] _lampGlows = Array.Empty<EmissionGlow>();
        private EmissionGlow _signGlow;
        private BenchSparks[] _sparks = Array.Empty<BenchSparks>();
        private BenchSparks _floorSparks;
        private Vector3[] _socketRest = Array.Empty<Vector3>();
        private Vector3 _floorRest;
        private Light[] _lights = Array.Empty<Light>();
        private Light _pitLight;
        private Light _tipLight;
        private float _pitLevel;
        private RadioTower _power;
        private IDisposable _fittings;
        private float _lightLevel;
        private bool _lightShadows;
        private int _fitting;
        private float _lastWeld = float.NegativeInfinity;
        private bool _powered;
        private float _poweredAt = float.NegativeInfinity;
        private HopperFeed _feed;
        private PurchaseQueue _waiting;
        private string _feeding;
        private Quaternion _turntableRest;
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

        /// <summary>True once the base has power (the radio tower's first level): the sign is lit.</summary>
        public bool Powered => _powered;

        /// <summary>
        /// True while the bay works: 07 feeding its hopper, a piece being fitted, and a moment after it is set on.
        /// </summary>
        public bool Working { get; private set; }

        /// <summary>Current intensity of the work lights (tests and debugging views).</summary>
        public float LightLevel => _lightLevel;

        /// <summary>
        /// 0..1 share of the pit light's full glow (tests and debugging views): up while the floor arm is raised.
        /// </summary>
        public float PitLevel => _pitLevel;

        /// <summary>Weld sparks in the air, all arms and the floor arm together (tests and debugging views).</summary>
        public int SparkCount
        {
            get
            {
                int count = _floorSparks != null ? _floorSparks.ParticleCount : 0;
                for (int i = 0; i < _sparks.Length; i++)
                {
                    count += _sparks[i].ParticleCount;
                }

                return count;
            }
        }

        /// <summary>Weld sparks in the air at gantry arm <paramref name="arm"/>'s tip (tests and debugging views).
        /// </summary>
        public int SparkCountOf(int arm)
        {
            return _sparks[arm].ParticleCount;
        }

        /// <summary>Weld sparks in the air at the floor arm's tip (tests and debugging views).</summary>
        public int FloorSparkCount => _floorSparks != null ? _floorSparks.ParticleCount : 0;

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

        // IRoverBay (fixed after initialisation; the rover turns the turntable and poses the arms)
        public Vector3 TurntablePosition => _turntable.position;

        public Quaternion TurntableRotation => _turntableRest;

        public Transform Turntable => _turntable;

        public Transform FloorLift => _floorLift;

        public Transform FloorTip => _floorTip;

        public int ArmCount => _sparkSockets.Length;

        public Transform GetArmJoint(int arm, RoverBayJoint joint)
        {
            switch (joint)
            {
                case RoverBayJoint.Yaw:
                    return _armYaws[arm];
                case RoverBayJoint.Upper:
                    return _armUppers[arm];
                case RoverBayJoint.Lower:
                    return _armLowers[arm];
                case RoverBayJoint.Tip:
                    return _armTips[arm];
                case RoverBayJoint.SparkSocket:
                    return _sparkSockets[arm];
                default:
                    throw new ArgumentOutOfRangeException(nameof(joint), joint, "Unknown Rover Bay joint.");
            }
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

        /// <summary>The joints the rover drives: each arm's Yaw, Upper, Lower, Tip; the floor arm.</summary>
        internal void WireArms(Transform[] yaws, Transform[] uppers, Transform[] lowers, Transform[] tips,
            Transform floorLift, Transform floorTip)
        {
            _armYaws = yaws;
            _armUppers = uppers;
            _armLowers = lowers;
            _armTips = tips;
            _floorLift = floorLift;
            _floorTip = floorTip;
        }

        /// <param name="power">The radio tower: its first level brings the base's power, which lights the sign.</param>
        internal bool Initialize(GameplayServices services, UpgradeService upgrades, SalvageCatalog bundles,
            RadioTower power)
        {
            if (upgrades == null)
            {
                throw new ArgumentNullException(nameof(upgrades));
            }

            _power = power != null ? power : throw new ArgumentNullException(nameof(power));

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
            _turntableRest = _turntable.rotation;
            _pad = StationPad.OnDeck("BayPad", transform, _turntable.position, _tuning.PadLook,
                services.Visuals.WarmRing);
            _lampGlows = new EmissionGlow[_lamps.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lampGlows[i] = new EmissionGlow(_lamps[i]);
            }

            _signGlow = new EmissionGlow(_sign);
            _lampLevel = _tuning.LampIdle;
            _powered = _power.ShownLevel > 0;
            ApplyLamps(Time.time);
            _sparks = new BenchSparks[_sparkSockets.Length];
            _socketRest = new Vector3[_sparkSockets.Length];
            for (int i = 0; i < _sparks.Length; i++)
            {
                _sparks[i] = new BenchSparks(_sparkSockets[i], _tuning, services.Visuals.Spark);
                _socketRest[i] = _sparkSockets[i].position;
            }

            _floorSparks = new BenchSparks(_floorTip, _tuning, services.Visuals.Spark);
            _floorRest = _floorTip.position;
            BuildLights();

            _feed = new HopperFeed("BayFeed", transform, services.Visuals.TetherBeam, bundles, _tuning.FeedLook);
            _waiting = new PurchaseQueue(PurchaseQueue.CapacityFor(_definitions));
            _purchases = services.Events.Subscribe<UpgradePurchased>(OnPurchased);
            _fittings = services.Events.Subscribe<RoverKitFitted>(OnKitFitted);
            _initialized = true;
            return true;
        }

        /// <summary>A warm point light under each work lamp, along its aim, at the low idle glow.</summary>
        private void BuildLights()
        {
            _lights = new Light[_lamps.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                var host = new GameObject("WorkLight");
                host.transform.SetParent(_lamps[i].transform, false);
                host.transform.localPosition = Vector3.forward * _tuning.LightOffset;
                Light light = host.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = _tuning.LightColor;
                light.range = _tuning.LightRange;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForcePixel;
                _lights[i] = light;
            }

            _lightLevel = _tuning.LightIdle;
            ApplyLights(false);
            _pitLight = PitLight("PitLight", _floorLift.parent, Vector3.up * _tuning.PitLightLift,
                _tuning.PitLightRange);
            _tipLight = PitLight("TipLight", _floorTip, Vector3.zero, _tuning.TipLightRange);
            ApplyPitLight();
        }

        /// <summary>A warm point light for the floor arm, dark until it rises.</summary>
        private Light PitLight(string name, Transform parent, Vector3 localPosition, float range)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            host.transform.localPosition = localPosition;
            Light light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = _tuning.LightColor;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            light.enabled = false;
            return light;
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

            if (!EveryArm(_armYaws) || !EveryArm(_armUppers) || !EveryArm(_armLowers) || !EveryArm(_armTips))
            {
                return "the bay's arm joints (Yaw/Upper/Lower/Tip, one per SparkSocket) are not assigned.";
            }

            if (_floorLift == null || _floorTip == null)
            {
                return "the bay's floor arm (FloorLift, FloorTip) is not assigned.";
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

        /// <summary>One joint per arm (as many as SparkSockets), none missing.</summary>
        private bool EveryArm(Transform[] joints)
        {
            return joints != null && joints.Length == _sparkSockets.Length && Array.IndexOf(joints, null) < 0;
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
            _fitting++;
            _events.Publish(new StationCued(StationCue.Fed, _feeding, _hopperMouth.position));
            _events.Publish(new RoverBayFitting(_feeding));
            _feeding = null;
        }

        /// <summary>The rover set a piece bought here on 07: the weld.</summary>
        private void OnKitFitted(RoverKitFitted fitted)
        {
            if (fitted.Gift || !Sells(fitted.UpgradeId))
            {
                return;
            }

            _fitting = Mathf.Max(0, _fitting - 1);
            _lastWeld = Time.time;
            _lampLevel = Mathf.Max(_lampLevel, _tuning.LampFlare);
            Weld();
        }

        /// <summary>
        /// Weld sparks from every arm that is out fitting (its tip well away from where it rests on the rail), or from
        /// the floor arm's tip when it is the one that rose.
        /// </summary>
        private void Weld()
        {
            float travel = _tuning.WeldArmTravel;
            for (int i = 0; i < _sparks.Length; i++)
            {
                if (Vector3.Distance(_sparkSockets[i].position, _socketRest[i]) > travel)
                {
                    _sparks[i].Burst();
                }
            }

            if (Vector3.Distance(_floorTip.position, _floorRest) > travel)
            {
                _floorSparks.Burst();
            }
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
            Working = _feed.Feeding || _waiting.Count > 0 || _fitting > 0 || now - _lastWeld < _tuning.WorkLinger;
            float light = Working ? _tuning.LightWorking : _tuning.LightIdle;
            _lightLevel = Damp.Toward(_lightLevel, light, _tuning.LightEase, deltaTime);
            ApplyLights(Working && _tuning.LightCastsShadows);
            bool rising = Vector3.Distance(_floorTip.position, _floorRest) > _tuning.PitTrigger;
            _pitLevel = Damp.Toward(_pitLevel, rising ? 1f : 0f, _tuning.LightEase, deltaTime);
            ApplyPitLight();
            float lamp = _pad.Occupied || Working ? _tuning.LampOccupied : _tuning.LampIdle;
            _lampLevel = Damp.Toward(_lampLevel, lamp, _tuning.LampEase, deltaTime);
            StepPower(now);
            ApplyLamps(now);
        }

        /// <summary>
        /// The base gets its power as the radio tower's first level shows. Bought in play, the sign flickers on;
        /// loaded with power, it is simply lit.
        /// </summary>
        private void StepPower(float now)
        {
            if (_powered || _power.ShownLevel <= 0)
            {
                return;
            }

            _powered = true;
            _poweredAt = _power.Crafting ? now : float.NegativeInfinity;
        }

        /// <summary>The pit and tip lights at the floor arm's share of their glow; off (and free) when dark.</summary>
        private void ApplyPitLight()
        {
            bool lit = _pitLevel > PitLightCutoff;
            _pitLight.enabled = lit;
            _tipLight.enabled = lit;
            _pitLight.intensity = _tuning.PitLightIntensity * _pitLevel;
            _tipLight.intensity = _tuning.TipLightIntensity * _pitLevel;
        }

        private void ApplyLights(bool shadows)
        {
            LightShadows mode = shadows ? LightShadows.Soft : LightShadows.None;
            bool changed = shadows != _lightShadows;
            _lightShadows = shadows;
            for (int i = 0; i < _lights.Length; i++)
            {
                _lights[i].intensity = _lightLevel;
                if (changed)
                {
                    _lights[i].shadows = mode;
                }
            }
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

        private void ApplyLamps(float now)
        {
            for (int i = 0; i < _lampGlows.Length; i++)
            {
                _lampGlows[i].Apply(_lampLevel);
            }

            float flicker = SignFlicker.Level(now - _poweredAt, _tuning.SignFlickerDuration, _tuning.SignFlickers,
                _tuning.SignFlickerDepth);
            _signGlow.Apply(_powered ? Mathf.Max(_tuning.SignGlow, _lampLevel) * flicker : 0f);
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
            _fittings?.Dispose();
            _pad?.Dispose();
        }
    }
}
