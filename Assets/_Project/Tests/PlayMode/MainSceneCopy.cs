using System;
using System.IO;
using UnityEngine.TestTools;
using MoonProject.Testing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Tests.PlayMode
{
    /// <summary>
    /// Before a session enters play mode, copies the built Main.unity to a scratch scene whose bootstrap saves to this
    /// editor's own slot, so the session never touches the player's progress or a session on another worktree;
    /// removes the copy and the slot's files afterwards. Derive one sealed class per session (IPrebuildSetup needs a
    /// parameterless constructor).
    /// </summary>
    public abstract class MainSceneCopy : IPrebuildSetup, IPostBuildCleanup
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string Parent = "Assets/_Project/Generated";
        private const string SceneSlotLine = "_saveSlot: main";

        private readonly string _folderName;
        private readonly string _slotName;

        protected MainSceneCopy(string folderName, string slotName)
        {
            _folderName = folderName;
            _slotName = slotName;
        }

        public string ScenePath => Folder + "/" + _folderName + "Main.unity";

        /// <summary>"&lt;slot name&gt;-&lt;process id&gt;": see <see cref="BootstrapHarness.ProcessSlot"/>.</summary>
        public string SaveSlot => BootstrapHarness.ProcessSlot(_slotName);

        private string Folder => Parent + "/" + _folderName;

        public void Setup()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(SaveSlot);
            DeleteCopy();
            AssetDatabase.CreateFolder(Parent, _folderName);
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
        private void DeleteCopy()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }
        }
#endif
    }
}
