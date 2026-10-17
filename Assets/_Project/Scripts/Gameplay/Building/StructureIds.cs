namespace MoonProject.Gameplay
{
    /// <summary>
    /// Stable ids and localization key helper for the robot-scale base structures of the opening (opening-v2 §5).
    /// </summary>
    public static class StructureIds
    {
        public const string ChargingPad = "charging_pad";
        public const string SolarArray = "solar_array";
        public const string BatteryBank = "battery_bank";
        public const string PowerPylon = "power_pylon";
        public const string AntennaMast = "antenna_mast";

        public const int Count = 5;

        public static readonly string[] All =
        {
            ChargingPad,
            SolarArray,
            BatteryBank,
            PowerPylon,
            AntennaMast,
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
