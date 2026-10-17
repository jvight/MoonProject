using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's visible progression (VISION ruling 11, docs/features/M3-11): the kit pieces its abilities bring and the
    /// gifts its friends give, each a node hidden until earned.
    /// <list type="bullet">
    /// <item>Equipment (<see cref="IRoverEquipment"/>, 0.5.5 §3.5): what a loaded game has fitted is there at once,
    /// read on the first tick of play (old saves finish migrating in Gameplay's Start); nothing is announced.</item>
    /// <item>A part fitted in play (<see cref="PartFitted"/>) plays its moment: bay kit (the Warm Headlamp's lamp bar,
    /// the Boost Coils' drums, the Cargo Cradle's rack, the Hover-Jump's coils, Cell Pack II's spine) is carried onto
    /// its socket by the Rover Bay's arms, or its floor arm for the coils (VISION ruling 14, <see cref="BayFitting"/>),
    /// in turn; Kenji's port parts (the tether ring, the tape deck) and Tilly's gifted cell grow in place. A swap
    /// takes the old part off first. A bay that is missing or cannot reach a socket is a contract bug: logged, and the
    /// piece simply appears.</item>
    /// <item>The moment's beats are <see cref="FeelKind.Install"/>: Anticipate as it starts (Seconds = until the seat;
    /// Strength 1 for a fit, 0.5 for a gift's soft settle), Impact as it seats. A part taken off
    /// (<see cref="PartRemoved"/>) shrinks away.</item>
    /// <item>Bell's gift (<see cref="IFriendRoster"/>, <see cref="FriendGift"/>), the fresh "07" and pennant, is not
    /// a socket part: repaired before the game loaded, there at once; during play, the soft moment the next time 07 is
    /// home. The tether ring's band glows faintly at rest and brightly while the tether holds.</item>
    /// <item>The road light: the lamp bar makes it wider and warmer and moves it to the bar's middle glass, and its
    /// three glasses glow; the drums' bands glow with the boost. Both dim while 07 rests on the charging dock.</item>
    /// </list>
    /// Registered by <see cref="RoverController"/> as <see cref="IRoverCargoSeat"/> (the rack's RelicSeat). Ticked by
    /// <see cref="RoverController"/> in Update, so the rack has its pose for this frame before any LateUpdate reads it.
    /// The bay (<see cref="IRoverBay"/>, Gameplay) is resolved at its first fitting, as Gameplay initialises later; the
    /// fitting is registered as <see cref="IBayFitView"/> for the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverKit : MonoBehaviour, IRoverCargoSeat
    {
        private const int PieceCount = 9;
        private const int DrumCount = 2;

        /// <summary>Glow changes smaller than this are not worth a new property block.</summary>
        private const float GlowEpsilon = 1e-4f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset): 'Visible kit and gifts', 'Headlamp'.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("Warm spot light under RoverModel 'HeadlampSocket' (07's road light).")]
        [SerializeField] private Light _headlamp;

        [Tooltip("Art's Kit_LampBar under HeadlampSocket (inactive until the Warm Headlamp is owned).")]
        [SerializeField] private Transform _lampBar;

        [Tooltip("The lamp bar's glasses Lamp_0..2 (glow renderers on M_LowPolyGlowOff).")]
        [SerializeField] private Renderer[] _lamps = new Renderer[RoverModelNodes.KitLampCount];

        [Tooltip("Art's Kit_CapacitorDrum under DrumSocket_L and DrumSocket_R, in that order (inactive until owned).")]
        [SerializeField] private Transform[] _drums = new Transform[DrumCount];

        [Tooltip("Each drum's Glow band (glow renderer on M_LowPolyGlowOff, cyan), in the same order.")]
        [SerializeField] private Renderer[] _drumGlows = new Renderer[DrumCount];

        [Tooltip("Art's Kit_CargoRack under CargoSocket (inactive until the Cargo Cradle is owned).")]
        [SerializeField] private Transform _cargoRack;

        [Tooltip("Art's Kit_BatterySpine under SpineSocket (inactive until Cell Pack II is owned).")]
        [SerializeField] private Transform _spine;

        [Tooltip("Art's Kit_TapeDeck under DeckSocket (inactive until Kenji's garage port fits it).")]
        [SerializeField] private Transform _tapeDeck;

        [Tooltip("Art's Kit_TetherRing on the Eye (inactive until Kenji's garage port fits it).")]
        [SerializeField] private Transform _tetherRing;

        [Tooltip("The tether ring's Glow band (glow renderer on M_LowPolyGlowOff, cyan).")]
        [SerializeField] private Renderer _ringGlow;

        [Tooltip("The rack's RelicSeat, where a carried relic rests (+Y up).")]
        [SerializeField] private Transform _relicSeat;

        [Tooltip("RoverModel 'SolarWing/CellFilled': Tilly's replacement cell (hidden until her gift).")]
        [SerializeField] private Transform _solarCell;

        [Tooltip("RoverModel 'Body/Decal07Fresh': Ro's fresh '07' (hidden until Bell's gift).")]
        [SerializeField] private Transform _freshSerial;

        [Tooltip("RoverModel 'Antenna/Pennant': Bell's radio pennant (hidden until her gift).")]
        [SerializeField] private Transform _pennant;

        private readonly KitFit[] _fits = new KitFit[PieceCount];
        /// <summary>The Install beats' strength for a gift's soft settle (a fit is 1).</summary>
        private const float GiftStrength = 0.5f;

        private readonly FriendGift _bellGift = new FriendGift();
        private readonly int[] _requested = new int[PieceCount];
        private readonly Transform[] _parts = new Transform[BayFitting.MaxParts];
        private RoverController _rover;
        private RoverVisualRig _rig;
        private GameContext _context;
        private EventBus _events;
        private IWorldLayout _world;
        private IFriendState _bell;
        private IDisposable _partsFitted;
        private IDisposable _partsRemoved;
        private IDisposable _tetherAttached;
        private IDisposable _tetherReleased;
        private bool _tetherHolds;
        private float _ringLevel = -1f;
        private MaterialPropertyBlock _glowBlock;
        private BayFitting _bayFitting;
        private IRoverBay _bay;
        private BayArm[] _arms;
        private int _requests;
        private bool _watching;
        private Vector3 _lampBarScale;
        private Vector3[] _drumScale;
        private Vector3 _rackScale;
        private Vector3 _spineScale;
        private Vector3 _deckScale;
        private Vector3 _ringScale;
        private Vector3 _cellScale;
        private Vector3 _serialScale;
        private Vector3 _pennantScale;
        private float _baseIntensity;
        private float _baseRange;
        private float _baseSpotAngle;
        private float _baseInnerSpotAngle;
        private Color _baseColor;
        private Vector3 _baseLightPosition;
        private Vector3 _warmLightPosition;
        private float _appliedWarmth = -1f;
        private float _lampLevel = 1f;
        private float _appliedLampLevel = -1f;
        private bool _docked;
        private BatteryLink _battery;
        private float _appliedDrumGlow = -1f;
        private bool _started;

        /// <summary>True when the piece is on 07 right now (on its way on, settling in or fitted).</summary>
        public bool Shows(RoverKitPiece piece)
        {
            return _fits[(int)piece].Visible;
        }

        /// <summary>The fit of <paramref name="piece"/> (for tests and tooling).</summary>
        public KitFit Fit(RoverKitPiece piece)
        {
            return _fits[(int)piece];
        }

        /// <summary>True while the Rover Bay is fitting a piece onto 07 (holding 07 on its turntable).</summary>
        public bool IsFitting => _bayFitting != null && _bayFitting.Active;

        /// <summary>The install moment in the bay (for tests and tooling).</summary>
        public BayFitting BayFitting => _bayFitting;

        /// <summary>How bright the road light is right now as a share of normal (dimmed while docked).</summary>
        public float LampLevel => _lampLevel;

        /// <summary>Tilly's cell is fitted (the solar wing then opens wider at rest).</summary>
        public bool HasMendedWing => !_fits[(int)RoverKitPiece.SolarCell].IsHidden;

        // IRoverCargoSeat
        public bool IsFitted => _rover.Has(RoverAbility.CargoCradle);

        public Vector3 Position => _relicSeat.position;

        public Quaternion Rotation => _relicSeat.rotation;

        /// <summary>Checks wiring, hides all kit and gifts, sets the road light; false when broken (logged).</summary>
        public bool Initialize(GameContext context, RoverController rover, RoverVisualRig rig)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _rover = rover;
            _rig = rig;
            _context = context;
            _battery = new BatteryLink(context);
            _events = context.Events;
            _world = context.Get<IWorldLayout>();
            KitSettings settings = _tuning.Kit;
            for (int i = 0; i < PieceCount; i++)
            {
                RoverKitPiece piece = (RoverKitPiece)i;
                _fits[i] = new KitFit(settings, !RoverKitPieces.TryGetAbility(piece, out _));
            }

            _glowBlock = new MaterialPropertyBlock();
            _bayFitting = new BayFitting(settings.Bay, rover);
            context.Register<IBayFitView>(_bayFitting);
            CacheRestScales();
            ConfigureHeadlamp();
            HideAll();
            _partsFitted = _events.Subscribe<PartFitted>(OnPartFitted);
            _partsRemoved = _events.Subscribe<PartRemoved>(OnPartRemoved);
            _tetherAttached = _events.Subscribe<TetherAttached>(attached => _tetherHolds = true);
            _tetherReleased = _events.Subscribe<TetherReleased>(released => _tetherHolds = false);
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverRigTuning is not assigned.")
                & Require(_headlamp != null, "Headlamp light is not assigned.")
                & Require(_lampBar != null && _cargoRack != null && _relicSeat != null && _spine != null,
                    "Lamp bar, cargo rack, its RelicSeat or the battery spine is not assigned.")
                & Require(_tapeDeck != null, "The tape deck (Kit_TapeDeck on DeckSocket) is not assigned.")
                & Require(_tetherRing != null && _ringGlow != null,
                    "The tether ring (Kit_TetherRing on the Eye) or its Glow band is not assigned.")
                & Require(_solarCell != null && _freshSerial != null && _pennant != null,
                    "A gift node (CellFilled, Decal07Fresh, Pennant) is not assigned.")
                & Require(AllSet(_lamps, RoverModelNodes.KitLampCount), "The lamp bar needs its three glasses.")
                & Require(AllSet(_drums, DrumCount) && AllSet(_drumGlows, DrumCount),
                    "Both capacitor drums and their glow bands are required.");
            return ok;
        }

        private static bool AllSet<T>(T[] items, int count) where T : UnityEngine.Object
        {
            if (items == null || items.Length != count)
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverKit)}: {message}", this);
            }

            return condition;
        }

        private void CacheRestScales()
        {
            _lampBarScale = _lampBar.localScale;
            _drumScale = new Vector3[DrumCount];
            for (int i = 0; i < DrumCount; i++)
            {
                _drumScale[i] = _drums[i].localScale;
            }

            _rackScale = _cargoRack.localScale;
            _spineScale = _spine.localScale;
            _deckScale = _tapeDeck.localScale;
            _ringScale = _tetherRing.localScale;
            _cellScale = _solarCell.localScale;
            _serialScale = _freshSerial.localScale;
            _pennantScale = _pennant.localScale;
        }

        /// <summary>The plain road light from the rig tuning, and where the lamp bar's middle glass moves it.</summary>
        private void ConfigureHeadlamp()
        {
            _headlamp.type = LightType.Spot;
            _headlamp.transform.localRotation = Quaternion.Euler(_tuning.HeadlampDownTilt, 0f, 0f);
            _baseIntensity = _tuning.HeadlampIntensity;
            _baseRange = _tuning.HeadlampRange;
            _baseSpotAngle = _tuning.HeadlampSpotAngle;
            _baseInnerSpotAngle = _tuning.HeadlampInnerSpotAngle;
            _baseColor = _headlamp.color;
            _baseLightPosition = _headlamp.transform.localPosition;
            Transform socket = _headlamp.transform.parent;
            Vector3 middleGlass = _lamps[RoverModelNodes.KitLampCount / 2].transform.position;
            _warmLightPosition = socket.InverseTransformPoint(middleGlass);
            ApplyWarmth(0f);
        }

        private void HideAll()
        {
            _lampBar.gameObject.SetActive(false);
            for (int i = 0; i < DrumCount; i++)
            {
                _drums[i].gameObject.SetActive(false);
            }

            _cargoRack.gameObject.SetActive(false);
            _spine.gameObject.SetActive(false);
            _tapeDeck.gameObject.SetActive(false);
            _tetherRing.gameObject.SetActive(false);
            _solarCell.gameObject.SetActive(false);
            _freshSerial.gameObject.SetActive(false);
            _pennant.gameObject.SetActive(false);
        }

        /// <summary>
        /// A part fitted in play: a swap takes the old one off first; bay kit waits its turn for the bay's arms, port
        /// parts and gifts grow in place. The Focus Lens and Side Bins show through their own drivers, so their moment
        /// is the seat alone.
        /// </summary>
        private void OnPartFitted(PartFitted fitted)
        {
            if (fitted.Replaced && (int)fitted.ReplacedPart < PieceCount)
            {
                _fits[(int)fitted.ReplacedPart].Remove();
            }

            float strength = fitted.Gift ? GiftStrength : 1f;
            int index = (int)fitted.Part;
            if (index >= PieceCount)
            {
                PublishInstall(FeelPhase.Anticipate, strength, 0f);
                PublishInstall(FeelPhase.Impact, strength, 0f);
                return;
            }

            KitFit fit = _fits[index];
            if (!fit.IsHidden)
            {
                return;
            }

            if (RoverKitPieces.TryGetAbility(fitted.Part, out _))
            {
                _requested[index] = ++_requests;
                return;
            }

            fit.Install();
            PublishInstall(FeelPhase.Anticipate, strength, _tuning.Kit.GrowSeconds);
        }

        /// <summary>A part taken off at its station shrinks away (its ability already paused).</summary>
        private void OnPartRemoved(PartRemoved removed)
        {
            int index = (int)removed.Part;
            if (index >= PieceCount)
            {
                return;
            }

            _requested[index] = 0;
            _fits[index].Remove();
        }

        /// <summary>What a loaded game has fitted is there at once (first tick, after every Start).</summary>
        private void ShowEquipped()
        {
            if (!_context.TryGet(out IRoverEquipment equipment))
            {
                Debug.LogError($"{nameof(RoverKit)}: no {nameof(IRoverEquipment)} is registered (Gameplay); "
                    + "fitted parts cannot show.", this);
                return;
            }

            foreach (RoverSocket socket in (RoverSocket[])Enum.GetValues(typeof(RoverSocket)))
            {
                if (equipment.TryGetFitted(socket, out RoverKitPiece part) && (int)part < PieceCount)
                {
                    _fits[(int)part].Show();
                }
            }
        }

        private void PublishInstall(FeelPhase phase, float strength, float seconds)
        {
            _events.Publish(new FeelBeat(FeelKind.Install, phase, _rover.Position, Vector3.up, strength, seconds));
        }

        public void Tick(float deltaTime)
        {
            if (!_started)
            {
                FindFriends();
                ShowEquipped();
            }

            Give(_bellGift.Step(IsAwake(_bell), IsHome()), RoverKitPiece.FreshPaint);
            StepBay(deltaTime);
            for (int i = 0; i < PieceCount; i++)
            {
                if (_fits[i].Step(deltaTime))
                {
                    bool gift = !RoverKitPieces.IsPortPiece((RoverKitPiece)i);
                    PublishInstall(FeelPhase.Impact, gift ? GiftStrength : 1f, 0f);
                }
            }

            ApplyPieces();
            DockSettings dock = _rover.Tuning.Dock;
            float lamp = _docked ? dock.LampDim : _battery.IsLowPower ? _tuning.FlankLamps.CrawlHeadlamp : 1f;
            _lampLevel = Smoothing.Damp(_lampLevel, lamp, dock.LampHalfLife, deltaTime);
            ApplyWarmth(_fits[(int)RoverKitPiece.LampBar].Lights);
            ApplyDrumGlow();
            ApplyRingGlow();
            _started = true;
        }

        /// <summary>07 rests on the charging dock (its road light and lamp bar dim) or left it.</summary>
        public void SetDocked(bool docked)
        {
            _docked = docked;
        }

        private void FindFriends()
        {
            if (!_context.TryGet(out IFriendRoster roster))
            {
                Debug.LogError($"{nameof(RoverKit)}: no {nameof(IFriendRoster)} is registered (Gameplay); friends' "
                    + "gifts cannot show.", this);
                return;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                IFriendState friend = roster.Get(i);
                if (friend.Id == RoverKitPieces.BellId)
                {
                    _bell = friend;
                }
            }

            if (_bell == null)
            {
                Debug.LogError($"{nameof(RoverKit)}: the friend roster lacks '{RoverKitPieces.BellId}'; her gift "
                    + "cannot show.", this);
            }
        }

        private static bool IsAwake(IFriendState friend)
        {
            return friend != null && friend.Activity != FriendActivity.Dormant
                && friend.Activity != FriendActivity.Repairing;
        }

        private bool IsHome()
        {
            KitSettings settings = _tuning.Kit;
            Vector3 offset = _rover.Position - _world.BasePosition;
            float radius = settings.GiftHomeRadius;
            return offset.x * offset.x + offset.z * offset.z <= radius * radius
                && _rover.Speed <= settings.GiftMaxSpeed;
        }

        private void Give(GiftCue cue, RoverKitPiece piece)
        {
            KitFit fit = _fits[(int)piece];
            if (cue == GiftCue.ShowSilently)
            {
                fit.Show();
            }
            else if (cue == GiftCue.Present)
            {
                fit.Install();
                PublishInstall(FeelPhase.Anticipate, GiftStrength, _tuning.Kit.GrowSeconds);
            }
        }

        /// <summary>The tether ring's band: faint at rest, bright while the tether holds (written on change).</summary>
        private void ApplyRingGlow()
        {
            KitFit fit = _fits[(int)RoverKitPiece.TetherRing];
            KitSettings settings = _tuning.Kit;
            float level = fit.Lights * (_tetherHolds ? settings.RingHoldGlow : settings.RingRestGlow);
            if (Mathf.Abs(level - _ringLevel) < GlowEpsilon)
            {
                return;
            }

            _ringLevel = level;
            _glowBlock.Clear();
            _glowBlock.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
            _glowBlock.SetFloat(EmissionStrengthId, Mathf.Clamp01(level));
            _ringGlow.SetPropertyBlock(_glowBlock);
        }

        /// <summary>Starts the next fitting the bay asked for once it is free; steps the one under way.</summary>
        private void StepBay(float deltaTime)
        {
            if (!_bayFitting.Active)
            {
                BeginNextFitting();
            }

            if (_bayFitting.Active && _bayFitting.Step(deltaTime) == BayCue.Landed)
            {
                RoverKitPiece piece = _bayFitting.Piece;
                _fits[(int)piece].Land();
                BayFitSettings settings = _tuning.Kit.Bay;
                _rig.KickHeave(-settings.SeatHeaveKick);
                _rig.KickAntenna(settings.SeatAntennaKick);
                PublishInstall(FeelPhase.Impact, 1f, 0f);
            }

            Watch();
        }

        /// <summary>07 watches the piece come in on the arm until it is on.</summary>
        private void Watch()
        {
            bool watching = _bayFitting.Active && _fits[(int)_bayFitting.Piece].IsCarried;
            if (watching)
            {
                _rover.Gaze.Set(this, _parts[0].position, _tuning.Kit.Bay.WatchPriority);
            }
            else if (_watching)
            {
                _rover.Gaze.Clear(this);
            }

            _watching = watching;
        }

        private void BeginNextFitting()
        {
            int next = -1;
            for (int i = 0; i < PieceCount; i++)
            {
                if (_requested[i] > 0 && (next < 0 || _requested[i] < _requested[next]))
                {
                    next = i;
                }
            }

            if (next < 0)
            {
                return;
            }

            var piece = (RoverKitPiece)next;
            _requested[next] = 0;
            KitFit fit = _fits[next];
            int count = CollectParts(piece);
            if (!ResolveBay() || !_bayFitting.Begin(_bay, _arms, piece, _parts, count,
                    piece == RoverKitPiece.HoverCoils, this))
            {
                fit.Show();
                PublishInstall(FeelPhase.Anticipate, 1f, 0f);
                PublishInstall(FeelPhase.Impact, 1f, 0f);
                return;
            }

            fit.Carry(_tuning.Kit.Bay.PickScale);
            PublishInstall(FeelPhase.Anticipate, 1f, _bayFitting.SeatSeconds);
        }

        /// <summary>Puts the nodes the bay carries for <paramref name="piece"/> in the parts buffer.</summary>
        private int CollectParts(RoverKitPiece piece)
        {
            switch (piece)
            {
                case RoverKitPiece.HoverCoils:
                    _parts[0] = _rover.HoverCoils.Mount;
                    return 1;
                case RoverKitPiece.LampBar:
                    _parts[0] = _lampBar;
                    return 1;
                case RoverKitPiece.CapacitorDrums:
                    _parts[0] = _drums[0];
                    _parts[1] = _drums[1];
                    return DrumCount;
                case RoverKitPiece.CargoRack:
                    _parts[0] = _cargoRack;
                    return 1;
                case RoverKitPiece.BatterySpine:
                    _parts[0] = _spine;
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(piece), piece,
                        "Only crafted kit is fitted in the bay.");
            }
        }

        /// <summary>
        /// The bay and its arms, kept once they check out against the art contract; false (logged at every fitting
        /// it fails) when the bay is missing or broken.
        /// </summary>
        private bool ResolveBay()
        {
            if (_arms != null)
            {
                return true;
            }

            if (!_context.TryGet(out IRoverBay bay))
            {
                Debug.LogError($"{nameof(RoverKit)}: no {nameof(IRoverBay)} is registered (Gameplay); the bay cannot "
                    + "fit kit onto 07.", this);
                return false;
            }

            if (bay.Turntable == null || bay.FloorLift == null || bay.FloorLift.parent == null || bay.FloorTip == null
                || bay.ArmCount < 1 || bay.ArmCount > BayPlanner.MaxArms)
            {
                Debug.LogError($"{nameof(RoverKit)}: the {nameof(IRoverBay)} lacks its Turntable, FloorLift, FloorTip "
                    + $"or 1..{BayPlanner.MaxArms} arms ({BayArm.Contract}).", this);
                return false;
            }

            var arms = new BayArm[bay.ArmCount];
            for (int i = 0; i < arms.Length; i++)
            {
                arms[i] = BayArm.Read(bay, i, out string problem);
                if (arms[i] == null)
                {
                    Debug.LogError($"{nameof(RoverKit)}: the Rover Bay's {problem} ({BayArm.Contract}).", this);
                    return false;
                }
            }

            _bay = bay;
            _arms = arms;
            return true;
        }

        private void ApplyPieces()
        {
            Place(_lampBar, _fits[(int)RoverKitPiece.LampBar], _lampBarScale);
            KitFit drums = _fits[(int)RoverKitPiece.CapacitorDrums];
            for (int i = 0; i < DrumCount; i++)
            {
                Place(_drums[i], drums, _drumScale[i]);
            }

            Place(_cargoRack, _fits[(int)RoverKitPiece.CargoRack], _rackScale);
            Place(_spine, _fits[(int)RoverKitPiece.BatterySpine], _spineScale);
            Place(_tapeDeck, _fits[(int)RoverKitPiece.TapeDeck], _deckScale);
            Place(_tetherRing, _fits[(int)RoverKitPiece.TetherRing], _ringScale);
            Place(_solarCell, _fits[(int)RoverKitPiece.SolarCell], _cellScale);
            KitFit paint = _fits[(int)RoverKitPiece.FreshPaint];
            Place(_freshSerial, paint, _serialScale);
            Place(_pennant, paint, _pennantScale);
        }

        /// <summary>
        /// A piece or gift node shown with its fit and scaled by it (written only while it moves or as it appears).
        /// Where it sits is the bay's while it is carried, its rest pose on its socket otherwise.
        /// </summary>
        private static void Place(Transform node, KitFit fit, Vector3 scale)
        {
            if (Reveal(node, fit))
            {
                node.localScale = scale * fit.Scale;
            }
        }

        /// <summary>Shows or hides <paramref name="node"/> with its fit; true when its pose needs writing.</summary>
        private static bool Reveal(Transform node, KitFit fit)
        {
            bool visible = fit.Visible;
            bool changed = node.gameObject.activeSelf != visible;
            if (changed)
            {
                node.gameObject.SetActive(visible);
            }

            return visible && (changed || fit.Moving);
        }

        /// <summary>
        /// The road light and the lamp bar's glasses, <paramref name="warmth"/> of the way to warm, at the current
        /// lamp level (dimmed on the dock).
        /// </summary>
        private void ApplyWarmth(float warmth)
        {
            if (Mathf.Abs(warmth - _appliedWarmth) < GlowEpsilon && Mathf.Abs(_lampLevel - _appliedLampLevel)
                < GlowEpsilon)
            {
                return;
            }

            KitSettings settings = _tuning.Kit;
            _headlamp.intensity = _lampLevel * Mathf.Lerp(_baseIntensity, settings.WarmIntensity, warmth);
            _headlamp.range = Mathf.Lerp(_baseRange, settings.WarmRange, warmth);
            _headlamp.spotAngle = Mathf.Lerp(_baseSpotAngle, settings.WarmSpotAngle, warmth);
            _headlamp.innerSpotAngle = Mathf.Lerp(_baseInnerSpotAngle, settings.WarmInnerSpotAngle, warmth);
            _headlamp.color = Color.Lerp(_baseColor, settings.WarmColor, warmth);
            _headlamp.transform.localPosition = Vector3.Lerp(_baseLightPosition, _warmLightPosition, warmth);
            SetGlow(_lamps, settings.LampBarGlow * warmth * _lampLevel);
            _appliedWarmth = warmth;
            _appliedLampLevel = _lampLevel;
        }

        private void ApplyDrumGlow()
        {
            KitSettings settings = _tuning.Kit;
            float lit = _fits[(int)RoverKitPiece.CapacitorDrums].Lights;
            float stored = settings.DrumIdleGlow * _battery.Drums01;
            float glow = lit * Mathf.Lerp(stored, settings.DrumGlow, _rover.BoostLevel);
            if (Mathf.Abs(glow - _appliedDrumGlow) < GlowEpsilon)
            {
                return;
            }

            SetGlow(_drumGlows, glow);
            _appliedDrumGlow = glow;
        }

        private void SetGlow(Renderer[] renderers, float intensity)
        {
            _glowBlock.SetVector(EmissionColorId, new Vector4(intensity, intensity, intensity, 1f));
            _glowBlock.SetFloat(EmissionStrengthId, Mathf.Clamp01(intensity));
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(_glowBlock);
            }
        }

        private void OnDestroy()
        {
            _partsFitted?.Dispose();
            _partsRemoved?.Dispose();
            _tetherAttached?.Dispose();
            _tetherReleased?.Dispose();
        }
    }
}
