using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Design ruling 7, "never stuck": when 07 has been trying to drive without getting anywhere for a few seconds
    /// (wedged against rocks, trapped in a hollow), it is lifted gently and set down on the nearest comfortable spot.
    /// Parking, holding still and slow climbs never count: only held throttle with no progress does.
    /// </summary>
    [Serializable]
    public sealed class RecoverySettings
    {
        [Tooltip("Throttle (0..1, eased) that counts as the player trying to move.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _tryingInput = 0.5f;

        [Tooltip("Moving farther than this (m) from where the attempt started counts as progress (resets the clock).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _progressRadius = 0.75f;

        [Tooltip("Seconds of trying without progress before 07 is helped out.")]
        [Range(1f, 15f)]
        [SerializeField] private float _stuckTime = 3.5f;

        [Tooltip("Closest distance (m) a recovery spot may be from where 07 got stuck.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _searchMinRadius = 2.5f;

        [Tooltip("Distance (m) between the search rings.")]
        [Range(0.25f, 5f)]
        [SerializeField] private float _searchRingStep = 1.5f;

        [Tooltip("Number of search rings (the farthest spot is MinRadius + (Rings - 1) * Step).")]
        [Range(1, 20)]
        [SerializeField] private int _searchRings = 6;

        [Tooltip("Spots tried per ring, starting straight behind 07 and sweeping round to the front.")]
        [Range(4, 36)]
        [SerializeField] private int _searchDirections = 12;

        [Tooltip("Steepest ground (deg) 07 may be set down on.")]
        [Range(0f, 45f)]
        [SerializeField] private float _maxSetDownSlope = 22f;

        [Tooltip("Radius (m) around a spot that must be free of rocks and props.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _clearance = 1.4f;

        [Tooltip("Height (m) of the gentle lift arc.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _liftHeight = 1.5f;

        [Tooltip("Seconds the lift, glide and set-down take.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _liftDuration = 2.2f;

        public float TryingInput => _tryingInput;

        public float ProgressRadius => _progressRadius;

        public float StuckTime => _stuckTime;

        public float SearchMinRadius => _searchMinRadius;

        public float SearchRingStep => _searchRingStep;

        public int SearchRings => _searchRings;

        public int SearchDirections => _searchDirections;

        public int SearchCandidates => _searchRings * _searchDirections;

        public float MaxSetDownSlope => _maxSetDownSlope;

        public float Clearance => _clearance;

        public float LiftHeight => _liftHeight;

        public float LiftDuration => _liftDuration;
    }
}
