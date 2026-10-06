using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes the gameplay content from code recipes: one RelicDefinition per <see cref="RelicRecipes"/> entry, the
    /// relic catalog, and the scrap catalog over Art's scrap prefabs. Rewritten in place on every run (GUIDs kept).
    /// Scrap prefabs are required (fails loudly). Relic prefabs are picked up when Art delivers them; until then a
    /// relic is written without a model and stays a buried site in game (a warning names each one).
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
                    Debug.LogWarning($"{BuilderPath}: {prefabPath} not delivered yet; '{recipe.Id}' stays a buried " +
                                     "site until it lands and this builder runs again.");
                }

                var definition = ScriptableObject.CreateInstance<RelicDefinition>();
                definition.Populate(recipe.Id, recipe.DisplayName, recipe.Memory, recipe.Mass, prefab,
                    recipe.AnswerNote, recipe.Placement);
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
