using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The fallen solar array's glint in the new sunlight (opening v3 R4): sunlight off flat glass, never a glowing
    /// ball. Each wing's own cell faces catch the light in the panel's shape (a sheen laid on them), with a small,
    /// sharp four-point sparkle on top that twinkles as the angle changes; both only while sunlight reaches the wing
    /// (never in the eclipse, never in the shade), strongest as the view nears the Sun's mirror in the cells.
    /// </summary>
    [Serializable]
    public sealed class ArrayGlintSettings
    {
        [Tooltip("Colour of the glint: the Sun's own white, a little warm.")]
        [SerializeField] private Color _color = new Color(1f, 0.96f, 0.88f, 1f);

        [Tooltip("How tightly the glint follows the Sun's mirror: higher is a smaller, sharper catch of light. Broad " +
            "enough that it calls from anywhere round the base, not only from the apron's centre.")]
        [Range(1f, 200f)]
        [SerializeField] private float _sharpness = 4f;

        [Tooltip("Share of the Sun's disc that has to clear Earth (or the shade) for the glint to catch at full " +
            "strength: a glint off glass is as bright as the Sun's surface, not its size, so the first sliver of the " +
            "limb already throws it, seconds after the breakout.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _fullCatchAtSunlight = 0.08f;

        [Header("The panel's sheen")]
        [Tooltip("Brightness the wing's cell faces add on the Sun's mirror: the panel itself lit, in its own shape.")]
        [Range(0f, 4f)]
        [SerializeField] private float _sheenIntensity = 0.8f;

        [Tooltip("How far the sheen floats off the cells, metres, so the panel never cuts it.")]
        [Range(0.001f, 0.1f)]
        [SerializeField] private float _sheenLift = 0.015f;

        [Header("The sparkle on top")]
        [Tooltip("Brightness of the four-point sparkle at the top of its twinkle on the Sun's mirror.")]
        [Range(0f, 4f)]
        [SerializeField] private float _sparkleIntensity = 1.4f;

        [Tooltip("Length of each of the sparkle's four arms, screen pixels: short and crisp.")]
        [Range(2f, 24f)]
        [SerializeField] private float _sparkleArmPixels = 8f;

        [Tooltip("Thickness of the sparkle's arms, screen pixels.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _sparkleWidthPixels = 0.7f;

        [Tooltip("Radius of the sparkle's bright centre, screen pixels.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _sparkleCorePixels = 1.3f;

        [Tooltip("Twinkles a second of the sparkle (glass catching the light unevenly as the angle changes).")]
        [Range(0f, 8f)]
        [SerializeField] private float _sparkleTwinkleHertz = 2.7f;

        [Tooltip("How much of the sparkle its twinkle takes away at its lowest (0 none, 1 all).")]
        [Range(0f, 1f)]
        [SerializeField] private float _sparkleTwinkleDepth = 0.6f;

        [Header("Shimmer and sampling")]
        [Tooltip("Shimmers a second of the panel's sheen (the cracked cells catching light unevenly).")]
        [Range(0f, 5f)]
        [SerializeField] private float _shimmerHertz = 1.3f;

        [Tooltip("How much of the sheen the shimmer takes away at its lowest (0 none, 1 all).")]
        [Range(0f, 1f)]
        [SerializeField] private float _shimmerDepth = 0.35f;

        [Tooltip("Seconds between looks at how much sunlight reaches the wings (the sky's sunlight test is not free).")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _sunlightSampleSeconds = 0.1f;

        public Color Color => _color;
        public float Sharpness => _sharpness;
        public float FullCatchAtSunlight => _fullCatchAtSunlight;
        public float SheenIntensity => _sheenIntensity;
        public float SheenLift => _sheenLift;
        public float SparkleIntensity => _sparkleIntensity;
        public float SparkleArmPixels => _sparkleArmPixels;
        public float SparkleWidthPixels => _sparkleWidthPixels;
        public float SparkleCorePixels => _sparkleCorePixels;
        public float SparkleTwinkleHertz => _sparkleTwinkleHertz;
        public float SparkleTwinkleDepth => _sparkleTwinkleDepth;
        public float ShimmerHertz => _shimmerHertz;
        public float ShimmerDepth => _shimmerDepth;
        public float SunlightSampleSeconds => _sunlightSampleSeconds;
    }
}
