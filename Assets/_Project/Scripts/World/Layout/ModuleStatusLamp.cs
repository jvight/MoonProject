using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The Lumen habitat's power module status lamp (opening v3 R4): blinks slow amber from dusk through the night
    /// (<see cref="StatusLampBlink"/>), its lens and a small halo in front of it together. The lamp is on the towable
    /// power module Gameplay stands beside the habitat (<see cref="ITowLine.TryFindLoad"/>, the module's tow id in
    /// tuning), found once (<see cref="StatusLampLens"/>: Art's PowerModule_StatusLamp, its amber slot by name, never the
    /// bezel); the lamp itself, with its halo at its origin, stands apart under [World]/ModuleLamp and follows the lens
    /// each frame as 07 tows it, so its position is the lamp's. No tow line or no such load (the opening did not stand it): no blink, no error. A load with no lamp to
    /// blink is broken wiring, reported once. Allocation-free per frame.
    /// </summary>
    public sealed class ModuleStatusLamp : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        // Art's lamp on the power module and the material its lens wears.
        private const string LampNode = "PowerModule_StatusLamp";
        private const string LensMaterial = "R1_LampAmber";

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' module lamp.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The lamp's halo at this lamp's origin, stood apart from the module (LofiBeaconHalo, amber).")]
        [SerializeField] private Renderer _halo;

        private readonly StatusLampBlink _blink = new StatusLampBlink();
        private GameContext _context;
        private ISunlight _sunlight;
        private StatusLampLens _lens;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _halo == null)
            {
                Debug.LogError($"{nameof(ModuleStatusLamp)}: World Settings and the lamp's halo must be assigned " +
                    "(the scene build wires them).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            _halo.enabled = false;
        }

        /// <summary>The lamp's level (0..1) on the last frame.</summary>
        public float Level { get; private set; }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            ModuleLampSettings lamp = _settings.Landmarks.ModuleLamp;
            if (_lens == null && !TryFindLens(lamp.TowId))
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

            _blink.OnSky(_sunlight.Phase, false);
            _blink.Step(Time.deltaTime, lamp.WakeSeconds);
            Level = _blink.Level(Time.time, lamp.BlinkSeconds, lamp.OnShare, lamp.EaseShare);
            transform.position = _lens.InFront(lamp.HaloLift);
            _lens.Renderer.GetPropertyBlock(_block, _lens.Slot);
            _block.SetFloat(EmissionStrengthId, Level * lamp.LampGlow);
            _lens.Renderer.SetPropertyBlock(_block, _lens.Slot);
            _halo.GetPropertyBlock(_block);
            _block.SetFloat(IntensityId, Level * lamp.HaloIntensity);
            _halo.SetPropertyBlock(_block);
        }

        private bool TryFindLens(string towId)
        {
            if (!_context.TryGet(out ITowLine tow) || !tow.TryFindLoad(towId, out Transform load) || load == null)
            {
                return false;
            }

            _lens = StatusLampLens.Find(load, LampNode, LensMaterial, out string problem);
            if (_lens == null)
            {
                Debug.LogError($"{nameof(ModuleStatusLamp)}: the tow load '{towId}' cannot blink: {problem}", this);
                enabled = false;
                return false;
            }

            _halo.enabled = true;
            return true;
        }
    }
}
