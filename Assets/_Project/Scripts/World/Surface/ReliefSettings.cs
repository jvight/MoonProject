using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The basin floor's gentle relief (owner, 2026-10-19: the plain read flat): low swells and shallow swales, a few
    /// long sinuous wrinkle ridges and the furrows of boulders that rolled down the big crater's wall. It fades to
    /// level over the opening's places and lanes, so the opening plays as it was laid out. No fields of small round
    /// pits, ever (the owner's hard rule).
    /// </summary>
    [Serializable]
    public sealed class ReliefSettings
    {
        [Header("Swells and swales")]
        [Tooltip("Height of the low swells and shallow swales over the plain, metres (either way from level).")]
        [Range(0f, 3f)]
        [SerializeField] private float _swellHeight = 0.6f;

        [Tooltip("Wavelength of the swells, metres: broad enough that the slopes stay gentle.")]
        [Range(20f, 200f)]
        [SerializeField] private float _swellWavelength = 70f;

        [Header("Wrinkle ridges")]
        [Tooltip("The long, sinuous mare ridges on the plain, away from the opening's places and lanes and run between " +
            "the craters (the relief gives way round each).")]
        [SerializeField] private WrinkleRidge[] _ridges =
        {
            new WrinkleRidge(new Vector2(20f, -30f), 90f, 180f, 1.1f, 18f, 0.6f, 8f, 5.5f, false, 9f, 110f, 45f),
            new WrinkleRidge(new Vector2(-60f, -110f), 225f, 150f, 1.0f, 17f, 0.6f, 7.5f, 5.5f, true, 10f, 120f, 40f),
        };

        [Header("Boulder tracks")]
        [Tooltip("Furrows of boulders that rolled down the big old crater's inner wall (bowl 2), each ending at its " +
            "boulder.")]
        [SerializeField] private BoulderTrack[] _tracks =
        {
            new BoulderTrack(new Vector2(94.6f, -97.9f), new Vector2(111.1f, -126.5f), 7f, 0.35f, "Rock_09"),
            new BoulderTrack(new Vector2(149.6f, -97.9f), new Vector2(133.1f, -126.5f), 6.5f, 0.3f, "Rock_08"),
            new BoulderTrack(new Vector2(70.4f, -164.4f), new Vector2(101.5f, -153.1f), 7f, 0.35f, "Rock_09"),
        };

        [Tooltip("Share of a track's boulder's height it sits sunk in the dust where it came to rest.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _trackBoulderSink = 0.3f;

        [Header("Level where the opening plays")]
        [Tooltip("Lanes and spots the relief leaves level besides the opening's sites: the first drive's cable lane, " +
            "trail A, and the retroreflector, whose golden-hour Sun clears The Peak by half a degree.")]
        [SerializeField] private ProtectedLane[] _protectedLanes =
        {
            new ProtectedLane(new Vector2(-2.4f, -4f), new Vector2(34.9f, 160.2f), 20f),
            new ProtectedLane(new Vector2(27f, 147f), new Vector2(42.7f, 148.6f), 12f),
            new ProtectedLane(new Vector2(42.7f, 148.6f), new Vector2(79.5f, 185.5f), 12f),
            new ProtectedLane(new Vector2(52.8f, 160.4f), new Vector2(38f, 222f), 30f),
        };

        [Tooltip("Level ground kept beyond each of the opening's site footprints (the wake pad, apron, lander, ruins, " +
            "the Scout's crater, the midway stop), metres.")]
        [Range(0f, 60f)]
        [SerializeField] private float _siteMargin = 20f;

        [Tooltip("Distance over which the relief fades back in beyond a protected lane or site, metres.")]
        [Range(5f, 80f)]
        [SerializeField] private float _protectFade = 30f;

        public float SwellHeight => _swellHeight;
        public float SwellWavelength => _swellWavelength;
        public WrinkleRidge[] Ridges => _ridges;
        public BoulderTrack[] Tracks => _tracks;
        public float TrackBoulderSink => _trackBoulderSink;
        public ProtectedLane[] ProtectedLanes => _protectedLanes;
        public float SiteMargin => _siteMargin;
        public float ProtectFade => _protectFade;
    }
}
