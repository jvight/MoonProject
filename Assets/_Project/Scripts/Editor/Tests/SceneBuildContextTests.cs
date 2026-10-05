using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MoonProject.App;
using MoonProject.Editor.SceneBuild;
using MoonProject.Testing;

namespace MoonProject.Editor.Tests
{
    public sealed class SceneBuildContextTests
    {
        private Scene _scene;
        private Scene _otherScene;
        private SceneBuildContext _context;

        [SetUp]
        public void SetUp()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(MainSceneBuilder.ControlsPath);
            Assert.IsNotNull(controls, $"Controls asset missing at {MainSceneBuilder.ControlsPath}");
            _scene = EditorSceneManager.NewPreviewScene();
            _context = new SceneBuildContext(_scene, controls);
        }

        [TearDown]
        public void TearDown()
        {
            if (_otherScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_otherScene);
            }

            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void Root_IsCreatedOnceInTheBuiltScene()
        {
            GameObject first = _context.Root("[Custom]");

            Assert.AreSame(first, _context.Root("[Custom]"));
            Assert.AreEqual(_scene, first.scene);
            Assert.AreSame(_context.WorldRoot, _context.Root(SceneBuildContext.WorldRootName));
        }

        [Test]
        public void SortRoots_PutsKnownRootsInCanonicalOrderThenOthers()
        {
            GameObject custom = _context.Root("[Custom]");
            GameObject ui = _context.UIRoot;
            GameObject world = _context.WorldRoot;
            GameObject bootstrap = _context.Root(SceneBuildContext.BootstrapRootName);
            GameObject systems = _context.SystemsRoot;

            _context.SortRoots();

            CollectionAssert.AreEqual(new[] { bootstrap, systems, world, ui, custom }, _scene.GetRootGameObjects());
        }

        [Test]
        public void AddSystem_KeepsCallOrder()
        {
            var b = _context.CreateSystem<RecordingSystem>("B");
            var a = _context.CreateSystem<RecordingSystem>("A");

            CollectionAssert.AreEqual(new[] { b, a }, _context.Systems);
            Assert.AreSame(_context.SystemsRoot.transform, a.transform.parent);
        }

        [Test]
        public void AddSystem_RejectsNonSystemsDuplicatesAndForeignObjects()
        {
            GameObject host = _context.CreateChild("Host", _context.SystemsRoot.transform);
            var plain = host.AddComponent<GameBootstrap>();
            var system = host.AddComponent<RecordingSystem>();
            _context.AddSystem(system);
            _otherScene = EditorSceneManager.NewPreviewScene();
            var foreign = new GameObject("Foreign");
            SceneManager.MoveGameObjectToScene(foreign, _otherScene);

            Assert.Throws<ArgumentException>(() => _context.AddSystem(plain));
            Assert.Throws<ArgumentException>(() => _context.AddSystem(system));
            Assert.Throws<ArgumentException>(() => _context.AddSystem(foreign.AddComponent<RecordingSystem>()));
            Assert.AreEqual(1, _context.Systems.Count);
        }

        [Test]
        public void LoadAsset_Missing_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => _context.LoadAsset<GameObject>("Assets/__does_not_exist__.prefab"));
        }

        [Test]
        public void InstantiatePrefab_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => _context.InstantiatePrefab(null, _context.WorldRoot.transform, Vector3.zero, Quaternion.identity));
        }
    }
}
