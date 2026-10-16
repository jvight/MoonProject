using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's radio tower and its upgrade station. Art's pad on the ground in front of its service port is the shop
    /// (<see cref="StationPad"/>). It stays put away until 07 first holds materials or a component to feed (before
    /// that the stub's pilot blink is the tower's only light, M3-16 P5); then its lamps breathe softly, glow inviting
    /// when the next level is affordable and its component delivered (P1), brightly while 07 is parked on it, and it
    /// goes away once the tower is whole.
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
        /// <summary>Metres the signal bloom's ring floats above the dust.</summary>
        private const float BloomLift = 0.06f;

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

        [Tooltip("Each stage's upper light rings (Lights/Ring_1..3), stage by stage: they light bottom to top.")]
        [SerializeField] private Renderer[] _ringLights = Array.Empty<Renderer>();

        [Tooltip("Each stage's offline plaque lamp (OfflinePlaque/OfflineLamp), lit until the tower is repaired.")]
        [SerializeField] private Renderer[] _offlineLamps = Array.Empty<Renderer>();

        [Tooltip("Each stage's BeaconSocket, in order.")]
        [SerializeField] private Transform[] _beaconSockets = Array.Empty<Transform>();

        [Tooltip("Each stage's service port HopperMouth (+Z out of the mouth), in order.")]
        [SerializeField] private Transform[] _hopperMouths = Array.Empty<Transform>();

        [Tooltip("Each stage's ServiceHatch door (pivoted on its left hinge; negative yaw opens it), in order.")]
        [SerializeField] private Transform[] _hatches = Array.Empty<Transform>();

        private EventBus _events;
        private BeamWork _beamWork;
        private IRoverState _rover;
        private IRoverRig _rig;
        private ITerrainQuery _terrain;
        private UpgradeService _upgrades;
        private MaterialStock _materials;
        private TowerParts _parts;
        private TowerOnline _online;
        private IDisposable _purchases;
        private IDisposable _notYet;
        private float _answeredAt = float.NegativeInfinity;
        private EmissionGlow[] _lights = Array.Empty<EmissionGlow>();
        private EmissionGlow[] _rings = Array.Empty<EmissionGlow>();
        private EmissionGlow[] _offline = Array.Empty<EmissionGlow>();
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

        /// <summary>
        /// The level whose power has reached home: it follows <see cref="ShownLevel"/> on the online sequence's first
        /// Follow (feel-pass §8.2), so the cable, the windows and the radio answer on their beat; at once on a load.
        /// </summary>
        public int PoweredLevel { get; private set; }

        /// <summary>The coming-online sequence (tests and debugging views).</summary>
        public TowerOnline Online => _online;

        /// <summary>The shown stage's offline plaque lamp level (tests and debugging views).</summary>
        public float OfflineLampLevel => _offline.Length > 0 ? _offline[ActiveStage].Intensity : 0f;

        /// <summary>Current beacon brightness (tests and debugging views).</summary>
        public float BeaconLevel => _beaconGlow != null ? _beaconGlow.Intensity : 0f;

        public float PadLevel => _pad != null ? _pad.Level : 0f;

        /// <summary>True while Art's pad shows in front of the port (07 has had something to feed it).</summary>
        public bool PadShown => _pad != null && _pad.Shown;

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
            GameObject[] stages, Renderer[] stageLights, Renderer[] ringLights, Renderer[] offlineLamps,
            Transform[] beaconSockets, Transform[] hopperMouths, Transform[] hatches)
        {
            _ringLights = ringLights;
            _offlineLamps = offlineLamps;
            _tuning = tuning;
            _definition = definition;
            _anchor = anchor;
            _stages = stages;
            _stageLights = stageLights;
            _beaconSockets = beaconSockets;
            _hopperMouths = hopperMouths;
            _hatches = hatches;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades, SalvageCatalog bundles,
            TowerParts parts)
        {
            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }

            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            _online = new TowerOnline(services.Events, _tuning);

            string problem = WiringProblem(upgrades);
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioTower)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _beamWork = services.Beam;
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _upgrades = upgrades;
            _materials = services.Materials;
            _lights = new EmissionGlow[_stageLights.Length];
            _rings = new EmissionGlow[_ringLights.Length];
            _offline = new EmissionGlow[_offlineLamps.Length];
            _hatchRest = new Quaternion[_hatches.Length];
            for (int i = 0; i < _lights.Length; i++)
            {
                _lights[i] = new EmissionGlow(_stageLights[i]);
                _offline[i] = new EmissionGlow(_offlineLamps[i]);
                _hatchRest[i] = _hatches[i].localRotation;
            }

            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i] = new EmissionGlow(_ringLights[i]);
            }

            BuildBeacon(services);
            _pad = new StationPad("TowerPad", transform, _terrain, _anchor.TransformPoint(_tuning.PadOffset),
                _tuning.PadLook, services.Visuals);
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
            _notYet = services.Events.Subscribe<StationNotYet>(OnNotYet);
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
                : _ringLights.Length != _stages.Length * (TowerOnline.Rings - 1) ||
                  _offlineLamps.Length != _stages.Length || Array.IndexOf(_ringLights, null) >= 0 ||
                  Array.IndexOf(_offlineLamps, null) >= 0
                    ? "each stage needs its light rings (Lights/Ring_1..3) and its offline plaque lamp."
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
            PoweredLevel = level;
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
            _online.Anticipate(mouth);
        }

        /// <summary>The level's stage swap, beacon flare and signal bloom begin (07's beam starts stitching).</summary>
        private void BeginSwap(float now)
        {
            _fromStage = TowerStageSwap.StageFor(ShownLevel, _stages.Length);
            _toStage = TowerStageSwap.StageFor(_craftingLevel, _stages.Length);
            ShownLevel = _craftingLevel;
            _swapStart = now;
            _bloomRadius = _definition.SignalRadiusAt(_craftingLevel);
            _online.Begin(BeaconPosition, now);
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
            _online.Step(now, out bool powered, out bool ring);
            if (powered)
            {
                PoweredLevel = ShownLevel;
            }

            if (ring)
            {
                _bloomStart = now;
            }
            float flare = StepSwap(now - _swapStart);
            UpdateBeacon(now, flare);
            int level = _upgrades.LevelOf(_definition.Id);
            if (level > 0 || _materials.Total > 0 || _parts.FirstCarried() >= 0)
            {
                bool affordable = _upgrades.TryGetOffer(_definition.Id, out UpgradeOffer offer) && offer.CanAfford &&
                                  !_parts.NeedsPart(_definition.Id, level + 1);
                _pad.Tick(_rover.Position, ShownLevel >= _definition.MaxLevel, affordable, now, deltaTime);
            }
            else
            {
                _pad.Rest(deltaTime);
            }

            UpdateBloom(now);
        }

        /// <summary>The port's moment: feed, hatch open, stitch while the stage swaps, hatch shut, next.</summary>
        private void StepMoment(float now, float deltaTime)
        {
            Vector3 eye = _rig.CutterOrigin.position;
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
            if (_feed.Feeding || stitching)
            {
                _beamWork.Work(deltaTime);
            }

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
            // While the tower comes online its lamps climb with the Acts, ring by ring from the bottom (feel-pass
            // §8.2); offline, its plaque lamp says so.
            _lights[stage].Apply(RingGlow(0) + flare);
            for (int ring = 1; ring < TowerOnline.Rings; ring++)
            {
                _rings[stage * (TowerOnline.Rings - 1) + ring - 1].Apply(RingGlow(ring) + flare);
            }

            for (int i = 0; i < _offline.Length; i++)
            {
                _offline[i].Apply(i == stage && ShownLevel <= 0 ? _tuning.OfflineLampGlow * OfflineBlink(now) : 0f);
            }
        }

        /// <summary>
        /// The dark tower answers a listen with its plaque lamp, not words (text audit 0.5.5 §3.4): a few blinks while
        /// Audio's "not yet" tone plays.
        /// </summary>
        private void OnNotYet(StationNotYet notYet)
        {
            if (notYet.ReasonKey == ProgressionKeys.NotYetTower)
            {
                _answeredAt = Time.time;
            }
        }

        /// <summary>The plaque lamp's level while it answers: 1 at rest, dipping to 0 on each blink.</summary>
        private float OfflineBlink(float now)
        {
            float t = (now - _answeredAt) / _tuning.OfflineBlinkSeconds;
            if (t < 0f || t >= 1f)
            {
                return 1f;
            }

            return 1f - Ease.Hump(Mathf.Repeat(t * _tuning.OfflineBlinks, 1f));
        }

        /// <summary>Ring <paramref name="ring"/>'s glow: lit once power has climbed to it.</summary>
        private float RingGlow(int ring)
        {
            bool lit = ShownLevel > 0 && (!_online.Running || _online.Climb >= (ring + 1f) / TowerOnline.Rings);
            return lit ? _tuning.PoweredGlow : _tuning.UnpoweredGlow;
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
            _bloomRing.Rebuild(_terrain, _bloomCentre, radius - half, radius + half, BloomLift);
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
            _notYet?.Dispose();
            _pad?.Dispose();

            if (_bloomRing != null)
            {
                Object.Destroy(_bloomRing.Mesh);
            }
        }
    }
}
