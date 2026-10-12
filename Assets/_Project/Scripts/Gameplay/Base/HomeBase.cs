using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home: stands the lander beside the base pad facing it and keeps it carrying across the basin (pillar 6): its
    /// lamps light the ground as 07 comes home and dim to a light left on while it is away, but the windows never dim
    /// (a little brighter far away) and a soft amber <see cref="HomeHalo"/> over the lander grows in with distance, so
    /// from far away home is a small amber cluster. It keeps the crew's keepsakes up in the lab (docs/features/M3-19
    /// §5): with the turntable facing the keepsake shelf, the Tether press lifts the relic that rode up beside 07 (or
    /// the one in 07's Cargo Cradle) into the free cubby nearest it; it floats there and settles with a soft overshoot:
    /// <see cref="RelicDeposited"/> and a save. While there is a relic to place, a warm glow marks the cubby it will
    /// take. Displayed relics turn slowly. In front of the lander the <see cref="ChargingDock"/> is where 07 rests at
    /// home.
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

        [Tooltip("Root of the lander, bay and tower (placed beside the base pad at boot).")]
        [SerializeField] private Transform _root;

        [Tooltip("The lander's 'Windows' glow renderer.")]
        [SerializeField] private Renderer _windows;

        [Tooltip("The lander's LampSocket_0..3.")]
        [SerializeField] private Transform[] _lampSockets = Array.Empty<Transform>();

        [Tooltip("The lab's keepsake shelf (Lab/Station_Keepsakes).")]
        [SerializeField] private Transform _keepsakes;

        [Tooltip("The keepsake shelf's cubbies (Slot_0..n), in order.")]
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();

        [Tooltip("The lander's DockAnchor: where 07 rests on the charging dock (its pivot, facing the lander).")]
        [SerializeField] private Transform _dockAnchor;

        [Tooltip("The lander's DockGlow renderer (the charging dock's contacts and strip).")]
        [SerializeField] private Renderer _dockGlow;

        [Tooltip("The dock lock's walls DockWall_0..3 (front, right, back, left), children of DockGlow.")]
        [SerializeField] private Transform[] _dockWalls = Array.Empty<Transform>();

        [Tooltip("The walls' lamp band segments Strip_0..2, wall by wall in the same order (12).")]
        [SerializeField] private Renderer[] _dockStrips = Array.Empty<Renderer>();

        [Tooltip("The dock's rim lamp rings DockRim_0..2 (outer to inner).")]
        [SerializeField] private Renderer[] _dockRims = Array.Empty<Renderer>();

        private EventBus _events;
        private IRoverState _rover;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RelicField _relics;
        private LiftStation _lift;
        private UpgradeService _upgrades;
        private RelicTuning _relicTuning;
        private Light[] _lamps = Array.Empty<Light>();
        private EmissionGlow _windowGlow;
        private HomeHalo _halo;
        private IHomeLights _lights;
        private ChargingDock _dock;
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

        /// <summary>Relics on the keepsake shelf (settled ones).</summary>
        public int DisplayedCount { get; private set; }

        /// <summary>0..1 share of the lamps' light right now (07's distance, eased): 1 home, a light left on far away.
        /// </summary>
        public float Warmth => _tuning.LampWarmth(_farness);

        /// <summary>Window glow right now (linear emission multiplier).</summary>
        public float WindowLevel => _windowGlow != null ? _windowGlow.Intensity : 0f;

        /// <summary>The halo over home (tests and debugging views).</summary>
        internal HomeHalo Halo => _halo;

        /// <summary>The charging dock where 07 rests at home.</summary>
        public ChargingDock Dock => _dock;

        /// <summary>The lab's keepsake shelf.</summary>
        public Vector3 KeepsakesPosition => _keepsakes.position;

        /// <summary>The front of home a friend looks over when she checks on it: the charging dock.</summary>
        public Vector3 InspectPosition => _dockAnchor.position;

        /// <summary>Which way a friend faces home's front from: out from the dock.</summary>
        public Vector3 InspectForward => -_dockAnchor.forward;

        public Vector3 LanderPosition => _root.position;

        /// <summary>The cubby the relic 07 brought up would take if placed now, or -1.</summary>
        public int HintedSlot { get; private set; } = -1;

        public float HintLevel => _hintGlow != null ? _hintGlow.Intensity : 0f;

        public BaseTuning Tuning => _tuning;

        /// <summary>07 is up in the lab with the turntable at rest facing the keepsake shelf.</summary>
        public bool KeepsakesFaced => _lift != null && _lift.Faces(LabStation.Keepsakes);

        /// <summary>A press would place the relic that rode up: the shelf is faced and a cubby is free.</summary>
        public bool CanPlace
        {
            get
            {
                Relic rider = KeepsakesFaced ? _lift.Rider : null;
                return rider != null && ShelfSlots.Nearest(rider.transform.position, _slotPositions, _taken) >= 0;
            }
        }

        internal void Wire(BaseTuning tuning, Transform root, Renderer windows, Transform[] lampSockets,
            Transform keepsakes, Transform[] slots, Transform dockAnchor, Renderer dockGlow)
        {
            _tuning = tuning;
            _root = root;
            _windows = windows;
            _lampSockets = lampSockets;
            _keepsakes = keepsakes;
            _slots = slots;
            _dockAnchor = dockAnchor;
            _dockGlow = dockGlow;
        }

        /// <summary>The dock lock's walls, their strips and the rim lamp rings (feel-pass §7.1).</summary>
        internal void WireDockLock(Transform[] walls, Renderer[] strips, Renderer[] rims)
        {
            _dockWalls = walls;
            _dockStrips = strips;
            _dockRims = rims;
        }

        /// <param name="lights">
        /// How lit home's warm points are as power returns (World, docs/features/M3-16): the windows, porch lamps and
        /// halo glow at their share of it.
        /// </param>
        /// <param name="lift">The belly lift: the relics that ride up with 07 and the lab's turntable.</param>
        internal bool Initialize(GameplayServices services, RelicField relics, UpgradeService upgrades,
            IHomeLights lights, LiftStation lift)
        {
            string problem = _tuning == null ? "BaseTuning is not assigned."
                : _root == null || _keepsakes == null ? "the base root or the lab's keepsake shelf is not assigned."
                : _windows == null ? "the lander windows are not assigned."
                : _slots.Length == 0 ? "the keepsake shelf has no cubbies."
                : _dockAnchor == null || _dockGlow == null ? "the lander's DockAnchor or DockGlow is not assigned."
                : _dockWalls.Length != DockLock.Walls || _dockStrips.Length != DockLock.Segments ||
                  _dockRims.Length != DockLock.RimRings || Array.IndexOf(_dockWalls, null) >= 0 ||
                  Array.IndexOf(_dockStrips, null) >= 0 || Array.IndexOf(_dockRims, null) >= 0
                    ? "the dock lock's DockWall_0..3 with their Strip_0..2 and DockRim_0..2 are not all assigned."
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
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _lights = lights ?? throw new ArgumentNullException(nameof(lights));
            _lift = lift != null ? lift : throw new ArgumentNullException(nameof(lift));
            _relicTuning = relics.Tuning;
            PlaceLander(services.Layout.BasePosition);

            _windowGlow = new EmissionGlow(_windows);
            _halo = new HomeHalo(transform, services.Meshes.Quad, services.Visuals.HomeHalo, _tuning, _root);
            _dock = new ChargingDock(_dockAnchor, _dockGlow, _dockWalls, _dockStrips, _dockRims, _tuning, _events);
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
            int count = relics.Relics.Count;
            _taken = new bool[_slots.Length];
            _carryTime = new float[count];
            _carryFrom = new Vector3[count];
            _carryFromRotation = new Quaternion[count];
            _displayAngle = new float[count];
            ReadSlots();
            _farness = _tuning.FarnessAt(Vector3.Distance(_rover.Position, _root.position));
            _initialized = true;
            return true;
        }

        /// <summary>After a load: puts every displayed relic on its cubby (a free one if its cubby is taken).</summary>
        internal void SyncDisplays()
        {
            ReadSlots();
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
                    Debug.LogError($"{nameof(HomeBase)}: no free cubby for saved relic '{relic.Definition.Id}'.",
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

        /// <summary>
        /// The Tether press facing the keepsake shelf: the relic that rode up beside 07 floats into the free cubby
        /// nearest it. False when there is none to place, the shelf is not faced, or every cubby is taken.
        /// </summary>
        internal bool TryPlace()
        {
            if (!CanPlace)
            {
                return false;
            }

            Relic rider = _lift.Rider;
            _lift.LetRiderGo(rider);
            return BeginDeposit(rider);
        }

        /// <summary>
        /// A relic riding in the Cargo Cradle is set down here: true when the keepsake shelf is faced and a cubby is
        /// free (it then floats onto the shelf like any deposit).
        /// </summary>
        internal bool TakeFromCradle(Relic relic)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            return relic.State == RelicState.Cradled && KeepsakesFaced && BeginDeposit(relic);
        }

        private void PlaceLander(Vector3 basePosition)
        {
            Vector3 spot = LanderSpot(_terrain, basePosition, _tuning);
            Vector3 facing = basePosition - spot;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            _root.SetPositionAndRotation(spot, rotation);
        }

        /// <summary>The cubbies' world positions (once the lander stands in place).</summary>
        private void ReadSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slotPositions[i] = _slots[i].position;
            }
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
            _dock.Step(_rover, now, deltaTime);
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                switch (relic.State)
                {
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
            float lamp = _tuning.LampIntensity * warmth * boost * _lights.Level(HomeLight.PorchLamps);
            for (int i = 0; i < _lamps.Length; i++)
            {
                _lamps[i].intensity = lamp;
            }

            _windowGlow.Apply(_tuning.WindowGlowAt(_farness) * boost * _lights.Level(HomeLight.Windows));
            _halo.Boost = boost * _lights.Level(HomeLight.Halo);
        }

        /// <returns>False when every cubby is taken (the relic stays where it is).</returns>
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
            Relic incoming = KeepsakesFaced ? Incoming() : null;
            HintedSlot = incoming != null
                ? ShelfSlots.Nearest(incoming.transform.position, _slotPositions, _taken)
                : -1;
            if (HintedSlot >= 0)
            {
                _hint.position = _slotPositions[HintedSlot] + _slots[HintedSlot].up * HintLift;
            }

            float target = HintedSlot >= 0 ? _tuning.SlotHintIntensity : 0f;
            _hintGlow.Apply(Damp.Toward(_hintGlow.Intensity, target, _tuning.SlotHintEase, deltaTime));
        }

        /// <summary>The relic waiting to be placed: the one that rode up, else the one in the cradle, or null.</summary>
        private Relic Incoming()
        {
            Relic rider = _lift.Rider;
            if (rider != null)
            {
                return rider;
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
            _dock?.Dispose();
        }
    }
}
