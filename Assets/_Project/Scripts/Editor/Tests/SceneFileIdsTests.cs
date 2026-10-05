using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.App;
using MoonProject.Editor.SceneBuild;
using MoonProject.Testing;

namespace MoonProject.Editor.Tests
{
    /// <summary>
    /// Builds the same non-trivial scene twice (nested hierarchy, duplicate names, cross-object references, prefab
    /// instances with overrides, an added child and an added component) and proves that normalised files are
    /// byte-identical, survive a Unity open + save unchanged, and keep every reference.
    /// </summary>
    public sealed class SceneFileIdsTests
    {
        private const string Folder = "Assets/__SceneFileIdsTests__";
        private const string PrefabPath = Folder + "/Crate.prefab";

        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(Folder), $"{Folder} is left over from an earlier run.");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            var root = new GameObject("Crate");
            var lid = new GameObject("Lid");
            lid.transform.SetParent(root.transform, false);
            lid.AddComponent<BoxCollider>();
            _prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(Folder);

            Assert.IsFalse(Directory.Exists(Folder), $"{Folder} was not removed.");
            Assert.IsFalse(File.Exists(Folder + ".meta"), $"{Folder}.meta was not removed.");
        }

        [Test]
        public void BuildingTwice_GivesIdenticalFiles_WhereUnityAloneDoesNot()
        {
            string rawA = BuildAndSave(Folder + "/A.unity");
            string rawB = BuildAndSave(Folder + "/B.unity");
            Assert.AreNotEqual(rawA, rawB, "Unity's own file IDs differ between identical builds");

            SceneFileIds.NormalizeFile(Folder + "/A.unity");
            SceneFileIds.NormalizeFile(Folder + "/B.unity");

            Assert.AreEqual(File.ReadAllText(Folder + "/A.unity"), File.ReadAllText(Folder + "/B.unity"));
        }

        [Test]
        public void NormalisedScene_SurvivesUnityOpenAndSaveUnchanged()
        {
            string path = Folder + "/A.unity";
            BuildAndSave(path);
            SceneFileIds.NormalizeFile(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            string normalized = File.ReadAllText(path);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene));

            Assert.AreEqual(normalized, File.ReadAllText(path));
        }

        [Test]
        public void NormalisedScene_KeepsReferencesAndPrefabOverrides()
        {
            string path = Folder + "/A.unity";
            BuildAndSave(path);
            SceneFileIds.NormalizeFile(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            GameObject[] roots = scene.GetRootGameObjects();
            Assert.AreEqual(new[] { "[Bootstrap]", "[Systems]", "[World]" }, System.Array.ConvertAll(roots, r => r.name));
            var bootstrap = roots[0].GetComponent<GameBootstrap>();
            SerializedProperty systems = new SerializedObject(bootstrap).FindProperty("_systems");
            Assert.AreEqual(2, systems.arraySize);
            Assert.AreSame(roots[1].transform.GetChild(1).GetComponent<RecordingSystem>(),
                systems.GetArrayElementAtIndex(0).objectReferenceValue, "systems keep their serialized order");
            Assert.AreSame(roots[1].transform.GetChild(0).GetComponent<RecordingSystem>(),
                systems.GetArrayElementAtIndex(1).objectReferenceValue);

            Transform world = roots[2].transform;
            Transform moved = world.Find("Crate");
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(moved.gameObject));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), moved.localPosition, "override kept");
            Transform extended = world.GetChild(3);
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(extended.gameObject));
            Assert.IsNotNull(extended.Find("Extra"), "added child kept");
            Assert.IsNotNull(extended.GetComponent<SphereCollider>(), "added component kept");
            Assert.IsNotNull(extended.Find("Lid").GetComponent<BoxCollider>(), "prefab content intact");
        }

        [Test]
        public void Normalize_IsIdempotent()
        {
            string path = Folder + "/A.unity";
            BuildAndSave(path);
            string once = SceneFileIds.Normalize(File.ReadAllText(path));

            Assert.AreEqual(once, SceneFileIds.Normalize(once));
        }

        /// <summary>Builds the reference scene from scratch and saves it with Unity's own IDs; returns the text.</summary>
        private string BuildAndSave(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapRoot = new GameObject("[Bootstrap]");
            var bootstrap = bootstrapRoot.AddComponent<GameBootstrap>();
            var systemsRoot = new GameObject("[Systems]");
            RecordingSystem first = AddSystem(systemsRoot.transform);
            RecordingSystem second = AddSystem(systemsRoot.transform);
            var serialized = new SerializedObject(bootstrap);
            SerializedProperty systems = serialized.FindProperty("_systems");
            systems.arraySize = 2;
            systems.GetArrayElementAtIndex(0).objectReferenceValue = second;
            systems.GetArrayElementAtIndex(1).objectReferenceValue = first;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var world = new GameObject("[World]");
            world.AddComponent<Light>();
            new GameObject("Dup").transform.SetParent(world.transform, false);
            new GameObject("Dup").transform.SetParent(world.transform, false);
            var moved = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, world.transform);
            moved.transform.localPosition = new Vector3(1f, 2f, 3f);
            var extended = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, world.transform);
            new GameObject("Extra").transform.SetParent(extended.transform, false);
            extended.AddComponent<SphereCollider>();

            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            return File.ReadAllText(path);
        }

        private static RecordingSystem AddSystem(Transform parent)
        {
            var host = new GameObject("Sys");
            host.transform.SetParent(parent, false);
            return host.AddComponent<RecordingSystem>();
        }
    }
}
