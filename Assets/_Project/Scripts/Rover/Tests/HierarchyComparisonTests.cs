using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The Rover builders skip saving a prefab the comparison calls unchanged, so it must see every serialized change
    /// (or the generated prefab goes stale) and nothing else (or re-runs churn the file).
    /// </summary>
    public sealed class HierarchyComparisonTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        /// <summary>
        /// Root with a lit child "Lamp" (Light + FixedJoint) and two bodies; the joint holds
        /// <paramref name="heldBody"/> (0 = BodyA, 1 = BodyB) and the lamp's mesh is <paramref name="mesh"/>.
        /// </summary>
        private GameObject Build(Mesh mesh, float intensity = 1f, int heldBody = 0)
        {
            var root = new GameObject("Root");
            _created.Add(root);
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0.4f, 0.9f);
            lamp.AddComponent<Light>().intensity = intensity;
            lamp.AddComponent<MeshFilter>().sharedMesh = mesh;

            var bodies = new Rigidbody[2];
            for (int i = 0; i < bodies.Length; i++)
            {
                var body = new GameObject(i == 0 ? "BodyA" : "BodyB");
                body.transform.SetParent(root.transform, false);
                bodies[i] = body.AddComponent<Rigidbody>();
            }

            lamp.AddComponent<FixedJoint>().connectedBody = bodies[heldBody];
            return root;
        }

        private Mesh NewMesh()
        {
            var mesh = new Mesh();
            _created.Add(mesh);
            return mesh;
        }

        [Test]
        public void IdenticalBuilds_HaveNoDifference()
        {
            Mesh mesh = NewMesh();
            Assert.IsNull(HierarchyComparison.FirstDifference(Build(mesh), Build(mesh)),
                "Same build, same assets: references into each hierarchy match by place.");
        }

        [Test]
        public void ChangedValue_IsFoundWithItsPlace()
        {
            Mesh mesh = NewMesh();
            string difference = HierarchyComparison.FirstDifference(Build(mesh), Build(mesh, 2f));
            StringAssert.Contains("Root/Lamp", difference);
            StringAssert.Contains("Light.m_Intensity", difference);
        }

        [Test]
        public void ReferenceInsideTheHierarchy_ToAnotherPlace_IsADifference()
        {
            Mesh mesh = NewMesh();
            string difference = HierarchyComparison.FirstDifference(Build(mesh), Build(mesh, heldBody: 1));
            StringAssert.Contains("FixedJoint.m_ConnectedBody", difference);
        }

        [Test]
        public void ReferenceToAnAsset_MustBeTheSameObject()
        {
            string difference = HierarchyComparison.FirstDifference(Build(NewMesh()), Build(NewMesh()));
            StringAssert.Contains("MeshFilter.m_Mesh", difference, "An equal-looking copy is another asset.");
        }

        [Test]
        public void ChangedPoseNameOrActiveState_IsADifference()
        {
            Mesh mesh = NewMesh();
            GameObject saved = Build(mesh);

            GameObject moved = Build(mesh);
            moved.transform.GetChild(0).localPosition += Vector3.up * 0.01f;
            StringAssert.Contains("Transform.m_LocalPosition", HierarchyComparison.FirstDifference(saved, moved));

            GameObject renamed = Build(mesh);
            renamed.transform.GetChild(1).name = "BodyC";
            StringAssert.Contains("GameObject.m_Name", HierarchyComparison.FirstDifference(saved, renamed));

            GameObject hidden = Build(mesh);
            hidden.transform.GetChild(2).gameObject.SetActive(false);
            StringAssert.Contains("GameObject.m_IsActive", HierarchyComparison.FirstDifference(saved, hidden));
        }

        [Test]
        public void ExtraComponentOrChild_IsADifference()
        {
            Mesh mesh = NewMesh();
            GameObject saved = Build(mesh);

            GameObject extraComponent = Build(mesh);
            extraComponent.transform.GetChild(1).gameObject.AddComponent<BoxCollider>();
            StringAssert.Contains("Root/BodyA", HierarchyComparison.FirstDifference(saved, extraComponent));

            GameObject extraChild = Build(mesh);
            new GameObject("Extra").transform.SetParent(extraChild.transform.GetChild(2), false);
            StringAssert.Contains("Root/BodyB", HierarchyComparison.FirstDifference(saved, extraChild));
        }
    }
}
