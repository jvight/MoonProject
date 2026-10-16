namespace MoonProject.Gameplay
{
    /// <summary>
    /// Stable ids and localization key helper for the 10 robot-scale base structures (0.5.7).
    /// </summary>
    public static class StructureIds
    {
        public const string ChargingPad = "charging_pad";
        public const string SolarArray = "solar_array";
        public const string BatteryBank = "battery_bank";
        public const string PowerPylon = "power_pylon";
        public const string Smelter = "smelter";
        public const string Fabricator = "fabricator";
        public const string StorageRack = "storage_rack";
        public const string UpgradeBay = "upgrade_bay";
        public const string AntennaMast = "antenna_mast";
        public const string VehiclePad = "vehicle_pad";

        public const int Count = 10;

        public static readonly string[] All =
        {
            ChargingPad,
            SolarArray,
            BatteryBank,
            PowerPylon,
            Smelter,
            Fabricator,
            StorageRack,
            UpgradeBay,
            AntennaMast,
            VehiclePad,
        };

        private const string NamePrefix = "structure.";
        private const string NameSuffix = ".name";
        private const string DescriptionSuffix = ".desc";

        /// <summary>Canonical localization key for a structure's display name ("structure.&lt;id&gt;.name").</summary>
        public static string NameKey(string id)
        {
            return NamePrefix + id + NameSuffix;
        }

        /// <summary>Canonical localization key for a structure's description ("structure.&lt;id&gt;.desc").</summary>
        public static string DescriptionKey(string id)
        {
            return NamePrefix + id + DescriptionSuffix;
        }
    }
}
