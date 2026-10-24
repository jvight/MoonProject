using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Resting on the charging dock at home (docs/features/M3-14): 07 eases onto the dock's anchor, never snapping,
    /// and its road light dims while it charges.
    /// </summary>
    [Serializable]
    public sealed class DockSettings
    {
        [Tooltip("Seconds 07 takes to ease from where it stopped onto the dock's anchor (position and heading).")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _settleSeconds = 1f;

        [Tooltip("Road light (and lamp bar) brightness while docked, as a share of normal.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lampDim = 0.3f;

        [Tooltip("Half-life (s) of the lamp dimming on the dock and coming back up on leaving it.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _lampHalfLife = 0.5f;

        [Tooltip("Seconds 07 takes to turn in place on the pad when the dock lets go facing another way, before its wheels are free.")]
        [Range(0.4f, 4f)]
        [SerializeField] private float _releaseTurnSeconds = 1.2f;

        [Tooltip("Heading difference (deg) from which the release turn happens; below it 07 is simply freed.")]
        [Range(0f, 30f)]
        [SerializeField] private float _releaseTurnThresholdDegrees = 3f;

        [Tooltip("Stick or throttle deflection (0..1) that cuts the release turn short and hands 07 to the player at once.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _releaseInterruptInput = 0.1f;

        public float SettleSeconds => _settleSeconds;

        public float ReleaseTurnSeconds => _releaseTurnSeconds;

        public float ReleaseTurnThresholdDegrees => _releaseTurnThresholdDegrees;

        public float ReleaseInterruptInput => _releaseInterruptInput;

        public float LampDim => _lampDim;

        public float LampHalfLife => _lampHalfLife;
    }
}
