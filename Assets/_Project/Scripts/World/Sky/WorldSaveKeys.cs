namespace MoonProject.World
{
    /// <summary>
    /// Keys and schema versions of the World save sections. Never rename a key: it identifies the data in old saves.
    /// Bump a version (and add a migration) whenever its DTO changes shape.
    /// </summary>
    public static class WorldSaveKeys
    {
        /// <summary>The lunar day clock time and whether the opening eclipse hold is still active.</summary>
        public const string Sky = "world.sky";
        public const int SkyVersion = 1;
    }
}
