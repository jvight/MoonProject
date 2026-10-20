using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The fallen solar array glints in the new sunlight (opening v3 R4): a sun-white halo on each fallen wing flares
    /// as the view comes near the Sun's mirror in its cells (<see cref="SunGlint"/>), only while sunlight reaches the
    /// wing (<see cref="ISunlight.SunlightAt"/>: never in the eclipse, never in the shade), with a slow shimmer. The
    /// scene build hangs a halo over each wing's cells and bakes its cracked-cell facet, aimed so the golden-hour Sun
    /// glints toward the base. Allocation-free per frame.
    /// </summary>
    public sealed class ArrayGlint : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' array glint.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The glint halo on each fallen wing (LofiBeaconHalo, sun white), over the middle of its cells.")]
        [SerializeField] private Renderer[] _halos = Array.Empty<Renderer>();

        [Tooltip("Each glint's cracked-cell facet normal in world space (baked by the scene build: it throws the " +
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
            if (_settings == null || _halos == null || _halos.Length == 0 || Array.IndexOf(_halos, null) >= 0
                || _normals == null || _normals.Length != _halos.Length)
            {
                Debug.LogError($"{nameof(ArrayGlint)}: World Settings, a halo on each fallen wing and each wing's " +
                    "normal must be assigned (the scene build wires them on the array).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            _sunlit = new float[_halos.Length];
            _levels = new float[_halos.Length];
        }

        /// <summary>The glint (0..1, before its brightness) each wing showed on the last frame.</summary>
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
                for (int i = 0; i < _halos.Length; i++)
                {
                    _sunlit[i] = _sunlight.SunlightAt(_halos[i].transform.position);
                }
            }

            Vector3 eye = view.Camera.transform.position;
            Vector3 towardSun = _sunlight.SunDirection;
            for (int i = 0; i < _halos.Length; i++)
            {
                Vector3 at = _halos[i].transform.position;
                Vector3 towardEye = (eye - at).normalized;
                float level = _sunlit[i] * SunGlint.Level(towardSun, _normals[i], towardEye, glint.Sharpness)
                    * SunGlint.Shimmer(time, glint.ShimmerHertz, glint.ShimmerDepth, i * 0.37f);
                _levels[i] = level;
                _block.Clear();
                _block.SetFloat(IntensityId, level * glint.Intensity);
                _halos[i].SetPropertyBlock(_block);
            }
        }
    }
}
