using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Pulses the landed cargo lander's amber emergency beacon (opening v2 §4): Art's R1 lens (its emission strength),
    /// World's tight halo and warm light, together in a slow <see cref="LocatorBlink"/>, and runs a calm wave of light
    /// along the old cable's marker lamps from 07's dead post toward it (<see cref="CableMarkerWave"/>). The scene
    /// build stands Art's beacon on the lander, hangs the halo and light on its light anchor and wires the markers in
    /// order from the post. Allocation-free per frame.
    /// </summary>
    public sealed class LanderBeacon : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' lander beacon.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Art's R1 beacon lens (LanderBeacon/BeaconLens, R1_LampAmber).")]
        [SerializeField] private Renderer _lens;

        [Tooltip("World's halo on the beacon (LofiBeaconHalo).")]
        [SerializeField] private Renderer _halo;

        [Tooltip("World's warm light on the beacon.")]
        [SerializeField] private Light _light;

        [Tooltip("The old cable's marker lamps (GroundCableSegment/MarkerLamp), in order from 07's post to the lander.")]
        [SerializeField] private Renderer[] _markers = new Renderer[0];

        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _lens == null || _halo == null || _light == null || _markers == null
                || System.Array.IndexOf(_markers, null) >= 0)
            {
                Debug.LogError($"{nameof(LanderBeacon)}: World Settings, the lens, the halo, the light and every " +
                    "cable marker must be assigned (the scene build wires them on the lander).", this);
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

            float time = Time.time;
            LanderBeaconSettings beacon = _settings.Landmarks.LanderBeacon;
            float level = LocatorBlink.Level(time, beacon.Period, beacon.Sharpness, beacon.Floor);
            Glow(_lens, level * beacon.LensGlow);
            _block.Clear();
            _block.SetFloat(IntensityId, level * beacon.HaloIntensity);
            _halo.SetPropertyBlock(_block);
            _light.intensity = level * beacon.LightIntensity;
            for (int i = 0; i < _markers.Length; i++)
            {
                float lift = CableMarkerWave.Lift(time, i, _markers.Length, beacon.MarkerRunSeconds,
                    beacon.MarkerRepeatSeconds, beacon.MarkerWaveWidth);
                Glow(_markers[i], beacon.MarkerGlow + lift * beacon.MarkerPulse);
            }
        }

        private void Glow(Renderer target, float strength)
        {
            _block.Clear();
            _block.SetFloat(EmissionStrengthId, strength);
            target.SetPropertyBlock(_block);
        }
    }
}
