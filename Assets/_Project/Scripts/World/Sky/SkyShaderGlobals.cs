using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Pushes <see cref="SkySettings"/> into the global shader properties read by LofiSky and LofiEarth
    /// (Shaders/World). Globals instead of material properties keep WorldSettings the single source of truth and
    /// leave the material assets untouched at runtime.
    /// </summary>
    public static class SkyShaderGlobals
    {
        /// <summary>Earth's axial tilt, degrees: its spin axis leans this much from "up" seen from the moon.</summary>
        public const float EarthAxialTilt = 23.4f;

        private const float EarthRimExponent = 2.6f;

        private static readonly int SkyTop = Shader.PropertyToID("_MoonSkyTop");
        private static readonly int HorizonGlow = Shader.PropertyToID("_MoonHorizonGlow");
        private static readonly int HorizonBand = Shader.PropertyToID("_MoonHorizonBand");
        private static readonly int StarParams = Shader.PropertyToID("_MoonStarParams");
        private static readonly int StarTwinkle = Shader.PropertyToID("_MoonStarTwinkle");
        private static readonly int StarShape = Shader.PropertyToID("_MoonStarShape");
        private static readonly int MilkyWayAxis = Shader.PropertyToID("_MoonMilkyWayAxis");
        private static readonly int MilkyWayColor = Shader.PropertyToID("_MoonMilkyWayColor");
        private static readonly int EarthDirection = Shader.PropertyToID("_MoonEarthDirection");
        private static readonly int EarthHalo = Shader.PropertyToID("_MoonEarthHalo");
        private static readonly int EarthLimb = Shader.PropertyToID("_MoonEarthLimb");
        private static readonly int EarthAxisX = Shader.PropertyToID("_MoonEarthAxisX");
        private static readonly int EarthAxisY = Shader.PropertyToID("_MoonEarthAxisY");
        private static readonly int EarthAxisZ = Shader.PropertyToID("_MoonEarthAxisZ");
        private static readonly int EarthSun = Shader.PropertyToID("_MoonEarthSun");
        private static readonly int EarthGlow = Shader.PropertyToID("_MoonEarthGlow");
        private static readonly int EarthAtmosphere = Shader.PropertyToID("_MoonEarthAtmosphere");
        private static readonly int ShootingStars = Shader.PropertyToID("_MoonShootingStars");
        private static readonly int SunDirection = Shader.PropertyToID("_MoonSunDirection");
        private static readonly int SunColor = Shader.PropertyToID("_MoonSunColor");
        private static readonly int EclipseRing = Shader.PropertyToID("_MoonEclipseRing");
        private static readonly int EarthEclipse = Shader.PropertyToID("_MoonEarthEclipse");

        public static void Apply(SkySettings sky)
        {
            if (sky == null)
            {
                throw new System.ArgumentNullException(nameof(sky));
            }

            Shader.SetGlobalVector(SkyTop, WithAlpha(sky.SkyTop.linear, sky.GradientExponent));
            Shader.SetGlobalVector(HorizonGlow, WithAlpha(sky.HorizonGlow.linear, sky.HorizonGlowHeight));
            Shader.SetGlobalVector(HorizonBand, WithAlpha(sky.HorizonBand.linear, sky.HorizonBandHeight));
            Shader.SetGlobalVector(StarParams,
                new Vector4(sky.StarDensity, sky.StarBrightness, sky.StarPowerLaw, sky.StarSize));
            Shader.SetGlobalVector(StarTwinkle,
                new Vector4(sky.TwinkleShare, sky.TwinkleSpeed, sky.TwinkleDepth, sky.StarGlow));
            Shader.SetGlobalVector(StarShape,
                new Vector4(sky.StarSizeSpread, sky.StarBrightest, sky.StarGlowFrom, 0f));
            Shader.SetGlobalVector(MilkyWayAxis, WithAlpha(sky.MilkyWayAxis, sky.MilkyWayIntensity));
            Shader.SetGlobalVector(MilkyWayColor, WithAlpha(sky.MilkyWayColor.linear, sky.MilkyWayWidth));

            Vector3 toEarth = sky.EarthDirection;
            float radius = sky.EarthDiameter * 0.5f * Mathf.Deg2Rad;
            Shader.SetGlobalVector(EarthDirection, WithAlpha(toEarth, radius));
            Shader.SetGlobalVector(EarthHalo,
                WithAlpha(sky.EarthAtmosphere.linear * sky.EarthHaloStrength, sky.EarthHaloSize));
            Shader.SetGlobalVector(EarthLimb,
                WithAlpha(sky.EarthAtmosphere.linear * sky.EarthLimb, sky.EarthLimbWidth));

            // Earth's frame as seen from the moon: X to the viewer's right, Y its (tilted) spin axis, Z toward the
            // viewer, so the lit side and the land masses read the same from anywhere on the map.
            Vector3 right = Vector3.Cross(Vector3.up, toEarth).normalized;
            Vector3 up = Vector3.Cross(toEarth, right);
            Quaternion tilt = Quaternion.AngleAxis(EarthAxialTilt, toEarth);
            Vector3 axisX = tilt * right;
            Vector3 axisY = tilt * up;
            Vector3 axisZ = -toEarth;
            Shader.SetGlobalVector(EarthAxisX, WithAlpha(axisX, Mathf.Tan(radius)));
            Shader.SetGlobalVector(EarthAxisY, axisY);
            Shader.SetGlobalVector(EarthAxisZ, axisZ);

            Vector3 sun = sky.EarthSunDirection;
            Vector3 sunWorld = (right * sun.x + up * sun.y - toEarth * sun.z).normalized;
            Shader.SetGlobalVector(EarthSun, WithAlpha(sunWorld, sky.EarthNightBrightness));
            Shader.SetGlobalVector(EarthGlow, new Vector4(sky.EarthGlow, Mathf.PI * 2f / sky.EarthSpinPeriod, 0f, 0f));
            Shader.SetGlobalVector(EarthAtmosphere,
                WithAlpha(sky.EarthAtmosphere.linear * sky.EarthRim, EarthRimExponent));
            Shader.SetGlobalVector(ShootingStars,
                new Vector4(sky.ShootingStarPeriod, sky.ShootingStarDuration, sky.ShootingStarBrightness, 0f));
            Shader.SetGlobalVector(SunDirection, Vector4.zero);
            Shader.SetGlobalVector(SunColor, Vector4.zero);
            Shader.SetGlobalVector(EclipseRing, Vector4.zero);
            Shader.SetGlobalVector(EarthEclipse, Vector4.zero);
        }

        /// <summary>
        /// Updates the sky and Earth shader globals for the live <see cref="SunClock"/> state and smoothed local
        /// sunlight blend (0 = earthlit night / eclipse, 1 = look C sunlit). Allocation-free.
        /// </summary>
        public static void ApplyCycle(SkySettings sky, SkyCycleSettings cycle, SunClock clock, float sunlitBlend)
        {
            if (sky == null || cycle == null || clock == null)
            {
                throw new System.ArgumentNullException(sky == null ? nameof(sky)
                    : cycle == null ? nameof(cycle) : nameof(clock));
            }

            float sunlit = Mathf.Clamp01(sunlitBlend);
            float eclipse = Mathf.Clamp01(clock.EclipseFactor);

            Color skyTop = Color.Lerp(sky.SkyTop, cycle.SunlitSkyTop, sunlit).linear;
            Color horizonGlow = Color.Lerp(sky.HorizonGlow, cycle.SunlitHorizonGlow, sunlit).linear * (1f - eclipse * 0.55f);
            Color horizonBand = Color.Lerp(sky.HorizonBand, cycle.SunlitHorizonBand, sunlit).linear * (1f - eclipse * 0.45f);
            Shader.SetGlobalVector(SkyTop, WithAlpha(skyTop, sky.GradientExponent));
            Shader.SetGlobalVector(HorizonGlow, WithAlpha(horizonGlow, sky.HorizonGlowHeight));
            Shader.SetGlobalVector(HorizonBand, WithAlpha(horizonBand, sky.HorizonBandHeight));

            float starScale = Mathf.Lerp(Mathf.Lerp(1f, cycle.SunlitStarScale, sunlit), cycle.EclipseStarScale, eclipse);
            Shader.SetGlobalVector(StarParams,
                new Vector4(sky.StarDensity, sky.StarBrightness * starScale, sky.StarPowerLaw, sky.StarSize));

            Vector3 toEarth = sky.EarthDirection;
            Vector3 toSun = clock.SunDirection;
            float earthRadiusRad = sky.EarthDiameter * 0.5f * Mathf.Deg2Rad;
            float sunRadiusRad = cycle.SunDiameter * 0.5f * Mathf.Deg2Rad;

            // Keep a soft red-orange atmospheric ring while the Sun is behind or grazing Earth's limb.
            float separation = Mathf.Acos(Mathf.Clamp(Vector3.Dot(toSun, toEarth), -1f, 1f));
            float limbProximity = 1f - SmoothStep01(earthRadiusRad - sunRadiusRad,
                earthRadiusRad + sunRadiusRad * 3.5f, separation);
            float ringWeight = Mathf.Max(eclipse, limbProximity * 0.75f);

            float normalAtmoScale = 1f - ringWeight * 0.75f;
            Shader.SetGlobalVector(EarthHalo,
                WithAlpha(sky.EarthAtmosphere.linear * (sky.EarthHaloStrength * normalAtmoScale), sky.EarthHaloSize));
            Shader.SetGlobalVector(EarthLimb,
                WithAlpha(sky.EarthAtmosphere.linear * (sky.EarthLimb * normalAtmoScale), sky.EarthLimbWidth));

            // LofiEarth's visible hemisphere has normalWS pointing along +toEarth at disc centre (because axisZ = -toEarth
            // reflects triangle winding), so reflecting toSun along toEarth makes dot(normalWS, earthSun) = -1 when the
            // Sun is behind Earth (new Earth in eclipse) and +1 when the Sun is opposite Earth (full Earth at night),
            // while preserving the right/up crescent direction.
            Vector3 earthSun = (toSun - 2f * Vector3.Dot(toSun, toEarth) * toEarth).normalized;
            float nightBrightness = Mathf.Lerp(sky.EarthNightBrightness, sky.EarthNightBrightness * 0.25f, ringWeight);
            Shader.SetGlobalVector(EarthSun, WithAlpha(earthSun, nightBrightness));

            float nightSideFacing = 1f - SmoothStep01(0.0f, 0.25f, clock.EarthPhase);
            Color cityColor = cycle.EclipseCityLightColor.linear * (cycle.EclipseCityLightStrength * nightSideFacing);
            Shader.SetGlobalVector(EarthEclipse, WithAlpha(cityColor, ringWeight));

            Color ringColor = cycle.EclipseRingColor.linear * (cycle.EclipseRingStrength * ringWeight);
            Shader.SetGlobalVector(EclipseRing, WithAlpha(ringColor, ringWeight > 1e-4f ? cycle.EclipseRingWidth : 0f));

            Shader.SetGlobalVector(SunDirection, WithAlpha(toSun, sunRadiusRad));
            Shader.SetGlobalVector(SunColor,
                WithAlpha(cycle.SunDiscColor.linear * cycle.SunCoreIntensity, cycle.SunCoronaStrength));
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            if (Mathf.Abs(edge1 - edge0) < 1e-6f)
            {
                return x >= edge1 ? 1f : 0f;
            }

            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static Vector4 WithAlpha(Color color, float alpha)
        {
            return new Vector4(color.r, color.g, color.b, alpha);
        }

        private static Vector4 WithAlpha(Vector3 vector, float alpha)
        {
            return new Vector4(vector.x, vector.y, vector.z, alpha);
        }
    }
}
