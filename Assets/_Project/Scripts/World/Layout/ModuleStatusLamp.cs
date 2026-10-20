using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The Lumen habitat's power module status lamp (opening v3 R4): blinks slow amber from dusk through the night
    /// (<see cref="StatusLampBlink"/>), its lens and a small halo in front of it together. The scene build wires the
    /// lens (Art's PowerModule_StatusLamp node) and stands the halo apart from the ruin, whose renderers are its scan
    /// targets'. It reads each property block before writing, so a scan's own highlight on the lens survives.
    /// Allocation-free per frame.
    /// </summary>
    public sealed class ModuleStatusLamp : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' module lamp.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The lamp's own amber lens (Art's PowerModule_StatusLamp on HabitatModules).")]
        [SerializeField] private Renderer _lens;

        [Tooltip("The lamp's halo in front of the lens (LofiBeaconHalo, amber).")]
        [SerializeField] private Renderer _halo;

        private readonly StatusLampBlink _blink = new StatusLampBlink();
        private GameContext _context;
        private ISunlight _sunlight;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _lens == null || _halo == null)
            {
                Debug.LogError($"{nameof(ModuleStatusLamp)}: World Settings, the lamp's lens and its halo must be " +
                    "assigned (the scene build wires them beside the habitat).", this);
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
            Write(_lens, EmissionStrengthId, Level * lamp.LampGlow);
            Write(_halo, IntensityId, Level * lamp.HaloIntensity);
        }

        private void Write(Renderer target, int property, float value)
        {
            target.GetPropertyBlock(_block);
            _block.SetFloat(property, value);
            target.SetPropertyBlock(_block);
        }
    }
}
