using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The cool night light of the moon: the earthlight (a directional light from Earth's side of the sky), the
    /// violet trilight ambient and the horizon-coloured exponential fog that hides the world's edge. Warm lights
    /// belong to the rover and the base; everything here stays cool so they glow.
    /// </summary>
    [Serializable]
    public sealed class AtmosphereSettings
    {
        [Header("Earthlight")]
        [Tooltip("Colour of the directional earthlight.")]
        [SerializeField] private Color _lightColor = new Color(0.78f, 0.80f, 1f);

        [Tooltip("Intensity of the earthlight.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lightIntensity = 1.7f;

        [Tooltip("Elevation the earthlight shines from, degrees. Low light makes gentle dunes and facets read.")]
        [Range(5f, 89f)]
        [SerializeField] private float _lightElevation = 30f;

        [Tooltip("Bearing offset of the earthlight from Earth's bearing, degrees. Swung west so the view from the " +
            "base toward The Peak is side-lit (best relief) while the light still comes from Earth's side of the sky.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _lightBearingOffset = -60f;

        [Tooltip("Darkness of the earthlight's shadows (1 = fully dark before ambient).")]
        [Range(0f, 1f)]
        [SerializeField] private float _shadowStrength = 0.8f;

        [Header("Ambient (trilight)")]
        [Tooltip("Ambient from above: the violet glow of the sky.")]
        [SerializeField] private Color _ambientSky = new Color(0.19f, 0.17f, 0.37f);

        [Tooltip("Ambient from the horizon.")]
        [SerializeField] private Color _ambientEquator = new Color(0.14f, 0.12f, 0.27f);

        [Tooltip("Ambient from below: bounce off the dust.")]
        [SerializeField] private Color _ambientGround = new Color(0.08f, 0.07f, 0.15f);

        [Header("Fog")]
        [Tooltip("Fog colour: matches the sky's horizon glow so distant hills melt into the sky.")]
        [SerializeField] private Color _fogColor = new Color(0.25f, 0.18f, 0.44f);

        [Tooltip("Exponential-squared fog density: keeps the crater crisp and hides the world's edge. 0.00085 " +
            "fogs ~11% at 400 m, ~51% at 1 km and >99% at 2.8 km.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _fogDensity = 0.00085f;

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
