using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

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
    /// <see cref="IRoverBay"/>) and the save sections,
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

        private readonly List<IDisposable> _saveTokens = new List<IDisposable>();
        private GlowMeshSet _meshes;

        public MaterialStock Materials { get; private set; }

        public UpgradeService Upgrades { get; private set; }

        public IUpgradeShop Shop { get; private set; }

        public IInteractionHints Hints { get; private set; }

        public RadioProgram Radio { get; private set; }

        /// <summary>How far power has come back through the base (docs/features/M3-16).</summary>
        public BasePower Power { get; private set; }

        /// <summary>Kenji's blueprint board: which kits the bay can build (docs/features/M3-16).</summary>
        public Blueprints Blueprints { get; private set; }

        /// <summary>The tower's story parts, one per level (docs/features/M3-16 P1).</summary>
        public TowerParts TowerParts { get; private set; }

        /// <summary>The story parts in the world: waiting, flying into 07, fed to the tower's port.</summary>
        public TowerPartField TowerPartField { get; private set; }

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

        internal void Wire(GameplayVisuals visuals, GlintTuning glints, UpgradeDefinition[] upgradeDefinitions,
            SalvageField salvage, RelicField relics, SonarSystem sonar, ExcavationSystem excavation,
            TetherSystem tether, HomeBase home, CargoCradle cradle, RadioTower tower, Workshop workshop,
            FriendField friends, CassetteField cassettes, LogCacheField logs, BellSignalField signals,
            CassetteShelf shelf, RelayField relays, BaseProgression progression, BlueprintPlotter plotter)
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
            Materials = new MaterialStock(context.Events);
            var save = context.Get<ISaveService>();
            var services = new GameplayServices(context.Events, context.Input, context.Get<ITerrainQuery>(),
                context.Get<IWorldLayout>(), context.Get<IWorldAnchors>(), context.Get<IRoverState>(),
                context.Get<IRoverRig>(), context.Get<IViewCamera>(), save, Materials, _visuals, _glints, _meshes);
            var abilities = context.Get<IRoverAbilities>();
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
            if (!_salvage.Initialize(services) || !_relics.Initialize(services, _salvage) ||
                !_excavation.Initialize(services, _relics, _salvage) ||
                !_tether.Initialize(services, _relics, _salvage) ||
                !_home.Initialize(services, _relics, _tether, Upgrades, lights) ||
                !_cradle.Initialize(services, seat, _relics, _home) ||
                !_tower.Initialize(services, Upgrades, _salvage.Catalog, TowerParts) ||
                !_workshop.Initialize(services, Upgrades, _salvage.Catalog, _tower, Power, Blueprints, lights) ||
                !_friends.Initialize(services, Radio, _cassettes.Catalog, _relics, _salvage, _home) ||
                !_cassettes.Initialize(services, Radio, KeepClearOfCassettes()) || !_logs.Initialize(services) ||
                !_sonar.Initialize(services, _relics, _friends, _salvage, abilities) ||
                !_signals.Initialize(services, _friends, _cassettes, _logs, _relics, abilities, _sonar.Tuning) ||
                !_shelf.Initialize(Radio, _cassettes.Catalog, _friends.BellTuning) ||
                !_relays.Initialize(services, Upgrades, _tower, _tether, _friends.Tuning, Placement(context)) ||
                !_progression.Initialize(services, Power, _home, _workshop, _sonar, _friends, _relays,
                    Upgrades) ||
                !_plotter.Initialize(services, Blueprints, _workshop, _progression.Tuning, Upgrades))
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
            _tether.Connect(_cradle);

            string tubeSite = UnknownTubeSite();
            if (tubeSite != null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: a blueprint tube lies at '{tubeSite}', which is no " +
                               "salvage site.", this);
                enabled = false;
                return;
            }

            TowerPartField = NewTowerPartField(services);
            if (TowerPartField == null)
            {
                enabled = false;
                return;
            }

            Shop = new UpgradeShop(Upgrades, stations, save, Blueprints, TowerParts);
            Hints = new InteractionHints(services.Rover, _sonar, _excavation, _salvage, _tether, _cradle, _home,
                stations, Upgrades, TowerParts, _friends, _relays);
            context.Register<IMaterialStock>(Materials);
            context.Register<ISalvageStatus>(_salvage);
            context.Register<ITetherAim>(_tether);
            context.Register<IUpgradeShop>(Shop);
            context.Register<IInteractionHints>(Hints);
            context.Register<IFriendRoster>(_friends);
            context.Register<IRoverBay>(_workshop);
            context.Register<IFriendStatuses>(_friends);
            context.Register<IRadioProgram>(Radio);
            context.Register<IStationReach>(_relays.Reach);
            context.Register<IRadioHop>(_relays.Hop);
            context.Register<IRelayStatus>(_relays);
            context.Register<IBasePower>(Power);
            context.Register<IBlueprintBoard>(Blueprints);
            context.Register<IStoryPartGate>(TowerParts);
            Upgrades.PublishSignals();
            RegisterSaveSections(save);
        }

        private void RegisterSaveSections(ISaveService save)
        {
            _saveTokens.Add(save.Register(new SaveSection<MaterialsSaveData>(GameplaySaveKeys.Materials,
                GameplaySaveKeys.MaterialsVersion, Materials.Capture, Materials.Restore,
                MaterialsSaveMigrations.Migrate)));
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
                () => new BlueprintsSaveData { found = Blueprints.Capture(), carried = Blueprints.CaptureCarried() },
                RestoreBlueprints)));
            _saveTokens.Add(save.Register(new SaveSection<TowerPartsSaveData>(GameplaySaveKeys.TowerParts,
                GameplaySaveKeys.TowerPartsVersion, TowerParts.Capture, RestoreTowerParts)));
        }

        private void RestoreTowerParts(TowerPartsSaveData data)
        {
            TowerParts.Restore(data);
            TowerPartField.Sync();
        }

        /// <summary>The tower's story parts, or null (logged) when the content is broken.</summary>
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

        /// <summary>The story parts in the world, or null (logged) when a part's model or site is missing.</summary>
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

        private void RestoreBlueprints(BlueprintsSaveData data)
        {
            Blueprints.Restore(data.found, data.carried);
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
                return new Blueprints(events, kits, _progression.Tuning.FirstKit, _progression.Tuning.Tubes,
                    SiteOfRelic);
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
                bool found = false;
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
        /// The save is loaded (Start runs after it): Audio and UI start from the real radio program, and Rover puts the
        /// carried blueprint tubes and story parts on 07's back.
        /// </summary>
        private void Start()
        {
            Radio.Announce();
            Power.Announce();
            Blueprints.Announce();
            TowerParts.Announce();
        }

        private void Update()
        {
            Power.Show(_tower.ShownLevel);
            TowerPartField.Step(Time.time);
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
                : null;
        }

        private void OnDestroy()
        {
            foreach (IDisposable token in _saveTokens)
            {
                token.Dispose();
            }

            _saveTokens.Clear();
            Blueprints?.Dispose();
            TowerPartField?.Dispose();
            _meshes?.Dispose();
        }
    }
}
