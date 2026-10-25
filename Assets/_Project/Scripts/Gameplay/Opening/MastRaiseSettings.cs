using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The antenna mast's raise (<see cref="MastRaise"/>, opening v3 R7): its segments lifting and locking, one slow,
    /// mechanical moment.
    /// </summary>
    [Serializable]
    public sealed class MastRaiseSettings
    {
        [Tooltip("Seconds the mast's segments take to lift and lock, each (eased).")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _segmentLiftSeconds = 1.6f;

        [Tooltip("Seconds between one segment locking and the next starting to lift.")]
        [Range(0f, 3f)]
        [SerializeField] private float _segmentGapSeconds = 0.5f;

        [Tooltip("Seconds the last segment takes to settle after it locks: the raise is done after it.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _settleSeconds = 0.4f;

        [Tooltip("How many segments the mast raises: base, middle, top.")]
        [Range(1, 6)]
        [SerializeField] private int _segments = 3;

        [Tooltip("Metres each segment sits lower than its authored place per segment above the cabinet: segment 1 starts one " +
                 "this far down, segment 2 two, segment 3 three, telescoped into the cabinet.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _segmentTravelMetres = 0.375f;

        [Tooltip("Metres a segment gives (sinks back, then rises again) as it locks home.")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _settleMetres = 0.012f;

        public float SegmentTravelMetres => _segmentTravelMetres;
        public float SettleMetres => _settleMetres;
        public float SegmentLiftSeconds => _segmentLiftSeconds;
        public float SegmentGapSeconds => _segmentGapSeconds;
        public float SettleSeconds => _settleSeconds;
        public int Segments => _segments;
    }
}
