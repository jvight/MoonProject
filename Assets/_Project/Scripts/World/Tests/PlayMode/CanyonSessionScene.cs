using System;
using System.IO;
using UnityEngine.TestTools;
using MoonProject.Testing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Before the canyon session enters play mode, copies the built Main.unity to a scratch scene whose bootstrap
    /// saves to its own slot, so the session starts from a fresh game and never touches the player's progress;
    /// removes the copy and the slot's files afterwards.
    /// </summary>
    public sealed class CanyonSessionScene : IPrebuildSetup, IPostBuildCleanup
    {
        public const string ScenePath = Folder + "/CanyonSessionMain.unity";
        public const string SaveSlot = "canyon-session";

        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string Parent = "Assets/_Project/Generated/World";
        private const string FolderName = "CanyonSession";
        private const string Folder = Parent + "/" + FolderName;
        private const string SceneSlotLine = "_saveSlot: main";

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
