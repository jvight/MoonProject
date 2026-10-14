using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's eye while it scans a fragment (docs/design/0.5.5-owner-feedback.md §1.2): the lid lifting fully, the lens
    /// fan's sweep, the eye's pulse at each quarter, and the lens's single flare as the lines stream in.
    /// </summary>
    [Serializable]
    public sealed class ScanLookSettings
    {
        [Tooltip("Half-life (s) of the lid lifting fully open as a scan starts, and settling back after.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _openHalfLife = 0.15f;

        [Tooltip("Half-life (s) of the lens fan easing in and out: soft, never switched.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _fanHalfLife = 0.12f;

        [Tooltip("How far (deg) the fan sweeps to each side across the fragment.")]
        [Range(0f, 45f)]
        [SerializeField] private float _sweepDegrees = 3f;

        [Tooltip("Seconds of one sweep across and back.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _sweepSeconds = 1.4f;

        [Tooltip("How much brighter the fan grows by the scan's end (share): the drawing nears complete.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fanSwell = 0.2f;

        [Tooltip("The eye's glow while scanning (multiplier): awake, learning.")]
        [Range(1f, 3f)]
        [SerializeField] private float _eyeGlow = 1.1f;

        [Tooltip("Each quarter of the scan: the eye's brief pulse (extra glow, share).")]
        [Range(0f, 2f)]
        [SerializeField] private float _quarterPulse = 0.2f;

        [Tooltip("Seconds a quarter's pulse takes to fade.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _pulseSeconds = 0.15f;

        [Tooltip("The lines reach the lens: its single flare (extra glow, share).")]
        [Range(0f, 4f)]
        [SerializeField] private float _flare = 0.45f;

        [Tooltip("Seconds the lens's flare lasts, up and down (100 ms).")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _flareSeconds = 0.1f;

        [Tooltip("Seconds 07 keeps looking after the scan completes: the dissolve's lines streaming in.")]
        [Range(0f, 3f)]
        [SerializeField] private float _dissolveSeconds = 0.9f;

        [Tooltip("The lens fan's colour: the scan's tech-cyan.")]
        [SerializeField] private Color _fanColour = new Color(0.373f, 0.953f, 1f);

        [Tooltip("The lens fan's glow at full: faint, the fragment's own sweep line carries most of the read.")]
        [Range(0f, 4f)]
        [SerializeField] private float _fanGlow = 0.55f;

        [Tooltip("Gaze priority of the fragment being scanned (above idle glances and the cut's focus).")]
        [Range(0, 10)]
        [SerializeField] private int _gazePriority = 8;

        public float OpenHalfLife => _openHalfLife;

        public float FanHalfLife => _fanHalfLife;

        public float SweepDegrees => _sweepDegrees;

        public float SweepSeconds => _sweepSeconds;

        public float FanSwell => _fanSwell;

        public float EyeGlow => _eyeGlow;

        public float QuarterPulse => _quarterPulse;

        public float PulseSeconds => _pulseSeconds;

        public float Flare => _flare;

        public float FlareSeconds => _flareSeconds;

        public int GazePriority => _gazePriority;

        public float DissolveSeconds => _dissolveSeconds;

        public Color FanColour => _fanColour;

        public float FanGlow => _fanGlow;
    }
}
