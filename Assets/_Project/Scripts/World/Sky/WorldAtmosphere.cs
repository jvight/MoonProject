using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonProject.World
{
    /// <summary>
    /// Applies <see cref="AtmosphereSettings"/> to the scene: trilight ambient, exponential-squared fog, the basin haze
    /// and distance desaturation (shader globals for LofiAtmosphere.hlsl), the
    /// directional earthlight shining from Earth's side of the sky and the shadowless fill opposite it. Used by the
    /// scene build (saved into the scene) and by <see cref="WorldSystem"/> (so play mode always matches the tuning
    /// asset).
    /// </summary>
    public static class WorldAtmosphere
    {
        private static readonly int HazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int HazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int DesaturateId = Shader.PropertyToID("_MoonDesaturate");

        public static void Apply(AtmosphereSettings atmosphere, SkySettings sky, Light earthlight, Light fill)
        {
            if (atmosphere == null || sky == null || earthlight == null || fill == null)
            {
                throw new ArgumentNullException(atmosphere == null ? nameof(atmosphere)
                    : sky == null ? nameof(sky) : earthlight == null ? nameof(earthlight) : nameof(fill));
            }

            if (fill == earthlight)
            {
                throw new ArgumentException("The fill must be a light of its own, not the earthlight.", nameof(fill));
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.sun = earthlight;

            earthlight.type = LightType.Directional;
            earthlight.shadows = LightShadows.Soft;
            earthlight.shadowStrength = atmosphere.ShadowStrength;
            earthlight.GetUniversalAdditionalLightData().usePipelineSettings = false;
            earthlight.shadowBias = atmosphere.ShadowDepthBias;
            earthlight.shadowNormalBias = atmosphere.ShadowNormalBias;
            earthlight.transform.rotation = Quaternion.LookRotation(-LightSourceDirection(atmosphere, sky));

            // The earthlight stays the main light (RenderSettings.sun): the fill is a Forward+ additional light.
            fill.type = LightType.Directional;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.LookRotation(-FillSourceDirection(atmosphere, sky));

            BlendedAtmosphere blended = BlendedAtmosphere.FromSettings(atmosphere, sky);
            ApplyBlended(in blended, earthlight, fill, true);
        }

        /// <summary>
        /// Applies blended region atmosphere values to <see cref="RenderSettings"/>, the haze/desaturation shader
        /// globals, and the directional earthlight and fill light.
        /// </summary>
        public static void ApplyBlended(in BlendedAtmosphere atmosphere, Light earthlight, Light fill,
            bool updateEnvironment = true)
        {
            if (earthlight == null || fill == null)
            {
                throw new ArgumentNullException(earthlight == null ? nameof(earthlight) : nameof(fill));
            }

            RenderSettings.ambientSkyColor = atmosphere.AmbientSky;
            RenderSettings.ambientEquatorColor = atmosphere.AmbientEquator;
            RenderSettings.ambientGroundColor = atmosphere.AmbientGround;
            RenderSettings.fogColor = atmosphere.FogColor;
            RenderSettings.fogDensity = atmosphere.FogDensity;

            Color haze = atmosphere.HazeColor.linear;
            Shader.SetGlobalVector(HazeColorId, new Vector4(haze.r, haze.g, haze.b, atmosphere.HazeMax));
            Shader.SetGlobalVector(HazeId,
                new Vector4(atmosphere.HazeDensity, 1f / atmosphere.HazeHeight, atmosphere.HazeBase, 0f));
            Shader.SetGlobalVector(DesaturateId, new Vector4(atmosphere.DesaturateStart,
                1f / Mathf.Max(atmosphere.DesaturateEnd - atmosphere.DesaturateStart, 1f),
                atmosphere.DesaturateAmount, 0f));

            if (updateEnvironment)
            {
                // URP lights with the ambient probe, which is only re-derived from the trilight colours on request.
                DynamicGI.UpdateEnvironment();
            }

            earthlight.color = atmosphere.LightColor;
            earthlight.intensity = atmosphere.LightIntensity;
            fill.color = atmosphere.FillColor;
            fill.intensity = atmosphere.FillIntensity;
        }

        /// <summary>Unit vector pointing from the ground toward where the earthlight comes from.</summary>
        public static Vector3 LightSourceDirection(AtmosphereSettings atmosphere, SkySettings sky)
        {
            return SourceDirection(LightBearing(atmosphere, sky), atmosphere.LightElevation);
        }

        /// <summary>Unit vector pointing from the ground toward where the fill comes from.</summary>
        public static Vector3 FillSourceDirection(AtmosphereSettings atmosphere, SkySettings sky)
        {
            return SourceDirection(LightBearing(atmosphere, sky) + atmosphere.FillBearingOffset,
                atmosphere.FillElevation);
        }

        private static float LightBearing(AtmosphereSettings atmosphere, SkySettings sky)
        {
            return sky.EarthBearing + atmosphere.LightBearingOffset;
        }

        private static Vector3 SourceDirection(float bearing, float elevationDegrees)
        {
            float elevation = elevationDegrees * Mathf.Deg2Rad;
            Vector2 horizontal = MoonSurface.BearingToDirection(bearing) * Mathf.Cos(elevation);
            return new Vector3(horizontal.x, Mathf.Sin(elevation), horizontal.y);
        }
    }
}
