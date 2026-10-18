using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The card that says what a newly fitted kit lets 07 do (docs/design/first-hour-qa.md R1).</summary>
    [Serializable]
    public sealed class UnlockCardSettings
    {
        [Tooltip("The card easing in and out: soft, rising a little.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.7f, 1f, 14f, 0.96f, 1f, 0.7f);

        [Tooltip("Seconds the card rests fully visible while 07 stays put.")]
        [Range(2f, 10f)]
        [SerializeField] private float _holdSeconds = 3f;

        [Tooltip("Seconds the card always gets to be read: once they pass, driving off or opening build mode sends it " +
                 "away at once so the next step's line follows promptly.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _minReadSeconds = 1.5f;

        public RevealSettings Reveal => _reveal;

        public float HoldSeconds => _holdSeconds;

        public float MinReadSeconds => _minReadSeconds;
    }
}
