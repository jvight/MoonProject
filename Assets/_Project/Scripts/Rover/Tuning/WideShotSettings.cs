using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The lonely wide shot (VISION pillar 6, "stillness is a reward"): after 07 has rested a while the camera drifts
    /// slowly out and up until 07 sits small in the lower third under a low horizon, turning gently toward Earth or
    /// The Peak, and keeps breathing there; any drive or look input hands the view back quickly but eased. Shut in by
    /// high ground (the canyon), where there is no horizon to show, it takes a gentler, closer, lower frame looking up
    /// the way between the walls instead. The composition is checked against the analytic terrain so the camera stays
    /// above the ground and out of canyon walls (it rises first, then comes closer).
    /// </summary>
    [Serializable]
    public sealed class WideShotSettings
    {
        [Header("Timing")]
        [Tooltip("Seconds of uninterrupted rest (IRoverStillness, nothing else going on) before the frame opens.")]
        [Range(2f, 60f)]
        [SerializeField] private float _delay = 9f;

        [Tooltip("Seconds the drift out takes to all but settle (eased in and out, never a jolt).")]
        [Range(2f, 20f)]
        [SerializeField] private float _openSeconds = 8f;

        [Tooltip("Seconds the view takes to come back to the chase camera on drive or look input (eased, no cut).")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _handBackSeconds = 0.8f;

        [Tooltip("Seconds a memory card or liner note appearing holds the wide shot back (time to read it).")]
        [Range(0f, 60f)]
        [SerializeField] private float _cardQuietSeconds = 14f;

        [Tooltip("Seconds an interaction (Bell's dial turned, an upgrade held at a station) holds the wide shot back.")]
        [Range(0f, 60f)]
        [SerializeField] private float _interactionQuietSeconds = 8f;

        [Header("Composition")]
        [Tooltip("Camera distance (m) from 07's follow point when nothing is in the way: 07 reads small.")]
        [Range(8f, 80f)]
        [SerializeField] private float _distance = 28f;

        [Tooltip("Camera elevation (deg above 07's follow point): low, so the horizon sits low and the sky is big.")]
        [Range(2f, 40f)]
        [SerializeField] private float _elevation = 7f;

        [Tooltip("Where 07 sits on screen in the wide shot (0 = centre, +y = lower; ~0.17 is the lower third).")]
        [Range(-0.4f, 0.4f)]
        [SerializeField] private float _screenY = 0.19f;

        [Tooltip("Extra vertical field of view (deg) in the wide shot.")]
        [Range(0f, 20f)]
        [SerializeField] private float _fovWiden = 4f;

        [Tooltip("Share (0..1) of the way the view turns from where it looked toward Earth or The Peak.")]
        [Range(0f, 1f)]
        [SerializeField] private float _yawShare = 0.85f;

        [Tooltip("Largest turn (deg) toward Earth or The Peak: a gentle bias, never a hard turn.")]
        [Range(0f, 90f)]
        [SerializeField] private float _maxYawSwing = 40f;

        [Tooltip("Earth or The Peak farther than this (deg) from where the view looked is not turned to at all.")]
        [Range(10f, 180f)]
        [SerializeField] private float _maxSubjectAngle = 110f;

        [Tooltip("Earth is preferred unless The Peak is this many degrees closer to where the view looked.")]
        [Range(0f, 90f)]
        [SerializeField] private float _earthPreference = 15f;

        [Header("Shut in (the canyon)")]
        [Tooltip("Radius (m) around 07 at which the skyline is measured to tell open ground from a canyon.")]
        [Range(5f, 100f)]
        [SerializeField] private float _skylineRadius = 20f;

        [Tooltip("Skyline samples around 07.")]
        [Range(4, 64)]
        [SerializeField] private int _skylineSamples = 12;

        [Tooltip("Mean skyline elevation (deg) up to which the ground counts as open: the full wide shot.")]
        [Range(0f, 45f)]
        [SerializeField] private float _openSkyline = 6f;

        [Tooltip("Mean skyline elevation (deg) from which 07 counts as shut in: the closer canyon frame.")]
        [Range(1f, 80f)]
        [SerializeField] private float _shutInSkyline = 25f;

        [Tooltip("Camera distance (m) when shut in: closer, so 07 still reads between the walls.")]
        [Range(4f, 40f)]
        [SerializeField] private float _shutInDistance = 11f;

        [Tooltip("Camera elevation (deg) when shut in: low, looking up the way between the walls to the strip of sky.")]
        [Range(2f, 45f)]
        [SerializeField] private float _shutInElevation = 5f;

        [Tooltip("Where 07 sits on screen when shut in (0 = centre, +y = lower): low, under the towering walls.")]
        [Range(-0.4f, 0.4f)]
        [SerializeField] private float _shutInScreenY = 0.28f;

        [Header("Terrain")]
        [Tooltip("Height (m) the camera keeps above the ground beneath it.")]
        [Range(0.3f, 10f)]
        [SerializeField] private float _clearance = 1.5f;

        [Tooltip("Height (m) the line from the camera to 07 keeps above the ground close to 07 (grows to the camera "
            + "clearance toward the camera), so no dune hides 07.")]
        [Range(0f, 5f)]
        [SerializeField] private float _sightClearance = 0.6f;

        [Tooltip("Ground samples along the line from 07 to the camera.")]
        [Range(2, 64)]
        [SerializeField] private int _terrainSamples = 16;

        [Tooltip("Highest elevation (deg) the camera rises to clear the ground before it comes closer instead.")]
        [Range(5f, 60f)]
        [SerializeField] private float _maxElevation = 30f;

        [Tooltip("Closest distance (m) the wide shot comes in to (a canyon gets a gentler, closer version).")]
        [Range(4f, 40f)]
        [SerializeField] private float _minDistance = 11f;

        [Tooltip("Distances tried between the full and the closest distance when the ground is in the way.")]
        [Range(1, 32)]
        [SerializeField] private int _distanceSteps = 8;

        [Header("Breathing")]
        [Tooltip("Slow sway (deg) of the view left and right once open, so the frame is never dead still.")]
        [Range(0f, 10f)]
        [SerializeField] private float _breathYaw = 1.6f;

        [Tooltip("Seconds per sway left and right.")]
        [Range(5f, 120f)]
        [SerializeField] private float _breathYawPeriod = 37f;

        [Tooltip("Slow rise and fall (deg) of the camera once open.")]
        [Range(0f, 5f)]
        [SerializeField] private float _breathElevation = 0.7f;

        [Tooltip("Seconds per rise and fall.")]
        [Range(5f, 120f)]
        [SerializeField] private float _breathElevationPeriod = 23f;

        [Tooltip("Slow drift in and out (fraction of the distance) once open.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _breathDistance = 0.04f;

        [Tooltip("Seconds per drift in and out.")]
        [Range(5f, 120f)]
        [SerializeField] private float _breathDistancePeriod = 29f;

        public float Delay => _delay;

        public float OpenSeconds => _openSeconds;

        public float HandBackSeconds => _handBackSeconds;

        public float CardQuietSeconds => _cardQuietSeconds;

        public float InteractionQuietSeconds => _interactionQuietSeconds;

        public float Distance => _distance;

        public float Elevation => _elevation;

        public float ScreenY => _screenY;

        public float FovWiden => _fovWiden;

        public float YawShare => _yawShare;

        public float MaxYawSwing => _maxYawSwing;

        public float MaxSubjectAngle => _maxSubjectAngle;

        public float EarthPreference => _earthPreference;

        public float SkylineRadius => _skylineRadius;

        public int SkylineSamples => _skylineSamples;

        public float OpenSkyline => _openSkyline;

        public float ShutInSkyline => Mathf.Max(_shutInSkyline, _openSkyline + 1f);

        public float ShutInDistance => _shutInDistance;

        public float ShutInElevation => _shutInElevation;

        public float ShutInScreenY => _shutInScreenY;

        public float Clearance => _clearance;

        public float SightClearance => Mathf.Min(_sightClearance, _clearance);

        public int TerrainSamples => _terrainSamples;

        public float MaxElevation => Mathf.Max(_maxElevation, Mathf.Max(_elevation, _shutInElevation));

        public float MinDistance => Mathf.Min(_minDistance, Mathf.Min(_distance, _shutInDistance));

        public int DistanceSteps => _distanceSteps;

        public float BreathYaw => _breathYaw;

        public float BreathYawPeriod => _breathYawPeriod;

        public float BreathElevation => _breathElevation;

        public float BreathElevationPeriod => _breathElevationPeriod;

        public float BreathDistance => _breathDistance;

        public float BreathDistancePeriod => _breathDistancePeriod;
    }
}
