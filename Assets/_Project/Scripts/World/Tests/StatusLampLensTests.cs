using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Finding Art's status lamp on a load (<see cref="StatusLampLens"/>): the lens's slot by its material's name, in
    /// any slot order, its middle and the way it faces in the node's space, followed as the load moves; a load with no
    /// lamp, or no amber lens, says why.
    /// </summary>
    public sealed class StatusLampLensTests
    {
        private const string Node = "PowerModule_StatusLamp";
        private const string Lens = "R1_LampAmber";
        private const string Bezel = "R1_Brass";
        private const string AnyShader = "Assets/_Project/Shaders/World/LofiBeaconHalo.shader";

        private GameObject _load;
        private Mesh _mesh;
        private Material[] _slots;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_load);
            Object.DestroyImmediate(_mesh);
            foreach (Material slot in _slots)
            {
                Object.DestroyImmediate(slot);
            }
        }

        [Test]
        public void TheLens_IsFoundByItsMaterial_InAnySlotOrder()
        {
            Build(new[] { Bezel, Lens });
            StatusLampLens lens = StatusLampLens.Find(_load.transform, Node, Lens, out string problem);
            Assert.IsNull(problem);
            Assert.AreEqual(1, lens.Slot, "the lens, not the bezel in slot 0");
            Assert.AreEqual(new Vector3(0f, 0f, -1f), lens.Facing);
        }

        [Test]
        public void TheLens_IsFollowedAsTheLoadMoves()
        {
            Build(new[] { Lens, Bezel });
            StatusLampLens lens = StatusLampLens.Find(_load.transform, Node, Lens, out _);
            _load.transform.SetPositionAndRotation(new Vector3(10f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f));
            Vector3 front = lens.InFront(0.5f);
            Assert.AreEqual(10f - 1.5f, front.x, 1e-4f, "turned with the load, half a metre out in front");
            Assert.AreEqual(5f, front.z, 1e-4f);
        }

        [Test]
        public void ALoadWithNoAmberLens_SaysWhy()
        {
            Build(new[] { Bezel, Bezel });
            Assert.IsNull(StatusLampLens.Find(_load.transform, Node, Lens, out string problem));
            StringAssert.Contains(Lens, problem);
            Assert.IsNull(StatusLampLens.Find(_load.transform, "NoSuchLamp", Lens, out problem));
            StringAssert.Contains("NoSuchLamp", problem);
        }

        // A load with its lamp one level down: two submeshes, the second a small lens a metre in front (-Z).
        private void Build(string[] materials)
        {
            _load = new GameObject("PowerModule");
            var body = new GameObject("Body");
            body.transform.SetParent(_load.transform, false);
            var lamp = new GameObject(Node);
            lamp.transform.SetParent(body.transform, false);
            var mesh = _mesh = new Mesh();
            mesh.SetVertices(new[]
            {
                new Vector3(-0.1f, -0.1f, 0f), new Vector3(0.1f, -0.1f, 0f), new Vector3(0f, 0.1f, 0f),
                new Vector3(-0.1f, -0.1f, -1f), new Vector3(0.1f, -0.1f, -1f), new Vector3(-0.1f, 0.1f, -1f),
                new Vector3(0.1f, 0.1f, -1f),
            });
            mesh.subMeshCount = 2;
            bool lensFirst = materials[0] == Lens;
            int[] bezel = { 0, 1, 2 };
            int[] lens = { 3, 4, 5, 4, 6, 5 };
            mesh.SetTriangles(lensFirst ? lens : bezel, 0);
            mesh.SetTriangles(lensFirst ? bezel : lens, 1);
            mesh.RecalculateBounds();
            lamp.AddComponent<MeshFilter>().sharedMesh = mesh;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AnyShader);
            var slots = _slots = new Material[materials.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new Material(shader) { name = materials[i] };
            }

            lamp.AddComponent<MeshRenderer>().sharedMaterials = slots;
        }
    }
}
