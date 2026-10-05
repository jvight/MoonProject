using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Picks the palette swatch of one terrain triangle from its slope, height and region weights: rock on steep
    /// faces, shadow dust on crater walls, light dust on crater rims, and large soft seeded patches of light and mid
    /// dust on the floor (a small per-triangle dither frays their borders). Immutable and thread-safe.
    /// </summary>
    public sealed class TerrainPainter
    {
        private const uint PatchSalt = 0x632BE59Bu;

        // Tone lost per unit of steepness: one full step from light to mid dust.
        private const float PatchStep = 1f;

        private readonly TerrainPaintSettings _settings;
        private readonly GradientNoise _patchNoise;
        private readonly float _invPatchWavelength;
        private readonly float _rockCos;
        private readonly float _rimRockCos;

        public TerrainPainter(TerrainPaintSettings settings, int seed)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _patchNoise = new GradientNoise(Hashing.Mix((uint)seed ^ PatchSalt));
            _invPatchWavelength = 1f / settings.PatchWavelength;
            _rockCos = Mathf.Cos(settings.RockSlope * Mathf.Deg2Rad);
            _rimRockCos = Mathf.Cos(settings.RimRockSlope * Mathf.Deg2Rad);
        }

        /// <param name="normal">Unit face normal.</param>
        /// <param name="center">World-space triangle centre.</param>
        /// <param name="region">Averaged region weights of the triangle's corners (height unused).</param>
        /// <param name="hash">Per-triangle seeded hash.</param>
        public PaletteSwatch Pick(Vector3 normal, Vector3 center, SurfaceSample region, uint hash)
        {
            float dither = Hashing.ToSigned(hash) * _settings.Dither;
            bool onRim = region.RimZone >= _settings.RimZoneStart;
            if (normal.y <= _rockCos || (onRim && normal.y <= _rimRockCos))
            {
                float lift = (center.y - _settings.RockLightHeight) / _settings.RockHeightBlend;
                return lift + dither * 4f > 0f ? PaletteSwatch.RockLight : PaletteSwatch.RockDark;
            }

            float slope = Mathf.Acos(Mathf.Clamp(normal.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (region.CraterBowl > _settings.CraterShadow && slope > _settings.CraterWallSlope)
            {
                return PaletteSwatch.DustShadow;
            }

            if (region.CraterRim > _settings.CraterHighlight)
            {
                return PaletteSwatch.DustLight;
            }

            if (region.RimZone > _settings.HighlandZone)
            {
                return PaletteSwatch.DustShadow;
            }

            float steepness = Mathf.Max(0f, slope - _settings.DarkenSlope) / _settings.DarkenRange;
            float patch = _patchNoise.Fractal(center.x * _invPatchWavelength, center.z * _invPatchWavelength, 2, 2f,
                0.5f);
            float tone = patch + dither - steepness * PatchStep;
            if (tone > _settings.LightPatch)
            {
                return PaletteSwatch.DustLight;
            }

            return _settings.ShadowPatch > -1f && tone < _settings.ShadowPatch
                ? PaletteSwatch.DustShadow
                : PaletteSwatch.DustMid;
        }
    }
}
