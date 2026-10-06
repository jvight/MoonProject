using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MoonProject.App;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;

namespace MoonProject.Editor.SceneBuild
{
    /// <summary>
    /// Builds <c>Assets/_Project/Scenes/Main.unity</c> from an empty scene on every run: creates the
    /// <see cref="GameBootstrap"/>, runs every <see cref="ISceneContributor"/> in order, wires the bootstrap's Controls
    /// asset and ordered system list, saves with structural file IDs (<see cref="SceneFileIds"/>: same content, same
    /// bytes), and makes the scene build index 0. Nothing is saved if any contributor fails.
    /// Entry points: menu MoonProject/Build/Main Scene, Build All (it runs last), <see cref="EditorCommands"/>, and batch:
    /// <code>python tools/unity_batch.py exec --method MoonProject.Editor.SceneBuild.MainSceneBuilder.BuildMainScene</code>
    /// </summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";
        public const string BuilderPath = "Main Scene";
        public const string BuilderPattern = "^" + BuilderPath + "$";
        public const int BuilderOrder = 1000;

        private const string InputActionsField = "_inputActions";
        private const string SystemsField = "_systems";

        public static void BuildMainScene()
        {
            BatchRunner.Run(nameof(BuildMainScene), args => BuilderRegistry.DiscoverAndRun(BuilderPattern));
        }

        [MenuItem(BuilderWindow.BuildMenuRoot + BuilderPath, priority = 20)]
        private static void BuildFromMenu()
        {
            BuilderWindow.RunFromMenu(BuilderPattern);
        }

        [MoonBuilder(BuilderPath, BuilderOrder, ReplacesOpenScene = true)]
        private static void Build()
        {
            // Load after NewScene: opening a scene in Single mode unloads unreferenced assets loaded before it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                throw new InvalidOperationException($"Controls asset missing at {ControlsPath}.");
            }

            var context = new SceneBuildContext(scene, controls);
            var bootstrap = context.Root(SceneBuildContext.BootstrapRootName).AddComponent<GameBootstrap>();
            context.Root(SceneBuildContext.SystemsRootName);

            IReadOnlyList<ISceneContributor> contributors = DiscoverContributors();
            bool failed = false;
            foreach (ISceneContributor contributor in contributors)
            {
                string name = contributor.GetType().FullName;
                BatchRunner.Log($"scene: {name} (order {contributor.Order})");
                using (var capture = new LogCapture())
                {
                    try
                    {
                        contributor.Contribute(context);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }

                    if (capture.ErrorCount > 0)
                    {
                        failed = true;
                        Debug.LogError($"Scene contributor {name} failed; Main.unity was not saved.");
                    }
                }
            }

            if (failed)
            {
                return;
            }

            context.SortRoots();
            WireBootstrap(bootstrap, controls, context.Systems);
            // Describe before saving: the reload below destroys the objects the context still references.
            string systemsSummary = DescribeSystems(context.Systems);
            int systemCount = context.Systems.Count;
            GeneratedAssets.EnsureFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Saving {ScenePath} failed.");
            }

            // Unity allocates random file IDs; derive them from the scene structure so identical builds are
            // byte-identical, then reload so the open scene carries the normalised IDs.
            SceneFileIds.NormalizeFile(ScenePath);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            MakeFirstBuildScene();
            BatchRunner.Log($"scene: saved {ScenePath} with {contributors.Count} contributor(s), " +
                            $"{systemCount} system(s): {systemsSummary}");
        }

        /// <summary>
        /// Every non-abstract <see cref="ISceneContributor"/> in the loaded editor assemblies, instantiated through its
        /// public parameterless constructor and sorted by order, then type name.
        /// </summary>
        public static IReadOnlyList<ISceneContributor> DiscoverContributors()
        {
            var contributors = new List<ISceneContributor>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ISceneContributor>())
            {
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"Scene contributor {type.FullName} needs a public parameterless constructor.");
                    continue;
                }

                contributors.Add((ISceneContributor)Activator.CreateInstance(type));
            }

            contributors.Sort((a, b) =>
            {
                int byOrder = a.Order.CompareTo(b.Order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName);
            });
            return contributors;
        }

        private static void WireBootstrap(GameBootstrap bootstrap, InputActionAsset controls,
            IReadOnlyList<MonoBehaviour> systems)
        {
            var serialized = new SerializedObject(bootstrap);
            SerializedProperty actions = RequireProperty(serialized, InputActionsField);
            SerializedProperty list = RequireProperty(serialized, SystemsField);
            actions.objectReferenceValue = controls;
            list.arraySize = systems.Count;
            for (int i = 0; i < systems.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty RequireProperty(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameBootstrap)} has no serialized field '{name}'; update {nameof(MainSceneBuilder)}.");
            }

            return property;
        }

        private static void MakeFirstBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!string.Equals(existing.path, ScenePath, StringComparison.Ordinal))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string DescribeSystems(IReadOnlyList<MonoBehaviour> systems)
        {
            if (systems.Count == 0)
            {
                return "none";
            }

            var names = new string[systems.Count];
            for (int i = 0; i < systems.Count; i++)
            {
                names[i] = $"{systems[i].name}:{systems[i].GetType().Name}";
            }

            return string.Join(", ", names);
        }
    }
}
