using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The ground's smallest scale (M3-18 V4): marks pressed into the dust. 07's worn tracks (desire paths it has
    /// driven a hundred times) out from home to the places it goes and between home's places, the lander's
    /// landing-blast scar and a few crew bootprints by the ladder. Drawn with the rover's track shader.
    /// </summary>
    [Serializable]
    public sealed class GroundMarkSettings
    {
        [Header("Worn tracks")]
        [Tooltip("The crew's faded tracks run anchor to anchor along this route (opening v2 §4: from 07's wake point " +
            "to the landed lander and on to the Lumen ruins), one worn pair of ruts per leg.")]
        [SerializeField] private string[] _route =
        {
            WorldAnchorIds.PlaceWake, WorldAnchorIds.PlaceLander, WorldAnchorIds.PlaceRuins,
        };

        [Tooltip("Distance between the centres of 07's two ruts, metres.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _trackGauge = 1.4f;

        [Tooltip("Half width of one rut, metres.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _rutHalfWidth = 0.22f;

        [Tooltip("Spacing of the points along a track, metres: about the terrain's fine facet.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _step = 1.5f;

        [Tooltip("How far a worn track wanders off the straight line, metres (none at its ends).")]
        [Range(0f, 10f)]
        [SerializeField] private float _wander = 3.5f;

        [Tooltip("Length of one wander, metres.")]
        [Range(5f, 200f)]
        [SerializeField] private float _wanderWavelength = 45f;

        [Tooltip("Strength of a fully worn rut (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _strength = 0.92f;

        [Tooltip("How much the wear comes and goes along a track (0..1): patchy, never a painted line.")]
        [Range(0f, 1f)]
        [SerializeField] private float _patchiness = 0.58f;

        [Tooltip("Length of the worn and faint patches along a track, metres.")]
        [Range(2f, 100f)]
        [SerializeField] private float _patchWavelength = 9f;

        [Tooltip("How much each rut edge scuffs on its own (0..1): close up a rut varies along and across, never a " +
            "painted line.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scuff = 0.52f;

        [Tooltip("Length of a scuff along a rut, metres: a few rover lengths.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _scuffWavelength = 3f;

        [Tooltip("Length over which a track fades in and out at its ends, metres.")]
        [Range(0f, 40f)]
        [SerializeField] private float _endFade = 3.2f;

        [Tooltip("Height of the marks above the ground, metres (the shader's depth offset does the rest).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _lift = 0.05f;

        [Tooltip("Tint the marks multiply the dust by at full strength: a little darker, a little cooler.")]
        [SerializeField] private Color _tint = new Color(0.52f, 0.53f, 0.62f, 1f);

        [Tooltip("Distance where the marks start to fade out, metres.")]
        [Range(0f, 300f)]
        [SerializeField] private float _fadeNear = 45f;

        [Tooltip("Distance where the marks are gone, metres.")]
        [Range(1f, 400f)]
        [SerializeField] private float _fadeFar = 130f;

        [Header("Landing-blast scar")]
        [Tooltip("Radius of the lander's landing-blast scar, metres.")]
        [Range(1f, 30f)]
        [SerializeField] private float _scarRadius = 13f;

        [Tooltip("Radius inside which the scar's streaks start (under the lander), metres.")]
        [Range(0f, 10f)]
        [SerializeField] private float _scarInner = 1.5f;

        [Tooltip("Number of radial streaks the blast swept into the dust.")]
        [Range(4, 64)]
        [SerializeField] private int _scarStreaks = 32;

        [Tooltip("Half width of one streak, metres.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _scarStreakHalfWidth = 0.6f;

        [Tooltip("Strength of a streak at its inner end (0..1); it fades outward.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scarStrength = 0.75f;

        [Header("Bootprints")]
        [Tooltip("Where the crew's bootprints start, in the lander's frame (the ladder's foot), metres.")]
        [SerializeField] private Vector3 _bootprintStart = new Vector3(1.6f, 0f, 3.2f);

        [Tooltip("Heading of the bootprints in the lander's frame, degrees from its forward.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _bootprintHeading = 55f;

        [Tooltip("Number of bootprints.")]
        [Range(0, 30)]
        [SerializeField] private int _bootprintCount = 9;

        [Tooltip("Distance between steps, metres.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _bootprintStride = 0.75f;

        [Tooltip("Bootprint half length and half width, metres.")]
        [SerializeField] private Vector2 _bootprintSize = new Vector2(0.16f, 0.07f);

        [Tooltip("Strength of a bootprint (0..1): a faint remnant, years old.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bootprintStrength = 0.5f;

        public string[] Route => _route;
        public float TrackGauge => _trackGauge;
        public float RutHalfWidth => _rutHalfWidth;
        public float Step => _step;
        public float Wander => _wander;
        public float WanderWavelength => _wanderWavelength;
        public float Strength => _strength;
        public float Patchiness => _patchiness;
        public float PatchWavelength => _patchWavelength;
        public float EndFade => _endFade;
        public float Scuff => _scuff;
        public float ScuffWavelength => _scuffWavelength;
        public float Lift => _lift;
        public Color Tint => _tint;
        public float FadeNear => _fadeNear;
        public float FadeFar => _fadeFar;
        public float ScarRadius => _scarRadius;
        public float ScarInner => _scarInner;
        public int ScarStreaks => _scarStreaks;
        public float ScarStreakHalfWidth => _scarStreakHalfWidth;
        public float ScarStrength => _scarStrength;
        public Vector3 BootprintStart => _bootprintStart;
        public float BootprintHeading => _bootprintHeading;
        public int BootprintCount => _bootprintCount;
        public float BootprintStride => _bootprintStride;
        public Vector2 BootprintSize => _bootprintSize;
        public float BootprintStrength => _bootprintStrength;
    }
}
