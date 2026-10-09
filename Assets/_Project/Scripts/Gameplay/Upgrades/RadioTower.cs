using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's radio tower and its upgrade station. A ring of warm light on the ground in front of its service port
    /// is the shop (<see cref="StationPad"/>): it breathes softly, glows inviting when the next level is affordable and
    /// brightly while 07 is parked on it.
    /// Before the first purchase the old mast stands dark; each level lights the beacon brighter. Buying one plays the
    /// port's moment (docs/features/M3-14, VISION ruling 14: 07 has no hands): 07, held still, feeds the port's hopper
    /// the recipe along its beam (<see cref="HopperFeed"/>), the service hatch swings open, and 07's beam stitches up
    /// the tower from the hatch to the beacon while the beacon flares and the next stage grows in; then the beam lets
    /// go, the hatch swings shut and a warm ring rolls out to the new clear-signal radius. A level bought during the
    /// moment waits its turn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioTower : MonoBehaviour, IUpgradeStation
    {
        [Tooltip("Tower tuning (Assets/_Project/Data/Tuning/Gameplay/RadioTowerTuning.asset).")]
        [SerializeField] private RadioTowerTuning _tuning;

        [Tooltip("The radio tower upgrade (Assets/_Project/Data/Content/Upgrades/Upgrade_radio_tower.asset).")]
        [SerializeField] private UpgradeDefinition _definition;

        [Tooltip("The lander's TowerAnchor (the stages stand on it; the pad is placed from it).")]
        [SerializeField] private Transform _anchor;

        [Tooltip("The RadioTower_L1..L3 instances, in order.")]
        [SerializeField] private GameObject[] _stages = Array.Empty<GameObject>();

        [Tooltip("Each stage's 'Lights' glow renderer, in order.")]
        [SerializeField] private Renderer[] _stageLights = Array.Empty<Renderer>();

        [Tooltip("Each stage's BeaconSocket, in order.")]
        [SerializeField] private Transform[] _beaconSockets = Array.Empty<Transform>();

        [Tooltip("Each stage's service port HopperMouth (+Z out of the mouth), in order.")]
        [SerializeField] private Transform[] _hopperMouths = Array.Empty<Transform>();

        [Tooltip("Each stage's ServiceHatch door (pivoted on its left hinge; negative yaw opens it), in order.")]
        [SerializeField] private Transform[] _hatches = Array.Empty<Transform>();

        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _rig;
        private ITerrainQuery _terrain;
        private UpgradeService _upgrades;
        private IDisposable _purchases;
        private EmissionGlow[] _lights = Array.Empty<EmissionGlow>();
        private Quaternion[] _hatchRest = Array.Empty<Quaternion>();
        private Transform _beacon;
        private GlowRenderer _beaconGlow;
        private Light _beaconLight;
        private StationPad _pad;
        private HopperFeed _feed;
        private RepairBeam _stitch;
        private PurchaseQueue _waiting;
        private TerrainRing _bloomRing;
        private GlowRenderer _bloomGlow;
        private Vector3 _bloomCentre;
        private float _bloomRadius;
        private float _bloomStart = float.NegativeInfinity;
        private float _swapStart = float.NegativeInfinity;
        private float _fedAt = float.NegativeInfinity;
        private string _crafting;
        private int _craftingLevel;
        private bool _stitchStarted;
        private bool _hatchClosed;
        private int _fromStage;
        private int _toStage;
        private bool _holding;
        private bool _initialized;

        public UpgradeDefinition Definition => _definition;

        /// <summary>True while 07 is parked on the pad.</summary>
        public bool Occupied => _pad != null && _pad.Occupied;

        public Vector3 PadCentre => _pad != null ? _pad.Centre : Vector3.zero;

        /// <summary>The level the tower shows (follows the moment's stage swap and loads).</summary>
        public int ShownLevel { get; private set; }

        /// <summary>Current beacon brightness (tests and debugging views).</summary>
        public float BeaconLevel => _beaconGlow != null ? _beaconGlow.Intensity : 0f;

        public float PadLevel => _pad != null ? _pad.Level : 0f;

        /// <summary>Where the beacon glows: the BeaconSocket of the stage currently standing.</summary>
        public Vector3 BeaconPosition => _beaconSockets[ActiveStage].position;

        /// <summary>The service port's hopper mouth on the stage currently standing.</summary>
        public Vector3 HopperMouth => _hopperMouths[ActiveStage].position;

        /// <summary>The service hatch on the stage currently standing (at its hinge).</summary>
        public Vector3 HatchPosition => _hatches[ActiveStage].position;

        /// <summary>True from a purchase until its port moment is over (the hatch shut again).</summary>
        public bool Crafting => _crafting != null;

        /// <summary>True while 07 is feeding the port's hopper.</summary>
        public bool Feeding => _feed != null && _feed.Feeding;

        /// <summary>Current feed beam brightness (tests and debugging views).</summary>
        public float FeedBeamLevel => _feed != null ? _feed.BeamLevel : 0f;

        /// <summary>Bundles flying into the hopper right now (tests and debugging views).</summary>
        public int BundlesInFlight => _feed != null ? _feed.BundlesInFlight : 0;

        /// <summary>Current stitching beam brightness (tests and debugging views).</summary>
        public float StitchLevel => _stitch != null ? _stitch.Level : 0f;

        /// <summary>Degrees the service hatch stands open now (tests and debugging views).</summary>
        public float HatchOpenAngle => _hatches.Length == 0 ? 0f
            : Quaternion.Angle(_hatchRest[ActiveStage], _hatches[ActiveStage].localRotation);

        /// <summary>The stage currently standing (tests and debugging views).</summary>
        public int ActiveStage
        {
            get
            {
                for (int i = 0; i < _stages.Length; i++)
                {
                    if (_stages[i].activeSelf)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public RadioTowerTuning Tuning => _tuning;

        public bool Sells(UpgradeDefinition definition)
        {
            return definition == _definition;
        }

        public int UpgradeCount => _definition != null ? 1 : 0;

        public UpgradeDefinition UpgradeAt(int index)
        {
            if (index != 0 || _definition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "The radio tower sells one upgrade.");
            }

            return _definition;
        }

        internal void Wire(RadioTowerTuning tuning, UpgradeDefinition definition, Transform anchor,
            GameObject[] stages, Renderer[] stageLights, Transform[] beaconSockets, Transform[] hopperMouths,
            Transform[] hatches)
        {
            _tuning = tuning;
            _definition = definition;
            _anchor = anchor;
            _stages = stages;
            _stageLights = stageLights;
            _beaconSockets = beaconSockets;
            _hopperMouths = hopperMouths;
            _hatches = hatches;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades, SalvageCatalog bundles)
        {
            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }

            string problem = WiringProblem(upgrades);
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioTower)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _upgrades = upgrades;
            _lights = new EmissionGlow[_stageLights.Length];
            _hatchRest = new Quaternion[_hatches.Length];
            for (int i = 0; i < _lights.Length; i++)
            {
                _lights[i] = new EmissionGlow(_stageLights[i]);
                _hatchRest[i] = _hatches[i].localRotation;
            }

            BuildBeacon(services);
            _pad = new StationPad("TowerPad", transform, _terrain, _anchor.TransformPoint(_tuning.PadOffset),
                _tuning.PadLook, services.Visuals.WarmRing);
            _feed = new HopperFeed("PortFeed", transform, services.Visuals.TetherBeam, bundles, _tuning.FeedLook,
                services.Events);
            _stitch = new RepairBeam("TowerStitch", transform, services.Visuals.TetherBeam, _tuning.StitchRate,
                _tuning.StitchSpread);
            _waiting = new PurchaseQueue(_definition.MaxLevel);
            _bloomRing = new TerrainRing(_tuning.BloomSegments);
            _bloomGlow = new GlowRenderer(GlowObject.Create("SignalBloom", transform, _bloomRing.Mesh,
                services.Visuals.WarmRing));
            _bloomCentre = SurfaceRules.OnSurface(_terrain, _anchor.position.x, _anchor.position.z);
            _purchases = services.Events.Subscribe<UpgradePurchased>(OnPurchased);
            ShowLevel(upgrades.LevelOf(_definition.Id));
            _initialized = true;
            return true;
        }

        private string WiringProblem(UpgradeService upgrades)
        {
            return _tuning == null ? "RadioTowerTuning is not assigned."
                : _definition == null ? "the radio tower UpgradeDefinition is not assigned."
                : _definition.Station != UpgradeStationKind.RadioTower ? $"'{_definition.Id}' is not sold at the tower."
                : upgrades == null || upgrades.Find(_definition.Id) == null
                    ? $"'{_definition.Id}' is not one of the GameplaySystem's upgrades."
                : _anchor == null ? "the tower anchor is not assigned."
                : _stages.Length == 0 || _stages.Length != _stageLights.Length ||
                  _stages.Length != _beaconSockets.Length || _stages.Length != _hopperMouths.Length ||
                  _stages.Length != _hatches.Length
                    ? "stages, their lights, beacon sockets, hopper mouths and hatches must match."
                : Array.IndexOf(_hopperMouths, null) >= 0 || Array.IndexOf(_hatches, null) >= 0
                    ? "a stage's service port (HopperMouth or ServiceHatch) is not assigned."
                : null;
        }

        /// <summary>Shows <paramref name="level"/> at once (boot, load): no moment, no flare, no bloom.</summary>
        internal void ShowLevel(int level)
        {
            ShownLevel = level;
            int stage = TowerStageSwap.StageFor(level, _stages.Length);
            for (int i = 0; i < _stages.Length; i++)
            {
                _stages[i].SetActive(i == stage);
                _stages[i].transform.localScale = Vector3.one;
                _hatches[i].localRotation = _hatchRest[i];
            }

            _fromStage = stage;
            _toStage = stage;
            _swapStart = float.NegativeInfinity;
            _bloomStart = float.NegativeInfinity;
            _fedAt = float.NegativeInfinity;
            _waiting.Clear();
            EndMoment();
        }

        private void BuildBeacon(GameplayServices services)
        {
            MeshRenderer ball = GlowObject.Create("Beacon", transform, services.Meshes.Sphere,
                services.Visuals.WarmGlow);
            _beacon = ball.transform;
            _beacon.localScale = Vector3.one * _tuning.BeaconRadius;
            _beaconGlow = new GlowRenderer(ball);
            _beaconLight = ball.gameObject.AddComponent<Light>();
            _beaconLight.type = LightType.Point;
            _beaconLight.color = _tuning.BeaconColor;
            _beaconLight.range = _tuning.BeaconLightRange;
            _beaconLight.shadows = LightShadows.None;
            _beaconLight.renderMode = LightRenderMode.ForcePixel;
            _beaconLight.intensity = 0f;
        }

        private void OnPurchased(UpgradePurchased purchase)
        {
            if (!string.Equals(purchase.UpgradeId, _definition.Id, StringComparison.Ordinal))
            {
                return;
            }

            _waiting.Enqueue(purchase.UpgradeId, purchase.Level);
            if (_crafting == null)
            {
                FeedNext(Time.time);
            }
        }

        /// <summary>07 starts feeding the port's hopper the recipe of the oldest level still waiting.</summary>
        private void FeedNext(float now)
        {
            _waiting.TryDequeue(out _crafting, out _craftingLevel);
            _feed.Begin(_definition.Levels[_craftingLevel - 1].Recipe, _crafting, now);
            _fedAt = float.NegativeInfinity;
            _stitchStarted = false;
            _hatchClosed = false;
            Vector3 mouth = HopperMouth;
            _rig.SetGazeTarget(this, mouth, GazePriorities.Focus);
            Hold(true);
            _events.Publish(new StationCued(StationCue.FeedStarted, _crafting, mouth));
        }

        /// <summary>The level's stage swap, beacon flare and signal bloom begin (07's beam starts stitching).</summary>
        private void BeginSwap(float now)
        {
            _fromStage = TowerStageSwap.StageFor(ShownLevel, _stages.Length);
            _toStage = TowerStageSwap.StageFor(_craftingLevel, _stages.Length);
            ShownLevel = _craftingLevel;
            _swapStart = now;
            _bloomStart = now + _tuning.FlareDuration;
            _bloomRadius = _definition.SignalRadiusAt(_craftingLevel);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            StepMoment(now, deltaTime);
            float flare = StepSwap(now - _swapStart);
            UpdateBeacon(now, flare);
            bool affordable = _upgrades.TryGetOffer(_definition.Id, out UpgradeOffer offer) && offer.CanAfford;
            _pad.Tick(_rover.Position, ShownLevel >= _definition.MaxLevel, affordable, now, deltaTime);
            UpdateBloom(now);
        }

        /// <summary>The port's moment: feed, hatch open, stitch while the stage swaps, hatch shut, next.</summary>
        private void StepMoment(float now, float deltaTime)
        {
            Vector3 eye = _rig.TetherOrigin.position;
            if (_feed.Step(eye, _rig.CargoSocket.position, HopperMouth, now, deltaTime))
            {
                _fedAt = now;
                _events.Publish(new StationCued(StationCue.Fed, _crafting, HopperMouth));
                _events.Publish(new StationCued(StationCue.HatchOpened, _crafting, HatchPosition));
            }

            float hatchTime = _tuning.HatchTime;
            float stitchDuration = TowerPortMoment.StitchDuration(_tuning.FlareDuration, _tuning.GrowDuration);
            float t = now - _fedAt;
            bool stitching = _crafting != null && TowerPortMoment.Stitching(t, hatchTime, stitchDuration);
            Vector3 stitchPoint = Vector3.Lerp(HatchPosition, BeaconPosition,
                TowerPortMoment.Climb(t, hatchTime, stitchDuration));
            _stitch.Step(stitching, true, eye, stitchPoint, now, deltaTime);
            if (_crafting == null || float.IsInfinity(t))
            {
                return;
            }

            SetHatches(TowerPortMoment.Openness(t, hatchTime, stitchDuration));
            if (stitching)
            {
                _rig.SetGazeTarget(this, stitchPoint, GazePriorities.Focus);
            }

            if (!_stitchStarted && t >= hatchTime)
            {
                _stitchStarted = true;
                BeginSwap(now);
                _events.Publish(new StationCued(StationCue.StitchStarted, _crafting, HatchPosition));
            }

            if (!_hatchClosed && t >= hatchTime + stitchDuration)
            {
                _hatchClosed = true;
                _rig.SetGazeTarget(this, HatchPosition, GazePriorities.Focus);
                _events.Publish(new StationCued(StationCue.HatchClosed, _crafting, HatchPosition));
            }

            if (t < TowerPortMoment.Duration(hatchTime, stitchDuration))
            {
                return;
            }

            SetHatches(0f);
            _crafting = null;
            if (_waiting.Count > 0)
            {
                FeedNext(now);
            }
            else
            {
                EndMoment();
            }
        }

        /// <summary>Every stage's hatch stands at <paramref name="openness"/>, so a swap never shuts it.</summary>
        private void SetHatches(float openness)
        {
            Quaternion open = Quaternion.Euler(0f, -_tuning.HatchAngle * openness, 0f);
            for (int i = 0; i < _hatches.Length; i++)
            {
                _hatches[i].localRotation = _hatchRest[i] * open;
            }
        }

        private void EndMoment()
        {
            _crafting = null;
            if (_holding)
            {
                _rig.ClearGazeTarget(this);
                Hold(false);
            }
        }

        private void Hold(bool hold)
        {
            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private float StepSwap(float t)
        {
            if (float.IsInfinity(t) || TowerStageSwap.Done(t, _tuning.FlareDuration, _tuning.GrowDuration))
            {
                return 0f;
            }

            if (_fromStage != _toStage)
            {
                bool swapped = TowerStageSwap.Swapped(t, _tuning.FlareDuration);
                _stages[_fromStage].SetActive(!swapped);
                _stages[_toStage].SetActive(swapped);
                if (swapped)
                {
                    float grow = TowerStageSwap.Grow(t, _tuning.FlareDuration, _tuning.GrowDuration, _tuning.GrowFrom,
                        _tuning.GrowOvershoot);
                    _stages[_toStage].transform.localScale = Vector3.one * grow;
                }
                else
                {
                    float sink = TowerStageSwap.Sink(t, _tuning.FlareDuration, _tuning.SinkTo);
                    _stages[_fromStage].transform.localScale = new Vector3(1f, sink, 1f);
                }
            }

            float flare = TowerStageSwap.Flare(t, _tuning.FlareDuration, _tuning.GrowDuration);
            if (TowerStageSwap.Done(t + Time.deltaTime, _tuning.FlareDuration, _tuning.GrowDuration))
            {
                _stages[_toStage].transform.localScale = Vector3.one;
                _stages[_fromStage].transform.localScale = Vector3.one;
            }

            return flare;
        }

        private void UpdateBeacon(float now, float flare)
        {
            int stage = ActiveStage;
            _beacon.position = _beaconSockets[stage].position;
            float breath = Breathing(now, _tuning.BeaconBreathDepth);
            float glow = _tuning.BeaconGlowAt(ShownLevel) * breath + _tuning.FlareGlow * flare;
            _beaconGlow.Apply(glow);
            float unit = Mathf.Max(0.01f, _tuning.BeaconGlowAt(1));
            _beaconLight.intensity = _tuning.BeaconLightIntensity * glow / unit;
            _beaconLight.enabled = glow > GlowRenderer.VisibleThreshold;
            float lights = ShownLevel <= 0 ? _tuning.UnpoweredGlow : _tuning.PoweredGlow;
            _lights[stage].Apply(lights + flare);
        }

        private void UpdateBloom(float now)
        {
            float t = now - _bloomStart;
            if (t < 0f || t >= _tuning.BloomDuration)
            {
                _bloomGlow.Apply(0f);
                return;
            }

            float progress = t / _tuning.BloomDuration;
            float radius = _bloomRadius * Ease.OutQuad(progress);
            float half = _tuning.BloomWidth * 0.5f;
            _bloomRing.Rebuild(_terrain, _bloomCentre, radius - half, radius + half, StationPad.RingLift);
            _bloomGlow.Apply(_tuning.BloomGlow * Mathf.Pow(1f - progress, _tuning.BloomFadeCurve));
        }

        /// <summary>1 at the top of a breath, 1 - <paramref name="depth"/> at the bottom.</summary>
        private float Breathing(float now, float depth)
        {
            return 1f - depth * (1f - MarkerEnvelope.Breath(now, _tuning.BreathPeriod));
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

            if (_bloomRing != null)
            {
                Object.Destroy(_bloomRing.Mesh);
            }
        }
    }
}
