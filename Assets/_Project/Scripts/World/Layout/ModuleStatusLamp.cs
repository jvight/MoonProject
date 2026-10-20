using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The Lumen habitat's power module status lamp (opening v3 R4): blinks slow amber from dusk through the night
    /// (<see cref="StatusLampBlink"/>), its lens and a small halo in front of it together. The scene build wires the
    /// lens (Art's PowerModule_StatusLamp node) and stands the halo apart from the ruin, whose renderers are its scan
    /// targets'. It lights only the lens's own material slot (found by name, so never the bezel).
    /// Allocation-free per frame.
    /// </summary>
    public sealed class ModuleStatusLamp : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' module lamp.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The lamp's node (Art's PowerModule_StatusLamp on HabitatModules): its amber lens and its bezel.")]
        [SerializeField] private Renderer _lens;

        [Tooltip("The material slot on the lamp's node that wears the amber lens (the scene build finds it by name).")]
        [Min(0)]
        [SerializeField] private int _lensSlot;

        [Tooltip("The lamp's halo in front of the lens (LofiBeaconHalo, amber).")]
        [SerializeField] private Renderer _halo;

        private readonly StatusLampBlink _blink = new StatusLampBlink();
        private GameContext _context;
        private ISunlight _sunlight;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _lens == null || _halo == null || _lensSlot >= _lens.sharedMaterials.Length)
            {
                Debug.LogError($"{nameof(ModuleStatusLamp)}: World Settings, the lamp's lens (and its slot) and its " +
                    "halo must be assigned (the scene build wires them beside the habitat).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>The lamp's level (0..1) on the last frame.</summary>
        public float Level { get; private set; }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            if (_sunlight == null)
            {
                if (!_context.TryGet(out _sunlight))
                {
                    return;
                }

                _blink.OnSky(_sunlight.Phase, true);
            }

            ModuleLampSettings lamp = _settings.Landmarks.ModuleLamp;
            _blink.OnSky(_sunlight.Phase, false);
            _blink.Step(Time.deltaTime, lamp.WakeSeconds);
            Level = _blink.Level(Time.time, lamp.BlinkSeconds, lamp.OnShare, lamp.EaseShare);
            _lens.GetPropertyBlock(_block, _lensSlot);
            _block.SetFloat(EmissionStrengthId, Level * lamp.LampGlow);
            _lens.SetPropertyBlock(_block, _lensSlot);
            _halo.GetPropertyBlock(_block);
            _block.SetFloat(IntensityId, Level * lamp.HaloIntensity);
            _halo.SetPropertyBlock(_block);
        }
    }
}
