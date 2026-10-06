using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The scrap chip: no permanent HUD, it drifts in when the balance changes, counts, lingers and leaves.
    /// </summary>
    [Serializable]
    public sealed class ScrapChipSettings
    {
        [Tooltip("The chip easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.35f, 0.7f, -10f, 0.92f, 1.8f, 0.6f);

        [Tooltip("Seconds of counting per scrap of change (bounded by the min and max below).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _secondsPerScrap = 0.06f;

        [Tooltip("Shortest count, even for a single scrap.")]
        [Range(0f, 2f)]
        [SerializeField] private float _minCountSeconds = 0.35f;

        [Tooltip("Longest count, even for a big gift.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _maxCountSeconds = 1.4f;

        [Tooltip("Seconds the chip lingers after the count settles before it fades away.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lingerSeconds = 3f;

        public RevealSettings Reveal => _reveal;

        public float SecondsPerScrap => _secondsPerScrap;

        public float MinCountSeconds => _minCountSeconds;

        public float MaxCountSeconds => _maxCountSeconds;

        public float LingerSeconds => _lingerSeconds;
    }
}
