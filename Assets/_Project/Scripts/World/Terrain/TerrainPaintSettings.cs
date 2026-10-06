using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Rules that give every terrain triangle one palette swatch: dust shades on the floor in large soft patches,
    /// shade in craters, light on crater rims, rock on steep faces and the rim mountains.
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

        [Tooltip("Rock above this height (metres) leans to the light rock swatch, below it to the dark one.")]
        [Range(0f, 200f)]
        [SerializeField] private float _rockLightHeight = 70f;

        [Tooltip("Height range (metres) over which rock fades from dark to light.")]
        [Range(1f, 200f)]
        [SerializeField] private float _rockHeightBlend = 60f;

        [Header("Dust")]
        [Tooltip("Size of the light/mid/shadow dust patches on the floor, metres.")]
        [Range(10f, 500f)]
        [SerializeField] private float _patchWavelength = 140f;

        [Tooltip("Patch value above which floor dust is light.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _lightPatch = 0.42f;

        [Tooltip("Patch value below which floor dust is in shadow tone. -1 disables shadow patches: shadow dust then " +
            "belongs to crater walls and the highlands only, so the floor never shows dark stains.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _shadowPatch = -1f;

        [Tooltip("Per-triangle random nudge of the patch value: frays the patch edges like hand painting.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _dither = 0.03f;

        [Tooltip("Faces steeper than this (degrees) start leaning towards darker dust.")]
        [Range(0f, 45f)]
        [SerializeField] private float _darkenSlope = 11f;

        [Tooltip("Extra steepness (degrees) over which dust darkens by one full patch step.")]
        [Range(1f, 60f)]
        [SerializeField] private float _darkenRange = 16f;

        [Header("Craters and highlands")]
        [Tooltip("Crater bowl weight (0 = edge, 1 = centre) above which steep faces are shadow dust.")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterShadow = 0.1f;

        [Tooltip("Inside a crater, faces steeper than this (degrees) are its walls and get shadow dust.")]
        [Range(0f, 45f)]
        [SerializeField] private float _craterWallSlope = 7f;

        [Tooltip("Crater rim weight above which faces are light dust (raised rims catch the light).")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterHighlight = 0.6f;

        [Tooltip("Rim zone above which gentle faces are shadow dust (dust settled between the rocks).")]
        [Range(0f, 1f)]
        [SerializeField] private float _highlandZone = 0.65f;

        public float RockSlope => _rockSlope;
        public float RimRockSlope => _rimRockSlope;
        public float RimZoneStart => _rimZoneStart;
        public float RockLightHeight => _rockLightHeight;
        public float RockHeightBlend => _rockHeightBlend;
        public float PatchWavelength => _patchWavelength;
        public float LightPatch => _lightPatch;
        public float ShadowPatch => _shadowPatch;
        public float Dither => _dither;
        public float DarkenSlope => _darkenSlope;
        public float DarkenRange => _darkenRange;
        public float CraterShadow => _craterShadow;
        public float CraterWallSlope => _craterWallSlope;
        public float CraterHighlight => _craterHighlight;
        public float HighlandZone => _highlandZone;
    }
}
