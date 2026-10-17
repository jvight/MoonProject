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
            RenderSettings.ambientSkyColor = atmosphere.AmbientSky;
            RenderSettings.ambientEquatorColor = atmosphere.AmbientEquator;
            RenderSettings.ambientGroundColor = atmosphere.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = atmosphere.FogColor;
            RenderSettings.fogDensity = atmosphere.FogDensity;
            RenderSettings.sun = earthlight;
            Color haze = atmosphere.HazeColor.linear;
            Shader.SetGlobalVector(HazeColorId, new Vector4(haze.r, haze.g, haze.b, atmosphere.HazeMax));
            Shader.SetGlobalVector(HazeId,
                new Vector4(atmosphere.HazeDensity, 1f / atmosphere.HazeHeight, atmosphere.HazeBase, 0f));
            Shader.SetGlobalVector(DesaturateId, new Vector4(atmosphere.DesaturateStart,
                1f / Mathf.Max(atmosphere.DesaturateEnd - atmosphere.DesaturateStart, 1f),
                atmosphere.DesaturateAmount, 0f));

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

        private const float DawnKeyCrossover = 0.04f;
        private const float LocalShadowAmbientFloor = 0.85f;

        /// <summary>
        /// Updates the scene's single shadow-casting key light (<paramref name="keyLight"/>), shadowless fill
        /// (<paramref name="fillLight"/>), trilight ambient, fog and basin haze for the live <see cref="SunClock"/>,
        /// smoothed global sunlight <paramref name="smoothedSunlight"/> (0..1 from <see cref="SunClock.GlobalSunlight"/>)
        /// and optional smoothed local sunlight <paramref name="localSunlight"/> (0..1 at 07's position).
        /// <para>
        /// The directional key light, fill light, fog and basin haze follow global sun elevation and eclipse so the
        /// sun never goes out across the moon when 07 enters a local ridge or crater shadow; local shadow only applies
        /// a subtle eased adaptation to the trilight ambient. As soon as the Sun's limb clears the horizon or Earth's
        /// edge (<c>smoothedSunlight &gt; DawnKeyCrossover</c>), the key light switches to the Sun and lights the
        /// landscape with warm grazing dawn light rather than waiting for <c>sunlit &gt; 0.5</c>.
        /// </para>
        /// </summary>
        public static void ApplyCycle(AtmosphereSettings atmosphere, SkySettings sky, SkyCycleSettings cycle,
            SunClock clock, float smoothedSunlight, Light keyLight, Light fillLight, bool updateAmbientProbe = false,
            float localSunlight = -1f)
        {
            if (atmosphere == null || sky == null || cycle == null || clock == null
                || keyLight == null || fillLight == null)
            {
                throw new ArgumentNullException(atmosphere == null ? nameof(atmosphere)
                    : sky == null ? nameof(sky) : cycle == null ? nameof(cycle) : clock == null ? nameof(clock)
                    : keyLight == null ? nameof(keyLight) : nameof(fillLight));
            }

            float sunlit = Mathf.Clamp01(smoothedSunlight);
            float local = localSunlight >= 0f ? Mathf.Clamp01(localSunlight) : sunlit;
            float ambientSunlit = Mathf.Lerp(sunlit * LocalShadowAmbientFloor, sunlit, local);
            float skyDayBlend = Mathf.SmoothStep(0f, 1f, Mathf.Pow(sunlit, 0.65f));
            float ambientBlend = Mathf.SmoothStep(0f, 1f, Mathf.Pow(ambientSunlit, 0.65f));
            float eclipse = Mathf.Clamp01(clock.EclipseFactor);
            float ambientEclipseScale = Mathf.Lerp(1f, cycle.EclipseAmbientScale, eclipse * (1f - skyDayBlend));

            RenderSettings.ambientSkyColor =
                Color.Lerp(atmosphere.AmbientSky, cycle.SunlitAmbientSky, ambientBlend) * ambientEclipseScale;
            RenderSettings.ambientEquatorColor =
                Color.Lerp(atmosphere.AmbientEquator, cycle.SunlitAmbientEquator, ambientBlend) * ambientEclipseScale;
            RenderSettings.ambientGroundColor =
                Color.Lerp(atmosphere.AmbientGround, cycle.SunlitAmbientGround, ambientBlend) * ambientEclipseScale;
            RenderSettings.fogColor =
                Color.Lerp(atmosphere.FogColor, cycle.SunlitFogColor, skyDayBlend) * ambientEclipseScale;
            RenderSettings.fogDensity = atmosphere.FogDensity;

            Color haze = (Color.Lerp(atmosphere.HazeColor, cycle.SunlitHazeColor, skyDayBlend) * ambientEclipseScale).linear;
            Shader.SetGlobalVector(HazeColorId, new Vector4(haze.r, haze.g, haze.b, atmosphere.HazeMax));

            if (updateAmbientProbe)
            {
                DynamicGI.UpdateEnvironment();
            }

            float phaseScale = Mathf.Lerp(cycle.MinEarthPhaseLight, 1f, Mathf.SmoothStep(0f, 0.7f, clock.EarthPhase));
            float earthKeyIntensity = Mathf.Lerp(atmosphere.LightIntensity * phaseScale, cycle.EclipseKeyIntensity,
                eclipse);
            float earthFillIntensity = Mathf.Lerp(atmosphere.FillIntensity * phaseScale, cycle.EclipseFillIntensity,
                eclipse);

            // Brief handoff bridge right around DawnKeyCrossover so the transition from earthlight to first dawn bead
            // is continuous.
            float handoff = 1f - Mathf.Clamp01(Mathf.Abs(sunlit - DawnKeyCrossover) / DawnKeyCrossover);
            float crossfadeBridge = handoff * handoff * (3f - 2f * handoff) * 0.22f;

            if (sunlit > DawnKeyCrossover)
            {
                float sunT = Mathf.Clamp01((sunlit - DawnKeyCrossover) / (1f - DawnKeyCrossover));
                float sunCurve = Mathf.SmoothStep(0f, 1f, Mathf.Pow(sunT, 0.55f));
                float emergenceRamp = Mathf.SmoothStep(0f, 1f, sunT / 0.08f);
                float sunWeight = Mathf.Lerp(0.42f, 1f, sunCurve) * emergenceRamp;
                float shadowWeight = Mathf.Lerp(0.70f, 1f, sunCurve) * emergenceRamp;

                float twilightSpan = Mathf.Max(cycle.MaxElevation - 1.5f - cycle.TwilightElevation * 0.75f, 1f);
                float twilightT = Mathf.Clamp01((cycle.MaxElevation - 1.5f - clock.Elevation) / twilightSpan);
                float lowElevationWarmth = twilightT * twilightT * (3f - 2f * twilightT);
                float dawnReleaseWarmth = 1f - Mathf.Clamp01(
                    clock.TimeSeconds / Mathf.Max(cycle.DawnSweepSeconds * 2.5f, 1f));
                float warmth = Mathf.Clamp01(Mathf.Max(lowElevationWarmth * 0.85f, dawnReleaseWarmth * 0.65f));

                keyLight.color = Color.Lerp(cycle.SunlitKeyColor, cycle.DawnKeyColor, warmth);
                keyLight.intensity = cycle.SunlitKeyIntensity * sunWeight;
                keyLight.shadowStrength = cycle.SunlitShadowStrength * shadowWeight;

                Vector3 toSun = clock.SunDirection;
                if (toSun.y < 0.035f)
                {
                    toSun = new Vector3(toSun.x, 0.035f, toSun.z).normalized;
                }

                keyLight.transform.rotation = Quaternion.LookRotation(-toSun);

                Vector3 toFill = SourceDirection(clock.Bearing + atmosphere.FillBearingOffset, atmosphere.FillElevation);
                fillLight.color = Color.Lerp(atmosphere.FillColor, cycle.SunlitFillColor, sunCurve);
                fillLight.intensity = Mathf.Lerp(earthFillIntensity, cycle.SunlitFillIntensity, sunCurve)
                    + crossfadeBridge;
                fillLight.transform.rotation = Quaternion.LookRotation(-toFill);
            }
            else
            {
                float earthWeight = Mathf.SmoothStep(0f, 1f, 1f - sunlit / DawnKeyCrossover);
                keyLight.color = atmosphere.LightColor;
                keyLight.intensity = earthKeyIntensity * earthWeight;
                keyLight.shadowStrength = atmosphere.ShadowStrength * (1f - eclipse * 0.45f) * earthWeight;
                keyLight.transform.rotation = Quaternion.LookRotation(-LightSourceDirection(atmosphere, sky));

                fillLight.color = atmosphere.FillColor;
                fillLight.intensity = earthFillIntensity + crossfadeBridge;
                fillLight.transform.rotation = Quaternion.LookRotation(-FillSourceDirection(atmosphere, sky));
            }
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
