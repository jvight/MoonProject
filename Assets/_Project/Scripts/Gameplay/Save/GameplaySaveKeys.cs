namespace MoonProject.Gameplay
{
    /// <summary>
    /// Keys and schema versions of the Gameplay save sections. Never rename a key: it identifies the data in saves.
    /// Bump a version (and add a migration) whenever its DTO changes shape in a way older JSON cannot read.
    /// </summary>
    public static class GameplaySaveKeys
    {
        /// <summary>07's hold (the key is the old scrap wallet's).</summary>
        public const string Materials = "gameplay.wallet";
        public const int MaterialsVersion = 3;

        public const string Salvage = "gameplay.salvage";
        public const int SalvageVersion = 1;
        public const string Battery = "gameplay.battery";
        public const int BatteryVersion = 1;
        public const string Containers = "gameplay.containers";
        public const int ContainersVersion = 1;
        public const string Scans = "gameplay.scans";
        public const int ScansVersion = 2;
        public const string Building = "gameplay.building";
        public const int BuildingVersion = 1;
        public const string Opening = "gameplay.opening";
        public const int OpeningVersion = 1;
        public const string StartKit = "gameplay.startkit";
        public const int StartKitVersion = 1;
        public const string Databank = "gameplay.databank";
        public const int DatabankVersion = 1;
        public const string Call = "gameplay.call";
        public const int CallVersion = 1;
        public const string Tow = "gameplay.tow";
        public const int TowVersion = 1;
    }
}
