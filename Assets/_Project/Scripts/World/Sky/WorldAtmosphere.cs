using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonProject.World
{
    /// <summary>
    /// Applies <see cref="AtmosphereSettings"/> to the scene: trilight ambient, exponential-squared fog, the
    /// directional earthlight shining from Earth's side of the sky and the shadowless fill opposite it. Used by the
    /// scene build (saved into the scene) and by <see cref="WorldSystem"/> (so play mode always matches the tuning
    /// asset).
    /// </summary>
    public static class WorldAtmosphere
    {
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
            RenderSettings.ambientSkyColor = atmosphere.AmbientSky;
            RenderSettings.ambientEquatorColor = atmosphere.AmbientEquator;
            RenderSettings.ambientGroundColor = atmosphere.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = atmosphere.FogColor;
            RenderSettings.fogDensity = atmosphere.FogDensity;
            RenderSettings.sun = earthlight;

            // URP lights with the ambient probe, which is only re-derived from the trilight colours on request.
            DynamicGI.UpdateEnvironment();

            earthlight.type = LightType.Directional;
            earthlight.color = atmosphere.LightColor;
            earthlight.intensity = atmosphere.LightIntensity;
            earthlight.shadows = LightShadows.Soft;
            earthlight.shadowStrength = atmosphere.ShadowStrength;
            earthlight.GetUniversalAdditionalLightData().usePipelineSettings = false;
            earthlight.shadowBias = atmosphere.ShadowDepthBias;
            earthlight.shadowNormalBias = atmosphere.ShadowNormalBias;
            earthlight.transform.rotation = Quaternion.LookRotation(-LightSourceDirection(atmosphere, sky));

            // The earthlight stays the main light (RenderSettings.sun): the fill is a Forward+ additional light.
            fill.type = LightType.Directional;
            fill.color = atmosphere.FillColor;
            fill.intensity = atmosphere.FillIntensity;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.LookRotation(-FillSourceDirection(atmosphere, sky));
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
