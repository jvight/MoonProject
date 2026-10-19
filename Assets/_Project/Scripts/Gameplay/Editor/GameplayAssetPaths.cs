using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Where the Gameplay builders write and what they read: tuning (create-if-missing), content (rewritten by the
    /// content builder), generated materials, and the Art prefabs of the content contracts (docs/ARCHITECTURE.md).
    /// </summary>
    internal static class GameplayAssetPaths
    {
        public const string TuningFolder = "Assets/_Project/Data/Tuning/Gameplay";
        public const string BaseTuning = TuningFolder + "/BaseTuning.asset";
        public const string BatteryTuning = TuningFolder + "/BatteryTuning.asset";
        public const string GlintTuning = TuningFolder + "/GlintTuning.asset";
        public const string SalvageTuning = TuningFolder + "/SalvageTuning.asset";
        public const string CargoTuning = TuningFolder + "/CargoTuning.asset";
        public const string CutterTuning = TuningFolder + "/CutterTuning.asset";
        public const string ScanTuning = TuningFolder + "/ScanTuning.asset";
        public const string OpeningTuning = TuningFolder + "/OpeningTuning.asset";
        public const string BuildingTuning = TuningFolder + "/BuildingTuning.asset";

        public const string ContentFolder = "Assets/_Project/Data/Content";
        public const string SalvageCatalog = ContentFolder + "/SalvageCatalog.asset";
        public const string MaterialCatalog = ContentFolder + "/MaterialCatalog.asset";
        public const string StructureFolder = ContentFolder + "/Structures";
        public const string StructureCatalog = ContentFolder + "/StructureCatalog.asset";
        public const string DatabankCatalogue = ContentFolder + "/DatabankCatalogue.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/Gameplay";
        public const string MaterialFolder = GeneratedFolder + "/Materials";
        public const string Visuals = GeneratedFolder + "/GameplayVisuals.asset";

        /// <summary>Art's R1 rubber, the built base's cables.</summary>
        public const string CableMaterial = "Assets/_Project/Generated/Art/R1/Materials/R1_Rubber.mat";

        public const string Shader = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShader = "Assets/_Project/Shaders/Gameplay/Glint.shader";
        public const string PartGlintMaterial = MaterialFolder + "/M_PartGlint.mat";

        public const string ArtPickupFolder = ArtPaths.Root + "/Pickups";

        /// <summary>Art's salvage site prefab (Site_kestrel).</summary>
        public static string SitePrefab(string name)
        {
            return ArtPaths.SitesFolder + "/Site_" + name + ".prefab";
        }

        /// <summary>Art's R1 crate prefab (Crate_M, Crate_L, Crate_Cargo_Open).</summary>
        public static string CratePrefab(string name)
        {
            return ArtPaths.R1ModelsFolder + "/Crates/" + name + ".prefab";
        }

        /// <summary>Art's bundle a cut piece of <paramref name="material"/> folds into.</summary>
        public static string BundlePrefab(Core.SalvageMaterial material)
        {
            return ArtPickupFolder + "/Material_" + material + ".prefab";
        }

        public static string StructureDefinition(string id)
        {
            return StructureFolder + "/Structure_" + id + ".asset";
        }

        public static string RuntimeStructurePrefab(string id)
        {
            return GeneratedFolder + "/Structures/" + id + ".prefab";
        }

        public static string R1StructurePrefab(string prefabName)
        {
            return ArtPaths.R1StructuresModelFolder + "/" + prefabName + ".prefab";
        }

        public static string Material(GlowRole role)
        {
            return MaterialFolder + "/M_Glow" + role + ".mat";
        }
    }
}
