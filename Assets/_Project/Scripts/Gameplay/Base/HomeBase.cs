using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home: stands the lander beside the base pad facing it and keeps it carrying across the basin (pillar 6): its
    /// lamps light the ground as 07 comes home and dim to a light left on while it is away, but the windows never dim
    /// (a little brighter far away) and a soft amber <see cref="HomeHalo"/> over the lander grows in with distance, so
    /// from far away home is a small amber cluster. It keeps the museum: a relic let go near the shelf (or one that
    /// rolls there and rests, or one set down from 07's Cargo Cradle) floats onto the nearest free slot and settles
    /// with a soft overshoot: <see cref="RelicDeposited"/> and a save. While a towed or cradled relic is in reach of
    /// the shelf, a warm glow marks the slot it will take. Displayed relics turn slowly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HomeBase : MonoBehaviour
    {
        /// <summary>
        /// Height (m) above a slot where the slot hint floats (just above where the relic's base sits).
        /// </summary>
        private const float HintLift = 0.15f;

        [Tooltip("Base tuning (Assets/_Project/Data/Tuning/Gameplay/BaseTuning.asset).")]
        [SerializeField] private BaseTuning _tuning;

        [Tooltip("Root of the lander, shelf and tower (placed beside the base pad at boot).")]
        [SerializeField] private Transform _root;

        [Tooltip("The lander's 'Windows' glow renderer.")]
        [SerializeField] private Renderer _windows;

        [Tooltip("The lander's LampSocket_0..3.")]
        [SerializeField] private Transform[] _lampSockets = Array.Empty<Transform>();

        [Tooltip("The museum shelf instance (at the lander's ShelfAnchor).")]
        [SerializeField] private Transform _shelf;

        [Tooltip("The shelf's 'Lights' glow renderer.")]
        [SerializeField] private Renderer _shelfLights;

        [Tooltip("The shelf's Slot_0..5, in order.")]
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();

        private EventBus _events;
        private IRoverState _rover;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RelicField _relics;
        private TetherSystem _tether;
        private UpgradeService _upgrades;
        private RelicTuning _relicTuning;
        private Light[] _lamps = Array.Empty<Light>();
        private EmissionGlow _windowGlow;
        private EmissionGlow _shelfGlow;
        private HomeHalo _halo;
        private Transform _hint;
        private GlowRenderer _hintGlow;
        private Vector3[] _slotPositions = Array.Empty<Vector3>();
        private bool[] _taken = Array.Empty<bool>();
        private float[] _carryTime = Array.Empty<float>();
        private Vector3[] _carryFrom = Array.Empty<Vector3>();
        private Quaternion[] _carryFromRotation = Array.Empty<Quaternion>();
        private float[] _displayAngle = Array.Empty<float>();
        private float _farness;
        private bool _initialized;

        /// <summary>Relics on the shelf (settled ones).</summary>
        public int DisplayedCount { get; private set; }

        /// <summary>0..1 share of the lamps' light right now (07's distance, eased): 1 home, a light left on far away.
        /// </summary>
        public float Warmth => _tuning.LampWarmth(_farness);

        /// <summary>Window glow right now (linear emission multiplier).</summary>
        public float WindowLevel => _windowGlow != null ? _windowGlow.Intensity : 0f;

        /// <summary>The halo over home (tests and debugging views).</summary>
        internal HomeHalo Halo => _halo;

        public Vector3 ShelfPosition => _shelf.position;

        /// <summary>Which way the shelf's front faces.</summary>
        public Vector3 ShelfForward => _shelf.forward;

        public Vector3 LanderPosition => _root.position;

        /// <summary>The slot the towed relic would take if let go now, or -1.</summary>
        public int HintedSlot { get; private set; } = -1;

        public float HintLevel => _hintGlow != null ? _hintGlow.Intensity : 0f;

        public BaseTuning Tuning => _tuning;

        internal void Wire(BaseTuning tuning, Transform root, Renderer windows, Transform[] lampSockets,
            Transform shelf, Renderer shelfLights, Transform[] slots)
        {
            _tuning = tuning;
            _root = root;
            _windows = windows;
            _lampSockets = lampSockets;
            _shelf = shelf;
            _shelfLights = shelfLights;
            _slots = slots;
        }

        /// <summary>True when <paramref name="position"/> is close enough to the shelf to deposit there.</summary>
        public bool InDepositZone(Vector3 position)
        {
            return ShelfSlots.InZone(position, _shelf.position, _tuning.DepositRadius, _tuning.DepositHeight);
        }

        internal bool Initialize(GameplayServices services, RelicField relics, TetherSystem tether,
            UpgradeService upgrades)
        {
            string problem = _tuning == null ? "BaseTuning is not assigned."
                : _root == null || _shelf == null ? "the base root or the shelf is not assigned."
                : _windows == null || _shelfLights == null ? "the lander windows or shelf lights are not assigned."
                : _slots.Length == 0 ? "the shelf has no slots."
                : null;
            if (problem != null)
            {
                Debug.LogError($"{nameof(HomeBase)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _terrain = services.Terrain;
            _save = services.Save;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _tether = tether ?? throw new ArgumentNullException(nameof(tether));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _relicTuning = relics.Tuning;
            PlaceLander(services.Layout.BasePosition);

            _windowGlow = new EmissionGlow(_windows);
            _shelfGlow = new EmissionGlow(_shelfLights);
            _halo = new HomeHalo(transform, services.Meshes.Quad, services.Visuals.HomeHalo, _tuning, _root);
            _lamps = new Light[_lampSockets.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lamps[i] = CreateLamp(_lampSockets[i]);
            }

            MeshRenderer hint = GlowObject.Create("SlotHint", transform, services.Meshes.Sphere,
                services.Visuals.WarmGlow);
            _hint = hint.transform;
            _hint.localScale = Vector3.one * _tuning.SlotHintRadius;
            _hintGlow = new GlowRenderer(hint);

            _slotPositions = new Vector3[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slotPositions[i] = _slots[i].position;
            }

            int count = relics.Relics.Count;
            _taken = new bool[_slots.Length];
            _carryTime = new float[count];
            _carryFrom = new Vector3[count];
            _carryFromRotation = new Quaternion[count];
            _displayAngle = new float[count];
            _farness = _tuning.FarnessAt(Vector3.Distance(_rover.Position, _root.position));
            _initialized = true;
            return true;
        }

        /// <summary>After a load: puts every displayed relic on its slot (a free one if its slot is taken).</summary>
        internal void SyncDisplays()
        {
            Array.Clear(_taken, 0, _taken.Length);
            DisplayedCount = 0;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (relic.State != RelicState.Displayed)
                {
                    continue;
                }

                int slot = relic.Slot >= 0 && relic.Slot < _slots.Length && !_taken[relic.Slot]
                    ? relic.Slot
                    : ShelfSlots.Nearest(relic.transform.position, _slotPositions, _taken);
                if (slot < 0)
                {
                    Debug.LogError($"{nameof(HomeBase)}: no free slot for saved relic '{relic.Definition.Id}'.",
                        this);
                    relic.Release();
                    continue;
                }

                _taken[slot] = true;
                relic.SetDisplayed(slot);
                _displayAngle[i] = 0f;
                relic.SetCarryPose(SlotPose(relic, slot), _slots[slot].rotation);
                DisplayedCount++;
            }
        }

        /// <summary>
        /// Where the lander stands (on the surface) for a base pad centred on <paramref name="basePosition"/>.
        /// </summary>
        public static Vector3 LanderSpot(ITerrainQuery terrain, Vector3 basePosition, BaseTuning tuning)
        {
            Vector3 spot = basePosition + tuning.LanderOffset;
            spot.y = terrain.SampleHeight(spot.x, spot.z);
            return spot;
        }

        private void PlaceLander(Vector3 basePosition)
        {
            Vector3 spot = LanderSpot(_terrain, basePosition, _tuning);
            Vector3 facing = basePosition - spot;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            _root.SetPositionAndRotation(spot, rotation);
        }

        private Light CreateLamp(Transform socket)
        {
            var host = new GameObject("Lamp");
            host.transform.SetParent(socket, false);
            var lamp = host.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = _tuning.LampColor;
            lamp.range = _tuning.LampRange;
            lamp.shadows = LightShadows.None;
            lamp.intensity = 0f;
            return lamp;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float now = Time.time;
            UpdateWarmth(deltaTime);
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                switch (relic.State)
                {
                    case RelicState.Loose when !relic.IsTethered && InDepositZone(relic.transform.position):
                        BeginDeposit(relic);
                        break;
                    case RelicState.Depositing:
                        StepDeposit(relic, deltaTime);
                        break;
                    case RelicState.Displayed:
                        StepDisplay(relic, now, deltaTime);
                        break;
                }
            }

            UpdateHint(deltaTime);
        }

        private void UpdateWarmth(float deltaTime)
        {
            float target = _tuning.FarnessAt(Vector3.Distance(_rover.Position, _root.position));
            _farness = Damp.Toward(_farness, target, _tuning.WarmEase, deltaTime);
            float boost = _upgrades.LightBoost;
            float warmth = _tuning.LampWarmth(_farness);
            float lamp = _tuning.LampIntensity * warmth * boost;
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lamps[i].intensity = lamp;
            }

            _windowGlow.Apply(_tuning.WindowGlowAt(_farness) * boost);
            _shelfGlow.Apply(_tuning.ShelfGlow * warmth * boost);
            _halo.Boost = boost;
        }

        /// <summary>
        /// A relic riding in the Cargo Cradle is set down here: true when it is in reach of the shelf and a slot is
        /// free (it then floats onto the shelf like any deposit).
        /// </summary>
        internal bool TakeFromCradle(Relic relic)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            return relic.State == RelicState.Cradled && InDepositZone(relic.transform.position) && BeginDeposit(relic);
        }

        /// <returns>False when every slot is taken (the relic stays where it is).</returns>
        private bool BeginDeposit(Relic relic)
        {
            int slot = ShelfSlots.Nearest(relic.transform.position, _slotPositions, _taken);
            if (slot < 0)
            {
                return false;
            }

            _taken[slot] = true;
            int index = relic.Index;
            _carryTime[index] = 0f;
            _carryFrom[index] = relic.transform.position;
            _carryFromRotation[index] = relic.transform.rotation;
            relic.BeginCarry(RelicState.Depositing, slot);
            return true;
        }

        private void StepDeposit(Relic relic, float deltaTime)
        {
            int index = relic.Index;
            _carryTime[index] += deltaTime;
            float progress = _carryTime[index] / _tuning.DepositDuration;
            int slot = relic.Slot;
            Vector3 end = SlotPose(relic, slot);
            Vector3 position = GlidePath.Position(_carryFrom[index], end, progress, _tuning.DepositLift,
                _tuning.DepositHover, _tuning.SettleOvershoot);
            Quaternion rotation = GlidePath.Rotation(_carryFromRotation[index], _slots[slot].rotation, progress);
            relic.SetCarryPose(position, rotation);
            if (progress < 1f)
            {
                return;
            }

            relic.SetDisplayed(slot);
            _displayAngle[index] = 0f;
            DisplayedCount++;
            _events.Publish(new RelicDeposited(relic.Definition.Id, end, DisplayedCount));
            _save.SaveNow();
        }

        private void StepDisplay(Relic relic, float now, float deltaTime)
        {
            int index = relic.Index;
            _displayAngle[index] += _relicTuning.DisplaySpin * deltaTime;
            Transform slot = _slots[relic.Slot];
            float bob = Mathf.Sin(now * 2f * Mathf.PI * _relicTuning.BobFrequency + index) * _relicTuning.DisplayBob;
            relic.SetCarryPose(SlotPose(relic, relic.Slot) + slot.up * bob,
                slot.rotation * Quaternion.Euler(0f, _displayAngle[index], 0f));
        }

        private void UpdateHint(float deltaTime)
        {
            Relic incoming = Incoming();
            HintedSlot = incoming != null && InDepositZone(incoming.transform.position)
                ? ShelfSlots.Nearest(incoming.transform.position, _slotPositions, _taken)
                : -1;
            if (HintedSlot >= 0)
            {
                _hint.position = _slotPositions[HintedSlot] + _slots[HintedSlot].up * HintLift;
            }

            float target = HintedSlot >= 0 ? _tuning.SlotHintIntensity : 0f;
            _hintGlow.Apply(Damp.Toward(_hintGlow.Intensity, target, _tuning.SlotHintEase, deltaTime));
        }

        /// <summary>The relic on its way to the shelf: on the tether, else in the cradle, or null.</summary>
        private Relic Incoming()
        {
            Relic towed = _tether.Towed;
            if (towed != null)
            {
                return towed;
            }

            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                if (_relics.Relics[i].State == RelicState.Cradled)
                {
                    return _relics.Relics[i];
                }
            }

            return null;
        }

        private Vector3 SlotPose(Relic relic, int slot)
        {
            return _slots[slot].position + _slots[slot].up * relic.RestHeight;
        }

        private void OnDestroy()
        {
            _halo?.Dispose();
        }
    }
}
