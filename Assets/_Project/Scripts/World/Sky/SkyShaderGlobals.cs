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
        private static readonly int StarParams = Shader.PropertyToID("_MoonStarParams");
        private static readonly int MilkyWayAxis = Shader.PropertyToID("_MoonMilkyWayAxis");
        private static readonly int MilkyWayColor = Shader.PropertyToID("_MoonMilkyWayColor");
        private static readonly int EarthDirection = Shader.PropertyToID("_MoonEarthDirection");
        private static readonly int EarthHalo = Shader.PropertyToID("_MoonEarthHalo");
        private static readonly int EarthAxisX = Shader.PropertyToID("_MoonEarthAxisX");
        private static readonly int EarthAxisY = Shader.PropertyToID("_MoonEarthAxisY");
        private static readonly int EarthAxisZ = Shader.PropertyToID("_MoonEarthAxisZ");
        private static readonly int EarthSun = Shader.PropertyToID("_MoonEarthSun");
        private static readonly int EarthGlow = Shader.PropertyToID("_MoonEarthGlow");
        private static readonly int EarthAtmosphere = Shader.PropertyToID("_MoonEarthAtmosphere");
        private static readonly int ShootingStars = Shader.PropertyToID("_MoonShootingStars");

        public static void Apply(SkySettings sky)
        {
            if (sky == null)
            {
                throw new System.ArgumentNullException(nameof(sky));
            }

            Shader.SetGlobalVector(SkyTop, WithAlpha(sky.SkyTop.linear, sky.GradientExponent));
            Shader.SetGlobalVector(HorizonGlow, WithAlpha(sky.HorizonGlow.linear, sky.HorizonGlowHeight));
            Shader.SetGlobalVector(StarParams,
                new Vector4(sky.StarDensity, sky.StarBrightness, sky.TwinkleSpeed, sky.StarSize));
            Shader.SetGlobalVector(MilkyWayAxis, WithAlpha(sky.MilkyWayAxis, sky.MilkyWayIntensity));
            Shader.SetGlobalVector(MilkyWayColor, WithAlpha(sky.MilkyWayColor.linear, sky.MilkyWayWidth));

            Vector3 toEarth = sky.EarthDirection;
            float radius = sky.EarthDiameter * 0.5f * Mathf.Deg2Rad;
            Shader.SetGlobalVector(EarthDirection, WithAlpha(toEarth, radius));
            Shader.SetGlobalVector(EarthHalo, WithAlpha(sky.EarthAtmosphere.linear * 0.35f, sky.EarthHaloSize));

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
            Shader.SetGlobalVector(EarthAtmosphere, WithAlpha(sky.EarthAtmosphere.linear, EarthRimExponent));
            Shader.SetGlobalVector(ShootingStars,
                new Vector4(sky.ShootingStarPeriod, sky.ShootingStarDuration, sky.ShootingStarBrightness, 0f));
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
