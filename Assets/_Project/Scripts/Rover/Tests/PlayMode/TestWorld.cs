using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Feel-metric test ground built in code (no assets), far from the world origin so nothing another test left
    /// behind (a crater, a bootstrap scene) can touch it. In local coordinates (see <see cref="Point"/> and
    /// <see cref="Local"/>): a large flat plain at y = 0, a 1 m high, 10 m long cosine crest on a strip at
    /// x = <see cref="BumpX"/>, a 30 degree slope rising toward -x from x = <see cref="SlopeX"/>, and a rock wall
    /// to get stuck against at (<see cref="WallX"/>, <see cref="WallZ"/>).
    /// <see cref="Height"/> is the analytic surface matching the colliders (world coordinates).
    /// </summary>
    public sealed class TestWorld : IDisposable
    {
        public const float BumpX = 60f;
        public const float BumpStartZ = 0f;
        public const float BumpLength = 10f;
        public const float BumpHeight = 1f;
        public const float SlopeX = -60f;
        public const float SlopeAngle = 30f;
        public const float WallX = 120f;
        public const float WallZ = 20f;

        /// <summary>Far beyond the game world's ~2.8 km extent, still precise for physics (sub-millimetre).</summary>
        private const float OriginX = 6000f;
        private const float OriginZ = 6000f;

        private const float BumpHalfWidth = 8f;
        private const float BumpStripStart = -70f;
        private const float BumpStripEnd = 70f;
        private const float BumpStep = 0.2f;
        private const float SlopeRun = 60f;
        private const float SlopeDepth = 300f;
        private const float PlainSize = 1200f;
        private const float PlayableHalfSize = 500f;
        private const float WallWidth = 6f;
        private const float WallHeight = 3f;
        private const float WallThickness = 1f;
        private const float SunPitch = 40f;
        private const float SunYaw = -35f;

        private readonly GameObject _root;
        private readonly Mesh _bumpMesh;

        public TestWorld()
        {
            _root = new GameObject("RoverTestWorld");
            _root.transform.position = new Vector3(OriginX, 0f, OriginZ);
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

            _bumpMesh = BuildBump();
            BuildSlope();
            BuildWall();
        }

        /// <summary>The default material of the plain, reused for every test mesh.</summary>
        public Material Material { get; }

        public ITerrainQuery Terrain { get; } = new TestTerrain(Height, new Rect(OriginX - PlayableHalfSize,
            OriginZ - PlayableHalfSize, 2f * PlayableHalfSize, 2f * PlayableHalfSize));

        /// <summary>The single directional light (no shadows unless a test turns them on).</summary>
        public Light Sun { get; }

        /// <summary>World position of local ground coordinates (x, z) at y = 0.</summary>
        public static Vector3 Point(float x, float z)
        {
            return new Vector3(OriginX + x, 0f, OriginZ + z);
        }

        /// <summary>Local coordinates of a world position.</summary>
        public static Vector3 Local(Vector3 world)
        {
            return new Vector3(world.x - OriginX, world.y, world.z - OriginZ);
        }

        /// <summary>Ground height at world (x, z).</summary>
        public static float Height(float x, float z)
        {
            float localX = x - OriginX;
            float localZ = z - OriginZ;
            if (localX <= SlopeX)
            {
                return (SlopeX - localX) * Mathf.Tan(SlopeAngle * Mathf.Deg2Rad);
            }

            return Mathf.Abs(localX - BumpX) <= BumpHalfWidth ? Bump(localZ) : 0f;
        }

        private static float Bump(float z)
        {
            float t = (z - BumpStartZ) / BumpLength;
            return t <= 0f || t >= 1f ? 0f : BumpHeight * 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * t));
        }

        private Mesh BuildBump()
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
            return mesh;
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
            slope.transform.localPosition = new Vector3(SlopeX, 0f, 0f) - lowTopEdge;
            slope.transform.localRotation = tilt;
        }

        /// <summary>A rock wall (Prop layer) across the way at (<see cref="WallX"/>, <see cref="WallZ"/>).</summary>
        private void BuildWall()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.layer = Layers.Prop;
            wall.transform.SetParent(_root.transform, false);
            wall.transform.localScale = new Vector3(WallWidth, WallHeight, WallThickness);
            wall.transform.localPosition = new Vector3(WallX, WallHeight * 0.5f, WallZ);
        }

        /// <summary>Destroys everything immediately, so the next test never overlaps this one's colliders.</summary>
        public void Dispose()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_bumpMesh);
        }
    }
}
