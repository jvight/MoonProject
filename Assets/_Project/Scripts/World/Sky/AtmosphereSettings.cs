using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The cool night light of the moon (pillar 6, "Alone, and at peace"): a low earthlight from Earth's side of the
    /// sky that rakes long shadows across the dunes, a deep violet trilight ambient so the moon is mostly cool
    /// shadow with soft-lit planes, and the horizon-coloured exponential fog that keeps near ground crisp while far
    /// rock dissolves toward the sky band. Warm lights belong to the rover and the base; everything here stays cool
    /// so they glow.
    /// </summary>
    [Serializable]
    public sealed class AtmosphereSettings
    {
        [Header("Earthlight")]
        [Tooltip("Colour of the directional earthlight.")]
        [SerializeField] private Color _lightColor = new Color(0.86f, 0.85f, 1f);

        [Tooltip("Intensity of the earthlight.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lightIntensity = 1.75f;

        [Tooltip("Elevation the earthlight shines from, degrees. Low light rakes long shadows across the dunes and " +
            "craters and makes the facets read.")]
        [Range(5f, 89f)]
        [SerializeField] private float _lightElevation = 19f;

        [Tooltip("Bearing offset of the earthlight from Earth's bearing, degrees. Swung west so the view from the " +
            "base toward The Peak is side-lit (best relief) while the light still comes from Earth's side of the sky.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _lightBearingOffset = -60f;

        [Tooltip("Darkness of the earthlight's shadows (1 = fully dark before ambient).")]
        [Range(0f, 1f)]
        [SerializeField] private float _shadowStrength = 0.88f;

        [Header("Ambient (trilight)")]
        [Tooltip("Ambient from above: the violet glow of the sky.")]
        [SerializeField] private Color _ambientSky = new Color(0.15f, 0.135f, 0.3f);

        [Tooltip("Ambient from the horizon.")]
        [SerializeField] private Color _ambientEquator = new Color(0.115f, 0.1f, 0.23f);

        [Tooltip("Ambient from below: bounce off the dust.")]
        [SerializeField] private Color _ambientGround = new Color(0.06f, 0.055f, 0.12f);

        [Header("Fog")]
        [Tooltip("Fog colour: matches the sky's horizon glow so distant hills melt into the sky.")]
        [SerializeField] private Color _fogColor = new Color(0.27f, 0.21f, 0.45f);

        [Tooltip("Exponential-squared fog density: near ground keeps its contrast, far rock dissolves toward the " +
            "horizon. 0.002 fogs ~9% at 150 m, ~30% at 300 m, ~63% at 500 m and ~98% at 1 km.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _fogDensity = 0.002f;

        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public float LightElevation => _lightElevation;
        public float LightBearingOffset => _lightBearingOffset;
        public float ShadowStrength => _shadowStrength;
        public Color AmbientSky => _ambientSky;
        public Color AmbientEquator => _ambientEquator;
        public Color AmbientGround => _ambientGround;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
    }
}
