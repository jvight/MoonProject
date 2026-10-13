using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Gameplay domain's single entry in the bootstrap's system list (after World and Rover). Resolves the world
    /// (surface, layout, anchors), rover and camera services, creates the material stock, the upgrade service (which
    /// grants rover abilities through <see cref="IRoverAbilities"/>) and the radio program, initialises the gameplay
    /// parts in dependency order (salvage sites, the relics in their hearts, excavation, tether, home, the Cargo
    /// Cradle, radio tower, workshop, friends, cassettes, log caches, sonar, Bell's signals, the cassette shelf, the
    /// relay network),
    /// registers the services other domains read (<see cref="IMaterialStock"/>, <see cref="ISalvageStatus"/>,
    /// <see cref="ITetherAim"/>, <see cref="IUpgradeShop"/>, <see cref="IInteractionHints"/>,
    /// <see cref="IFriendRoster"/>, <see cref="IFriendStatuses"/>, <see cref="IRadioProgram"/>,
    /// <see cref="IStationReach"/>, <see cref="IRadioHop"/>, <see cref="IRelayStatus"/>, the workshop as
    /// <see cref="IRoverBay"/>, 07's battery as <see cref="IRoverBattery"/> and its beam work as
    /// <see cref="IBeamStatus"/>, the tech tree as <see cref="ITechTree"/> and the recovery panel's rows as
    /// <see cref="IRecoveryProgress"/>, docs/features/M3-21) and the save sections,
    /// announces the radio's signal radius and, once the save is loaded, the radio program, and owns the shared glow
    /// meshes. The radio-hop moves 07 through Core's <see cref="IRoverPlacement"/> when the Rover domain registers it;
    /// Core's <see cref="IRoverCargoSeat"/> (where a relic rides in the Cargo Cradle) is required at boot.
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

        [Tooltip("07's hold and home's shelves (Assets/_Project/Data/Tuning/Gameplay/CargoTuning.asset).")]
        [SerializeField] private CargoTuning _cargo;

        [Tooltip("07's cutter tiers (Assets/_Project/Data/Tuning/Gameplay/CutterTuning.asset).")]
        [SerializeField] private CutterTuning _cutterTuning;

        [Tooltip("Every upgrade crafted from materials (Assets/_Project/Data/Content/Upgrades).")]
        [SerializeField] private UpgradeDefinition[] _upgradeDefinitions = Array.Empty<UpgradeDefinition>();

        [SerializeField] private SalvageField _salvage;
        [SerializeField] private RelicField _relics;
        [SerializeField] private SonarSystem _sonar;
        [SerializeField] private ExcavationSystem _excavation;
        [SerializeField] private TetherSystem _tether;
        [SerializeField] private HomeBase _home;
        [SerializeField] private CargoCradle _cradle;
        [SerializeField] private RadioTower _tower;
        [SerializeField] private Workshop _workshop;
        [SerializeField] private FriendField _friends;
        [SerializeField] private CassetteField _cassettes;
        [SerializeField] private LogCacheField _logs;
        [SerializeField] private BellSignalField _signals;
        [SerializeField] private CassetteShelf _shelf;
        [SerializeField] private RelayField _relays;
        [SerializeField] private BaseProgression _progression;
        [SerializeField] private BlueprintPlotter _plotter;
        [SerializeField] private LiftStation _lift;
        [SerializeField] private BatteryField _battery;

        private readonly List<IDisposable> _saveTokens = new List<IDisposable>();
        private readonly List<IDisposable> _flowTokens = new List<IDisposable>();
        private GlowMeshSet _meshes;
        private IRoverAbilities _abilities;
        private IRoverRig _rig;

        public MaterialStock Materials { get; private set; }

        /// <summary>Cargo and storage tuning (07's spares, the hold, home's shelves).</summary>
        public CargoTuning Cargo => _cargo;

        /// <summary>07's cutter as its fitted kits make it (docs/features/M3-26).</summary>
        public Cutter Cutter { get; private set; }

        /// <summary>Bundles waiting in the world because 07's hold was full (docs/features/M3-25 §2.3).</summary>
        public CargoBundles Bundles { get; private set; }

        /// <summary>The dock's unload onto home's shelves (M3-25 §3.2).</summary>
        public DockUnload Unload { get; private set; }

        /// <summary>Tilly fetching waiting bundles inside the station's reach (M3-25 T3).</summary>
        public TillyFetch Fetch { get; private set; }

        /// <summary>Home's supply shelves as they stand, and building more (M3-25 §3.1).</summary>
        public HomeShelves Shelves { get; private set; }

        /// <summary>The crew's supply crates, one beside each site's heart (M3-25 T4).</summary>
        public CrewCrates Crates { get; private set; }

        public UpgradeService Upgrades { get; private set; }

        public IUpgradeShop Shop { get; private set; }

        public IInteractionHints Hints { get; private set; }

        public RadioProgram Radio { get; private set; }

        /// <summary>How far power has come back through the base (docs/features/M3-16).</summary>
        public BasePower Power { get; private set; }

        /// <summary>Kenji's blueprint board: which kits the bay can build (docs/features/M3-16).</summary>
        public Blueprints Blueprints { get; private set; }

        /// <summary>The tower's components, one per level (docs/features/M3-16 P1).</summary>
        public TowerParts TowerParts { get; private set; }

        /// <summary>The components in the world: waiting, flying into 07, fed to the tower's port.</summary>
        public TowerPartField TowerPartField { get; private set; }

        /// <summary>The blueprint tubes waiting on the ground at world anchors (docs/features/M3-21).</summary>
        public GroundTubes GroundTubes { get; private set; }

        /// <summary>Kenji's garage service port: the tether ring, then the tape deck (first-hour v2 §3).</summary>
        public GaragePort Port { get; private set; }

        /// <summary>The recipe 07's view rests on: the tower's hopper or a board sheet (docs/features/M3-24 B).</summary>
        public RecipeLook RecipeLook { get; private set; }

        /// <summary>What 07 and the station know and have built (docs/features/M3-21).</summary>
        public TechTree Tech { get; private set; }

        /// <summary>The four recovery targets' lamps (docs/features/M3-21).</summary>
        public RecoveryProgress Recovery { get; private set; }

        public SalvageField Salvage => _salvage;

        public RelicField Relics => _relics;

        public SonarSystem Sonar => _sonar;

        public ExcavationSystem Excavation => _excavation;

        public TetherSystem Tether => _tether;

        public HomeBase Home => _home;

        public CargoCradle Cradle => _cradle;

        public RadioTower Tower => _tower;

        public Workshop Workshop => _workshop;

        public FriendField Friends => _friends;

        public CassetteField Cassettes => _cassettes;

        public LogCacheField Logs => _logs;

        public BellSignalField Signals => _signals;

        public CassetteShelf Shelf => _shelf;

        public RelayField Relays => _relays;

        public BaseProgression Progression => _progression;

        public BlueprintPlotter Plotter => _plotter;

        public LiftStation Lift => _lift;

        /// <summary>07's battery: drains, chargers, the crawl and the warnings (docs/features/M3-20).</summary>
        public BatteryField Battery => _battery;

        internal void Wire(GameplayVisuals visuals, GlintTuning glints, UpgradeDefinition[] upgradeDefinitions,
            SalvageField salvage, RelicField relics, SonarSystem sonar, ExcavationSystem excavation,
            TetherSystem tether, HomeBase home, CargoCradle cradle, RadioTower tower, Workshop workshop,
            FriendField friends, CassetteField cassettes, LogCacheField logs, BellSignalField signals,
            CassetteShelf shelf, RelayField relays, BaseProgression progression, BlueprintPlotter plotter,
            LiftStation lift)
        {
            _visuals = visuals;
            _glints = glints;
            _upgradeDefinitions = upgradeDefinitions;
            _salvage = salvage;
            _relics = relics;
            _sonar = sonar;
            _excavation = excavation;
            _tether = tether;
            _home = home;
            _cradle = cradle;
            _tower = tower;
            _workshop = workshop;
            _friends = friends;
            _cassettes = cassettes;
            _logs = logs;
            _signals = signals;
            _shelf = shelf;
            _relays = relays;
            _progression = progression;
            _plotter = plotter;
            _lift = lift;
        }

        internal void WireCutter(CutterTuning cutter)
        {
            _cutterTuning = cutter;
        }

        internal void WireBattery(BatteryField battery)
        {
            _battery = battery;
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
                Debug.LogError($"{nameof(GameplaySystem)}: {problem}", this);
                enabled = false;
                return;
            }

            if (!context.TryGet(out IRoverCargoSeat seat))
            {
                Debug.LogError($"{nameof(GameplaySystem)}: no {nameof(IRoverCargoSeat)} is registered. The Rover " +
                               "domain registers 07's cargo seat for the Cargo Cradle (docs/features/M3-11).", this);
                enabled = false;
                return;
            }

            if (!context.TryGet(out IHomeLights lights))
            {
                Debug.LogError($"{nameof(GameplaySystem)}: no {nameof(IHomeLights)} is registered. The World domain " +
                               "registers home's warm points (docs/features/M3-16).", this);
                enabled = false;
                return;
            }

            _meshes = new GlowMeshSet();
            var hold = new CargoHold(context.Events, _materialCatalog, _cargo.HoldSlots);
            var storage = new HomeStorage(context.Events, _materialCatalog, _cargo.ShelfLayout(),
                _cargo.BuiltInShelves);
            Materials = new MaterialStock(context.Events, hold, storage);
            Materials.Stow(_cargo.StarterItems);
            var save = context.Get<ISaveService>();
            var services = new GameplayServices(context.Events, context.Input, context.Get<ITerrainQuery>(),
                context.Get<IWorldLayout>(), context.Get<IWorldAnchors>(), context.Get<IRoverState>(),
                context.Get<IRoverRig>(), context.Get<IViewCamera>(), save, Materials, _visuals, _glints, _meshes);
            GlintVeil veil = services.GlintVeil;
            _flowTokens.Add(context.Events.Subscribe<TitleShown>(_ => veil.Hide()));
            _flowTokens.Add(context.Events.Subscribe<SessionStarted>(_ => veil.Show(Time.time)));
            var abilities = context.Get<IRoverAbilities>();
            _abilities = abilities;
            Upgrades = new UpgradeService(context.Events, Materials, abilities, _upgradeDefinitions);
            Radio = new RadioProgram(context.Events, _cassettes.Catalog.Ids());
            Power = new BasePower(context.Events);
            Blueprints = NewBlueprints(context.Events);
            TowerParts = NewTowerParts(context.Events);
            if (Blueprints == null || TowerParts == null)
            {
                enabled = false;
                return;
            }

            Vector3 lander = HomeBase.LanderSpot(services.Terrain, services.Layout.BasePosition, _home.Tuning);
            Cutter = new Cutter(context.Events, abilities, _cutterTuning);
            if (!_salvage.Initialize(services, Cutter) || !_relics.Initialize(services, _salvage) ||
                !_excavation.Initialize(services, _relics, _salvage) ||
                !_tether.Initialize(services, _relics, _salvage, abilities) ||
                !_home.Initialize(services, _relics, Upgrades, lights, _lift) ||
                !_cradle.Initialize(services, seat, _relics, _home) ||
                !_lift.Initialize(services, Power, context.Get<IRoverStillness>(), _tether, _cradle, Blueprints,
                    _progression.Tuning) ||
                !_tower.Initialize(services, Upgrades, _salvage.Catalog, TowerParts) ||
                !_workshop.Initialize(services, Upgrades, _salvage.Catalog, _tower, Power, Blueprints, lights) ||
                !_friends.Initialize(services, Radio, _cassettes.Catalog, _relics, _salvage, _home) ||
                !_cassettes.Initialize(services, Radio, KeepClearOfCassettes()) || !_logs.Initialize(services) ||
                !_sonar.Initialize(services, _relics, _friends, _salvage, abilities, Power) ||
                !_signals.Initialize(services, _friends, _cassettes, _logs, _relics, abilities, _sonar.Tuning) ||
                !_shelf.Initialize(Radio, _cassettes.Catalog, _friends.BellTuning) ||
                !_relays.Initialize(services, Upgrades, _tower, _tether, _friends.Tuning, Placement(context),
                    _home.Dock.Position) ||
                !_progression.Initialize(services, Power, _home, _workshop, _sonar, _friends, _relays,
                    Upgrades, _lift, _tower, TowerParts, () => _battery.Model.Charge01) ||
                !_plotter.Initialize(services, Blueprints, _workshop, _progression.Tuning, Upgrades) ||
                !_battery.Initialize(services, _relays.Reach, _tether, _home, abilities, _friends, _sonar.Tuning))

            {
                enabled = false;
                return;
            }

            var stations = new IUpgradeStation[] { _tower, _workshop };
            string unsold = UnsoldUpgrade(stations);
            if (unsold != null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: no station sells upgrade '{unsold}'.", this);
                enabled = false;
                return;
            }

            _friends.Connect(_sonar);
            _salvage.Connect(_excavation);
            _tether.Connect(_cradle, _home);

            string tubeSite = UnknownTubeSite();
            if (tubeSite != null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: a blueprint tube lies at '{tubeSite}', which is no " +
                               "salvage site or relay mast.", this);
                enabled = false;
                return;
            }

            if (!NewCargo(services))
            {
                enabled = false;
                return;
            }

            context.Register<Map.IWaitingBundles>(Bundles);

            TowerPartField = NewTowerPartField(services);
            GroundTubes = TowerPartField != null ? NewGroundTubes(services) : null;
            Port = GroundTubes != null ? NewPort(services, abilities) : null;
            if (Port == null || !NewGoals(context.Events))
            {
                enabled = false;
                return;
            }

            _progression.ConnectFinds(Port, TowerPartField);

            Shop = new UpgradeShop(Upgrades, stations, save, Blueprints, TowerParts);
            RecipeLook = new RecipeLook(services.View, Shop, _tower, _plotter, Blueprints.Kits, _progression.Tuning);
            Hints = new InteractionHints(_excavation, _salvage, _tether, _cradle, _home,
                stations, Upgrades, TowerParts, _friends, _relays, services.Beam, services.Pickups, Shelves, Crates);
            context.Register<IMaterialStock>(Materials);
            context.Register<IMaterialCatalog>(_materialCatalog);
            context.Register<ICutter>(Cutter);
            context.Register<ICargoHold>(Materials.Hold);
            context.Register<IHomeStorage>(Materials.Home);
            context.Register<IDockState>(_home.Dock);
            context.Register<IStackTransfer>(new StackTransfer(Materials, _materialCatalog, _home.Dock, Unload));
            context.Register<ISalvageStatus>(_salvage);
            context.Register<ITetherAim>(_tether);
            context.Register<IUpgradeShop>(Shop);
            context.Register<IRecipeLook>(RecipeLook);
            context.Register<IInteractionHints>(Hints);
            context.Register<IFriendRoster>(_friends);
            context.Register<IRoverBay>(_workshop);
            context.Register<IFriendStatuses>(_friends);
            context.Register<IRadioProgram>(Radio);
            context.Register<IStationReach>(_relays.Reach);
            context.Register<IRadioHop>(_relays.Hop);
            context.Register<IRelayStatus>(_relays);
            context.Register<IBasePower>(Power);
            context.Register<ILabInterior>(_lift.Lab);
            context.Register<IBlueprintBoard>(Blueprints);
            context.Register<IComponentGate>(TowerParts);
            _home.Dock.Connect(_battery.Model);
            context.Register<IRoverBattery>(_battery.Model);
            context.Register<IChargerReach>(_battery);
            context.Register<IBeamStatus>(services.Beam);
            context.Register<ITechTree>(Tech);
            context.Register<IRecoveryProgress>(Recovery);
            Upgrades.PublishSignals();
            RegisterSaveSections(save);
        }

        private void RegisterSaveSections(ISaveService save)
        {
            _saveTokens.Add(save.Register(new SaveSection<MaterialsSaveData>(GameplaySaveKeys.Materials,
                GameplaySaveKeys.MaterialsVersion, CaptureMaterials, RestoreMaterials,
                MaterialsSaveMigrations.Migrate)));
            _saveTokens.Add(save.Register(new SaveSection<CratesSaveData>(GameplaySaveKeys.Crates,
                GameplaySaveKeys.CratesVersion, Crates.Capture, Crates.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<SalvageSaveData>(GameplaySaveKeys.Salvage,
                GameplaySaveKeys.SalvageVersion, _salvage.Capture, _salvage.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<RelicsSaveData>(GameplaySaveKeys.Relics,
                GameplaySaveKeys.RelicsVersion, _relics.Capture, RestoreRelics)));
            _saveTokens.Add(save.Register(new SaveSection<UpgradesSaveData>(GameplaySaveKeys.Upgrades,
                GameplaySaveKeys.UpgradesVersion, Upgrades.Capture, RestoreUpgrades)));
            _saveTokens.Add(save.Register(new SaveSection<FriendsSaveData>(GameplaySaveKeys.Friends,
                GameplaySaveKeys.FriendsVersion, _friends.Capture, _friends.Restore,
                FriendsSaveMigrations.Migrate)));
            _saveTokens.Add(save.Register(new SaveSection<RadioSaveData>(GameplaySaveKeys.Radio,
                GameplaySaveKeys.RadioVersion, Radio.Capture, RestoreRadio)));
            _saveTokens.Add(save.Register(new SaveSection<LogsSaveData>(GameplaySaveKeys.Logs,
                GameplaySaveKeys.LogsVersion, _logs.Capture, _logs.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<BellSignalSaveData>(GameplaySaveKeys.BellSignals,
                GameplaySaveKeys.BellSignalsVersion, _signals.Capture, _signals.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<RelaysSaveData>(GameplaySaveKeys.Relays,
                GameplaySaveKeys.RelaysVersion, _relays.Capture, _relays.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<ProgressionSaveData>(GameplaySaveKeys.Progression,
                GameplaySaveKeys.ProgressionVersion, _progression.Capture, _progression.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<BlueprintsSaveData>(GameplaySaveKeys.Blueprints,
                GameplaySaveKeys.BlueprintsVersion,
                Blueprints.Capture, RestoreBlueprints)));
            _saveTokens.Add(save.Register(new SaveSection<LiftSaveData>(GameplaySaveKeys.Lift,
                GameplaySaveKeys.LiftVersion, _lift.Capture, _lift.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<TowerPartsSaveData>(GameplaySaveKeys.TowerParts,
                GameplaySaveKeys.TowerPartsVersion, TowerParts.Capture, RestoreTowerParts)));
            _saveTokens.Add(save.Register(new SaveSection<BatterySaveData>(GameplaySaveKeys.Battery,
                GameplaySaveKeys.BatteryVersion, _battery.Capture, _battery.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<GaragePortSaveData>(GameplaySaveKeys.GaragePort,
                GameplaySaveKeys.GaragePortVersion, Port.Capture, Port.Restore)));
        }

        private void RestoreTowerParts(TowerPartsSaveData data)
        {
            TowerParts.Restore(data);
            TowerPartField.Sync();
        }

        /// <summary>The tower's components, or null (logged) when the content is broken.</summary>
        private TowerParts NewTowerParts(EventBus events)
        {
            try
            {
                return new TowerParts(events, _tower.Definition != null ? _tower.Definition.Id : null,
                    _progression.Tuning.TowerParts);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>The components in the world, or null (logged) when a part's model or site is missing.</summary>
        private TowerPartField NewTowerPartField(GameplayServices services)
        {
            try
            {
                return new TowerPartField(TowerParts, transform, services, _salvage, _relays.Reach, _tower,
                    _friends.Tuning, _progression.Tuning, () => _progression.ChasmSeen);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>The tubes waiting on the ground, or null (logged) when a tube's anchor is missing.</summary>
        private GroundTubes NewGroundTubes(GameplayServices services)
        {
            try
            {
                return new GroundTubes(Blueprints, Upgrades, _progression.Tuning.Tubes, transform, services,
                    _friends.Tuning, _progression.Tuning);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>Kenji's garage port, or null (logged) when its site, its relic or Art's port is missing.</summary>
        private GaragePort NewPort(GameplayServices services, IRoverAbilities abilities)
        {
            try
            {
                return new GaragePort(transform, services, _salvage, _relics, abilities, _progression.Tuning, _tether);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>The tech tree and the recovery rows; false (logged) when their content is broken.</summary>
        private bool NewGoals(EventBus events)
        {
            foreach (TechKit kit in _progression.Tuning.TechKits)
            {
                if (Upgrades.Find(kit.KitId) == null || !_workshop.Sells(Upgrades.Find(kit.KitId)))
                {
                    Debug.LogError($"{nameof(GameplaySystem)}: tech kit '{kit.KitId}' ({kit.Node}) is no kit the " +
                                   "bay fits.", this);
                    return false;
                }
            }

            try
            {
                Tech = new TechTree(events, () => _tower.ShownLevel, () => _relays.LitMasts, Blueprints.IsDecoded,
                    Upgrades.LevelOf, _progression.Tuning.TechKits);
                Recovery = new RecoveryProgress(events, () => _tower.ShownLevel, _tower.Definition.MaxLevel,
                    () => _relays.LitChainMasts, _relays.ChainMastCount, _progression.Tuning.RecoveryLamps);
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return false;
            }
        }

        private void RestoreBlueprints(BlueprintsSaveData data)
        {
            Blueprints.Restore(data);
            GroundTubes.Sync();
            _lift.RestoreReader();
            _plotter.SyncSheets();
        }

        /// <summary>The board over the bay's kits, or null (logged) when its tubes are miswired.</summary>
        private Blueprints NewBlueprints(EventBus events)
        {
            var kits = new List<string>();
            foreach (UpgradeDefinition definition in _upgradeDefinitions)
            {
                if (_workshop.Sells(definition))
                {
                    kits.Add(definition.Id);
                }
            }

            try
            {
                return new Blueprints(events, kits, _progression.Tuning.Tubes, SiteOfRelic);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {exception.Message}", this);
                return null;
            }
        }

        /// <summary>The salvage site whose heart held relic <paramref name="relicId"/>, or null.</summary>
        private string SiteOfRelic(string relicId)
        {
            foreach (Relic relic in _relics.Relics)
            {
                if (relic.Definition.Id == relicId)
                {
                    return relic.Home != null ? relic.Home.Id : null;
                }
            }

            return null;
        }

        private string UnknownTubeSite()
        {
            foreach (BlueprintTube tube in _progression.Tuning.Tubes)
            {
                if (tube.IsAtAnchor)
                {
                    continue;
                }

                bool found = _relays.Reach.IndexOf(tube.SiteId) > StationReach.Home;
                foreach (SalvageSite site in _salvage.Sites)
                {
                    found |= site.Id == tube.SiteId;
                }

                if (!found)
                {
                    return tube.SiteId;
                }
            }

            return null;
        }

        /// <summary>Core's rover placement for the radio-hop, or null while no domain registers it.</summary>
        private static IRoverPlacement Placement(GameContext context)
        {
            return context.TryGet(out IRoverPlacement placement) ? placement : null;
        }

        /// <summary>
        /// The save is loaded (Start runs after it): Audio and UI start from the real radio program, Rover puts the
        /// carried blueprint tubes and components on 07's back, and the tech board, the porthole and the recovery
        /// panel show what is known at once.
        /// </summary>
        private void Start()
        {
            Radio.Announce();
            Power.Announce();
            Blueprints.Announce();
            TowerParts.Announce();
            GroundTubes.Sync();
            Port.Announce();
            Tech.Announce();
            Recovery.Announce();
        }

        private void Update()
        {
            float now = Time.time;
            Power.Show(_tower.PoweredLevel);
            Materials.Home.SetPowered(_tower.PoweredLevel > 0);
            int holdSlots = HoldSlots();
            if (holdSlots > Materials.Hold.SlotCount)
            {
                Materials.Hold.SetSlotCount(holdSlots);
            }

            Cutter.Step();
            Bundles.Step(now, Time.deltaTime);
            Unload.Step(_home.Dock.Docked, _rig.CargoSocket.position, now);
            if (Unload.DockedNow)
            {
                Bundles.NewTrip();
            }

            Fetch.Step(now);
            Shelves.Step(now, Time.deltaTime);
            Crates.Step(now, Time.deltaTime);
            TowerPartField.Step(now);
            GroundTubes.Step(now);
            Port.Step(Time.deltaTime);
            RecipeLook.Step();
            Tech.Refresh();
            Recovery.Refresh();
        }

        /// <summary>Where every material is, the bundles waiting in the world included (M3-25 §6).</summary>
        private MaterialsSaveData CaptureMaterials()
        {
            MaterialsSaveData data = Materials.Capture();
            data.waiting = Bundles.Capture();
            return data;
        }

        private void RestoreMaterials(MaterialsSaveData data)
        {
            Materials.Restore(data);
            Bundles.Restore(data.waiting);
        }

        /// <summary>
        /// The world side of cargo (M3-25): bundles that wait, the dock's unload and Tilly's fetch to home's shelves
        /// beside the lift. False (logged) when the fetching friend is missing.
        /// </summary>
        private bool NewCargo(GameplayServices services)
        {
            Friend fetcher = _friends.Find(_cargo.FetchFriend);
            if (fetcher == null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: no friend '{_cargo.FetchFriend}' to fetch waiting bundles " +
                               "(CargoTuning's fetch friend).", this);
                return false;
            }

            Bundles = new CargoBundles(transform, services, _salvage.Catalog, _materialCatalog, _cargo,
                _friends.Tuning);
            Unload = new DockUnload(services.Events, Materials, _cargo, services.Save);
            LiftStation lift = _lift;
            Fetch = new TillyFetch(Bundles, _relays.Reach, fetcher, _cargo, () => lift.RideSpot);
            Crates = new CrewCrates(_salvage, _cargo, Cutter, services);
            Transform[] racks = _home.ShelfRacks;
            int seats = _cargo.ShelfSlots * HomeShelves.Seats;
            var rackSeats = new Transform[racks.Length][];
            for (int i = 0; i < racks.Length; i++)
            {
                rackSeats[i] = _home.ShelfSeats(i, seats);
            }

            try
            {
                Shelves = new HomeShelves(Materials, _cargo, services.Events, services.Input, services.Rover,
                    services.Save, racks, _home.ShelfLamps, rackSeats, _home.ShelfCrates);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: home's supply shelves are not wired: {exception.Message} " +
                               "(Art's Lander/Shelves).", this);
                return false;
            }
            _rig = services.Rig;
            return true;
        }

        /// <summary>07's hold as its fitted kits make it (M3-25 §2.2): kits only ever add slots.</summary>
        private int HoldSlots()
        {
            return _cargo.HoldSlots + (_abilities.Has(RoverAbility.CargoCradle) ? _cargo.CradleSlots : 0) +
                   (_abilities.Has(RoverAbility.SideBins) ? _cargo.SideBinSlots : 0);
        }

        private void RestoreRadio(RadioSaveData data)
        {
            Radio.Restore(data);
            _cassettes.SyncCollected();
            _shelf.Sync();
        }

        /// <summary>Every salvage site, friend site and friend part: a basin cassette keeps clear of them.</summary>
        private List<Vector3> KeepClearOfCassettes()
        {
            var points = new List<Vector3>();
            foreach (SalvageSite site in _salvage.Sites)
            {
                points.Add(site.Position);
            }

            foreach (Friend friend in _friends.Friends)
            {
                points.Add(friend.Site.Position);
                points.AddRange(friend.Site.Parts);
            }

            return points;
        }

        private void RestoreRelics(RelicsSaveData data)
        {
            _relics.Restore(data);
            _cradle.AdoptRestored();
            _home.SyncDisplays();
            _sonar.RefreshDiscoveredSites();
        }

        private void RestoreUpgrades(UpgradesSaveData data)
        {
            Upgrades.Restore(data);
            _tower.ShowLevel(Upgrades.LevelOf(_tower.Definition.Id));
            Power.Restore(_tower.ShownLevel);
        }

        /// <summary>The id of an upgrade no station sells (it could never be bought), or null.</summary>
        private string UnsoldUpgrade(IUpgradeStation[] stations)
        {
            foreach (UpgradeDefinition definition in _upgradeDefinitions)
            {
                bool sold = false;
                foreach (IUpgradeStation station in stations)
                {
                    sold |= station.Sells(definition);
                }

                if (!sold)
                {
                    return definition.Id;
                }
            }

            return null;
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
                : _upgradeDefinitions == null || _upgradeDefinitions.Length == 0 ? "no upgrade definitions."
                : _salvage == null ? "SalvageField is not assigned."
                : _relics == null ? "RelicField is not assigned."
                : _sonar == null ? "SonarSystem is not assigned."
                : _excavation == null ? "ExcavationSystem is not assigned."
                : _tether == null ? "TetherSystem is not assigned."
                : _home == null ? "HomeBase is not assigned."
                : _cradle == null ? "CargoCradle is not assigned."
                : _home.Tuning == null ? "HomeBase has no BaseTuning."
                : _tower == null ? "RadioTower is not assigned."
                : _workshop == null ? "Workshop is not assigned."
                : _friends == null ? "FriendField is not assigned."
                : _cassettes == null ? "CassetteField is not assigned."
                : _cassettes.Catalog == null ? "CassetteField has no CassetteCatalog."
                : _cassettes.Catalog.Validate() != null ? "CassetteCatalog " + _cassettes.Catalog.Validate() + "."
                : _logs == null ? "LogCacheField is not assigned."
                : _signals == null ? "BellSignalField is not assigned."
                : _shelf == null ? "CassetteShelf is not assigned."
                : _relays == null ? "RelayField is not assigned."
                : _progression == null ? "BaseProgression is not assigned."
                : _progression.Tuning == null ? "BaseProgression has no ProgressionTuning."
                : _plotter == null ? "BlueprintPlotter is not assigned."
                : _battery == null ? "BatteryField is not assigned."
                : null;
        }

        private void OnDestroy()
        {
            foreach (IDisposable token in _saveTokens)
            {
                token.Dispose();
            }

            _saveTokens.Clear();
            foreach (IDisposable token in _flowTokens)
            {
                token.Dispose();
            }

            _flowTokens.Clear();
            Blueprints?.Dispose();
            TowerPartField?.Dispose();
            GroundTubes?.Dispose();
            Bundles?.Dispose();
            Shelves?.Dispose();
            Port?.Dispose();
            _meshes?.Dispose();
        }
    }
}
