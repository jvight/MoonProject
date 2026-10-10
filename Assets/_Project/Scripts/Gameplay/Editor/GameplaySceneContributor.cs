using System;
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
    /// home base built from Art's prefabs (lander with its charging dock, museum shelf on its ShelfAnchor, the three
    /// radio tower stages with their service ports on its TowerAnchor, Kenji's Rover Bay on its WorkshopAnchor). Art's
    /// base prefabs are meshes only, so gameplay makes them solid here: a static mesh collider on each body, on the
    /// Prop layer, so 07 drives around them (and up the bay's ramp onto its floor) and the camera never slips inside.
    /// Each friend's home socket is the lander's node, or for a radio tower home (Bell's corner) a fixed empty under
    /// the TowerAnchor at the socket every tower stage carries, so stage swaps never move it; the cassette shelf stands
    /// the same way on the stages' CassetteShelfAnchor. Only the first stage is saved showing, as a fresh game starts.
    /// The base is stood beside the pad here for the editor view and re-seated on the real ground at boot; the salvage
    /// sites (Art's wrecks on the World's site anchors, the relics in their hearts) and Kestrel-3's trail bits, the
    /// friends, cassettes, log caches and the relay masts (Art's RelayMast and RelayMast_Broken on the World's relay
    /// anchors, with their relay parts) are placed from the World's surface and anchors at boot (the editor previews
    /// the wrecks and masts: <see cref="GameplayEditPreview"/>). Fails loudly when a required asset or prefab node is
    /// missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
        private const int LampSockets = 4;
        private const int ShelfSlots = 6;
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
            UpgradeDefinition[] benchKit = { hoverJump, cargoCradle, warmHeadlamp, boostCoils };
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

            GameObject baseRoot = context.CreateChild("Base", host.transform);
            Vector3 offset = baseTuning.LanderOffset;
            baseRoot.transform.SetPositionAndRotation(new Vector3(offset.x, 0f, offset.z),
                Quaternion.LookRotation(new Vector3(-offset.x, 0f, -offset.z)));
            Transform lander = context.InstantiatePrefab(GameplayAssetPaths.Lander, baseRoot.transform).transform;
            Transform shelf = context.InstantiatePrefab(GameplayAssetPaths.MuseumShelf,
                Child(lander, "ShelfAnchor")).transform;
            MakeSolid(lander);
            MakeSolid(shelf);
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
                Children(lander, "LampSocket_", LampSockets), shelf, Glow(shelf), Children(shelf, "Slot_", ShelfSlots),
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
            Transform lift = Child(lander, "LiftPlatform");
            Transform plotterRoot = Child(bay, BaseModelBuilder.PlotterName);
            Transform carriage = Child(plotterRoot, BaseModelBuilder.PenCarriageName);
            plotter.Wire(plotterRoot, carriage, Child(carriage, BaseModelBuilder.PenHeadName),
                Child(plotterRoot, BaseModelBuilder.PlotterSheetName).gameObject,
                Renderer(Child(plotterRoot, BaseModelBuilder.PlotterLampName)),
                Child(bay, BaseModelBuilder.BlueprintsName),
                BaseModelBuilder.PlotterDrawWidth, BaseModelBuilder.PlotterDrawHeight);
            progression.Wire(progressionTuning,
                PlaceSpots(dock, lander, lift, towerAnchor, shelf, bay, plotterRoot, cassetteShelf), lift);
            WireLift(liftStation, liftTuning, towerTuning, lander, lift, dock);
            gameplay.Wire(visuals, glintTuning,
                new[] { radioTower, hoverJump, cargoCradle, warmHeadlamp, boostCoils }, salvage, relics, sonar,
                excavation, tether, home, cradle, tower, workshop, friends, cassettes, logs, signals, tapeRack, relays,
                progression, plotter, liftStation);
            context.AddSystem(gameplay);
        }

        /// <summary>
        /// The places' spots in BaseProgression's place order. The dock stands at the foot of the crew's
        /// ladder (Art's ChargingDockMeshes), so the ladder is greeted there; Kenji's board is greeted at his pen
        /// plotter, which draws its sheets.
        /// </summary>
        private static Transform[] PlaceSpots(Transform dock, Transform lander, Transform lift, Transform tower,
            Transform shelf, Transform bay, Transform board, Transform rack)
        {
            return new[] { dock, lander, dock, lift, tower, shelf, bay, board, rack };
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
        /// The crew's lift on the lander: its platform made a kinematic deck 07 can ride (on the Prop layer, which 07
        /// drives on), and the art nodes the station moves and lights.
        /// </summary>
        private static void WireLift(LiftStation station, LiftTuning tuning, RadioTowerTuning looks, Transform lander,
            Transform platform, Transform dock)
        {
            if (!platform.TryGetComponent(out MeshFilter deck) || deck.sharedMesh == null)
            {
                throw new InvalidOperationException("Gameplay scene build: LiftPlatform has no mesh (M3-16 lift "
                                                    + "contract).");
            }

            Transform spot = Child(platform, "RoverSpot");
            platform.gameObject.layer = Layers.Prop;
            AddDeckColliders(platform, deck.sharedMesh.bounds, spot.localPosition.y);
            var body = platform.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            station.Wire(tuning, looks, lander, platform, body, spot,
                Child(lander, "LiftBottom"), Child(lander, "LiftTop"), Child(lander, "LiftCable_L"),
                Child(lander, "LiftCable_R"), Child(lander, BaseModelBuilder.LiftSnappedCableName),
                Renderer(Child(lander, BaseModelBuilder.WinchLampName)),
                Child(lander, BaseModelBuilder.LiftHopperMouthName), dock, BaseModelBuilder.LiftCableBuiltLength);
        }

        /// <summary>
        /// The platform's solid deck: a slab from the deck's surface (where 07's RoverSpot rests) down to the mesh's
        /// foot, and a ramp from the deck's front edge down to the tip of the mesh's front wedge, so 07 drives up onto
        /// it. The railings and carriage stay visual only: a box over the whole mesh would be a block 07 cannot climb.
        /// The deck is symmetric front to back, so its front edge mirrors its back one (the mesh's back).
        /// </summary>
        private static void AddDeckColliders(Transform platform, Bounds mesh, float surface)
        {
            float back = mesh.min.z;
            float front = -back;
            float rise = surface - mesh.min.y;
            float run = mesh.max.z - front;
            if (rise <= 0f || run <= 0f)
            {
                throw new InvalidOperationException($"Gameplay scene build: LiftPlatform's mesh has no deck under "
                                                    + $"its RoverSpot or no front ramp (bounds {mesh}).");
            }

            var slab = platform.gameObject.AddComponent<BoxCollider>();
            slab.center = new Vector3(mesh.center.x, surface - rise * 0.5f, 0.5f * (back + front));
            slab.size = new Vector3(mesh.size.x, rise, front - back);

            var ramp = new GameObject("LiftRamp").transform;
            ramp.SetParent(platform, false);
            ramp.gameObject.layer = Layers.Prop;
            float slope = Mathf.Atan2(rise, run);
            ramp.localRotation = Quaternion.Euler(slope * Mathf.Rad2Deg, 0f, 0f);
            float thickness = rise * Mathf.Cos(slope);
            Vector3 middle = new Vector3(mesh.center.x, surface - rise * 0.5f, front + run * 0.5f);
            ramp.localPosition = middle - ramp.localRotation * Vector3.up * (thickness * 0.5f);
            var incline = ramp.gameObject.AddComponent<BoxCollider>();
            incline.size = new Vector3(mesh.size.x, thickness, Mathf.Sqrt(rise * rise + run * run));
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
