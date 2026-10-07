using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Colours the terrain (sRGB vertex colours for the LofiTerrain shader). Rock on steep faces and the rim gets
    /// one flat palette tone per facet, two-toned by steepness, height, patches and facing. The dust is toned
    /// continuously from the palette's dust swatches by low-frequency patches, then shaded on crater walls and the
    /// highlands, lit on crater rims, cooled and darkened in Whispering Canyon and turned charcoal in its chasm; the
    /// light and shadow sides of dunes come from the earthlight itself. The dust tone depends only on the position
    /// (and the smooth region weights there), so every vertex shared by several triangles gets one colour: patches
    /// change value gently, never as paper-cut shapes or isolated facets (design ruling 9). Immutable and
    /// thread-safe.
    /// </summary>
    public sealed class TerrainPainter
    {
        private const uint PatchSalt = 0x632BE59Bu;
        private const uint DetailPatchSalt = 0x9E6C63D0u;

        // Half-widths of the soft bands around the crater and highland thresholds (in their 0..1 weights).
        private const float CraterBand = 0.15f;
        private const float HighlandBand = 0.15f;

        private readonly TerrainPaintSettings _settings;
        private readonly GradientNoise _patchNoise;
        private readonly GradientNoise _detailPatchNoise;
        private readonly float _invPatchWavelength;
        private readonly float _invDetailPatchWavelength;
        private readonly float _rockCos;
        private readonly float _rimRockCos;
        private readonly float _lightX;
        private readonly float _lightZ;
        private readonly Color _shadowDust;
        private readonly Color _midDust;
        private readonly Color _lightDust;
        private readonly Color _charcoal;
        private readonly Color32 _rockLight;
        private readonly Color32 _rockDark;

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
            _shadowDust = Palette.Get(PaletteSwatch.DustShadow);
            _midDust = Palette.Get(PaletteSwatch.DustMid);
            _lightDust = Palette.Get(PaletteSwatch.DustLight);
            _charcoal = Palette.Get(PaletteSwatch.Charcoal);
            _rockLight = Palette.Get(PaletteSwatch.RockLight);
            _rockDark = Palette.Get(PaletteSwatch.RockDark);
        }

        /// <summary>True when a face is rock (steep, or moderately steep on the rim): see <see cref="Rock"/>.</summary>
        /// <param name="faceNormal">Unit normal of the triangle itself.</param>
        /// <param name="region">Averaged region weights of the triangle's corners.</param>
        public bool IsRock(Vector3 faceNormal, SurfaceSample region)
        {
            bool onRim = region.RimZone >= _settings.RimZoneStart;
            return faceNormal.y <= _rockCos || (onRim && faceNormal.y <= _rimRockCos);
        }

        /// <summary>The one flat tone of a rock facet.</summary>
        /// <param name="faceNormal">Unit normal of the triangle itself.</param>
        /// <param name="center">World-space triangle centre.</param>
        /// <param name="hash">Per-triangle seeded hash.</param>
        public Color32 Rock(Vector3 faceNormal, Vector3 center, uint hash)
        {
            float slope = Mathf.Acos(Mathf.Clamp(faceNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
            float faceFacing = faceNormal.x * _lightX + faceNormal.z * _lightZ;
            float rock = (_settings.RockLightSlope - slope) / _settings.RockSlopeBlend
                + (center.y - _settings.RockLightHeight) / _settings.RockHeightBlend
                + Patch(center) * _settings.RockMottle + faceFacing * _settings.RockFacing
                + Hashing.ToSigned(hash) * _settings.RockDither;
            return rock > 0f ? _rockLight : _rockDark;
        }

        /// <summary>The dust colour at a vertex.</summary>
        /// <param name="position">World-space vertex position.</param>
        /// <param name="region">Region weights there (height unused).</param>
        public Color32 Ground(Vector3 position, SurfaceSample region)
        {
            float detail = _detailPatchNoise.Fractal(position.x * _invDetailPatchWavelength,
                position.z * _invDetailPatchWavelength, 2, 2f, 0.5f);
            float tone = Patch(position) * _settings.PatchStrength + detail * _settings.DetailPatchStrength
                + _settings.ToneBias;

            Color dust = tone < 0f
                ? Color.Lerp(_midDust, _shadowDust, SmoothMath.Smootherstep(0f, _settings.ShadowReach, -tone))
                : Color.Lerp(_midDust, _lightDust,
                    _settings.LightShare * SmoothMath.Smootherstep(0f, _settings.LightReach, tone));

            float wall = SmoothMath.Smootherstep(_settings.CraterShadow - CraterBand,
                    _settings.CraterShadow + CraterBand, region.CraterBowl)
                * (1f - SmoothMath.Smootherstep(_settings.CraterFloor - CraterBand, _settings.CraterFloor + CraterBand,
                    region.CraterBowl));
            dust = Color.Lerp(dust, _shadowDust, wall * _settings.CraterWallShade);
            dust = Color.Lerp(dust, _lightDust, SmoothMath.Smootherstep(0f, 1f, region.CraterRim)
                * _settings.CraterRimLight);
            float highland = SmoothMath.Smootherstep(_settings.HighlandZone - HighlandBand,
                _settings.HighlandZone + HighlandBand, region.RimZone);
            dust = Color.Lerp(dust, _shadowDust, highland * _settings.HighlandShade);

            float canyonTone = _settings.CanyonMidShare * Mathf.Clamp01(0.5f + tone * 0.5f);
            Color canyon = Color.Lerp(_shadowDust, _midDust, canyonTone);
            dust = Color.Lerp(dust, canyon, region.CanyonFloor);
            dust = Color.Lerp(dust, _charcoal, SmoothMath.Smootherstep(0f, _settings.ChasmDark, region.Chasm));
            return dust;
        }

        private float Patch(Vector3 position)
        {
            return _patchNoise.Fractal(position.x * _invPatchWavelength, position.z * _invPatchWavelength, 2, 2f, 0.5f);
        }
    }
}
