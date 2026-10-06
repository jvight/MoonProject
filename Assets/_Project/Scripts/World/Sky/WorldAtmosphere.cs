using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.World
{
    /// <summary>
    /// Applies <see cref="AtmosphereSettings"/> to the scene: trilight ambient, exponential-squared fog and the directional
    /// earthlight shining from Earth's side of the sky. Used by the scene build (saved into the scene) and by
    /// <see cref="WorldSystem"/> (so play mode always matches the tuning asset).
    /// </summary>
    public static class WorldAtmosphere
    {
        public static void Apply(AtmosphereSettings atmosphere, SkySettings sky, Light earthlight)
        {
            if (atmosphere == null || sky == null || earthlight == null)
            {
                throw new ArgumentNullException(atmosphere == null ? nameof(atmosphere)
                    : sky == null ? nameof(sky) : nameof(earthlight));
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
            earthlight.transform.rotation = Quaternion.LookRotation(-LightSourceDirection(atmosphere, sky));
        }

        /// <summary>Unit vector pointing from the ground toward where the earthlight comes from.</summary>
        public static Vector3 LightSourceDirection(AtmosphereSettings atmosphere, SkySettings sky)
        {
            float elevation = atmosphere.LightElevation * Mathf.Deg2Rad;
            Vector2 horizontal = MoonSurface.BearingToDirection(sky.EarthBearing + atmosphere.LightBearingOffset)
                * Mathf.Cos(elevation);
            return new Vector3(horizontal.x, Mathf.Sin(elevation), horizontal.y);
        }
    }
}
