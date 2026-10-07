using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>The slow, soft red light on The Peak: a promise on the horizon, never an alarm.</summary>
    [Serializable]
    public sealed class BeaconSettings
    {
        [Tooltip("Seconds per breath of the beacon.")]
        [Range(1f, 10f)]
        [SerializeField] private float _period = 3.2f;

        [Tooltip("Shape of the breath: 1 = plain ease in/out, higher = rests dim longer and swells more softly.")]
        [Range(1f, 4f)]
        [SerializeField] private float _sharpness = 1.6f;

        [Tooltip("Lamp glow at the dimmest point: a linear multiplier of the palette emission (1 = authored).")]
        [Range(0f, 4f)]
        [SerializeField] private float _lampMinIntensity = 0.1f;

        [Tooltip("Lamp glow at the brightest point (linear multiplier). It blooms softly near the top.")]
        [Range(0f, 8f)]
        [SerializeField] private float _lampMaxIntensity = 4.1f;

        [Tooltip("Colour of the halo around the lamp.")]
        [SerializeField] private Color _haloColor = Palette.Get(PaletteSwatch.AlertSoft);

        [Tooltip("Halo brightness at the dimmest point.")]
        [Range(0f, 4f)]
        [SerializeField] private float _haloMinIntensity = 0.04f;

        [Tooltip("Halo brightness at the brightest point.")]
        [Range(0f, 8f)]
        [SerializeField] private float _haloMaxIntensity = 1.8f;

        [Tooltip("Halo radius up close, metres.")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _haloRadius = 2.2f;

        [Tooltip("Smallest apparent radius of the halo, degrees, so the beacon still reads from the base.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.55f;

        public float Period => _period;
        public float Sharpness => _sharpness;
        public float LampMinIntensity => _lampMinIntensity;
        public float LampMaxIntensity => _lampMaxIntensity;
        public Color HaloColor => _haloColor;
        public float HaloMinIntensity => _haloMinIntensity;
        public float HaloMaxIntensity => _haloMaxIntensity;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
    }
}
