using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes the gameplay content from code recipes: one RelicDefinition per <see cref="RelicRecipes"/> entry, the
    /// relic catalog, the scrap catalog over Art's scrap prefabs, the radio tower and workshop upgrades, the friends
    /// (Tilly) and their catalog, Ro's cassettes and their catalog, and the crew log caches and their catalog.
    /// Rewritten in place on every run (GUIDs kept). Every Art prefab it references (the content contracts) is
    /// required: a missing one fails the build loudly.
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
            BuildHoverJump();
            BuildFriends();
            BuildCassettes();
            BuildLogCaches();
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
        /// (15, 40, 80 scrap) widen it to 110, 170 and 260 m and warm the base up. The basin's scrap covers twice
        /// everything on sale (tower and workshop), and every relic brought home adds a gift (design ruling 5). Its
        /// name and level texts live in the localization tables (upgrade.radio_tower.*).
        /// </summary>
        private static void BuildRadioTower()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.Populate("radio_tower", UpgradeStationKind.RadioTower, 60f, new[]
            {
                new UpgradeLevel(15, 110f, 1.25f), new UpgradeLevel(40, 170f, 1.5f), new UpgradeLevel(80, 260f, 1.8f),
            });
            GeneratedAssets.CreateOrReplace(upgrade, GameplayAssetPaths.RadioTowerUpgrade);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.RadioTowerUpgrade}");
        }

        /// <summary>
        /// Hover-Jump, the workshop's first rover ability (docs/features/M3-03-workshop-hoverjump.md): one level for
        /// 150 scrap, the slice's big scrap sink once the tower is done. Its texts live in the localization tables
        /// (upgrade.rover.hover_jump.*).
        /// </summary>
        private static void BuildHoverJump()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.Populate("rover.hover_jump", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(150, RoverAbility.HoverJump),
            });
            GeneratedAssets.CreateOrReplace(upgrade, GameplayAssetPaths.HoverJumpUpgrade);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.HoverJumpUpgrade}");
        }

        /// <summary>
        /// Tilly, Ines's survey drone (docs/features/M3-02-friends-tilly.md): she lies in a shallow crater 60-110 m
        /// east of home, in view from the base edge, her rotor, lens and cell scattered 30-60 m around her; repaired,
        /// she perches on the lander and spots for 07. Her texts live in the localization tables (friend.tilly.*).
        /// </summary>
        private static void BuildFriends()
        {
            var tilly = ScriptableObject.CreateInstance<FriendDefinition>();
            tilly.Populate("tilly", LoadArt(GameplayAssetPaths.FriendPrefab("Tilly_Broken")),
                LoadArt(GameplayAssetPaths.FriendPrefab("Tilly")), new[]
                {
                    new FriendPart("rotor", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyRotor"))),
                    new FriendPart("lens", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyLens"))),
                    new FriendPart("cell", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyCell"))),
                }, "FriendSocket_tilly", FriendDefinition.SpotterAbility, 3.5f, "tilly",
                new FriendPlacement(41, new Vector2(60f, 110f), 90f, 55f, new Vector2(0.3f, 2.5f), 10f, true,
                    new Vector2(30f, 60f)));
            var friends = new[]
            {
                GeneratedAssets.CreateOrReplace(tilly, GameplayAssetPaths.FriendDefinition("tilly")),
            };

            var catalog = ScriptableObject.CreateInstance<FriendCatalog>();
            catalog.Populate(friends);
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.FriendCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: friend catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {friends.Length} friend(s) and {GameplayAssetPaths.FriendCatalog}");
        }

        /// <summary>
        /// Ro's first three tapes (docs/features/M3-05 "Cassettes"): Lumen After Dark, Vol. 1 in her tin box beside
        /// Bell at the canyon terminus, Dust &amp; Honey tucked against a small crater rim in the basin (no gate), and
        /// Slow Orbit on the glinting ledge, a short Hover-Jump up. Anchor offsets are in the anchor's frame (x right,
        /// y forward); content faces back toward 07 arriving. Their texts live in the localization tables
        /// (cassette.&lt;id&gt;.*).
        /// </summary>
        private static void BuildCassettes()
        {
            var hoverJump = new AbilityGate(true, RoverAbility.HoverJump);
            var noAnchor = new AnchorSpot(string.Empty, Vector2.zero);
            CassetteDefinition[] cassettes =
            {
                Cassette("after_dark_1", CassetteSiteRule.Anchor,
                    new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(2.2f, -0.7f)), 0, hoverJump),
                Cassette("dust_and_honey", CassetteSiteRule.BasinPlanner, noAnchor, 73, AbilityGate.Open),
                Cassette("slow_orbit", CassetteSiteRule.Anchor, new AnchorSpot(WorldAnchorIds.CanyonLedge,
                    Vector2.zero), 0, hoverJump),
            };

            var catalog = ScriptableObject.CreateInstance<CassetteCatalog>();
            catalog.Populate(cassettes);
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.CassetteCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: cassette catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {cassettes.Length} cassette(s) and {GameplayAssetPaths.CassetteCatalog}");
        }

        private static CassetteDefinition Cassette(string id, CassetteSiteRule site, AnchorSpot anchor,
            int plannerSeed, AbilityGate gate)
        {
            var cassette = ScriptableObject.CreateInstance<CassetteDefinition>();
            cassette.Populate(id, LoadArt(GameplayAssetPaths.CassettePrefab(id)), site, anchor, plannerSeed, gate);
            return GeneratedAssets.CreateOrReplace(cassette, GameplayAssetPaths.CassetteDefinition(id));
        }

        /// <summary>
        /// Ro's battered tin box at the canyon terminus beside Bell, holding her first log (log.ro_1) and the first
        /// tape; past the chasm, so it needs Hover-Jump.
        /// </summary>
        private static void BuildLogCaches()
        {
            var ro = ScriptableObject.CreateInstance<LogCacheDefinition>();
            ro.Populate("ro_1", LoadArt(GameplayAssetPaths.LogCache),
                new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(2.2f, 0.3f)),
                new AbilityGate(true, RoverAbility.HoverJump));
            var caches = new[]
            {
                GeneratedAssets.CreateOrReplace(ro, GameplayAssetPaths.LogCacheDefinition("ro_1")),
            };

            var catalog = ScriptableObject.CreateInstance<LogCacheCatalog>();
            catalog.Populate(caches);
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.LogCacheCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: log cache catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {caches.Length} log cache(s) and {GameplayAssetPaths.LogCacheCatalog}");
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
