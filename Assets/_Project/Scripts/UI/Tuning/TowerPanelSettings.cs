using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The radio tower upgrade panel and its hold-to-confirm ring (no accidental purchases).</summary>
    [Serializable]
    public sealed class TowerPanelSettings
    {
        [Tooltip("The panel easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.45f, 0.35f, 14f, 0.96f, 1.3f, 0.65f);

        [Tooltip("Seconds the button must be held to buy the next level.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _holdSeconds = 0.6f;

        [Tooltip("Seconds a full ring takes to drain back after letting go early (it never snaps to empty).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _drainSeconds = 0.35f;

        [Tooltip("How visible (0..1) the panel must be before a hold counts: no buying through a fade-in.")]
        [Range(0f, 1f)]
        [SerializeField] private float _armVisibility = 0.9f;

        [Tooltip("Seconds the panel glows after a purchase before showing the next level.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _celebrateSeconds = 1.4f;

        public RevealSettings Reveal => _reveal;

        public float HoldSeconds => _holdSeconds;

        public float DrainSeconds => _drainSeconds;

        public float ArmVisibility => _armVisibility;

        public float CelebrateSeconds => _celebrateSeconds;
    }
}
