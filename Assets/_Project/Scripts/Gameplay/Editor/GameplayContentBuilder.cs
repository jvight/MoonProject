using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes the gameplay content from code recipes: the material catalog, the salvage catalog over Art's Kestrel site
    /// and bundle prefabs (<see cref="SalvageEconomy"/>), and the base structures and their catalog
    /// (<see cref="BaseStructureRecipes"/>). Rewritten in place on every run (GUIDs kept). Every Art prefab it
    /// references (the content contracts) is required: a missing one fails the build loudly.
    /// </summary>
    internal static class GameplayContentBuilder
    {
        public const string BuilderPath = "Gameplay/Content";
        public const int BuilderOrder = 510;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            BuildMaterials();
            BuildSalvage();
            BuildStructures();
            BuildDatabank();
            AssetDatabase.SaveAssets();
        }

        /// <summary>The material catalog from the economy table (<see cref="SalvageEconomy.Materials"/>).</summary>
        private static void BuildMaterials()
        {
            var catalog = ScriptableObject.CreateInstance<MaterialCatalog>();
            catalog.Populate(SalvageEconomy.Materials());
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: the material catalog is broken: {problem}.");
            }

            GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.MaterialCatalog);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.MaterialCatalog}");
        }

        /// <summary>
        /// The player-built base structures (<see cref="BaseStructureRecipes"/>) and their catalog
        /// (<see cref="StructureCatalog"/>).
        /// </summary>
        internal static void BuildStructures()
        {
            BaseStructureRecipes.BuildRuntimePrefabs();
            StructureDefinition[] definitions = BaseStructureRecipes.CreateDefinitions(requireRuntimePrefabs: true);
            for (int i = 0; i < definitions.Length; i++)
            {
                StructureDefinition definition = definitions[i];
                definitions[i] = GeneratedAssets.CreateOrReplace(
                    definition,
                    GameplayAssetPaths.StructureDefinition(definition.Id));
            }

            var catalog = ScriptableObject.CreateInstance<StructureCatalog>();
            catalog.Populate(definitions);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: the structure catalog is broken: {problem}.");
            }

            GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.StructureCatalog);
            Debug.Log($"{BuilderPath}: wrote {definitions.Length} structure definitions and " +
                      GameplayAssetPaths.StructureCatalog);
        }

        /// <summary>The databank's 18 entries over Art's models (<see cref="DatabankRecipes"/>).</summary>
        private static void BuildDatabank()
        {
            var catalogue = ScriptableObject.CreateInstance<DatabankCatalogue>();
            catalogue.Populate(DatabankRecipes.Create());
            string problem = catalogue.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: the databank catalogue is broken: {problem}.");
            }

            GeneratedAssets.CreateOrReplace(catalogue, GameplayAssetPaths.DatabankCatalogue);
            Debug.Log($"{BuilderPath}: wrote {catalogue.Count} databank entries to " +
                      GameplayAssetPaths.DatabankCatalogue);
        }

        private static GameObject LoadArt(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: Art prefab {path} is missing (content contracts, docs/ARCHITECTURE.md). " +
                    "Run the Art builders.");
            }

            return prefab;
        }

        /// <summary>
        /// The salvage catalog: Art's Kestrel impact on its World anchor and the three material bundles cut pieces
        /// fold into.
        /// </summary>
        private static void BuildSalvage()
        {
            GameObject prefab = LoadArt(GameplayAssetPaths.SitePrefab(SalvageEconomy.Kestrel));
            var sites = new[]
            {
                new SalvageSiteEntry(SalvageEconomy.SiteId(SalvageEconomy.Kestrel), prefab, 1,
                    SalvageCutLines.Across(prefab), SalvageSizes.Deal(prefab, SalvageEconomy.KestrelPieceClasses)),
            };
            var catalog = ScriptableObject.CreateInstance<SalvageCatalog>();
            catalog.Populate(sites, LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Metal)),
                LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Wiring)),
                LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Optics)));
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.SalvageCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: salvage catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.SalvageCatalog} ({sites.Length} site)");
        }
    }
}
