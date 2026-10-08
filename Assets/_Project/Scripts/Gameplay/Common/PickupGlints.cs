using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The glints that make pickups read from far away: one camera-facing star per resting pickup, all in a single
    /// dynamic mesh drawn with one material (one draw call, any camera). Each glint keeps a minimum on-screen size,
    /// twinkles on its own phase, and fades out close to the camera (where the model itself reads) and at the far end
    /// of its range. Buffers are allocated once for every pickup; a frame of glints allocates nothing.
    /// </summary>
    public sealed class PickupGlints : IDisposable
    {
        /// <summary>The far fade starts at this fraction of the maximum distance.</summary>
        private const float FarFadeStart = 0.8f;

        private const float WorldBoundsSize = 4000f;
        private const int VerticesPerGlint = 4;
        private const int IndicesPerGlint = 6;

        private const MeshUpdateFlags Quiet = MeshUpdateFlags.DontRecalculateBounds |
                                              MeshUpdateFlags.DontValidateIndices;

        private static readonly Vector2[] Corners =
        {
            new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f),
        };

        private readonly GlintTuning _tuning;
        private readonly Mesh _mesh;
        private readonly Vector3[] _centres;
        private readonly Vector2[] _sizeAndBrightness;
        private readonly int _capacity;
        private Vector3 _camera;
        private float _minHalfSizePerMetre;
        private float _twinklePhase;
        private int _count;

        public PickupGlints(Transform parent, Material material, GlintTuning tuning, int capacity, int layer)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            _capacity = Mathf.Max(1, capacity);
            int vertexCount = _capacity * VerticesPerGlint;
            _centres = new Vector3[vertexCount];
            _sizeAndBrightness = new Vector2[vertexCount];
            var corners = new Vector2[vertexCount];
            var indices = new int[_capacity * IndicesPerGlint];
            for (int i = 0; i < _capacity; i++)
            {
                int v = i * VerticesPerGlint;
                for (int c = 0; c < VerticesPerGlint; c++)
                {
                    corners[v + c] = Corners[c];
                }

                int t = i * IndicesPerGlint;
                indices[t] = v;
                indices[t + 1] = v + 2;
                indices[t + 2] = v + 1;
                indices[t + 3] = v + 1;
                indices[t + 4] = v + 2;
                indices[t + 5] = v + 3;
            }

            _mesh = new Mesh
            {
                name = "PickupGlints",
                indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_centres);
            _mesh.SetUVs(0, corners);
            _mesh.SetUVs(1, _sizeAndBrightness);
            _mesh.SetTriangles(indices, 0, false);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * WorldBoundsSize);
            _mesh.SetSubMesh(0, Range(0), Quiet);

            var host = new GameObject("PickupGlints") { layer = layer };
            host.transform.SetParent(parent, false);
            host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            host.AddComponent<MeshFilter>().sharedMesh = _mesh;
            Renderer = host.AddComponent<MeshRenderer>();
            GlowObject.Configure(Renderer, material);
        }

        public MeshRenderer Renderer { get; }

        /// <summary>Glints drawn this frame (tests and debugging views).</summary>
        public int Drawn { get; private set; }

        /// <summary>Brightest glint this frame.</summary>
        public float Brightest { get; private set; }

        public void Begin(Vector3 cameraPosition, float time)
        {
            _camera = cameraPosition;
            _minHalfSizePerMetre = Mathf.Tan(_tuning.GlintMinAngle * Mathf.Deg2Rad);
            _twinklePhase = 2f * Mathf.PI * _tuning.GlintTwinkleRate * time;
            _count = 0;
            Brightest = 0f;
        }

        /// <summary>Adds the glint of a resting pickup at <paramref name="piece"/> with its own twinkle phase.</summary>
        public void Add(Vector3 piece, float phase)
        {
            if (_count == _capacity)
            {
                return;
            }

            Vector3 position = piece + Vector3.up * _tuning.GlintLift;
            Vector3 toCamera = _camera - position;
            float distance = toCamera.magnitude;
            if (distance > 1e-3f)
            {
                position += toCamera * (Mathf.Min(_tuning.GlintPull, distance * 0.5f) / distance);
            }

            float maxDistance = _tuning.GlintMaxDistance;
            float fade = Ease.Step(_tuning.GlintFadeNear, _tuning.GlintFadeFar, distance) *
                         (1f - Ease.Step(maxDistance * FarFadeStart, maxDistance, distance));
            if (fade <= 0f)
            {
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(_twinklePhase + phase);
            float depth = _tuning.GlintTwinkleDepth;
            float brightness = _tuning.GlintIntensity * fade * (1f - depth + depth * wave * wave * wave);
            float halfSize = Mathf.Max(_tuning.GlintSize, distance * _minHalfSizePerMetre);
            var sizeAndBrightness = new Vector2(halfSize, brightness);
            int v = _count * VerticesPerGlint;
            for (int c = 0; c < VerticesPerGlint; c++)
            {
                _centres[v + c] = position;
                _sizeAndBrightness[v + c] = sizeAndBrightness;
            }

            Brightest = Mathf.Max(Brightest, brightness);
            _count++;
        }

        /// <summary>Uploads this frame's glints.</summary>
        public void End()
        {
            Drawn = _count;
            if (_count > 0)
            {
                // The whole buffer is uploaded (a sub-range would shrink the vertex count below what the index
                // buffer references); only the first _count glints are drawn.
                _mesh.SetVertices(_centres, 0, _centres.Length, Quiet);
                _mesh.SetUVs(1, _sizeAndBrightness, 0, _sizeAndBrightness.Length, Quiet);
            }

            _mesh.SetSubMesh(0, Range(_count), Quiet);
            Renderer.enabled = _count > 0;
        }

        private SubMeshDescriptor Range(int glints)
        {
            return new SubMeshDescriptor(0, glints * IndicesPerGlint)
            {
                firstVertex = 0,
                vertexCount = glints * VerticesPerGlint,
                bounds = _mesh.bounds,
            };
        }

        public void Dispose()
        {
            if (_mesh != null)
            {
                Object.Destroy(_mesh);
            }
        }
    }
}
