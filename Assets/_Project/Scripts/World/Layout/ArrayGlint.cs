using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The fallen solar array glints in the new sunlight (opening v3 R4), as sunlight off flat glass: each wing's cell
    /// faces catch the light in the panel's own shape (a sheen laid on them) and a small, sharp four-point sparkle
    /// twinkles on top, both as the view nears the Sun's mirror in the cells (<see cref="SunGlint"/>, a broad lobe so it
    /// calls from anywhere round the base) and only while sunlight reaches the wing (<see cref="ISunlight.SunlightAt"/>:
    /// never in the eclipse, never in the shade). The scene build lays a sheen on each wing and a sparkle over its
    /// cells and bakes the cracked-cell facet, aimed so the golden-hour Sun glints toward the base. Allocation-free per
    /// frame.
    /// </summary>
    public sealed class ArrayGlint : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        // A second phase for each wing's sparkle, so it never twinkles in step with its sheen or the other wing.
        private const float SparklePhase = 0.53f;
        private const float WingPhase = 0.37f;

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' array glint.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The sheen on each fallen wing's cells (LofiGlintSheen), in the wing's own shape.")]
        [SerializeField] private Renderer[] _sheens = Array.Empty<Renderer>();

        [Tooltip("The four-point sparkle over each wing's cells (LofiGlintStar).")]
        [SerializeField] private Renderer[] _sparkles = Array.Empty<Renderer>();

        [Tooltip("Each wing's cracked-cell facet normal in world space (baked by the scene build: it throws the " +
            "golden-hour Sun at the apron's eye).")]
        [SerializeField] private Vector3[] _normals = Array.Empty<Vector3>();

        private GameContext _context;
        private ISunlight _sunlight;
        private MaterialPropertyBlock _block;
        private float[] _sunlit = Array.Empty<float>();
        private float[] _levels = Array.Empty<float>();
        private float _nextSample;

        public void Initialize(GameContext context)
        {
            int wings = _sparkles != null ? _sparkles.Length : 0;
            if (_settings == null || wings == 0 || Array.IndexOf(_sparkles, null) >= 0 || _sheens == null
                || _sheens.Length != wings || Array.IndexOf(_sheens, null) >= 0 || _normals == null
                || _normals.Length != wings)
            {
                Debug.LogError($"{nameof(ArrayGlint)}: World Settings, a sheen and a sparkle on each fallen wing and " +
                    "each wing's normal must be assigned (the scene build wires them on the array).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            _sunlit = new float[wings];
            _levels = new float[wings];
        }

        /// <summary>The glint (0..1, before its shimmer and brightness) each wing showed on the last frame.</summary>
        public float LevelOf(int wing) => wing >= 0 && wing < _levels.Length ? _levels[wing] : 0f;

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            if (_sunlight == null && !_context.TryGet(out _sunlight))
            {
                return;
            }

            if (!_context.TryGet(out IViewCamera view) || view.Camera == null)
            {
                return;
            }

            ArrayGlintSettings glint = _settings.Landmarks.ArrayGlint;
            float time = Time.time;
            if (time >= _nextSample)
            {
                _nextSample = time + glint.SunlightSampleSeconds;
                for (int i = 0; i < _sparkles.Length; i++)
                {
                    _sunlit[i] = _sunlight.SunlightAt(_sparkles[i].transform.position);
                }
            }

            Vector3 eye = view.Camera.transform.position;
            Vector3 towardSun = _sunlight.SunDirection;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                Vector3 towardEye = (eye - _sparkles[i].transform.position).normalized;
                float level = _sunlit[i] * SunGlint.Level(towardSun, _normals[i], towardEye, glint.Sharpness);
                _levels[i] = level;
                float sheen = SunGlint.Shimmer(time, glint.ShimmerHertz, glint.ShimmerDepth, i * WingPhase);
                float twinkle = SunGlint.Shimmer(time, glint.SparkleTwinkleHertz, glint.SparkleTwinkleDepth,
                    i * WingPhase + SparklePhase);
                Write(_sheens[i], level * sheen * glint.SheenIntensity);
                Write(_sparkles[i], level * twinkle * glint.SparkleIntensity);
            }
        }

        private void Write(Renderer target, float intensity)
        {
            _block.Clear();
            _block.SetFloat(IntensityId, intensity);
            target.SetPropertyBlock(_block);
        }
    }
}
