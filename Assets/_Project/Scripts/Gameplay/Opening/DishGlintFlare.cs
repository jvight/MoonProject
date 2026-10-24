using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The catch of sunlight on the Peak's dish when the answer reaches it: a small, sharp four-point star (the Glint
    /// shader's camera-facing star) that flares in a few frames and fades fast, never a ball. Its size is an angle, so
    /// it stays a few pixels across at any distance; its brightness is capped under bloom. All glints share one
    /// dynamic mesh built up front; a frame allocates nothing.
    /// </summary>
    public sealed class DishGlintFlare : IDisposable
    {
        private const int VerticesPerGlint = 4;
        private const int IndicesPerGlint = 6;
        private const float WorldBoundsSize = 4000f;
        private const MeshUpdateFlags Quiet = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Vector2[] Corners =
        {
            new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f),
        };

        private readonly CallAnswerSettings _settings;
        private readonly Mesh _mesh;
        private readonly MeshRenderer _renderer;
        private readonly Vector3[] _centres;
        private readonly Vector2[] _sizeAndBrightness;
        private readonly Vector3[] _places;
        private readonly float[] _ages;
        private readonly float[] _strengths;
        private int _next;

        public DishGlintFlare(Transform parent, Material material, CallAnswerSettings settings, int capacity)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The flare needs at least one glint.");
            }

            int vertexCount = capacity * VerticesPerGlint;
            _centres = new Vector3[vertexCount];
            _sizeAndBrightness = new Vector2[vertexCount];
            _places = new Vector3[capacity];
            _ages = new float[capacity];
            _strengths = new float[capacity];
            var corners = new Vector2[vertexCount];
            var indices = new int[capacity * IndicesPerGlint];
            for (int i = 0; i < capacity; i++)
            {
                _ages[i] = float.MaxValue;
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

            _mesh = new Mesh { name = "DishGlintFlare" };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_centres);
            _mesh.SetUVs(0, corners);
            _mesh.SetUVs(1, _sizeAndBrightness);
            _mesh.SetTriangles(indices, 0, false);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * WorldBoundsSize);

            var host = new GameObject("DishGlintFlare");
            host.transform.SetParent(parent, false);
            host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            host.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = host.AddComponent<MeshRenderer>();
            GlowObject.Configure(_renderer, material);
            var look = new MaterialPropertyBlock();
            look.SetColor(ColorId, settings.GlintColor);
            _renderer.SetPropertyBlock(look);
            _renderer.enabled = false;
        }

        /// <summary>Glints alive now (tests).</summary>
        public int Alive { get; private set; }

        /// <summary>The brightest glint this frame (tests).</summary>
        public float Brightest { get; private set; }

        /// <summary>
        /// 0..1 share of a glint's brightness <paramref name="age"/> seconds in: a quick rise over
        /// <paramref name="riseShare"/> of its life, then a fast fade to nothing.
        /// </summary>
        public static float Curve(float age, float seconds, float riseShare)
        {
            if (seconds <= 0f || age <= 0f || age >= seconds)
            {
                return 0f;
            }

            float t = age / seconds;
            float rise = Mathf.Clamp(riseShare, 0.01f, 0.99f);
            if (t < rise)
            {
                return Ease.OutCubic(t / rise);
            }

            float fall = 1f - (t - rise) / (1f - rise);
            return fall * fall;
        }

        /// <param name="strength">Share (0..1) of the flare's capped brightness.</param>
        public void Spawn(Vector3 position, float strength)
        {
            _places[_next] = position;
            _strengths[_next] = Mathf.Clamp01(strength);
            _ages[_next] = 0f;
            _next = (_next + 1) % _places.Length;
        }

        public void Step(float deltaTime, Vector3 cameraPosition)
        {
            float seconds = _settings.GlintSeconds;
            float tanHalf = Mathf.Tan(_settings.GlintHalfAngle * Mathf.Deg2Rad);
            Alive = 0;
            Brightest = 0f;
            for (int i = 0; i < _places.Length; i++)
            {
                int v = i * VerticesPerGlint;
                _ages[i] = Mathf.Min(_ages[i] + deltaTime, seconds);
                float level = Curve(_ages[i], seconds, _settings.GlintRiseShare) * _strengths[i] *
                              _settings.GlintIntensity;
                var sizeAndBrightness = new Vector2(Vector3.Distance(cameraPosition, _places[i]) * tanHalf, level);
                for (int c = 0; c < VerticesPerGlint; c++)
                {
                    _centres[v + c] = _places[i];
                    _sizeAndBrightness[v + c] = sizeAndBrightness;
                }

                if (level > GlowRenderer.VisibleThreshold)
                {
                    Alive++;
                    Brightest = Mathf.Max(Brightest, level);
                }
            }

            _mesh.SetVertices(_centres, 0, _centres.Length, Quiet);
            _mesh.SetUVs(1, _sizeAndBrightness, 0, _sizeAndBrightness.Length, Quiet);
            _renderer.enabled = Alive > 0;
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
