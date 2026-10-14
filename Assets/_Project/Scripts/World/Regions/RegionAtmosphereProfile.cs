using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Per-region volume bounds, environmental modifiers, and atmosphere/sky parameters (docs/design/0.6-world-regions.md).
    /// In S1 both <see cref="RegionIds.Basin"/> and <see cref="RegionIds.Canyon"/> default to look A ("Moon grey").
    /// </summary>
    [Serializable]
    public sealed class RegionAtmosphereProfile
    {
        public const float DefaultBasinOuterRadius = 420f;
        public const float DefaultBasinBlendBand = 60f;
        public const float DefaultCanyonHalfWidth = 38f;
        public const float DefaultCanyonBlendBand = 25f;
        public const float DefaultFarClipPlane = 3200f;

        [Header("Region")]
        [Tooltip("Stable region identifier (RegionIds).")]
        [SerializeField] private string _regionId = RegionIds.Basin;

        [Tooltip("Outer radius (m) for disc volumes or corridor half-width (m) for capsule volumes.")]
        [Range(1f, 2000f)]
        [SerializeField] private float _outerRadius = DefaultBasinOuterRadius;

        [Tooltip("Width (m) of this region's signed-distance blend band at its boundary.")]
        [Range(1f, 500f)]
        [SerializeField] private float _blendBand = DefaultBasinBlendBand;

        [Tooltip("Recommended camera far clip plane (m) inside this region.")]
        [Range(200f, 8000f)]
        [SerializeField] private float _farClipPlane = DefaultFarClipPlane;

        [Header("Earthlight & Fill")]
        [Tooltip("Colour of the directional earthlight in this region.")]
        [SerializeField] private Color _lightColor = new Color(0.92f, 0.94f, 1f);

        [Tooltip("Intensity of the directional earthlight in this region.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lightIntensity = 1.95f;

        [Tooltip("Colour of the directional fill light in this region.")]
        [SerializeField] private Color _fillColor = new Color(0.62f, 0.7f, 1f);

        [Tooltip("Intensity of the directional fill light in this region.")]
        [Range(0f, 2f)]
        [SerializeField] private float _fillIntensity = 0.45f;

        [Header("Ambient (trilight)")]
        [Tooltip("Ambient colour from above.")]
        [SerializeField] private Color _ambientSky = new Color(0.075f, 0.078f, 0.092f);

        [Tooltip("Ambient colour from the horizon.")]
        [SerializeField] private Color _ambientEquator = new Color(0.052f, 0.054f, 0.062f);

        [Tooltip("Ambient colour from below.")]
        [SerializeField] private Color _ambientGround = new Color(0.02f, 0.021f, 0.028f);

        [Header("Fog")]
        [Tooltip("Exponential-squared fog colour.")]
        [SerializeField] private Color _fogColor = new Color(0.035f, 0.04f, 0.06f);

        [Tooltip("Exponential-squared fog density.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _fogDensity = 0.0006f;

        [Header("Haze & Desaturation")]
        [Tooltip("Colour of the low ground haze.")]
        [SerializeField] private Color _hazeColor = new Color(0.035f, 0.04f, 0.06f);

        [Tooltip("Haze density at its base, per metre of view.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _hazeDensity = 0.00021f;

        [Tooltip("Height over which the haze thins by e (metres).")]
        [Range(0.5f, 40f)]
        [SerializeField] private float _hazeHeight = 6f;

        [Tooltip("World height of the haze's base (metres).")]
        [Range(-60f, 60f)]
        [SerializeField] private float _hazeBase;

        [Tooltip("Maximum coverage of the haze (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _hazeMax = 0.165f;

        [Tooltip("Distance (metres) where colour starts to drain from far things.")]
        [Range(0f, 1000f)]
        [SerializeField] private float _desaturateStart = 120f;

        [Tooltip("Distance (metres) where far things are as grey as they get.")]
        [Range(1f, 2000f)]
        [SerializeField] private float _desaturateEnd = 650f;

        [Tooltip("How grey far things get (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _desaturateAmount = 0.45f;

        [Header("Sky Gradient")]
        [Tooltip("Colour at the zenith.")]
        [SerializeField] private Color _skyTop = new Color(0.0507f, 0.0702f, 0.1517f);

        [Tooltip("Soft glow hugging the horizon.")]
        [SerializeField] private Color _horizonGlow = new Color(0.1648f, 0.1045f, 0.2855f);

        [Tooltip("Height of the horizon glow.")]
        [Range(0.01f, 0.6f)]
        [SerializeField] private float _horizonGlowHeight = 0.056f;

        [Tooltip("Thin band of light right along the horizon line.")]
        [SerializeField] private Color _horizonBand = new Color(0.1511f, 0.1107f, 0.2319f);

        [Tooltip("Height of the thin horizon band.")]
        [Range(0.002f, 0.2f)]
        [SerializeField] private float _horizonBandHeight = 0.03f;

        [Tooltip("Exponent of the horizon-to-zenith blend.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _gradientExponent = 0.55f;

        public RegionAtmosphereProfile()
        {
        }

        public RegionAtmosphereProfile(
            string regionId,
            float outerRadius,
            float blendBand,
            float farClipPlane = DefaultFarClipPlane,
            float fogDensity = 0.0006f,
            float lightIntensity = 1.95f)
        {
            _regionId = regionId ?? RegionIds.Basin;
            _outerRadius = outerRadius;
            _blendBand = blendBand;
            _farClipPlane = farClipPlane;
            _fogDensity = fogDensity;
            _lightIntensity = lightIntensity;
        }

        public string RegionId => _regionId;
        public float OuterRadius => _outerRadius;
        public float BlendBand => _blendBand;
        public float FarClipPlane => _farClipPlane;
        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public Color FillColor => _fillColor;
        public float FillIntensity => _fillIntensity;
        public Color AmbientSky => _ambientSky;
        public Color AmbientEquator => _ambientEquator;
        public Color AmbientGround => _ambientGround;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public Color HazeColor => _hazeColor;
        public float HazeDensity => _hazeDensity;
        public float HazeHeight => _hazeHeight;
        public float HazeBase => _hazeBase;
        public float HazeMax => _hazeMax;
        public float DesaturateStart => _desaturateStart;
        public float DesaturateEnd => _desaturateEnd;
        public float DesaturateAmount => _desaturateAmount;
        public Color SkyTop => _skyTop;
        public Color HorizonGlow => _horizonGlow;
        public float HorizonGlowHeight => _horizonGlowHeight;
        public Color HorizonBand => _horizonBand;
        public float HorizonBandHeight => _horizonBandHeight;
        public float GradientExponent => _gradientExponent;

        /// <summary>Creates the default Look A profile for the home basin.</summary>
        public static RegionAtmosphereProfile CreateBasin()
        {
            return new RegionAtmosphereProfile(RegionIds.Basin, DefaultBasinOuterRadius, DefaultBasinBlendBand);
        }

        /// <summary>Creates the default Look A profile for Whispering Canyon.</summary>
        public static RegionAtmosphereProfile CreateCanyon()
        {
            return new RegionAtmosphereProfile(RegionIds.Canyon, DefaultCanyonHalfWidth, DefaultCanyonBlendBand);
        }

        /// <summary>Returns null when valid, otherwise a description of the first invalid setting.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(_regionId))
            {
                return "Regions: a region profile has an empty regionId.";
            }

            if (_outerRadius <= 0f || _blendBand <= 0f)
            {
                return $"Regions ({_regionId}): outerRadius and blendBand must be positive.";
            }

            if (_farClipPlane <= 0f)
            {
                return $"Regions ({_regionId}): farClipPlane must be positive.";
            }

            if (_hazeHeight <= 0f)
            {
                return $"Regions ({_regionId}): hazeHeight must be positive.";
            }

            return null;
        }
    }
}
