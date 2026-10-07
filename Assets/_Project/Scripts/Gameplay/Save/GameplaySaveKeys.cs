namespace MoonProject.Gameplay
{
    /// <summary>
    /// Keys and schema versions of the Gameplay save sections. Never rename a key: it identifies the data in old saves.
    /// Bump a version (and add a migration) whenever its DTO changes shape.
    /// </summary>
    public static class GameplaySaveKeys
    {
        public const string Wallet = "gameplay.wallet";
        public const int WalletVersion = 1;

        public const string Scrap = "gameplay.scrap";
        public const int ScrapVersion = 1;

        public const string Relics = "gameplay.relics";
        public const int RelicsVersion = 1;

        public const string Upgrades = "gameplay.upgrades";
        public const int UpgradesVersion = 1;

        public const string Friends = "gameplay.friends";
        public const int FriendsVersion = 1;

        public const string Radio = "gameplay.radio";
        public const int RadioVersion = 1;
    }
}
