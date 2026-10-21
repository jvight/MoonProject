using System.Collections.Generic;
using System.Globalization;
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

        private static readonly string[] RelayHutNodes =
        {
            "RelayHut", "RelayHut/Door", "RelayHut/MastStub", "RelayHut/MastStub/RelayLamp", "ScanApproach",
        };

        private const string CellPackSocketPrefix = "Topper_Socket/SSR3_Topper_06/FragmentSocket_CellPack_";
        private const string CellPackNamePrefix = "Fragment_CellPack_";
        private const int CellPackCount = 3;
        private const float MaxBayOffApproachDegrees = 25f;

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

        [Test]
        public void Relay2Site_UsesArtsHut_And06ShowsItsOpenBatteryBay_TowardScanApproach()
        {
            Transform world = World();
            Transform site = world.Find(Relay2SitePlacer.SiteName);
            Assert.IsNotNull(site, "[World] has Relay2Site");
            foreach (string node in RelayHutNodes)
            {
                Assert.IsNotNull(site.Find(node), $"Relay2Site/{node} exists");
            }

            Transform hut = site.Find("RelayHut");
            Assert.IsEmpty(hut.GetComponentsInChildren<MeshCollider>(true), "RelayHut has no mesh colliders");
            Collider[] hutColliders = hut.GetComponentsInChildren<Collider>(true);
            Assert.AreEqual(1, hutColliders.Length, "RelayHut has one simple collider");
            Assert.AreEqual(Layers.Prop, hutColliders[0].gameObject.layer, "RelayHut blocks on Prop layer");

            Transform six = world.Find(OpeningLayoutPlacer.BasinPropsRootName + "/Elder06");
            Assert.IsNotNull(six, "Elder06 stands in BasinProps");
            for (int i = 0; i < CellPackCount; i++)
            {
                string index = i.ToString(CultureInfo.InvariantCulture);
                Transform cell = six.Find(CellPackSocketPrefix + index + "/" + CellPackNamePrefix + index);
                Assert.IsNotNull(cell, $"Elder06 has {CellPackNamePrefix}{index} seated in its socket");
                Assert.IsNotNull(cell.GetComponentInChildren<Renderer>(), $"{CellPackNamePrefix}{index} renders");
            }

            Transform approach = site.Find("ScanApproach");
            Vector3 toApproach = Vector3.ProjectOnPlane(approach.position - six.position, Vector3.up).normalized;
            Vector3 bayOutward = Vector3.ProjectOnPlane(six.right, Vector3.up).normalized;
            Assert.LessOrEqual(Vector3.Angle(bayOutward, toApproach), MaxBayOffApproachDegrees,
                "06's open battery bay faces ScanApproach");
            Assert.Greater(Vector3.Angle(-six.forward, Vector3.up), 1f, "06 keeps its slump");
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
