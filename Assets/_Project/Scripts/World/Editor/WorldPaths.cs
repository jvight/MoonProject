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
        public const string BeaconFolder = GeneratedRoot + "/Beacon";
        public const string BeaconModelPrefab = BeaconFolder + "/PeakBeaconModel.prefab";
        public const string BeaconHaloMesh = BeaconFolder + "/BeaconHaloQuad.asset";
        public const string BeaconHaloMaterial = BeaconFolder + "/M_BeaconHalo.mat";
        public const string BeaconHaloShader = "MoonProject/World/LofiBeaconHalo";
        public const string CanyonGlowMaterial = GeneratedRoot + "/Canyon/M_CanyonGlow.mat";
        public const string SkyShader = "MoonProject/World/LofiSky";
        public const string EarthShader = "MoonProject/World/LofiEarth";
        /// <summary>Art rocks (Generated/Art/Rocks) used as pebbles: the pebble, rounded and slab shapes.</summary>
        public static readonly string[] PebbleRocks = { "Rock_00", "Rock_01", "Rock_03" };

        /// <summary>Art rocks used as boulders: the jagged, big rounded and boulder shapes.</summary>
        public static readonly string[] BoulderRocks = { "Rock_02", "Rock_04", "Rock_05" };

        public const string CapturePoses = "Assets/_Project/Scripts/World/Editor/Captures/world_views.json";
    }
}
