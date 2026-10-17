using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// The landed cargo lander's amber emergency beacon (opening v2 §4): a slow, warm locator pulse on top of the hull
    /// that never goes fully dark, so the lander reads from 07's wake point 165 m away, in front of The Peak.
    /// </summary>
    [Serializable]
    public sealed class LanderBeaconSettings
    {
        [Tooltip("How far above the top of the lander's hull the beacon's light sits, metres.")]
        [Range(0f, 2f)]
        [SerializeField] private float _height = 0.3f;

        [Tooltip("Colour of the beacon: an amber emergency light.")]
        [SerializeField] private Color _color = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Seconds of one pulse: slow, a calm signal.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _period = 3.2f;

        [Tooltip("How short the peak is within its period (a power of the wave): higher is a crisper flash.")]
        [Range(1f, 12f)]
        [SerializeField] private float _sharpness = 3f;

        [Tooltip("The beacon's level between flashes (0..1): it never goes fully dark, so the lander still reads.")]
        [Range(0f, 1f)]
        [SerializeField] private float _floor = 0.3f;

        [Tooltip("Brightness of the beacon's halo at a flash's peak.")]
        [Range(0f, 4f)]
        [SerializeField] private float _haloIntensity = 0.9f;

        [Tooltip("Radius of the halo up close, metres: tight on the lamp; it grows to its minimum apparent size only " +
            "from far off.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _haloRadius = 0.25f;

        [Tooltip("Smallest apparent radius of the halo, degrees, so it reads from the wake point.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.35f;

        [Tooltip("Intensity of the warm light the beacon casts on the hull and the dust at a flash's peak.")]
        [Range(0f, 10f)]
        [SerializeField] private float _lightIntensity = 1.6f;

        [Tooltip("Range of the beacon's light, metres.")]
        [Range(2f, 40f)]
        [SerializeField] private float _lightRange = 14f;

        public float Height => _height;
        public Color Color => _color;
        public float Period => _period;
        public float Sharpness => _sharpness;
        public float Floor => _floor;
        public float HaloIntensity => _haloIntensity;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
        public float LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
    }
}
