using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's radio tower and its upgrade station. A ring of warm light on the ground in front of it is the shop:
    /// it breathes softly, glows inviting when the next level is affordable and brightly while 07 is parked on it.
    /// Before the first purchase the old mast stands dark; each level lights the beacon brighter, and buying one
    /// flares the beacon, grows the next stage in and rolls a warm ring out to the new clear-signal radius.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioTower : MonoBehaviour, IUpgradeStation
    {
        /// <summary>Metres the pad and bloom rings float above the dust.</summary>
        private const float RingLift = 0.06f;

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

        private IRoverState _rover;
        private ITerrainQuery _terrain;
        private UpgradeService _upgrades;
        private IDisposable _purchases;
        private EmissionGlow[] _lights = Array.Empty<EmissionGlow>();
        private Transform _beacon;
        private GlowRenderer _beaconGlow;
        private Light _beaconLight;
        private TerrainRing _padRing;
        private GlowRenderer _padGlow;
        private TerrainRing _bloomRing;
        private GlowRenderer _bloomGlow;
        private Vector3 _bloomCentre;
        private float _bloomRadius;
        private float _bloomStart = float.NegativeInfinity;
        private float _swapStart = float.NegativeInfinity;
        private int _fromStage;
        private int _toStage;
        private float _padLevel;
        private bool _initialized;

        public UpgradeDefinition Definition => _definition;

        /// <summary>True while 07 is parked on the pad.</summary>
        public bool Occupied { get; private set; }

        public Vector3 PadCentre { get; private set; }

        /// <summary>The level the tower shows (follows purchases and loads).</summary>
        public int ShownLevel { get; private set; }

        /// <summary>Current beacon brightness (tests and debugging views).</summary>
        public float BeaconLevel => _beaconGlow != null ? _beaconGlow.Intensity : 0f;

        public float PadLevel => _padLevel;

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

        internal void Wire(RadioTowerTuning tuning, UpgradeDefinition definition, Transform anchor,
            GameObject[] stages, Renderer[] stageLights, Transform[] beaconSockets)
        {
            _tuning = tuning;
            _definition = definition;
            _anchor = anchor;
            _stages = stages;
            _stageLights = stageLights;
            _beaconSockets = beaconSockets;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades)
        {
            string problem = _tuning == null ? "RadioTowerTuning is not assigned."
                : _definition == null ? "the radio tower UpgradeDefinition is not assigned."
                : _anchor == null ? "the tower anchor is not assigned."
                : _stages.Length == 0 || _stages.Length != _stageLights.Length ||
                  _stages.Length != _beaconSockets.Length ? "stages, their lights and beacon sockets must match."
                : null;
            if (problem != null)
            {
                Debug.LogError($"{nameof(RadioTower)}: {problem}", this);
                enabled = false;
                return false;
            }

            _rover = services.Rover;
            _terrain = services.Terrain;
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _lights = new EmissionGlow[_stageLights.Length];
            for (int i = 0; i < _lights.Length; i++)
            {
                _lights[i] = new EmissionGlow(_stageLights[i]);
            }

            BuildBeacon(services);
            Vector3 pad = _anchor.TransformPoint(_tuning.PadOffset);
            PadCentre = SurfaceRules.OnSurface(_terrain, pad.x, pad.z);
            _padRing = new TerrainRing(_tuning.PadSegments);
            _padRing.Rebuild(_terrain, PadCentre, _tuning.PadRadius - _tuning.PadRingWidth * 0.5f,
                _tuning.PadRadius + _tuning.PadRingWidth * 0.5f, RingLift);
            _padGlow = new GlowRenderer(GlowObject.Create("TowerPad", transform, _padRing.Mesh,
                services.Visuals.WarmRing));
            _bloomRing = new TerrainRing(_tuning.BloomSegments);
            _bloomGlow = new GlowRenderer(GlowObject.Create("SignalBloom", transform, _bloomRing.Mesh,
                services.Visuals.WarmRing));
            _bloomCentre = SurfaceRules.OnSurface(_terrain, _anchor.position.x, _anchor.position.z);
            _purchases = services.Events.Subscribe<UpgradePurchased>(OnPurchased);
            ShowLevel(upgrades.LevelOf(_definition.Id));
            _initialized = true;
            return true;
        }

        /// <summary>Shows <paramref name="level"/> at once (boot, load): no flare, no bloom.</summary>
        internal void ShowLevel(int level)
        {
            ShownLevel = level;
            int stage = TowerStageSwap.StageFor(level, _stages.Length);
            for (int i = 0; i < _stages.Length; i++)
            {
                _stages[i].SetActive(i == stage);
                _stages[i].transform.localScale = Vector3.one;
            }

            _fromStage = stage;
            _toStage = stage;
            _swapStart = float.NegativeInfinity;
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

            float now = Time.time;
            _fromStage = TowerStageSwap.StageFor(ShownLevel, _stages.Length);
            _toStage = TowerStageSwap.StageFor(purchase.Level, _stages.Length);
            ShownLevel = purchase.Level;
            _swapStart = now;
            _bloomStart = now + _tuning.FlareDuration;
            _bloomRadius = _definition.SignalRadiusAt(purchase.Level);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            Occupied = SurfaceRules.HorizontalDistance(_rover.Position, PadCentre) <= _tuning.PadRadius;
            float flare = StepSwap(now - _swapStart);
            UpdateBeacon(now, flare);
            UpdatePad(now, deltaTime);
            UpdateBloom(now);
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

        private void UpdatePad(float now, float deltaTime)
        {
            float target;
            if (ShownLevel >= _definition.MaxLevel)
            {
                target = _tuning.PadDone;
            }
            else if (Occupied)
            {
                target = _tuning.PadOccupied;
            }
            else if (_upgrades.TryGetOffer(_definition.Id, out UpgradeOffer offer) && offer.CanAfford)
            {
                target = _tuning.PadInviting * Breathing(now, _tuning.PadBreathDepth);
            }
            else
            {
                target = _tuning.PadIdle * Breathing(now, _tuning.PadBreathDepth);
            }

            _padLevel = Damp.Toward(_padLevel, target, _tuning.PadEase, deltaTime);
            _padGlow.Apply(_padLevel);
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
            _bloomRing.Rebuild(_terrain, _bloomCentre, radius - half, radius + half, RingLift);
            _bloomGlow.Apply(_tuning.BloomGlow * Mathf.Pow(1f - progress, _tuning.BloomFadeCurve));
        }

        /// <summary>1 at the top of a breath, 1 - <paramref name="depth"/> at the bottom.</summary>
        private float Breathing(float now, float depth)
        {
            return 1f - depth * (1f - MarkerEnvelope.Breath(now, _tuning.BreathPeriod));
        }

        private void OnDestroy()
        {
            _purchases?.Dispose();
            if (_padRing != null)
            {
                Object.Destroy(_padRing.Mesh);
            }

            if (_bloomRing != null)
            {
                Object.Destroy(_bloomRing.Mesh);
            }
        }
    }
}
