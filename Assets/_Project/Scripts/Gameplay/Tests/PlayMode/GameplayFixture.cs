using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// A complete gameplay stack built in test code: a flat world with canyon, relay and salvage site anchors, a fake
    /// 07, test content (salvage site, bundle, relic, Tilly, Bell, cassette, log cache, tape rack, relay
    /// mast and relay part stand-ins in place of the Art prefabs, with the contracts' node names; default tuning;
    /// SoftGlow materials) and the real gameplay components, wired the way the scene contributor wires them and booted
    /// through GameBootstrap with a private save slot.
    /// </summary>
    public sealed class GameplayFixture : IDisposable
    {
        public const string ShaderPath = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShaderPath = "Assets/_Project/Shaders/Gameplay/Glint.shader";

        public static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));

        private static readonly string[] RelicIds =
        {
            "walkman", "rubber_duck", "golden_record", "astronaut_boot", "teapot", "garden_gnome",
        };

        /// <summary>Where each relic rests, as in the content builder (the gnome beside the duck).</summary>
        private static readonly string[] RelicSites =
        {
            "depot", "garage", "kestrel", "drill", "lander", "garage",
        };

        /// <summary>The haul bot's id: it stands inside the drill site, as in the content builder.</summary>
        public const string HaulBot = "derelict.haul_bot";

        /// <summary>The drill site's node the haul bot stands on, and 06's node relay.2's hut is set on.</summary>
        private const string HaulBotSocket = "HaulBotSocket";

        private const string HutSocket = "HutSocket";

        /// <summary>Stand-in hull fragment variants for Kestrel-3's scrap field (as Art's).</summary>
        private const int HullFragments = 4;

        /// <summary>Units in each stand-in crew crate (as the economy's).</summary>
        public const int CrateUnits = 6;

        /// <summary>Whose belongings each relic is, as in the content builder.</summary>
        private static readonly string[] RelicCrews = { "ro", "kenji", "ro", "ines", "theo", "theo" };

        private static readonly string[] SiteNames = { "depot", "kestrel", "drill", "garage", "lander" };

        /// <summary>The stand-in lab's floor (m above the ground), its stations' lens height and distance from the axis.
        /// </summary>
        private const float LabFloor = 3.6f;
        private const float LensHeight = 1.25f;
        private const float StationRadius = 2.4f;

        /// <summary>Seconds the turntable may take to come to rest after the ride (a half turn and its delay).</summary>
        private const float LabTurnTimeout = 4f;

        private static readonly float[] RelicMasses = { 6f, 3f, 5f, 9f, 7f, 14f };

        /// <summary>
        /// Each relic stand-in's size (m) at true scale, gently exaggerated (VISION ruling 13, the art report): the
        /// walkman 0.25, the duck 0.20, the golden record 0.40, the boot 0.40, the teapot 0.35, the gnome 0.50.
        /// </summary>
        private static readonly Vector3[] RelicSizes =
        {
            new Vector3(0.25f, 0.16f, 0.08f), new Vector3(0.16f, 0.2f, 0.14f), new Vector3(0.4f, 0.4f, 0.06f),
            new Vector3(0.18f, 0.4f, 0.3f), new Vector3(0.35f, 0.24f, 0.22f), new Vector3(0.22f, 0.5f, 0.2f),
        };

        /// <summary>A cassette stand-in's size (m): 0.18 m across, as the art rescaled them.</summary>
        private static readonly Vector3 CassetteSize = new Vector3(0.18f, 0.115f, 0.03f);

        private readonly List<Object> _created = new List<Object>();
        private readonly InputActionAsset _controls;

        private readonly bool _cargoSeat;

        private GameplayFixture(InputActionAsset controls, string saveSlot, bool cargoSeat)
        {
            _cargoSeat = cargoSeat;
            _controls = controls;
            SaveSlot = saveSlot;
        }

        public string SaveSlot { get; }

        public FlatWorldSystem World { get; private set; }

        public GameplayFakeRover Rover { get; private set; }

        public GameplaySystem Gameplay { get; private set; }

        public GameBootstrap Bootstrap { get; private set; }

        public EventRecorder Events { get; private set; }

        public GlintTuning GlintTuning { get; private set; }

        public SalvageTuning SalvageTuning { get; private set; }

        public ProgressionTuning ProgressionTuning { get; private set; }

        /// <summary>The opening's anchors, reserve, cargo points and crates, as the tuning builder makes it.</summary>
        public OpeningTuning OpeningTuning { get; private set; }

        /// <summary>The fragment sets and the scan verb (0.5.5 §1), as the tuning builder makes it.</summary>
        public ScanTuning ScanTuning { get; private set; }

        public CargoTuning CargoTuning { get; private set; }

        public SonarTuning SonarTuning { get; private set; }

        public RelicTuning RelicTuning { get; private set; }

        public ExcavationTuning ExcavationTuning { get; private set; }

        public TetherTuning TetherTuning { get; private set; }

        public BaseTuning BaseTuning { get; private set; }

        public RadioTowerTuning TowerTuning { get; private set; }

        public LiftTuning LiftTuning { get; private set; }

        /// <summary>The stand-in crew's lift beside the lander.</summary>
        public LiftStation Lift { get; private set; }

        public UpgradeDefinition RadioTowerUpgrade { get; private set; }

        public WorkshopTuning WorkshopTuning { get; private set; }

        public UpgradeDefinition HoverJumpUpgrade { get; private set; }

        public UpgradeDefinition CargoCradleUpgrade { get; private set; }

        public UpgradeDefinition WarmHeadlampUpgrade { get; private set; }

        public UpgradeDefinition BoostCoilsUpgrade { get; private set; }

        public UpgradeDefinition CellPackUpgrade { get; private set; }

        public UpgradeDefinition FocusLensUpgrade { get; private set; }

        public UpgradeDefinition SideBinsUpgrade { get; private set; }

        public FriendTuning FriendTuning { get; private set; }

        public FriendDefinition Tilly { get; private set; }

        public CassetteTuning CassetteTuning { get; private set; }

        public CassetteCatalog Cassettes { get; private set; }

        public LogCacheTuning LogCacheTuning { get; private set; }

        /// <summary>The lander's FriendSocket_tilly stand-in.</summary>
        public Transform TillyPerch { get; private set; }

        public BellTuning BellTuning { get; private set; }

        public FriendDefinition Bell { get; private set; }

        /// <summary>The tower's BellCorner stand-in (pinned under the tower anchor, as the scene build does).</summary>
        public Transform BellCorner { get; private set; }

        /// <summary>The tape rack's Slot_0..7 stand-ins.</summary>
        public Transform[] ShelfSlots { get; private set; }

        public RelayTuning RelayTuning { get; private set; }

        public BuildingTuning BuildingTuning { get; private set; }

        public StructureCatalog StructureCatalog { get; private set; }

        /// <summary>
        /// Builds and boots everything; 07 starts at the base facing +Z, its tether already fitted unless
        /// <paramref name="tether"/> is false (Kenji's garage port's own tests earn it). A new game (no save yet) starts with an empty
        /// stock unless <paramref name="spares"/> keeps 07's starting spares (0.5.5 D4) in its hold.
        /// </summary>
        /// <param name="cargoSeat">False boots a rover that registers no cargo seat (the boot check's test).</param>
        public static GameplayFixture Boot(InputActionAsset controls, string saveSlot = null, bool cargoSeat = true,
            bool tether = true, bool spares = false)
        {
            var fixture = new GameplayFixture(controls, saveSlot ?? BootstrapHarness.NewTestSlot(), cargoSeat);
            fixture.Build(spares);
            if (tether)
            {
                fixture.Rover.Grant(RoverAbility.Tether);
            }

            return fixture;
        }

        /// <summary>
        /// 07 wears <paramref name="part"/> as a loaded game would have it (0.5.5 §3.5): in its socket, its ability
        /// granted, with no craft and no moment. For tests about what a part does, not how it is fitted.
        /// </summary>
        public void Wear(RoverKitPiece part)
        {
            Gameplay.Equipment.RestoreOwned(part);
        }

        /// <summary>The salvage site named <paramref name="name"/> ("depot", ...).</summary>
        public SalvageSite FindSite(string name)
        {
            SalvageSite site = Gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + name);
            if (site == null)
            {
                throw new ArgumentException($"No salvage site '{name}'.", nameof(name));
            }

            return site;
        }

        public Relic FindRelic(string id)
        {
            Relic relic = Gameplay.Relics.Find(id);
            if (relic == null)
            {
                throw new ArgumentException($"No relic '{id}'.", nameof(id));
            }

            return relic;
        }

        /// <summary>
        /// Brings the base's power as far as tower level <paramref name="towerLevel"/> powers it (docs/features/M3-16),
        /// the way a loaded game would: the tower stands at that level, no moment.
        /// </summary>
        public void PowerTo(int towerLevel)
        {
            Gameplay.Upgrades.Restore(new UpgradesSaveData
            {
                upgrades = new[] { new UpgradeSaveData { id = RadioTowerUpgrade.Id, level = towerLevel } },
            });
            Gameplay.Tower.ShowLevel(towerLevel);
        }

        /// <summary>
        /// Home powered and the belly lift restored, as a loaded game would show them; then 07 rests on the platform
        /// (heading in from the dock) until it has carried 07 up into the lab and the turntable rests facing the station
        /// that wants what 07 carries (docs/features/M3-19). The stand-in rover stays below: the lab's logic needs no
        /// ride pose.
        /// </summary>
        public IEnumerator RideIntoTheLab()
        {
            PowerTo(Math.Max(1, Gameplay.Upgrades.LevelOf(RadioTowerUpgrade.Id)));
            LiftStation lift = Gameplay.Lift;
            lift.Restore(new LiftSaveData { restored = true });
            Vector3 heading = lift.RideHeading;
            Rover.Place(lift.RideSpot, Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg);
            float timeout = LiftTuning.BoardDelay + LiftTuning.RideSeconds + LiftTuning.SettleSeconds + LabTurnTimeout;
            yield return Waits.Until(() => lift.Phase == LiftPhase.Inside && !lift.Turning, timeout);
        }

        /// <summary>The relic stands on the keepsake shelf, as a loaded game shows it (no deposit, no news).</summary>
        public void Display(Relic relic, int slot)
        {
            relic.Restore(RelicState.Displayed, 1f, true, relic.transform.position, Quaternion.identity, slot);
            Gameplay.Home.SyncDisplays();
        }

        /// <summary>
        /// Every tower component already fed to the port (docs/features/M3-16 P1), for sessions about something else
        /// that buy tower levels: as a load would leave them, silently.
        /// </summary>
        public void DeliverTowerParts()
        {
            TowerParts parts = Gameplay.TowerParts;
            var delivered = new string[parts.Count];
            for (int i = 0; i < delivered.Length; i++)
            {
                delivered[i] = parts.PartAt(i).PartId;
            }

            parts.Restore(new TowerPartsSaveData { delivered = delivered });
            Gameplay.TowerPartField.Sync();
        }

        /// <summary>
        /// 07 has learned <paramref name="kits"/>' blueprints, and only those (no sheet is pinned from the start,
        /// docs/features/M3-21 §4): the bay can build them. Their fragments are left as they lie.
        /// </summary>
        public void Learn(params string[] kits)
        {
            Gameplay.Blueprints.Restore(new BlueprintsSaveData { found = kits });
        }

        /// <summary>Puts materials on home's shelves, as a trip home would.</summary>
        public void GiveMaterials(int metal, int wiring, int optics)
        {
            if (metal > 0)
            {
                Gameplay.Materials.Deliver(SalvageMaterial.Metal, metal);
            }

            if (wiring > 0)
            {
                Gameplay.Materials.Deliver(SalvageMaterial.Wiring, wiring);
            }

            if (optics > 0)
            {
                Gameplay.Materials.Deliver(SalvageMaterial.Optics, optics);
            }
        }

        /// <summary>
        /// Saves a capture of the rover camera to Logs/gameplay-captures/<paramref name="name"/>.png.
        /// </summary>
        public void Capture(string name)
        {
            FrameCapture.SavePng(Rover.Camera, 960, 540, Path.Combine(CaptureFolder, name + ".png"));
        }

        /// <summary>Tears the stack down; <paramref name="keepSave"/> leaves the save file for a reboot.</summary>
        public void Dispose(bool keepSave)
        {
            Events?.Dispose();
            if (Bootstrap != null)
            {
                Object.DestroyImmediate(Gameplay.gameObject);
                Object.DestroyImmediate(Bootstrap.gameObject);
            }

            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            if (!keepSave)
            {
                BootstrapHarness.DeleteSaveFiles(SaveSlot);
            }
        }

        public void Dispose()
        {
            Dispose(false);
        }

        private void Build(bool spares)
        {
            World = Track(FlatWorldSystem.Create());
            Rover = Track(GameplayFakeRover.Create(Vector3.zero, 0f, _cargoSeat));

            GlintTuning = Asset<GlintTuning>();
            SonarTuning = Asset<SonarTuning>();
            RelicTuning = Asset<RelicTuning>();
            ExcavationTuning = Asset<ExcavationTuning>();
            TetherTuning = Asset<TetherTuning>();
            BaseTuning = Asset<BaseTuning>();
            TowerTuning = Asset<RadioTowerTuning>();
            RadioTowerUpgrade = Asset<UpgradeDefinition>();
            RadioTowerUpgrade.Populate("radio_tower", UpgradeStationKind.RadioTower, 60f, new[]
            {
                new UpgradeLevel(new Recipe(0, 1, 1), 110f, 1.25f), new UpgradeLevel(new Recipe(1, 2, 0), 170f, 1.5f),
                new UpgradeLevel(new Recipe(0, 2, 2), 260f, 1.8f),
            });
            WorkshopTuning = Asset<WorkshopTuning>();
            HoverJumpUpgrade = Asset<UpgradeDefinition>();
            HoverJumpUpgrade.Populate("rover.hover_jump", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(new Recipe(4, 2, 1), RoverAbility.HoverJump),
            });
            CargoCradleUpgrade = RoverKit("rover.cargo_cradle", new Recipe(4, 0, 0), RoverAbility.CargoCradle);
            WarmHeadlampUpgrade = RoverKit("rover.warm_headlamp", new Recipe(1, 0, 3), RoverAbility.WarmHeadlamp);
            BoostCoilsUpgrade = RoverKit("rover.boost_coils", new Recipe(1, 2, 1), RoverAbility.BoostCoils);
            CellPackUpgrade = RoverKit("rover.cell_pack", new Recipe(3, 0, 2), RoverAbility.CellPack);
            FocusLensUpgrade = RoverKit("rover.focus_lens", new Recipe(0, 1, 3), RoverAbility.FocusLens);
            SideBinsUpgrade = RoverKit("rover.side_bins", new Recipe(3, 1, 0), RoverAbility.SideBins);

            var relicCatalog = Asset<RelicCatalog>();
            var definitions = new RelicDefinition[RelicIds.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i] = Asset<RelicDefinition>();
                definitions[i].Populate(RelicIds[i], RelicMasses[i],
                    Template("Relic_" + RelicIds[i], RelicSizes[i]), i,
                    WorldAnchorIds.SitePrefix + RelicSites[i], RelicCrews[i]);
            }

            relicCatalog.Populate(definitions);

            var visuals = Asset<GameplayVisuals>();
            Shader shader = LoadShader(ShaderPath);
            visuals.Populate(GlowMaterial(shader, GlowRole.TractorBeam),
                GlowMaterial(shader, GlowRole.TetherBeam), GlowMaterial(shader, GlowRole.Flash),
                GlowMaterial(shader, GlowRole.RelicHalo), GlowMaterial(shader, GlowRole.Dust),
                GlowMaterial(shader, GlowRole.WarmRing), GlowMaterial(shader, GlowRole.WarmGlow),
                Track(GlintMaterials.CreatePart(LoadShader(GlintShaderPath))),
                GlowMaterial(shader, GlowRole.Spark),
                GlowMaterial(shader, GlowRole.HomeHalo), GlowMaterial(shader, GlowRole.LinkPulse));

            visuals.PopulatePads(new[] { Pad(1.6f), Pad(2.4f), Pad(3f) }, new[] { 1.6f, 2.4f, 3f });
            visuals.PopulateFeel(GlowMaterial(shader, GlowRole.Spark), GlowMaterial(shader, GlowRole.TetherBeam),
                GlowMaterial(shader, GlowRole.WarmGlow), GlowMaterial(shader, GlowRole.WarmRing),
                Asset<FeelVfxTuning>());
            visuals.PopulateComponents(new[]
            {
                Template("Part_TowerCoil", new Vector3(0.5f, 0.6f, 0.5f)),
                Template("Part_Transformer", new Vector3(0.6f, 0.5f, 0.45f)),
                Template("Part_RelaySpare", new Vector3(0.4f, 0.3f, 0.5f)),
            });
            ScanTuning = Asset<ScanTuning>();
            visuals.PopulateScans(FragmentTemplates(), JumperTemplate());
            visuals.PopulateGaragePort(PortTemplate(), Template("Kit_TapeDeck", new Vector3(0.3f, 0.15f, 0.12f)));
            visuals.PopulateHologram(GlowMaterial(shader, GlowRole.TractorBeam), GlowMaterial(shader, GlowRole.TetherBeam));
            visuals.PopulateContainers(new[]
            {
                ContainerTemplate("CoilCrate"), ContainerTemplate("TapeCase"), ContainerTemplate("JunctionBox"),
                ContainerTemplate("SupplyPod"), SledTemplate(), ContainerTemplate(ContainerKinds.Cabinet),
                ContainerTemplate(ContainerKinds.SpareCrate), PocketTemplate(),
            });
            var root = new GameObject("[Gameplay]");
            root.SetActive(false);
            Gameplay = root.AddComponent<GameplaySystem>();
            var salvage = Child<SalvageField>(root, "Salvage");
            var relics = Child<RelicField>(root, "Relics");
            var sonar = Child<SonarSystem>(root, "Sonar");
            var excavation = Child<ExcavationSystem>(root, "Excavation");
            var tether = Child<TetherSystem>(root, "Tether");
            var home = Child<HomeBase>(root, "Home");
            var cradle = Child<CargoCradle>(root, "CargoCradle");
            var tower = Child<RadioTower>(root, "RadioTower");
            var workshop = Child<Workshop>(root, "Workshop");
            var friends = Child<FriendField>(root, "Friends");
            var cassettes = Child<CassetteField>(root, "Cassettes");
            var logs = Child<LogCacheField>(root, "LogCaches");
            var signals = Child<BellSignalField>(root, "BellSignals");
            var relays = Child<RelayField>(root, "Relays");
            var battery = Child<BatteryField>(root, "Battery");
            var progression = Child<BaseProgression>(root, "Progression");
            var plotter = Child<BlueprintPlotter>(root, "Plotter");
            var build = Child<BuildField>(root, "Build");
            Lift = Child<LiftStation>(root, "CrewLift");
            CassetteShelf shelf = BuildBase(root.transform, home, tower, workshop, progression, plotter);
            BuildFriends(friends);
            BuildCassettes(cassettes);
            BuildLogCaches(logs);
            BuildStructures(build);
            RelayTuning = Asset<RelayTuning>();
            relays.Wire(RelayTuning, RelayModel("RelayMast", false), RelayModel("RelayMast_Broken", true),
                Template("Part_RelayModule", Vector3.one * 0.3f));
            BuildSalvage(salvage);
            relics.Wire(relicCatalog, RelicTuning);
            sonar.Wire(SonarTuning);
            excavation.Wire(ExcavationTuning);
            tether.Wire(TetherTuning);
            Gameplay.Wire(visuals, GlintTuning, new[]
                {
                    RadioTowerUpgrade, HoverJumpUpgrade, CargoCradleUpgrade, WarmHeadlampUpgrade, BoostCoilsUpgrade,
                    CellPackUpgrade, FocusLensUpgrade, SideBinsUpgrade,
                }, salvage, relics, sonar, excavation, tether, home, cradle, tower, workshop, friends, cassettes, logs,
                signals, shelf, relays, progression, plotter, Lift);
            battery.Wire(Asset<BatteryTuning>());
            Gameplay.WireBattery(battery);
            var materials = Asset<MaterialCatalog>();
            materials.Populate(new[]
            {
                new MaterialEntry(SalvageMaterial.Metal, "metal", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Wiring, "wiring", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Optics, "optics", MaterialFamily.Salvage, 4, true),
            });
            CargoTuning = Asset<CargoTuning>();
            Gameplay.WireCargo(materials, CargoTuning);
            Gameplay.WireCutter(Asset<CutterTuning>());
            Gameplay.WireScans(ScanTuning);
            Gameplay.WireBuilding(build);
            Gameplay.WireRuins(Ruins(root.transform));
            OpeningTuning = Asset<OpeningTuning>();
            Gameplay.WireOpening(OpeningTuning);
            root.SetActive(true);

            Bootstrap = BootstrapHarness.Create(_controls, SaveSlot, World, Rover, Gameplay);
            // A boot that failed its wiring (the boot checks' tests) has no stock to empty.
            bool emptyStock = !spares && Gameplay.Materials != null &&
                              Bootstrap.Context.Get<ISaveService>().LoadResult == SaveLoadResult.NoSave;
            if (emptyStock && !Gameplay.Materials.TrySpend(CargoTuning.StarterItems))
            {
                throw new InvalidOperationException("A new game's hold should carry 07's spares.");
            }

            Events = new EventRecorder(Bootstrap.Context.Events);
        }

        /// <summary>A stand-in for Art's StationPad_&lt;cm&gt;: a flat disc and its six PadLamp_n glow lamps.</summary>
        private GameObject Pad(float radius)
        {
            GameObject pad = Template("StationPad_" + Mathf.RoundToInt(radius * 100f),
                new Vector3(radius * 2f, 0.01f, radius * 2f));
            for (int i = 0; i < StationPad.LampCount; i++)
            {
                float angle = i * Mathf.PI * 2f / StationPad.LampCount;
                Block(pad.transform, new Vector3(Mathf.Cos(angle), 0.02f, Mathf.Sin(angle)) * (radius - 0.2f),
                    Vector3.one * 0.08f).name = StationPad.LampPrefix + i;
            }

            return pad;
        }

        /// <summary>
        /// A stand-in for Art's lander (with its charging dock), shelf, tower stages (with their service ports), Rover
        /// Bay and tape rack with the contract's node names and positions, wired the way the scene contributor wires
        /// the real prefabs.
        /// </summary>
        private CassetteShelf BuildBase(Transform parent, HomeBase home, RadioTower tower, Workshop workshop,
            BaseProgression progression, BlueprintPlotter plotter)
        {
            var baseRoot = new GameObject("Base").transform;
            baseRoot.SetParent(parent, false);
            Transform lander = Node("Lander", baseRoot, Vector3.zero);
            Block(lander, new Vector3(0f, 1.6f, 0f), new Vector3(4f, 3.2f, 4f));
            Transform windows = Block(lander, new Vector3(0f, 2f, 2.05f), new Vector3(2.5f, 0.6f, 0.1f));
            var sockets = new Transform[4];
            for (int i = 0; i < sockets.Length; i++)
            {
                sockets[i] = Node("LampSocket_" + i, lander, new Vector3(-1.5f + i, 3.4f, 1.6f));
            }

            Transform anchor = Node("TowerAnchor", lander, new Vector3(-6f, 0f, -1f));
            float[] heights = { 3.78f, 6.55f, 10f };
            var stages = new GameObject[3];
            var lights = new Renderer[3];
            var rings = new Renderer[3 * (TowerOnline.Rings - 1)];
            var offline = new Renderer[3];
            var beacons = new Transform[3];
            var hoppers = new Transform[3];
            var hatches = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Transform stage = Node("RadioTower_L" + (i + 1), anchor, Vector3.zero);
                Block(stage, new Vector3(0f, heights[i] * 0.5f, 0f), new Vector3(0.6f, heights[i], 0.6f));
                lights[i] = Block(stage, new Vector3(0f, heights[i] * 0.8f, 0.35f), new Vector3(0.3f, 0.3f, 0.05f))
                    .GetComponent<Renderer>();
                lights[i].name = "Lights";
                for (int ring = 1; ring < TowerOnline.Rings; ring++)
                {
                    rings[i * (TowerOnline.Rings - 1) + ring - 1] = Named(Block(lights[i].transform,
                        new Vector3(0f, ring, 0f), Vector3.one), "Ring_" + ring).GetComponent<Renderer>();
                }

                Transform plaque = Named(Block(stage, new Vector3(0.4f, 0.8f, 0.9f), new Vector3(0.3f, 0.2f, 0.02f)),
                    "OfflinePlaque");
                offline[i] = Named(Block(plaque, new Vector3(0f, 0f, 0.6f), new Vector3(0.2f, 0.2f, 0.5f)),
                    "OfflineLamp").GetComponent<Renderer>();
                beacons[i] = Node("BeaconSocket", stage, new Vector3(0f, heights[i], 0f));
                hoppers[i] = Node("HopperMouth", stage, new Vector3(0.5f, 1.12f, 1.25f));
                hoppers[i].localRotation = Quaternion.Euler(-40f, 0f, 0f);
                hatches[i] = Node("ServiceHatch", stage, new Vector3(-0.72f, 0.62f, 1.08f));
                Block(hatches[i], new Vector3(0.21f, 0f, 0.01f), new Vector3(0.42f, 0.5f, 0.02f));
                stages[i] = stage.gameObject;
            }

            Transform dock = Node("DockAnchor", lander, new Vector3(0f, 0.05f, 4.15f));
            dock.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform dockGlow = Named(Block(lander, new Vector3(0f, 0.02f, 4.15f), new Vector3(1.6f, 0.04f, 2.4f)),
                "DockGlow");
            Transform lab = BuildLab(lander);
            Transform keepsakes = lab.Find("Station_Keepsakes");
            var slots = new Transform[6];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Node("Slot_" + i, keepsakes, new Vector3(-0.9f + 0.6f * i, 0.2f, 0.3f));
            }

            home.Wire(BaseTuning, baseRoot, windows.GetComponent<Renderer>(), sockets, keepsakes, slots, dock,
                dockGlow.GetComponent<Renderer>());
            var walls = new Transform[DockLock.Walls];
            var strips = new Renderer[DockLock.Segments];
            for (int i = 0; i < walls.Length; i++)
            {
                walls[i] = Named(Block(dockGlow, new Vector3(i % 2 == 0 ? 0f : (i == 1 ? 0.85f : -0.85f), 0.17f,
                    i % 2 == 0 ? (i == 0 ? 1.2f : -1.2f) : 0f), new Vector3(0.3f, 0.35f, 0.05f)), "DockWall_" + i);
                for (int s = 0; s < DockLock.SegmentsPerWall; s++)
                {
                    strips[i * DockLock.SegmentsPerWall + s] = Named(Block(walls[i],
                        new Vector3(0.1f * (s - 1), 0.2f, 0f), new Vector3(0.08f, 0.02f, 0.04f)),
                        "Strip_" + s).GetComponent<Renderer>();
                }
            }

            var rims = new Renderer[DockLock.RimRings];
            for (int i = 0; i < rims.Length; i++)
            {
                rims[i] = Named(Block(dockGlow, new Vector3(0f, 0.03f, 0.3f * i), new Vector3(0.05f, 0.01f, 0.05f)),
                    "DockRim_" + i).GetComponent<Renderer>();
            }

            home.WireDockLock(walls, strips, rims);
            WireShelves(home, lander);
            tower.Wire(TowerTuning, RadioTowerUpgrade, anchor, stages, lights, rings, offline, beacons, hoppers,
                hatches);
            Transform bay = Node("WorkshopAnchor", lander, new Vector3(12.5f, 0f, -2f));
            BuildBay(bay, workshop, plotter);
            TillyPerch = Node("FriendSocket_tilly", lander, new Vector3(-1.6f, 3.3f, 0.9f));
            BellCorner = Node("BellCorner", anchor, new Vector3(-4.9f, 0f, 0.4f));
            BellCorner.localRotation = Quaternion.Euler(0f, 74f, 0f);
            Transform rack = Node("CassetteShelf", Node("CassetteShelfAnchor", anchor, new Vector3(-4.6f, 0f, -2.75f)),
                Vector3.zero);
            rack.parent.localRotation = Quaternion.Euler(0f, 60f, 0f);
            Block(rack, new Vector3(0f, 0.58f, -0.05f), new Vector3(0.56f, 1.16f, 0.1f)).name = "Rack";
            ShelfSlots = new Transform[8];
            for (int i = 0; i < ShelfSlots.Length; i++)
            {
                ShelfSlots[i] = Node("Slot_" + i, rack, new Vector3(i % 2 == 0 ? 0.12f : -0.12f,
                    1.003f - 0.16f * (i / 2), 0.006f));
            }

            var tapes = rack.gameObject.AddComponent<CassetteShelf>();
            tapes.Wire(ShelfSlots);
            Transform lift = BuildLift(lander, lab, dock);
            ProgressionTuning = Asset<ProgressionTuning>();
            progression.Wire(ProgressionTuning, new[] { dock, lander, dock, lift, anchor, lift, bay, rack });
            return tapes;
        }

        /// <summary>
        /// A stand-in Rover Bay with the art contract's nodes and positions (docs/ARCHITECTURE.md: Turntable, three
        /// arms Yaw/Upper/Lower/Tip down to their SparkSockets folded along the rail in the rest pose with the
        /// contract's link lengths, the floor arm FloorLift/FloorTip, HopperMouth in the back-right corner, Lamp_0/1,
        /// BaySign) and Kenji's pen plotter (Plotter/PenCarriage/PenHead, PlotterSheet, PlotterLamp), wired as the scene
        /// contributor wires the real one.
        /// </summary>
        private void BuildBay(Transform anchor, Workshop workshop, BlueprintPlotter plotter)
        {
            Transform bay = Node("RoverBay", anchor, Vector3.zero);
            Transform turntable = Node("Turntable", bay, new Vector3(0f, 0.15f, -0.35f));
            turntable.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Block(turntable, new Vector3(0f, -0.05f, 0f), new Vector3(3.5f, 0.1f, 3.5f));
            Vector3[] shoulders = { new Vector3(-1.3f, 3.08f, -0.35f), new Vector3(0f, 3.08f, -1.65f),
                new Vector3(1.3f, 3.08f, -0.35f) };
            float[] yaws = { 90f, 0f, -90f };
            float[] restYaws = { -90f, -90f, 90f };
            var armYaws = new Transform[shoulders.Length];
            var uppers = new Transform[shoulders.Length];
            var lowers = new Transform[shoulders.Length];
            var tips = new Transform[shoulders.Length];
            var sockets = new Transform[shoulders.Length];
            for (int i = 0; i < shoulders.Length; i++)
            {
                Transform arm = Node("Arm_" + i, bay, shoulders[i]);
                arm.localRotation = Quaternion.Euler(0f, yaws[i], 0f);
                armYaws[i] = Node("Yaw", arm, Vector3.zero);
                armYaws[i].localRotation = Quaternion.Euler(0f, restYaws[i], 0f);
                uppers[i] = Node("Upper", armYaws[i], Vector3.zero);
                uppers[i].localRotation = Quaternion.Euler(-90f, 0f, 0f);
                lowers[i] = Node("Lower", uppers[i], new Vector3(0f, -1.3f, 0f));
                lowers[i].localRotation = Quaternion.Euler(170f, 0f, 0f);
                tips[i] = Node("Tip", lowers[i], new Vector3(0f, -1.2f, 0f));
                tips[i].localRotation = Quaternion.Euler(-80f, 0f, 0f);
                sockets[i] = Node("SparkSocket", tips[i], new Vector3(0f, -0.26f, 0.06f));
                sockets[i].localRotation = Quaternion.Euler(90f, 180f, 0f);
            }

            Transform floorArm = Node("FloorArm", bay, new Vector3(0f, 0f, -0.35f));
            Transform floorLift = Node("FloorLift", floorArm, new Vector3(0f, 0.105f, 0f));
            Transform floorTip = Node("FloorTip", floorLift, new Vector3(0f, 0.03f, 0f));

            Transform mouth = Node("HopperMouth", bay, new Vector3(1.55f, 1.15f, -1.85f));
            mouth.localRotation = Quaternion.Euler(-40f, -45.9f, 0f);
            Renderer[] lamps =
            {
                Named(Block(bay, new Vector3(-1.55f, 3.08f, 1.65f), Vector3.one * 0.25f), "Lamp_0")
                    .GetComponent<Renderer>(),
                Named(Block(bay, new Vector3(1.55f, 3.08f, 1.65f), Vector3.one * 0.25f), "Lamp_1")
                    .GetComponent<Renderer>(),
            };
            Transform sign = Named(Block(bay, new Vector3(0f, 4.02f, 1.92f), new Vector3(0.8f, 0.42f, 0.04f)),
                "BaySign");
            workshop.Wire(WorkshopTuning,
                new[]
                {
                    HoverJumpUpgrade, CargoCradleUpgrade, WarmHeadlampUpgrade, BoostCoilsUpgrade, CellPackUpgrade,
                    FocusLensUpgrade, SideBinsUpgrade,
                },
                bay, turntable, mouth, lamps, sign.GetComponent<Renderer>(), sockets);
            workshop.WireArms(armYaws, uppers, lowers, tips, floorLift, floorTip);
            Transform plotterRoot = Node("Plotter", bay, new Vector3(-2.2f, 0f, -1.2f));
            Transform carriage = Node("PenCarriage", plotterRoot, new Vector3(0f, 0.9f, 0f));
            Transform head = Node("PenHead", carriage, Vector3.zero);
            Transform sheet = Block(plotterRoot, new Vector3(0.25f, 0.88f, 0f), new Vector3(0.5f, 0.01f, 0.35f));
            sheet.name = "PlotterSheet";
            Transform lamp = Block(plotterRoot, new Vector3(0.6f, 1f, 0.17f), new Vector3(0.05f, 0.05f, 0.05f));
            lamp.name = "PlotterLamp";
            Transform board = Node("Blueprints", bay, new Vector3(-2.2f, 1.6f, -1.6f));
            foreach (string ability in new[]
                     {
                         "HoverJump", "CargoCradle", "WarmHeadlamp", "BoostCoils", "CellPack", "FocusLens", "SideBins",
                     })
            {
                Block(board, Vector3.zero, new Vector3(0.3f, 0.4f, 0.01f)).name = "Blueprint_" + ability;
            }

            plotter.Wire(plotterRoot, carriage, head, sheet.gameObject, lamp.GetComponent<Renderer>(), board, 0.5f,
                0.35f);
        }

        /// <summary>A one-level rover ability sold at the bench, as in the content builder.</summary>
        private UpgradeDefinition RoverKit(string id, Recipe recipe, RoverAbility ability)
        {
            var upgrade = Asset<UpgradeDefinition>();
            upgrade.Populate(id, UpgradeStationKind.Workshop, 0f, new[] { new UpgradeLevel(recipe, ability) });
            return upgrade;
        }

        /// <summary>
        /// The five salvage sites as stand-in wrecks with the site contract's nodes (the canyon lander past the
        /// Hover-Jump gate), 06 and the Mule as stand-in derelicts (0.5.5 §1.7) and the three material bundles, as in
        /// the content builder.
        /// </summary>
        private void BuildSalvage(SalvageField field)
        {
            SalvageTuning = Asset<SalvageTuning>();
            var sites = new SalvageSiteEntry[SiteNames.Length];
            for (int i = 0; i < sites.Length; i++)
            {
                string name = SiteNames[i];
                GameObject model = SiteModel(name, name == "depot");
                sites[i] = new SalvageSiteEntry(WorldAnchorIds.SitePrefix + name, model,
                    name == "lander" ? new AbilityGate(true, RoverAbility.HoverJump) : AbilityGate.Open, false, 1,
                    SalvageCutLines.Across(model), SmallPieces(model),
                    new SupplyCrate("kenji", SalvageMaterial.Metal, CrateUnits));
            }

            GameObject elder = DerelictModel("Derelict_06", false);
            GameObject mule = DerelictModel("Derelict_Mule", true);
            GameObject haulBot = DerelictModel("Derelict_HaulBot", true);
            var hut = new[] { new SocketProp(HutSocket, Template("RelayHut", new Vector3(2f, 2f, 1.6f))) };
            var derelicts = new[]
            {
                SalvageSiteEntry.Derelict(WorldAnchorIds.Micro06, WorldAnchorIds.Micro06, string.Empty, elder,
                    SalvageCutLines.Across(elder), SmallPieces(elder), hut),
                SalvageSiteEntry.Derelict(WorldAnchorIds.MicroMule, WorldAnchorIds.MicroMule, string.Empty, mule,
                    SalvageCutLines.Across(mule), SmallPieces(mule), Array.Empty<SocketProp>()),
                SalvageSiteEntry.Derelict(HaulBot, WorldAnchorIds.SitePrefix + "drill", HaulBotSocket, haulBot,
                    SalvageCutLines.Across(haulBot), SmallPieces(haulBot), Array.Empty<SocketProp>()),
            };
            var fragments = new GameObject[HullFragments];
            var seams = new PieceCutLine[HullFragments];
            for (int i = 0; i < HullFragments; i++)
            {
                fragments[i] = HullFragment(i);
                SalvageCutLines.TryThrough(fragments[i].transform, i, out seams[i]);
            }

            var catalog = Asset<SalvageCatalog>();
            catalog.Populate(sites, derelicts, Template("Material_Metal", Vector3.one * 0.35f),
                Template("Material_Wiring", Vector3.one * 0.35f), Template("Material_Optics", Vector3.one * 0.35f),
                fragments, seams, PieceSize.Small);
            field.Wire(SalvageTuning, catalog);
        }

        /// <summary>Every piece of a stand-in wreck is small (one short segment, the small yield).</summary>
        private static PieceSizeEntry[] SmallPieces(GameObject model)
        {
            var sizes = new List<PieceSize>();
            foreach (Transform node in model.transform)
            {
                if (SalvagePieceName.TryParsePiece(node.name, out _, out _, out _))
                {
                    sizes.Add(PieceSize.Small);
                }
            }

            return SalvageSizes.Deal(model, sizes);
        }

        /// <summary>
        /// A stand-in wreck in its anchor's frame (+Z the way 07 arrives): a skeleton wall behind the heart, one small
        /// piece of each material around it (each worth the small yield), every cut point on the face toward 07's
        /// approach, and at the depot also a drag piece behind them.
        /// </summary>
        private GameObject SiteModel(string name, bool drag)
        {
            var root = new GameObject("Site_" + name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Named(Block(root.transform, new Vector3(0f, 1f, 4.5f), new Vector3(6f, 2f, 1f)), "Skeleton");
            Piece(root.transform, "Salvage_0_Metal", new Vector3(-3.5f, 0.4f, 0.5f), Vector3.one * 0.8f);
            Piece(root.transform, "Salvage_1_Wiring", new Vector3(3.5f, 0.4f, 0.5f), Vector3.one * 0.8f);
            Piece(root.transform, "Salvage_2_Optics", new Vector3(-2.5f, 0.4f, 3f), Vector3.one * 0.8f);
            if (drag)
            {
                Piece(root.transform, "Salvage_3_Metal_Drag", new Vector3(2.5f, 0.3f, 3f),
                    new Vector3(1.4f, 0.4f, 1.4f));
            }

            Node("Heart", root.transform, Vector3.zero);
            if (name == "drill")
            {
                // The haul bot's seat on clear ground at the rig's right (Art's HaulBotSocket, 0.5.5 §1.7).
                Node(HaulBotSocket, root.transform, new Vector3(6f, 0f, -2f));
            }

            string blueprint = name == "garage" ? "CargoCradle"
                : name == "kestrel" ? "FocusLens"
                : name == "lander" ? "SideBins"
                : null;
            if (blueprint != null)
            {
                FragmentSockets(root.transform, blueprint, new Vector3(-10f, 0f, -8f));
            }

            if (name == "garage")
            {
                // Kenji's bench beside the port's pad, the spare crate's front toward 07 coming in (0.5.5 §2).
                Node(GaragePort.SpareCrateSocket, root.transform, new Vector3(9f, 0f, -1f)).localRotation =
                    Quaternion.Euler(0f, 180f, 0f);
            }

            for (int i = 0; i < RelicIds.Length; i++)
            {
                if (RelicSites[i] == name)
                {
                    // Art's relic pocket at the wreck's edge (0.5.5 §2): the gnome beside the duck's.
                    Transform pocket = Node("RelicPocket_" + RelicIds[i], root.transform,
                        i == RelicIds.Length - 1 ? new Vector3(1.2f, 0f, 0f) : Vector3.zero);
                    Node("DigSpot", pocket, Vector3.zero);
                }
            }

            Transform crate = Named(Block(root.transform, new Vector3(0f, 0.3f, 2.4f), new Vector3(0.8f, 0.6f, 0.6f)),
                "CrewCrate");
            Transform lid = Named(Block(crate, new Vector3(0f, 0.35f, 0f), new Vector3(0.82f, 0.1f, 0.62f)), "Lid");
            Node("CutPoint", lid, new Vector3(0f, 0f, -0.32f)).localRotation = Quaternion.Euler(0f, 180f, 0f);
            Named(Block(lid, new Vector3(0f, 0f, -0.31f), new Vector3(0.7f, 0.02f, 0.02f)), "Seal");
            Named(Block(root.transform, new Vector3(0f, 2.2f, 4.5f), Vector3.one * 0.16f), "AnswerLamp");
            return root;
        }

        /// <summary>
        /// A stand-in derelict (0.5.5 §1.7) in its anchor's frame: a slumped body, its <c>TapeCaseSocket</c> on the
        /// ground against its right flank facing out of its side and its <c>HutSocket</c> on its left (06's), or one
        /// small Metal piece on its back (the Mule's track skirt, the haul bot's load); its fragment sockets in a row
        /// 12 m behind it, clear of every other E verb (06's Cell Pack, the Mule's Warm Headlamp, the haul bot's Boost
        /// Coils).
        /// </summary>
        private GameObject DerelictModel(string name, bool piece)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Named(Block(root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1f, 1.8f)), "Skeleton");
            if (piece)
            {
                Piece(root.transform, "Salvage_0_Metal", new Vector3(0f, 0.4f, -1.3f), Vector3.one * 0.6f);
            }
            else
            {
                Node("TapeCaseSocket", root.transform, new Vector3(1.1f, 0f, 0.2f)).localRotation =
                    Quaternion.Euler(0f, 90f, 0f);
                Node(HutSocket, root.transform, new Vector3(-1.9f, 0f, 0.4f)).localRotation =
                    Quaternion.Euler(0f, 90f, 0f);
            }

            FragmentSockets(root.transform, name == "Derelict_06" ? "CellPack"
                : name == "Derelict_Mule" ? "WarmHeadlamp"
                : "BoostCoils", new Vector3(-1.4f, 0f, -12f));
            Named(Block(root.transform, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.12f), "AnswerLamp");
            return root;
        }

        /// <summary>
        /// The set <paramref name="blueprint"/>'s FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt; nodes in a row from
        /// <paramref name="first"/>, 1.4 m apart (0.5.5 §1.3).
        /// </summary>
        private void FragmentSockets(Transform root, string blueprint, Vector3 first)
        {
            if (!ScanTuning.TryGetSet(blueprint, out ScanSet set))
            {
                throw new ArgumentException($"ScanTuning has no set '{blueprint}'.", nameof(blueprint));
            }

            for (int i = 0; i < set.Count; i++)
            {
                Node(ScanFragment.SocketPrefix + blueprint + "_" + i, root, first + Vector3.right * (1.4f * i));
            }
        }

        /// <summary>
        /// A stand-in for each of Art's fragments (0.5.5 §1.2): Solid and Lines boxes, the ScanTarget at their middle,
        /// and the inactive Brackets and DustOutline.
        /// </summary>
        private GameObject[] FragmentTemplates()
        {
            var fragments = new List<GameObject>();
            foreach (ScanSet set in ScanTuning.Sets)
            {
                for (int i = 0; i < set.Count; i++)
                {
                    var root = new GameObject(ScanFragment.PrefabPrefix + set.Blueprint + "_" + i);
                    root.transform.position = new Vector3(0f, -500f, 0f);
                    _created.Add(root);
                    Named(Block(root.transform, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.4f), "Solid");
                    Named(Block(root.transform, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.42f), "Lines");
                    Node("ScanTarget", root.transform, new Vector3(0f, 0.2f, 0f));
                    Named(Block(root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.5f, 0.02f, 0.5f)),
                        "Brackets").gameObject.SetActive(false);
                    Named(Block(root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.5f, 0.02f, 0.5f)),
                        "DustOutline").gameObject.SetActive(false);
                    fragments.Add(root);
                }
            }

            return fragments.ToArray();
        }

        /// <summary>
        /// A stand-in for Kenji's crashed test jumper (0.5.5 §1.3): a low frame on its root, its nose (+Z) to the
        /// chasm, and the Hover-Jump fragment sockets in a row 3 m to its side.
        /// </summary>
        private GameObject JumperTemplate()
        {
            GameObject jumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jumper.name = "JumperWreck";
            Object.DestroyImmediate(jumper.GetComponent<Collider>());
            jumper.transform.position = new Vector3(0f, -500f, 0f);
            jumper.transform.localScale = new Vector3(1.6f, 0.5f, 2.4f);
            _created.Add(jumper);
            var unscaled = Node("Sockets", jumper.transform, Vector3.zero);
            unscaled.localScale = new Vector3(1f / 1.6f, 1f / 0.5f, 1f / 2.4f);
            FragmentSockets(unscaled, "HoverJump", new Vector3(-1.4f, 0f, -3f));
            return jumper;
        }

        private static void Piece(Transform site, string name, Vector3 position, Vector3 size)
        {
            Transform piece = Named(Block(site, position, size), name);
            Node("CutPoint", piece, new Vector3(0f, 0f, -0.5f)).localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        /// <summary>
        /// A stand-in Tilly with the friend rig contract's nodes (broken and repaired), her three parts and her
        /// definition, placed east of home in the flat world (which has no craters, so the crater preference allows a
        /// flat floor); and a stand-in Bell with her rig's nodes, her three parts in the canyon's alcoves, Lumen After
        /// Dark, Vol. 1 as her fourth need and her corner by the tower, as in the content builder.
        /// </summary>
        private void BuildFriends(FriendField friends)
        {
            FriendTuning = Asset<FriendTuning>();
            BellTuning = Asset<BellTuning>();
            Tilly = Asset<FriendDefinition>();
            Tilly.Populate("tilly", FriendModel("Tilly_Broken", true), FriendModel("Tilly", false),
                FriendBodyKind.Drone, new[]
                {
                    new FriendPart("rotor", Template("Part_TillyRotor", Vector3.one * 0.3f)),
                    new FriendPart("lens", Template("Part_TillyLens", Vector3.one * 0.3f)),
                    new FriendPart("cell", Template("Part_TillyCell", Vector3.one * 0.3f)),
                }, Array.Empty<string>(), FriendHome.Lander, "FriendSocket_tilly", false,
                FriendDefinition.SpotterAbility, 2f, "tilly",
                new FriendPlacement(41, new Vector2(60f, 110f), 90f, 55f, new Vector2(0f, 4f), 10f, true));
            Bell = Asset<FriendDefinition>();
            Bell.Populate("bell", BellModel("Bell_Broken", true), BellModel("Bell", false),
                FriendBodyKind.RadioCabinet, new[]
                {
                    new FriendPart("knob", Template("Part_BellKnob", Vector3.one * 0.3f)),
                    new FriendPart("cone", Template("Part_BellCone", Vector3.one * 0.3f)),
                    new FriendPart("valve", Template("Part_BellValve", Vector3.one * 0.3f)),
                }, new[] { "after_dark_1" }, FriendHome.RadioTower, "BellCorner", true,
                FriendDefinition.RadioDialAbility, 2.5f, "bell",
                new FriendAnchorPlacement(new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(0f, 4.85f)),
                    WorldAnchorIds.CanyonAlcovePrefix, WorldAnchorIds.CanyonLanding, 40f, WorldAnchorIds.CanyonExit));
            var catalog = Asset<FriendCatalog>();
            catalog.Populate(new[] { Tilly, Bell });
            friends.Wire(catalog, FriendTuning, BellTuning, new[] { TillyPerch, BellCorner });
        }

        /// <summary>
        /// A stand-in Bell with her rig contract's nodes under their parents (Body with Lid,
        /// DialFace/Needle/DialLamp/Knob, Speaker, TapeSlot, Antenna and PartLamp_0..3; Leg_* at the hips with their
        /// Shin_*); the broken one tipped back with her lid open.
        /// </summary>
        private GameObject BellModel(string name, bool broken)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Transform body = Node(BellRig.BodyNode, root.transform, new Vector3(0f, broken ? 0.3f : 0.75f, 0f));
            Named(Block(body, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.5f)), "Cabinet");
            Transform lid = Node(BellRig.LidNode, body, new Vector3(0f, 0.68f, -0.25f));
            Named(Block(lid, new Vector3(0f, 0.02f, 0.25f), new Vector3(0.9f, 0.04f, 0.5f)), "LidPanel");
            Transform dial = Node("DialFace", body, new Vector3(0f, 0.43f, 0.262f));
            Named(Block(dial, new Vector3(0.05f, 0.05f, 0.01f), new Vector3(0.12f, 0.01f, 0.01f)), BellRig.NeedleNode);
            Named(Block(dial, Vector3.zero, new Vector3(0.3f, 0.15f, 0.01f)), BellRig.DialLampNode);
            Named(Block(dial, new Vector3(-0.31f, 0.07f, -0.006f), new Vector3(0.1f, 0.1f, 0.06f)), BellRig.KnobNode);
            Named(Block(body, new Vector3(0.2f, 0.21f, 0.28f), new Vector3(0.2f, 0.2f, 0.02f)), BellRig.SpeakerNode);
            Node(BellRig.TapeSlotNode, body, new Vector3(-0.16f, 0.21f, 0.274f));
            Named(Block(body, new Vector3(-0.46f, 0.8f, -0.15f), new Vector3(0.02f, 0.4f, 0.02f)), "Antenna");
            for (int i = 0; i < 4; i++)
            {
                Named(Block(body, new Vector3(0.1f - 0.06f * i, 0.37f, 0.262f), Vector3.one * 0.04f),
                    BellRig.PartLampPrefix + i);
            }

            for (int leg = 0; leg < BellRig.Corners.Length; leg++)
            {
                float side = leg % 2 == 0 ? -1f : 1f;
                float front = leg < 2 ? 1f : -1f;
                string corner = BellRig.Corners[leg];
                Transform hip = Node(BellRig.LegPrefix + corner, root.transform,
                    new Vector3(0.33f * side, broken ? 0.3f : 0.74f, 0.15f * front));
                Named(Block(hip, new Vector3(0f, -0.17f, 0f), new Vector3(0.04f, 0.35f, 0.04f)), "Thigh_" + corner);
                Transform shin = Node(BellRig.ShinPrefix + corner, hip,
                    new Vector3(0.13f * side, -0.35f, 0.08f * front));
                Named(Block(shin, new Vector3(0f, -0.2f, 0f), new Vector3(0.03f, 0.4f, 0.03f)), "Foot_" + corner);
                if (broken)
                {
                    hip.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                }
            }

            if (broken)
            {
                body.localRotation = Quaternion.Euler(-34f, 10f, 0f);
                lid.localRotation = Quaternion.Euler(-74f, 0f, 0f);
            }

            return root;
        }

        /// <summary>
        /// A stand-in relay mast with the contract's nodes (root at the pad centre, +Z toward home): the footing and
        /// junction box at the pad's back edge, the mast on it with its dish and lamp, the part socket and beam point
        /// on the box front; the broken one leaning back and to its left.
        /// </summary>
        private GameObject RelayModel(string name, bool broken)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Named(Block(root.transform, new Vector3(0f, 0.15f, -2.2f), new Vector3(1.9f, 0.3f, 1.9f)),
                RelayRig.BaseNode);
            Transform mast = Named(Block(root.transform, new Vector3(0f, 0.3f, -2.2f), Vector3.one), RelayRig.MastNode);
            Named(Block(mast, new Vector3(0f, 3.7f, 0f), new Vector3(0.6f, 7.4f, 0.6f)), "Core");
            Node(RelayRig.DishNode, mast, new Vector3(0f, 6.1f, 0.4f));
            Named(Block(mast, new Vector3(0f, 7.95f, 0f), Vector3.one * 0.5f), RelayRig.LampNode);
            Node(RelayRig.PartSocketNode, root.transform, new Vector3(0f, 0.78f, -1.24f));
            Node(RelayRig.BeamPointNode, root.transform, new Vector3(0f, 1.12f, -1.24f));
            Node(RelayRig.CabinetSocketNode, root.transform, new Vector3(1.75f, 0f, -2.8f));
            if (broken)
            {
                mast.localRotation = Quaternion.Euler(-9f, 0f, 10f);
            }

            return root;
        }

        /// <summary>
        /// Four stand-in supply shelves behind the lander (Art's Lander/Shelves/Shelf_1..4: a lamp, twelve slots of
        /// three crate seats) and a crate per salvage material, as the scene contributor wires them.
        /// </summary>
        private void WireShelves(HomeBase home, Transform lander)
        {
            const int racks = 4;
            const int slots = 12;
            var shelves = new Transform[racks];
            var lamps = new Renderer[racks];
            var seats = new Transform[racks * slots * HomeShelves.Seats];
            for (int r = 0; r < racks; r++)
            {
                shelves[r] = Node("Shelf_" + (r + 1), lander, new Vector3(-4f + 2.6f * r, 0f, -3.5f));
                lamps[r] = Named(Block(shelves[r], new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 0.1f, 0.1f)),
                    "Lamp").GetComponent<Renderer>();
                for (int s = 0; s < slots; s++)
                {
                    Transform slot = Node("Slot_" + s.ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                        shelves[r], new Vector3(-0.9f + 0.36f * (s % 6), 0.3f + 0.5f * (s / 6), 0f));
                    for (int k = 0; k < HomeShelves.Seats; k++)
                    {
                        seats[(r * slots + s) * HomeShelves.Seats + k] =
                            Node("Stack_" + k, slot, new Vector3(0f, 0.12f * k, 0f));
                    }
                }
            }

            var crates = new GameObject[SalvageMaterials.Count];
            crates[(int)SalvageMaterial.Metal] = Template("ShelfCrate_Metal", Vector3.one * 0.3f);
            crates[(int)SalvageMaterial.Wiring] = Template("ShelfCrate_Wiring", Vector3.one * 0.3f);
            crates[(int)SalvageMaterial.Optics] = Template("ShelfCrate_Optics", Vector3.one * 0.3f);
            home.WireShelves(shelves, lamps, seats, crates);
        }

        private static Transform Named(Transform node, string name)
        {
            node.name = name;
            return node;
        }

        /// <summary>
        /// Ro's three tapes as in the content builder: one in her case beside the terminus cache, one in the supply pod
        /// on the ledge (both behind the Hover-Jump gate) and one in her case on 06.
        /// </summary>
        private void BuildCassettes(CassetteField field)
        {
            CassetteTuning = Asset<CassetteTuning>();
            var hoverJump = new AbilityGate(true, RoverAbility.HoverJump);
            Cassettes = Asset<CassetteCatalog>();
            Cassettes.Populate(new[]
            {
                Cassette("after_dark_1", CassetteSiteRule.Anchor,
                    new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(1.2f, 1.9f)), hoverJump,
                    ContainerKinds.TapeCase),
                Cassette("dust_and_honey", CassetteSiteRule.Derelict, new AnchorSpot(WorldAnchorIds.Micro06,
                    Vector2.zero), AbilityGate.Open, ContainerKinds.TapeCase),
                Cassette("slow_orbit", CassetteSiteRule.Anchor,
                    new AnchorSpot(WorldAnchorIds.CanyonLedge, Vector2.zero), hoverJump, ContainerKinds.SupplyPod),
            });
            Transform pod = Object.Instantiate(ContainerTemplate(ContainerKinds.SupplyPod), field.transform).transform;
            pod.name = ContainerKinds.SupplyPod;
            field.Wire(Cassettes, CassetteTuning, pod);
        }

        private CassetteDefinition Cassette(string id, CassetteSiteRule site, AnchorSpot anchor, AbilityGate gate,
            string container)
        {
            var cassette = Asset<CassetteDefinition>();
            cassette.Populate(id, Template("Cassette_" + id, CassetteSize), site, anchor, gate, container);
            return cassette;
        }

        /// <summary>Ro's tin box beside the terminus, as in the content builder.</summary>
        private void BuildLogCaches(LogCacheField field)
        {
            LogCacheTuning = Asset<LogCacheTuning>();
            var ro = Asset<LogCacheDefinition>();
            ro.Populate("ro_1", Template("LogCache", new Vector3(0.5f, 0.3f, 0.35f)),
                new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(1.7f, 3f)),
                new AbilityGate(true, RoverAbility.HoverJump));
            var catalog = Asset<LogCacheCatalog>();
            catalog.Populate(new[] { ro });
            field.Wire(catalog, LogCacheTuning);
        }

        /// <summary>
        /// Stand-ins for the Lumen ruin pieces World stands under [World]/Ruins, at the ruins anchor: each a block with
        /// its Scan_1..N targets (as ScanTuning's ruin sets) above it, the workshop and the scatter with none.
        /// </summary>
        private Transform[] Ruins(Transform parent)
        {
            (string Id, Vector3 Offset, int Targets)[] pieces =
            {
                ("HabitatModules", new Vector3(-6f, 0f, 2f), 2), ("KenjiWorkshop", new Vector3(7f, 0f, 1f), 0),
                ("FallenSolarArray", new Vector3(2f, 0f, -12f), 3), ("RadioTower", new Vector3(2f, 0f, 14f), 2),
                ("ScatterSet", new Vector3(-6f, 0f, -12f), 0),
            };
            var ruins = new Transform[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform piece = Node(pieces[i].Id, parent, new Vector3(95f, 0f, 185f) + pieces[i].Offset);
                Named(Block(piece, new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 2.4f)), "Body");
                for (int t = 1; t <= pieces[i].Targets; t++)
                {
                    Node("Scan_" + t, piece, new Vector3(-1.2f + t * 0.8f, 1.3f, -1.3f));
                }

                ruins[i] = piece;
            }

            return ruins;
        }

        private void BuildStructures(BuildField field)
        {
            BuildingTuning = Asset<BuildingTuning>();
            StructureCatalog = Asset<StructureCatalog>();
            var ports = new[] { new StructurePort(new Vector3(0f, 0.2f, -1.1f), Vector3.back) };
            StructureCatalog.Populate(new[]
            {
                StructureDef(StructureIds.ChargingPad, new Vector3(2.2f, 0.4f, 2.2f), 12f, 0.25f, 0f, 120f, 0f, 0f,
                    new Recipe(4, 2, 0), 3f, ports, false),
                StructureDef(StructureIds.SolarArray, new Vector3(2.0f, 1.8f, 2.0f), 18f, 0.25f, 180f, 0f, 0f, 14f,
                    new Recipe(3, 2, 1), 3f, ports, false),
                StructureDef(StructureIds.BatteryBank, new Vector3(1.4f, 1.2f, 1.4f), 15f, 0.25f, 0f, 0f, 360f, 0f,
                    new Recipe(3, 3, 0), 2.5f, ports, true),
                StructureDef(StructureIds.PowerPylon, new Vector3(0.8f, 2.4f, 0.8f), 22f, 0.2f, 0f, 15f, 0f, 16f,
                    new Recipe(2, 1, 0), 1.5f, ports, false),
                StructureDef(StructureIds.Smelter, new Vector3(2.0f, 1.8f, 2.0f), 12f, 0.3f, 0f, 150f, 0f, 0f,
                    new Recipe(6, 2, 0), 4f, ports, true),
                StructureDef(StructureIds.Fabricator, new Vector3(2.2f, 1.6f, 1.8f), 12f, 0.3f, 0f, 140f, 0f, 0f,
                    new Recipe(5, 3, 1), 4f, ports, true),
                StructureDef(StructureIds.StorageRack, new Vector3(1.8f, 1.5f, 1.0f), 16f, 0.2f, 0f, 10f, 0f, 0f,
                    new Recipe(4, 0, 0), 2f, ports, true),
                StructureDef(StructureIds.UpgradeBay, new Vector3(2.6f, 2.0f, 2.6f), 10f, 0.35f, 0f, 160f, 0f, 0f,
                    new Recipe(6, 4, 2), 5f, ports, true),
                StructureDef(StructureIds.AntennaMast, new Vector3(1.6f, 3.5f, 1.6f), 15f, 0.3f, 0f, 90f, 0f, 0f,
                    new Recipe(4, 3, 2), 4.5f, ports, true),
                StructureDef(StructureIds.VehiclePad, new Vector3(3.2f, 0.5f, 3.2f), 10f, 0.4f, 0f, 100f, 0f, 0f,
                    new Recipe(8, 3, 1), 5f, ports, true),
            });
            field.Wire(StructureCatalog, BuildingTuning);
        }

        private StructureDefinition StructureDef(string id, Vector3 footprint, float maxSlope, float clearance,
            float produceW, float consumeW, float storeWh, float radius, Recipe cost, float seconds,
            StructurePort[] ports, bool artPending)
        {
            var def = Asset<StructureDefinition>();
            GameObject prefab = artPending ? null : StructureTemplate(id, footprint);
            def.Populate(id, StructureIds.NameKey(id), footprint, maxSlope, clearance, produceW, consumeW, storeWh,
                radius, cost, seconds, ports, prefab, artPending);
            return def;
        }

        private GameObject StructureTemplate(string id, Vector3 footprint)
        {
            var root = new GameObject("Structure_" + id);
            root.layer = Layers.Prop;
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            var stages = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float sliceH = footprint.y * 0.25f;
                Transform stage = Named(Block(root.transform, new Vector3(0f, sliceH * (i + 0.5f), 0f),
                    new Vector3(footprint.x, sliceH, footprint.z)), "Stage_" + (i + 1));
                stage.gameObject.layer = Layers.Prop;
                stages[i] = stage;
            }

            if (id == StructureIds.ChargingPad)
            {
                // The R1 pad's dock-lock bars, one per corner (PadLock raises them as 07 charges).
                for (int i = 0; i < 4; i++)
                {
                    Named(Block(root.transform, new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0.4f, i < 2 ? -0.9f : 0.9f),
                        new Vector3(0.1f, 0.8f, 0.1f)), "TotemBar_" + (i + 1));
                }
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, footprint.y * 0.5f, 0f);
            collider.size = footprint;
            var view = root.AddComponent<StructureView>();
            view.Populate(stages, new[] { stages[3].GetComponent<Renderer>() }, new Collider[] { collider });
            return root;
        }

        private GameObject FriendModel(string name, bool broken)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Transform body = Block(root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.45f, 0.3f, 0.45f));
            body.name = FriendRig.BodyNode;
            Block(root.transform, new Vector3(0f, 0.3f, 0.24f), Vector3.one * 0.12f).name = FriendRig.EyeNode;
            for (int i = 0; i < FriendRig.RotorNodes.Length; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -0.27f : 0.27f, 0.3f, i < 2 ? 0.27f : -0.27f);
                Block(root.transform, corner, new Vector3(0.16f, 0.02f, 0.04f)).name = FriendRig.RotorNodes[i];
            }

            Block(root.transform, new Vector3(0f, 0.5f, -0.1f), new Vector3(0.02f, 0.3f, 0.02f)).name =
                FriendRig.AntennaNode;
            for (int i = 0; i < 3; i++)
            {
                Block(root.transform, new Vector3(-0.1f + 0.1f * i, 0.42f, 0f), Vector3.one * 0.05f).name =
                    FriendRig.PartLampPrefix + i;
            }

            Node(FriendRig.TetherPointNode, root.transform, new Vector3(0f, 0.1f, 0f));
            if (broken)
            {
                root.transform.GetChild(0).localRotation = Quaternion.Euler(14f, 25f, 78f);
            }

            return root;
        }

        /// <summary>
        /// A stand-in for the crew's lab in the lander's cabin (docs/ARCHITECTURE.md "Contract: the lab"): Lab at the
        /// cabin floor's middle, 3.6 m up, with the three station frames round the axis at 07's lens height, +Z toward
        /// the axis, clockwise from above from the front: Chart at the front, Board behind, Keepsakes on the left.
        /// </summary>
        private static Transform BuildLab(Transform lander)
        {
            Transform lab = Node("Lab", lander, new Vector3(0f, LabFloor, 0f));
            Node("RingRail", lab, new Vector3(0f, 2.3f, 0f));
            string[] names = { "Station_Chart", "Station_Board", "Station_Keepsakes" };
            float[] bearings = { 0f, 180f, 270f };
            for (int i = 0; i < names.Length; i++)
            {
                Quaternion round = Quaternion.Euler(0f, bearings[i], 0f);
                Transform station = Node(names[i], lab, round * new Vector3(0f, LensHeight, StationRadius));
                station.localRotation = round * Quaternion.Euler(0f, 180f, 0f);
            }

            return lab;
        }

        /// <summary>
        /// A stand-in belly lift with the art contract's nodes (BellyLift on the ground at the axis; PlatformDown and
        /// PlatformUp; the Platform built at PlatformDown in its jammed tilt with RoverSpot, PadGlow and Weather_Dust;
        /// the hatch leaves, the cables and CableJam with its StitchPoint), its platform a kinematic deck as the scene
        /// contributor makes it, wired to <see cref="Lift"/>. Returns PlatformDown.
        /// </summary>
        private Transform BuildLift(Transform lander, Transform lab, Transform dock)
        {
            Transform belly = Node("BellyLift", lander, Vector3.zero);
            Transform down = Node("PlatformDown", belly, Vector3.zero);
            Transform up = Node("PlatformUp", belly, new Vector3(0f, LabFloor, 0f));
            Transform platform = Node("Platform", belly, new Vector3(0f, -0.15f, 0f));
            platform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            platform.gameObject.layer = Layers.Prop;
            var deck = platform.gameObject.AddComponent<BoxCollider>();
            deck.center = new Vector3(0f, -0.1f, 0f);
            deck.size = new Vector3(3.4f, 0.2f, 3.4f);
            var body = platform.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            Transform spot = Node("RoverSpot", platform, Vector3.zero);
            Transform pad = Block(platform, new Vector3(0f, 0.01f, 0f), new Vector3(3f, 0.02f, 3f));
            pad.name = "PadGlow";
            Transform dust = Node("Weather_Dust", platform, Vector3.zero);
            Transform hatchLeft = Node("HatchLeafL", belly, new Vector3(-0.9f, 2.6f, 0f));
            Transform hatchRight = Node("HatchLeafR", belly, new Vector3(0.9f, 2.6f, 0f));
            Transform cableLeft = Node("Cable_L", belly, new Vector3(-1.3f, 3.4f, 0f));
            Transform cableRight = Node("Cable_R", belly, new Vector3(1.3f, 3.4f, 0f));
            Transform jam = Node("CableJam", belly, new Vector3(1.3f, 0.4f, 0f));
            Transform stitch = Node("StitchPoint", jam, Vector3.zero);
            var stations = new Transform[LabInterior.StationCount];
            for (int i = 0; i < stations.Length; i++)
            {
                stations[i] = lab.Find("Station_" + (LabStation)i);
            }

            LiftTuning = Asset<LiftTuning>();
            Lift.Wire(LiftTuning, TowerTuning, platform, body, down, up, Quaternion.identity, spot,
                pad.GetComponent<Renderer>(), dust, hatchLeft, hatchRight, 1.2f, cableLeft, cableRight, 3.4f, jam,
                stitch, lab, stations, dock);
            return down;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        private static Transform Block(Transform parent, Vector3 localPosition, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = size;
            return block.transform;
        }

        /// <summary>
        /// A stand-in for one of Art's sealed containers (0.5.5 §2): a body with its <c>Lid</c> hinged at the back,
        /// holding <c>CutPoint</c> and the glowing <c>Seal</c>, and the <c>ItemSocket</c> inside.
        /// </summary>
        private GameObject ContainerTemplate(string name)
        {
            GameObject container = Template(name, new Vector3(0.9f, 0.6f, 0.7f));
            var lid = new GameObject(SealedContainer.LidPath);
            lid.transform.SetParent(container.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.6f, -0.35f);
            var cut = new GameObject("CutPoint");
            cut.transform.SetParent(lid.transform, false);
            cut.transform.localPosition = new Vector3(0f, 0f, 0.7f);
            Transform seal = Block(lid.transform, new Vector3(0f, 0f, 0.7f), new Vector3(0.8f, 0.04f, 0.04f));
            seal.name = "Seal";
            var socket = new GameObject(SealedContainer.ItemSocketPath);
            socket.transform.SetParent(container.transform, false);
            socket.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            return container;
        }

        /// <summary>
        /// A stand-in for Art's TillySled (0.5.5 §2): PartSocket_0 behind its <c>Clamp</c> (with CutPoint and Seal),
        /// PartSocket_1 under its tipped hull, and its <c>SensorPod</c> container holding PartSocket_2.
        /// </summary>
        private GameObject SledTemplate()
        {
            GameObject sled = Template(ContainerKinds.TillySled, new Vector3(1.2f, 0.4f, 0.9f));
            Transform clamp = Node("Clamp", sled.transform, new Vector3(0.38f, 0.32f, 0.18f));
            Node("CutPoint", clamp, new Vector3(0f, 0f, 0.05f));
            Block(clamp, Vector3.zero, new Vector3(0.2f, 0.04f, 0.04f)).name = "Seal";
            Node("PartSocket_0", sled.transform, new Vector3(0.38f, 0.32f, 0.06f));
            Node("PartSocket_1", sled.transform, new Vector3(-0.1f, 0.02f, -0.22f));
            Transform pod = Node("SensorPod", sled.transform, new Vector3(0.55f, 0f, 0.75f));
            Transform lid = Node(SealedContainer.LidPath, pod, new Vector3(0f, 0.3f, -0.15f));
            Node("CutPoint", lid, new Vector3(0f, 0f, 0.3f));
            Block(lid, new Vector3(0f, 0f, 0.3f), new Vector3(0.3f, 0.04f, 0.04f)).name = "Seal";
            Node("PartSocket_2", pod, new Vector3(0f, 0.1f, 0f));
            return sled;
        }

        /// <summary>
        /// A stand-in for Art's RockfallPocket (0.5.5 §2): a slumped rubble heap with an empty <c>DigSpot</c> (+Y up)
        /// in its front gap.
        /// </summary>
        private GameObject PocketTemplate()
        {
            GameObject pocket = Template(ContainerKinds.RockfallPocket, new Vector3(1.5f, 0.75f, 1f));
            Node("DigSpot", pocket.transform, new Vector3(-0.04f, 0.06f, 0.24f));
            return pocket;
        }

        /// <summary>A stand-in for Art's GaragePort with its Tray and ArmTip nodes.</summary>
        private GameObject PortTemplate()
        {
            GameObject port = Template("GaragePort", new Vector3(2.6f, 1.6f, 0.12f));
            var tray = new GameObject(GameplayVisuals.PortTrayNode);
            tray.transform.SetParent(port.transform, false);
            tray.transform.localPosition = new Vector3(-0.85f, 0.65f, 0.45f);
            var armTip = new GameObject(GameplayVisuals.PortArmTipNode);
            armTip.transform.SetParent(port.transform, false);
            armTip.transform.localPosition = new Vector3(0.9f, 1.35f, 1.32f);
            return port;
        }

        /// <summary>
        /// A stand-in for Art's hull fragment <paramref name="variant"/>: a bent plate whose root is the piece, its
        /// CutPoint on the edge facing +Z.
        /// </summary>
        private GameObject HullFragment(int variant)
        {
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "HullFragment_" + variant;
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.position = new Vector3(0f, -500f, 0f);
            plate.transform.localScale = new Vector3(1.8f, 0.6f, 0.2f);
            Node("CutPoint", plate.transform, new Vector3(0f, 0f, 0.6f));
            _created.Add(plate);
            return plate;
        }

        private GameObject Template(string name, Vector3 size)
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = name;
            Object.DestroyImmediate(template.GetComponent<Collider>());
            template.transform.position = new Vector3(0f, -500f, 0f);
            template.transform.localScale = size;
            var prefab = new GameObject(name);
            prefab.transform.position = new Vector3(0f, -500f, 0f);
            template.transform.SetParent(prefab.transform, true);
            _created.Add(prefab);
            return prefab;
        }

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private Material GlowMaterial(Shader shader, GlowRole role)
        {
            return Track(GlowMaterials.Create(shader, role));
        }

        private T Asset<T>() where T : ScriptableObject
        {
            return Track(ScriptableObject.CreateInstance<T>());
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created is Component component ? component.gameObject : (Object)created);
            return created;
        }

        private static Shader LoadShader(string path)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader missing at {path}.");
            }

            return shader;
#else
            throw new NotSupportedException("The fixture loads the shader through the editor's AssetDatabase.");
#endif
        }
    }
}
