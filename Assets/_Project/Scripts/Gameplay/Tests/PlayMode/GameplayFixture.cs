using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// A complete gameplay stack built in test code: a flat world with canyon and relay anchors, a fake 07, test
    /// content (relic, scrap, Tilly, Bell, cassette, log cache, tape rack, relay mast and relay part stand-ins in place
    /// of the Art prefabs, with the contracts' node names; default tuning; SoftGlow materials) and the real gameplay
    /// components, wired the way the scene contributor wires them and booted through GameBootstrap with a private save
    /// slot.
    /// </summary>
    public sealed class GameplayFixture : IDisposable
    {
        public const string ShaderPath = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShaderPath = "Assets/_Project/Shaders/Gameplay/Glint.shader";

        public static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));

        private static readonly string[] RelicIds =
        {
            "cassette_player", "rubber_duck", "golden_record", "astronaut_boot", "teapot", "garden_gnome",
        };

        private static readonly RelicPlacementBand[] RelicBands =
        {
            RelicPlacementBand.Onboarding, RelicPlacementBand.Onboarding, RelicPlacementBand.RimView,
            RelicPlacementBand.Wanderer, RelicPlacementBand.Wanderer, RelicPlacementBand.Wanderer,
        };

        private static readonly float[] RelicMasses = { 6f, 3f, 5f, 9f, 7f, 14f };

        private readonly List<Object> _created = new List<Object>();
        private readonly InputActionAsset _controls;

        private GameplayFixture(InputActionAsset controls, string saveSlot)
        {
            _controls = controls;
            SaveSlot = saveSlot;
        }

        public string SaveSlot { get; }

        public FlatWorldSystem World { get; private set; }

        public FakeRoverSystem Rover { get; private set; }

        public GameplaySystem Gameplay { get; private set; }

        public GameBootstrap Bootstrap { get; private set; }

        public EventRecorder Events { get; private set; }

        public ScrapTuning ScrapTuning { get; private set; }

        public GlintTuning GlintTuning { get; private set; }

        public SonarTuning SonarTuning { get; private set; }

        public RelicTuning RelicTuning { get; private set; }

        public ExcavationTuning ExcavationTuning { get; private set; }

        public TetherTuning TetherTuning { get; private set; }

        public BaseTuning BaseTuning { get; private set; }

        public RadioTowerTuning TowerTuning { get; private set; }

        public UpgradeDefinition RadioTowerUpgrade { get; private set; }

        public WorkshopTuning WorkshopTuning { get; private set; }

        public UpgradeDefinition HoverJumpUpgrade { get; private set; }

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

        /// <summary>Builds and boots everything; 07 starts at the base facing +Z.</summary>
        public static GameplayFixture Boot(InputActionAsset controls, string saveSlot = null)
        {
            var fixture = new GameplayFixture(controls, saveSlot ?? BootstrapHarness.NewTestSlot());
            fixture.Build();
            return fixture;
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

        private void Build()
        {
            World = Track(FlatWorldSystem.Create());
            Rover = Track(FakeRoverSystem.Create(Vector3.zero, 0f));

            ScrapTuning = Asset<ScrapTuning>();
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
                new UpgradeLevel(15, 110f, 1.25f), new UpgradeLevel(40, 170f, 1.5f), new UpgradeLevel(80, 260f, 1.8f),
            });
            WorkshopTuning = Asset<WorkshopTuning>();
            HoverJumpUpgrade = Asset<UpgradeDefinition>();
            HoverJumpUpgrade.Populate("rover.hover_jump", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(150, RoverAbility.HoverJump),
            });
            var placement = Asset<RelicPlacementTuning>();

            var relicCatalog = Asset<RelicCatalog>();
            var definitions = new RelicDefinition[RelicIds.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i] = Asset<RelicDefinition>();
                definitions[i].Populate(RelicIds[i], RelicMasses[i],
                    Template("Relic_" + RelicIds[i], new Vector3(0.7f, 0.6f, 0.5f)), i, RelicBands[i]);
            }

            relicCatalog.Populate(definitions);
            var scrapCatalog = Asset<ScrapCatalog>();
            scrapCatalog.Populate(new[]
            {
                new ScrapVariant(Template("Scrap_A", Vector3.one * 0.3f), 1, 3f),
                new ScrapVariant(Template("Scrap_B", Vector3.one * 0.45f), 2, 2f),
            });

            var visuals = Asset<GameplayVisuals>();
            Shader shader = LoadShader(ShaderPath);
            visuals.Populate(GlowMaterial(shader, GlowRole.SonarRing), GlowMaterial(shader, GlowRole.SiteRing),
                GlowMaterial(shader, GlowRole.SitePillar), GlowMaterial(shader, GlowRole.TractorBeam),
                GlowMaterial(shader, GlowRole.TetherBeam), GlowMaterial(shader, GlowRole.Flash),
                GlowMaterial(shader, GlowRole.RelicHalo), GlowMaterial(shader, GlowRole.Dust),
                GlowMaterial(shader, GlowRole.WarmRing), GlowMaterial(shader, GlowRole.WarmGlow),
                Track(GlintMaterials.Create(LoadShader(GlintShaderPath))),
                Track(GlintMaterials.CreatePart(LoadShader(GlintShaderPath))),
                GlowMaterial(shader, GlowRole.FriendPillar), GlowMaterial(shader, GlowRole.Spark),
                GlowMaterial(shader, GlowRole.HomeHalo), GlowMaterial(shader, GlowRole.LinkPulse));

            var root = new GameObject("[Gameplay]");
            root.SetActive(false);
            Gameplay = root.AddComponent<GameplaySystem>();
            var relics = Child<RelicField>(root, "Relics");
            var scrap = Child<ScrapField>(root, "Scrap");
            var sonar = Child<SonarSystem>(root, "Sonar");
            var excavation = Child<ExcavationSystem>(root, "Excavation");
            var tether = Child<TetherSystem>(root, "Tether");
            var home = Child<HomeBase>(root, "Home");
            var tower = Child<RadioTower>(root, "RadioTower");
            var workshop = Child<Workshop>(root, "Workshop");
            var friends = Child<FriendField>(root, "Friends");
            var cassettes = Child<CassetteField>(root, "Cassettes");
            var logs = Child<LogCacheField>(root, "LogCaches");
            var signals = Child<SignalField>(root, "BellSignals");
            var relays = Child<RelayField>(root, "Relays");
            CassetteShelf shelf = BuildBase(root.transform, home, tower, workshop);
            BuildFriends(friends);
            BuildCassettes(cassettes);
            BuildLogCaches(logs);
            RelayTuning = Asset<RelayTuning>();
            relays.Wire(RelayTuning, RelayModel("RelayMast", false), RelayModel("RelayMast_Broken", true),
                Template("Part_RelayModule", Vector3.one * 0.3f));
            relics.Wire(relicCatalog, placement, RelicTuning);
            scrap.Wire(ScrapTuning, scrapCatalog);
            sonar.Wire(SonarTuning);
            excavation.Wire(ExcavationTuning);
            tether.Wire(TetherTuning);
            Gameplay.Wire(visuals, GlintTuning, new[] { RadioTowerUpgrade, HoverJumpUpgrade }, relics, scrap, sonar,
                excavation, tether, home, tower, workshop, friends, cassettes, logs, signals, shelf, relays);
            root.SetActive(true);

            Bootstrap = BootstrapHarness.Create(_controls, SaveSlot, World, Rover, Gameplay);
            Events = new EventRecorder(Bootstrap.Context.Events);
        }

        /// <summary>
        /// A stand-in for Art's lander, shelf, tower stages and tape rack with the contract's node names and positions,
        /// wired the way the scene contributor wires the real prefabs.
        /// </summary>
        private CassetteShelf BuildBase(Transform parent, HomeBase home, RadioTower tower, Workshop workshop)
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

            Transform shelf = Node("MuseumShelf", Node("ShelfAnchor", lander, new Vector3(6f, 0f, 1.2f)),
                Vector3.zero);
            Transform shelfLights = Block(shelf, new Vector3(0f, 2.4f, 0f), new Vector3(4.2f, 0.1f, 0.6f));
            var slots = new Transform[6];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Node("Slot_" + i, shelf, new Vector3(-1.37f + 1.37f * (i % 3), i < 3 ? 0.38f : 1.78f,
                    0.04f));
            }

            Transform anchor = Node("TowerAnchor", lander, new Vector3(-6f, 0f, -1f));
            float[] heights = { 3.78f, 6.55f, 10f };
            var stages = new GameObject[3];
            var lights = new Renderer[3];
            var beacons = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Transform stage = Node("RadioTower_L" + (i + 1), anchor, Vector3.zero);
                Block(stage, new Vector3(0f, heights[i] * 0.5f, 0f), new Vector3(0.6f, heights[i], 0.6f));
                lights[i] = Block(stage, new Vector3(0f, heights[i] * 0.8f, 0.35f), new Vector3(0.3f, 0.3f, 0.05f))
                    .GetComponent<Renderer>();
                beacons[i] = Node("BeaconSocket", stage, new Vector3(0f, heights[i], 0f));
                stages[i] = stage.gameObject;
            }

            home.Wire(BaseTuning, baseRoot, windows.GetComponent<Renderer>(), sockets, shelf,
                shelfLights.GetComponent<Renderer>(), slots);
            tower.Wire(TowerTuning, RadioTowerUpgrade, anchor, stages, lights, beacons);
            Transform workbench = Node("Workbench", Node("WorkshopAnchor", lander, new Vector3(12.5f, 0f, -2f)),
                Vector3.zero);
            Block(workbench, new Vector3(0f, 0.47f, 0f), new Vector3(2.3f, 0.95f, 0.9f));
            Transform lamp = Block(workbench, new Vector3(0.55f, 2.02f, 0.18f), Vector3.one * 0.12f);
            workshop.Wire(WorkshopTuning, new[] { HoverJumpUpgrade }, workbench.parent, lamp.GetComponent<Renderer>(),
                Node("SparkSocket", workbench, new Vector3(-0.82f, 1.14f, 0.32f)));
            TillyPerch = Node("FriendSocket_tilly", lander, new Vector3(-1.6f, 3.3f, 0.9f));
            BellCorner = Node("BellCorner", anchor, new Vector3(-4.9f, 0f, 0.4f));
            BellCorner.localRotation = Quaternion.Euler(0f, 74f, 0f);
            Transform rack = Node("CassetteShelf", Node("CassetteShelfAnchor", anchor, new Vector3(-4.6f, 0f, -2.75f)),
                Vector3.zero);
            rack.parent.localRotation = Quaternion.Euler(0f, 60f, 0f);
            Block(rack, new Vector3(0f, 0.6f, -0.05f), new Vector3(0.6f, 1.2f, 0.1f)).name = "Rack";
            ShelfSlots = new Transform[8];
            for (int i = 0; i < ShelfSlots.Length; i++)
            {
                ShelfSlots[i] = Node("Slot_" + i, rack, new Vector3(i % 2 == 0 ? 0.2f : -0.2f,
                    1.033f - 0.27f * (i / 2), 0.01f));
            }

            var tapes = rack.gameObject.AddComponent<CassetteShelf>();
            tapes.Wire(ShelfSlots);
            return tapes;
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
                new FriendPlacement(41, new Vector2(60f, 110f), 90f, 55f, new Vector2(0f, 4f), 10f, true,
                    new Vector2(30f, 60f)));
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
        /// A stand-in Bell with her rig contract's nodes under their parents (Body with Lid, DialFace/Needle/DialLamp,
        /// Speaker, TapeSlot, Antenna and PartLamp_0..3; Leg_* at the hips with their Shin_*); the broken one tipped
        /// back with her lid open.
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
            if (broken)
            {
                mast.localRotation = Quaternion.Euler(-9f, 0f, 10f);
            }

            return root;
        }

        private static Transform Named(Transform node, string name)
        {
            node.name = name;
            return node;
        }

        /// <summary>
        /// Ro's three tapes as in the content builder: one beside the terminus cache, one on the ledge (both behind the
        /// Hover-Jump gate) and one planned in the basin.
        /// </summary>
        private void BuildCassettes(CassetteField field)
        {
            CassetteTuning = Asset<CassetteTuning>();
            var hoverJump = new AbilityGate(true, RoverAbility.HoverJump);
            Cassettes = Asset<CassetteCatalog>();
            Cassettes.Populate(new[]
            {
                Cassette("after_dark_1", CassetteSiteRule.Anchor,
                    new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(1.2f, 1.9f)), hoverJump),
                Cassette("dust_and_honey", CassetteSiteRule.BasinPlanner, new AnchorSpot(string.Empty, Vector2.zero),
                    AbilityGate.Open),
                Cassette("slow_orbit", CassetteSiteRule.Anchor,
                    new AnchorSpot(WorldAnchorIds.CanyonLedge, Vector2.zero), hoverJump),
            });
            field.Wire(Cassettes, CassetteTuning);
        }

        private CassetteDefinition Cassette(string id, CassetteSiteRule site, AnchorSpot anchor, AbilityGate gate)
        {
            var cassette = Asset<CassetteDefinition>();
            cassette.Populate(id, Template("Cassette_" + id, new Vector3(0.35f, 0.22f, 0.07f)), site, anchor, 73,
                gate);
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
