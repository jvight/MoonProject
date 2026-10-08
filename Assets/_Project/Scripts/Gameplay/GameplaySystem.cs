using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Gameplay domain's single entry in the bootstrap's system list (after World and Rover). Resolves the world
    /// (surface, layout, anchors), rover and camera services, creates the wallet, the upgrade service (which grants
    /// rover abilities through <see cref="IRoverAbilities"/>) and the radio program, initialises the gameplay parts in
    /// dependency order (relics, scrap, excavation, tether, home, radio tower, workshop, friends, cassettes, log
    /// caches, sonar, Bell's signals, the cassette shelf, the relay network), registers the services other domains
    /// read (<see cref="IScrapWallet"/>, <see cref="ITetherAim"/>, <see cref="IUpgradeShop"/>,
    /// <see cref="IInteractionHints"/>, <see cref="IFriendRoster"/>, <see cref="IFriendStatuses"/>,
    /// <see cref="IRadioProgram"/>, <see cref="IStationReach"/>, <see cref="IRadioHop"/>, <see cref="IRelayStatus"/>)
    /// and the save sections, announces the radio's signal radius and, once the save is loaded, the radio program, and
    /// owns the shared glow meshes. The radio-hop moves 07 through Core's <see cref="IRoverPlacement"/> when the Rover
    /// domain registers it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySystem : MonoBehaviour, IGameSystem
    {
        [Tooltip("SoftGlow materials (Generated/Gameplay/GameplayVisuals.asset).")]
        [SerializeField] private GameplayVisuals _visuals;

        [Tooltip("Every upgrade bought with scrap (Assets/_Project/Data/Content/Upgrades).")]
        [SerializeField] private UpgradeDefinition[] _upgradeDefinitions = Array.Empty<UpgradeDefinition>();

        [SerializeField] private RelicField _relics;
        [SerializeField] private ScrapField _scrap;
        [SerializeField] private SonarSystem _sonar;
        [SerializeField] private ExcavationSystem _excavation;
        [SerializeField] private TetherSystem _tether;
        [SerializeField] private HomeBase _home;
        [SerializeField] private RadioTower _tower;
        [SerializeField] private Workshop _workshop;
        [SerializeField] private FriendField _friends;
        [SerializeField] private CassetteField _cassettes;
        [SerializeField] private LogCacheField _logs;
        [SerializeField] private SignalField _signals;
        [SerializeField] private CassetteShelf _shelf;
        [SerializeField] private RelayField _relays;

        private readonly List<IDisposable> _saveTokens = new List<IDisposable>();
        private GlowMeshSet _meshes;

        public ScrapWallet Wallet { get; private set; }

        public UpgradeService Upgrades { get; private set; }

        public IUpgradeShop Shop { get; private set; }

        public IInteractionHints Hints { get; private set; }

        public RadioProgram Radio { get; private set; }

        public RelicField Relics => _relics;

        public ScrapField Scrap => _scrap;

        public SonarSystem Sonar => _sonar;

        public ExcavationSystem Excavation => _excavation;

        public TetherSystem Tether => _tether;

        public HomeBase Home => _home;

        public RadioTower Tower => _tower;

        public Workshop Workshop => _workshop;

        public FriendField Friends => _friends;

        public CassetteField Cassettes => _cassettes;

        public LogCacheField Logs => _logs;

        public SignalField Signals => _signals;

        public CassetteShelf Shelf => _shelf;

        public RelayField Relays => _relays;

        internal void Wire(GameplayVisuals visuals, UpgradeDefinition[] upgradeDefinitions, RelicField relics,
            ScrapField scrap, SonarSystem sonar, ExcavationSystem excavation, TetherSystem tether, HomeBase home,
            RadioTower tower, Workshop workshop, FriendField friends, CassetteField cassettes, LogCacheField logs,
            SignalField signals, CassetteShelf shelf, RelayField relays)
        {
            _visuals = visuals;
            _upgradeDefinitions = upgradeDefinitions;
            _relics = relics;
            _scrap = scrap;
            _sonar = sonar;
            _excavation = excavation;
            _tether = tether;
            _home = home;
            _tower = tower;
            _workshop = workshop;
            _friends = friends;
            _cassettes = cassettes;
            _logs = logs;
            _signals = signals;
            _shelf = shelf;
            _relays = relays;
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

            _meshes = new GlowMeshSet();
            Wallet = new ScrapWallet(context.Events);
            var save = context.Get<ISaveService>();
            var services = new GameplayServices(context.Events, context.Input, context.Get<ITerrainQuery>(),
                context.Get<IWorldLayout>(), context.Get<IWorldAnchors>(), context.Get<IRoverState>(),
                context.Get<IRoverRig>(), context.Get<IViewCamera>(), save, Wallet, _visuals, _meshes);
            var abilities = context.Get<IRoverAbilities>();
            Upgrades = new UpgradeService(context.Events, Wallet, abilities, _upgradeDefinitions);
            Radio = new RadioProgram(context.Events, _cassettes.Catalog.Ids());

            Vector3 lander = HomeBase.LanderSpot(services.Terrain, services.Layout.BasePosition, _home.Tuning);
            if (!_relics.Initialize(services) || !_scrap.Initialize(services, _relics.Sites, lander) ||
                !_excavation.Initialize(services, _relics) || !_tether.Initialize(services, _relics) ||
                !_home.Initialize(services, _relics, _tether, Upgrades) || !_tower.Initialize(services, Upgrades) ||
                !_workshop.Initialize(services, Upgrades) ||
                !_friends.Initialize(services, Radio, _cassettes.Catalog, _relics, _scrap, _home) ||
                !_cassettes.Initialize(services, Radio, _scrap.Tuning, KeepClearOfCassettes()) ||
                !_logs.Initialize(services, _scrap.Tuning) || !_sonar.Initialize(services, _relics, _friends) ||
                !_signals.Initialize(services, _friends, _cassettes, _logs, _relics, abilities, _sonar.Tuning) ||
                !_shelf.Initialize(Radio, _cassettes.Catalog, _friends.BellTuning) ||
                !_relays.Initialize(services, Upgrades, _tether, _friends.Tuning, _scrap.Tuning, Placement(context)))
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

            Shop = new UpgradeShop(Upgrades, stations, save);
            Hints = new InteractionHints(services.Rover, _sonar, _excavation, _tether, _home, stations, Upgrades,
                _friends, _relays);
            context.Register<IScrapWallet>(Wallet);
            context.Register<ITetherAim>(_tether);
            context.Register<IUpgradeShop>(Shop);
            context.Register<IInteractionHints>(Hints);
            context.Register<IFriendRoster>(_friends);
            context.Register<IFriendStatuses>(_friends);
            context.Register<IRadioProgram>(Radio);
            context.Register<IStationReach>(_relays.Reach);
            context.Register<IRadioHop>(_relays.Hop);
            context.Register<IRelayStatus>(_relays);
            Upgrades.PublishSignals();
            RegisterSaveSections(save);
        }

        private void RegisterSaveSections(ISaveService save)
        {
            _saveTokens.Add(save.Register(new SaveSection<WalletSaveData>(GameplaySaveKeys.Wallet,
                GameplaySaveKeys.WalletVersion, Wallet.Capture, Wallet.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<ScrapSaveData>(GameplaySaveKeys.Scrap,
                GameplaySaveKeys.ScrapVersion, _scrap.Capture, _scrap.Restore)));
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
        }

        /// <summary>Core's rover placement for the radio-hop, or null while no domain registers it.</summary>
        private static IRoverPlacement Placement(GameContext context)
        {
            return context.TryGet(out IRoverPlacement placement) ? placement : null;
        }

        /// <summary>The save is loaded (Start runs after it): Audio and UI start from the real radio program.</summary>
        private void Start()
        {
            Radio.Announce();
        }

        private void RestoreRadio(RadioSaveData data)
        {
            Radio.Restore(data);
            _cassettes.SyncCollected();
            _shelf.Sync();
        }

        /// <summary>Every relic site, friend site and friend part: a basin cassette keeps clear of them all.</summary>
        private List<Vector3> KeepClearOfCassettes()
        {
            var points = new List<Vector3>();
            foreach (RelicSite site in _relics.Sites)
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
            _home.SyncDisplays();
            _sonar.RefreshDiscoveredSites();
        }

        private void RestoreUpgrades(UpgradesSaveData data)
        {
            Upgrades.Restore(data);
            _tower.ShowLevel(Upgrades.LevelOf(_tower.Definition.Id));
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

            return _upgradeDefinitions == null || _upgradeDefinitions.Length == 0 ? "no upgrade definitions."
                : _relics == null ? "RelicField is not assigned."
                : _scrap == null ? "ScrapField is not assigned."
                : _sonar == null ? "SonarSystem is not assigned."
                : _excavation == null ? "ExcavationSystem is not assigned."
                : _tether == null ? "TetherSystem is not assigned."
                : _home == null ? "HomeBase is not assigned."
                : _home.Tuning == null ? "HomeBase has no BaseTuning."
                : _tower == null ? "RadioTower is not assigned."
                : _workshop == null ? "Workshop is not assigned."
                : _friends == null ? "FriendField is not assigned."
                : _cassettes == null ? "CassetteField is not assigned."
                : _cassettes.Catalog == null ? "CassetteField has no CassetteCatalog."
                : _cassettes.Catalog.Validate() != null ? "CassetteCatalog " + _cassettes.Catalog.Validate() + "."
                : _logs == null ? "LogCacheField is not assigned."
                : _signals == null ? "SignalField is not assigned."
                : _shelf == null ? "CassetteShelf is not assigned."
                : _relays == null ? "RelayField is not assigned."
                : null;
        }

        private void OnDestroy()
        {
            foreach (IDisposable token in _saveTokens)
            {
                token.Dispose();
            }

            _saveTokens.Clear();
            _meshes?.Dispose();
        }
    }
}
