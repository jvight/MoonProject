using System;
using System.IO;
using UnityEngine.TestTools;
using MoonProject.Testing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Before the rover's real-game session enters play mode, copies the built Main.unity to a scratch scene whose
    /// bootstrap saves to its own slot, so the session starts from a fresh game and never touches the player's
    /// progress, nor a session running on another worktree at the same time; removes the copy and the slot's files
    /// afterwards.
    /// </summary>
    public sealed class RoverSessionScene : IPrebuildSetup, IPostBuildCleanup
    {
        public const string ScenePath = Folder + "/RoverSessionMain.unity";
        /// <summary>
        /// "rover-session-&lt;process id&gt;", this editor's own: see <see cref="BootstrapHarness.ProcessSlot"/>.
        /// </summary>
        public static string SaveSlot => BootstrapHarness.ProcessSlot("rover-session");

        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string Parent = "Assets/_Project/Generated/Rover";
        private const string FolderName = "Session";
        private const string Folder = Parent + "/" + FolderName;
        private const string SceneSlotLine = "_saveSlot: main";
        private const string RoverPrefab = "Assets/_Project/Generated/Rover/Rover.prefab";
        private const string OriginalRoverPrefabKey = "MoonProject.RoverSessionScene.OriginalRoverPrefab";
        private const string RoverSetupDepthKey = "MoonProject.RoverSessionScene.RoverSetupDepth";

        public void Setup()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
            DeleteCopy();
            int depth = SessionState.GetInt(RoverSetupDepthKey, 0);
            if (depth == 0)
            {
                var rover = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RoverPrefab);
                if (rover == null || !rover.TryGetComponent(out RoverBodyLanguage body) || body.FaceDirector == null)
                {
                    if (File.Exists(RoverPrefab))
                    {
                        SessionState.SetString(OriginalRoverPrefabKey, File.ReadAllText(RoverPrefab));
                    }

                    Type roverBuilder = Type.GetType("MoonProject.Rover.Editor.RoverPrefabBuilder, MoonProject.Rover.Editor");
                    roverBuilder?.GetMethod("Build")?.Invoke(null, null);
                }
            }

            SessionState.SetInt(RoverSetupDepthKey, depth + 1);
            AssetDatabase.CreateFolder(Parent, FolderName);
            string text = File.ReadAllText(MainScene);
            int first = text.IndexOf(SceneSlotLine, StringComparison.Ordinal);
            if (first < 0 || text.IndexOf(SceneSlotLine, first + 1, StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException(
                    $"{MainScene} must hold exactly one '{SceneSlotLine}' (the bootstrap's save slot).");
            }

            File.WriteAllText(ScenePath, text.Replace(SceneSlotLine, "_saveSlot: " + SaveSlot));
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            DeleteCopy();
            int depth = SessionState.GetInt(RoverSetupDepthKey, 0);
            if (depth > 0)
            {
                depth--;
                SessionState.SetInt(RoverSetupDepthKey, depth);
                if (depth == 0)
                {
                    string originalRover = SessionState.GetString(OriginalRoverPrefabKey, string.Empty);
                    if (!string.IsNullOrEmpty(originalRover))
                    {
                        File.WriteAllText(RoverPrefab, originalRover);
                        SessionState.EraseString(OriginalRoverPrefabKey);
                        AssetDatabase.ImportAsset(RoverPrefab, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }

            BootstrapHarness.DeleteSaveFiles(SaveSlot);
#endif
        }

#if UNITY_EDITOR
        private static void DeleteCopy()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }
        }
#endif
    }
}
