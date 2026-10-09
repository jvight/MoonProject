using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Core;
using MoonProject.World;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Editor-only preview of what the game places at boot, so the Scene view shows the basin's layout outside Play:
    /// Art's salvage wrecks on the World's site anchors and the broken relay masts on its relay anchors, read from the
    /// World's own edit-mode preview (<see cref="WorldSystem.Anchors"/>). The base itself (lander, tower, bay, shelf,
    /// rack) is part of the built scene already. The preview is one hidden root per scene (HideAndDontSave: never
    /// saved, never in a build, not in the hierarchy) built when a scene holding the gameplay and a world preview is
    /// opened or saved (the scene build saves a fresh one), removed before Play and rebuilt after it. Building it
    /// twice leaves one; a scene without both systems is left alone.
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayEditPreview
    {
        public const string RootName = "[GameplayPreview]";

        static GameplayEditPreview()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += Ensure;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += RebuildOpenScenes;
        }

        /// <summary>
        /// Rebuilds the preview of <paramref name="scene"/> from scratch; removes it when the scene has no gameplay
        /// or no world preview to read anchors from. Never in Play.
        /// </summary>
        public static void Rebuild(Scene scene)
        {
            if (Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Remove(scene);
            SalvageField salvage = FindInScene<SalvageField>(scene);
            RelayField relays = FindInScene<RelayField>(scene);
            WorldSystem world = FindInScene<WorldSystem>(scene);
            if (salvage == null || relays == null || world == null || !world.HasGeneratedWorld)
            {
                return;
            }

            var root = new GameObject(RootName) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(root, scene);
            IWorldAnchors anchors = world.Anchors;
            foreach (SalvageSiteEntry site in salvage.Catalog.Sites)
            {
                if (anchors.TryGet(site.AnchorId, out WorldAnchor anchor))
                {
                    Place(site.Prefab, anchor, root.transform);
                }
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                if (anchor.Id.StartsWith(WorldAnchorIds.RelayPrefix, StringComparison.Ordinal))
                {
                    Place(relays.BrokenPrefab, anchor, root.transform);
                }
            }
        }

        /// <summary>Builds the preview of <paramref name="scene"/> unless it already has one.</summary>
        public static void Ensure(Scene scene)
        {
            if (Root(scene) == null)
            {
                Rebuild(scene);
            }
        }

        /// <summary>Removes the preview of <paramref name="scene"/>, if any.</summary>
        public static void Remove(Scene scene)
        {
            GameObject root = Root(scene);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>The preview root of <paramref name="scene"/>, or null.</summary>
        public static GameObject Root(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == RootName && (root.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
                {
                    return root;
                }
            }

            return null;
        }

        /// <summary>Play never sees the preview: it goes before Play starts and is back once editing resumes.
        /// </summary>
        internal static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    ForEachOpenScene(Remove);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    RebuildOpenScenes();
                    break;
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Rebuild(scene);
        }

        private static void RebuildOpenScenes()
        {
            ForEachOpenScene(Rebuild);
        }

        private static void ForEachOpenScene(Action<Scene> action)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                action(SceneManager.GetSceneAt(i));
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>The prefab at the anchor, facing its (level) forward as the game places it, hidden like the root.
        /// </summary>
        private static void Place(GameObject prefab, WorldAnchor anchor, Transform root)
        {
            Vector3 forward = anchor.Forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            GameObject instance = Object.Instantiate(prefab, anchor.Position, Quaternion.LookRotation(forward), root);
            instance.name = prefab.name;
            Hide(instance.transform);
        }

        private static void Hide(Transform node)
        {
            node.gameObject.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < node.childCount; i++)
            {
                Hide(node.GetChild(i));
            }
        }
    }
}
