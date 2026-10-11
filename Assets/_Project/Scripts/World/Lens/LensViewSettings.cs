using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How the picture through 07's own eye looks (docs/design/cutscenes.md §1): the curved soft lid, the lens' shallow
    /// focus, the old electronics' dips and roll band, the warm lift and the dust speck on the glass. Rover drives how
    /// much of each through <see cref="MoonProject.Core.ILensView"/>; these say what "all of it" looks like.
    /// </summary>
    [Serializable]
    public sealed class LensViewSettings
    {
        /// <summary>Smallest gap (m) between the sharp and the blurred speck distances, so the ramp never divides by 0.</summary>
        private const float MinFocusSpan = 0.01f;

        [Tooltip("Width of the lid's soft edge, in viewport heights. Wide and soft: the lid is far too near the " +
            "lens to ever be sharp.")]
        [Range(0.01f, 0.4f)]
        [SerializeField] private float _lidSoftness = 0.14f;

        [Tooltip("How much higher the lid's edge sits in the middle of the frame than at its sides, in viewport " +
            "heights: the brow-shutter's arc seen from inside.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _lidCurve = 0.12f;

        [Tooltip("Deepest exposure dip of the old electronics (Flicker01 = 1), as a fraction of the light. " +
            "Photosensitivity: never above 30%.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _maxDip = 0.3f;

        [Tooltip("Height of the horizontal roll band, in viewport heights.")]
        [Range(0.01f, 0.4f)]
        [SerializeField] private float _rollBandHeight = 0.1f;

        [Tooltip("How dark the roll band is, as a share of the current dip.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rollBandDarkness = 0.6f;

        [Tooltip("Viewport heights the roll band travels down the frame per second of full flicker; it moves only " +
            "while the picture dips, so it passes once instead of strobing.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _rollBandSpeed = 1.1f;

        [Tooltip("Colour of the brief warm lift (Warmth01 = 1).")]
        [SerializeField] private Color _warmTint = new Color(1f, 0.62f, 0.32f, 1f);

        [Tooltip("How much warm light the lift adds at its peak (premultiplied, 0..1).")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _warmLift = 0.1f;

        [Tooltip("Radius of the dust speck when sharp, in viewport heights.")]
        [Range(0.005f, 0.1f)]
        [SerializeField] private float _speckRadius = 0.028f;

        [Tooltip("How much light the sharp speck's dark centre takes away.")]
        [Range(0f, 1f)]
        [SerializeField] private float _speckDarkness = 0.45f;

        [Tooltip("Brightness of the speck's pale rim when sharp.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _speckRim = 0.12f;

        [Tooltip("Colour of the speck's pale rim.")]
        [SerializeField] private Color _speckRimColor = new Color(0.93f, 0.9f, 1f, 1f);

        [Tooltip("The speck is fully sharp while the lens focuses at or nearer than this (m): it sits on the glass.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _speckSharpDistance = 0.25f;

        [Tooltip("At or beyond this focus distance (m) the speck is fully blurred: a faint, wide smudge.")]
        [Range(0.1f, 20f)]
        [SerializeField] private float _speckBlurDistance = 3f;

        [Tooltip("How many times wider the fully blurred speck is than the sharp one.")]
        [Range(1f, 8f)]
        [SerializeField] private float _speckBlurGrowth = 3.5f;

        [Tooltip("Lens focal length in mm for the depth of field: longer = shallower focus.")]
        [Range(10f, 300f)]
        [SerializeField] private float _focalLength = 60f;

        [Tooltip("Lens aperture (f-stop) for the depth of field: lower = shallower focus, softer blur.")]
        [Range(1f, 32f)]
        [SerializeField] private float _aperture = 1.6f;

        [Tooltip("Nearest focus the lens can hunt to (m).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _nearestFocus = 0.1f;

        [Tooltip("Priority of the lens' own volume: above the world's look, below the pause menu's backdrop " +
            "blur (100), so a menu still blurs over the opening.")]
        [Range(1f, 99f)]
        [SerializeField] private float _volumePriority = 90f;

        public float LidSoftness => _lidSoftness;
        public float LidCurve => _lidCurve;
        public float MaxDip => _maxDip;
        public float RollBandHeight => _rollBandHeight;
        public float RollBandDarkness => _rollBandDarkness;
        public float RollBandSpeed => _rollBandSpeed;
        public Color WarmTint => _warmTint;
        public float WarmLift => _warmLift;
        public float SpeckRadius => _speckRadius;
        public float SpeckDarkness => _speckDarkness;
        public float SpeckRim => _speckRim;
        public Color SpeckRimColor => _speckRimColor;
        public float SpeckSharpDistance => _speckSharpDistance;
        public float SpeckBlurDistance => Mathf.Max(_speckBlurDistance, _speckSharpDistance + MinFocusSpan);
        public float SpeckBlurGrowth => _speckBlurGrowth;
        public float FocalLength => _focalLength;
        public float Aperture => _aperture;
        public float NearestFocus => _nearestFocus;
        public float VolumePriority => _volumePriority;
    }
}
