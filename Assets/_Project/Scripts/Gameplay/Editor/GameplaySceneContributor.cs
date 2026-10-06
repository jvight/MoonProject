using System;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The Gameplay domain's part of Main.unity: under [Gameplay], the <see cref="GameplaySystem"/> (one system,
    /// initialised after World, Rover and Audio) and its parts, wired to the tuning, content and material assets, plus
    /// the home base built from Art's prefabs (lander, museum shelf on its ShelfAnchor, the three radio tower stages on
    /// its TowerAnchor). The base is stood beside the pad here for the editor view and re-seated on the real ground
    /// at boot; relic sites and the scrap field are planned from the World's surface at boot. Fails loudly when a
    /// required asset or prefab node is missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
        private const int LampSockets = 4;
        private const int ShelfSlots = 6;
        private const int TowerStages = 3;

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
            var scrapCatalog = context.LoadAsset<ScrapCatalog>(GameplayAssetPaths.ScrapCatalog);
            var relicCatalog = context.LoadAsset<RelicCatalog>(GameplayAssetPaths.RelicCatalog);
            var radioTower = context.LoadAsset<UpgradeDefinition>(GameplayAssetPaths.RadioTowerUpgrade);
            Require(visuals.Validate(), nameof(GameplayVisuals));
            Require(scrapCatalog.Validate(), nameof(ScrapCatalog));
            Require(relicCatalog.Validate(), nameof(RelicCatalog));
            Require(radioTower.Validate(), nameof(UpgradeDefinition));

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

            GameObject baseRoot = context.CreateChild("Base", host.transform);
            Vector3 offset = baseTuning.LanderOffset;
            baseRoot.transform.SetPositionAndRotation(new Vector3(offset.x, 0f, offset.z),
                Quaternion.LookRotation(new Vector3(-offset.x, 0f, -offset.z)));
            Transform lander = context.InstantiatePrefab(GameplayAssetPaths.Lander, baseRoot.transform).transform;
            Transform shelf = context.InstantiatePrefab(GameplayAssetPaths.MuseumShelf,
                Child(lander, "ShelfAnchor")).transform;
            Transform towerAnchor = Child(lander, "TowerAnchor");
            var stages = new GameObject[TowerStages];
            var stageLights = new Renderer[TowerStages];
            var beacons = new Transform[TowerStages];
            for (int i = 0; i < TowerStages; i++)
            {
                stages[i] = context.InstantiatePrefab(GameplayAssetPaths.RadioTowerStage(i + 1), towerAnchor);
                stageLights[i] = Glow(stages[i].transform);
                beacons[i] = Child(stages[i].transform, "BeaconSocket");
            }

            relics.Wire(relicCatalog, placement, relicTuning);
            scrap.Wire(scrapTuning, scrapCatalog);
            sonar.Wire(sonarTuning);
            excavation.Wire(excavationTuning);
            tether.Wire(tetherTuning);
            home.Wire(baseTuning, baseRoot.transform, Renderer(Child(lander, "Windows")),
                Children(lander, "LampSocket_", LampSockets), shelf, Glow(shelf), Children(shelf, "Slot_", ShelfSlots));
            tower.Wire(towerTuning, radioTower, towerAnchor, stages, stageLights, beacons);
            gameplay.Wire(visuals, new[] { radioTower }, relics, scrap, sonar, excavation, tether, home, tower);
            context.AddSystem(gameplay);
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
