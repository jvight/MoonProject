using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Where the Gameplay builders write and what they read: tuning (create-if-missing), content (rewritten by the
    /// content builder), generated materials, and the Art prefabs of the content contracts (docs/ARCHITECTURE.md: M2
    /// content, friends, Bell and cassettes).
    /// </summary>
    internal static class GameplayAssetPaths
    {
        public const string TuningFolder = "Assets/_Project/Data/Tuning/Gameplay";
        public const string SonarTuning = TuningFolder + "/SonarTuning.asset";
        public const string RelicTuning = TuningFolder + "/RelicTuning.asset";
        public const string ExcavationTuning = TuningFolder + "/ExcavationTuning.asset";
        public const string TetherTuning = TuningFolder + "/TetherTuning.asset";
        public const string BaseTuning = TuningFolder + "/BaseTuning.asset";
        public const string RadioTowerTuning = TuningFolder + "/RadioTowerTuning.asset";
        public const string WorkshopTuning = TuningFolder + "/WorkshopTuning.asset";
        public const string FriendTuning = TuningFolder + "/FriendTuning.asset";
        public const string CassetteTuning = TuningFolder + "/CassetteTuning.asset";
        public const string LogCacheTuning = TuningFolder + "/LogCacheTuning.asset";
        public const string BellTuning = TuningFolder + "/BellTuning.asset";
        public const string RelayTuning = TuningFolder + "/RelayTuning.asset";
        public const string GlintTuning = TuningFolder + "/GlintTuning.asset";
        public const string SalvageTuning = TuningFolder + "/SalvageTuning.asset";

        public const string ContentFolder = "Assets/_Project/Data/Content";
        public const string RelicFolder = ContentFolder + "/Relics";
        public const string RelicCatalog = ContentFolder + "/RelicCatalog.asset";
        public const string SalvageCatalog = ContentFolder + "/SalvageCatalog.asset";
        public const string UpgradeFolder = ContentFolder + "/Upgrades";
        public const string RadioTowerUpgrade = UpgradeFolder + "/Upgrade_radio_tower.asset";
        public const string HoverJumpUpgrade = UpgradeFolder + "/Upgrade_rover_hover_jump.asset";
        public const string CargoCradleUpgrade = UpgradeFolder + "/Upgrade_rover_cargo_cradle.asset";
        public const string WarmHeadlampUpgrade = UpgradeFolder + "/Upgrade_rover_warm_headlamp.asset";
        public const string BoostCoilsUpgrade = UpgradeFolder + "/Upgrade_rover_boost_coils.asset";
        public const string FriendFolder = ContentFolder + "/Friends";
        public const string FriendCatalog = ContentFolder + "/FriendCatalog.asset";
        public const string CassetteFolder = ContentFolder + "/Cassettes";
        public const string CassetteCatalog = ContentFolder + "/CassetteCatalog.asset";
        public const string LogCacheFolder = ContentFolder + "/LogCaches";
        public const string LogCacheCatalog = ContentFolder + "/LogCacheCatalog.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/Gameplay";
        public const string MaterialFolder = GeneratedFolder + "/Materials";
        public const string Visuals = GeneratedFolder + "/GameplayVisuals.asset";

        public const string Shader = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShader = "Assets/_Project/Shaders/Gameplay/Glint.shader";
        public const string SalvageGlintMaterial = MaterialFolder + "/M_SalvageGlint.mat";
        public const string PartGlintMaterial = MaterialFolder + "/M_PartGlint.mat";

        public const string ArtRelicFolder = ArtPaths.Root + "/Relics";
        public const string ArtBaseFolder = ArtPaths.Root + "/Base";
        public const string Lander = ArtBaseFolder + "/Lander.prefab";
        public const string MuseumShelf = ArtBaseFolder + "/MuseumShelf.prefab";
        public const string Workbench = ArtBaseFolder + "/Workbench.prefab";
        public const string CassetteShelf = ArtBaseFolder + "/CassetteShelf.prefab";
        public const string ArtFriendFolder = ArtPaths.Root + "/Friends";
        public const string ArtPickupFolder = ArtPaths.Root + "/Pickups";
        public const string LogCache = ArtPaths.Root + "/Props/LogCache.prefab";
        public const string RelayMast = ArtPaths.RelayFolder + "/RelayMast.prefab";
        public const string RelayMastBroken = ArtPaths.RelayFolder + "/RelayMast_Broken.prefab";
        public const string RelayPart = ArtPaths.RelayFolder + "/Part_RelayModule.prefab";

        /// <summary>Art's salvage site prefab (Site_depot, Site_kestrel...).</summary>
        public static string SitePrefab(string name)
        {
            return ArtPaths.SitesFolder + "/Site_" + name + ".prefab";
        }

        /// <summary>Art's loose trail bit prefab (Debris_Metal_0...).</summary>
        public static string DebrisPrefab(string name)
        {
            return ArtPaths.SitesFolder + "/" + name + ".prefab";
        }

        /// <summary>Art's bundle a cut piece of <paramref name="material"/> folds into.</summary>
        public static string BundlePrefab(Core.SalvageMaterial material)
        {
            return ArtPickupFolder + "/Material_" + material + ".prefab";
        }

        public static string CassetteDefinition(string id)
        {
            return CassetteFolder + "/Cassette_" + id + ".asset";
        }

        public static string CassettePrefab(string id)
        {
            return ArtPickupFolder + "/Cassette_" + id + ".prefab";
        }

        public static string LogCacheDefinition(string logId)
        {
            return LogCacheFolder + "/LogCache_" + logId + ".asset";
        }

        public static string FriendDefinition(string id)
        {
            return FriendFolder + "/Friend_" + id + ".asset";
        }

        /// <summary>A friend model or part pickup prefab (e.g. Tilly, Tilly_Broken, Part_TillyRotor).</summary>
        public static string FriendPrefab(string name)
        {
            return ArtFriendFolder + "/" + name + ".prefab";
        }

        public static string RadioTowerStage(int level)
        {
            return ArtBaseFolder + "/RadioTower_L" + level + ".prefab";
        }

        public static string RelicDefinition(string id)
        {
            return RelicFolder + "/Relic_" + id + ".asset";
        }

        public static string RelicPrefab(string id)
        {
            return ArtRelicFolder + "/Relic_" + id + ".prefab";
        }

        public static string Material(GlowRole role)
        {
            return MaterialFolder + "/M_Glow" + role + ".mat";
        }
    }
}
