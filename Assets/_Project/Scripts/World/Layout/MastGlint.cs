using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The radio tower's mast tip catching the earthlight (opening v3 B10), so nightfall leads 07 to the antenna it
    /// needs without a word: one small sparkle at the tip, lit as the view nears Earth's mirror in it
    /// (<see cref="SunGlint"/>, a broad lobe), only by night and as full as Earth is. The scene build stands the
    /// sparkle and bakes the tip's glint normal, aimed so Earth glints toward the base. Allocation-free per frame.
    /// </summary>
    public sealed class MastGlint : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' mast glint.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The four-point sparkle at the mast's tip (LofiGlintStar).")]
        [SerializeField] private Renderer _sparkle;

        [Tooltip("The tip's glint normal in world space (baked by the scene build: it throws Earth at the apron).")]
        [SerializeField] private Vector3 _normal = Vector3.up;

        private GameContext _context;
        private ISunlight _sunlight;
        private IWorldLayout _layout;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _sparkle == null || _normal == Vector3.zero)
            {
                Debug.LogError($"{nameof(MastGlint)}: World Settings, the sparkle and the tip's normal must be " +
                    "assigned (the scene build wires them on the radio tower).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>The glint (0..1, before its twinkle and brightness) on the last frame.</summary>
        public float Level { get; private set; }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            if ((_sunlight == null && !_context.TryGet(out _sunlight))
                || (_layout == null && !_context.TryGet(out _layout)))
            {
                return;
            }

            if (!_context.TryGet(out IViewCamera view) || view.Camera == null)
            {
                return;
            }

            MastGlintSettings glint = _settings.Landmarks.MastGlint;
            Vector3 towardEye = (view.Camera.transform.position - _sparkle.transform.position).normalized;
            Level = _sunlight.EarthPhase * SunGlint.Night(_sunlight.SunDirection, glint.NightFullBelow)
                * SunGlint.Level(_layout.EarthDirection, _normal, towardEye, glint.Sharpness);
            float twinkle = SunGlint.Shimmer(Time.time, glint.TwinkleHertz, glint.TwinkleDepth, 0f);
            _block.Clear();
            _block.SetFloat(IntensityId, Level * twinkle * glint.Intensity);
            _sparkle.SetPropertyBlock(_block);
        }
    }
}
