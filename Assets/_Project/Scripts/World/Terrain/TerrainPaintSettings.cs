using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Rules that give every terrain triangle one palette swatch. Floor dust follows low-frequency patches and the
    /// smoothed tilt of the ground toward the earthlight (lit dune sides light, lee sides in shadow tone), never
    /// per-facet randomness (design ruling 9: patchy, never confetti). Crater walls are shaded, crater rims lit,
    /// steep faces and the rim are rock.
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
        [Tooltip("Size of the broad light and mid dust patches on the floor, metres.")]
        [Range(10f, 500f)]
        [SerializeField] private float _patchWavelength = 140f;

        [Tooltip("Strength of the broad patches in the dust tone.")]
        [Range(0f, 2f)]
        [SerializeField] private float _patchStrength = 0.45f;

        [Tooltip("Size of the small dust patches, metres: several metres across, never a single facet.")]
        [Range(4f, 100f)]
        [SerializeField] private float _detailPatchWavelength = 15f;

        [Tooltip("Strength of the small patches in the dust tone.")]
        [Range(0f, 2f)]
        [SerializeField] private float _detailPatchStrength = 0.7f;

        [Tooltip("Ground tilt toward the earthlight is measured over this distance (metres), so the fine grain of " +
            "single facets never changes their colour: only dunes, hills and crater walls do.")]
        [Range(1f, 30f)]
        [SerializeField] private float _tiltSmoothing = 7f;

        [Tooltip("Tone gained per unit of smoothed tilt toward the earthlight (sine of the tilt). Lit dune sides " +
            "turn light dust, lee sides shadow dust.")]
        [Range(0f, 20f)]
        [SerializeField] private float _facingStrength = 5f;

        [Tooltip("Dust tone above which a face is light dust.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _lightTone = 0.22f;

        [Tooltip("Dust tone below which a lee face (see lee tilt) is shadow dust.")]
        [Range(-2f, 1f)]
        [SerializeField] private float _shadowTone = -0.3f;

        [Tooltip("Only ground tilted at least this far away from the earthlight (smoothed, degrees) can be shadow " +
            "dust: dune lee sides darken, flat ground never shows dark stains.")]
        [Range(0f, 30f)]
        [SerializeField] private float _leeTilt = 6f;

        [Tooltip("Per-face nudge of the dust tone. Keep it tiny: it only frays patch borders by a facet or so " +
            "(design ruling 9 forbids isolated bright facets).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _dither = 0.02f;

        [Header("Whispering Canyon")]
        [Tooltip("Canyon floor tone above which a face is mid dust instead of shadow dust. The canyon is cooler and " +
            "darker than the basin: no light dust inside.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _canyonMidTone = 0.05f;

        [Tooltip("Chasm weight above which the trough is charcoal: it reads deep through darkness, not depth.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chasmDark = 0.5f;

        [Header("Craters and highlands")]
        [Tooltip("Crater bowl weight (0 = edge, 1 = centre) where the shaded crater wall starts.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterShadow = 0.12f;

        [Tooltip("Crater bowl weight where the wall ends and the crater floor (painted like the open floor) begins.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterFloor = 0.7f;

        [Tooltip("Crater rim weight above which faces are light dust (raised rims catch the light).")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterHighlight = 0.6f;

        [Tooltip("Rim zone above which gentle faces are shadow dust (dust settled between the rocks).")]
        [Range(0f, 1f)]
        [SerializeField] private float _highlandZone = 0.65f;

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
        public float TiltSmoothing => _tiltSmoothing;
        public float FacingStrength => _facingStrength;
        public float LightTone => _lightTone;
        public float ShadowTone => _shadowTone;
        public float LeeTilt => _leeTilt;
        public float Dither => _dither;
        public float CanyonMidTone => _canyonMidTone;
        public float ChasmDark => _chasmDark;
        public float CraterShadow => _craterShadow;
        public float CraterFloor => _craterFloor;
        public float CraterHighlight => _craterHighlight;
        public float HighlandZone => _highlandZone;
    }
}
