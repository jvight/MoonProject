using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The HUD's cell row (docs/design/first-hour-v2 6.4): how it eases in and out, when it stays, how far it dims
    /// while the beam works and how its cells breathe. It exists once the charge has first dropped below the reveal
    /// share and shows while the cells are not full, while charging and for a few seconds after full.
    /// </summary>
    [Serializable]
    public sealed class CellRowSettings
    {
        [Tooltip("The row easing in as the cells start to empty, and out once full and settled.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(1.2f, 0.9f, 4f, 0.97f, 1.2f, 0.7f);

        [Tooltip("Seconds the row stays after the cells are full, so the last cell is seen filling.")]
        [Range(0f, 10f)]
        [SerializeField] private float _fullHoldSeconds = 4f;

        [Tooltip("The share of its brightness the row keeps while the beam works (it dims, never hides).")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _dimShare = 0.5f;

        [Tooltip("How fast the row eases between bright and dim (per second).")]
        [Range(1f, 12f)]
        [SerializeField] private float _dimRate = 5f;

        [Tooltip("Seconds for the cell in use to breathe once.")]
        [Range(0.8f, 6f)]
        [SerializeField] private float _breathSeconds = 2.4f;

        [Tooltip("Seconds for the last lit cell to breathe once on the sealed reserve (Low): slower, never red.")]
        [Range(2f, 10f)]
        [SerializeField] private float _lowBreathSeconds = 5f;

        [Tooltip("How far a breathing cell dims at the bottom of its breath (0 none, 1 out).")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _breathDepth = 0.45f;

        public RevealSettings Reveal => _reveal;

        public float FullHoldSeconds => _fullHoldSeconds;

        public float DimShare => _dimShare;

        public float DimRate => _dimRate;

        public float BreathSeconds => _breathSeconds;

        public float LowBreathSeconds => _lowBreathSeconds;

        public float BreathDepth => _breathDepth;
    }
}
