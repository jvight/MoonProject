using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's body through the cut's charge-up (docs/design/0.5.5-owner-feedback.md §5.3): the brace's squat, the
    /// settle and body-only tremor at each stage lock, the stiffened rockers and the soft rise on a release.
    /// </summary>
    [Serializable]
    public sealed class ChargeBraceSettings
    {
        [Tooltip("The brace: how far (m) 07 lowers on its suspension before anything builds (3 cm).")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _squat = 0.03f;

        [Tooltip("Spring frequency (Hz) of the brace's squat: settled within the 0.5 s brace.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _squatFrequency = 2.4f;

        [Tooltip("How far further (m) 07 settles at each stage lock (0.5 cm).")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _settlePerStage = 0.005f;

        [Tooltip("Spring frequency (Hz) of each stage's settle: quick, so the full lock is still by the held breath.")]
        [Range(1f, 20f)]
        [SerializeField] private float _settleFrequency = 8f;

        [Tooltip("A stage lock's micro-tremor in the body (never the camera): its peak (m).")]
        [Range(0f, 0.01f)]
        [SerializeField] private float _tremor = 0.0015f;

        [Tooltip("Seconds a stage lock's tremor lasts (60 ms), fading to nothing.")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float _tremorSeconds = 0.06f;

        [Tooltip("The tremor's frequency (Hz): a fine buzz, not a sway.")]
        [Range(5f, 80f)]
        [SerializeField] private float _tremorFrequency = 40f;

        [Tooltip("Half-life (s) of the rockers stiffening as the wheels lock (the bogies stop following the ground).")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _stiffenHalfLife = 0.06f;

        [Tooltip("A release or drain: spring frequency (Hz) of 07 rising back (about 300 ms).")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _riseFrequency = 2.6f;

        [Tooltip("A release or drain: damping ratio of the rise (under 1 gives its one soft overshoot).")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _riseDamping = 0.5f;

        public float Squat => _squat;

        public float SquatFrequency => _squatFrequency;

        public float SettlePerStage => _settlePerStage;

        public float SettleFrequency => _settleFrequency;

        public float Tremor => _tremor;

        public float TremorSeconds => _tremorSeconds;

        public float TremorFrequency => _tremorFrequency;

        public float StiffenHalfLife => _stiffenHalfLife;

        public float RiseFrequency => _riseFrequency;

        public float RiseDamping => _riseDamping;
    }
}
