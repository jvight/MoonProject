using System;
using System.IO;
using UnityEngine.TestTools;
using MoonProject.Testing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Tests.PlayMode.Look
{
    /// <summary>
    /// Before the look capture enters play mode, copies the built Main.unity to a scratch scene whose bootstrap saves
    /// to this editor's own slot, so every run starts from the same fresh game and never touches the player's progress
    /// or a session on another worktree; removes the copy and the slot's files afterwards.
    /// </summary>
    public sealed class LookCaptureScene : IPrebuildSetup, IPostBuildCleanup
    {
        public const string ScenePath = Folder + "/LookCaptureMain.unity";

        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string Parent = "Assets/_Project/Generated";
        private const string FolderName = "LookCapture";
        private const string Folder = Parent + "/" + FolderName;
        private const string SceneSlotLine = "_saveSlot: main";

        /// <summary>"look-capture-&lt;process id&gt;": see <see cref="BootstrapHarness.ProcessSlot"/>.</summary>
        public static string SaveSlot => BootstrapHarness.ProcessSlot("look-capture");

        public void Setup()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
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
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            DeleteCopy();
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
