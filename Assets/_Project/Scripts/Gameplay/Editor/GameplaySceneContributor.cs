using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The Gameplay domain's part of Main.unity: under [Gameplay], the <see cref="GameplaySystem"/> (one system,
    /// initialised after World and Rover) and its parts, wired to the tuning, content and material assets, plus the
    /// home base built from Art's prefabs (lander, museum shelf on its ShelfAnchor, the three radio tower stages on its
    /// TowerAnchor, Kenji's workbench on its WorkshopAnchor). Art's base prefabs are meshes only, so gameplay makes
    /// them solid here: a static mesh collider on each body, on the Prop layer, so 07 drives around them and the camera
    /// never slips inside. Each friend's home socket is the lander's node, or for a radio tower home (Bell's corner) a
    /// fixed empty under the TowerAnchor at the socket every tower stage carries, so stage swaps never move it; the
    /// cassette shelf stands the same way on the stages' CassetteShelfAnchor. The
    /// base is stood beside the pad here for the editor view and re-seated on the real ground at boot; relic sites, the
    /// scrap field, friends, cassettes, log caches and the relay masts (Art's RelayMast and RelayMast_Broken on the
    /// World's relay anchors, with their relay parts) are placed from the World's surface and anchors at boot. Fails
    /// loudly when a required asset or prefab node is missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
        private const int LampSockets = 4;
        private const int ShelfSlots = 6;
        private const int TowerStages = 3;
        private const int CassetteSlots = 8;
        private const string CassetteShelfAnchor = "CassetteShelfAnchor";

        /// <summary>Metres a socket may differ between tower stages and still be the same spot.</summary>
        private const float SocketTolerance = 0.01f;

        public int Order => 500;

        public void Contribute(SceneBuildContext context)
        {
            var visuals = context.LoadAsset<GameplayVisuals>(GameplayAssetPaths.Visuals);
            var scrapTuning = context.LoadAsset<ScrapTuning>(GameplayAssetPaths.ScrapTuning);
            var sonarTuning = context.LoadAsset<SonarTuning>(GameplayAssetPaths.SonarTuning);
            var relicTuning = context.LoadAsset<RelicTuning>(GameplayAssetPaths.RelicTuning);
            var placement = context.LoadAsset<RelicPlacementTuning>(GameplayAssetPaths.RelicPlacement);
            var excavationTuning = context.LoadAsset<ExcavationTuning>(GameplayAssetPaths.ExcavationTuning);
            var tetherTuning = context.LoadAsset<TetherTuning>(GameplayAssetPaths.TetherTuning);
            var baseTuning = context.LoadAsset<BaseTuning>(GameplayAssetPaths.BaseTuning);
            var towerTuning = context.LoadAsset<RadioTowerTuning>(GameplayAssetPaths.RadioTowerTuning);
            var workshopTuning = context.LoadAsset<WorkshopTuning>(GameplayAssetPaths.WorkshopTuning);
            var scrapCatalog = context.LoadAsset<ScrapCatalog>(GameplayAssetPaths.ScrapCatalog);
            var relicCatalog = context.LoadAsset<RelicCatalog>(GameplayAssetPaths.RelicCatalog);
            var radioTower = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.RadioTowerUpgrade);
            var hoverJump = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.HoverJumpUpgrade);
            var friendTuning = context.LoadAsset<FriendTuning>(GameplayAssetPaths.FriendTuning);
            var friendCatalog = context.LoadAsset<FriendCatalog>(GameplayAssetPaths.FriendCatalog);
            var cassetteTuning = context.LoadAsset<CassetteTuning>(GameplayAssetPaths.CassetteTuning);
            var cassetteCatalog = context.LoadAsset<CassetteCatalog>(GameplayAssetPaths.CassetteCatalog);
            var logCacheTuning = context.LoadAsset<LogCacheTuning>(GameplayAssetPaths.LogCacheTuning);
            var logCacheCatalog = context.LoadAsset<LogCacheCatalog>(GameplayAssetPaths.LogCacheCatalog);
            var bellTuning = context.LoadAsset<BellTuning>(GameplayAssetPaths.BellTuning);
            var relayTuning = context.LoadAsset<RelayTuning>(GameplayAssetPaths.RelayTuning);
            var relayMast = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayMast);
            var relayMastBroken = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayMastBroken);
            var relayPart = context.LoadAsset<GameObject>(GameplayAssetPaths.RelayPart);
            RequireRelayRig(relayMast);
            RequireRelayRig(relayMastBroken);
            Require(visuals.Validate(), nameof(GameplayVisuals));
            Require(scrapCatalog.Validate(), nameof(ScrapCatalog));
            Require(relicCatalog.Validate(), nameof(RelicCatalog));
            Require(radioTower.Validate(), nameof(UpgradeDefinition));
            Require(hoverJump.Validate(), nameof(UpgradeDefinition));
            Require(friendCatalog.Validate(), nameof(FriendCatalog));
            Require(cassetteCatalog.Validate(), nameof(CassetteCatalog));
            Require(logCacheCatalog.Validate(), nameof(LogCacheCatalog));

            Transform root = context.GameplayRoot.transform;
            GameObject host = context.CreateChild("GameplaySystem", root);
            var gameplay = host.AddComponent<GameplaySystem>();
            var relics = Part<RelicField>(context, host, "Relics");
            var scrap = Part<ScrapField>(context, host, "Scrap");
            var sonar = Part<SonarSystem>(context, host, "Sonar");
            var excavation = Part<ExcavationSystem>(context, host, "Excavation");
            var tether = Part<TetherSystem>(context, host, "Tether");
            var home = Part<HomeBase>(context, host, "Home");
            var tower = Part<RadioTower>(context, host, "RadioTower");
            var workshop = Part<Workshop>(context, host, "Workshop");
            var friends = Part<FriendField>(context, host, "Friends");
            var cassettes = Part<CassetteField>(context, host, "Cassettes");
            var logs = Part<LogCacheField>(context, host, "LogCaches");
            var signals = Part<SignalField>(context, host, "BellSignals");
            var relays = Part<RelayField>(context, host, "Relays");

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
            for (int i = 0; i < TowerStages; i++)
            {
                stages[i] = context.InstantiatePrefab(GameplayAssetPaths.RadioTowerStage(i + 1), towerAnchor);
                MakeSolid(stages[i].transform);
                stageLights[i] = Glow(stages[i].transform);
                beacons[i] = Child(stages[i].transform, "BeaconSocket");
            }

            Transform shelfAnchor = TowerSocket(context, CassetteShelfAnchor, towerAnchor, stages);
            Transform cassetteShelf = context.InstantiatePrefab(GameplayAssetPaths.CassetteShelf, shelfAnchor)
                .transform;
            MakeSolid(cassetteShelf);
            var tapeRack = cassetteShelf.gameObject.AddComponent<CassetteShelf>();
            tapeRack.Wire(Children(cassetteShelf, "Slot_", CassetteSlots));

            Transform workshopAnchor = Child(lander, "WorkshopAnchor");
            Transform workbench = context.InstantiatePrefab(GameplayAssetPaths.Workbench, workshopAnchor).transform;
            MakeSolid(workbench);

            relics.Wire(relicCatalog, placement, relicTuning);
            scrap.Wire(scrapTuning, scrapCatalog);
            sonar.Wire(sonarTuning);
            excavation.Wire(excavationTuning);
            tether.Wire(tetherTuning);
            home.Wire(baseTuning, baseRoot.transform, Renderer(Child(lander, "Windows")),
                Children(lander, "LampSocket_", LampSockets), shelf, Glow(shelf), Children(shelf, "Slot_", ShelfSlots));
            tower.Wire(towerTuning, radioTower, towerAnchor, stages, stageLights, beacons);
            workshop.Wire(workshopTuning, new[] { hoverJump }, workshopAnchor, Glow(workbench),
                Child(workbench, "SparkSocket"));
            var homes = new Transform[friendCatalog.Friends.Count];
            for (int i = 0; i < homes.Length; i++)
            {
                homes[i] = HomeSocket(context, friendCatalog.Friends[i], lander, towerAnchor, stages);
            }

            friends.Wire(friendCatalog, friendTuning, bellTuning, homes);
            cassettes.Wire(cassetteCatalog, cassetteTuning);
            logs.Wire(logCacheCatalog, logCacheTuning);
            relays.Wire(relayTuning, relayMast, relayMastBroken, relayPart);
            gameplay.Wire(visuals, new[] { radioTower, hoverJump }, relics, scrap, sonar, excavation, tether, home,
                tower, workshop, friends, cassettes, logs, signals, tapeRack, relays);
            context.AddSystem(gameplay);
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
