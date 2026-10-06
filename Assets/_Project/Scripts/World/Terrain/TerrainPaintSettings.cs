using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Rules that give every terrain triangle one palette swatch. Floor dust is painted by facet orientation to the
    /// earthlight (lit facets light, lee facets in shadow tone) on top of large soft patches, so facets and dunes
    /// read even where the ground is gentle; crater walls are shaded, crater rims lit, steep faces and the rim rock.
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

        [Header("Dust")]
        [Tooltip("Size of the light and mid dust patches on the floor, metres.")]
        [Range(10f, 500f)]
        [SerializeField] private float _patchWavelength = 140f;

        [Tooltip("Strength of the large patches in the dust tone.")]
        [Range(0f, 2f)]
        [SerializeField] private float _patchStrength = 0.45f;

        [Tooltip("Tone gained per unit of facet tilt toward the earthlight (sine of the tilt). 5 = a 5 degree tilt " +
            "shifts a facet by almost half a step: gentle dunes show their lit and lee sides.")]
        [Range(0f, 20f)]
        [SerializeField] private float _facingStrength = 5f;

        [Tooltip("Dust tone above which a face is light dust.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _lightTone = 0.25f;

        [Tooltip("Dust tone below which a lee facet (see lee tilt) is shadow dust.")]
        [Range(-2f, 1f)]
        [SerializeField] private float _shadowTone = -0.3f;

        [Tooltip("Only facets tilted at least this far away from the earthlight (degrees) can be shadow dust: dune " +
            "lee sides and crater outer walls darken, flat ground never shows dark speckles.")]
        [Range(0f, 30f)]
        [SerializeField] private float _leeTilt = 6f;

        [Tooltip("Per-triangle random nudge of the tone (uniform +-). A little wider than the light threshold on " +
            "purpose: about 8% of flat facets (more in light patches) turn light dust, a hand-painted mosaic that " +
            "shows the facets; tilting toward the earthlight raises the odds, so lit dune sides turn mostly light.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dither = 0.3f;

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
        public float PatchWavelength => _patchWavelength;
        public float PatchStrength => _patchStrength;
        public float FacingStrength => _facingStrength;
        public float LightTone => _lightTone;
        public float ShadowTone => _shadowTone;
        public float LeeTilt => _leeTilt;
        public float Dither => _dither;
        public float CraterShadow => _craterShadow;
        public float CraterFloor => _craterFloor;
        public float CraterHighlight => _craterHighlight;
        public float HighlandZone => _highlandZone;
    }
}
