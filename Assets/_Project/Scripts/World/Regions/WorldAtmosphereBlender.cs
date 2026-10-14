using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Blended atmosphere, fog, haze, desaturation, sky gradient, and far clip values at a world position.
    /// </summary>
    public readonly struct BlendedAtmosphere
    {
        public Color LightColor { get; }
        public float LightIntensity { get; }
        public Color FillColor { get; }
        public float FillIntensity { get; }
        public Color AmbientSky { get; }
        public Color AmbientEquator { get; }
        public Color AmbientGround { get; }
        public Color FogColor { get; }
        public float FogDensity { get; }
        public Color HazeColor { get; }
        public float HazeDensity { get; }
        public float HazeHeight { get; }
        public float HazeBase { get; }
        public float HazeMax { get; }
        public float DesaturateStart { get; }
        public float DesaturateEnd { get; }
        public float DesaturateAmount { get; }
        public Color SkyTop { get; }
        public Color HorizonGlow { get; }
        public float HorizonGlowHeight { get; }
        public Color HorizonBand { get; }
        public float HorizonBandHeight { get; }
        public float GradientExponent { get; }
        public float FarClipPlane { get; }

        public BlendedAtmosphere(
            Color lightColor,
            float lightIntensity,
            Color fillColor,
            float fillIntensity,
            Color ambientSky,
            Color ambientEquator,
            Color ambientGround,
            Color fogColor,
            float fogDensity,
            Color hazeColor,
            float hazeDensity,
            float hazeHeight,
            float hazeBase,
            float hazeMax,
            float desaturateStart,
            float desaturateEnd,
            float desaturateAmount,
            Color skyTop,
            Color horizonGlow,
            float horizonGlowHeight,
            Color horizonBand,
            float horizonBandHeight,
            float gradientExponent,
            float farClipPlane)
        {
            LightColor = lightColor;
            LightIntensity = lightIntensity;
            FillColor = fillColor;
            FillIntensity = fillIntensity;
            AmbientSky = ambientSky;
            AmbientEquator = ambientEquator;
            AmbientGround = ambientGround;
            FogColor = fogColor;
            FogDensity = fogDensity;
            HazeColor = hazeColor;
            HazeDensity = hazeDensity;
            HazeHeight = hazeHeight;
            HazeBase = hazeBase;
            HazeMax = hazeMax;
            DesaturateStart = desaturateStart;
            DesaturateEnd = desaturateEnd;
            DesaturateAmount = desaturateAmount;
            SkyTop = skyTop;
            HorizonGlow = horizonGlow;
            HorizonGlowHeight = horizonGlowHeight;
            HorizonBand = horizonBand;
            HorizonBandHeight = horizonBandHeight;
            GradientExponent = gradientExponent;
            FarClipPlane = farClipPlane;
        }

        /// <summary>Builds a snapshot from <see cref="AtmosphereSettings"/> and <see cref="SkySettings"/>.</summary>
        public static BlendedAtmosphere FromSettings(AtmosphereSettings atmosphere, SkySettings sky,
            float farClipPlane = RegionAtmosphereProfile.DefaultFarClipPlane)
        {
            return new BlendedAtmosphere(
                atmosphere.LightColor,
                atmosphere.LightIntensity,
                atmosphere.FillColor,
                atmosphere.FillIntensity,
                atmosphere.AmbientSky,
                atmosphere.AmbientEquator,
                atmosphere.AmbientGround,
                atmosphere.FogColor,
                atmosphere.FogDensity,
                atmosphere.HazeColor,
                atmosphere.HazeDensity,
                atmosphere.HazeHeight,
                atmosphere.HazeBase,
                atmosphere.HazeMax,
                atmosphere.DesaturateStart,
                atmosphere.DesaturateEnd,
                atmosphere.DesaturateAmount,
                sky.SkyTop,
                sky.HorizonGlow,
                sky.HorizonGlowHeight,
                sky.HorizonBand,
                sky.HorizonBandHeight,
                sky.GradientExponent,
                farClipPlane);
        }

        /// <summary>Builds a snapshot directly from a single <see cref="RegionAtmosphereProfile"/>.</summary>
        public static BlendedAtmosphere FromProfile(RegionAtmosphereProfile profile)
        {
            return new BlendedAtmosphere(
                profile.LightColor,
                profile.LightIntensity,
                profile.FillColor,
                profile.FillIntensity,
                profile.AmbientSky,
                profile.AmbientEquator,
                profile.AmbientGround,
                profile.FogColor,
                profile.FogDensity,
                profile.HazeColor,
                profile.HazeDensity,
                profile.HazeHeight,
                profile.HazeBase,
                profile.HazeMax,
                profile.DesaturateStart,
                profile.DesaturateEnd,
                profile.DesaturateAmount,
                profile.SkyTop,
                profile.HorizonGlow,
                profile.HorizonGlowHeight,
                profile.HorizonBand,
                profile.HorizonBandHeight,
                profile.GradientExponent,
                profile.FarClipPlane);
        }

        /// <summary>Linearly interpolates two atmosphere snapshots by <paramref name="t"/> in <c>[0, 1]</c>.</summary>
        public static BlendedAtmosphere Lerp(in BlendedAtmosphere a, in BlendedAtmosphere b, float t)
        {
            if (t <= 0f)
            {
                return a;
            }

            if (t >= 1f)
            {
                return b;
            }

            return new BlendedAtmosphere(
                Color.LerpUnclamped(a.LightColor, b.LightColor, t),
                Mathf.LerpUnclamped(a.LightIntensity, b.LightIntensity, t),
                Color.LerpUnclamped(a.FillColor, b.FillColor, t),
                Mathf.LerpUnclamped(a.FillIntensity, b.FillIntensity, t),
                Color.LerpUnclamped(a.AmbientSky, b.AmbientSky, t),
                Color.LerpUnclamped(a.AmbientEquator, b.AmbientEquator, t),
                Color.LerpUnclamped(a.AmbientGround, b.AmbientGround, t),
                Color.LerpUnclamped(a.FogColor, b.FogColor, t),
                Mathf.LerpUnclamped(a.FogDensity, b.FogDensity, t),
                Color.LerpUnclamped(a.HazeColor, b.HazeColor, t),
                Mathf.LerpUnclamped(a.HazeDensity, b.HazeDensity, t),
                Mathf.LerpUnclamped(a.HazeHeight, b.HazeHeight, t),
                Mathf.LerpUnclamped(a.HazeBase, b.HazeBase, t),
                Mathf.LerpUnclamped(a.HazeMax, b.HazeMax, t),
                Mathf.LerpUnclamped(a.DesaturateStart, b.DesaturateStart, t),
                Mathf.LerpUnclamped(a.DesaturateEnd, b.DesaturateEnd, t),
                Mathf.LerpUnclamped(a.DesaturateAmount, b.DesaturateAmount, t),
                Color.LerpUnclamped(a.SkyTop, b.SkyTop, t),
                Color.LerpUnclamped(a.HorizonGlow, b.HorizonGlow, t),
                Mathf.LerpUnclamped(a.HorizonGlowHeight, b.HorizonGlowHeight, t),
                Color.LerpUnclamped(a.HorizonBand, b.HorizonBand, t),
                Mathf.LerpUnclamped(a.HorizonBandHeight, b.HorizonBandHeight, t),
                Mathf.LerpUnclamped(a.GradientExponent, b.GradientExponent, t),
                Mathf.LerpUnclamped(a.FarClipPlane, b.FarClipPlane, t));
        }

        public bool ApproximatelyEquals(in BlendedAtmosphere other)
        {
            const float eps = 1e-6f;
            return Mathf.Abs(LightIntensity - other.LightIntensity) <= eps
                && Mathf.Abs(FillIntensity - other.FillIntensity) <= eps
                && Mathf.Abs(FogDensity - other.FogDensity) <= eps
                && Mathf.Abs(HazeDensity - other.HazeDensity) <= eps
                && Mathf.Abs(HazeHeight - other.HazeHeight) <= eps
                && Mathf.Abs(HazeBase - other.HazeBase) <= eps
                && Mathf.Abs(HazeMax - other.HazeMax) <= eps
                && Mathf.Abs(DesaturateStart - other.DesaturateStart) <= eps
                && Mathf.Abs(DesaturateEnd - other.DesaturateEnd) <= eps
                && Mathf.Abs(DesaturateAmount - other.DesaturateAmount) <= eps
                && Mathf.Abs(HorizonGlowHeight - other.HorizonGlowHeight) <= eps
                && Mathf.Abs(HorizonBandHeight - other.HorizonBandHeight) <= eps
                && Mathf.Abs(GradientExponent - other.GradientExponent) <= eps
                && Mathf.Abs(FarClipPlane - other.FarClipPlane) <= eps
                && SameColor(LightColor, other.LightColor)
                && SameColor(FillColor, other.FillColor)
                && SameColor(AmbientSky, other.AmbientSky)
                && SameColor(AmbientEquator, other.AmbientEquator)
                && SameColor(AmbientGround, other.AmbientGround)
                && SameColor(FogColor, other.FogColor)
                && SameColor(HazeColor, other.HazeColor)
                && SameColor(SkyTop, other.SkyTop)
                && SameColor(HorizonGlow, other.HorizonGlow)
                && SameColor(HorizonBand, other.HorizonBand);
        }

        public bool HasSameAmbient(in BlendedAtmosphere other)
        {
            return SameColor(AmbientSky, other.AmbientSky)
                && SameColor(AmbientEquator, other.AmbientEquator)
                && SameColor(AmbientGround, other.AmbientGround);
        }

        private static bool SameColor(Color a, Color b)
        {
            const float eps = 1e-6f;
            return Mathf.Abs(a.r - b.r) <= eps
                && Mathf.Abs(a.g - b.g) <= eps
                && Mathf.Abs(a.b - b.b) <= eps
                && Mathf.Abs(a.a - b.a) <= eps;
        }
    }

    /// <summary>
    /// Evaluates blended <see cref="RegionAtmosphereProfile"/> values across region seams and applies them through
    /// <see cref="WorldAtmosphere"/> and <see cref="SkyShaderGlobals"/> (the single source of truth for scene lighting,
    /// fog, haze, and sky gradient). Allocation-free per frame.
    /// </summary>
    public sealed class WorldAtmosphereBlender
    {
        private readonly WorldRegions _regions;
        private readonly Light _earthlight;
        private readonly Light _fill;
        private BlendedAtmosphere _lastApplied;
        private bool _hasApplied;

        public WorldAtmosphereBlender(WorldRegions regions, Light earthlight = null, Light fill = null)
        {
            _regions = regions ?? throw new ArgumentNullException(nameof(regions));
            _earthlight = earthlight;
            _fill = fill;
        }

        /// <summary>Computes the blended atmosphere snapshot at <paramref name="position"/> without allocations.</summary>
        public BlendedAtmosphere BlendAt(Vector3 position)
        {
            int count = _regions.SlotCount;
            Span<float> weights = stackalloc float[count];
            _regions.EvaluateWeights(position, weights);

            // Fast path when a single region holds 100% weight.
            for (int i = 0; i < count; i++)
            {
                if (weights[i] >= 1f)
                {
                    return BlendedAtmosphere.FromProfile(_regions.ProfileAt(i));
                }
            }

            BlendedAtmosphere result = default;
            float accumulated = 0f;
            for (int i = 0; i < count; i++)
            {
                float weight = weights[i];
                if (weight <= 0f)
                {
                    continue;
                }

                BlendedAtmosphere profile = BlendedAtmosphere.FromProfile(_regions.ProfileAt(i));
                if (accumulated <= 0f)
                {
                    result = profile;
                    accumulated = weight;
                }
                else
                {
                    accumulated += weight;
                    result = BlendedAtmosphere.Lerp(in result, in profile, weight / accumulated);
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates and applies the blended atmosphere at <paramref name="position"/> through
        /// <see cref="WorldAtmosphere"/> and <see cref="SkyShaderGlobals"/>.
        /// </summary>
        public void ApplyAt(Vector3 position, bool force = false)
        {
            BlendedAtmosphere blended = BlendAt(position);
            if (!force && _hasApplied && blended.ApproximatelyEquals(in _lastApplied))
            {
                return;
            }

            bool updateAmbient = force || !_hasApplied || !blended.HasSameAmbient(in _lastApplied);
            _lastApplied = blended;
            _hasApplied = true;

            if (_earthlight != null && _fill != null)
            {
                WorldAtmosphere.ApplyBlended(in blended, _earthlight, _fill, updateAmbient);
            }

            SkyShaderGlobals.ApplyGradient(
                blended.SkyTop,
                blended.GradientExponent,
                blended.HorizonGlow,
                blended.HorizonGlowHeight,
                blended.HorizonBand,
                blended.HorizonBandHeight);
        }
    }
}
