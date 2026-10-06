using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Where the Gameplay builders write and what they read: tuning (create-if-missing), content (rewritten by the
    /// content builder), generated materials, and the Art prefabs of the M2 content contract (docs/ARCHITECTURE.md).
    /// </summary>
    internal static class GameplayAssetPaths
    {
        public const string TuningFolder = "Assets/_Project/Data/Tuning/Gameplay";
        public const string ScrapTuning = TuningFolder + "/ScrapTuning.asset";
        public const string SonarTuning = TuningFolder + "/SonarTuning.asset";
        public const string RelicTuning = TuningFolder + "/RelicTuning.asset";
        public const string RelicPlacement = TuningFolder + "/RelicPlacementTuning.asset";
        public const string ExcavationTuning = TuningFolder + "/ExcavationTuning.asset";
        public const string TetherTuning = TuningFolder + "/TetherTuning.asset";
        public const string BaseTuning = TuningFolder + "/BaseTuning.asset";
        public const string RadioTowerTuning = TuningFolder + "/RadioTowerTuning.asset";
        public const string WorkshopTuning = TuningFolder + "/WorkshopTuning.asset";
        public const string FriendTuning = TuningFolder + "/FriendTuning.asset";

        public const string ContentFolder = "Assets/_Project/Data/Content";
        public const string RelicFolder = ContentFolder + "/Relics";
        public const string RelicCatalog = ContentFolder + "/RelicCatalog.asset";
        public const string ScrapCatalog = ContentFolder + "/ScrapCatalog.asset";
        public const string UpgradeFolder = ContentFolder + "/Upgrades";
        public const string RadioTowerUpgrade = UpgradeFolder + "/Upgrade_radio_tower.asset";
        public const string HoverJumpUpgrade = UpgradeFolder + "/Upgrade_rover_hover_jump.asset";
        public const string FriendFolder = ContentFolder + "/Friends";
        public const string FriendCatalog = ContentFolder + "/FriendCatalog.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/Gameplay";
        public const string MaterialFolder = GeneratedFolder + "/Materials";
        public const string Visuals = GeneratedFolder + "/GameplayVisuals.asset";

        public const string Shader = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShader = "Assets/_Project/Shaders/Gameplay/Glint.shader";
        public const string GlintMaterial = MaterialFolder + "/M_ScrapGlint.mat";
        public const string PartGlintMaterial = MaterialFolder + "/M_PartGlint.mat";

        public const string ArtRelicFolder = ArtPaths.Root + "/Relics";
        public const string ArtBaseFolder = ArtPaths.Root + "/Base";
        public const string Lander = ArtBaseFolder + "/Lander.prefab";
        public const string MuseumShelf = ArtBaseFolder + "/MuseumShelf.prefab";
        public const string ArtFriendFolder = ArtPaths.Root + "/Friends";

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

        public static string ScrapPrefab(string kind)
        {
            return ArtPaths.ScrapFolder + "/Scrap_" + kind + ".prefab";
        }

        public static string Material(GlowRole role)
        {
            return MaterialFolder + "/M_Glow" + role + ".mat";
        }
    }
}
