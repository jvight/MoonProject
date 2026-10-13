using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Drives the fallen supply pod's locator beacon on the canyon ledge (0.5.5): Art's cracked beacon glass, World's
    /// halo and warm light hung under it, all pulsing together in a slow <see cref="LocatorBlink"/>. The scene build
    /// wires it to the pod Gameplay stands under its root; the pod carries the halo and light wherever it is placed.
    /// </summary>
    public sealed class PodBeacon : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its canyon's beacon fields.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The pod's cracked beacon glass (Art's SupplyPod/Glow, on M_LowPolyGlowOff).")]
        [SerializeField] private Renderer _glass;

        [Tooltip("World's halo on the beacon (LofiBeaconHalo).")]
        [SerializeField] private Renderer _halo;

        [Tooltip("World's warm light on the beacon.")]
        [SerializeField] private Light _light;

        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _glass == null || _halo == null || _light == null)
            {
                Debug.LogError($"{nameof(PodBeacon)}: World Settings, the pod's glass, the halo and the light must all " +
                    "be assigned (the scene build wires them to Gameplay's SupplyPod).", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            CanyonSettings canyon = _settings.Surface.Canyon;
            float level = LocatorBlink.Level(Time.time, canyon.BeaconPeriod, canyon.BeaconSharpness,
                canyon.BeaconFloor);
            float glass = level * canyon.BeaconGlassGlow;
            _block.Clear();
            _block.SetVector(EmissionColorId, new Vector4(glass, glass, glass, 1f));
            _glass.SetPropertyBlock(_block);
            _block.Clear();
            _block.SetFloat(IntensityId, level * canyon.GlowIntensity);
            _halo.SetPropertyBlock(_block);
            _light.intensity = level * canyon.GlowLightIntensity;
        }
    }
}
