namespace MoonProject.World.Editor
{
    /// <summary>Asset locations owned by the World domain (docs/ARCHITECTURE.md, "Content pipeline").</summary>
    public static class WorldPaths
    {
        public const string GeneratedRoot = "Assets/_Project/Generated/World";
        public const string Settings = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        public const string SkyMaterial = GeneratedRoot + "/M_Sky.mat";
        public const string EarthMaterial = GeneratedRoot + "/M_Earth.mat";
        public const string GroundMaterial = GeneratedRoot + "/M_Ground.mat";
        /// <summary>Marks pressed into the dust: the rover's track shader, World's tint and fade.</summary>
        public const string GroundMarkMaterial = GeneratedRoot + "/M_GroundMarks.mat";
        public const string GroundMarkShader = "Assets/_Project/Shaders/Rover/RoverTrack.shader";
        public const string VolumeProfile = GeneratedRoot + "/WorldVolumeProfile.asset";
        /// <summary>The PC quality level's renderer: World's builder writes its SSAO from the post settings.</summary>
        public const string PcRenderer = "Assets/Settings/PC_Renderer.asset";
        /// <summary>The PC quality level's pipeline asset: World's builder sets its colour grading mode.</summary>
        public const string PcPipeline = "Assets/Settings/PC_RPAsset.asset";
        public const string BeaconFolder = GeneratedRoot + "/Beacon";
        /// <summary>Art's R1 Peak beacon (its lamp node is <see cref="PeakBeaconBuilder.LampNodeName"/>).</summary>
        public const string BeaconModelPrefab = R1PropsFolder + "PeakBeacon.prefab";
        public const string BeaconHaloMesh = BeaconFolder + "/BeaconHaloQuad.asset";
        public const string BeaconHaloMaterial = BeaconFolder + "/M_BeaconHalo.mat";
        public const string BeaconHaloShader = "Assets/_Project/Shaders/World/LofiBeaconHalo.shader";
        /// <summary>The landed lander's amber emergency beacon's halo (opening v2 §4).</summary>
        public const string LanderBeaconMaterial = GeneratedRoot + "/M_LanderBeacon.mat";

        /// <summary>The small halo each cable marker lamp blooms with as the Begin wave passes.</summary>
        public const string CableMarkerHaloMaterial = GeneratedRoot + "/M_CableMarkerHalo.mat";
        public const string SkyShader = "Assets/_Project/Shaders/World/LofiSky.shader";
        public const string EarthShader = "Assets/_Project/Shaders/World/LofiEarth.shader";
        public const string GroundShader = "Assets/_Project/Shaders/World/R1Terrain.shader";
        public const string RegolithAlbedoTexture = "Assets/_Project/Art/R1/Textures/T_R1_Regolith_Albedo.png";
        public const string RegolithNormalTexture = "Assets/_Project/Art/R1/Textures/T_R1_Regolith_Normal.png";
        public const string RegolithMaskTexture = "Assets/_Project/Art/R1/Textures/T_R1_Regolith_Mask.png";
        public const string BasaltAlbedoTexture = "Assets/_Project/Art/R1/Textures/T_R1_Basalt_Albedo.png";
        public const string BasaltNormalTexture = "Assets/_Project/Art/R1/Textures/T_R1_Basalt_Normal.png";
        public const string BasaltMaskTexture = "Assets/_Project/Art/R1/Textures/T_R1_Basalt_Mask.png";
        public const string R1BasaltMaterial = "Assets/_Project/Generated/Art/R1/Materials/R1_Basalt.mat";
        public const string R1RocksFolder = "Assets/_Project/Art/R1/Models/Rocks";
        /// <summary>R1 basalt rocks (Art/R1/Models/Rocks) used as pebbles: 5 irregular rounded cobble and lump shapes.</summary>
        public static readonly string[] PebbleRocks = { "Rock_00", "Rock_01", "Rock_02", "Rock_03", "Rock_04" };

        /// <summary>R1 basalt rocks used as boulders: 5 weathered lunar breccia and basalt boulder shapes.</summary>
        public static readonly string[] BoulderRocks = { "Rock_05", "Rock_06", "Rock_07", "Rock_08", "Rock_09" };

        /// <summary>Art's warm light-decal disc (1 m radius, additive) for the disc light pools.</summary>
        public const string LightPoolPrefab = "Assets/_Project/Generated/Art/Props/LightPool.prefab";

        /// <summary>Art's R1 ruins kit World places at the Lumen station site (RUINS-WORLD).</summary>
        public const string RuinsPrefabFolder = "Assets/_Project/Generated/Art/R1/Models/Ruins/";

        public static string RuinPrefab(string id) => RuinsPrefabFolder + id + ".prefab";

        /// <summary>Art's R1 story props for the opening (Generated/Art/R1/Models/Props, opening v2 §4).</summary>
        public const string R1PropsFolder = "Assets/_Project/Generated/Art/R1/Models/Props/";
        public const string DeadChargingPostPrefab = R1PropsFolder + "DeadChargingPost.prefab";
        public const string GroundCableSegmentPrefab = R1PropsFolder + "GroundCableSegment.prefab";
        public const string GroundCableEndPrefab = R1PropsFolder + "GroundCableEnd.prefab";
        public const string LanderServicePortPrefab = R1PropsFolder + "LanderServicePort.prefab";
        public const string LanderBeaconPrefab = R1PropsFolder + "LanderBeacon.prefab";
        public const string BuildApronMarkingsPrefab = R1PropsFolder + "BuildApronMarkings.prefab";
        public const string DeadAntennaMastPrefab = R1PropsFolder + "DeadAntennaMast.prefab";
        public const string SignalPillarPrefab = R1PropsFolder + "SignalPillar.prefab";

        /// <summary>Art's R1 Kestrel-3 debris kit (Generated/Art/R1/Models/Wreck).</summary>
        public static string KestrelDebrisPrefab(string id) => "Assets/_Project/Generated/Art/R1/Models/Wreck/" + id + ".prefab";

        /// <summary>Art's R1 rock prefabs (Generated/Art/R1/Models/Rocks), the big boulders among them.</summary>
        public static string R1RockPrefab(string id) => "Assets/_Project/Generated/Art/R1/Models/Rocks/" + id + ".prefab";

        /// <summary>Art's R1 cargo lander, landed at place.lander in the opening (opening v2 §4).</summary>
        public const string CargoLanderPrefab = "Assets/_Project/Generated/Art/R1/Models/Lander/CargoLander.prefab";

        public const string CapturePoses = "Assets/_Project/Scripts/World/Editor/Captures/world_views.json";
    }
}
