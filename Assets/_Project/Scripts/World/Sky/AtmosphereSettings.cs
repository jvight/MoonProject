using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The cool night light of the moon (pillar 6, "Alone, and at peace"): a low earthlight from Earth's side of the
    /// sky that rakes long shadows across the dunes, a faint cool fill from the far horizon opposite it so the sides
    /// of built things it leaves dark still show their paint and rust, a deep violet trilight ambient so the moon is
    /// mostly cool shadow with soft-lit planes, and the horizon-coloured exponential fog that keeps near ground crisp
    /// while far rock dissolves toward the sky band. Warm lights belong to the rover and the base; everything here
    /// stays cool so they glow.
    /// </summary>
    [Serializable]
    public sealed class AtmosphereSettings
    {
        // Below this rise (in haze height scales) a view ray counts as level: its mean density is the eye's own.
        private const float HazeLevelRise = 1e-3f;

        [Header("Earthlight")]
        [Tooltip("Colour of the directional earthlight: a near-white with a faint cool cast, because Earth is " +
            "blue-green. Much brighter than the dim grey-blue ambient, so lit faces and shade faces split.")]
        [SerializeField] private Color _lightColor = new Color(0.92f, 0.94f, 1f);

        [Tooltip("Intensity of the earthlight. Square-on with the sky's ambient it stays under Art's reference light " +
            "scaled by the bloom threshold, so lit cream and dust never glow like lamps.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lightIntensity = 1.95f;

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
        [SerializeField] private float _shadowStrength = 0.78f;

        [Tooltip("Depth bias of the earthlight's shadows. Low, grazing light needs more than the pipeline default " +
            "so the facets never speckle themselves with shadow acne.")]
        [Range(0f, 3f)]
        [SerializeField] private float _shadowDepthBias = 1f;

        [Tooltip("Normal bias of the earthlight's shadows for everything but the ground (whose separate facets " +
            "would crack apart under it).")]
        [Range(0f, 3f)]
        [SerializeField] private float _shadowNormalBias = 0.5f;

        [Header("Fill")]
        [Tooltip("Colour of the fill: the cool glow of the far horizon opposite the earthlight, a little bluer than " +
            "it. It lights built things (the base, wrecks, rocks, 07), never the ground: LofiTerrain takes no " +
            "directional fill, so the dunes' shadow sides and the earthlight's long shadows stay deep.")]
        [SerializeField] private Color _fillColor = new Color(0.62f, 0.7f, 1f);

        [Tooltip("Intensity of the fill (it casts no shadows). Soft: the faces the earthlight leaves dark show their " +
            "paint, rust and dust instead of a flat violet, while staying far darker than the lit faces.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fillIntensity = 0.45f;

        [Tooltip("Elevation the fill shines from, degrees. Near the horizon it lifts walls and leaves top faces " +
            "(and their dust caps) to the earthlight.")]
        [Range(0f, 30f)]
        [SerializeField] private float _fillElevation = 6f;

        [Tooltip("Bearing of the fill relative to the earthlight's, degrees. Roughly opposite it, swung toward the " +
            "south so the base's side the spawn view looks at, which faces away from the earthlight, catches it.")]
        [Range(90f, 270f)]
        [SerializeField] private float _fillBearingOffset = 232f;

        [Header("Ambient (trilight)")]
        [Tooltip("Ambient from above: the dark sky's faint cool grey-blue (look A \"Moon grey\"). Weak, so shadow " +
            "cores read near-black.")]
        [SerializeField] private Color _ambientSky = new Color(0.075f, 0.078f, 0.092f);

        [Tooltip("Ambient from the horizon: darker than the sky band, so side faces fade from a lit top to a dark " +
            "foot.")]
        [SerializeField] private Color _ambientEquator = new Color(0.052f, 0.054f, 0.062f);

        [Tooltip("Ambient from below: the faint bounce off the dust, a dark band that grounds every prop's foot.")]
        [SerializeField] private Color _ambientGround = new Color(0.02f, 0.021f, 0.028f);

        [Header("Fog")]
        [Tooltip("Fog colour: a dark cool grey near the sky's horizon, so distant hills melt into the night (look A " +
            "\"Moon grey\").")]
        [SerializeField] private Color _fogColor = new Color(0.035f, 0.04f, 0.06f);

        [Tooltip("Exponential-squared fog density: near ground keeps its contrast, far rock dissolves toward the " +
            "horizon. 0.0006 is thin: the far rim stays crisp, only the distant ranges soften.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _fogDensity = 0.0006f;

        [Header("Basin haze")]
        [Tooltip("Colour of the thin haze lying in the basin: the fog's own grey, so the far floor and the foot of " +
            "the rim sink into one calm plane.")]
        [SerializeField] private Color _hazeColor = new Color(0.035f, 0.04f, 0.06f);

        [Tooltip("Haze density at its base, per metre of view: thin, so ground within play distance stays crisp.")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _hazeDensity = 0.00021f;

        [Tooltip("Height over which the haze thins by e (metres): it lies low, so the rim's crest stays above it.")]
        [Range(0.5f, 40f)]
        [SerializeField] private float _hazeHeight = 6f;

        [Tooltip("World height of the haze's base: the basin floor.")]
        [Range(-20f, 20f)]
        [SerializeField] private float _hazeBase = 0f;

        [Tooltip("The most the haze may cover (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _hazeMax = 0.165f;

        [Header("Distance desaturation")]
        [Tooltip("Distance (metres) where colour starts to drain from far things.")]
        [Range(0f, 1000f)]
        [SerializeField] private float _desaturateStart = 120f;

        [Tooltip("Distance (metres) where far things are as grey as they get.")]
        [Range(1f, 2000f)]
        [SerializeField] private float _desaturateEnd = 650f;

        [Tooltip("How grey far things get (0..1): far hills become calm planes of value, not colour.")]
        [Range(0f, 1f)]
        [SerializeField] private float _desaturateAmount = 0.45f;

        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public float LightElevation => _lightElevation;
        public float LightBearingOffset => _lightBearingOffset;
        public float ShadowStrength => _shadowStrength;
        public float ShadowDepthBias => _shadowDepthBias;
        public float ShadowNormalBias => _shadowNormalBias;
        public Color FillColor => _fillColor;
        public float FillIntensity => _fillIntensity;
        public float FillElevation => _fillElevation;
        public float FillBearingOffset => _fillBearingOffset;
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

        /// <summary>
        /// How much of the view from <paramref name="eye"/> to <paramref name="point"/> the basin haze covers (0..1):
        /// density times the ray's length times the mean of exp(-height / scale) along it. LofiAtmosphere.hlsl's
        /// MoonHazeAmount is the same formula.
        /// </summary>
        public float HazeAmount(Vector3 eye, Vector3 point)
        {
            Vector3 ray = point - eye;
            float falloff = 1f / _hazeHeight;
            float eyeHeight = (eye.y - _hazeBase) * falloff;
            float rise = ray.y * falloff;
            float mean = Mathf.Abs(rise) > HazeLevelRise
                ? (Mathf.Exp(-eyeHeight) - Mathf.Exp(-eyeHeight - rise)) / rise
                : Mathf.Exp(-eyeHeight);
            return Mathf.Min(1f - Mathf.Exp(-_hazeDensity * ray.magnitude * mean), _hazeMax);
        }
    }
}
