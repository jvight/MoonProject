using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One cutter tier's numbers (docs/features/M3-26 §3).</summary>
    [Serializable]
    public struct CutterTier
    {
        [Tooltip("Seconds of a fresh charge run, from the press to the crack.")]
        [Range(0.1f, 1f)] [SerializeField] private float _chargeSeconds;

        [Tooltip("Seconds of a resumed charge run (it starts part-way: the line is still warm).")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _resumeChargeSeconds;

        [Tooltip("Multiplier on cutting speed once the beam fires.")]
        [Range(0.5f, 3f)] [SerializeField] private float _speedFactor;

        [Tooltip("Multiplier on the battery drain of beam work.")]
        [Range(0.3f, 1.5f)] [SerializeField] private float _drainFactor;

        [Tooltip("Conduit lamps the charge run lights on 07's back.")]
        [Range(1, 12)] [SerializeField] private int _lampCount;

        public CutterTier(float chargeSeconds, float resumeChargeSeconds, float speedFactor,
            float drainFactor, int lampCount)
        {
            _chargeSeconds = chargeSeconds;
            _resumeChargeSeconds = resumeChargeSeconds;
            _speedFactor = speedFactor;
            _drainFactor = drainFactor;
            _lampCount = lampCount;
        }

        public float ChargeSeconds => _chargeSeconds;

        public float ResumeChargeSeconds => _resumeChargeSeconds;

        public float SpeedFactor => _speedFactor;

        public float DrainFactor => _drainFactor;

        public int LampCount => _lampCount;
    }
}
