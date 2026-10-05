using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Editor.SceneBuild
{
    /// <summary>
    /// What a <see cref="ISceneContributor"/> builds with: the scene being built, one root GameObject per domain,
    /// the ordered system list that becomes <c>GameBootstrap._systems</c>, the Controls asset, and helpers that
    /// fail loudly on missing assets. Root objects are created on first use and kept in a fixed hierarchy order.
    /// </summary>
    public sealed class SceneBuildContext
    {
        public const string BootstrapRootName = "[Bootstrap]";
        public const string SystemsRootName = "[Systems]";
        public const string WorldRootName = "[World]";
        public const string RoverRootName = "[Rover]";
        public const string GameplayRootName = "[Gameplay]";
        public const string AudioRootName = "[Audio]";
        public const string UIRootName = "[UI]";

        private static readonly string[] CanonicalRootOrder =
        {
            BootstrapRootName, SystemsRootName, WorldRootName, RoverRootName, GameplayRootName, AudioRootName,
            UIRootName,
        };

        private readonly Dictionary<string, GameObject> _roots = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly List<GameObject> _rootCreationOrder = new List<GameObject>();
        private readonly List<MonoBehaviour> _systems = new List<MonoBehaviour>();

        public SceneBuildContext(Scene scene, InputActionAsset controls)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException("Scene must be valid and loaded.", nameof(scene));
            }

            Scene = scene;
            Controls = controls != null ? controls : throw new ArgumentNullException(nameof(controls));
        }

        public Scene Scene { get; }

        /// <summary>The Controls input asset the bootstrap uses (Assets/_Project/Data/Input/Controls.inputactions).</summary>
        public InputActionAsset Controls { get; }

        /// <summary>Systems in initialisation order (the order of <see cref="AddSystem{T}"/> calls).</summary>
        public IReadOnlyList<MonoBehaviour> Systems => _systems;

        public GameObject SystemsRoot => Root(SystemsRootName);

        public GameObject WorldRoot => Root(WorldRootName);

        public GameObject RoverRoot => Root(RoverRootName);

        public GameObject GameplayRoot => Root(GameplayRootName);

        public GameObject AudioRoot => Root(AudioRootName);

        public GameObject UIRoot => Root(UIRootName);

        /// <summary>The scene-root GameObject called <paramref name="name"/>, created on first request.</summary>
        public GameObject Root(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Root name is empty.", nameof(name));
            }

            if (_roots.TryGetValue(name, out GameObject root))
            {
                return root;
            }

            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, Scene);
            _roots.Add(name, root);
            _rootCreationOrder.Add(root);
            return root;
        }

        /// <summary>
        /// Appends <paramref name="system"/> (a component in this scene implementing <see cref="IGameSystem"/>) to the
        /// bootstrap's ordered system list.
        /// </summary>
        public T AddSystem<T>(T system) where T : MonoBehaviour
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (!(system is IGameSystem))
            {
                throw new ArgumentException($"{system.GetType().Name} does not implement {nameof(IGameSystem)}.",
                    nameof(system));
            }

            if (system.gameObject.scene != Scene)
            {
                throw new ArgumentException($"{system.name} is not in the scene being built.", nameof(system));
            }

            if (_systems.Contains(system))
            {
                throw new ArgumentException($"{system.name} ({system.GetType().Name}) was already added.",
                    nameof(system));
            }

            _systems.Add(system);
            return system;
        }

        /// <summary>Creates a GameObject called <paramref name="name"/> under [Systems] with a new
        /// <typeparamref name="T"/> and adds it as a system.</summary>
        public T CreateSystem<T>(string name) where T : MonoBehaviour
        {
            GameObject host = CreateChild(name, SystemsRoot.transform);
            return AddSystem(host.AddComponent<T>());
        }

        /// <summary>An empty child GameObject (identity transform) of <paramref name="parent"/>.</summary>
        public GameObject CreateChild(string name, Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        /// <summary>Loads the asset at <paramref name="path"/>; throws if it is missing (fail fast on broken content).</summary>
        public T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Scene build: no {typeof(T).Name} at {path}. " +
                                                    "Run its builder first (MoonProject/Build/Build All).");
            }

            return asset;
        }

        /// <summary>Instantiates the prefab at <paramref name="prefabPath"/> (keeping the prefab link).</summary>
        public GameObject InstantiatePrefab(string prefabPath, Transform parent)
        {
            return InstantiatePrefab(LoadAsset<GameObject>(prefabPath), parent, Vector3.zero, Quaternion.identity);
        }

        /// <summary>Instantiates <paramref name="prefab"/> under <paramref name="parent"/> at a local pose.</summary>
        public GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 localPosition,
            Quaternion localRotation)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        /// <summary>Puts the domain roots in canonical order (known roots first, others in creation order).</summary>
        public void SortRoots()
        {
            int index = 0;
            foreach (string name in CanonicalRootOrder)
            {
                if (_roots.TryGetValue(name, out GameObject root))
                {
                    root.transform.SetSiblingIndex(index++);
                }
            }

            foreach (GameObject root in _rootCreationOrder)
            {
                if (Array.IndexOf(CanonicalRootOrder, root.name) < 0)
                {
                    root.transform.SetSiblingIndex(index++);
                }
            }
        }
    }
}
