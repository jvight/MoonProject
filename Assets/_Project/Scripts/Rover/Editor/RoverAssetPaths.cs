namespace MoonProject.Rover.Editor
{
    /// <summary>Every asset the Rover builders read or write.</summary>
    public static class RoverAssetPaths
    {
        /// <summary>The art box's model (rig contract in docs/ARCHITECTURE.md). Required; never substituted.</summary>
        public const string RoverModel = "Assets/_Project/Generated/Art/Rover/RoverModel.prefab";

        /// <summary>The art box's Hover-Jump coils, mounted on RoverModel 'CoilSocket'. Required.</summary>
        public const string HoverCoils = "Assets/_Project/Generated/Art/Rover/HoverCoils.prefab";

        public const string GeneratedRoot = "Assets/_Project/Generated/Rover";
        public const string RoverPrefab = GeneratedRoot + "/Rover.prefab";
        public const string CameraRigPrefab = GeneratedRoot + "/RoverCameraRig.prefab";
        public const string SpherePhysicsMaterial = GeneratedRoot + "/Physics/RoverSphere.asset";
        public const string TrackMaterial = GeneratedRoot + "/Materials/M_RoverTrack.mat";
        public const string DustMaterial = GeneratedRoot + "/Materials/M_RoverDust.mat";
        public const string DustTexture = GeneratedRoot + "/Textures/DustPuff.asset";

        public const string TuningFolder = "Assets/_Project/Data/Tuning";
        public const string RoverTuning = TuningFolder + "/RoverTuning.asset";
        public const string RigTuning = TuningFolder + "/RoverRigTuning.asset";
        public const string FxTuning = TuningFolder + "/RoverFxTuning.asset";
        public const string CameraTuning = TuningFolder + "/RoverCameraTuning.asset";
        public const string CharacterTuning = TuningFolder + "/RoverCharacterTuning.asset";

        public const string TrackShader = "Assets/_Project/Shaders/Rover/RoverTrack.shader";
        public const string ParticlesSimpleLitShader =
            "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesSimpleLit.shader";
    }
}
