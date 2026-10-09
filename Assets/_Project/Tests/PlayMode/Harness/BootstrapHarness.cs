using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Testing
{
    /// <summary>
    /// Builds a <see cref="GameBootstrap"/> at runtime the way MainSceneBuilder wires it in Main.unity (Controls asset +
    /// ordered system list through the serialized fields), then activates it so Awake initialises the systems and
    /// loads the save. Every bootstrap gets its own save slot (never the player's "main"), so tests cannot touch real
    /// progress; delete the slot's files with <see cref="DeleteSaveFiles"/>. Every worktree's batch editor saves under
    /// the same persistentDataPath, so a slot shared between runs is never fixed: see <see cref="ProcessSlot"/>. For
    /// PlayMode tests running in the editor.
    /// </summary>
    public static class BootstrapHarness
    {
        public const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";

        /// <summary>
        /// A private copy of the Controls asset, so enabling maps in a test never touches the project asset.
        /// Destroy it in TearDown.
        /// </summary>
        public static InputActionAsset LoadControlsCopy()
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Controls asset missing at {ControlsPath}.");
            }

            return Object.Instantiate(asset);
#else
            throw new NotSupportedException("BootstrapHarness loads assets through the editor's AssetDatabase.");
#endif
        }

        /// <summary>
        /// The save slot "<paramref name="name"/>-&lt;process id&gt;" of a scratch copy of the real scene: the same
        /// throughout this editor process (the copy's bootstrap, the test and both cleanups agree on it), never the
        /// same as a batch editor running that test on another worktree at the same time.
        /// </summary>
        public static string ProcessSlot(string name)
        {
            using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
            {
                return name + "-" + process.Id.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>A fresh save slot name for one test ("test-&lt;guid&gt;").</summary>
        public static string NewTestSlot()
        {
            return "test-" + Guid.NewGuid().ToString("N");
        }

        /// <summary>Like <see cref="Create(InputActionAsset, string, MonoBehaviour[])"/> with a fresh test slot.</summary>
        public static GameBootstrap Create(InputActionAsset controls, params MonoBehaviour[] systems)
        {
            return Create(controls, NewTestSlot(), systems);
        }

        /// <summary>
        /// Creates "[Bootstrap]" with <paramref name="controls"/>, save slot <paramref name="saveSlot"/> and
        /// <paramref name="systems"/> (in that order) wired into its serialized fields, then activates it:
        /// GameBootstrap.Awake (initialise systems, load the save) runs before this returns.
        /// </summary>
        public static GameBootstrap Create(InputActionAsset controls, string saveSlot, params MonoBehaviour[] systems)
        {
#if UNITY_EDITOR
            var host = new GameObject("[Bootstrap]");
            host.SetActive(false);
            var bootstrap = host.AddComponent<GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_inputActions").objectReferenceValue = controls;
            serialized.FindProperty("_saveSlot").stringValue = saveSlot;
            SerializedProperty list = serialized.FindProperty("_systems");
            list.arraySize = systems.Length;
            for (int i = 0; i < systems.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
            return bootstrap;
#else
            throw new NotSupportedException("BootstrapHarness wires GameBootstrap through the editor's SerializedObject.");
#endif
        }

        /// <summary>Deletes every file of <paramref name="saveSlot"/> (save, backup, temp, set-aside corrupt files).</summary>
        public static void DeleteSaveFiles(string saveSlot)
        {
            string directory = SaveService.DefaultDirectory;
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, saveSlot + SaveService.Extension + "*"))
            {
                File.Delete(file);
            }
        }
    }
}
