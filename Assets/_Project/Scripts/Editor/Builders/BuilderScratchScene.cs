using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// A private preview scene for builders that assemble temporary GameObjects (prefab roots, helper objects), so a
    /// builder never adds objects to, or dirties, the scene the user has open. Disposing it destroys whatever is left.
    /// <code>
    /// using (var scratch = new BuilderScratchScene())
    /// {
    ///     GameObject root = scratch.Create("Rock_01");
    ///     GameObject lid = scratch.Create("Lid", root.transform);
    ///     GameObject model = scratch.Instantiate(modelPrefab, root.transform);
    ///     GeneratedAssets.SavePrefab(root, "Assets/_Project/Generated/Art/Rocks/Rock_01.prefab");
    /// }
    /// </code>
    /// Create objects only through these methods: <c>new GameObject()</c>, <c>GameObject.CreatePrimitive</c> and
    /// <c>PrefabUtility.InstantiatePrefab(prefab)</c> put them in the open scene.
    /// </summary>
    public sealed class BuilderScratchScene : IDisposable
    {
        private bool _disposed;

        public BuilderScratchScene()
        {
            Scene = EditorSceneManager.NewPreviewScene();
        }

        public Scene Scene { get; }

        /// <summary>An empty GameObject at the scratch scene root, or under <paramref name="parent"/>.</summary>
        public GameObject Create(string name, Transform parent = null)
        {
            ThrowIfDisposed();

            // Born DontSave so its brief moment in the active scene (Unity has no "create in scene X") never marks
            // that scene dirty; the flags are cleared once it lives in the scratch scene.
            GameObject gameObject = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(gameObject, Scene);
            gameObject.hideFlags = HideFlags.None;
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        /// <summary>A linked instance of <paramref name="prefab"/>, created directly in the scratch scene.</summary>
        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            ThrowIfDisposed();
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Scene);
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            EditorSceneManager.ClosePreviewScene(Scene);
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BuilderScratchScene));
            }
        }
    }
}
