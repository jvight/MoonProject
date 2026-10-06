namespace MoonProject.World.Editor
{
    /// <summary>Asset locations owned by the World domain (docs/ARCHITECTURE.md, "Content pipeline").</summary>
    public static class WorldPaths
    {
        public const string GeneratedRoot = "Assets/_Project/Generated/World";
        public const string Settings = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        public const string SkyMaterial = GeneratedRoot + "/M_Sky.mat";
        public const string EarthMaterial = GeneratedRoot + "/M_Earth.mat";
        public const string VolumeProfile = GeneratedRoot + "/WorldVolumeProfile.asset";
        public const string SkyShader = "MoonProject/World/LofiSky";
        public const string EarthShader = "MoonProject/World/LofiEarth";
        public const string CapturePoses = "Assets/_Project/Scripts/World/Editor/Captures/world_views.json";
    }
}
