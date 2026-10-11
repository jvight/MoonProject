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
        public const string GroundMarkShader = "MoonProject/Rover/Track";
        /// <summary>Home's marks in the lander's frame: worn tracks between its places, the blast scar, bootprints.</summary>
        public const string HomeGroundMarks = GeneratedRoot + "/HomeGroundMarks.asset";
        public const string VolumeProfile = GeneratedRoot + "/WorldVolumeProfile.asset";
        /// <summary>Home's warm grade, blended in a step per power stage over the main profile.</summary>
        public const string HomeGradeProfile = GeneratedRoot + "/HomeGradeProfile.asset";
        /// <summary>The PC quality level's renderer: World's builder writes its SSAO from the post settings.</summary>
        public const string PcRenderer = "Assets/Settings/PC_Renderer.asset";
        /// <summary>The PC quality level's pipeline asset: World's builder sets its colour grading mode.</summary>
        public const string PcPipeline = "Assets/Settings/PC_RPAsset.asset";
        public const string BeaconFolder = GeneratedRoot + "/Beacon";
        public const string BeaconModelPrefab = BeaconFolder + "/PeakBeaconModel.prefab";
        public const string BeaconHaloMesh = BeaconFolder + "/BeaconHaloQuad.asset";
        public const string BeaconHaloMaterial = BeaconFolder + "/M_BeaconHalo.mat";
        public const string BeaconHaloShader = "MoonProject/World/LofiBeaconHalo";
        public const string CanyonGlowMaterial = GeneratedRoot + "/Canyon/M_CanyonGlow.mat";
        public const string TowerPilotMaterial = GeneratedRoot + "/Beacon/M_TowerPilot.mat";
        public const string SkyShader = "MoonProject/World/LofiSky";
        public const string EarthShader = "MoonProject/World/LofiEarth";
        public const string GroundShader = "MoonProject/World/LofiTerrain";
        /// <summary>Art rocks (Generated/Art/Rocks) used as pebbles: the pebble, rounded and slab shapes.</summary>
        public static readonly string[] PebbleRocks = { "Rock_00", "Rock_01", "Rock_03" };

        /// <summary>Art rocks used as boulders: the jagged, big rounded and boulder shapes.</summary>
        public static readonly string[] BoulderRocks = { "Rock_02", "Rock_04", "Rock_05" };

        /// <summary>Art's warm light-decal disc (1 m radius, additive) for the disc light pools.</summary>
        public const string LightPoolPrefab = "Assets/_Project/Generated/Art/Props/LightPool.prefab";

        /// <summary>Art's landmark kit World places (M3-18 L2, L4, L7).</summary>
        public const string LandmarkFolder = "Assets/_Project/Generated/Art/Landmarks/";
        public const string RoutePostPrefab = LandmarkFolder + "RoutePost.prefab";
        public const string BridgeNearPrefab = LandmarkFolder + "ChasmBridge_Near.prefab";
        public const string BridgeFarPrefab = LandmarkFolder + "ChasmBridge_Far.prefab";
        public const string BridgeCablePrefab = LandmarkFolder + "ChasmBridge_Cable.prefab";
        public static readonly string[] DishFramePrefabs =
        {
            LandmarkFolder + "DishFrame_0.prefab", LandmarkFolder + "DishFrame_1.prefab",
            LandmarkFolder + "DishFrame_2.prefab",
        };

        /// <summary>Wires <see cref="LandmarkPrefabs"/> on a WorldSystem (scene build and test rigs).</summary>
        public static void AssignLandmarks(UnityEditor.SerializedObject world)
        {
            Landmark(world, "_routePost", RoutePostPrefab);
            Landmark(world, "_bridgeNear", BridgeNearPrefab);
            Landmark(world, "_bridgeFar", BridgeFarPrefab);
            Landmark(world, "_bridgeCable", BridgeCablePrefab);
            UnityEditor.SerializedProperty frames = world.FindProperty("_landmarkPrefabs._dishFrames");
            frames.arraySize = DishFramePrefabs.Length;
            for (int i = 0; i < DishFramePrefabs.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = Require(DishFramePrefabs[i]);
            }
        }

        private static void Landmark(UnityEditor.SerializedObject world, string field, string path)
        {
            world.FindProperty("_landmarkPrefabs." + field).objectReferenceValue = Require(path);
        }

        private static UnityEngine.GameObject Require(string path)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            return prefab != null ? prefab
                : throw new System.InvalidOperationException($"No landmark prefab at {path}; run Art/Landmarks.");
        }

        public const string CapturePoses = "Assets/_Project/Scripts/World/Editor/Captures/world_views.json";
    }
}
