using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Testing
{
    /// <summary>
    /// Builds a <see cref="GameBootstrap"/> at runtime the way MainSceneBuilder wires it in Main.unity (Controls asset +
    /// ordered system list through the serialized fields), then activates it so Awake initialises the systems.
    /// For PlayMode tests running in the editor.
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
        /// Creates "[Bootstrap]" with <paramref name="controls"/> and <paramref name="systems"/> (in that order) wired
        /// into its serialized fields, then activates it: GameBootstrap.Awake runs before this returns.
        /// </summary>
        public static GameBootstrap Create(InputActionAsset controls, params MonoBehaviour[] systems)
        {
#if UNITY_EDITOR
            var host = new GameObject("[Bootstrap]");
            host.SetActive(false);
            var bootstrap = host.AddComponent<GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_inputActions").objectReferenceValue = controls;
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
    }
}
