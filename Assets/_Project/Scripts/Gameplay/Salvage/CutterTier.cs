using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One cutter tier's numbers (docs/features/M3-26 §3).</summary>
    [Serializable]
    public struct CutterTier
    {
        [Tooltip("Seconds one charge stage takes to build (0.5.5 §5.1: a piece needs 1-3 stages).")]
        [Range(0.05f, 1f)] [SerializeField] private float _stageSeconds;

        [Tooltip("Multiplier on cutting speed once the beam fires.")]
        [Range(0.5f, 3f)] [SerializeField] private float _speedFactor;

        [Tooltip("Multiplier on the battery drain of beam work.")]
        [Range(0.3f, 1.5f)] [SerializeField] private float _drainFactor;

        [Tooltip("Conduit lamps the charge run lights on 07's back.")]
        [Range(1, 12)] [SerializeField] private int _lampCount;

        public CutterTier(float stageSeconds, float speedFactor, float drainFactor, int lampCount)
        {
            _stageSeconds = stageSeconds;
            _speedFactor = speedFactor;
            _drainFactor = drainFactor;
            _lampCount = lampCount;
        }

        public float StageSeconds => _stageSeconds;

        public float SpeedFactor => _speedFactor;

        public float DrainFactor => _drainFactor;

        public int LampCount => _lampCount;
    }
}
