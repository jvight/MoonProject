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

        public void Setup()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
            DeletePutAway();
            DeleteCopy();
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
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int kept = 0;
            var listed = new EditorBuildSettingsScene[scenes.Length + 1];
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath)
                {
                    listed[kept++] = scene;
                }
            }

            listed[kept++] = new EditorBuildSettingsScene(ScenePath, true);
            Array.Resize(ref listed, kept);
            EditorBuildSettings.scenes = listed;
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int kept = 0;
            var restored = new EditorBuildSettingsScene[scenes.Length];
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath)
                {
                    restored[kept++] = scene;
                }
            }

            if (kept != scenes.Length)
            {
                Array.Resize(ref restored, kept);
                EditorBuildSettings.scenes = restored;
            }

            DeleteCopy();
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
            DeletePutAway();
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

        private static void DeletePutAway()
        {
            string directory = MoonProject.Core.Save.SaveService.DefaultDirectory;
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, SaveSlot + MoonProject.Core.Save.SaveService.PutAwayMarker + "*"))
            {
                File.Delete(file);
            }
        }
#endif
    }
}
