using UnityEngine;
using MoonProject.Art;

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
        private static readonly int EarthOcean = Shader.PropertyToID("_MoonEarthOcean");
        private static readonly int EarthLand = Shader.PropertyToID("_MoonEarthLand");
        private static readonly int EarthCloud = Shader.PropertyToID("_MoonEarthCloud");
        private static readonly int ShootingStars = Shader.PropertyToID("_MoonShootingStars");
        private static readonly int SunDirection = Shader.PropertyToID("_MoonSunDirection");
        private static readonly int SunColor = Shader.PropertyToID("_MoonSunColor");
        private static readonly int EclipseRing = Shader.PropertyToID("_MoonEclipseRing");
        private static readonly int EarthAurora = Shader.PropertyToID("_MoonEarthAurora");
        private static readonly int EarthAuroraBand = Shader.PropertyToID("_MoonEarthAuroraBand");
        private static readonly int EarthVariant = Shader.PropertyToID("_MoonEarthVariant");
        private static readonly int SunlitGround = Shader.PropertyToID("_MoonSunlitGround");

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
            Shader.SetGlobalVector(EarthOcean, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.EarthOcean, sky), 1f));
            Shader.SetGlobalVector(EarthLand, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.EarthLand, sky), 1f));
            Shader.SetGlobalVector(EarthCloud, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.Cream, sky), 1f));
            Color auroraColor = sky.EarthAuroraColor.linear * sky.EarthAuroraIntensity;
            Shader.SetGlobalVector(EarthAurora, WithAlpha(auroraColor, sky.EarthAuroraDriftSpeed));
            Shader.SetGlobalVector(EarthAuroraBand,
                new Vector4(sky.EarthAuroraLatitude, sky.EarthAuroraWidth, 0f, 0f));
            Shader.SetGlobalFloat(EarthVariant, (float)sky.EarthVariant);
            Shader.SetGlobalVector(ShootingStars,
                new Vector4(sky.ShootingStarPeriod, sky.ShootingStarDuration, sky.ShootingStarBrightness, 0f));
            Shader.SetGlobalVector(SunDirection, Vector4.zero);
            Shader.SetGlobalVector(SunColor, Vector4.zero);
            Shader.SetGlobalVector(EclipseRing, Vector4.zero);
            Shader.SetGlobalVector(SunlitGround, Vector4.zero);
        }

        /// <summary>
        /// Pure world-space unit direction from Earth toward the Sun as pushed to <c>_MoonEarthSun.xyz</c> to light
        /// Earth's visible hemisphere (whose outward normal at disc centre is <c>-sky.EarthDirection</c>).
        /// <list type="bullet">
        /// <item>When the Sun is at Earth's bearing (new Earth / eclipse), points along <c>+EarthDirection</c> (away
        /// from the Moon, so <c>dot(frontNormal, litDir) = -1</c>).</item>
        /// <item>When the Sun is 90 degrees clockwise of Earth (first quarter, <c>EarthPhase = 0.5</c>), points along
        /// Earth's right limb (<c>+right</c>).</item>
        /// <item>When the Sun is 180 degrees opposite Earth (full Earth, <c>EarthPhase = 1</c>), points along
        /// <c>-EarthDirection</c> (toward the Moon, so <c>dot(frontNormal, litDir) = +1</c>).</item>
        /// </list>
        /// </summary>
        public static Vector3 EarthLitDirection(SkySettings sky, float sunBearing, float sunElevation = 0f)
        {
            if (sky == null)
            {
                throw new System.ArgumentNullException(nameof(sky));
            }

            Vector3 toEarth = sky.EarthDirection;
            Vector3 right = Vector3.Cross(Vector3.up, toEarth).normalized;
            Vector3 up = Vector3.Cross(toEarth, right).normalized;

            float elongationRad = Mathf.DeltaAngle(sky.EarthBearing, sunBearing) * Mathf.Deg2Rad;
            float cosElongation = Mathf.Cos(elongationRad);
            float sinElongation = Mathf.Sin(elongationRad);
            float pitchOffsetRad = (sunElevation - sky.EarthElevation) * Mathf.Deg2Rad * Mathf.Max(0f, cosElongation);
            float cosPitch = Mathf.Cos(pitchOffsetRad);
            float sinPitch = Mathf.Sin(pitchOffsetRad);

            return (right * (sinElongation * cosPitch)
                + up * sinPitch
                + toEarth * (cosElongation * cosPitch)).normalized;
        }

        /// <summary> Overload of <see cref="EarthLitDirection(SkySettings, float, float)"/> for a live <see cref="SunClock"/>.</summary>
        public static Vector3 EarthLitDirection(SkySettings sky, SunClock clock)
        {
            if (clock == null)
            {
                throw new System.ArgumentNullException(nameof(clock));
            }

            return EarthLitDirection(sky, clock.Bearing, clock.Elevation);
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
            float skyDayBlend = WorldAtmosphere.DayBlend(cycle, sunlit);
            float eclipse = Mathf.Clamp01(clock.EclipseFactor);

            Color skyTop = Color.Lerp(sky.SkyTop, cycle.SunlitSkyTop, skyDayBlend).linear;
            Color horizonGlow = Color.Lerp(sky.HorizonGlow, cycle.SunlitHorizonGlow, skyDayBlend).linear * (1f - eclipse * 0.55f);
            Color horizonBand = Color.Lerp(sky.HorizonBand, cycle.SunlitHorizonBand, skyDayBlend).linear * (1f - eclipse * 0.45f);
            Shader.SetGlobalVector(SkyTop, WithAlpha(skyTop, sky.GradientExponent));
            Shader.SetGlobalVector(HorizonGlow, WithAlpha(horizonGlow, sky.HorizonGlowHeight));
            Shader.SetGlobalVector(HorizonBand, WithAlpha(horizonBand, sky.HorizonBandHeight));

            // While sunlit ground fills the frame, the eye adapts so stars and the Milky Way dim strongly; as the Sun
            // drops through Sunset toward the rim, eye adaptation relaxes and the stars return.
            float elevationAdapt = SmoothStep01(2f, cycle.TwilightElevation, clock.Elevation);
            float dayAdaptation = skyDayBlend * Mathf.Lerp(0.45f, 1f, elevationAdapt);
            float starScale = Mathf.Lerp(Mathf.Lerp(1f, cycle.SunlitStarScale, dayAdaptation), cycle.EclipseStarScale, eclipse);
            Shader.SetGlobalVector(StarParams,
                new Vector4(sky.StarDensity, sky.StarBrightness * starScale, sky.StarPowerLaw, sky.StarSize));

            float milkyWayScale = Mathf.Lerp(1f, cycle.SunlitMilkyWayScale, dayAdaptation) * (1f - eclipse * 0.25f);
            Shader.SetGlobalVector(MilkyWayAxis, WithAlpha(sky.MilkyWayAxis, sky.MilkyWayIntensity * milkyWayScale));

            Shader.SetGlobalVector(EarthOcean, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.EarthOcean, sky), 1f));
            Shader.SetGlobalVector(EarthLand, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.EarthLand, sky), 1f));
            Shader.SetGlobalVector(EarthCloud, WithAlpha(EarthMeshBuilder.FaceColor(PaletteSwatch.Cream, sky), 1f));

            Vector3 toEarth = sky.EarthDirection;
            Vector3 toSun = clock.SunDirection;
            float earthRadiusRad = sky.EarthDiameter * 0.5f * Mathf.Deg2Rad;
            float sunRadiusRad = cycle.SunDiameter * 0.5f * Mathf.Deg2Rad;

            // Keep a soft red-orange atmospheric ring while the Sun is behind or grazing Earth's limb.
            float separation = Mathf.Acos(Mathf.Clamp(Vector3.Dot(toSun, toEarth), -1f, 1f));
            float limbProximity = 1f - SmoothStep01(earthRadiusRad - sunRadiusRad,
                earthRadiusRad + sunRadiusRad * 3.5f, separation);
            float ringWeight = Mathf.Max(eclipse, limbProximity * 0.75f);

            float phaseHaloScale = Mathf.Lerp(0.18f, 1f, SmoothStep01(0.02f, 0.7f, clock.EarthPhase))
                * (1f - ringWeight * 0.85f);
            float normalLimbScale = 1f - ringWeight * 0.85f;
            Shader.SetGlobalVector(EarthHalo,
                WithAlpha(sky.EarthAtmosphere.linear * (sky.EarthHaloStrength * phaseHaloScale), sky.EarthHaloSize));
            Shader.SetGlobalVector(EarthLimb,
                WithAlpha(sky.EarthAtmosphere.linear * (sky.EarthLimb * normalLimbScale), sky.EarthLimbWidth));

            Vector3 earthSun = EarthLitDirection(sky, clock);
            float nightBrightness = Mathf.Lerp(sky.EarthNightBrightness, sky.EarthNightBrightness * 0.35f, ringWeight);
            Shader.SetGlobalVector(EarthSun, WithAlpha(earthSun, nightBrightness));

            float spinSpeed = Mathf.PI * 2f / sky.EarthSpinPeriod;
            Shader.SetGlobalVector(EarthGlow,
                new Vector4(sky.EarthGlow, spinSpeed, clock.TimeSeconds * spinSpeed, ringWeight));

            Color auroraColor = sky.EarthAuroraColor.linear * sky.EarthAuroraIntensity;
            Shader.SetGlobalVector(EarthAurora, WithAlpha(auroraColor, sky.EarthAuroraDriftSpeed));
            Shader.SetGlobalVector(EarthAuroraBand,
                new Vector4(sky.EarthAuroraLatitude, sky.EarthAuroraWidth,
                    clock.TimeSeconds * sky.EarthAuroraDriftSpeed, 0f));
            Shader.SetGlobalFloat(EarthVariant, (float)sky.EarthVariant);

            Color ringColor = cycle.EclipseRingColor.linear * (cycle.EclipseRingStrength * ringWeight);
            Shader.SetGlobalVector(EclipseRing, WithAlpha(ringColor, ringWeight > 1e-4f ? cycle.EclipseRingWidth : 0f));

            Shader.SetGlobalVector(SunDirection, WithAlpha(toSun, sunRadiusRad));
            Shader.SetGlobalVector(SunColor,
                WithAlpha(cycle.SunDiscColor.linear * cycle.SunCoreIntensity, cycle.SunCoronaStrength));
            Shader.SetGlobalVector(SunlitGround, new Vector4(skyDayBlend, 0f, 0f, 0f));
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

