using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The cargo screen (docs/features/M3-25 §2.4): Tab or View opens it over a blur; the game does not pause.</summary>
    [Serializable]
    public sealed class CargoScreenSettings
    {
        [Tooltip("The screen easing in and out (about 200 ms: the grids slide in with it).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.2f, 0.18f, 18f, 0.98f, 1.8f, 0.7f);

        [Tooltip("Largest slot cell (px at 1080p); the cells shrink to fit when the screen is narrow or the text big.")]
        [Range(32f, 96f)]
        [SerializeField] private float _maxCell = 62f;

        [Tooltip("Smallest slot cell (px): below this the screen would be unreadable, so it stops shrinking.")]
        [Range(24f, 64f)]
        [SerializeField] private float _minCell = 34f;

        [Tooltip("Share of the panel's width 07's grid may use.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _widthShare = 0.82f;

        [Tooltip("Share of the panel's height the screen may use; the cells shrink until it fits.")]
        [Range(0.6f, 1f)]
        [SerializeField] private float _heightShare = 0.94f;

        [Tooltip("How much the cells shrink in one step while the screen is too tall (0.92 = 8 % smaller).")]
        [Range(0.5f, 0.99f)]
        [SerializeField] private float _shrinkStep = 0.92f;

        public RevealSettings Reveal => _reveal;

        public float MaxCell => _maxCell;

        public float MinCell => _minCell;

        public float WidthShare => _widthShare;

        public float HeightShare => _heightShare;

        public float ShrinkStep => _shrinkStep;
    }
}
