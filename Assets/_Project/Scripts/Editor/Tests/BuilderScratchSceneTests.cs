using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Builders;

namespace MoonProject.Editor.Tests
{
    public sealed class BuilderScratchSceneTests
    {
        private const string Folder = "Assets/__ScratchSceneTests__";

        private Scene _open;

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(Folder), $"{Folder} is left over from an earlier run.");
            _open = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            Assert.IsFalse(Directory.Exists(Folder), $"{Folder} was not removed.");
            Assert.IsFalse(File.Exists(Folder + ".meta"), $"{Folder}.meta was not removed.");
        }

        [Test]
        public void ObjectsLiveInTheScratchSceneAndNeverTouchTheOpenScene()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject root = scratch.Create("Root");
                GameObject child = scratch.Create("Child", root.transform);

                Assert.AreEqual(scratch.Scene, root.scene);
                Assert.AreEqual(scratch.Scene, child.scene);
                Assert.AreSame(root.transform, child.transform.parent);
                Assert.AreEqual(HideFlags.None, root.hideFlags, "saved prefabs must not inherit DontSave flags");
            }

            Assert.AreEqual(0, _open.rootCount);
            Assert.IsFalse(_open.isDirty);
        }

        [Test]
        public void SavedPrefabKeepsHierarchyAndPrefabInstances()
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(Folder));
            GameObject inner;
            using (var scratch = new BuilderScratchScene())
            {
                GameObject part = scratch.Create("Part");
                part.AddComponent<BoxCollider>();
                inner = GeneratedAssets.SavePrefab(part, Folder + "/Part.prefab");
            }

            GameObject saved;
            using (var scratch = new BuilderScratchScene())
            {
                GameObject root = scratch.Create("Assembly");
                GameObject instance = scratch.Instantiate(inner, root.transform);
                instance.transform.localPosition = Vector3.up;
                Assert.AreEqual(scratch.Scene, instance.scene);
                saved = GeneratedAssets.SavePrefab(root, Folder + "/Assembly.prefab");
            }

            Transform part0 = saved.transform.Find("Part");
            Assert.IsNotNull(part0);
            Assert.AreEqual(Vector3.up, part0.localPosition);
            Assert.AreSame(inner, PrefabUtility.GetCorrespondingObjectFromSource(part0.gameObject));
            Assert.AreEqual(0, _open.rootCount);
        }

        [Test]
        public void DisposeDestroysLeftoversAndBlocksFurtherUse()
        {
            var scratch = new BuilderScratchScene();
            GameObject leftover = scratch.Create("Leftover");

            scratch.Dispose();

            Assert.IsTrue(leftover == null);
            Assert.Throws<ObjectDisposedException>(() => scratch.Create("Late"));
            Assert.DoesNotThrow(scratch.Dispose);
        }
    }
}
