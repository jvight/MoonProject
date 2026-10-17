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
        /// <summary>The orange vent's bead field (M3-27): the rover's track shader tinted volcanic orange.</summary>
        public const string VentBeadMaterial = GeneratedRoot + "/M_VentBeads.mat";
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
        public const string BeaconHaloShader = "Assets/_Project/Shaders/World/LofiBeaconHalo.shader";
        public const string CanyonGlowMaterial = GeneratedRoot + "/Canyon/M_CanyonGlow.mat";
        public const string TowerPilotMaterial = GeneratedRoot + "/Beacon/M_TowerPilot.mat";
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

        /// <summary>
        /// Art's R1 structures World stands as props until their own models land (opening v2 §4): the dead charging
        /// post at the wake (a PowerPylon) and the dead west relay mast (an unlit AntennaMast).
        /// </summary>
        public const string WakePostPrefab = "Assets/_Project/Generated/Art/R1/Models/Structures/PowerPylon.prefab";
        public const string WestMastPrefab = "Assets/_Project/Generated/Art/R1/Models/Structures/AntennaMast.prefab";

        /// <summary>Art's R1 rubber, for 07's old charging cable on the dust.</summary>
        public const string CableMaterial = "Assets/_Project/Generated/Art/R1/Materials/R1_Rubber.mat";

        /// <summary>Art's R1 cargo lander, landed at place.lander in the opening (opening v2 §4).</summary>
        public const string CargoLanderPrefab = "Assets/_Project/Generated/Art/R1/Models/Lander/CargoLander.prefab";

        /// <summary>Art's landmark kit World places (M3-18 L2, L4, L7).</summary>
        public const string LandmarkFolder = "Assets/_Project/Generated/Art/Landmarks/";
        /// <summary>Art's props folder, where Kestrel-3's HullFragment variants are (0.5.5).</summary>
        public const string HullFragmentFolder = "Assets/_Project/Generated/Art/Props/";
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

            UnityEditor.SerializedProperty fragments = world.FindProperty("_landmarkPrefabs._hullFragments");
            fragments.arraySize = MoonProject.Art.Editor.PropModelBuilder.HullFragmentVariants;
            for (int i = 0; i < fragments.arraySize; i++)
            {
                fragments.GetArrayElementAtIndex(i).objectReferenceValue = Require(HullFragmentFolder
                    + MoonProject.Art.Editor.PropModelBuilder.HullFragmentPrefix + i + ".prefab");
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
