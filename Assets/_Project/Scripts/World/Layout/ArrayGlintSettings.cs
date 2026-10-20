using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The fallen solar array's glint in the new sunlight (opening v3 R4): a sun-white halo on each fallen wing that
    /// flares as the view comes near the Sun's mirror in its cells, only while sunlight reaches the wing (never in the
    /// eclipse, never in the shade), with a slow shimmer so it reads as glass catching light.
    /// </summary>
    [Serializable]
    public sealed class ArrayGlintSettings
    {
        [Tooltip("Colour of the glint: the Sun's own white, a little warm.")]
        [SerializeField] private Color _color = new Color(1f, 0.96f, 0.88f, 1f);

        [Tooltip("Brightness of a wing's halo when the view sits right on the Sun's mirror in its cells.")]
        [Range(0f, 10f)]
        [SerializeField] private float _intensity = 3f;

        [Tooltip("Radius of the glint's halo, metres.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _radius = 0.35f;

        [Tooltip("Smallest angular size of the glint, degrees: it still reads as a point from across the basin.")]
        [Range(0f, 3f)]
        [SerializeField] private float _minAngle = 0.5f;

        [Tooltip("How tightly the glint follows the Sun's mirror: higher is a smaller, sharper catch of light.")]
        [Range(1f, 200f)]
        [SerializeField] private float _sharpness = 12f;

        [Tooltip("Shimmers a second of the glint (the cracked cells catching light unevenly).")]
        [Range(0f, 5f)]
        [SerializeField] private float _shimmerHertz = 1.3f;

        [Tooltip("How much of the glint the shimmer takes away at its lowest (0 none, 1 all).")]
        [Range(0f, 1f)]
        [SerializeField] private float _shimmerDepth = 0.35f;

        [Tooltip("Seconds between looks at how much sunlight reaches the wings (the sky's sunlight test is not free).")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _sunlightSampleSeconds = 0.1f;

        public Color Color => _color;
        public float Intensity => _intensity;
        public float Radius => _radius;
        public float MinAngle => _minAngle;
        public float Sharpness => _sharpness;
        public float ShimmerHertz => _shimmerHertz;
        public float ShimmerDepth => _shimmerDepth;
        public float SunlightSampleSeconds => _sunlightSampleSeconds;
    }
}
