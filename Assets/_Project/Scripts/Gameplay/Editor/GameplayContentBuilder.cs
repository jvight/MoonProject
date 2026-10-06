using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes the gameplay content from code recipes: one RelicDefinition per <see cref="RelicRecipes"/> entry, the
    /// relic catalog, the scrap catalog over Art's scrap prefabs, and the radio tower upgrade. Rewritten in place on
    /// every run (GUIDs kept).
    /// Every Art prefab it references (M2 content contract) is required: a missing one fails the build loudly.
    /// </summary>
    internal static class GameplayContentBuilder
    {
        public const string BuilderPath = "Gameplay/Content";
        public const int BuilderOrder = 510;

        private static readonly (string Kind, int Value, float Weight)[] ScrapKinds =
        {
            ("Bolt", 1, 3f), ("Gear", 2, 2f), ("Panel", 3, 1f), ("Coil", 2, 2f),
        };

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            BuildScrapCatalog();
            BuildRelics();
            BuildRadioTower();
            AssetDatabase.SaveAssets();
        }

        private static void BuildScrapCatalog()
        {
            var variants = new ScrapVariant[ScrapKinds.Length];
            for (int i = 0; i < ScrapKinds.Length; i++)
            {
                string path = GameplayAssetPaths.ScrapPrefab(ScrapKinds[i].Kind);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"{BuilderPath}: Art prefab {path} is missing (M2 content contract). Run the Art builders.");
                }

                variants[i] = new ScrapVariant(prefab, ScrapKinds[i].Value, ScrapKinds[i].Weight);
            }

            var catalog = ScriptableObject.CreateInstance<ScrapCatalog>();
            catalog.Populate(variants);
            GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.ScrapCatalog);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.ScrapCatalog} ({variants.Length} variants)");
        }

        /// <summary>
        /// The radio tower: before any purchase the old mast is dark and the clear signal reaches 60 m; three levels
        /// (15, 40, 80 scrap) widen it to 110, 170 and 260 m and warm the base up. Twice the total cost lies in the
        /// basin as scrap, and every relic brought home adds a gift (design ruling 5). Its name and level texts live in
        /// the localization tables (upgrade.radio_tower.*).
        /// </summary>
        private static void BuildRadioTower()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.Populate("radio_tower", 60f, new[]
            {
                new UpgradeLevel(15, 110f, 1.25f), new UpgradeLevel(40, 170f, 1.5f), new UpgradeLevel(80, 260f, 1.8f),
            });
            GeneratedAssets.CreateOrReplace(upgrade, GameplayAssetPaths.RadioTowerUpgrade);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.RadioTowerUpgrade}");
        }

        private static void BuildRelics()
        {
            RelicRecipe[] recipes = RelicRecipes.All;
            var definitions = new RelicDefinition[recipes.Length];
            for (int i = 0; i < recipes.Length; i++)
            {
                RelicRecipe recipe = recipes[i];
                string prefabPath = GameplayAssetPaths.RelicPrefab(recipe.Id);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"{BuilderPath}: Art prefab {prefabPath} is missing (M2 content contract). " +
                        "Run the Art builders.");
                }

                var definition = ScriptableObject.CreateInstance<RelicDefinition>();
                definition.Populate(recipe.Id, recipe.Mass, prefab, recipe.AnswerNote, recipe.Placement);
                definitions[i] = GeneratedAssets.CreateOrReplace(definition,
                    GameplayAssetPaths.RelicDefinition(recipe.Id));
            }

            var catalog = ScriptableObject.CreateInstance<RelicCatalog>();
            catalog.Populate(definitions);
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.RelicCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: relic catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {definitions.Length} relic definitions and " +
                      GameplayAssetPaths.RelicCatalog);
        }
    }
}
