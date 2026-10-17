using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Pulses the landed cargo lander's amber emergency beacon (opening v2 §4): World's halo and warm light on top of
    /// the hull, together in a slow <see cref="LocatorBlink"/>. The scene build hangs both on the lander and wires them.
    /// </summary>
    public sealed class LanderBeacon : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' lander beacon.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("World's halo on the beacon (LofiBeaconHalo).")]
        [SerializeField] private Renderer _halo;

        [Tooltip("World's warm light on the beacon.")]
        [SerializeField] private Light _light;

        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _halo == null || _light == null)
            {
                Debug.LogError($"{nameof(LanderBeacon)}: World Settings, the halo and the light must all be assigned " +
                    "(the scene build hangs them on the lander).", this);
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

            LanderBeaconSettings beacon = _settings.Landmarks.LanderBeacon;
            float level = LocatorBlink.Level(Time.time, beacon.Period, beacon.Sharpness, beacon.Floor);
            _block.Clear();
            _block.SetFloat(IntensityId, level * beacon.HaloIntensity);
            _halo.SetPropertyBlock(_block);
            _light.intensity = level * beacon.LightIntensity;
        }
    }
}
