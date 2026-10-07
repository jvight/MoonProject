using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Breathes the light on The Peak. The lamp follows the palette glow contract (docs/ARCHITECTURE.md, "Glow
    /// modulation": a MaterialPropertyBlock <c>_EmissionColor</c> set as a linear vector (i, i, i, 1), never as a
    /// gamma-encoded colour, on the shared material); the halo is a camera-facing glow
    /// (LofiBeaconHalo) that never shrinks below a minimum apparent size, so it reads from the base at night.
    /// <see cref="WorldSystem"/> places and configures it; it sits still at full glow while editing.
    /// </summary>
    public sealed class PeakBeacon : MonoBehaviour
    {
        /// <summary>Scene node name; art's M4 broken satellite dish replaces the model under this node.</summary>
        public const string NodeName = "PeakBeacon";

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int MinAngleTanId = Shader.PropertyToID("_MinAngleTan");

        [Tooltip("The lamp renderer (palette AlertSoft face on the shared low-poly material).")]
        [SerializeField] private Renderer _lamp;

        [Tooltip("The halo renderer (LofiBeaconHalo material on a unit quad).")]
        [SerializeField] private Renderer _halo;

        private BeaconSettings _settings;
        private MaterialPropertyBlock _lampBlock;
        private MaterialPropertyBlock _haloBlock;

        /// <summary>Takes the tuning and shows the beacon at full glow until play mode animates it.</summary>
        public void Configure(BeaconSettings settings)
        {
            if (_lamp == null || _halo == null)
            {
                Debug.LogError($"{nameof(PeakBeacon)}: Lamp and Halo renderers must be assigned.", this);
                enabled = false;
                return;
            }

            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _lampBlock = _lampBlock ?? new MaterialPropertyBlock();
            _haloBlock = _haloBlock ?? new MaterialPropertyBlock();
            Apply(1f);
        }

        private void Update()
        {
            if (_settings != null)
            {
                Apply(BeaconPulse.Evaluate(Time.time, _settings.Period, _settings.Sharpness));
            }
        }

        private void Apply(float pulse)
        {
            float lamp = Mathf.Lerp(_settings.LampMinIntensity, _settings.LampMaxIntensity, pulse);
            _lampBlock.SetVector(EmissionColorId, new Vector4(lamp, lamp, lamp, 1f));
            _lamp.SetPropertyBlock(_lampBlock);

            _haloBlock.SetColor(ColorId, _settings.HaloColor);
            _haloBlock.SetFloat(IntensityId, Mathf.Lerp(_settings.HaloMinIntensity, _settings.HaloMaxIntensity, pulse));
            _haloBlock.SetFloat(RadiusId, _settings.HaloRadius);
            _haloBlock.SetFloat(MinAngleTanId, Mathf.Tan(_settings.HaloMinAngle * Mathf.Deg2Rad));
            _halo.SetPropertyBlock(_haloBlock);
        }
    }
}
