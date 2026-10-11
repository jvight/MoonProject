using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The Gameplay domain's part of Main.unity: under [Gameplay], the <see cref="GameplaySystem"/> (one system,
    /// initialised after World and Rover) and its parts, wired to the tuning, content and material assets, plus the
    /// home base built from Art's prefabs (the habitat lander with its charging dock, its belly lift and the crew's lab
    /// inside with the keepsake shelf, the three radio tower stages with their service ports on its TowerAnchor,
    /// Kenji's Rover Bay on its WorkshopAnchor). Art's
    /// base prefabs are meshes only, so gameplay makes them solid here: a static mesh collider on each body, on the
    /// Prop layer, so 07 drives around them (and up the bay's ramp onto its floor) and the camera never slips inside.
    /// Each friend's home socket is the lander's node, or for a radio tower home (Bell's corner) a fixed empty under
    /// the TowerAnchor at the socket every tower stage carries, so stage swaps never move it; the cassette shelf stands
    /// the same way on the stages' CassetteShelfAnchor. Only the first stage is saved showing, as a fresh game starts.
    /// The base is stood beside the pad here for the editor view and re-seated on the real ground at boot; the salvage
    /// sites (Art's wrecks on the World's site anchors, the relics in their hearts), the friends, cassettes, log
    /// caches and the relay masts (Art's RelayMast and RelayMast_Broken on the World's relay anchors, with their
    /// relay parts) are placed from the World's surface and anchors at boot (the editor previews
    /// the wrecks and masts: <see cref="GameplayEditPreview"/>). Fails loudly when a required asset or prefab node is
    /// missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
        private const int LampSockets = 4;
        private const string KeepsakeSlotPrefix = "Slot_";
        private const string WeatherDustName = "Weather_Dust";

        /// <summary>The belly lift's drive-on ramps: how many round its rim, how steep (deg) and how thick (m).</summary>
        private const int DeckRampSegments = 16;
        private const float DeckRampSlope = 14f;
        private const float DeckRampThickness = 0.12f;
        private const int TowerStages = 3;
        private const int CassetteSlots = 8;
        private const string CassetteShelfAnchor = "CassetteShelfAnchor";
        private const int BayLamps = 2;
        private const int BayArms = 3;

        /// <summary>Metres a socket may differ between tower stages and still be the same spot.</summary>
        private const float SocketTolerance = 0.01f;

        public int Order => 500;

        public void Contribute(SceneBuildContext context)
        {
            var visuals = context.LoadAsset<GameplayVisuals>(GameplayAssetPaths.Visuals);
            var sonarTuning = context.LoadAsset<SonarTuning>(GameplayAssetPaths.SonarTuning);
            var relicTuning = context.LoadAsset<RelicTuning>(GameplayAssetPaths.RelicTuning);
            var excavationTuning = context.LoadAsset<ExcavationTuning>(GameplayAssetPaths.ExcavationTuning);
            var tetherTuning = context.LoadAsset<TetherTuning>(GameplayAssetPaths.TetherTuning);
            var baseTuning = context.LoadAsset<BaseTuning>(GameplayAssetPaths.BaseTuning);
            var towerTuning = context.LoadAsset<RadioTowerTuning>(GameplayAssetPaths.RadioTowerTuning);
            var workshopTuning = context.LoadAsset<WorkshopTuning>(GameplayAssetPaths.WorkshopTuning);
            var relicCatalog = context.LoadAsset<RelicCatalog>(GameplayAssetPaths.RelicCatalog);
            var radioTower = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.RadioTowerUpgrade);
            var hoverJump = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.HoverJumpUpgrade);
            var cargoCradle = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.CargoCradleUpgrade);
            var warmHeadlamp = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.WarmHeadlampUpgrade);
            var boostCoils = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.BoostCoilsUpgrade);
            var cellPack = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.CellPackUpgrade);
            UpgradeDefinition[] benchKit = { hoverJump, cargoCradle, warmHeadlamp, boostCoils, cellPack };
            var friendTuning = context.LoadAsset<FriendTuning>(GameplayAssetPaths.FriendTuning);
            var friendCatalog = context.LoadAsset<FriendCatalog>(GameplayAssetPaths.FriendCatalog);
            var cassetteTuning = context.LoadAsset<CassetteTuning>(GameplayAssetPaths.CassetteTuning);
            var cassetteCatalog = context.LoadAsset<CassetteCatalog>(GameplayAssetPaths.CassetteCatalog);
            var logCacheTuning = context.LoadAsset<LogCacheTuning>(GameplayAssetPaths.LogCacheTuning);
            var logCacheCatalog = context.LoadAsset<LogCacheCatalog>(GameplayAssetPaths.LogCacheCatalog);
            var bellTuning = context.LoadAsset<BellTuning>(GameplayAssetPaths.BellTuning);
            var relayTuning = context.LoadAsset<RelayTuning>(GameplayAssetPaths.RelayTuning);
            var glintTuning = context.LoadAsset<GlintTuning>(GameplayAssetPaths.GlintTuning);
            var salvageTuning = context.LoadAsset<SalvageTuning>(GameplayAssetPaths.SalvageTuning);
            var progressionTuning = context.LoadAsset<ProgressionTuning>(GameplayAssetPaths.ProgressionTuning);
            var liftTuning = context.LoadAsset<LiftTuning>(GameplayAssetPaths.LiftTuning);
            var batteryTuning = context.LoadAsset<BatteryTuning>(GameplayAssetPaths.BatteryTuning);
            var salvageCatalog = context.LoadAsset<SalvageCatalog>(GameplayAssetPaths.SalvageCatalog);
            var relayMast = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayMast);
            var relayMastBroken = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayMastBroken);
            var relayPart = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayPart);
            RequireRelayRig(relayMast);
            RequireRelayRig(relayMastBroken);
            Require(visuals.Validate(), nameof(GameplayVisuals));
            Require(salvageCatalog.Validate(), nameof(SalvageCatalog));
            Require(relicCatalog.Validate(), nameof(RelicCatalog));
            Require(radioTower.Validate(), nameof(UpgradeDefinition));
            foreach (UpgradeDefinition kit in benchKit)
            {
                Require(kit.Validate(), nameof(UpgradeDefinition));
            }
            Require(friendCatalog.Validate(), nameof(FriendCatalog));
            Require(cassetteCatalog.Validate(), nameof(CassetteCatalog));
            Require(logCacheCatalog.Validate(), nameof(LogCacheCatalog));

            Transform root = context.GameplayRoot.transform;
            GameObject host = context.CreateChild("GameplaySystem", root);
            var gameplay = host.AddComponent<GameplaySystem>();
            var salvage = Part<SalvageField>(context, host, "Salvage");
            var relics = Part<RelicField>(context, host, "Relics");
            var sonar = Part<SonarSystem>(context, host, "Sonar");
            var excavation = Part<ExcavationSystem>(context, host, "Excavation");
            var tether = Part<TetherSystem>(context, host, "Tether");
            var home = Part<HomeBase>(context, host, "Home");
            var cradle = Part<CargoCradle>(context, host, "CargoCradle");
            var tower = Part<RadioTower>(context, host, "RadioTower");
            var liftStation = Part<LiftStation>(context, host, "CrewLift");
            var workshop = Part<Workshop>(context, host, "Workshop");
            var friends = Part<FriendField>(context, host, "Friends");
            var cassettes = Part<CassetteField>(context, host, "Cassettes");
            var logs = Part<LogCacheField>(context, host, "LogCaches");
            var signals = Part<BellSignalField>(context, host, "BellSignals");
            var relays = Part<RelayField>(context, host, "Relays");
            var progression = Part<BaseProgression>(context, host, "Progression");
            var plotter = Part<BlueprintPlotter>(context, host, "Plotter");
            var battery = Part<BatteryField>(context, host, "Battery");

            GameObject baseRoot = context.CreateChild("Base", host.transform);
            Vector3 offset = baseTuning.LanderOffset;
            baseRoot.transform.SetPositionAndRotation(new Vector3(offset.x, 0f, offset.z),
                Quaternion.LookRotation(new Vector3(-offset.x, 0f, -offset.z)));
            Transform lander = context.InstantiatePrefab(GameplayAssetPaths.Lander, baseRoot.transform).transform;
            MakeSolid(lander);
            Transform lab = Child(lander, BaseModelBuilder.LabName);
            Transform keepsakes = Station(lab, LabStation.Keepsakes);
            Transform towerAnchor = Child(lander, "TowerAnchor");
            var stages = new GameObject[TowerStages];
            var stageLights = new Renderer[TowerStages];
            var beacons = new Transform[TowerStages];
            var hoppers = new Transform[TowerStages];
            var hatches = new Transform[TowerStages];
            for (int i = 0; i < TowerStages; i++)
            {
                stages[i] = context.InstantiatePrefab(GameplayAssetPaths.RadioTowerStage(i + 1), towerAnchor);
                MakeSolid(stages[i].transform);
                stageLights[i] = Glow(stages[i].transform);
                beacons[i] = Child(stages[i].transform, "BeaconSocket");
                hoppers[i] = Child(stages[i].transform, "HopperMouth");
                hatches[i] = Child(stages[i].transform, "ServiceHatch");
            }

            Transform shelfAnchor = TowerSocket(context, CassetteShelfAnchor, towerAnchor, stages);
            Transform cassetteShelf = context.InstantiatePrefab(GameplayAssetPaths.CassetteShelf, shelfAnchor)
                .transform;
            MakeSolid(cassetteShelf);
            var tapeRack = cassetteShelf.gameObject.AddComponent<CassetteShelf>();
            tapeRack.Wire(Children(cassetteShelf, "Slot_", CassetteSlots));

            Transform bay = context.InstantiatePrefab(GameplayAssetPaths.RoverBay, Child(lander, "WorkshopAnchor"))
                .transform;
            MakeSolid(bay);
            var bayLamps = new Renderer[BayLamps];
            for (int i = 0; i < BayLamps; i++)
            {
                bayLamps[i] = Renderer(Child(bay, "Lamp_" + i));
            }

            var armYaws = new Transform[BayArms];
            var armUppers = new Transform[BayArms];
            var armLowers = new Transform[BayArms];
            var armTips = new Transform[BayArms];
            var weldSockets = new Transform[BayArms];
            for (int i = 0; i < BayArms; i++)
            {
                armYaws[i] = Child(Child(bay, "Arm_" + i), "Yaw");
                armUppers[i] = Child(armYaws[i], "Upper");
                armLowers[i] = Child(armUppers[i], "Lower");
                armTips[i] = Child(armLowers[i], "Tip");
                weldSockets[i] = Child(armTips[i], "SparkSocket");
            }

            Transform floorLift = Child(Child(bay, "FloorArm"), "FloorLift");

            salvage.Wire(salvageTuning, salvageCatalog);
            relics.Wire(relicCatalog, relicTuning);
            sonar.Wire(sonarTuning);
            excavation.Wire(excavationTuning);
            tether.Wire(tetherTuning);
            home.Wire(baseTuning, baseRoot.transform, Renderer(Child(lander, "Windows")),
                Children(lander, "LampSocket_", LampSockets), keepsakes, Cubbies(keepsakes, relicCatalog),
                Child(lander, "DockAnchor"), Renderer(Child(lander, "DockGlow")));
            tower.Wire(towerTuning, radioTower, towerAnchor, stages, stageLights, beacons, hoppers, hatches);
            workshop.Wire(workshopTuning, benchKit, bay, Child(bay, "Turntable"), Child(bay, "HopperMouth"), bayLamps,
                Renderer(Child(bay, "BaySign")), weldSockets);
            workshop.WireArms(armYaws, armUppers, armLowers, armTips, floorLift, Child(floorLift, "FloorTip"));
            var homes = new Transform[friendCatalog.Friends.Count];
            for (int i = 0; i < homes.Length; i++)
            {
                homes[i] = HomeSocket(context, friendCatalog.Friends[i], lander, towerAnchor, stages);
            }

            ShowFirstStage(stages);

            friends.Wire(friendCatalog, friendTuning, bellTuning, homes);
            cassettes.Wire(cassetteCatalog, cassetteTuning);
            logs.Wire(logCacheCatalog, logCacheTuning);
            relays.Wire(relayTuning, relayMast, relayMastBroken, relayPart);
            Transform dock = Child(lander, "DockAnchor");
            Transform belly = Child(lander, BaseModelBuilder.BellyLiftName);
            Transform lift = Child(belly, BaseModelBuilder.PlatformDownName);
            Transform plotterRoot = Child(bay, BaseModelBuilder.PlotterName);
            Transform carriage = Child(plotterRoot, BaseModelBuilder.PenCarriageName);
            plotter.Wire(plotterRoot, carriage, Child(carriage, BaseModelBuilder.PenHeadName),
                Child(plotterRoot, BaseModelBuilder.PlotterSheetName).gameObject,
                Renderer(Child(plotterRoot, BaseModelBuilder.PlotterLampName)),
                Child(bay, BaseModelBuilder.BlueprintsName),
                BaseModelBuilder.PlotterDrawWidth, BaseModelBuilder.PlotterDrawHeight);
            progression.Wire(progressionTuning, PlaceSpots(dock, lander, lift, towerAnchor, bay, cassetteShelf));
            WireLift(liftStation, liftTuning, towerTuning, belly, lab, dock);
            gameplay.Wire(visuals, glintTuning,
                new[] { radioTower, hoverJump, cargoCradle, warmHeadlamp, boostCoils, cellPack }, salvage, relics,
                sonar,
                excavation, tether, home, cradle, tower, workshop, friends, cassettes, logs, signals, tapeRack, relays,
                progression, plotter, liftStation);
            battery.Wire(batteryTuning);
            gameplay.WireBattery(battery);
            context.AddSystem(gameplay);
        }

        /// <summary>
        /// The places' spots in BaseProgression's place order. The dock stands at the foot of the crew's
        /// ladder (Art's ChargingDockMeshes), so the ladder is greeted there; the lab is greeted over the belly lift's
        /// shaft, once 07 is up in it.
        /// </summary>
        private static Transform[] PlaceSpots(Transform dock, Transform lander, Transform lift, Transform tower,
            Transform bay, Transform rack)
        {
            return new[] { dock, lander, dock, lift, tower, lift, bay, rack };
        }

        /// <summary>The lab's station frame <paramref name="station"/> (Lab/Station_&lt;station&gt;).</summary>
        private static Transform Station(Transform lab, LabStation station)
        {
            return Child(lab, BaseModelBuilder.StationPrefix + station);
        }

        /// <summary>
        /// The keepsake shelf's cubbies, Slot_0 up in order; there must be one for every relic (every keepsake has its
        /// outline).
        /// </summary>
        private static Transform[] Cubbies(Transform keepsakes, RelicCatalog relics)
        {
            var cubbies = new List<Transform>();
            for (Transform cubby = keepsakes.Find(KeepsakeSlotPrefix + 0); cubby != null;
                 cubby = keepsakes.Find(KeepsakeSlotPrefix + cubbies.Count))
            {
                cubbies.Add(cubby);
            }

            if (cubbies.Count < relics.Relics.Count)
            {
                throw new InvalidOperationException($"Gameplay scene build: the keepsake shelf has {cubbies.Count} " +
                                                    $"cubbies for {relics.Relics.Count} relics (lab contract).");
            }

            return cubbies.ToArray();
        }

        private static Transform HomeSocket(SceneBuildContext context, FriendDefinition friend, Transform lander,
            Transform towerAnchor, GameObject[] stages)
        {
            switch (friend.Home)
            {
                case FriendHome.Lander:
                    return Child(lander, friend.HomeSocket);
                case FriendHome.RadioTower:
                    return TowerSocket(context, friend.HomeSocket, towerAnchor, stages);
                default:
                    throw new InvalidOperationException($"Gameplay scene build: friend '{friend.Id}' has an unknown " +
                                                        $"home {friend.Home}.");
            }
        }

        /// <summary>
        /// A fixed empty under the tower anchor at the socket <paramref name="name"/> that every tower stage carries
        /// (they scale while swapping, the socket must not). Throws when the stages disagree about where it is.
        /// </summary>
        private static Transform TowerSocket(SceneBuildContext context, string name, Transform towerAnchor,
            GameObject[] stages)
        {
            Transform first = Child(stages[0].transform, name);
            for (int i = 1; i < stages.Length; i++)
            {
                Transform other = Child(stages[i].transform, name);
                if (Vector3.Distance(first.position, other.position) > SocketTolerance)
                {
                    throw new InvalidOperationException($"Gameplay scene build: '{name}' stands elsewhere on " +
                                                        $"{stages[i].name} than on {stages[0].name} (Bell contract, " +
                                                        "docs/ARCHITECTURE.md).");
                }
            }

            Transform socket = context.CreateChild(name, towerAnchor).transform;
            socket.SetPositionAndRotation(first.position, first.rotation);
            return socket;
        }

        /// <summary>
        /// Saves only the first tower stage shown, as a fresh game starts (the radio tower shows its level at boot), so
        /// the stages never stand inside each other in the editor. Their sockets are read while all of them stand.
        /// </summary>
        private static void ShowFirstStage(GameObject[] stages)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                stages[i].SetActive(i == 0);
            }
        }

        /// <summary>Fails the build when a relay mast prefab breaks its node contract (naming the node).</summary>
        private static void RequireRelayRig(GameObject prefab)
        {
            _ = new RelayRig(prefab);
        }

        /// <summary>A static collider of the prefab's own body mesh, on the Prop layer (props are solid).</summary>
        private static void MakeSolid(Transform prefab)
        {
            if (!prefab.TryGetComponent(out MeshFilter body) || body.sharedMesh == null)
            {
                throw new InvalidOperationException($"Gameplay scene build: {prefab.name} has no body mesh on its " +
                                                    "root (M2 content contract).");
            }

            prefab.gameObject.layer = Layers.Prop;
            prefab.gameObject.AddComponent<MeshCollider>().sharedMesh = body.sharedMesh;
        }

        /// <summary>
        /// The crew's belly lift and the lab (docs/ARCHITECTURE.md "Contract: the lab"): its platform made a kinematic
        /// deck 07 drives onto and rides (on the Prop layer, which 07 drives on), and the art nodes the station moves.
        /// </summary>
        private static void WireLift(LiftStation station, LiftTuning tuning, RadioTowerTuning looks, Transform belly,
            Transform lab, Transform dock)
        {
            Transform platform = Child(belly, BaseModelBuilder.PlatformName);
            if (!platform.TryGetComponent(out MeshFilter deck) || deck.sharedMesh == null)
            {
                throw new InvalidOperationException("Gameplay scene build: BellyLift/Platform has no mesh (lab "
                                                    + "contract).");
            }

            platform.gameObject.layer = Layers.Prop;
            Transform down = Child(belly, BaseModelBuilder.PlatformDownName);
            AddDeckCollider(platform, deck.sharedMesh.bounds);
            AddDeckRamps(platform, deck.sharedMesh.bounds, down.localPosition.y);
            var body = platform.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            Transform cableJam = Child(belly, BaseModelBuilder.CableJamName);
            Transform reader = Station(lab, LabStation.Reader);
            var stations = new Transform[LabInterior.StationCount];
            for (int i = 0; i < stations.Length; i++)
            {
                stations[i] = Station(lab, (LabStation)i);
            }

            station.Wire(tuning, looks, platform, body, down, Child(belly, BaseModelBuilder.PlatformUpName), BaseModelBuilder.BellyPlatformLevel,
                Child(platform, BaseModelBuilder.RoverSpotName), Renderer(Child(platform, BaseModelBuilder.PadGlowName)),
                Child(platform, WeatherDustName), Child(belly, BaseModelBuilder.HatchLeafLeftName),
                Child(belly, BaseModelBuilder.HatchLeafRightName), BaseModelBuilder.HatchLeafTravel,
                Child(belly, BaseModelBuilder.CableLeftName), Child(belly, BaseModelBuilder.CableRightName),
                BaseModelBuilder.BellyCableBuiltLength, cableJam, Child(cableJam, BaseModelBuilder.StitchPointName),
                lab, stations, Child(reader, BaseModelBuilder.ReaderSlotName),
                Child(reader, BaseModelBuilder.ReaderReelName), Child(reader, BaseModelBuilder.ReaderTapeName), dock);
        }

        /// <summary>
        /// The platform's solid deck: a slab under its top (the pivot) as wide as its mesh, so 07 drives straight on
        /// from the ground it rests flush with. The rim lamps stay visual only.
        /// </summary>
        private static void AddDeckCollider(Transform platform, Bounds mesh)
        {
            float depth = -mesh.min.y;
            if (depth <= 0f)
            {
                throw new InvalidOperationException("Gameplay scene build: BellyLift/Platform's mesh does not hang "
                                                    + $"under its pivot (bounds {mesh}; lab contract: pivot at the "
                                                    + "middle of its top).");
            }

            var slab = platform.gameObject.AddComponent<BoxCollider>();
            slab.center = new Vector3(mesh.center.x, -depth * 0.5f, mesh.center.z);
            slab.size = new Vector3(mesh.size.x, depth, mesh.size.z);
        }

        /// <summary>
        /// A ring of gentle ramps round the platform's rim, from the ground up to its top (<paramref name="rise"/> m on
        /// its stops), so 07 drives on from any side. They are invisible and ride with the platform, which the
        /// turntable turns; up in the lab only the held 07 touches them.
        /// </summary>
        private static void AddDeckRamps(Transform platform, Bounds mesh, float rise)
        {
            if (rise <= 0f)
            {
                return;
            }

            float radius = Mathf.Max(mesh.extents.x, mesh.extents.z);
            float slope = DeckRampSlope * Mathf.Deg2Rad;
            float run = rise / Mathf.Tan(slope);
            float length = Mathf.Sqrt(rise * rise + run * run);
            float width = 2f * (radius + run) * Mathf.Tan(Mathf.PI / DeckRampSegments);
            for (int i = 0; i < DeckRampSegments; i++)
            {
                var ramp = new GameObject("DeckRamp_" + i).transform;
                ramp.SetParent(platform, false);
                ramp.gameObject.layer = Layers.Prop;
                Quaternion round = Quaternion.Euler(0f, 360f * i / DeckRampSegments, 0f);
                ramp.localRotation = round * Quaternion.Euler(DeckRampSlope, 0f, 0f);
                Vector3 middle = round * new Vector3(0f, -rise * 0.5f, radius + run * 0.5f);
                ramp.localPosition = middle - ramp.localRotation * Vector3.up * (DeckRampThickness * 0.5f);
                var incline = ramp.gameObject.AddComponent<BoxCollider>();
                incline.size = new Vector3(width, DeckRampThickness, length);
            }
        }

        private static T Part<T>(SceneBuildContext context, GameObject host, string name) where T : Component
        {
            return context.CreateChild(name, host.transform).AddComponent<T>();
        }

        private static Transform Child(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"Gameplay scene build: {parent.name} has no '{name}' node " +
                                                "(M2 content contract, docs/ARCHITECTURE.md).");
        }

        private static Transform[] Children(Transform parent, string prefix, int count)
        {
            var children = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                children[i] = Child(parent, prefix + i);
            }

            return children;
        }

        /// <summary>The prefab's "Lights" glow renderer.</summary>
        private static Renderer Glow(Transform prefab)
        {
            return Renderer(Child(prefab, "Lights"));
        }

        private static Renderer Renderer(Transform node)
        {
            if (!node.TryGetComponent(out Renderer renderer))
            {
                throw new InvalidOperationException($"Gameplay scene build: '{node.name}' has no renderer.");
            }

            return renderer;
        }

        private static void Require(string problem, string asset)
        {
            if (problem != null)
            {
                throw new InvalidOperationException($"Gameplay scene build: {asset} {problem}. " +
                                                    "Run the Gameplay builders (MoonProject/Build/Build All).");
            }
        }
    }
}
