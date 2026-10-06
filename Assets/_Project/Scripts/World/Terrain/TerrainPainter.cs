using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Picks the palette swatch of one terrain triangle. Rock on steep faces and the rim, two-toned per face by
    /// steepness, height, patches and facing; shaded crater walls and lit crater rims; and floor dust toned only by
    /// low-frequency patches and the smoothed tilt of the ground toward the earthlight, so colour changes come in
    /// patches and dune sides, never as isolated facets (design ruling 9). Immutable and thread-safe.
    /// </summary>
    public sealed class TerrainPainter
    {
        private const uint PatchSalt = 0x632BE59Bu;
        private const uint DetailPatchSalt = 0x9E6C63D0u;

        private readonly TerrainPaintSettings _settings;
        private readonly GradientNoise _patchNoise;
        private readonly GradientNoise _detailPatchNoise;
        private readonly float _invPatchWavelength;
        private readonly float _invDetailPatchWavelength;
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
            _detailPatchNoise = new GradientNoise(Hashing.Mix((uint)seed ^ DetailPatchSalt));
            _invPatchWavelength = 1f / settings.PatchWavelength;
            _invDetailPatchWavelength = 1f / settings.DetailPatchWavelength;
            _rockCos = Mathf.Cos(settings.RockSlope * Mathf.Deg2Rad);
            _rimRockCos = Mathf.Cos(settings.RimRockSlope * Mathf.Deg2Rad);
            Vector2 horizontal = new Vector2(lightDirection.x, lightDirection.z);
            if (horizontal.sqrMagnitude < 1e-6f)
            {
                throw new ArgumentException("The earthlight must not shine straight down.", nameof(lightDirection));
            }

            horizontal.Normalize();
            _lightX = horizontal.x;
            _lightZ = horizontal.y;
            _leeFacing = -Mathf.Sin(settings.LeeTilt * Mathf.Deg2Rad);
        }

        /// <summary>Distance (metres) over which the ground normal given to <see cref="Pick"/> is smoothed.</summary>
        public float TiltSmoothing => _settings.TiltSmoothing;

        /// <param name="faceNormal">Unit normal of the triangle itself.</param>
        /// <param name="groundNormal">Ground normal around it, smoothed over <see cref="TiltSmoothing"/>.</param>
        /// <param name="center">World-space triangle centre.</param>
        /// <param name="region">Averaged region weights of the triangle's corners (height unused).</param>
        /// <param name="hash">Per-triangle seeded hash.</param>
        public PaletteSwatch Pick(Vector3 faceNormal, Vector3 groundNormal, Vector3 center, SurfaceSample region,
            uint hash)
        {
            float nudge = Hashing.ToSigned(hash);
            float patch = _patchNoise.Fractal(center.x * _invPatchWavelength, center.z * _invPatchWavelength, 2, 2f,
                0.5f);
            bool onRim = region.RimZone >= _settings.RimZoneStart;
            if (faceNormal.y <= _rockCos || (onRim && faceNormal.y <= _rimRockCos))
            {
                float slope = Mathf.Acos(Mathf.Clamp(faceNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
                float faceFacing = faceNormal.x * _lightX + faceNormal.z * _lightZ;
                float rock = (_settings.RockLightSlope - slope) / _settings.RockSlopeBlend
                    + (center.y - _settings.RockLightHeight) / _settings.RockHeightBlend
                    + patch * _settings.RockMottle + faceFacing * _settings.RockFacing + nudge * _settings.RockDither;
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

            float detail = _detailPatchNoise.Fractal(center.x * _invDetailPatchWavelength,
                center.z * _invDetailPatchWavelength, 2, 2f, 0.5f);
            float facing = groundNormal.x * _lightX + groundNormal.z * _lightZ;
            float tone = patch * _settings.PatchStrength + detail * _settings.DetailPatchStrength
                + facing * _settings.FacingStrength + nudge * _settings.Dither;
            if (tone > _settings.LightTone)
            {
                return PaletteSwatch.DustLight;
            }

            bool leeShade = facing < _leeFacing && tone < _settings.ShadowTone;
            return leeShade ? PaletteSwatch.DustShadow : PaletteSwatch.DustMid;
        }
    }
}
