using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Core;
using MoonProject.World.Editor;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The built Main scene's colliders: every convex mesh collider cooks into a whole hull (past PhysX's 256-polygon
    /// limit it keeps a partial hull and warns in the player log at load, as the elders' round wheels did), and every
    /// databank story scan host blocks 07 with one simple collider on <see cref="Layers.Prop"/>.
    /// </summary>
    public sealed class SceneColliderTests
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";

        // What PhysX says when a hull goes past its polygon limit.
        private const string PartialHullWarning = "Couldn't create a Convex Mesh";

        private static readonly string[] StoryHosts =
        {
            OpeningLayoutPlacer.KestrelPanelHostName, "Elder06", "Elder01",
        };

        private Scene _scene;

        [OneTimeSetUp]
        public void OpenMainScene()
        {
            _scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CloseMainScene()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void EveryConvexMeshCollider_CooksIntoAWholeHull()
        {
            var partial = new List<string>();
            string cooking = null;
            void Listen(string message, string stack, LogType type)
            {
                if (message.Contains(PartialHullWarning) && !partial.Contains(cooking))
                {
                    partial.Add(cooking);
                }
            }

            Application.logMessageReceived += Listen;
            try
            {
                foreach (GameObject root in _scene.GetRootGameObjects())
                {
                    foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
                    {
                        if (collider.convex && collider.sharedMesh != null)
                        {
                            cooking = AnimationUtility.CalculateTransformPath(collider.transform, null);
                            Physics.BakeMesh(collider.sharedMesh.GetInstanceID(), true);
                        }
                    }
                }
            }
            finally
            {
                Application.logMessageReceived -= Listen;
            }

            Assert.IsEmpty(partial, "convex colliders cooked to a partial hull:\n" + string.Join("\n", partial));
        }

        [Test]
        public void EveryStoryHost_BlocksWithASimpleCollider_OnThePropLayer()
        {
            Transform props = World().Find(OpeningLayoutPlacer.BasinPropsRootName);
            Assert.IsNotNull(props, "[World] has its BasinProps");
            foreach (string name in StoryHosts)
            {
                Transform host = props.Find(name);
                Assert.IsNotNull(host, $"{name} stands in BasinProps");
                Assert.IsEmpty(host.GetComponentsInChildren<MeshCollider>(true), $"{name} has no mesh colliders");
                Collider[] colliders = host.GetComponentsInChildren<Collider>(true);
                Assert.AreEqual(1, colliders.Length, $"{name} has one simple collider");
                Assert.AreEqual(Layers.Prop, colliders[0].gameObject.layer, $"{name}'s collider is on the Prop layer");
            }
        }

        private Transform World()
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == "[World]")
                {
                    return root.transform;
                }
            }

            Assert.Fail("Main has no [World] root");
            return null;
        }
    }
}
