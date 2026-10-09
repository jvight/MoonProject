using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes the gameplay content from code recipes: the salvage catalog over Art's site, bundle and debris prefabs
    /// (<see cref="SalvageEconomy"/>), one RelicDefinition per <see cref="RelicRecipes"/> entry and the relic catalog,
    /// the radio tower and the Rover Bay's kit crafted from the <see cref="SalvageEconomy"/> recipes, the friends
    /// (Tilly and Bell) and their catalog, Ro's cassettes and their catalog, and the crew log caches and their catalog.
    /// Rewritten in place on every run (GUIDs kept). Every Art prefab it references (the content contracts) is
    /// required: a missing one fails the build loudly.
    /// </summary>
    internal static class GameplayContentBuilder
    {
        public const string BuilderPath = "Gameplay/Content";
        public const int BuilderOrder = 510;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            BuildSalvage();
            BuildRelics();
            BuildRadioTower();
            BuildWorkshopUpgrades();
            BuildFriends();
            BuildCassettes();
            BuildLogCaches();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The radio tower: before any purchase the old mast is dark and the clear signal reaches 60 m; three levels
        /// crafted from wiring and optics (<see cref="SalvageEconomy.RadioTower"/>) widen it to 110, 170 and 260 m and
        /// warm the base up.
        /// The sites yield at least twice every recipe (VISION ruling 5, an EditMode test keeps it). Its name and level
        /// texts live in the localization tables (upgrade.radio_tower.*).
        /// </summary>
        private static void BuildRadioTower()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.Populate("radio_tower", UpgradeStationKind.RadioTower, 60f, new[]
            {
                new UpgradeLevel(SalvageEconomy.RadioTower[0], 110f, 1.25f),
                new UpgradeLevel(SalvageEconomy.RadioTower[1], 170f, 1.5f),
                new UpgradeLevel(SalvageEconomy.RadioTower[2], 260f, 1.8f),
            });
            GeneratedAssets.CreateOrReplace(upgrade, GameplayAssetPaths.RadioTowerUpgrade);
            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.RadioTowerUpgrade}");
        }

        /// <summary>
        /// Kenji's bench sells rover kit, offered in this order (docs/features/M3-03, M3-11): Hover-Jump, then the
        /// Cargo Cradle, the Warm Headlamp and the Boost Coils, one level each, crafted from the
        /// <see cref="SalvageEconomy"/> recipes. Each grants its rover ability; the Rover domain shows its kit on 07.
        /// Their texts live in the localization tables (upgrade.rover.&lt;ability&gt;.*).
        /// </summary>
        private static void BuildWorkshopUpgrades()
        {
            RoverKit("rover.hover_jump", SalvageEconomy.HoverJump, RoverAbility.HoverJump,
                GameplayAssetPaths.HoverJumpUpgrade);
            RoverKit("rover.cargo_cradle", SalvageEconomy.CargoCradle, RoverAbility.CargoCradle,
                GameplayAssetPaths.CargoCradleUpgrade);
            RoverKit("rover.warm_headlamp", SalvageEconomy.WarmHeadlamp, RoverAbility.WarmHeadlamp,
                GameplayAssetPaths.WarmHeadlampUpgrade);
            RoverKit("rover.boost_coils", SalvageEconomy.BoostCoils, RoverAbility.BoostCoils,
                GameplayAssetPaths.BoostCoilsUpgrade);
        }

        private static void RoverKit(string id, Recipe recipe, RoverAbility ability, string path)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.Populate(id, UpgradeStationKind.Workshop, 0f, new[] { new UpgradeLevel(recipe, ability) });
            GeneratedAssets.CreateOrReplace(upgrade, path);
            Debug.Log($"{BuilderPath}: wrote {path}");
        }

        /// <summary>
        /// Tilly, Ines's survey drone (docs/features/M3-02-friends-tilly.md): she lies in a shallow crater 60-110 m
        /// east of home, in view from the base edge, her rotor, lens and cell scattered 30-60 m around her; repaired,
        /// she perches on the lander and spots for 07. Bell, Ro's radio cabinet on legs (docs/features/M3-05): she
        /// leans against the back wall of the canyon terminus (4.85 m in from its anchor, where her back, 1.03 m
        /// behind her root, meets the sheer wall), her knob, cone and valve in the three alcoves (or along the way in),
        /// and Lumen After Dark, Vol. 1 is the fourth thing she needs; repaired she walks home to her corner by the
        /// radio tower and brings the radio dial. Their texts
        /// live in the localization tables (friend.&lt;id&gt;.*).
        /// </summary>
        private static void BuildFriends()
        {
            var tilly = ScriptableObject.CreateInstance<FriendDefinition>();
            tilly.Populate("tilly", LoadArt(GameplayAssetPaths.FriendPrefab("Tilly_Broken")),
                LoadArt(GameplayAssetPaths.FriendPrefab("Tilly")), FriendBodyKind.Drone, new[]
                {
                    new FriendPart("rotor", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyRotor"))),
                    new FriendPart("lens", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyLens"))),
                    new FriendPart("cell", LoadArt(GameplayAssetPaths.FriendPrefab("Part_TillyCell"))),
                }, Array.Empty<string>(), FriendHome.Lander, "FriendSocket_tilly", false,
                FriendDefinition.SpotterAbility, 3.5f, "tilly",
                new FriendPlacement(41, new Vector2(60f, 110f), 90f, 55f, new Vector2(0.3f, 2.5f), 10f, true,
                    new Vector2(30f, 60f)));
            var bell = ScriptableObject.CreateInstance<FriendDefinition>();
            bell.Populate("bell", LoadArt(GameplayAssetPaths.FriendPrefab("Bell_Broken")),
                LoadArt(GameplayAssetPaths.FriendPrefab("Bell")), FriendBodyKind.RadioCabinet, new[]
                {
                    new FriendPart("knob", LoadArt(GameplayAssetPaths.FriendPrefab("Part_BellKnob"))),
                    new FriendPart("cone", LoadArt(GameplayAssetPaths.FriendPrefab("Part_BellCone"))),
                    new FriendPart("valve", LoadArt(GameplayAssetPaths.FriendPrefab("Part_BellValve"))),
                }, new[] { "after_dark_1" }, FriendHome.RadioTower, "BellCorner", true,
                FriendDefinition.RadioDialAbility, 4f, "bell",
                new FriendAnchorPlacement(new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(0f, 4.85f)),
                    WorldAnchorIds.CanyonAlcovePrefix, WorldAnchorIds.CanyonLanding, 40f, WorldAnchorIds.CanyonExit));
            var friends = new[]
            {
                GeneratedAssets.CreateOrReplace(tilly, GameplayAssetPaths.FriendDefinition("tilly")),
                GeneratedAssets.CreateOrReplace(bell, GameplayAssetPaths.FriendDefinition("bell")),
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
        /// Ro's first three tapes (docs/features/M3-05 "Cassettes"): Lumen After Dark, Vol. 1 by her tin box beside
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
                    new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(1.2f, 1.9f)), 0, hoverJump),
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
                new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(1.7f, 3f)),
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

        /// <summary>
        /// The salvage catalog: Art's five wreck sites on their World anchors (the canyon lander past the Hover-Jump
        /// gate), the three material bundles cut pieces fold into, and Kestrel-3's loose trail bits spread evenly down
        /// its debris furrow from the trail anchor toward the crater.
        /// </summary>
        private static void BuildSalvage()
        {
            IReadOnlyList<string> names = SalvageEconomy.SiteNames;
            var sites = new SalvageSiteEntry[names.Count];
            for (int i = 0; i < sites.Length; i++)
            {
                AbilityGate gate = names[i] == SalvageEconomy.GatedSite
                    ? new AbilityGate(true, RoverAbility.HoverJump)
                    : AbilityGate.Open;
                sites[i] = new SalvageSiteEntry(SalvageEconomy.SiteId(names[i]),
                    LoadArt(GameplayAssetPaths.SitePrefab(names[i])), gate);
            }

            IReadOnlyList<string> bits = SalvageEconomy.TrailBits;
            var trail = new SalvageTrailBit[bits.Count];
            for (int i = 0; i < trail.Length; i++)
            {
                if (!SalvagePieceName.TryParseDebris(bits[i], out SalvageMaterial material))
                {
                    throw new InvalidOperationException($"{BuilderPath}: '{bits[i]}' is not a debris prefab name.");
                }

                float distance = trail.Length > 1 ? SalvageEconomy.TrailLength * i / (trail.Length - 1) : 0f;
                trail[i] = new SalvageTrailBit(LoadArt(GameplayAssetPaths.DebrisPrefab(bits[i])), material, distance);
            }

            var catalog = ScriptableObject.CreateInstance<SalvageCatalog>();
            catalog.Populate(sites, LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Metal)),
                LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Wiring)),
                LoadArt(GameplayAssetPaths.BundlePrefab(SalvageMaterial.Optics)), trail);
            catalog = GeneratedAssets.CreateOrReplace(catalog, GameplayAssetPaths.SalvageCatalog);
            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: salvage catalog {problem}.");
            }

            Debug.Log($"{BuilderPath}: wrote {GameplayAssetPaths.SalvageCatalog} ({sites.Length} sites, " +
                      $"{trail.Length} trail bits)");
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
                definition.Populate(recipe.Id, recipe.Mass, prefab, recipe.AnswerNote,
                    SalvageEconomy.SiteId(recipe.Site), recipe.HeartOffset);
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
