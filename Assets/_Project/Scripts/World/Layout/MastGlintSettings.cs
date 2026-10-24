using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The radio tower's mast tip catching the earthlight at nightfall (opening v3 B10, <see cref="MastGlint"/>): from
    /// the base only the leaning mast shows over the habitat, so its antenna, the part 07 needs, gives back a small,
    /// pale point of Earth's light against the dark cliff. A calm twinkle, never a beacon.
    /// </summary>
    [Serializable]
    public sealed class MastGlintSettings
    {
        [Tooltip("Name of the radio tower ruin under [World]/Ruins.")]
        [SerializeField] private string _tower = "RadioTower";

        [Tooltip("Art's leaning mast in the tower; the glint stands at its highest point, the antenna at its tip.")]
        [SerializeField] private string _mast = "MastLeaning";

        [Tooltip("Colour of the glint: the earthlight, pale and cool, never neon.")]
        [SerializeField] private Color _color = new Color(0.82f, 0.9f, 1f, 1f);

        [Tooltip("Brightness of the glint on Earth's mirror in the antenna, under a full Earth.")]
        [Range(0f, 8f)]
        [SerializeField] private float _intensity = 1.2f;

        [Tooltip("Sharpness of the glint's lobe: low, as the antenna's rods and dish throw the light far round the base.")]
        [Range(0.5f, 64f)]
        [SerializeField] private float _sharpness = 1.5f;

        [Tooltip("How far below the horizon, degrees, the Sun must be for the full glint: it wakes as the sky darkens " +
            "into the first night, never against the day.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _nightFullBelow = 1f;

        [Tooltip("Slow twinkle of the glint, hertz.")]
        [Range(0f, 3f)]
        [SerializeField] private float _twinkleHertz = 0.35f;

        [Tooltip("How deep the twinkle dips (0 steady, 1 out at each dip).")]
        [Range(0f, 1f)]
        [SerializeField] private float _twinkleDepth = 0.45f;

        [Tooltip("Length of the glint's arms on screen, pixels: small, a point of light, never a lens flare.")]
        [Range(1f, 32f)]
        [SerializeField] private float _armPixels = 6f;

        [Tooltip("Width of the glint's arms on screen, pixels.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _widthPixels = 0.7f;

        [Tooltip("Radius of the glint's core on screen, pixels.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _corePixels = 1.4f;

        [Tooltip("How far the glint stands off the mast's tip toward the light, metres, so the mast never hides it.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lift = 0.1f;

        public string Tower => _tower;
        public string Mast => _mast;
        public Color Color => _color;
        public float Intensity => _intensity;
        public float Sharpness => _sharpness;
        public float NightFullBelow => _nightFullBelow;
        public float TwinkleHertz => _twinkleHertz;
        public float TwinkleDepth => _twinkleDepth;
        public float ArmPixels => _armPixels;
        public float WidthPixels => _widthPixels;
        public float CorePixels => _corePixels;
        public float Lift => _lift;
    }
}
