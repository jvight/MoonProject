using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The ground's smallest scale (M3-18 V4): marks pressed into the dust, the crew's faded tracks along the
    /// opening's route (opening v2 §4). Drawn with the rover's track shader.
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
    }
}
