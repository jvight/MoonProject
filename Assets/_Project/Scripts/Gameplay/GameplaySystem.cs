using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Gameplay domain's single entry in the bootstrap's system list (after World and Rover), for the opening
    /// (docs/design/opening-v2.md): resolves the world (surface, layout, anchors, sunlight), rover and camera services,
    /// creates 07's hold, its cutter and start kit, initialises the gameplay parts in dependency order (salvage, the
    /// battery, base building, the opening with its crates and pads, scanning), registers the services other domains
    /// read (<see cref="IMaterialStock"/>, <see cref="ICargoHold"/>, <see cref="ICutter"/>,
    /// <see cref="IRoverSystems"/>, <see cref="ISalvageStatus"/>, <see cref="IInteractionHints"/>, 07's battery as
    /// <see cref="IRoverBattery"/> and <see cref="IBuildEnergy"/>, its beam work as <see cref="IBeamStatus"/>, the
    /// recovery protocol as <see cref="IRecoveryProgress"/>, and <see cref="BuildField"/>) and the save sections, and
    /// owns the shared glow meshes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySystem : MonoBehaviour, IGameSystem
    {
        [Tooltip("SoftGlow materials (Generated/Gameplay/GameplayVisuals.asset).")]
        [SerializeField] private GameplayVisuals _visuals;

        [Tooltip("How pickups glint from afar (Assets/_Project/Data/Tuning/Gameplay/GlintTuning.asset).")]
        [SerializeField] private GlintTuning _glints;

        [Tooltip("What each material is (Assets/_Project/Data/Content/MaterialCatalog.asset).")]
        [SerializeField] private MaterialCatalog _materialCatalog;

        [Tooltip("07's hold and the sealed containers (Assets/_Project/Data/Tuning/Gameplay/CargoTuning.asset).")]
        [SerializeField] private CargoTuning _cargo;

        [Tooltip("07's cutter tiers (Assets/_Project/Data/Tuning/Gameplay/CutterTuning.asset).")]
        [SerializeField] private CutterTuning _cutterTuning;

        [Tooltip("07's scan archive: every databank entry (Assets/_Project/Data/Content/DatabankCatalogue.asset).")]
        [SerializeField] private DatabankCatalogue _databankCatalogue;

        [Tooltip("The ruins' structure sets, the story scans and the scan verb (Assets/_Project/Data/Tuning/Gameplay/" +
                 "ScanTuning.asset).")]
        [SerializeField] private ScanTuning _scanTuning;

        [Tooltip("The dock lock on a built pad (Assets/_Project/Data/Tuning/Gameplay/BaseTuning.asset).")]
        [SerializeField] private BaseTuning _baseTuning;

        [Tooltip("The opening's anchors, lander reserve, cargo scan points and supply crates " +
                 "(Assets/_Project/Data/Tuning/Gameplay/OpeningTuning.asset).")]
        [SerializeField] private OpeningTuning _openingTuning;

        [SerializeField] private SalvageField _salvage;
        [SerializeField] private BatteryField _battery;
        [SerializeField] private BuildField _building;
        [SerializeField] private Transform[] _ruins = Array.Empty<Transform>();

        [Tooltip("World's story props in the basin ([World]/BasinProps, e.g. the Scout): hosts of story scans only.")]
        [SerializeField] private Transform[] _storyProps = Array.Empty<Transform>();

        [Tooltip("World's service port on the lander ([World]/Lander/ServicePort): the open crate stands at it.")]
        [SerializeField] private Transform _landerPort;

        [Tooltip("The bay outline on World's apron markings ([World]/OpeningProps/ApronMarkings/BayGlowOutline): it " +
                 "glows while build mode is open.")]
        [SerializeField] private Renderer _apronOutline;

        [Tooltip("World's apron cable channel ([World]/OpeningProps/ApronMarkings/CableChannel_Anchor): where the " +
                 "lander's cable across the apron takes a pad's own cable.")]
        [SerializeField] private Transform _apronCableChannel;

        /// <summary>The recovery protocol's Power steps: the pad, solar power on, a night-ready base
        /// (opening-v2).</summary>
        private const int PowerSteps = 3;

        /// <summary>The E verbs a scan yields to (0.5.5 §1.2: scanning comes last).</summary>
        private static readonly InteractionKind[] VerbsBeforeScan =
        {
            InteractionKind.PickUp, InteractionKind.Build, InteractionKind.Salvage,
        };

        private readonly List<IDisposable> _saveTokens = new List<IDisposable>();
        private readonly List<IDisposable> _flowTokens = new List<IDisposable>();
        private IRoverState _roverState;
        private InputReader _input;
        private GlowMeshSet _meshes;

        public MaterialStock Materials { get; private set; }

        /// <summary>Cargo tuning (the hold, the containers).</summary>
        public CargoTuning Cargo => _cargo;

        /// <summary>07's cutter as its fitted kits and its start kit make it (docs/features/M3-26, opening-v2
        /// §10).</summary>
        public Cutter Cutter { get; private set; }

        /// <summary>
        /// What 07's hold had no room for, held where it was (docs/features/M3-25 §2.3, 0.5.5 §2): pieces on their
        /// wrecks.
        /// </summary>
        public CargoBundles Bundles { get; private set; }

        /// <summary>The world's sealed containers: every find that rests in one (0.5.5 §2).</summary>
        public SealedContainers Containers { get; private set; }

        public IInteractionHints Hints { get; private set; }

        /// <summary>The apron's bay outline, glowing while build mode is open (B5: where a base belongs).</summary>
        public ApronGlow ApronGlow { get; private set; }

        /// <summary>The first pad's home: a readable frame and marker on the apron until a pad is placed.</summary>
        public ApronGuide ApronGuide { get; private set; }

        /// <summary>The ruins' and the lander's scan targets, scanned to learn structures (0.5.5 §1).</summary>
        public ScanField Scans { get; private set; }

        /// <summary>The wire box that shows which target E would scan.</summary>
        public ScanTargetMark ScanMark { get; private set; }

        /// <summary>07's scan archive: what the scans and milestones unlocked (docs/design/databank.md).</summary>
        public Databank Databank { get; private set; }

        /// <summary>The recovery protocol's rows (docs/features/M3-21).</summary>
        public RecoveryProgress Recovery { get; private set; }

        /// <summary>07's start kit (opening-v2 §10): what works, what is broken, the B6 igniter fit.</summary>
        public RoverStartKit StartKit { get; private set; }

        /// <summary>The opening in the world: the lander, the built pads, the supply crates, the objective
        /// flow.</summary>
        public OpeningField Opening { get; private set; }

        /// <summary>The call from the sky (opening v3 §2): its state, its decode and its point of light.</summary>
        public CallProgress Call { get; private set; }

        public CallReply Reply { get; private set; }

        public MastRaise MastRaise { get; private set; }

        public MastRaiseView MastRaiseView { get; private set; }

        public CallAnswer Answer { get; private set; }

        public NetworkPulseView PulseView { get; private set; }

        public ReleaseSequence Release { get; private set; }

        /// <summary>Seconds of holding E at the mast to transmit (tests).</summary>
        public float HoldSecondsForTests => _openingTuning.Answer.HoldSeconds;

        /// <summary>How the mast raises and the reply times (tests).</summary>
        public CallReplySettings MastRaiseSettings => _openingTuning.Reply;

        public RangeRingField RangeRing { get; private set; }

        public RangeRingLine RangeRingLine { get; private set; }

        /// <summary>The dock lock on the built pad 07 charges on (opening-v2 §5).</summary>
        public PadLock PadLock { get; private set; }

        /// <summary>The base's lamps, which follow the Sun (null while the opening could not be built).</summary>
        public BaseLamps BaseLamps { get; private set; }

        /// <summary>07's winch cable: what it tows, hooked and let go with the tether key (opening v3 R6).</summary>
        public TowLine Tow { get; private set; }

        public SalvageField Salvage => _salvage;

        /// <summary>07's battery: drains, chargers, the crawl and the warnings (docs/features/M3-20).</summary>
        public BatteryField Battery => _battery;

        /// <summary>07's free-placement base building field (0.5.7 slice 6).</summary>
        public BuildField Building => _building;

        /// <summary>What base structures 07 knows how to build (0.5.7 slice 6).</summary>
        public KnownStructures KnownStructures => _building.Known;

        /// <summary>The placed base structures and their power grid (0.5.7 slice 6).</summary>
        public PlacedBase PlacedBase => _building.PlacedBase;

        internal void Wire(GameplayVisuals visuals, GlintTuning glints, SalvageField salvage)
        {
            _visuals = visuals;
            _glints = glints;
            _salvage = salvage;
        }

        internal void WireCutter(CutterTuning cutter)
        {
            _cutterTuning = cutter;
        }

        internal void WireScans(ScanTuning scans)
        {
            _scanTuning = scans;
        }

        internal void WireBattery(BatteryField battery)
        {
            _battery = battery;
        }

        internal void WireOpening(OpeningTuning opening, BaseTuning dockLock)
        {
            _openingTuning = opening;
            _baseTuning = dockLock;
        }

        internal void WireBuilding(BuildField building)
        {
            _building = building;
        }

        internal void WireRuins(Transform[] ruins)
        {
            _ruins = ruins ?? Array.Empty<Transform>();
        }

        internal void WireStoryProps(Transform[] storyProps)
        {
            _storyProps = storyProps ?? Array.Empty<Transform>();
        }

        internal void WireLanderPort(Transform landerPort)
        {
            _landerPort = landerPort;
        }

        internal void WireApronOutline(Renderer apronOutline)
        {
            _apronOutline = apronOutline;
        }

        internal void WireApronCableChannel(Transform channel)
        {
            _apronCableChannel = channel;
        }

        internal void WireDatabank(DatabankCatalogue catalogue)
        {
            _databankCatalogue = catalogue;
        }

        internal void WireCargo(MaterialCatalog catalog, CargoTuning cargo)
        {
            _materialCatalog = catalog;
            _cargo = cargo;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string problem = WiringProblem();
            if (problem != null)
            {
                Fail(problem);
                return;
            }

            _meshes = new GlowMeshSet();
            Materials = new MaterialStock(context.Events,
                new CargoHold(context.Events, _materialCatalog, _cargo.HoldSlots));
            var save = context.Get<ISaveService>();
            var services = new GameplayServices(context.Events, context.Input, context.Get<ITerrainQuery>(),
                context.Get<IWorldLayout>(), context.Get<IWorldAnchors>(), context.Get<IRoverState>(),
                context.Get<IRoverRig>(), context.Get<IViewCamera>(), save, Materials, _visuals, _glints, _meshes);
            _roverState = services.Rover;
            _input = services.Input;
            GlintVeil veil = services.GlintVeil;
            _flowTokens.Add(context.Events.Subscribe<TitleShown>(_ => veil.Hide()));
            _flowTokens.Add(context.Events.Subscribe<SessionStarted>(_ => veil.Show(Time.time)));
            var abilities = context.Get<IRoverAbilities>();
            Cutter = new Cutter(context.Events, abilities, _cutterTuning);
            Containers = new SealedContainers(_salvage, _cargo, Cutter, services);
            if (!_salvage.Initialize(services, Cutter, _scanTuning) || !_battery.Initialize(services, abilities) ||
                !_building.Initialize(services))
            {
                Fail("a gameplay part did not initialise (see its error).");
                return;
            }

            Bundles = new CargoBundles(transform, services, _salvage, _materialCatalog, _cargo);
            _salvage.ConnectHeld(Bundles);
            _salvage.Connect(Containers);
            StartKit = new RoverStartKit(context.Events, Materials.Hold, () => PadLock != null && PadLock.OnPad,
                _openingTuning.IgniterPart, _openingTuning.IsFieldFit(_openingTuning.IgniterPart));
            Cutter.ConnectSystems(StartKit);
            _battery.ConnectSystems(StartKit);
            _battery.ConnectStructures(_building.Catalog);
            Opening = NewOpening(context, services);
            if (Opening != null)
            {
                context.Register<ISupplyPurposes>(Opening.Caches);
            }

            Scans = Opening != null ? NewScans(services) : null;
            Containers.YieldTo(ScanComesFirst);
            _salvage.YieldTo(ScanComesFirst);
            Opening?.Caches.HoldBack(_openingTuning.CargoCrate, CargoScanned);
            if (Scans == null)
            {
                Fail("the opening or scanning did not stand (see its error).");
                return;
            }

            Databank = new Databank(_databankCatalogue, context.Events,
                new WorldDatabankSources(Scans, Opening.Flow.HasReached));
            _salvage.ConnectScans(Scans);
            ApronGlow = new ApronGlow(_apronOutline, _building.Tuning.ApronGlowFadeSeconds);
            ApronGuide = new ApronGuide(transform, _apronOutline, _visuals.BeamCore, _building.Tuning);
            ScanMark = new ScanTargetMark(transform, _visuals.BeamCore, _scanTuning);
            _building.MarkApron(_apronOutline.bounds.center);
            var firstUse = new FirstUseSpots(Opening, _building, context.Get<IViewCamera>(), context.Get<IRoverState>(),
                _building.Tuning.FirstUseViewMargin);
            var hints = new InteractionHints(_salvage, Containers, Scans, _building, services.Beam, Cutter,
                services.Pickups, firstUse);
            Hints = hints;
            Recovery = new RecoveryProgress(context.Events, PowerLamps, PowerSteps, _openingTuning.RecoveryLamps);
            Call = new CallProgress(context.Events, _openingTuning.Call, services.Layout);
            Reply = new CallReply(context.Events, Call, _openingTuning.Reply);
            MastRaise = new MastRaise(context.Events, _openingTuning.Reply);
            MastRaiseView = new MastRaiseView(context.Events, _building, MastRaise, _openingTuning.Reply);
            services.Anchors.TryGet(WorldAnchorIds.RelayPrefix + "1", out WorldAnchor relay);
            Answer = new CallAnswer(context.Events, Call, Reply, MastRaise, _openingTuning.Answer, relay.Position,
                services.Layout.PeakPosition);
            RangeRing = new RangeRingField(context.Events, _battery.Tuning.RangeRing,
                new RangeRingModel(_battery.Tuning, _battery.Tuning.RangeRing, services.Terrain), _battery.Model,
                Opening.Pads, services.Rover);
            RangeRingLine = new RangeRingLine(transform, _visuals.BeamCore, RangeRing, _battery.Tuning.RangeRing);
            Tow = NewTow(services);
            if (Tow == null)
            {
                Fail("the tow did not stand (see its error).");
                return;
            }

            _battery.ConnectTow(Tow);
            _flowTokens.Add(context.Events.Subscribe<ChargingChanged>(OnChargingChanged));
            context.Register<IMaterialStock>(Materials);
            context.Register<IMaterialCatalog>(_materialCatalog);
            context.Register<ICargoHold>(Materials.Hold);
            context.Register<ICutter>(Cutter);
            context.Register<IRoverSystems>(StartKit);
            context.Register<ISalvageStatus>(_salvage);
            context.Register<IInteractionHints>(Hints);
            context.Register<IInteractionFocus>(hints);
            context.Register<IRoverBattery>(_battery.Model);
            context.Register<IBuildEnergy>(_battery);
            context.Register<IBeamStatus>(services.Beam);
            context.Register<IRecoveryProgress>(Recovery);
            context.Register<ICallSource>(Call);
            context.Register<IAnswerStatus>(Answer);
            services.Anchors.TryGet(WorldAnchorIds.SitePrefix + "kestrel", out WorldAnchor kestrel);
            Release = new ReleaseSequence(context.Events, Call, RangeRing, RangeRing.Show, _openingTuning.Answer,
                relay.Position + Vector3.up * _openingTuning.Answer.RelayLampHeight,
                services.Layout.PeakPosition + Vector3.up * _openingTuning.Answer.DishHeight, kestrel.Position);
            PulseView = new NetworkPulseView(transform, _visuals.BeamCore, services.Meshes.Sphere, _visuals.Flash,
                context.Events, _openingTuning.Answer);
            context.Register<IRangeRing>(RangeRing);
            context.Register<ITowLine>(Tow);
            context.Register<IDatabank>(Databank);
            context.Register<BuildField>(_building);
            RegisterSaveSections(save);
        }

        private CallSaveData CaptureCall()
        {
            CallSaveData data = Call.Capture();
            Reply.Capture(data);
            Release.Capture(data);
            return data;
        }

        private void RestoreCall(CallSaveData data)
        {
            Call.Restore(data);
            Reply.Restore(data);
            Release.Restore(data);
        }

        private void RegisterSaveSections(ISaveService save)
        {
            _saveTokens.Add(save.Register(new SaveSection<MaterialsSaveData>(GameplaySaveKeys.Materials,
                GameplaySaveKeys.MaterialsVersion, Materials.Capture, Materials.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<SalvageSaveData>(GameplaySaveKeys.Salvage,
                GameplaySaveKeys.SalvageVersion, _salvage.Capture, _salvage.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<BatterySaveData>(GameplaySaveKeys.Battery,
                GameplaySaveKeys.BatteryVersion, _battery.Capture, _battery.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<ContainersSaveData>(GameplaySaveKeys.Containers,
                GameplaySaveKeys.ContainersVersion, Containers.Capture, Containers.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<OpeningSaveData>(GameplaySaveKeys.Opening,
                GameplaySaveKeys.OpeningVersion, Opening.Capture, Opening.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<StartKitSaveData>(GameplaySaveKeys.StartKit,
                GameplaySaveKeys.StartKitVersion, StartKit.Capture, StartKit.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<ScansSaveData>(GameplaySaveKeys.Scans,
                GameplaySaveKeys.ScansVersion, Scans.Capture, Scans.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<DatabankSaveData>(GameplaySaveKeys.Databank,
                GameplaySaveKeys.DatabankVersion, Databank.Capture, Databank.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<CallSaveData>(GameplaySaveKeys.Call,
                GameplaySaveKeys.CallVersion, CaptureCall, RestoreCall)));
            _saveTokens.Add(save.Register(new SaveSection<TowSaveData>(GameplaySaveKeys.Tow,
                GameplaySaveKeys.TowVersion, Tow.Capture, Tow.Restore)));
            _saveTokens.Add(_building.RegisterSave(save));
        }

        /// <summary>
        /// The opening (docs/design/opening-v2.md), or null (logged) when its anchors, Art's crates or the sky are
        /// missing. Built pads charge 07 through it from now on.
        /// </summary>
        private OpeningField NewOpening(GameContext context, GameplayServices services)
        {
            if (!context.TryGet(out ISunlight sunlight))
            {
                Debug.LogError($"{nameof(GameplaySystem)}: the opening needs the World's {nameof(ISunlight)}.", this);
                return null;
            }

            try
            {
                var opening = new OpeningField(_openingTuning, services, _building, sunlight, Containers, _ruins,
                    _storyProps, _landerPort, transform, _salvage.Tuning.ReachRadius, StartKit);
                _battery.ConnectPads(opening);
                services.Anchors.TryGet(_openingTuning.ApronAnchor, out WorldAnchor apron);
                _building.SetApronCable(new ApronCable(_apronCableChannel.position, apron.Position,
                    _openingTuning.PortCableReach));
                PadLock = new PadLock(context.Events, _baseTuning, _building, _battery.Model, services.Rover,
                    _openingTuning.PadChargeRadius, _openingTuning.PadPlateMargin);
                BaseLamps = new BaseLamps(context.Events, _baseTuning, _building, sunlight);
                return opening;
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>
        /// The ruins' structure sets and story scans and the lander's cargo bay; null (logged) when a target is
        /// missing.
        /// </summary>
        private ScanField NewScans(GameplayServices services)
        {
            var scans = new ScanField(_scanTuning, services, KnownStructures, OtherVerbOnOffer);
            string problem = AddRuins(scans) ?? AddStoryProps(scans)
                ?? scans.AddRuin(_scanTuning.LanderCargo, Opening.LanderCargo);
            if (problem != null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: scanning: {problem} (opening-v2 §1).", this);
                return null;
            }

            return scans;
        }

        private string AddRuins(ScanField scans)
        {
            for (int i = 0; i < _ruins.Length; i++)
            {
                Transform ruin = _ruins[i];
                if (ruin == null)
                {
                    return "a ruin transform in _ruins is null";
                }

                if (_scanTuning.TryGetRuinSet(ruin.name, out RuinScanSet set))
                {
                    string problem = scans.AddRuin(set, ruin);
                    if (problem != null)
                    {
                        return problem;
                    }
                }

                foreach (StoryScan story in _scanTuning.Stories)
                {
                    string problem = story.HostId == ruin.name ? scans.AddStory(story, ruin) : null;
                    if (problem != null)
                    {
                        return problem;
                    }
                }
            }

            return null;
        }

        /// <summary>The story scans on World's basin props (basin dressing §2: the Scout).</summary>
        private string AddStoryProps(ScanField scans)
        {
            for (int i = 0; i < _storyProps.Length; i++)
            {
                Transform prop = _storyProps[i];
                if (prop == null)
                {
                    return "a story prop transform in _storyProps is null";
                }

                foreach (StoryScan story in _scanTuning.Stories)
                {
                    string problem = story.HostId == prop.name ? scans.AddStory(story, prop) : null;
                    if (problem != null)
                    {
                        return problem;
                    }
                }
            }

            return null;
        }

        /// <summary>The lander's cargo has taught what it holds: its crate may give it up (B3 before B4).</summary>
        private bool CargoScanned()
        {
            IReadOnlyList<string> taught = _scanTuning.LanderCargo.StructureIds;
            for (int i = 0; i < taught.Count; i++)
            {
                if (!KnownStructures.IsKnown(taught[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Another E verb is on offer: a scan waits for it (0.5.5 §1.2), unless the player looks at a scan target more
        /// squarely than at that verb's.
        /// </summary>
        private bool OtherVerbOnOffer()
        {
            for (int i = 0; i < VerbsBeforeScan.Length; i++)
            {
                if (Hints.TryGet(VerbsBeforeScan[i], out InteractionHint hint) && !ScanComesFirst(hint.Position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The player looks at a scan target more squarely than at <paramref name="otherTarget"/> (or that is not in
        /// view at all): the scan takes E, so a crate beside 07 never silences the panel in front of it.
        /// </summary>
        private bool ScanComesFirst(Vector3 otherTarget)
        {
            if (Scans == null || Scans.LookedAt(out float scanScore) == null)
            {
                return false;
            }

            float margin = Scans.Candidate != null ? -Scans.Tuning.LookStickiness : Scans.Tuning.LookStickiness;
            return !Scans.InView(otherTarget, out float otherScore) || scanScore + margin < otherScore;
        }

        /// <summary>07 starts a charge: the next full hold says so once more.</summary>
        private void OnChargingChanged(ChargingChanged changed)
        {
            if (changed.Charging)
            {
                Bundles.NewTrip();
            }
        }

        /// <summary>The Power row's lit lamps: one per built step of the base's power.</summary>
        private int PowerLamps()
        {
            return (Opening.Built(StructureIds.ChargingPad) ? 1 : 0) + (Opening.PowerOn ? 1 : 0) +
                   (Opening.NightReady ? 1 : 0);
        }

        /// <summary>
        /// The save is loaded (Start runs after it): the readout, the recovery protocol and the opening start from it
        /// at once.
        /// </summary>
        private void Start()
        {
            StartKit.Announce();
            Recovery.Announce();
            Call.Announce();
            Release.Announce();
            Opening.Announce();
        }

        private void Update()
        {
            float now = Time.time;
            float deltaTime = Time.deltaTime;
            Cutter.Step();
            Bundles.Step(now);
            Containers.Step(now, deltaTime);
            Scans.Step(now, deltaTime);
            ScanMark.Step(Scans.Scanning == null ? Scans.Candidate : null, deltaTime);
            Opening.Step(deltaTime);
            Tow.Step(_input.TetherPressed);
            Reply.Step(deltaTime, Opening.NightFallen, _roverState.Speed);
            MastRaise.Step(deltaTime);
            MastRaiseView.Step();
            PulseView.Step(deltaTime);
            Release.Step(deltaTime);
            Answer.Step(deltaTime, Opening.NightFallen, _input.ExcavateHeld, _roverState.Position, _roverState.Speed);
            RangeRing.Step(deltaTime);
            RangeRingLine.Step();
            PadLock.Step(deltaTime);
            BaseLamps.Step(deltaTime);
            ApronGlow.Step(_building.State != BuildModeState.Closed, deltaTime);
            ApronGuide.Step(_building.State != BuildModeState.Closed && !AnyPadStands(), deltaTime);
            Recovery.Refresh();
        }

        private void FixedUpdate()
        {
            Tow?.FixedStep(Time.fixedDeltaTime);
        }

        /// <summary>
        /// 07's winch cable, with home at the apron, and the opening's towable loads stood on it; null (logged) without
        /// the winch, the apron or Art's loads.
        /// </summary>
        private TowLine NewTow(GameplayServices services)
        {
            Transform winch = services.Rig != null ? services.Rig.WinchOrigin : null;
            if (winch == null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: the tow needs IRoverRig.WinchOrigin (07's winch).", this);
                return null;
            }

            if (!services.Anchors.TryGet(_openingTuning.ApronAnchor, out WorldAnchor apron))
            {
                Debug.LogError($"{nameof(GameplaySystem)}: the tow's home is world anchor " +
                               $"'{_openingTuning.ApronAnchor}', which the World does not publish.", this);
                return null;
            }

            var tow = new TowLine(services.Events, _openingTuning.Tow, winch, services.Rover, apron.Position);
            Transform loads = new GameObject("TowLoads").transform;
            loads.SetParent(transform, false);
            try
            {
                foreach (TowLoadSpot spot in _openingTuning.TowLoads)
                {
                    tow.Add(TowLoads.Stand(spot, Opening.FrameOf(spot.AnchorId), loads, services.Terrain,
                        _openingTuning.Tow));
                }
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }

            return tow;
        }

        /// <summary>
        /// The domain cannot run (logged): it and its parts stand still, so nothing half-wired steps.
        /// </summary>
        private void Fail(string problem)
        {
            Debug.LogError($"{nameof(GameplaySystem)}: {problem}", this);
            enabled = false;
            if (_salvage != null)
            {
                _salvage.enabled = false;
            }

            if (_battery != null)
            {
                _battery.enabled = false;
            }

            if (_building != null)
            {
                _building.enabled = false;
            }
        }

        private string WiringProblem()
        {
            if (_visuals == null)
            {
                return "GameplayVisuals is not assigned (run Gameplay/Materials).";
            }

            string visuals = _visuals.Validate();
            if (visuals != null)
            {
                return "GameplayVisuals: " + visuals;
            }

            return _glints == null ? "GlintTuning is not assigned."
                : _materialCatalog == null ? "MaterialCatalog is not assigned (run Gameplay/Content)."
                : _materialCatalog.Validate() != null ? "MaterialCatalog: " + _materialCatalog.Validate() + "."
                : _cargo == null ? "CargoTuning is not assigned."
                : _cutterTuning == null ? "CutterTuning is not assigned."
                : _scanTuning == null ? "ScanTuning is not assigned."
                : _databankCatalogue == null ? "DatabankCatalogue is not assigned (run Gameplay/Content)."
                : _baseTuning == null ? "BaseTuning is not assigned."
                : _openingTuning == null ? "OpeningTuning is not assigned."
                : _openingTuning.Call.Validate() != null ? "OpeningTuning: " + _openingTuning.Call.Validate() + "."
                : _salvage == null ? "SalvageField is not assigned."
                : _battery == null ? "BatteryField is not assigned."
                : _building == null ? "BuildField is not assigned."
                : _landerPort == null ? "The lander's service port is not assigned (rebuild the Main scene)."
                : _apronOutline == null ? "The apron's bay outline is not assigned (rebuild the Main scene)."
                : _apronCableChannel == null
                    ? "The apron's cable channel is not assigned (rebuild the Main scene)."
                : null;
        }

        /// <summary>A charging pad is placed (even unwelded): the first pad has found its home.</summary>
        private bool AnyPadStands()
        {
            PlacedBase placed = PlacedBase;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed.StructureAt(i).Definition.Id == StructureIds.ChargingPad)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            ApronGuide?.Dispose();
            ScanMark?.Dispose();
            foreach (IDisposable token in _saveTokens)
            {
                token.Dispose();
            }

            BaseLamps?.Dispose();
            _saveTokens.Clear();
            foreach (IDisposable token in _flowTokens)
            {
                token.Dispose();
            }

            _flowTokens.Clear();
            Databank?.Dispose();
            StartKit?.Dispose();
            Call?.Dispose();
            MastRaise?.Dispose();
            MastRaiseView?.Dispose();
            Answer?.Dispose();
            PulseView?.Dispose();
            Release?.Dispose();
            RangeRing?.Dispose();
            Bundles?.Dispose();
            Opening?.Dispose();
            _meshes?.Dispose();
        }
    }
}
