using System;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The Gameplay domain's part of Main.unity: under [Gameplay], the <see cref="GameplaySystem"/> (one system,
    /// initialised after World, Rover and Audio) and its parts, wired to the tuning, content and material assets.
    /// Relic sites and the scrap field are planned from the World's surface at boot, so the scene holds no content
    /// instances; it fails loudly when a required asset is missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
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
            var scrapCatalog = context.LoadAsset<ScrapCatalog>(GameplayAssetPaths.ScrapCatalog);
            var relicCatalog = context.LoadAsset<RelicCatalog>(GameplayAssetPaths.RelicCatalog);
            Require(visuals.Validate(), nameof(GameplayVisuals));
            Require(scrapCatalog.Validate(), nameof(ScrapCatalog));
            Require(relicCatalog.Validate(), nameof(RelicCatalog));

            Transform root = context.GameplayRoot.transform;
            GameObject host = context.CreateChild("GameplaySystem", root);
            var gameplay = host.AddComponent<GameplaySystem>();
            var relics = context.CreateChild("Relics", host.transform).AddComponent<RelicField>();
            var scrap = context.CreateChild("Scrap", host.transform).AddComponent<ScrapField>();
            var sonar = context.CreateChild("Sonar", host.transform).AddComponent<SonarSystem>();
            var excavation = context.CreateChild("Excavation", host.transform).AddComponent<ExcavationSystem>();
            var tether = context.CreateChild("Tether", host.transform).AddComponent<TetherSystem>();

            relics.Wire(relicCatalog, placement, relicTuning);
            scrap.Wire(scrapTuning, scrapCatalog);
            sonar.Wire(sonarTuning);
            excavation.Wire(excavationTuning);
            tether.Wire(tetherTuning);
            gameplay.Wire(visuals, relics, scrap, sonar, excavation, tether);
            context.AddSystem(gameplay);
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
