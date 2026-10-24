namespace MoonProject.Rover.Editor
{
    /// <summary>Every asset the Rover builders read or write.</summary>
    public static class RoverAssetPaths
    {
        /// <summary>The art box's R1 07 v3 model (rig contract in docs/ARCHITECTURE.md). Required; never substituted.</summary>
        public const string RoverModel = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";

        /// <summary>The art box's lens glass ring (cutscenes O10), mounted on RoverModel 'Eye'. Required.</summary>
        public const string LensGlass = "Assets/_Project/Generated/Art/R1/Models/Rover/LensGlass.prefab";

        /// <summary>The art box's Focus Lens kit (cutter tier 1), mounted on LensSocket. Required.</summary>
        public const string FocusLens = "Assets/_Project/Generated/Art/R1/Models/Rover/FocusLens.prefab";

        /// <summary>The art box's scan fan (0.5.5 §1.2), mounted on the eye's ScanFanSocket. Required.</summary>
        public const string ScanFan = "Assets/_Project/Generated/Art/R1/Models/Rover/ScanFan.prefab";

        public const string GeneratedRoot = "Assets/_Project/Generated/Rover";
        public const string RoverPrefab = GeneratedRoot + "/Rover.prefab";
        public const string ScreenColliderMesh = GeneratedRoot + "/RoverScreenCollider.asset";
        public const string NozzleBladeMesh = GeneratedRoot + "/NozzleBladeMesh.asset";
        public const string NozzleCoreMesh = GeneratedRoot + "/NozzleCoreMesh.asset";
        public const string CameraRigPrefab = GeneratedRoot + "/RoverCameraRig.prefab";
        public const string SpherePhysicsMaterial = GeneratedRoot + "/Physics/RoverSphere.asset";
        public const string TrackMaterial = GeneratedRoot + "/Materials/M_RoverTrack.mat";
        public const string DustMaterial = GeneratedRoot + "/Materials/M_RoverDust.mat";
        public const string MoteMaterial = GeneratedRoot + "/Materials/M_RoverMote.mat";
        public const string ThreadMaterial = GeneratedRoot + "/Materials/M_RoverTowThread.mat";
        public const string TowCableMaterial = "Assets/_Project/Generated/Art/R1/Materials/R1_Rubber.mat";
        public const string DustTexture = GeneratedRoot + "/Textures/DustPuff.asset";

        public const string TuningFolder = "Assets/_Project/Data/Tuning";
        public const string RoverTuning = TuningFolder + "/RoverTuning.asset";
        public const string RigTuning = TuningFolder + "/RoverRigTuning.asset";
        public const string FxTuning = TuningFolder + "/RoverFxTuning.asset";
        public const string CameraTuning = TuningFolder + "/RoverCameraTuning.asset";
        public const string CharacterTuning = TuningFolder + "/RoverCharacterTuning.asset";
        public const string FaceTuning = TuningFolder + "/FaceTuning.asset";
        public const string FaceDirectorTuning = TuningFolder + "/FaceDirectorTuning.asset";

        public const string TrackShader = "Assets/_Project/Shaders/Rover/RoverTrack.shader";
        public const string ParticlesSimpleLitShader =
            "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesSimpleLit.shader";
        public const string UnlitShader = "Packages/com.unity.render-pipelines.universal/Shaders/Unlit.shader";
        public const string ParticlesUnlitShader =
            "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesUnlit.shader";
    }
}
