using System;
using System.IO;
using UnityEngine.TestTools;
using MoonProject.Testing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Before the playthrough enters play mode, copies the built Main.unity to a scratch scene whose bootstrap saves
    /// to its own slot, so the run starts from a fresh game and never reads or writes the player's progress; removes
    /// the copy and the slot's files afterwards.
    /// </summary>
    public sealed class PlaythroughScene : IPrebuildSetup, IPostBuildCleanup
    {
        public const string MainScene = "Assets/_Project/Scenes/Main.unity";
        public const string ScenePath = "Assets/_Project/Generated/Gameplay/Playthrough/PlaythroughMain.unity";
        public const string SaveSlot = "playthrough";

        private const string Folder = "Assets/_Project/Generated/Gameplay/Playthrough";
        private const string SceneSlotLine = "_saveSlot: main";

        public void Setup()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
            DeleteCopy();
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Generated/Gameplay", "Playthrough");
            }

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
