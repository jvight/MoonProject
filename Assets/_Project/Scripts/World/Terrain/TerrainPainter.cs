using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Picks the palette swatch of one terrain triangle. Rock on steep faces and the rim, two-toned by steepness,
    /// height, patches and facing; shaded crater walls and lit crater rims; and floor dust toned by large seeded
    /// patches plus the facet's tilt toward the earthlight, so lit and lee facets differ by a whole swatch and the
    /// low-poly relief reads even on gentle ground. A small per-triangle dither frays every border. Immutable and
    /// thread-safe.
    /// </summary>
    public sealed class TerrainPainter
    {
        private const uint PatchSalt = 0x632BE59Bu;

        // Rock uses a fraction of the dust dither: enough to interleave the two rock tones along their border.
        private const float RockDitherScale = 1.4f;

        private readonly TerrainPaintSettings _settings;
        private readonly GradientNoise _patchNoise;
        private readonly float _invPatchWavelength;
        private readonly float _rockCos;
        private readonly float _rimRockCos;
        private readonly float _lightX;
        private readonly float _lightZ;
        private readonly float _leeFacing;

        /// <param name="lightDirection">Direction toward the earthlight (only its horizontal part is used).</param>
        public TerrainPainter(TerrainPaintSettings settings, int seed, Vector3 lightDirection)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _patchNoise = new GradientNoise(Hashing.Mix((uint)seed ^ PatchSalt));
            _invPatchWavelength = 1f / settings.PatchWavelength;
            _rockCos = Mathf.Cos(settings.RockSlope * Mathf.Deg2Rad);
            _rimRockCos = Mathf.Cos(settings.RimRockSlope * Mathf.Deg2Rad);
            Vector2 horizontal = new Vector2(lightDirection.x, lightDirection.z);
            if (horizontal.sqrMagnitude < 1e-6f)
            {
                throw new ArgumentException("The earthlight must not shine straight down.", nameof(lightDirection));
            }

            horizontal.Normalize();
            _leeFacing = -Mathf.Sin(settings.LeeTilt * Mathf.Deg2Rad);
            _lightX = horizontal.x;
            _lightZ = horizontal.y;
        }

        /// <param name="normal">Unit face normal.</param>
        /// <param name="center">World-space triangle centre.</param>
        /// <param name="region">Averaged region weights of the triangle's corners (height unused).</param>
        /// <param name="hash">Per-triangle seeded hash.</param>
        public PaletteSwatch Pick(Vector3 normal, Vector3 center, SurfaceSample region, uint hash)
        {
            float dither = Hashing.ToSigned(hash) * _settings.Dither;
            float patch = _patchNoise.Fractal(center.x * _invPatchWavelength, center.z * _invPatchWavelength, 2, 2f,
                0.5f);
            float facing = normal.x * _lightX + normal.z * _lightZ;
            bool onRim = region.RimZone >= _settings.RimZoneStart;
            if (normal.y <= _rockCos || (onRim && normal.y <= _rimRockCos))
            {
                float slope = Mathf.Acos(Mathf.Clamp(normal.y, -1f, 1f)) * Mathf.Rad2Deg;
                float rock = (_settings.RockLightSlope - slope) / _settings.RockSlopeBlend
                    + (center.y - _settings.RockLightHeight) / _settings.RockHeightBlend
                    + patch * _settings.RockMottle + facing * _settings.RockFacing + dither * RockDitherScale;
                return rock > 0f ? PaletteSwatch.RockLight : PaletteSwatch.RockDark;
            }

            if (region.CraterBowl > _settings.CraterShadow && region.CraterBowl < _settings.CraterFloor)
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

            float tone = patch * _settings.PatchStrength + facing * _settings.FacingStrength + dither;
            if (tone > _settings.LightTone)
            {
                return PaletteSwatch.DustLight;
            }

            bool leeShade = facing < _leeFacing && tone < _settings.ShadowTone;
            return leeShade ? PaletteSwatch.DustShadow : PaletteSwatch.DustMid;
        }
    }
}
