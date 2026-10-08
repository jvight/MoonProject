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
        public const int MaterialsVersion = 2;

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
    }
}
