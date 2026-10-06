using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Feel-metric test ground built in code (no assets): a large flat plain at y = 0, a 1 m high, 10 m long cosine
    /// crest on a strip at x = <see cref="BumpX"/>, and a 30 degree slope rising toward -x from
    /// x = <see cref="SlopeX"/>.
    /// <see cref="Height"/> is the analytic surface matching the colliders.
    /// </summary>
    public sealed class TestWorld : IDisposable
    {
        public const float BumpX = 60f;
        public const float BumpStartZ = 0f;
        public const float BumpLength = 10f;
        public const float BumpHeight = 1f;
        public const float SlopeX = -60f;
        public const float SlopeAngle = 30f;

        private const float BumpHalfWidth = 8f;
        private const float BumpStripStart = -70f;
        private const float BumpStripEnd = 70f;
        private const float BumpStep = 0.2f;
        private const float SlopeRun = 60f;
        private const float SlopeDepth = 300f;
        private const float PlainSize = 1200f;
        private const float SunPitch = 40f;
        private const float SunYaw = -35f;

        private readonly GameObject _root;

        public TestWorld()
        {
            _root = new GameObject("TestWorld");
            GameObject plain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plain.name = "Plain";
            plain.layer = Layers.Ground;
            plain.transform.SetParent(_root.transform, false);
            plain.transform.localScale = new Vector3(PlainSize, 2f, PlainSize);
            plain.transform.localPosition = Vector3.down;
            Material = plain.GetComponent<MeshRenderer>().sharedMaterial;

            Sun = new GameObject("Sun").AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.transform.SetParent(_root.transform, false);
            Sun.transform.rotation = Quaternion.Euler(SunPitch, SunYaw, 0f);

            BuildBump();
            BuildSlope();
        }

        /// <summary>The default material of the plain, reused for every test mesh.</summary>
        public Material Material { get; }

        public ITerrainQuery Terrain { get; } = new TestTerrain(Height);

        /// <summary>The single directional light (no shadows unless a test turns them on).</summary>
        public Light Sun { get; }

        public static float Height(float x, float z)
        {
            if (x <= SlopeX)
            {
                return (SlopeX - x) * Mathf.Tan(SlopeAngle * Mathf.Deg2Rad);
            }

            return Mathf.Abs(x - BumpX) <= BumpHalfWidth ? Bump(z) : 0f;
        }

        private static float Bump(float z)
        {
            float t = (z - BumpStartZ) / BumpLength;
            return t <= 0f || t >= 1f ? 0f : BumpHeight * 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * t));
        }

        private void BuildBump()
        {
            int rows = Mathf.RoundToInt((BumpStripEnd - BumpStripStart) / BumpStep) + 1;
            var vertices = new Vector3[rows * 2];
            var triangles = new int[(rows - 1) * 6];
            for (int r = 0; r < rows; r++)
            {
                float z = BumpStripStart + r * BumpStep;
                float y = Bump(z);
                vertices[r * 2] = new Vector3(BumpX - BumpHalfWidth, y, z);
                vertices[r * 2 + 1] = new Vector3(BumpX + BumpHalfWidth, y, z);
                if (r == 0)
                {
                    continue;
                }

                int t = (r - 1) * 6;
                int a = (r - 1) * 2;
                triangles[t] = a;
                triangles[t + 1] = a + 2;
                triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1;
                triangles[t + 4] = a + 2;
                triangles[t + 5] = a + 3;
            }

            var mesh = new Mesh { name = "BumpStrip", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var strip = new GameObject("BumpStrip") { layer = Layers.Ground };
            strip.transform.SetParent(_root.transform, false);
            strip.AddComponent<MeshFilter>().sharedMesh = mesh;
            strip.AddComponent<MeshRenderer>().sharedMaterial = Material;
            strip.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildSlope()
        {
            GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Slope";
            slope.layer = Layers.Ground;
            slope.transform.SetParent(_root.transform, false);
            slope.transform.localScale = new Vector3(SlopeRun, 1f, SlopeDepth);
            Quaternion tilt = Quaternion.Euler(0f, 0f, -SlopeAngle);
            Vector3 lowTopEdge = tilt * new Vector3(SlopeRun * 0.5f, 0.5f, 0f);
            slope.transform.SetPositionAndRotation(new Vector3(SlopeX, 0f, 0f) - lowTopEdge, tilt);
        }

        public void Dispose()
        {
            Object.Destroy(_root);
        }
    }
}
