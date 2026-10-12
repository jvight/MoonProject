namespace MoonProject.Gameplay
{
    /// <summary>
    /// Keys and schema versions of the Gameplay save sections. Never rename a key: it identifies the data in old saves.
    /// Bump a version (and add a migration) whenever its DTO changes shape.
    /// </summary>
    public static class GameplaySaveKeys
    {
        /// <summary>07's salvaged materials; the scrap wallet's key (version 1), so an old balance migrates.</summary>
        public const string Materials = "gameplay.wallet";
        public const int MaterialsVersion = 3;

        /// <summary>The crew supply crates already cut open (docs/features/M3-25 T4).</summary>
        public const string Crates = "gameplay.crates";
        public const int CratesVersion = 1;

        public const string Relics = "gameplay.relics";
        public const int RelicsVersion = 1;

        public const string Upgrades = "gameplay.upgrades";
        public const int UpgradesVersion = 1;

        public const string Friends = "gameplay.friends";
        public const int FriendsVersion = 2;

        public const string Radio = "gameplay.radio";
        public const int RadioVersion = 1;

        public const string Logs = "gameplay.logs";
        public const int LogsVersion = 1;

        public const string BellSignals = "gameplay.bell_signals";
        public const int BellSignalsVersion = 1;

        public const string Relays = "gameplay.relays";
        public const int RelaysVersion = 1;

        public const string Salvage = "gameplay.salvage";
        public const int SalvageVersion = 1;
        public const string Progression = "gameplay.progression";
        public const int ProgressionVersion = 1;
        public const string Blueprints = "gameplay.blueprints";
        public const int BlueprintsVersion = 1;
        public const string Lift = "gameplay.lift";
        public const int LiftVersion = 1;
        public const string TowerParts = "gameplay.towerparts";
        public const int TowerPartsVersion = 1;
        public const string Map = "gameplay.map";
        public const int MapVersion = 1;
        public const string Battery = "gameplay.battery";
        public const int BatteryVersion = 1;
        public const string GaragePort = "gameplay.garage_port";
        public const int GaragePortVersion = 1;
    }
}
