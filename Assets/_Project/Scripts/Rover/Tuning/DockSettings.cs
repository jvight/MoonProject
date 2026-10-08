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

        public float SettleSeconds => _settleSeconds;

        public float LampDim => _lampDim;

        public float LampHalfLife => _lampHalfLife;
    }
}
