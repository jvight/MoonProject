using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How the ground is coloured. Steep faces and the rim are rock, one flat tone per facet. Everywhere else the
    /// dust is toned continuously, vertex by vertex: low-frequency patches move it between the shadow and light
    /// dust, crater walls shade, crater rims catch the light and the canyon runs cooler and darker (dune sides get
    /// their light and shadow from the earthlight itself). Being continuous, large patches change value gently,
    /// never show paper-cut edges, and no single facet can stand out (design ruling 9).
    /// </summary>
    [Serializable]
    public sealed class TerrainPaintSettings
    {
        [Header("Rock")]
        [Tooltip("Any face steeper than this (degrees) is rock.")]
        [Range(10f, 80f)]
        [SerializeField] private float _rockSlope = 32f;

        [Tooltip("On the rim (see rim zone), faces steeper than this (degrees) are rock.")]
        [Range(5f, 80f)]
        [SerializeField] private float _rimRockSlope = 22f;

        [Tooltip("Rim zone (0 = floor, 1 = crest) from which the lower rim rock slope applies.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rimZoneStart = 0.3f;

        [Tooltip("Rock faces gentler than this (degrees) lean to the light rock swatch, steeper ones to the dark " +
            "one, like the two-tone rocks of the art kit.")]
        [Range(10f, 80f)]
        [SerializeField] private float _rockLightSlope = 42f;

        [Tooltip("Slope range (degrees) over which rock fades from light to dark.")]
        [Range(1f, 60f)]
        [SerializeField] private float _rockSlopeBlend = 14f;

        [Tooltip("Rock above this height (metres) leans to the light rock swatch, below it to the dark one.")]
        [Range(0f, 200f)]
        [SerializeField] private float _rockLightHeight = 70f;

        [Tooltip("Height range (metres) over which rock fades from dark to light.")]
        [Range(1f, 200f)]
        [SerializeField] private float _rockHeightBlend = 80f;

        [Tooltip("How much the large patches mottle the rock between light and dark.")]
        [Range(0f, 2f)]
        [SerializeField] private float _rockMottle = 0.6f;

        [Tooltip("How much facing the earthlight lightens rock faces.")]
        [Range(0f, 10f)]
        [SerializeField] private float _rockFacing = 2.5f;

        [Tooltip("Per-face nudge of the rock tone: interleaves the two rock swatches along their border.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rockDither = 0.2f;

        [Header("Dust")]
        [Tooltip("Size of the broad dust patches on the floor, metres.")]
        [Range(10f, 500f)]
        [SerializeField] private float _patchWavelength = 140f;

        [Tooltip("Strength of the broad patches in the dust tone.")]
        [Range(0f, 2f)]
        [SerializeField] private float _patchStrength = 1.1f;

        [Tooltip("Size of the small dust patches, metres: several metres across, never a single facet.")]
        [Range(4f, 100f)]
        [SerializeField] private float _detailPatchWavelength = 22f;

        [Tooltip("Strength of the small patches in the dust tone.")]
        [Range(0f, 2f)]
        [SerializeField] private float _detailPatchStrength = 0.35f;

        [Tooltip("Shifts every dust tone: below zero the moon is mostly deep, cool dust with lighter patches.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _toneBias = -0.1f;

        [Tooltip("Tone below zero at which the dust has turned all the way to the shadow dust.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _shadowReach = 1.1f;

        [Tooltip("Tone above zero at which the dust has turned as light as it gets (see light share).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _lightReach = 1.1f;

        [Tooltip("How far the lightest open dust goes from the mid dust toward the light dust (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lightShare = 0.55f;

        [Header("Whispering Canyon")]
        [Tooltip("How far the canyon floor's dust may go from the shadow dust toward the mid dust (0..1): the " +
            "canyon is cooler and darker than the basin, with no light dust inside.")]
        [Range(0f, 1f)]
        [SerializeField] private float _canyonMidShare = 0.85f;

        [Tooltip("Chasm weight at which the trough has turned fully charcoal: it reads deep through darkness.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _chasmDark = 0.5f;

        [Header("Craters and highlands")]
        [Tooltip("Crater bowl weight (0 = edge, 1 = centre) where the shaded crater wall starts.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterShadow = 0.12f;

        [Tooltip("Crater bowl weight where the wall ends and the crater floor (toned like the open floor) begins.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterFloor = 0.7f;

        [Tooltip("How far crater walls turn toward the shadow dust (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterWallShade = 0.6f;

        [Tooltip("How far the crest of a crater rim turns toward the light dust (0..1); it fades out down the rim.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterRimLight = 0.5f;

        [Tooltip("Rim zone around which gentle faces turn toward the shadow dust (dust settled between the rocks).")]
        [Range(0f, 1f)]
        [SerializeField] private float _highlandZone = 0.65f;

        [Tooltip("How far the highland dust turns toward the shadow dust (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _highlandShade = 0.8f;

        public float RockSlope => _rockSlope;
        public float RimRockSlope => _rimRockSlope;
        public float RimZoneStart => _rimZoneStart;
        public float RockLightSlope => _rockLightSlope;
        public float RockSlopeBlend => _rockSlopeBlend;
        public float RockLightHeight => _rockLightHeight;
        public float RockHeightBlend => _rockHeightBlend;
        public float RockMottle => _rockMottle;
        public float RockFacing => _rockFacing;
        public float RockDither => _rockDither;
        public float PatchWavelength => _patchWavelength;
        public float PatchStrength => _patchStrength;
        public float DetailPatchWavelength => _detailPatchWavelength;
        public float DetailPatchStrength => _detailPatchStrength;
        public float ToneBias => _toneBias;
        public float ShadowReach => _shadowReach;
        public float LightReach => _lightReach;
        public float LightShare => _lightShare;
        public float CanyonMidShare => _canyonMidShare;
        public float ChasmDark => _chasmDark;
        public float CraterShadow => _craterShadow;
        public float CraterFloor => _craterFloor;
        public float CraterWallShade => _craterWallShade;
        public float CraterRimLight => _craterRimLight;
        public float HighlandZone => _highlandZone;
        public float HighlandShade => _highlandShade;
    }
}
