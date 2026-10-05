using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using Object = UnityEngine.Object;

namespace MoonProject.Editor.Tests
{
    public sealed class GeneratedAssetsTests
    {
        /// <summary>Dedicated top-level temp folder: nothing else under Assets is created or touched.</summary>
        private const string Folder = "Assets/__MoonEditorTests__";

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(Folder), $"{Folder} is left over from an earlier run.");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);

            Assert.IsFalse(AssetDatabase.IsValidFolder(Folder), $"{Folder} was not removed.");
            Assert.IsFalse(Directory.Exists(Folder), $"{Folder} still exists on disk.");
            Assert.IsFalse(File.Exists(Folder + ".meta"), $"{Folder}.meta still exists on disk.");
        }

        [Test]
        public void CreateOrReplace_KeepsGuidAndUpdatesContent()
        {
            string path = Folder + "/Nested/Tri.asset";
            Mesh first = GeneratedAssets.CreateOrReplace(Triangle(1f), path);
            string guid = AssetDatabase.AssetPathToGUID(path);

            Mesh second = GeneratedAssets.CreateOrReplace(Triangle(2f), path);

            Assert.AreSame(first, second);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual(2f, second.vertices[1].x);
            Assert.AreEqual("Tri", second.name);
        }

        [Test]
        public void CreateOrReplace_DifferentTypeAtPath_Throws()
        {
            string path = Folder + "/Thing.asset";
            GeneratedAssets.CreateOrReplace(Triangle(1f), path);
            var material = new Material(Shader.Find("Hidden/InternalErrorShader"));

            Assert.Throws<InvalidOperationException>(() => GeneratedAssets.CreateOrReplace(material, path));
            Object.DestroyImmediate(material);
        }

        [Test]
        public void SavePrefab_KeepsGuidAndDestroysSource()
        {
            string path = Folder + "/Box.prefab";
            GeneratedAssets.SavePrefab(new GameObject("A"), path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var source = new GameObject("B");
            source.AddComponent<BoxCollider>();

            GameObject prefab = GeneratedAssets.SavePrefab(source, path);

            Assert.IsTrue(source == null, "the scene object is destroyed after saving");
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.IsNotNull(prefab.GetComponent<BoxCollider>());
            Assert.AreEqual("Box", prefab.name);
        }

        [Test]
        public void WriteFileIfChanged_SkipsIdenticalContent()
        {
            string path = Folder + "/data.bytes";
            byte[] bytes = { 1, 2, 3 };

            Assert.IsTrue(GeneratedAssets.WriteFileIfChanged(path, bytes));
            Assert.IsFalse(GeneratedAssets.WriteFileIfChanged(path, bytes));
            Assert.IsTrue(GeneratedAssets.WriteFileIfChanged(path, new byte[] { 1, 2, 4 }));
        }

        [Test]
        public void PathsOutsideAssets_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => GeneratedAssets.EnsureFolder("Library/Foo"));
            Assert.Throws<ArgumentException>(() => GeneratedAssets.WriteFileIfChanged("C:/temp/x.bytes", new byte[1]));
        }

        private static Mesh Triangle(float x)
        {
            return new Mesh
            {
                vertices = new[] { Vector3.zero, new Vector3(x, 0f, 0f), Vector3.up },
                triangles = new[] { 0, 1, 2 },
            };
        }
    }
}
